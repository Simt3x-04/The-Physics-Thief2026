using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.UI;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Scene-local wiring component for HUDtest.unity.
    ///
    /// Place this on a root GameObject called "UIIntegration" in the scene.
    /// It connects:
    ///   • The player's HealthComponent.OnDied → GameManager.ReportPlayerDeath()
    ///   • GameManager.OnRetryRequested        → respawn at checkpoint / restore HP
    ///   • DeathScreenController.Bind()        → the same HealthComponent
    ///   • HUDManager.Bind()                   → HealthComponent + BriefcaseInventory
    ///   • PauseMenuController (via GameManager state events, nothing extra needed)
    ///
    /// The scene must have:
    ///   • A GameManager root GameObject (persistent singleton)
    ///   • A DeathScreenCanvas with DeathScreenController
    ///   • A PauseMenuCanvas with PauseMenuController
    ///   • The existing HUDTest GO with HealthComponent + BriefcaseInventory
    ///   • The existing HUD GO with HUDManager
    ///
    /// CHECKPOINT LOGIC
    /// ────────────────
    /// Because LevelManager and Checkpoint are still stubs, this component tracks
    /// the checkpoint position internally.  Call SetCheckpoint(transform) from a
    /// Checkpoint trigger (or use the serialised initialSpawnPoint as the default).
    /// On retry: health is fully restored, player is teleported to the last checkpoint.
    ///
    /// INTEGRATION CONTRACT FOR DAVIDE (PlayerController owner):
    ///   When PlayerController.Respawn(Vector3) is implemented, it will be called
    ///   automatically — no code changes needed here.  The player GO is located by
    ///   tag "Player" or via the serialised playerTransform field.
    /// </summary>
    [DisallowMultipleComponent]
    public class HUDTestIntegration : MonoBehaviour
    {
        [Header("Player references (auto-found if null)")]
        [Tooltip("The HealthComponent on the player / HUDTest GO.")]
        [SerializeField] private HealthComponent playerHealth;

        [Tooltip("The player's Transform for respawn teleport.")]
        [SerializeField] private Transform playerTransform;

        [Header("UI Screens (auto-found by type if null)")]
        [SerializeField] private DeathScreenController deathScreen;
        [SerializeField] private HUDManager hudManager;

        [Header("Checkpoint")]
        [Tooltip("Transform the player spawns at before any checkpoint is activated.")]
        [SerializeField] private Transform initialSpawnPoint;

        // ── Runtime state ────────────────────────────────────────────────────
        private Transform _currentCheckpoint;   // null = use initialSpawnPoint

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            ResolveReferences();
            WireEvents();
            ApplyInitialState();
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        // ── Reference resolution ─────────────────────────────────────────────
        private void ResolveReferences()
        {
            // HealthComponent
            if (playerHealth == null)
            {
                // Try the HUDTest GO first, then search the scene.
                var hudTestGO = GameObject.Find("HUDTest");
                if (hudTestGO != null)
                    playerHealth = hudTestGO.GetComponent<HealthComponent>();
                if (playerHealth == null)
                    playerHealth = FindFirstObjectByType<HealthComponent>();
            }

            // Player Transform (for respawn)
            if (playerTransform == null && playerHealth != null)
                playerTransform = playerHealth.transform;

            // DeathScreenController
            if (deathScreen == null)
                deathScreen = FindFirstObjectByType<DeathScreenController>(FindObjectsInactive.Include);

            // HUDManager
            if (hudManager == null)
                hudManager = FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);

            // Initial spawn point default: player's current position
            if (initialSpawnPoint == null && playerTransform != null)
            {
                var spawnGO = new GameObject("InitialSpawnPoint");
                spawnGO.transform.position = playerTransform.position;
                spawnGO.transform.SetParent(transform, true);
                initialSpawnPoint = spawnGO.transform;
            }

            // Warn about missing critical references
            if (playerHealth == null)
                Debug.LogError("[HUDTestIntegration] playerHealth not found. " +
                               "Assign it in the Inspector or add a HealthComponent to the HUDTest GO.");
            if (deathScreen == null)
                Debug.LogError("[HUDTestIntegration] DeathScreenController not found. " +
                               "Add a DeathScreenCanvas to HUDtest.unity.");
            if (GameManager.Instance == null)
                Debug.LogError("[HUDTestIntegration] GameManager.Instance is null. " +
                               "Add a GameManager root GO to HUDtest.unity.");
        }

        private void WireEvents()
        {
            // Player death → GameManager state
            if (playerHealth != null)
            {
                playerHealth.OnDied -= OnPlayerDied;   // guard against double-subscribe
                playerHealth.OnDied += OnPlayerDied;
            }

            // GameManager retry → respawn
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRetryRequested -= HandleRetry;
                GameManager.Instance.OnRetryRequested += HandleRetry;
            }

            // Bind Death screen to this player's health
            if (deathScreen != null && playerHealth != null)
                deathScreen.Bind(playerHealth);

            // Bind HUD
            if (hudManager != null && playerHealth != null)
            {
                var inv = playerHealth.GetComponent<
                    PhysicsThief.Player.BriefcaseInventory>();
                hudManager.Bind(playerHealth, inv, null);
            }
        }

        private void UnwireEvents()
        {
            if (playerHealth != null) playerHealth.OnDied -= OnPlayerDied;
            if (GameManager.Instance != null) GameManager.Instance.OnRetryRequested -= HandleRetry;
        }

        private void ApplyInitialState()
        {
            // HUDtest starts in Playing state so the HUD is immediately visible.
            if (GameManager.Instance != null &&
                GameManager.Instance.State != GameState.Playing)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }

        // ── Event handlers ───────────────────────────────────────────────────
        private void OnPlayerDied()
        {
            // Route through GameManager — this sets GameOver state (freezes time, 
            // shows cursor) and fires OnGameStateChanged so DeathScreenController
            // receives the state change and fades in.
            GameManager.Instance?.ReportPlayerDeath();

            Debug.Log("[HUDTestIntegration] Player died → GameManager.ReportPlayerDeath()");
        }

        private void HandleRetry()
        {
            // Restore health
            playerHealth?.ResetHealth();

            // Teleport to last checkpoint (or initial spawn)
            var target = _currentCheckpoint != null ? _currentCheckpoint : initialSpawnPoint;
            if (target != null && playerTransform != null)
            {
                playerTransform.position = target.position;
                playerTransform.rotation = target.rotation;
                Debug.Log($"[HUDTestIntegration] Respawned at '{target.name}' ({target.position})");
            }
            else
            {
                Debug.LogWarning("[HUDTestIntegration] No spawn point found — player position unchanged.");
            }

            // Update HUD immediately
            if (hudManager != null && playerHealth != null)
                hudManager.UpdateHealthBar(playerHealth.CurrentHP, playerHealth.MaxHP);

            Debug.Log("[HUDTestIntegration] Retry complete — health restored, player teleported.");
        }

        // ── Public API ───────────────────────────────────────────────────────
        /// <summary>
        /// Call from a Checkpoint trigger (or any game logic) to register a new respawn point.
        /// </summary>
        public void SetCheckpoint(Transform checkpoint)
        {
            _currentCheckpoint = checkpoint;
            Debug.Log($"[HUDTestIntegration] Checkpoint set: {checkpoint.name}");
        }
    }
}
