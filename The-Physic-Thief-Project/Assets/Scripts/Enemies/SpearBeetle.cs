using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Coleottero con lancia che pattuglia i corridoi (mischia).</summary>
    public class SpearBeetle : EnemyBase
    {
        [SerializeField] private Transform[] patrolPoints;
        private SpearBeetleData beetleData;
        private bool isArmorShattered;

        private int currentPatrolIndex;

        protected override void Awake()
        {
            base.Awake();
            beetleData = data as SpearBeetleData;
            if(beetleData == null)
            {
                Debug.LogError($"No data", this);
                enabled = false; 
            }
        }

        public override void Attack() { }
        public void PatrolCorridor() { }
        public void ShatterArmor() { }
    }
}
