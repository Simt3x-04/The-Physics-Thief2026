using System;
using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    public enum HardnessState
    {
        Normal,
        Fragile,   // pasted hardness <= fragile threshold
        Rigid      // pasted hardness >= rigid threshold
    }

    /// <summary>
    /// Hardness (GDD 3.2). Hardness is mostly DATA: the object's value is read by other systems (e.g. the damage
    /// formula, a projectile deciding whether it is blocked, fall damage on a soft surface).
    /// This reactor only adds what happens when a hardness is PASTED:
    /// - Fragile (&lt;= 2): the object shatters for good if it hits something fast enough (GDD 3.8).
    /// - Rigid (&gt;= 8): diamond-rigid, tinted. Bulletproof cover and stable bridges are decided by whoever reads IsRigid.
    /// The base hardness never triggers anything (a pillow does not shatter just because its hardness is 1).
    /// </summary>
    public class HardnessReactor : ReactorBase
    {
        [Header("Thresholds (GDD 3.2)")]
        [SerializeField] private float fragileThreshold = 2f;
        [SerializeField] private float rigidThreshold = 8f;

        [Header("Fragile objects")]
        [Tooltip("A fragile object (pasted hardness) shatters when it hits something at least this fast, in m/s.")]
        [SerializeField] private float shatterImpactSpeed = 4f;

        [Header("Tints (placeholder visuals)")]
        [SerializeField] private Color fragileTint = new Color(1f, 0.8f, 0.88f, 1f);
        [SerializeField] private Color rigidTint = new Color(0.7f, 0.95f, 1f, 1f);

        private ObjectTint tint;

        public HardnessState State { get; private set; }

        /// <summary>For other systems: based on the CURRENT hardness (base or pasted).</summary>
        public bool IsFragile => physObj.GetValue(AttributeType.Hardness) <= fragileThreshold;
        public bool IsRigid => physObj.GetValue(AttributeType.Hardness) >= rigidThreshold;

        /// <summary>For audio and particle effects.</summary>
        public event Action<HardnessState> OnHardnessStateChanged;

        protected override void Awake()
        {
            base.Awake();
            tint = ObjectTint.For(gameObject);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (tint != null) tint.Clear(this);
        }

        protected override void HandlePropertyChange(AttributeType type, float newValue)
        {
            if (type != AttributeType.Hardness) return;

            // Only a pasted value triggers effects. When it expires the event arrives with the base value: back to normal.
            HardnessState next = physObj.HasOverride(AttributeType.Hardness) ? Resolve(newValue) : HardnessState.Normal;
            SetState(next);
        }

        private HardnessState Resolve(float hardness)
        {
            if (hardness <= fragileThreshold) return HardnessState.Fragile;
            if (hardness >= rigidThreshold) return HardnessState.Rigid;
            return HardnessState.Normal;
        }

        private void SetState(HardnessState next)
        {
            if (next == State) return;
            State = next;

            switch (next)
            {
                case HardnessState.Fragile: tint.Set(this, fragileTint); break;
                case HardnessState.Rigid: tint.Set(this, rigidTint); break;
                default: tint.Clear(this); break;
            }
            OnHardnessStateChanged?.Invoke(next);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (State != HardnessState.Fragile) return;

            if (collision.relativeVelocity.magnitude >= shatterImpactSpeed)
            {
                Destroy(gameObject);   // shattered objects stay destroyed (GDD 3.8). TODO: shatter particles and sound.
            }
        }
    }
}
