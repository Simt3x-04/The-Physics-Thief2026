using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Core of the game: an object with the 5 physical properties. It keeps the base values and the temporary
    /// overrides (each with its own timer) and RAISES events when a value changes: reactors and HUD
    /// listen to them. It does not contain the effect logic (that lives in the reactors).
    /// </summary>
    [DisallowMultipleComponent]
    public class PhysicalObject : MonoBehaviour, IResettable
    {
        [Header("Base data")]
        [Tooltip("Shared asset with the base values. If empty, the fields below are used (handy for tests).")]
        [SerializeField] private PhysicalObjectDefinitionSO definition;
        [SerializeField] private string displayName;
        [SerializeField] private MaterialTag material;
        [Tooltip("Only the attributes that differ from the default: Mass 1, Velocity 0, Temperature 20, Magnetism 0, Hardness 1.")]
        [SerializeField] private List<PropertyValue> baselineProperties = new List<PropertyValue>();
        [Tooltip("Off for objects that can only be scanned (copy FROM them) and have no reactors to use a pasted value, " +
                 "e.g. apples and apple trees. The scanner then refuses the paste and keeps the slot.")]
        [SerializeField] private bool acceptsPastes = true;

        private readonly Dictionary<AttributeType, TemporaryOverride> activeOverrides = new Dictionary<AttributeType, TemporaryOverride>();
        private readonly List<AttributeType> expiredBuffer = new List<AttributeType>();

        private Vector3 launchDirection;

        public PhysicalObjectDefinitionSO Definition => definition;

        /// <summary>False for scan-only objects: the scanner does not paste on them (the slot is kept).</summary>
        public bool AcceptsPastes => acceptsPastes;

        /// <summary>Direction chosen when the current Velocity was pasted (zero if none). Read by RigidbodyReactor.</summary>
        public Vector3 LaunchDirection => launchDirection;

        /// <summary>Name shown by the scanner.</summary>
        public string DisplayName
        {
            get
            {
                if (definition != null) return definition.displayName;
                return string.IsNullOrEmpty(displayName) ? name : displayName;
            }
        }

        /// <summary>Arguments: changed attribute, new value (also when it goes back to the base value).</summary>
        public event Action<AttributeType, float> OnPropertyValueChanged;

        /// <summary>Fired when an override expires on its own (timer bar, "pop" sound).</summary>
        public event Action<AttributeType> OnOverrideExpired;

        private void Update()
        {
            if (activeOverrides.Count == 0) return;

            expiredBuffer.Clear();
            foreach (KeyValuePair<AttributeType, TemporaryOverride> pair in activeOverrides)
            {
                if (pair.Value.Tick(Time.deltaTime)) expiredBuffer.Add(pair.Key);
            }
            for (int i = 0; i < expiredBuffer.Count; i++)
            {
                RevertToBaseline(expiredBuffer[i]);
            }
        }

        /// <summary>
        /// Set by objects that move on their own (enemies): it returns their real speed in m/s, so that the scanner
        /// shows and copies it. The base value stays untouched: it is what drives a RigidbodyReactor.
        /// </summary>
        public Func<float> LiveVelocity { get; set; }

        /// <summary>Current value: the override if there is one, otherwise the live speed or the base value.</summary>
        public float GetValue(AttributeType type)
        {
            if (activeOverrides.TryGetValue(type, out TemporaryOverride o)) return o.property.value;
            if (type == AttributeType.Velocity && LiveVelocity != null) return LiveVelocity();
            return GetBaseValue(type);
        }

        public float GetBaseValue(AttributeType type)
        {
            if (definition != null) return definition.GetBaseValue(type);

            for (int i = 0; i < baselineProperties.Count; i++)
            {
                if (baselineProperties[i].type == type) return baselineProperties[i].value;
            }
            return PropertyRules.DefaultValue(type);
        }

        public MaterialTag GetMaterial()
        {
            return definition != null ? definition.material : material;
        }

        public bool HasOverride(AttributeType type)
        {
            return activeOverrides.ContainsKey(type);
        }

        /// <summary>Seconds left on the override (0 if there is none). Used by the timer bars.</summary>
        public float GetRemainingTime(AttributeType type)
        {
            return activeOverrides.TryGetValue(type, out TemporaryOverride o) ? Math.Max(0f, o.remainingTime) : 0f;
        }

        /// <summary>Total duration of the override (0 if there is none).</summary>
        public float GetDuration(AttributeType type)
        {
            return activeOverrides.TryGetValue(type, out TemporaryOverride o) ? o.duration : 0f;
        }

        /// <summary>The 5 attributes with their current values.</summary>
        public IReadOnlyList<PropertyValue> GetScannableProperties()
        {
            var result = new List<PropertyValue>();
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                result.Add(new PropertyValue(type, GetValue(type)));
            }
            return result;
        }

        /// <summary>
        /// Pastes a value for 'duration' seconds. The same attribute REPLACES the value and restarts the timer
        /// (values never add up). The value is limited by the GDD caps.
        /// </summary>
        public void ApplyProperty(AttributeType type, float newValue, float duration, Vector3 direction = default)
        {
            if (type == AttributeType.Velocity) launchDirection = direction;

            float clamped = PropertyRules.Clamp(type, newValue);
            activeOverrides[type] = new TemporaryOverride(new PropertyValue(type, clamped), duration);
            OnPropertyValueChanged?.Invoke(type, clamped);
        }

        /// <summary>Removes all overrides (room reset) and notifies the listeners.</summary>
        public void ResetState()
        {
            expiredBuffer.Clear();
            expiredBuffer.AddRange(activeOverrides.Keys);
            activeOverrides.Clear();
            for (int i = 0; i < expiredBuffer.Count; i++)
            {
                OnPropertyValueChanged?.Invoke(expiredBuffer[i], GetBaseValue(expiredBuffer[i]));
            }
        }

        private void RevertToBaseline(AttributeType type)
        {
            activeOverrides.Remove(type);
            OnPropertyValueChanged?.Invoke(type, GetBaseValue(type));
            OnOverrideExpired?.Invoke(type);
        }
    }
}
