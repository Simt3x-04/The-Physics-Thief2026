using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>Reazioni termiche in base alla temperatura e al MaterialTag (valori soglia di esempio).</summary>
    public class TemperatureReactor : ReactorBase
    {
        [SerializeField] private float freezeThreshold = 0f;
        [SerializeField] private float boilThreshold = 100f;
        [SerializeField] private float ignitionThreshold = 300f;
        [SerializeField] private float meltThreshold = 1000f;

        protected override void HandlePropertyChange(AttributeType type, float newValue) { }

        /// <summary>TODO: confronta la temperatura con le soglie e chiama la reazione giusta.</summary>
        private void EvaluateThresholds(float temperature) { }

        private void FreezeWaterToIce() { }
        private void EvaporateWater() { }
        private void IgniteWood() { }
        private void MeltMetal() { }
    }
}
