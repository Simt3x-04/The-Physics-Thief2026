using System;

namespace PhysicsThief.Properties
{
    /// <summary>Limits and default values of the attributes (GDD 3.2, 3.9 and 7). Pure C#, testable.</summary>
    public static class PropertyRules
    {
        // Indexed by (int)AttributeType: Mass, Velocity, Temperature, Magnetism, Hardness.
        private static readonly float[] Min = { 0.1f, 0f, 0f, 0f, 1f };
        private static readonly float[] Max = { 1000f, 50f, 500f, 2f, 10f };
        private static readonly float[] Default = { 1f, 0f, 20f, 0f, 1f };   // room temperature 20 C

        /// <summary>Cap on PASTED values (e.g. max mass 1000 kg, max velocity 50 m/s).</summary>
        public static float Clamp(AttributeType type, float value)
        {
            int i = (int)type;
            return Math.Max(Min[i], Math.Min(Max[i], value));
        }

        /// <summary>Value used when an object does not specify an attribute.</summary>
        public static float DefaultValue(AttributeType type)
        {
            return Default[(int)type];
        }
    }
}
