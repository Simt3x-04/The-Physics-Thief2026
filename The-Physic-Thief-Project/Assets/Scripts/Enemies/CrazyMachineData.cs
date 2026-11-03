using UnityEngine;

namespace PhysicsThief.Enemies
{
    [CreateAssetMenu(menuName = "Physics Thief/CrazyMachineData", fileName = "CrazyMachineData")]
    public class CrazyMachineData:EnemyData
    {
        [Header("Radiator")]
        [SerializeField] public float radiatorStartTemperature = 200f;
        [SerializeField] private float radiatorSafeTemperature = 50f;
        [SerializeField] private float ventHeatAmount = 40f;

        [Header("Nucleous")]
        [SerializeField] private float coreDamageMultiplier = 2f;

        public float RadiatorStartTemperature => radiatorStartTemperature;
        public float RadiatorSafeTemperature => radiatorSafeTemperature;
        public float VentHeatAmount => ventHeatAmount;
        public float CoreDamageMultiplier => coreDamageMultiplier;

        protected override void OnValidate()
        {
            base.OnValidate();
            radiatorStartTemperature = Mathf.Max(0f, radiatorStartTemperature);
            radiatorSafeTemperature = Mathf.Max(radiatorSafeTemperature, 0f);
            ventHeatAmount = Mathf.Max(0f, ventHeatAmount);
            coreDamageMultiplier = Mathf.Max(1f, coreDamageMultiplier);
        }
    }
}