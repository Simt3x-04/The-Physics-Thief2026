using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>Applica Mass e Velocity al Rigidbody di Unity.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyReactor : ReactorBase
    {
        [SerializeField] private Rigidbody rb;

        protected override void HandlePropertyChange(AttributeType type, float newValue) { }

        /// <summary>TODO: rb.mass = newMass.</summary>
        private void UpdateMass(float newMass) { }

        /// <summary>TODO: imposta la velocita' del Rigidbody e/o lancia l'oggetto come proiettile.</summary>
        private void LaunchAsProjectile(float velocity) { }
    }
}
