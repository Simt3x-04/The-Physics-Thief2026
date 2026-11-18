using UnityEngine;

namespace PhysicsThief.Level
{
    /// <summary>
    /// A gate that slides to an open position (default: up) and back. It can be driven by a mechanism
    /// (SetOpen, e.g. a PressurePlate) or unlocked by the LevelManager (Open).
    /// </summary>
    public class Gate : MonoBehaviour
    {
        [SerializeField] private int gateID;
        [Tooltip("Where the gate goes when open, relative to its closed position (default: up by 3.2 m).")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 3.2f, 0f);
        [SerializeField] private float moveSpeed = 4f;

        private Vector3 closedPosition;
        private bool isOpen;

        public int GateID => gateID;
        public bool IsOpen => isOpen;

        private void Awake()
        {
            closedPosition = transform.position;
        }

        private void Update()
        {
            Vector3 target = isOpen ? closedPosition + openOffset : closedPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        }

        public void Open() { SetOpen(true); }
        public void Close() { SetOpen(false); }

        public void SetOpen(bool open)
        {
            isOpen = open;
        }
    }
}
