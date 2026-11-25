using System;
using System.Globalization;

namespace PhysicsThief.Properties
{
    /// <summary>Value of a physical property: that which is copied into the clipboard and pasted onto an object.</summary>
    [Serializable]
    public struct PropertyValue
    {
        // Index by (int)AttributeType: Mass, Velocity, Temperature, Magnetism, Hardness.
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

        /// <summary>E.g., "600 kg", "0.1 kg", "0 °C". Hardness without units (scale 1–10).</summary>
        public static string Format(AttributeType type, float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + Units[(int)type];
        }
    }
}
