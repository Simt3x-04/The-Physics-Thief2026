using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>Effetti magnetici: attrae oggetti metallici e disarma i nemici con scudo.</summary>
    public class MagnetismReactor : ReactorBase
    {
        [SerializeField] private float attractionRadius = 6f;
        [SerializeField] private float attractionForce = 20f;
        [SerializeField] private LayerMask metallicLayers;

        protected override void HandlePropertyChange(AttributeType type, float newValue) { }

        /// <summary>TODO: OverlapSphere sui layer metallici e applica una forza verso questo oggetto.</summary>
        private void AttractMetallicObjects() { }

        /// <summary>TODO: trova i nemici vicini e chiama enemy.OnPhysicsAltered(Magnetism, valore).</summary>
        private void StripEnemyShields() { }
    }
}
