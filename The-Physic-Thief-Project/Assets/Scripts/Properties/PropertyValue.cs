using System;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Valore di una proprieta' fisica (es. Temperature = 0). E' il "dato" che si copia
    /// nella valigetta e si incolla su un altro oggetto.
    /// </summary>
    [Serializable]
    public struct PropertyValue
    {
        public AttributeType type;
        public float value;
        public bool isUnlocked;

        /// <summary>TODO: stringa per la UI con unita' di misura (es. "0 C", "1000 kg").</summary>
        public string GetFormattedValue() { return default; }
    }
}
