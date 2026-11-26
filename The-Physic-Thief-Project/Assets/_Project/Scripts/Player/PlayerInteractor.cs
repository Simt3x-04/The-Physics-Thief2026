using System;
using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Player
{
    /// <summary>
    /// The E key (GDD 3.7: pick apples, open doors, use switches). Every frame it looks along the camera for the nearest
    /// IInteractable within reach and publishes its prompt for the HUD; on E it calls Interact on it, once per press.
    /// Solid colliders without an IInteractable (walls, houses) block it; trigger zones without one do not.
    /// It reads E only from PlayerInputHandler.InteractPressed, the single input path of the player.
    /// Sits on the Main Camera next to PlayerScanner; it talks to the HUD only through events.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("References (auto-found if empty)")]
        [Tooltip("Where the interaction ray starts and looks (the camera).")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private PlayerController player;

        [Header("Reach")]
        [Tooltip("Maximum distance from the eyes, in meters.")]
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private LayerMask interactLayers = ~0;

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private HealthComponent playerHealth;
        private string shownPrompt;

        /// <summary>What E would act on right now (null if nothing).</summary>
        public IInteractable CurrentTarget { get; private set; }

        public float InteractRange => interactRange;

        /// <summary>The prompt currently published (null if none), for a HUD that subscribes late.</summary>
        public string CurrentPrompt => shownPrompt;

        /// <summary>Prompt to show under the crosshair; null means hide it.</summary>
        public event Action<string> OnPromptChanged;

        /// <summary>Short feedback after an interaction ("Ate an apple: +25 HP", "Health is already full").</summary>
        public event Action<string> OnMessage;

        /// <summary>True only while playing: not paused, loading, dead, in a menu or on the ending screen.</summary>
        public static bool IsGameplayActive =>
            Time.timeScale > 0f && (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);

        private void Awake()
        {
            if (rayOrigin == null) rayOrigin = transform;
            // The camera is a child of the Player root, which holds the input handler, controller and health.
            if (input == null) input = GetComponentInParent<PlayerInputHandler>();
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (player != null) playerHealth = player.GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (input != null) input.InteractPressed += HandleInteract;
            else Debug.LogWarning("[PlayerInteractor] No PlayerInputHandler found in the parents: E will do nothing.", this);
        }

        private void OnDisable()
        {
            if (input != null) input.InteractPressed -= HandleInteract;
            CurrentTarget = null;
            SetPrompt(null);
        }

        private void Update()
        {
            RefreshTarget();
        }

        private bool CanAct => IsGameplayActive && player != null && (playerHealth == null || !playerHealth.IsDead);

        private void RefreshTarget()
        {
            CurrentTarget = CanAct ? FindTarget(rayOrigin.position, rayOrigin.forward) : null;
            SetPrompt(CurrentTarget != null ? CurrentTarget.GetPrompt(player) : null);
        }

        private void HandleInteract()
        {
            if (!CanAct) return;

            // Look again now instead of trusting last frame's target: the apple may be gone or the player may have turned.
            IInteractable target = FindTarget(rayOrigin.position, rayOrigin.forward);
            if (target == null) return;

            string message = target.Interact(player);
            if (!string.IsNullOrEmpty(message)) OnMessage?.Invoke(message);
            RefreshTarget();
        }

        /// <summary>
        /// The nearest usable IInteractable along the ray within reach, or null. A solid collider in front (a wall)
        /// hides everything behind it; the player's own colliders are ignored.
        /// </summary>
        public IInteractable FindTarget(Vector3 origin, Vector3 direction)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, interactRange, interactLayers, QueryTriggerInteraction.Collide);

            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.distance >= nearestDistance) continue;
                if (player != null && hit.collider.transform.IsChildOf(player.transform)) continue;   // our own body

                IInteractable candidate = hit.collider.GetComponentInParent<IInteractable>();
                if (hit.collider.isTrigger && candidate == null) continue;   // zones never block

                nearestDistance = hit.distance;
                nearest = candidate;   // null means a solid surface without an interactable: it blocks
            }

            return nearest != null && nearest.CanInteract(player) ? nearest : null;
        }

        private void SetPrompt(string prompt)
        {
            if (prompt == shownPrompt) return;
            shownPrompt = prompt;
            OnPromptChanged?.Invoke(prompt);
        }
    }
}
