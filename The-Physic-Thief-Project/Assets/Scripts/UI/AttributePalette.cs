using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>One color per attribute, shared by the HUD slots, the scanner panel and the timer bars above objects.</summary>
    public static class AttributePalette
    {
        // Indexed by (int)AttributeType: Mass, Velocity, Temperature, Magnetism, Hardness.
        private static readonly Color[] Colors =
        {
            new Color(0.96f, 0.62f, 0.04f),   // Mass: amber
            new Color(0.13f, 0.83f, 0.93f),   // Velocity: cyan
            new Color(0.94f, 0.27f, 0.27f),   // Temperature: red
            new Color(0.65f, 0.55f, 0.98f),   // Magnetism: violet
            new Color(0.58f, 0.64f, 0.72f)    // Hardness: steel
        };

        public static Color Get(AttributeType type)
        {
            return Colors[(int)type];
        }
    }
}
