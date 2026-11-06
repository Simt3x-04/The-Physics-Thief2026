using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Core of the game: any object/enemy with modifiable physical properties.
    /// Maintains base values ​​plus temporary overrides, and NOTIFIES Reactors whenever a value changes
    /// (Observer pattern). It does not contain the logic for reactions; that resides in ReactorBase.
    /// </summary>
    [DisallowMultipleComponent]
    public class PhysicalObject : MonoBehaviour, IResettable
    {
        [SerializeField] private PhysicalObjectDefinitionSO definition;

        private readonly Dictionary<AttributeType, float> currentValues = new Dictionary<AttributeType, float>();
        private readonly Dictionary<AttributeType, TemporaryOverride> activeOverrides = new Dictionary<AttributeType, TemporaryOverride>();

        public PhysicalObjectDefinitionSO Definition => definition;

        /// <summary>Topics: property changed, new value.</summary>
        public event Action<AttributeType, float> OnPropertyValueChanged;

        /// <summary>Triggered when an override expires (the UI displays a warning, and the audio makes a "pop" sound).</summary>
        public event Action<AttributeType> OnOverrideExpired;

        /// <summary>TODO: populate currentValues ​​from the base values ​​in the definition.</summary>
        private void Awake() { }

        /// <summary>TODO: Call Tick for each TemporaryOverride; if expired, call RevertToBaseline.</summary>
        private void Update() { }

        /// <summary>TODO: current value (override if present, otherwise base).</summary>
        public float GetValue(AttributeType type) { return default; }

        /// <summary>TODO: initial value of the definition.</summary>
        public float GetBaseValue(AttributeType type) { return default; }

        public MaterialTag GetMaterial() { return default; }

        public bool HasOverride(AttributeType type) { return default; }

        /// <summary>TODO: list of properties displayed/copyable from the scanner.</summary>
        public IReadOnlyList<PropertyValue> GetScannableProperties() { return default; }

        /// <summary>TODO: create/replace the TemporaryOverride, update currentValues, and invoke OnPropertyValueChanged.</summary>
        public void ApplyProperty(AttributeType type, float newValue, float duration) { }

        /// <summary>TODO: remove all overrides and restore the base values ​​(ResetPuzzleRoom).</summary>
        public void ResetState() { }

        /// <summary>TODO: remove the override, restore the base, and invoke OnPropertyValueChanged and OnOverrideExpired.</summary>
        private void RevertToBaseline(AttributeType type) { }
    }
}
