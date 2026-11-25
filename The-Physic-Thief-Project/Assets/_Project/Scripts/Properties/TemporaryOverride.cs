using System;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// A property pasted onto an object, with its own independent timer (GDD 3.3).
    /// </summary>
    [Serializable]
    public class TemporaryOverride
    {
        public PropertyValue property;
        public float duration;
        public float remainingTime;

        public bool HasExpired => remainingTime <= 0f;

        public TemporaryOverride(PropertyValue property, float duration)
        {
            this.property = property;
            this.duration = duration;
            remainingTime = duration;
        }

        /// <summary>Reduces the remaining time. Returns true if the timer has expired.</summary>
        public bool Tick(float deltaTime)
        {
            if (!HasExpired)
            {
                remainingTime -= deltaTime;
            }
            return HasExpired;
        }
    }
}
