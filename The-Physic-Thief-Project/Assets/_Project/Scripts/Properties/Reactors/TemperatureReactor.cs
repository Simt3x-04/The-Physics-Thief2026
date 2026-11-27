using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>What the heat is currently doing to an object.</summary>
    public enum ThermalState
    {
        Normal,
        Frozen,       // water -> solid ice (reversible)
        Evaporated,   // water -> gone for a while (reversible)
        Hot,          // superheated metal, hurts on contact (reversible)
        Burning,      // wood / paper on fire: burns until it is gone (irreversible)
        Melting       // metal melting: gone for good (irreversible)
    }

    /// <summary>
    /// Reacts to a PASTED temperature (GDD 3.2), depending on the object's MaterialTag:
    /// Water: &lt;= 0 freezes (becomes solid), &gt;= 100 evaporates.  Wood / Paper: &gt;= 100 ignites.
    /// Metal: &gt;= 70 superheats, &gt;= 500 melts.  Other materials do nothing visible.
    /// Reversible effects revert when the pasted value expires (GDD 3.8); burning and melting do not.
    /// The object's own BASE temperature never triggers anything (boiling water stays water, a hot stove stays a stove).
    /// Freezing enemies is up to the enemy scripts (EnemyBase.OnPhysicsAltered), not to this reactor.
    /// </summary>
    public class TemperatureReactor : ReactorBase
    {
        [Header("Thresholds (GDD 3.2, degrees C)")]
        [SerializeField] private float freezeThreshold = 0f;
        [SerializeField] private float superheatThreshold = 70f;
        [SerializeField] private float boilThreshold = 100f;
        [SerializeField] private float ignitionThreshold = 100f;
        [SerializeField] private float meltThreshold = 500f;

        [Header("Burning and melting")]
        [Tooltip("Seconds between ignition and the object being gone for good.")]
        [SerializeField] private float burnDuration = 1.5f;
        [SerializeField] private float meltDuration = 1f;

        [Header("Superheated metal")]
        [Tooltip("Placeholder damage per second to whatever touches hot metal. Tune with the group.")]
        [SerializeField] private float contactDamagePerSecond = 10f;
        [Tooltip("Meters around the metal where touching still counts.")]
        [SerializeField] private float contactSkin = 0.1f;

        [Header("Tints (placeholder visuals until real effects exist)")]
        [SerializeField] private Color frozenTint = new Color(0.65f, 0.88f, 1f, 1f);
        [SerializeField] private Color hotTint = new Color(1f, 0.35f, 0.15f, 1f);
        [SerializeField] private Color burningTint = new Color(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color meltingTint = new Color(1f, 0.85f, 0.55f, 1f);

        private Renderer[] renderers;
        private Collider[] colliders;
        private bool[] originalIsTrigger;
        private ObjectTint tint;
        private readonly HashSet<Component> victims = new HashSet<Component>();
        private bool consumed;   // burning / melting cannot be undone

        public ThermalState State { get; private set; }

        /// <summary>For audio and particle effects.</summary>
        public event Action<ThermalState> OnThermalStateChanged;

        protected override void Awake()
        {
            base.Awake();
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            tint = ObjectTint.For(gameObject);

            originalIsTrigger = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) originalIsTrigger[i] = colliders[i].isTrigger;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearTint();
        }

        protected override void HandlePropertyChange(AttributeType type, float newValue)
        {
            if (type != AttributeType.Temperature || consumed) return;

            // Only a pasted temperature triggers effects. When it expires the event arrives with the base value: back to normal.
            ThermalState next = physObj.HasOverride(AttributeType.Temperature)
                ? Resolve(physObj.GetMaterial(), newValue)
                : ThermalState.Normal;
            SetState(next);
        }

        /// <summary>Which thermal state a material is in at a given pasted temperature.</summary>
        private ThermalState Resolve(MaterialTag material, float temperature)
        {
            switch (material)
            {
                case MaterialTag.Water:
                    if (temperature <= freezeThreshold) return ThermalState.Frozen;
                    if (temperature >= boilThreshold) return ThermalState.Evaporated;
                    break;
                case MaterialTag.Wood:
                case MaterialTag.Paper:
                    if (temperature >= ignitionThreshold) return ThermalState.Burning;
                    break;
                case MaterialTag.Metal:
                    if (temperature >= meltThreshold) return ThermalState.Melting;
                    if (temperature >= superheatThreshold) return ThermalState.Hot;
                    break;
            }
            return ThermalState.Normal;
        }

        private void SetState(ThermalState next)
        {
            if (next == State) return;

            ExitState(State);
            State = next;
            EnterState(next);
            OnThermalStateChanged?.Invoke(next);
        }

        private void EnterState(ThermalState state)
        {
            switch (state)
            {
                case ThermalState.Frozen:
                    SetTint(frozenTint);
                    for (int i = 0; i < colliders.Length; i++) colliders[i].isTrigger = false;   // liquid (trigger) becomes a solid walkway
                    break;
                case ThermalState.Evaporated:
                    SetRenderingOff(true);
                    SetCollidersEnabled(false);
                    break;
                case ThermalState.Hot:
                    SetTint(hotTint);
                    break;
                case ThermalState.Burning:
                    consumed = true;
                    SetTint(burningTint);
                    StartCoroutine(DestroyAfter(burnDuration));
                    break;
                case ThermalState.Melting:
                    consumed = true;
                    SetTint(meltingTint);
                    StartCoroutine(DestroyAfter(meltDuration));
                    break;
            }
        }

        private void ExitState(ThermalState state)
        {
            switch (state)
            {
                case ThermalState.Frozen:
                    ClearTint();
                    for (int i = 0; i < colliders.Length; i++) colliders[i].isTrigger = originalIsTrigger[i];
                    break;
                case ThermalState.Evaporated:
                    SetRenderingOff(false);
                    SetCollidersEnabled(true);
                    break;
                case ThermalState.Hot:
                    ClearTint();
                    break;
            }
        }

        private IEnumerator DestroyAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Destroy(gameObject);   // destroyed and melted objects stay destroyed (GDD 3.8)
        }

        /// <summary>
        /// Superheated metal hurts whatever touches it. It uses an overlap check instead of collision callbacks,
        /// so it also works for static metal and for kinematic characters (the KCC player).
        /// </summary>
        private void FixedUpdate()
        {
            if (State != ThermalState.Hot || colliders.Length == 0) return;

            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);

            Collider[] touching = Physics.OverlapBox(bounds.center, bounds.extents + Vector3.one * contactSkin,
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

            victims.Clear();
            foreach (Collider other in touching)
            {
                if (other.transform.IsChildOf(transform)) continue;   // our own colliders

                Component victim = FindVictim(other);
                if (victim != null && victims.Add(victim)) DealDamage(victim, contactDamagePerSecond * Time.fixedDeltaTime);
            }
        }

        /// <summary>Prefers the real damage rules (player controller, enemies); falls back to a bare HealthComponent.</summary>
        private static Component FindVictim(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable is Component component) return component;
            return other.GetComponentInParent<HealthComponent>();
        }

        private static void DealDamage(Component victim, float amount)
        {
            if (victim is IDamageable damageable) damageable.TakeDamage(amount);
            else if (victim is HealthComponent health) health.TakeDamage(amount);
        }

        // ---------- visual helpers ----------

        private void SetTint(Color color)
        {
            tint.Set(this, color);
        }

        private void ClearTint()
        {
            if (tint != null) tint.Clear(this);
        }

        /// <summary>forceRenderingOff is separate from Renderer.enabled, so it does not clash with the timer bar's flicker.</summary>
        private void SetRenderingOff(bool off)
        {
            for (int i = 0; i < renderers.Length; i++) renderers[i].forceRenderingOff = off;
        }

        private void SetCollidersEnabled(bool state)
        {
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = state;
        }
    }
}
