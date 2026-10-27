using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Mini-boss che lancia voti '4' e '5'. I proiettili riflessi con alta velocita'/elasticita' lo danneggiano.</summary>
    public class MathExamMiniBoss : EnemyBase
    {
        [SerializeField] private Projectile gradeProjectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float gradeFourDamage = 15f;
        [SerializeField] private float gradeFiveDamage = 25f;

        public override void Attack() { }
        public void FireGradeProjectiles(int gradeValue) { }
        public void TakeReflectedDamage(Projectile reflected) { }
    }
}
