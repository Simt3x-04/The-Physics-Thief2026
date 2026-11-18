using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Player;
using PhysicsThief.UI;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Wires Start, Pause, Options, and Death into HUDtest.unity.
    ///
    /// Place on a root GameObject called "UIIntegration" in HUDtest.
    /// Run via  Tools → UI Flow → Wire HUDtest Scene  which creates this GO
    /// and fills every Inspector field automatically.
    ///
    /// WHAT IT DOES
    /// ─────────────
    ///   • Player.HealthComponent.OnDied → GameManager.ReportPlayerDeath()
    ///     (GameManager sets GameOver state, freezes time, shows cursor,
    ///      fires OnGameStateChanged which DeathScreenController listens to)
    ///
    ///   • GameManager.OnRetryRequested → restore HP + re-enable player input
    ///     (Retry button on death screen calls GameManager.RetryFromCheckpoint()
    ///      which sets Playing and fires OnRetryRequested)
    ///
    ///   • GameManager.OnGameStateChanged → PlayerMotor.SetInputEnabled()
    ///     (blocks movement while paused / dead / in a menu)
    ///
    ///   • DeathScreenController.Bind(playerHealth) so the screen listens
    ///     to the real player health component.
    ///
    ///   • HUDManager.Bind(health, inventory, scanner) if HUDManager exists.
    ///
    /// CHECKPOINT RESPAWN (LevelManager)
    /// ──────────────────────────────────
    ///   LevelManager subscribes to OnRetryRequested and handles:
    ///     • Teleporting the player to the active checkpoint (or default SpawnPoint)
    ///     • Resetting health (redundant with this class, but idempotent)
    ///   This class resets health for backward compatibility with scenes that
    ///   don't have a LevelManager.
    ///
    /// HOT-METAL DAMAGE (Davide's TemperatureReactor)
    /// ───────────────────────────────────────────────
    ///   The Metal GO in HUDtest already has TemperatureReactor.
    ///   To trigger contact damage, paste a temperature >= 70 °C onto Metal
    ///   using the in-game scanner (right-click scan → 1–5 to copy → left-click paste).
    ///   Temperature >= 70 °C → ThermalState.Hot → FixedUpdate overlap check → TakeDamage().
    ///
    ///   For fast testing without the scanner:
    ///     • Select Metal in Play Mode → Inspector → PhysicalObject → paste any value,
    ///       or use HudDebugInput keyboard shortcuts (see HudDebugInput.cs).
    ///
    /// INTEGRATION CONTRACT FOR DAVIDE
    /// ─────────────────────────────────
    ///   PlayerMotor.SetInputEnabled(bool) is already implemented — it disables
    ///   movement and look when false.  This component calls it on every state change.
    ///   No changes needed to PlayerMotor.
    /// </summary>
    [DisallowMultipleComponent]
    public class HUDTestIntegration : MonoBehaviour
    {
        [Header("Player — assign the Player GO's components")]
        [Tooltip("HealthComponent on the Player GameObject (not HUDTest).")]
        [SerializeField] private HealthComponent playerHealth;

        [Tooltip("PlayerMotor on the Player GameObject (for input enable/disable).")]
        [SerializeField] private PlayerMotor playerMotor;

        [Header("UI Screens (auto-found if null)")]
        [SerializeField] private DeathScreenController deathScreen;
        [SerializeField] private HUDManager hudManager;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _eventsWired;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            ResolveReferences();
            WireEvents();
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        // ── Reference resolution ──────────────────────────────────────────────
        private void ResolveReferences()
        {
            // HealthComponent — prefer the Player GO, not HUDTest
            if (playerHealth == null)
            {
                var playerGO = GameObject.Find("Player");
                if (playerGO != null) playerHealth = playerGO.GetComponent<HealthComponent>();
            }
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<HealthComponent>();

            // PlayerMotor
            if (playerMotor == null && playerHealth != null)
                playerMotor = playerHealth.GetComponent<PlayerMotor>();
            if (playerMotor == null)
                playerMotor = FindFirstObjectByType<PlayerMotor>(FindObjectsInactive.Include);

            // DeathScreenController
            if (deathScreen == null)
                deathScreen = FindFirstObjectByType<DeathScreenController>(FindObjectsInactive.Include);

            // HUDManager (optional — scene may not have one yet)
            if (hudManager == null)
                hudManager = FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);

            // Validation
            if (playerHealth == null)
                Debug.LogError("[HUDTestIntegration] playerHealth not found. " +
                               "Assign the Player GO's HealthComponent in the Inspector.");
            if (GameManager.Instance == null)
                Debug.LogError("[HUDTestIntegration] GameManager.Instance is null. " +
                               "Add a GameManager root GO (Tools → UI Flow → Wire HUDtest Scene).");
            if (deathScreen == null)
                Debug.LogError("[HUDTestIntegration] DeathScreenController not found. " +
                               "Run Tools → UI Flow → Wire HUDtest Scene to add it.");
        }

        private void WireEvents()
        {
            if (_eventsWired) return;

            // Player death → game state
            if (playerHealth != null)
            {
                playerHealth.OnDied -= OnPlayerDied;
                playerHealth.OnDied += OnPlayerDied;
            }

            // GameManager retry → restore HP + re-enable input
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRetryRequested    -= HandleRetry;
                GameManager.Instance.OnRetryRequested    += HandleRetry;
                GameManager.Instance.OnGameStateChanged  -= HandleStateChanged;
                GameManager.Instance.OnGameStateChanged  += HandleStateChanged;
            }

            // Bind Death screen to player health
            if (deathScreen != null && playerHealth != null)
                deathScreen.Bind(playerHealth);

            // Bind HUD (optional)
            if (hudManager != null && playerHealth != null)
            {
                var inv     = playerHealth.GetComponent<
                                  PhysicsThief.Player.BriefcaseInventory>();
                var scanner = playerHealth.GetComponent<
                                  PhysicsThief.Player.PlayerScanner>();
                hudManager.Bind(playerHealth, inv, scanner);
            }

            // Start in Playing state so player can move immediately
            if (GameManager.Instance != null &&
                GameManager.Instance.State != GameState.Playing)
                GameManager.Instance.SetState(GameState.Playing);

            _eventsWired = true;
            Debug.Log("[HUDTestIntegration] Wired: health → death screen, retry, state → input.");
        }

        private void UnwireEvents()
        {
            if (playerHealth != null) playerHealth.OnDied -= OnPlayerDied;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRetryRequested   -= HandleRetry;
                GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────
        private void OnPlayerDied()
        {
            // GameManager.SetState(GameOver) freezes time, shows cursor, fires OnGameStateChanged.
            // DeathScreenController listens to OnGameStateChanged and fades in automatically.
            GameManager.Instance?.ReportPlayerDeath();
            Debug.Log("[HUDTestIntegration] Player reached 0 HP → ReportPlayerDeath()");
        }

        private void HandleRetry()
        {
            // Retry button: GameManager already set Playing + fired OnRetryRequested.
            // LevelManager.RespawnPlayer() handles teleport + health reset.
            // We reset health here too for scenes that don't have a LevelManager (backward compat).
            playerHealth?.ResetHealth();
            Debug.Log("[HUDTestIntegration] Retry: health restored to full.");
        }

        private void HandleStateChanged(GameState state)
        {
            // Keep player movement locked while any screen has focus.
            bool canMove = state == GameState.Playing;
            playerMotor?.SetInputEnabled(canMove);
        }
    }
}
