using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>Thermal reactions based on temperature and MaterialTag (example threshold values).</summary>
    public class TemperatureReactor : ReactorBase
    {
        [SerializeField] private float freezeThreshold = 0f;
        [SerializeField] private float boilThreshold = 100f;
        [SerializeField] private float ignitionThreshold = 300f;
        [SerializeField] private float meltThreshold = 1000f;

        protected override void HandlePropertyChange(AttributeType type, float newValue) { }

        /// <summary>TODO: compare the temperature against the thresholds and trigger the appropriate response.</summary>
        private void EvaluateThresholds(float temperature) { }

        private void FreezeWaterToIce() { }
        private void EvaporateWater() { }
        private void IgniteWood() { }
        private void MeltMetal() { }
    }
}
