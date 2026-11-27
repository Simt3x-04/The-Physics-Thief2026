using UnityEngine;

namespace PhysicsThief.Enemies{
    public abstract class BossBase : EnemyBase{
        [Header("Boss")]
        [SerializeField] protected bool alwaysFaceTarget = true;

        protected override void Start(){
            base.Start();
            LockOnTarget();
        }

        protected override void Update(){
            base.Update();
            if(alwaysFaceTarget && target != null && !IsDead) FaceTarget(data.TurnSpeed);
        }

        protected void LockOnTarget(){
            AcquireTarget();

            if(target == null) Debug.LogWarning("No player found");

        }
    }
}
