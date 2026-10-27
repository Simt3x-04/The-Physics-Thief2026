using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Coleottero con lancia che pattuglia i corridoi (mischia).</summary>
    public class SpearBeetle : EnemyBase
    {
        [SerializeField] private Transform[] patrolPoints;
        private bool isArmorShattered;

        public override void Attack() { }
        public void PatrolCorridor() { }
        public void ShatterArmor() { }
    }
}
