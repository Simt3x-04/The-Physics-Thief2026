using System;
using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Player
{
    /// <summary>
    /// Briefcase scanner (GDD 3.1). Hold right click to scan; while scanning, keys 1-5 copy the attribute of the
    /// aimed object into the first free slot; without scanning, keys 1-5 select slots; left click pastes.
    /// When the selected slots include Velocity, pasting becomes "hold and release": hold the left click on the
    /// object, aim at where it should go, release to launch it there. It talks to the HUD only through events.
    /// </summary>
    public class PlayerScanner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private BriefcaseInventory inventory;
        [SerializeField] private PlayerInputHandler input;

        [Header("Scanning")]
        [SerializeField] private float rayDistance = 15f;
        [SerializeField] private LayerMask scanLayer = ~0;

        [Header("Velocity aiming")]
        [Tooltip("How far the aim ray looks for a target. If nothing is hit, the object is launched along the view direction.")]
        [SerializeField] private float aimMaxDistance = 100f;
        [SerializeField] private LayerMask aimLayers = ~0;
        [Tooltip("Aiming at a surface at least this flat (0 = wall, 1 = perfect floor) launches the object horizontally instead of into the ground.")]
        [SerializeField, Range(0f, 1f)] private float floorNormalThreshold = 0.7f;

        private PhysicalObject aimTarget;

        public PhysicalObject CurrentTarget { get; private set; }
        public bool IsAiming => aimTarget != null;

        /// <summary>Scanned object (null when right click is released or nothing is aimed at).</summary>
        public event Action<PhysicalObject> OnTargetChanged;

        /// <summary>Short messages for invalid actions ("No free slot...").</summary>
        public event Action<string> OnMessage;

        /// <summary>While aiming a velocity: object position and the point it will be launched towards.</summary>
        public event Action<Vector3, Vector3> OnAimUpdated;
        public event Action OnAimEnded;

        private void Awake()
        {
            if (rayOrigin == null) rayOrigin = transform;
            // The input handler can sit on this object or on any parent (e.g. the Player root).
            if (input == null) input = GetComponentInParent<PlayerInputHandler>();
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
            if (IsAiming) EndAim();
        }

        private void Update()
        {
            PhysicalObject found = input.ScanHeld ? ScanTarget() : null;
            if (found != CurrentTarget)
            {
                CurrentTarget = found;
                OnTargetChanged?.Invoke(found);
            }

            if (IsAiming) UpdateAiming();
        }

        // ---------- scan / copy ----------

        /// <summary>Raycast from the aim point: the PhysicalObject hit (null if none, or if a wall is in the way).</summary>
        public PhysicalObject ScanTarget()
        {
            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, rayDistance, scanLayer, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<PhysicalObject>();
            }
            return null;
        }

        /// <summary>Copies the attribute of the scanned object into the first free slot. True if it worked.</summary>
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

        // ---------- paste ----------

        /// <summary>
        /// Left click on the aimed object. Pastes at once, unless the selection contains Velocity:
        /// then it starts aiming and the paste happens when the button is released.
        /// </summary>
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

            if (SelectionContainsVelocity())
            {
                aimTarget = target;
                OnMessage?.Invoke("Aim where it should go, then release to launch");
                return true;
            }
            return ApplySelected(target, Vector3.zero);
        }

        /// <summary>Consumes the selected slots (or the active one) and applies them to the target.</summary>
        private bool ApplySelected(PhysicalObject target, Vector3 launchDirection)
        {
            var taken = inventory.Model.ConsumeSelected();
            if (taken.Count == 0)
            {
                OnMessage?.Invoke("Nothing to paste - copy an attribute first");
                return false;
            }

            foreach (PropertyValue property in taken)
            {
                target.ApplyProperty(property.type, property.value, inventory.Model.PropertyDuration, launchDirection);
            }
            return true;
        }

        /// <summary>True if the slots that the next paste would use hold a Velocity.</summary>
        private bool SelectionContainsVelocity()
        {
            BriefcaseModel model = inventory.Model;

            bool anySelected = false;
            for (int i = 0; i < model.UnlockedSlots; i++)
            {
                if (model.IsSelected(i)) anySelected = true;
            }

            for (int i = 0; i < model.UnlockedSlots; i++)
            {
                bool used = anySelected ? model.IsSelected(i) : i == model.ActiveSlotIndex;
                if (!used) continue;

                PropertyValue? slot = model.GetSlot(i);
                if (slot.HasValue && slot.Value.type == AttributeType.Velocity) return true;
            }
            return false;
        }

        // ---------- velocity aiming (hold and release) ----------

        private void UpdateAiming()
        {
            if (aimTarget == null)   // the target was destroyed while aiming
            {
                EndAim();
                return;
            }

            Vector3 objectPosition = aimTarget.transform.position;
            Vector3 aimPoint = ComputeAimPoint(aimTarget, objectPosition);
            OnAimUpdated?.Invoke(objectPosition, aimPoint);

            if (!input.PasteHeld)   // button released: launch
            {
                Vector3 direction = aimPoint - objectPosition;
                if (direction.sqrMagnitude < 0.0001f) direction = rayOrigin.forward;

                PhysicalObject target = aimTarget;
                EndAim();
                ApplySelected(target, direction.normalized);
            }
        }

        /// <summary>
        /// Where the crosshair points: the first surface hit (ignoring the aimed object itself), or a far point
        /// along the view direction when nothing is hit (horizon, sky). Floors are flattened to the object's height
        /// so the object slides along the ground instead of digging into it.
        /// </summary>
        private Vector3 ComputeAimPoint(PhysicalObject ignored, Vector3 objectPosition)
        {
            Vector3 origin = rayOrigin.position;
            Vector3 forward = rayOrigin.forward;

            RaycastHit[] hits = Physics.RaycastAll(origin, forward, aimMaxDistance, aimLayers, QueryTriggerInteraction.Ignore);

            bool found = false;
            RaycastHit best = default;
            float bestDistance = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<PhysicalObject>() == ignored) continue;   // the object does not block its own aim
                if (hit.distance < bestDistance)
                {
                    best = hit;
                    bestDistance = hit.distance;
                    found = true;
                }
            }

            if (!found) return origin + forward * aimMaxDistance;

            Vector3 point = best.point;
            if (best.normal.y > floorNormalThreshold) point.y = objectPosition.y;
            return point;
        }

        private void EndAim()
        {
            aimTarget = null;
            OnAimEnded?.Invoke();
        }

        // ---------- input ----------

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
