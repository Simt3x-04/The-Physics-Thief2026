using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Enemies
{
    /// <summary>Guerriero con scudo di foglie: spara chicchi di mais.</summary>
    public class CornSoldier : EnemyBase
    {
        [SerializeField] private Projectile kernelPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private bool hasShield = true;

        public override void Attack() { }
        public void FireKernelBurst() { }
        public void Defend() { }

        /// <summary>TODO: se Magnetism e' alto, hasShield = false.</summary>
        public override void OnPhysicsAltered(AttributeType type, float newValue) { }
    }
}
