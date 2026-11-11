using System;

namespace PhysicsThief.Properties
{
    /// <summary>Limiti e valori di default degli attributi (GDD 3.2, 3.9 e 7). C# puro, testabile.</summary>
    public static class PropertyRules
    {
        // Indicizzati da (int)AttributeType: Mass, Velocity, Temperature, Magnetism, Hardness.
        private static readonly float[] Min = { 0.1f, 0f, 0f, 0f, 1f };
        private static readonly float[] Max = { 1000f, 50f, 500f, 2f, 10f };
        private static readonly float[] Default = { 1f, 0f, 20f, 0f, 1f };   // temperatura ambiente 20 C

        /// <summary>Tetto ai valori INCOLLATI (es. massa max 1000 kg, velocita' max 50 m/s).</summary>
        public static float Clamp(AttributeType type, float value)
        {
            int i = (int)type;
            return Math.Max(Min[i], Math.Min(Max[i], value));
        }

        /// <summary>Valore usato quando un oggetto non specifica un attributo.</summary>
        public static float DefaultValue(AttributeType type)
        {
            return Default[(int)type];
        }
    }
}
