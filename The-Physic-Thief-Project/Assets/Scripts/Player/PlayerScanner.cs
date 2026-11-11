using System;
using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Player
{
    /// <summary>
    ///Briefcase scanner (GDD 3.1): hold right-click to scan; with the scanner open, keys 1–5 copy an attribute
    /// into the first available slot; without the scanner open, keys 1–5 select slots; left-click pastes the attribute onto the targeted object.
    /// Communicates with the HUD solely via events.
    /// </summary>
    public class PlayerScanner : MonoBehaviour
    {
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float rayDistance = 15f;
        [SerializeField] private LayerMask scanLayer = ~0;
        [SerializeField] private BriefcaseInventory inventory;
        [SerializeField] private PlayerInputHandler input;

        public PhysicalObject CurrentTarget { get; private set; }

        /// <summary>Scanned object (null when the right mouse button is released or when nothing is being targeted).</summary>
        public event Action<PhysicalObject> OnTargetChanged;

        /// <summary>Short messages for invalid actions ("No free slot...").</summary>
        public event Action<string> OnMessage;

        private void Awake()
        {
            if (rayOrigin == null) rayOrigin = transform;
            if (input == null) input = GetComponent<PlayerInputHandler>();
        }

        private void OnEnable()
        {
            input.NumberPressed += HandleNumber;
            input.PastePressed += HandlePaste;
            input.TrashPressed += HandleTrash;
            input.ScrollStepped += HandleScroll;
        }

        private void OnDisable()
        {
            input.NumberPressed -= HandleNumber;
            input.PastePressed -= HandlePaste;
            input.TrashPressed -= HandleTrash;
            input.ScrollStepped -= HandleScroll;
        }

        private void Update()
        {
            PhysicalObject found = input.ScanHeld ? ScanTarget() : null;
            if (found != CurrentTarget)
            {
                CurrentTarget = found;
                OnTargetChanged?.Invoke(found);
            }
        }

        /// <summary>Raycast from the aiming point: returns the PhysicalObject hit (null if none is hit or if a wall obstructs it).</summary>
        public PhysicalObject ScanTarget()
        {
            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, rayDistance, scanLayer, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<PhysicalObject>();
            }
            return null;
        }

        /// <summary>Copies the attribute of the scanned object into the first available slot. Returns true if successful.</summary>
        public bool CopyProperty(AttributeType type)
        {
            if (CurrentTarget == null)
            {
                OnMessage?.Invoke("Hold right click and aim at an object to copy");
                return false;
            }

            CopyResult result = inventory.Model.TryAdd(new PropertyValue(type, CurrentTarget.GetValue(type)));
            string message = result.ToMessage();
            if (message != null) OnMessage?.Invoke(message);
            return result == CopyResult.Success;
        }

        /// <summary>Pastes the selected slots (or the active one) onto the targeted object. Returns true if something was pasted.</summary>
        public bool PasteProperty()
        {
            PhysicalObject target = ScanTarget();
            if (target == null)
            {
                OnMessage?.Invoke("Aim at an object to paste");
                return false;
            }
            if (target.transform.root == transform.root)
            {
                OnMessage?.Invoke("Gino cannot paste on himself");
                return false;
            }

            var taken = inventory.Model.ConsumeSelected();
            if (taken.Count == 0)
            {
                OnMessage?.Invoke("Nothing to paste - copy an attribute first");
                return false;
            }

            foreach (PropertyValue property in taken)
            {
                target.ApplyProperty(property.type, property.value, inventory.Model.PropertyDuration);
            }
            return true;
        }

        //input

        private void HandleNumber(int index)
        {
            if (input.ScanHeld)
            {
                CopyProperty((AttributeType)index);
            }
            else
            {
                inventory.Model.SetActiveSlot(index);
                inventory.Model.ToggleSelected(index);
            }
        }

        private void HandlePaste() { PasteProperty(); }
        private void HandleTrash() { inventory.Model.TrashActive(); }
        private void HandleScroll(int step) { inventory.Model.CycleActiveSlot(step); }
    }
}