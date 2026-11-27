using UnityEngine;

namespace PhysicsThief.Enemies
{
///<summary>
/// THe enemy walk in a straight line inside of a circle of radius RoamRadius starting from a HomePosition
/// When it reaches the border is stops, waits a RoamPauseTime and then turn around of a random angle
    public class RoamState: IEnemyState{
        private enum Phase {Walking, Pausing, Turning}
       
        private const float MaxAngleFromInward = 75f;
        private Phase phase;
        private Vector3 heading; //Current direction
        private Vector3 nextHeading; //Direction after the pause
        private float timer; //stop time when reaching the border

        public void Enter(EnemyBase owner){
            heading = IsOutsideArea(owner) ? InwardDirection(owner) : RandomDirection();
            phase = Phase.Walking;
        }

        public void Tick(EnemyBase owner){
            if(owner.TrySpotTarget()){
                owner.StateMachine.ChangeState(owner.CreateCombatState(), owner);
                return;
            }

            EnemyData d= owner.Data;
            switch(phase){
                case Phase.Walking:
                    if(HitBoundary(owner)){
                        owner.Stop();
                        timer = d.RoamPauseTime;
                        phase = Phase.Pausing;
                        return;
                    }
                    owner.MoveTowards(owner.transform.position + heading, d.MoveSpeed);
                    break;
                
                case Phase.Pausing:
                    timer -= Time.deltaTime;
                    if(timer <= 0f)
                    {
                        nextHeading = PickInwardHeading(owner);
                        phase = Phase.Turning;
                    }
                    break;

                case Phase.Turning:
                    Quaternion targetRot = Quaternion.LookRotation(nextHeading);
                    owner.transform.rotation = Quaternion.RotateTowards(owner.transform.rotation, targetRot, d.TurnSpeed*Time.deltaTime);
                    if(Quaternion.Angle(owner.transform.rotation, targetRot)< 1f)
                    {
                        heading = nextHeading;
                        phase = Phase.Walking;
                    }
                    break;
            } 
        }

        public void Exit (EnemyBase owner){
            owner.Stop();
        }

        private static Vector3 Flat(Vector3 v){
            v.y = 0f;
            return v;
        }

        private static bool IsOutsideArea(EnemyBase owner){
            Vector3 fromHome = Flat(owner.transform.position - owner.HomePosition);
            return fromHome.magnitude >= owner.Data.RoamRadius;
        }

        private bool HitBoundary(EnemyBase owner){
            Vector3 fromHome = Flat(owner.transform.position - owner.HomePosition);
            if(fromHome.magnitude < owner.Data.RoamRadius) return false;
            return Vector3.Dot(heading, fromHome.normalized) > 0f;
        }

        private static Vector3 InwardDirection(EnemyBase owner){
            Vector3 inward = Flat(owner.HomePosition - owner.transform.position);
            return inward.sqrMagnitude < 0.0001f ? RandomDirection() : inward.normalized;
        }

        private static Vector3 PickInwardHeading(EnemyBase owner){
            float angle = Random.Range(-MaxAngleFromInward, MaxAngleFromInward);
            return (Quaternion.Euler(0f, angle, 0f)*InwardDirection(owner)).normalized;
        }

        private static Vector3 RandomDirection(){
            return Quaternion.Euler(0f,Random.Range(0f, 360f), 0f)*Vector3.forward;
        }
    }
}

