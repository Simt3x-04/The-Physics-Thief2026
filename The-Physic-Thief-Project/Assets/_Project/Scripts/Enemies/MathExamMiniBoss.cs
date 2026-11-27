using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Mini-boss that throws '4' and '5' grades. Projectiles reflected with high velocity/elasticity damage it.</summary>
    public class MathExamMiniBoss : BossBase
    {
        [SerializeField] private Transform firePoint;

        private MathExamData examData;

        protected override void Awake()
        {
            base.Awake();
            examData = data as MathExamData;
            if(examData == null){
                Debug.LogError($"No MathExamData!");
                enabled = false;
            }
        }

        public override void Attack(){
            //TODO: choose the projectil and call FireGradeProjectiles(gradeValue)
        }

        public void FireGradeProjectiles(int gradeValue){
            //TODO: shoot examData.GradeProjectilePrefab from firePoint; the damage is examData.GradeFourDamage or Five
        }

        public void TakeReflectedDamage(Projectile reflected) { 
            //TODO: if the projectile is reflected and hits the enemy inflict reflect.Damage * examData.reflectedDamageMultiplier using healthComp.TakeDamage
        }
    }
}
