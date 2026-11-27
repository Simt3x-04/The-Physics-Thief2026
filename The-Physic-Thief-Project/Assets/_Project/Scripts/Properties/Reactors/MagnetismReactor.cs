using System.Collections.Generic;
using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>
    /// Magnetism (GDD 3.2): an object with enough magnetism pulls nearby METAL objects (MaterialTag.Metal) towards itself.
    /// It uses the CURRENT value, base or pasted: a natural magnet (the Magnet, the Asteroid) always attracts, and a
    /// pasted magnetism turns any object into a magnet until it expires.
    /// Pulled objects need a Rigidbody. Their mass matters, so Mass and Magnetism combine: light objects fly to the
    /// magnet, a 20 kg crate slides, a 600 kg block stays put (see 'Mass Influence'). Metal objects without a
    /// Rigidbody (gates, walls) simply stay where they are.
    /// Stripping enemy shields and helmets is up to the enemy scripts (EnemyBase.OnPhysicsAltered), not this reactor.
    /// </summary>
    public class MagnetismReactor : ReactorBase
    {
        [Header("Field (GDD 3.2: 0 none, 1 moderate, 2 strong)")]
        [Tooltip("Below this magnetism (in tesla) the object attracts nothing: a fridge (0.3 T) is too weak.")]
        [SerializeField] private float minimumField = 0.5f;
        [Tooltip("Reach in meters for each tesla: 1 T pulls up to this far, 2 T twice as far.")]
        [SerializeField] private float radiusPerTesla = 5f;

        [Header("Pull")]
        [Tooltip("Acceleration (m/s^2) at point-blank range, for each tesla, on an object of 'Reference Mass'. " +
                 "Note: to start moving, a body on the ground needs about 6 m/s^2 (friction), whatever its mass.")]
        [SerializeField] private float accelerationPerTesla = 40f;
        [SerializeField] private float referenceMass = 1f;
        [Tooltip("0 = every object is pulled equally, whatever its mass. 1 = realistic force: a heavy object barely moves. " +
                 "0.5 = light objects fly, a 20 kg crate slides, 600 kg stays put.")]
        [Range(0f, 1f)]
        [SerializeField] private float massInfluence = 0.5f;
        [Tooltip("Fraction of the pull that is still felt at the very edge of the reach (1 = no fade). " +
                 "Without it, objects near the edge would never overcome friction.")]
        [Range(0f, 1f)]
        [SerializeField] private float edgeStrength = 0.3f;
        [Tooltip("Hard cap on the pull (m/s^2), so very light objects are not shot like bullets.")]
        [SerializeField] private float maxAcceleration = 60f;
        [Tooltip("Objects closer than this (meters) are not pulled any more, to avoid jitter on contact.")]
        [SerializeField] private float stopDistance = 0.6f;
        [SerializeField] private LayerMask attractionLayers = ~0;

        [Header("Tint of a pasted magnetism (placeholder visual)")]
        [SerializeField] private Color magnetTint = new Color(0.72f, 0.6f, 1f, 1f);

        private readonly Collider[] buffer = new Collider[64];
        private readonly HashSet<Rigidbody> pulled = new HashSet<Rigidbody>();
        private ObjectTint tint;

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
            if (type != AttributeType.Magnetism) return;

            // Only a PASTED magnetism is tinted: natural magnets keep their normal look.
            bool pastedMagnet = physObj.HasOverride(AttributeType.Magnetism) && newValue >= minimumField;
            if (pastedMagnet) tint.Set(this, magnetTint);
            else tint.Clear(this);
        }

        private void FixedUpdate()
        {
            float field = physObj.GetValue(AttributeType.Magnetism);
            if (field < minimumField) return;

            float radius = radiusPerTesla * field;
            Vector3 center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, radius, buffer, attractionLayers, QueryTriggerInteraction.Ignore);

            pulled.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider other = buffer[i];
                Rigidbody body = other.attachedRigidbody;
                if (body == null || body.isKinematic) continue;      // only free bodies can be pulled
                if (other.transform.IsChildOf(transform)) continue;  // not ourselves
                if (!pulled.Add(body)) continue;                     // one pull per body per physics step

                PhysicalObject target = other.GetComponentInParent<PhysicalObject>();
                if (target == null || target.GetMaterial() != MaterialTag.Metal) continue;   // only metal is attracted

                Vector3 toMagnet = center - body.position;
                float distance = toMagnet.magnitude;
                if (distance < stopDistance) continue;

                // Stronger when close (never below edgeStrength) and weaker on heavy objects.
                float closeness = 1f - Mathf.Clamp01(distance / radius);
                float falloff = Mathf.Lerp(edgeStrength, 1f, closeness);
                float massFactor = Mathf.Pow(referenceMass / Mathf.Max(body.mass, 0.01f), massInfluence);
                float acceleration = Mathf.Min(accelerationPerTesla * field * falloff * massFactor, maxAcceleration);

                // Acceleration mode ignores mass, which is already accounted for in massFactor.
                body.AddForce(toMagnet / distance * acceleration, ForceMode.Acceleration);
            }
        }

        /// <summary>Select the magnet in the Scene view to see how far it reaches (useful while tuning).</summary>
        private void OnDrawGizmosSelected()
        {
            PhysicalObject source = physObj != null ? physObj : GetComponent<PhysicalObject>();
            if (source == null) return;

            float field = source.GetValue(AttributeType.Magnetism);
            if (field < minimumField) return;

            Gizmos.color = magnetTint;
            Gizmos.DrawWireSphere(transform.position, radiusPerTesla * field);
        }
    }
}
