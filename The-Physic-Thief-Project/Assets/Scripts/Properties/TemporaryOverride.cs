using System;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Property attached to an object with an independent timer (default 5 s).
    /// </summary>
    [Serializable]
    public class TemporaryOverride
    {
        public PropertyValue property;
        public float duration;
        public float remainingTime;

        public bool HasExpired => remainingTime <= 0f;

        /// <summary>TODO: initialize property, duration, and remainingTime = duration.</summary>
        public TemporaryOverride(PropertyValue property, float duration) { }

        /// <summary>TODO: scale remainingTime by deltaTime. Returns true if it has just expired.</summary>
        public bool Tick(float deltaTime) { return default; }
    }
}
