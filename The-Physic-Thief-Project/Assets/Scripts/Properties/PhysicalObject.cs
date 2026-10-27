using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Cuore del gioco: qualsiasi oggetto/nemico con proprieta' fisiche modificabili.
    /// Mantiene valori base + override temporanei e AVVISA i Reactor quando un valore cambia
    /// (Observer). Non contiene la logica delle reazioni: quella sta nei ReactorBase.
    /// </summary>
    [DisallowMultipleComponent]
    public class PhysicalObject : MonoBehaviour, IResettable
    {
        [SerializeField] private PhysicalObjectDefinitionSO definition;

        private readonly Dictionary<AttributeType, float> currentValues = new Dictionary<AttributeType, float>();
        private readonly Dictionary<AttributeType, TemporaryOverride> activeOverrides = new Dictionary<AttributeType, TemporaryOverride>();

        public PhysicalObjectDefinitionSO Definition => definition;

        /// <summary>Argomenti: proprieta' cambiata, nuovo valore.</summary>
        public event Action<AttributeType, float> OnPropertyValueChanged;

        /// <summary>Scatta quando un override scade (la UI mostra l'avviso, l'audio fa "pop").</summary>
        public event Action<AttributeType> OnOverrideExpired;

        /// <summary>TODO: popola currentValues dai valori base della definition.</summary>
        private void Awake() { }

        /// <summary>TODO: fai Tick di ogni TemporaryOverride; se scaduto chiama RevertToBaseline.</summary>
        private void Update() { }

        /// <summary>TODO: valore attuale (override se presente, altrimenti base).</summary>
        public float GetValue(AttributeType type) { return default; }

        /// <summary>TODO: valore base dalla definition.</summary>
        public float GetBaseValue(AttributeType type) { return default; }

        public MaterialTag GetMaterial() { return default; }

        public bool HasOverride(AttributeType type) { return default; }

        /// <summary>TODO: elenco delle proprieta' mostrate/copiabili dallo scanner.</summary>
        public IReadOnlyList<PropertyValue> GetScannableProperties() { return default; }

        /// <summary>TODO: crea/sostituisce il TemporaryOverride, aggiorna currentValues, invoca OnPropertyValueChanged.</summary>
        public void ApplyProperty(AttributeType type, float newValue, float duration) { }

        /// <summary>TODO: rimuove tutti gli override e ripristina i valori base (ResetPuzzleRoom).</summary>
        public void ResetState() { }

        /// <summary>TODO: rimuove l'override, ripristina il base, invoca OnPropertyValueChanged e OnOverrideExpired.</summary>
        private void RevertToBaseline(AttributeType type) { }
    }
}
