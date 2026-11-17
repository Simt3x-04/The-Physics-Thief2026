using UnityEngine;

namespace PhysicsThief.Enemies
{
    [CreateAssetMenu(menuName = "Physics Thief/SpearBeetleData", fileName = "SpearBeetleData")]
    public class SpearBeetleData : EnemyData
    {

        [Header("Ranged Attack")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float projectileDamage = 5f;
        [SerializeField] private float shootRange = 10f;

        [Header("Armor")]
        [Range(0f,1f)][SerializeField] private float shatteredDamageReduction = 0f;
        [Range(0f,1f)][SerializeField] private float armorBreakHealthPercentage = 0.5f;
        [Range(0f,1f)][SerializeField] private float armorDamageReduction = 0.5f;

        public Projectile ProjectilePrefab => projectilePrefab;
        public float ProjectileDamage => projectileDamage;
        public float ShootRange => shootRange;
        public float ShatteredDamageReduction => shatteredDamageReduction;
        public float ArmorBreakHealthPercentage => armorBreakHealthPercentage;
        public float ArmorDamageReduction => armorDamageReduction;

        protected override void OnValidate()
        {
            base.OnValidate();
            projectileDamage = Mathf.Max(0f, projectileDamage);
            shootRange = Mathf.Max(0f, shootRange);
            shatteredDamageReduction = Mathf.Clamp01(shatteredDamageReduction);
            armorBreakHealthPercentage = Mathf.Clamp01(armorBreakHealthPercentage);
            armorDamageReduction = Mathf.Clamp01(armorDamageReduction);
            
        }
    }
}