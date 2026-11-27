using System.Collections.Generic;
using UnityEngine;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// ScriptableObject with the "base" data of an object type (initial values + material).
    /// One asset per object type (e.g. "Boulder", "Paper") instead of duplicating the values in the scenes.
    /// </summary>
    [CreateAssetMenu(fileName = "PhysicalObjectDefinition", menuName = "Physics Thief/Physical Object Definition")]
    public class PhysicalObjectDefinitionSO : ScriptableObject
    {
        public string displayName;
        public MaterialTag material;
        public List<PropertyValue> baselineProperties = new List<PropertyValue>();

        /// <summary>Base value of the attribute; if it is not listed, the default is used (PropertyRules).</summary>
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
