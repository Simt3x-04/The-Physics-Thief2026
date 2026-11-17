using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>Magnetic effects: attracts metal objects and disarms shielded enemies.</summary>
    public class MagnetismReactor : ReactorBase
    {
        [SerializeField] private float attractionRadius = 6f;
        [SerializeField] private float attractionForce = 20f;
        [SerializeField] private LayerMask metallicLayers;

        protected override void HandlePropertyChange(AttributeType type, float newValue) { }

        /// <summary>TODO: Use OverlapSphere on metal layers and apply a force toward this object.</summary>
        private void AttractMetallicObjects() { }

        /// <summary>TODO: find nearby enemies and call enemy.OnPhysicsAltered(Magnetism, value).</summary>
        private void StripEnemyShields() { }
    }
}
