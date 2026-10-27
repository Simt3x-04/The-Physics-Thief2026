using System.Collections.Generic;
using UnityEngine;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// ScriptableObject con i dati "di base" di un tipo di oggetto (valori iniziali + materiale).
    /// I designer creano un asset per tipo di oggetto invece di duplicare i valori nelle scene.
    /// </summary>
    [CreateAssetMenu(fileName = "PhysicalObjectDefinition", menuName = "Physics Thief/Physical Object Definition")]
    public class PhysicalObjectDefinitionSO : ScriptableObject
    {
        public string displayName;
        public MaterialTag material;
        public List<PropertyValue> baselineProperties = new List<PropertyValue>();

        /// <summary>TODO: cerca in baselineProperties il valore del tipo richiesto (0 se assente).</summary>
        public float GetBaseValue(AttributeType type) { return default; }
    }
}
