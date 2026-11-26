using PhysicsThief.Core;
using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Si nasconde sotto terra e carica Gino a testa bassa.</summary>
    public class AngryCarrot : EnemyBase
    {
        private AngryCarrotData carrotData;
        private IDamageable targetDamageable;    
        public AngryCarrotData CarrotData=>carrotData;

        protected override void Awake()
        {
            base.Awake();
            if(!enabled) return;
            carrotData = data as AngryCarrotData;
            if(carrotData == null)
            {
                Debug.LogError("No AngryCarrot data");
                enabled = false;
            }
        }

        public override IEnemyState CreateIdleState() => new RoamState();
        public override IEnemyState CreateCombatState()=> new AngryCarrotDashState();

        public bool IsTargetInMeleeReach()
        {
            if(target == null) return false;
            if(DistanceToTarget() > data.AttackRange) return false;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y =0f;
            if(toTarget.sqrMagnitude < 0.0001f) return true;

            return Vector3.Angle(transform.forward, toTarget) <= carrotData.MeleeHalfAngle;
        }

        public override void Attack()
        {
            if(!IsTargetInMeleeReach()) return;
                    
            if(targetDamageable == null) targetDamageable = target.GetComponentInParent<IDamageable>();

            targetDamageable?.TakeDamage(data.AttackDamage);
        }




    }
}
