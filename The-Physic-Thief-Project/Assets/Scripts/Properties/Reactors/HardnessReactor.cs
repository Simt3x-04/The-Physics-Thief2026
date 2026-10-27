using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>Durezza: sotto soglia l'oggetto diventa fragile, sopra e' a prova di proiettile.</summary>
    public class HardnessReactor : ReactorBase
    {
        [SerializeField] private float fragileBelow = 2f;
        [SerializeField] private float bulletproofAbove = 8f;

        public bool IsFragile { get; private set; }
        public bool IsBulletproof { get; private set; }

        protected override void HandlePropertyChange(AttributeType type, float newValue) { }

        private void SetFragileState() { }
        private void SetBulletproofState() { }
    }
}
