using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Calabrone volante che attacca a distanza con dardi di veleno.</summary>
    public class Hornet : EnemyBase
    {
        [SerializeField] private Projectile venomDartPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float hoverHeight = 3f;
        private bool isStalled;

        public override void Attack() { }
        public void HoverOutRanged() { }
        public void FireVenomDart() { }

        /// <summary>TODO: il calabrone perde quota e precipita (es. dopo un cambio di proprieta').</summary>
        public void StallAndCrash() { }
    }
}
