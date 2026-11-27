using UnityEngine;

namespace PhysicsThief.Enemies
{
    [CreateAssetMenu(menuName = "Physics Thief/SpearBeetleData", fileName = "SpearBeetleData")]
    public class SpearBeetleData : EnemyData
    {


        [Header("Armor")]
        [Range(0f,1f)][SerializeField] private float shatteredDamageReduction = 0f;
        [Range(0f,1f)][SerializeField] private float armorBreakHealthPercentage = 0.5f;
        [Range(0f,1f)][SerializeField] private float armorDamageReduction = 0.5f;

        public float ShatteredDamageReduction => shatteredDamageReduction;
        public float ArmorBreakHealthPercentage => armorBreakHealthPercentage;
        public float ArmorDamageReduction => armorDamageReduction;

        protected override void OnValidate()
        {
            base.OnValidate();
            shatteredDamageReduction = Mathf.Clamp01(shatteredDamageReduction);
            armorBreakHealthPercentage = Mathf.Clamp01(armorBreakHealthPercentage);
            armorDamageReduction = Mathf.Clamp01(armorDamageReduction);
            
        }
    }
}