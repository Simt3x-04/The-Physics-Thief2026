using UnityEngine;

namespace PhysicsThief.Enemies{
    public class CombatState : IEnemyState{
        private float loseSightTimer;

        public void Enter(EnemyBase owner){
            loseSightTimer = 0f;
        }

        public void Tick(EnemyBase owner){
            EnemyData d = owner.Data;
            Transform target = owner.Target;

            if(target == null || owner.DistanceToTarget()>d.LoseTargetRange){
                GiveUp(owner);
                return;
            }

            bool canSee = owner.HasLineOfSight();
            if(canSee){
                loseSightTimer = 0f;
            }else{
                loseSightTimer += Time.deltaTime;
                if(loseSightTimer >= d.LoseSightTime){
                    GiveUp(owner);
                    return;
                }
            }

            if(!canSee || owner.DistanceToTarget() > d.AttackRange){
                owner.MoveTowards(target.position, d.MoveSpeed);
            }else{
                owner.Stop();
                owner.FaceTarget(d.TurnSpeed);
                owner.TryAttack();
            }
        }

        public void Exit(EnemyBase owner){
            owner.Stop();
        }

        public void GiveUp(EnemyBase owner){
            owner.StateMachine.ChangeState(owner.CreateIdleState(), owner);
        }
    }
}
