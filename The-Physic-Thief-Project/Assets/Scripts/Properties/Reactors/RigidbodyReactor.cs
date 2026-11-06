using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>Apply Mass and Velocity to the Unity Rigidbody.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyReactor : ReactorBase
    {
        [SerializeField] private Rigidbody rb;

        protected override void HandlePropertyChange(AttributeType type, float newValue) { }

        /// <summary>TODO: rb.mass = newMass.</summary>
        private void UpdateMass(float newMass) { }

        /// <summary>TODO: sets the Rigidbody's velocity and/or launches the object as a projectile.</summary>
        private void LaunchAsProjectile(float velocity) { }
    }
}
