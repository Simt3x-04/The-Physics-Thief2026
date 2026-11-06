using System;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Proprieta' incollata su un oggetto, con un timer indipendente (GDD 3.3).
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

        /// <summary>Scala il tempo residuo. Ritorna true se il timer e' scaduto.</summary>
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
