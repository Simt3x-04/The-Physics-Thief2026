using System.Collections.Generic;
using UnityEngine;

namespace PhysicsThief.Properties
{
    ///<summary>
    /// A ScriptableObject containing the "base" data for an object type (initial values ​​+ material).
    /// Designers create one asset per object type, rather than duplicating values ​​across scenes.
    /// </summary>
    [CreateAssetMenu(fileName = "PhysicalObjectDefinition", menuName = "Physics Thief/Physical Object Definition")]
    public class PhysicalObjectDefinitionSO : ScriptableObject
    {
        public string displayName;
        public MaterialTag material;
        public List<PropertyValue> baselineProperties = new List<PropertyValue>();

        /// <summary>TODO: search for the value of the requested type in baselineProperties (0 if absent).</summary>
        public float GetBaseValue(AttributeType type) { return default; }
    }
}
