using System;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Proprieta' incollata su un oggetto con un timer indipendente (default 5 s).
    /// Unisce TemporaryOverride (UML 2) e AppliedPropertyInstance (v2).
    /// </summary>
    [Serializable]
    public class TemporaryOverride
    {
        public PropertyValue property;
        public float duration;
        public float remainingTime;

        public bool HasExpired => remainingTime <= 0f;

        /// <summary>TODO: inizializza property, duration e remainingTime = duration.</summary>
        public TemporaryOverride(PropertyValue property, float duration) { }

        /// <summary>TODO: scala remainingTime di deltaTime. Ritorna true se e' appena scaduto.</summary>
        public bool Tick(float deltaTime) { return default; }
    }
}
