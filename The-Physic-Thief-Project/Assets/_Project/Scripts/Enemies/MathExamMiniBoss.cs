using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Mini-boss that throws '4' and '5' grades. Projectiles reflected with high velocity/elasticity damage it.</summary>
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
