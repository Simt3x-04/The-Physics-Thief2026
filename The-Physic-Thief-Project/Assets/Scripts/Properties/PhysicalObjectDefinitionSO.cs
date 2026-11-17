using System.Collections.Generic;
using UnityEngine;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// ScriptableObject con i dati "di base" di un tipo di oggetto (valori iniziali + materiale).
    /// Un asset per tipo di oggetto (es. "Boulder", "Paper") invece di duplicare i valori nelle scene.
    /// </summary>
    [CreateAssetMenu(fileName = "PhysicalObjectDefinition", menuName = "Physics Thief/Physical Object Definition")]
    public class PhysicalObjectDefinitionSO : ScriptableObject
    {
        public string displayName;
        public MaterialTag material;
        public List<PropertyValue> baselineProperties = new List<PropertyValue>();

        /// <summary>Valore base dell'attributo; se non e' elencato si usa il default (PropertyRules).</summary>
        public float GetBaseValue(AttributeType type)
        {
            for (int i = 0; i < baselineProperties.Count; i++)
            {
                if (baselineProperties[i].type == type) return baselineProperties[i].value;
            }
            return PropertyRules.DefaultValue(type);
        }
    }
}
