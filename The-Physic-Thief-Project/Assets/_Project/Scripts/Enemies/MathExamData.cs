using UnityEngine;

namespace PhysicsThief.Enemies
{
    [CreateAssetMenu(menuName = "Physics Thief/MathExamData", fileName = "MathExamData")]
    public class MathExamData:EnemyData{
        [Header("GradesProjectile")]
        [SerializeField] private Projectile gradeProjectilePrefab;
        [SerializeField] private float gradeFourDamage = 25f;
        [SerializeField] private float gradeFiveDamage = 15f;

        [Header("ReflectedProj")]
        [SerializeField] float minReflectedSpeed = 10f;
        [SerializeField] float minReflectedElasticity = 0.5f;
        [SerializeField] float reflectedDamageMultiplier = 1.5f;

        public Projectile GradeProjectilePrefab => gradeProjectilePrefab;
        public float GradeFourDamage => gradeFourDamage;
        public float GradeFiveDamage => gradeFiveDamage;
        public float MinReflectedSpeed => minReflectedSpeed;
        public float MinReflectedElasticity => minReflectedElasticity;
        public float ReflectedDamageMultiplier => reflectedDamageMultiplier;

        protected override void OnValidate()
        {
            base.OnValidate();
            gradeFourDamage = Mathf.Max(0f, gradeFourDamage);
            gradeFiveDamage = Mathf.Max(0f, gradeFiveDamage);
            minReflectedSpeed = Mathf.Max(0f, minReflectedSpeed);
            minReflectedElasticity = Mathf.Max(0f, minReflectedElasticity);
            reflectedDamageMultiplier = Mathf.Max(0f, reflectedDamageMultiplier);
        }
    }
}
