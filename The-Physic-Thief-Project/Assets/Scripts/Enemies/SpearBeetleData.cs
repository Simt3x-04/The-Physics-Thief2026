using UnityEngine;

namespace PhysicsThief.Enemies
{
    [CreateAssetMenu(menuName = "Physics Thief/SpearBeetleData", fileName = "SpearBeetleData")]
    public class SpearBeetleData : EnemyData
    {
        [Header("Patrol")]
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float waitTimeAtPoint = 1f;
        [SerializeField] private float pointReachDistance = 0.3f;

        [Header("Armor")]
        [Range(0f,1f)]
        [SerializeField] private float shatteredDamageReduction = 0f; 

        public float PatrolSpeed => patrolSpeed;
        public float WaitTimeAtPoint => waitTimeAtPoint;
        public float PointReachDistance => pointReachDistance;
        public float ShatteredDamageReduction => shatteredDamageReduction;

        protected override void OnValidate()
        {
            base.OnValidate();
            patrolSpeed = Mathf.Max(0f, patrolSpeed);
            waitTimeAtPoint = Mathf.Max(0f, waitTimeAtPoint);
            pointReachDistance = Mathf.Max(0f, pointReachDistance);
            shatteredDamageReduction = Mathf.Max(0f, shatteredDamageReduction);
        }
    }
}