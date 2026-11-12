using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>
    /// Applies Mass and Velocity (GDD 3.2) to the Unity Rigidbody.
    /// - Mass: the Rigidbody mass always follows the attribute (pasted value, then back to the original).
    /// - Velocity: while a velocity is pasted, the object is launched where the player is looking.
    /// - Light flyer (optional): floats without gravity and falls while its mass is at least 'groundedMass'.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyReactor : ReactorBase
    {
        [SerializeField] private Rigidbody rb;

        [Header("Velocity (GDD 3.2 and 3.9)")]
        [Tooltip("Hard cap on the Rigidbody speed, to avoid tunneling.")]
        [SerializeField] private float maxVelocity = 50f;

        [Header("Light flyer (GDD 3.2: 200 kg or more grounds light flyers)")]
        [SerializeField] private bool isLightFlyer;
        [SerializeField] private float groundedMass = 200f;
        [Tooltip("How fast a grounded flyer climbs back to its hover position once it is light again.")]
        [SerializeField] private float hoverReturnSpeed = 2f;
        [SerializeField] private float hoverMaxSpeed = 3f;

        private Vector3 hoverPosition;
        private bool groundedByMass;

        protected override void Awake()
        {
            base.Awake();
            if (rb == null) rb = GetComponent<Rigidbody>();

            hoverPosition = rb.position;
            rb.maxLinearVelocity = maxVelocity;
            UpdateMass(physObj.GetValue(AttributeType.Mass));   // start from the object's base mass
        }

        protected override void HandlePropertyChange(AttributeType type, float newValue)
        {
            if (type == AttributeType.Mass)
            {
                UpdateMass(newValue);
            }
            else if (type == AttributeType.Velocity && physObj.HasOverride(AttributeType.Velocity))
            {
                // Only while a velocity is pasted: when it expires nothing happens and the object keeps moving by itself.
                LaunchAsProjectile(newValue);
            }
        }

        private void UpdateMass(float newMass)
        {
            rb.mass = newMass;

            if (isLightFlyer)
            {
                groundedByMass = newMass >= groundedMass;
                rb.useGravity = groundedByMass;   // flyers have no gravity while light
            }
        }

        /// <summary>Moves the object in the direction the player is looking (GDD 3.2). Uses continuous collision (GDD 3.9).</summary>
        private void LaunchAsProjectile(float velocity)
        {
            Camera cam = Camera.main;
            Vector3 direction = cam != null ? cam.transform.forward : transform.forward;

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = direction * velocity;
        }

        private void FixedUpdate()
        {
            if (!isLightFlyer || groundedByMass) return;
            if (physObj.HasOverride(AttributeType.Velocity)) return;   // do not fight a launch

            // A light flyer floats and drifts back to the position where it started hovering.
            Vector3 toHover = hoverPosition - rb.position;
            rb.linearVelocity = Vector3.ClampMagnitude(toHover * hoverReturnSpeed, hoverMaxSpeed);
        }
    }
}
