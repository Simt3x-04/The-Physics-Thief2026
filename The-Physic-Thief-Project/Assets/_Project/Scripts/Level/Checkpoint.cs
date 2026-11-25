using UnityEngine;

namespace PhysicsThief.Level
{
    /// <summary>
    /// Trigger that records the respawn point in the LevelManager when the player enters.
    ///
    /// SETUP:
    ///   1. Create a GameObject with a Collider set to IsTrigger = true.
    ///   2. Add this Checkpoint component.
    ///   3. Optionally assign a separate respawnPoint transform for the actual spawn position
    ///      (e.g., offset from the trigger to avoid spawning inside geometry).
    ///      If respawnPoint is null, the checkpoint's own transform is used.
    ///   4. LevelManager auto-finds in the scene; you can also assign it explicitly.
    ///
    /// BEHAVIOR:
    ///   - When the player enters, calls LevelManager.SetCheckpoint(respawnPoint).
    ///   - Re-entering the same checkpoint is safe (no side effects).
    ///   - Activating a later checkpoint replaces the previous one (latest wins).
    ///   - Visual/audio feedback can be added by subscribing to OnCheckpointActivated.
    ///
    /// NOTE: Detects the player by polling in FixedUpdate (written for the previous character
    /// controller, which did not fire trigger callbacks). OnTriggerEnter is kept as well.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("The transform used for respawning. If null, uses this object's transform.")]
        [SerializeField] private Transform respawnPoint;

        [Tooltip("The LevelManager to notify. Auto-found if null.")]
        [SerializeField] private LevelManager levelManager;

        [Header("Visual Feedback (Optional)")]
        [Tooltip("Renderer to tint when activated.")]
        [SerializeField] private Renderer checkpointRenderer;

        [Tooltip("Color to apply when activated.")]
        [SerializeField] private Color activatedColor = new Color(0.2f, 1f, 0.4f, 1f);

        // ─────────────────────────────────────────────────────────────────────
        private bool _isActivated;
        private Color _originalColor;
        private Collider _collider;
        private bool _playerWasInside;
        private static Collider[] _overlapResults = new Collider[8];

        /// <summary>Returns the spawn position for this checkpoint.</summary>
        public Transform RespawnPoint => respawnPoint != null ? respawnPoint : transform;

        /// <summary>Returns true if this checkpoint has been activated this session.</summary>
        public bool IsActivated => _isActivated;

        /// <summary>Fired when this checkpoint is activated for the first time.</summary>
        public event System.Action OnCheckpointActivated;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Auto-find LevelManager if not assigned
            if (levelManager == null)
                levelManager = FindFirstObjectByType<LevelManager>();

            // Store original color for visual feedback
            if (checkpointRenderer != null)
                _originalColor = checkpointRenderer.material.color;

            // Get and configure collider
            _collider = GetComponent<Collider>();
            if (_collider != null && !_collider.isTrigger)
            {
                Debug.LogWarning($"[Checkpoint] '{name}' collider is not set to IsTrigger. Setting it now.");
                _collider.isTrigger = true;
            }
        }

        private void FixedUpdate()
        {
            // Poll for player overlap
            if (_collider == null) return;
            
            bool playerInside = IsPlayerOverlapping();
            
            // Detect enter (wasn't inside, now is inside)
            if (playerInside && !_playerWasInside)
            {
                Activate();
            }
            
            _playerWasInside = playerInside;
        }

        private bool IsPlayerOverlapping()
        {
            // Use the appropriate overlap check based on collider type
            int count = 0;
            
            if (_collider is BoxCollider box)
            {
                Vector3 center = transform.TransformPoint(box.center);
                Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
                count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlapResults, 
                    transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            }
            else if (_collider is SphereCollider sphere)
            {
                Vector3 center = transform.TransformPoint(sphere.center);
                float radius = sphere.radius * Mathf.Max(transform.lossyScale.x, 
                    transform.lossyScale.y, transform.lossyScale.z);
                count = Physics.OverlapSphereNonAlloc(center, radius, _overlapResults, 
                    ~0, QueryTriggerInteraction.Ignore);
            }
            else if (_collider is CapsuleCollider capsule)
            {
                // Capsule overlap is more complex, fall back to bounds check
                Bounds bounds = _collider.bounds;
                count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, _overlapResults, 
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            }
            else
            {
                // Generic fallback using bounds
                Bounds bounds = _collider.bounds;
                count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, _overlapResults, 
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            }

            // Check if any of the overlapping colliders is the player
            for (int i = 0; i < count; i++)
            {
                if (IsPlayer(_overlapResults[i]))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Standard trigger callback, in addition to the FixedUpdate polling (Activate is safe to call twice).
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Only activate for the player
            if (!IsPlayer(other)) return;

            Activate();
        }

        /// <summary>
        /// Activates this checkpoint, recording it as the respawn point.
        /// Safe to call multiple times (idempotent for visual feedback, but always updates LevelManager).
        /// </summary>
        public void Activate()
        {
            if (levelManager == null)
            {
                Debug.LogError($"[Checkpoint] '{name}': LevelManager not found. Cannot activate checkpoint.");
                return;
            }

            // Always update LevelManager (latest wins)
            levelManager.SetCheckpoint(RespawnPoint);

            // Visual feedback only on first activation
            if (!_isActivated)
            {
                _isActivated = true;

                if (checkpointRenderer != null)
                    checkpointRenderer.material.color = activatedColor;

                OnCheckpointActivated?.Invoke();
                Debug.Log($"[Checkpoint] '{name}' activated.");
            }
        }

        /// <summary>
        /// Resets the checkpoint's activated state (for fresh runs).
        /// Does NOT clear the LevelManager's checkpoint—use LevelManager.ClearCheckpoint() for that.
        /// </summary>
        public void ResetActivation()
        {
            _isActivated = false;
            _playerWasInside = false;
            if (checkpointRenderer != null)
                checkpointRenderer.material.color = _originalColor;
        }

        // ─────────────────────────────────────────────────────────────────────
        private bool IsPlayer(Collider col)
        {
            // Check by tag first (most reliable)
            if (col.CompareTag("Player")) return true;

            // Fallback: check by name or component
            if (col.name == "Player") return true;
            if (col.GetComponent<PhysicsThief.Player.PlayerController>() != null) return true;
            if (col.GetComponentInParent<PhysicsThief.Player.PlayerController>() != null) return true;

            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            // Draw spawn position in Scene view
            Transform spawn = respawnPoint != null ? respawnPoint : transform;
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(spawn.position, 0.5f);
            Gizmos.DrawLine(spawn.position, spawn.position + spawn.forward * 1.5f);
        }
    }
}
