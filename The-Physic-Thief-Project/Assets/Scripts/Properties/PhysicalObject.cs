using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Cuore del gioco: un oggetto con le 5 proprieta' fisiche. Tiene i valori base e gli override
    /// temporanei (ognuno col suo timer) e AVVISA con eventi quando un valore cambia: i Reactor e l'HUD
    /// ascoltano. Non contiene la logica degli effetti (quella sta nei Reactor).
    /// </summary>
    [DisallowMultipleComponent]
    public class PhysicalObject : MonoBehaviour, IResettable
    {
        [Header("Dati base")]
        [Tooltip("Asset condiviso con i valori base. Se vuoto si usano i campi qui sotto (comodo per i test).")]
        [SerializeField] private PhysicalObjectDefinitionSO definition;
        [SerializeField] private string displayName;
        [SerializeField] private MaterialTag material;
        [Tooltip("Solo gli attributi diversi dal default: Mass 1, Velocity 0, Temperature 20, Magnetism 0, Hardness 1.")]
        [SerializeField] private List<PropertyValue> baselineProperties = new List<PropertyValue>();

        private readonly Dictionary<AttributeType, TemporaryOverride> activeOverrides = new Dictionary<AttributeType, TemporaryOverride>();
        private readonly List<AttributeType> expiredBuffer = new List<AttributeType>();

        public PhysicalObjectDefinitionSO Definition => definition;

        /// <summary>Nome mostrato dallo scanner.</summary>
        public string DisplayName
        {
            get
            {
                if (definition != null) return definition.displayName;
                return string.IsNullOrEmpty(displayName) ? name : displayName;
            }
        }

        /// <summary>Argomenti: attributo cambiato, nuovo valore (anche quando torna al valore base).</summary>
        public event Action<AttributeType, float> OnPropertyValueChanged;

        /// <summary>Scatta quando un override scade da solo (barra timer, suono "pop").</summary>
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

        /// <summary>Valore attuale: l'override se c'e', altrimenti il valore base.</summary>
        public float GetValue(AttributeType type)
        {
            return activeOverrides.TryGetValue(type, out TemporaryOverride o) ? o.property.value : GetBaseValue(type);
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

        /// <summary>Secondi rimasti all'override (0 se non ce n'e'). Serve alle barre timer.</summary>
        public float GetRemainingTime(AttributeType type)
        {
            return activeOverrides.TryGetValue(type, out TemporaryOverride o) ? Math.Max(0f, o.remainingTime) : 0f;
        }

        /// <summary>Durata totale dell'override (0 se non ce n'e').</summary>
        public float GetDuration(AttributeType type)
        {
            return activeOverrides.TryGetValue(type, out TemporaryOverride o) ? o.duration : 0f;
        }

        /// <summary>I 5 attributi con i valori attuali.</summary>
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
        /// Incolla un valore per 'duration' secondi. Lo stesso attributo SOSTITUISCE il valore e riavvia il timer
        /// (i valori non si sommano mai). Il valore e' limitato dai tetti del GDD.
        /// </summary>
        public void ApplyProperty(AttributeType type, float newValue, float duration)
        {
            float clamped = PropertyRules.Clamp(type, newValue);
            activeOverrides[type] = new TemporaryOverride(new PropertyValue(type, clamped), duration);
            OnPropertyValueChanged?.Invoke(type, clamped);
        }

        /// <summary>Toglie tutti gli override (reset della stanza) e avvisa chi ascolta.</summary>
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
