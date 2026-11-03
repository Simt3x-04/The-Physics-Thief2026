using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Mini-boss che lancia voti '4' e '5'. I proiettili riflessi con alta velocita'/elasticita' lo danneggiano.</summary>
    public class MathExamMiniBoss : EnemyBase
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
            //TODO: shoot examData.GradeProjectilePrefab da firePoint; il danno è examData.GradeFourDamage o Five
        }

        public void TakeReflectedDamage(Projectile reflected) { 
            //TODO: if the projectile is reflected and hits the enemy inflict reflect.Damage * examData.reflectedDamageMultiplier using healthComp.TakeDamage
        }
    }
}
