using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Enemies
{
    /// <summary>Warrior with a leaf shield: shoots corn kernels.</summary>
    public class CornSoldier : EnemyBase
    {
        [SerializeField] private Projectile kernelPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private bool hasShield = true;

        public override void Attack() { }
        public void FireKernelBurst() { }
        public void Defend() { }

        /// <summary>TODO: if Magnetism is high, hasShield = false.</summary>
        public override void OnPhysicsAltered(AttributeType type, float newValue) { }
    }
}
