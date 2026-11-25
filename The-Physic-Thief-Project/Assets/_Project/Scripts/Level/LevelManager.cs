using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Player;

namespace PhysicsThief.Level
{
    /// <summary>
    /// Manages a single level scene: spawn point, checkpoints, puzzle rooms, and gates.
    /// The overall flow (pause, death, menus) is handled by GameManager; this class owns respawn.
    ///
    /// SETUP:
    ///   1. Add a LevelManager root GameObject to each gameplay scene.
    ///   2. Assign the Player's PlayerController and HealthComponent in the Inspector,
    ///      OR let the component auto-find them on Awake.
    ///   3. Assign a defaultSpawnPoint (typically named "SpawnPoint") as the initial
    ///      respawn location before any checkpoint is activated.
    ///   4. Place Checkpoint triggers in the scene; each Checkpoint calls SetCheckpoint()
    ///      when the player enters.
    ///
    /// RESPAWN FLOW:
    ///   GameManager.OnRetryRequested → RespawnPlayer()
    ///     → Teleport player to currentCheckpoint (or defaultSpawnPoint)
    ///     → Reset health
    ///     → Clear stale velocity (handled inside PlayerController.Respawn)
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Player References (auto-found if null)")]
        [Tooltip("The player's controller, used to teleport on respawn.")]
        [SerializeField] private PlayerController player;

        [Tooltip("The player's health component to reset on respawn.")]
        [SerializeField] private HealthComponent playerHealth;

        [Header("Spawn Points")]
        [Tooltip("Initial spawn point used before any checkpoint is activated.")]
        [SerializeField] private Transform defaultSpawnPoint;

        [Tooltip("Currently active checkpoint (set by Checkpoint triggers). Falls back to defaultSpawnPoint if null.")]
        [SerializeField] private Transform currentCheckpoint;

        [Header("Level Elements")]
        [SerializeField] private List<Gate> gates = new List<Gate>();

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Returns the active checkpoint, or the default spawn if none is set.</summary>
        public Transform CurrentSpawnPoint => currentCheckpoint != null ? currentCheckpoint : defaultSpawnPoint;

        /// <summary>Returns true if any checkpoint has been activated.</summary>
        public bool HasActiveCheckpoint => currentCheckpoint != null;

        // ─────────────────────────────────────────────────────────────────────
        private bool _retryInProgress;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRetryRequested -= RespawnPlayer;
                GameManager.Instance.OnRetryRequested += RespawnPlayer;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRetryRequested -= RespawnPlayer;
        }

        private void ResolveReferences()
        {
            // Auto-find Player references if not assigned
            if (player == null)
            {
                var playerGO = GameObject.Find("Player");
                if (playerGO != null)
                {
                    player = playerGO.GetComponent<PlayerController>();
                    playerHealth = playerGO.GetComponent<HealthComponent>();
                }
            }
            if (player == null)
                player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (playerHealth == null && player != null)
                playerHealth = player.GetComponent<HealthComponent>();
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<HealthComponent>(FindObjectsInactive.Include);

            // Auto-find default spawn point if not assigned
            if (defaultSpawnPoint == null)
            {
                var spawnGO = GameObject.Find("SpawnPoint");
                if (spawnGO != null) defaultSpawnPoint = spawnGO.transform;
            }

            // Validation
            if (player == null)
                Debug.LogError("[LevelManager] PlayerController not found. Assign it in the Inspector or ensure a Player GO exists.");
            if (playerHealth == null)
                Debug.LogError("[LevelManager] HealthComponent not found. Assign it in the Inspector.");
            if (defaultSpawnPoint == null)
                Debug.LogWarning("[LevelManager] defaultSpawnPoint not assigned. Create a 'SpawnPoint' GameObject or assign one.");
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Called by Checkpoint triggers when the player activates a checkpoint.
        /// The new checkpoint replaces any previous one (latest wins).
        /// </summary>
        /// <param name="checkpoint">The respawn transform for this checkpoint.</param>
        public void SetCheckpoint(Transform checkpoint)
        {
            if (checkpoint == null)
            {
                Debug.LogWarning("[LevelManager] SetCheckpoint called with null transform.");
                return;
            }

            bool isNew = currentCheckpoint != checkpoint;
            currentCheckpoint = checkpoint;

            if (isNew)
                Debug.Log($"[LevelManager] Checkpoint activated: {checkpoint.name} at {checkpoint.position}");
        }

        /// <summary>
        /// Clears the active checkpoint. Called when starting a fresh run or leaving the level.
        /// </summary>
        public void ClearCheckpoint()
        {
            currentCheckpoint = null;
            Debug.Log("[LevelManager] Checkpoint cleared.");
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Respawns the player at the current checkpoint or default spawn point.
        /// Called by GameManager.OnRetryRequested after the death screen's Retry button.
        /// Guards against rapid double-clicks causing overlapping respawns.
        /// </summary>
        public void RespawnPlayer()
        {
            // Guard against overlapping retries
            if (_retryInProgress)
            {
                Debug.LogWarning("[LevelManager] RespawnPlayer called while retry already in progress.");
                return;
            }

            Transform spawnPoint = CurrentSpawnPoint;
            if (spawnPoint == null)
            {
                Debug.LogError("[LevelManager] No spawn point available! Cannot respawn. " +
                               "Assign a defaultSpawnPoint or place checkpoints in the scene.");
                return;
            }

            if (player == null)
            {
                Debug.LogError("[LevelManager] PlayerController is null. Cannot teleport player.");
                return;
            }

            _retryInProgress = true;

            // Teleport (clears velocity internally), then face the spawn point's direction (yaw only: stay upright)
            player.Respawn(spawnPoint.position);
            player.transform.rotation = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f);

            // Reset health (if HUDTestIntegration is also subscribed, that's fine—ResetHealth is idempotent)
            playerHealth?.ResetHealth();

            string source = currentCheckpoint != null ? $"checkpoint '{currentCheckpoint.name}'" : "default spawn";
            Debug.Log($"[LevelManager] Player respawned at {source}: {spawnPoint.position}");

            _retryInProgress = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Resets puzzle props in the specified room. (Stub for future implementation.)</summary>
        public void ResetPuzzleRoom(int roomID)
        {
            // TODO: Implement puzzle room reset when puzzle system is built.
        }

        /// <summary>Unlocks the gate with the specified ID.</summary>
        public void UnlockGate(int gateID)
        {
            foreach (var gate in gates)
            {
                if (gate != null && gate.GateID == gateID)
                {
                    gate.Open();
                    return;
                }
            }
        }
    }
}
