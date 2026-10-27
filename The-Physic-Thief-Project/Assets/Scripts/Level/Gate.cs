using UnityEngine;

namespace PhysicsThief.Level
{
    /// <summary>Cancello/porta sbloccabile risolvendo un puzzle.</summary>
    public class Gate : MonoBehaviour
    {
        [SerializeField] private int gateID;
        private bool isOpen;

        public int GateID => gateID;
        public bool IsOpen => isOpen;

        public void Open() { }
        public void Close() { }
    }
}
