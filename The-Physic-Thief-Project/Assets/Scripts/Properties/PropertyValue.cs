using System;
using System.Globalization;

namespace PhysicsThief.Properties
{
    /// <summary>Valore di una proprieta' fisica: cio' che si copia nella valigetta e si incolla su un oggetto.</summary>
    [Serializable]
    public struct PropertyValue
    {
        // Indicizzato da (int)AttributeType: Mass, Velocity, Temperature, Magnetism, Hardness.
        private static readonly string[] Units = { " kg", " m/s", " \u00B0C", " T", "" };

        public AttributeType type;
        public float value;

        public PropertyValue(AttributeType type, float value)
        {
            this.type = type;
            this.value = value;
        }

        public string GetFormattedValue()
        {
            return Format(type, value);
        }

        /// <summary>Es. "600 kg", "0.1 kg", "0 °C". Durezza senza unita' (scala 1-10).</summary>
        public static string Format(AttributeType type, float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + Units[(int)type];
        }
    }
}
