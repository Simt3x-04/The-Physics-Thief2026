using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Flying hornet that attacks from a distance with poison darts.</summary>
    public class Hornet : EnemyBase
    {
        [SerializeField] private Projectile venomDartPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float hoverHeight = 3f;
        private bool isStalled;

        public override void Attack() { }
        public void HoverOutRanged() { }
        public void FireVenomDart() { }

        /// <summary>TODO: the hornet loses altitude and falls (e.g. after a property change).</summary>
        public void StallAndCrash() { }
    }
}
