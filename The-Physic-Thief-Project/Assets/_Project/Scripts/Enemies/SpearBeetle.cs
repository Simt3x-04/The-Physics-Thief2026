using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Beetle with a spear that patrols the corridors (melee).</summary>
    public class SpearBeetle : EnemyBase
    {
        [SerializeField] private Transform[] patrolPoints;
        private bool isArmorShattered;

        public override void Attack() { }
        public void PatrolCorridor() { }
        public void ShatterArmor() { }
    }
}
