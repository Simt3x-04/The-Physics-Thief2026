using UnityEngine;
namespace PhysicsThief.Enemies
{
    public class AngryCarrotDashState : IEnemyState
    {
        private enum Phase {Dash, Recover}
        private Phase phase;
        private float phaseTimer;
        private float loseSightTimer;
        private Vector3 heading;

        public void Enter(EnemyBase owner)
        {
            loseSightTimer = 0f;
            StartDash((AngryCarrot)owner);
        }

         public void Tick(EnemyBase owner)
        {
            var carrot = (AngryCarrot) owner;
            EnemyData d = carrot.Data;

            if (carrot.IsDead)
            {
                carrot.Stop();
                return;
            }

            if (carrot.Target == null || carrot.DistanceToTarget() > d.LoseTargetRange)
            {
                GiveUp(carrot);
                return;
            }

            if (carrot.HasLineOfSight())
            {
                loseSightTimer = 0f;
            }
            else
            {
                loseSightTimer+=Time.deltaTime;
                if(loseSightTimer >= d.LoseSightTime)
                {
                    GiveUp(carrot);
                    return;
                }
            }

            switch (phase)
            {
                case Phase.Dash: TickDash(carrot); break;
                case Phase.Recover: TickRecover(carrot); break;
            }
        }

         public void Exit(EnemyBase owner)
        {
            owner.Stop();
        }

        private void TickDash(AngryCarrot carrot)
        {
            if (carrot.IsTargetInMeleeReach())
            {
                carrot.Stop();
                carrot.TryAttack();
                StartRecover(carrot);
                return;
            }

            phaseTimer -= Time.deltaTime;
            if(phaseTimer <= 0f)
            {
                StartRecover(carrot);
                return;
            }

            Vector3 toTarget = carrot.Target.position - carrot.transform.position;
            toTarget.y = 0f;
            if(toTarget.sqrMagnitude > 0.0001f)
            {
                float maxRadians = carrot.CarrotData.DashTurnSpeed*Mathf.Deg2Rad*Time.deltaTime;
                heading = Vector3.RotateTowards(heading, toTarget.normalized, maxRadians, 0f);
            }
            carrot.MoveTowards(carrot.transform.position + heading, carrot.CarrotData.ChargeSpeed);
        }

        private void TickRecover(AngryCarrot carrot)
        {
            carrot.Stop();
            carrot.FaceTarget(carrot.Data.TurnSpeed);

            if(carrot.IsTargetInMeleeReach()) carrot.TryAttack();

            phaseTimer -= Time.deltaTime;

            if(phaseTimer <= 0f) StartDash(carrot);
        }

        private void StartDash(AngryCarrot carrot)
        {
            phase = Phase.Dash;
            phaseTimer = carrot.CarrotData.DashDuration;

            Vector3 toTarget = carrot.Target != null ? carrot.Target.position - carrot.transform.position : carrot.transform.forward;
            toTarget.y = 0f;

            heading = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : carrot.transform.forward;
        }

        private void StartRecover(AngryCarrot carrot)
        {
            phase = Phase.Recover;
            phaseTimer = carrot.CarrotData.ChargeCooldown;
        }

        private void GiveUp(AngryCarrot carrot){
            carrot.StateMachine.ChangeState(carrot.CreateIdleState(), carrot);
        }
    }
    
}

