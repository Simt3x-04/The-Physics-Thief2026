using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Player;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Glue between the player, the GameManager and the UI screens of a gameplay scene (the village map).
    /// Place it on a root GameObject ("GameplayIntegration"); the menu
    /// Tools > Physics Thief > 1 - Setup Gameplay In Open Scene creates it and fills the fields.
    ///
    /// WHAT IT DOES
    ///   - Player.HealthComponent.OnDied -> GameManager.ReportPlayerDeath()
    ///     (GameManager sets GameOver, freezes time and shows the cursor; the death screen
    ///      listens to OnDied on its own and fades in).
    ///   - GameManager.OnRetryRequested -> restores the player's health.
    ///   - GameManager.OnGameStateChanged -> PlayerController.SetInputEnabled()
    ///     (no look / movement while paused, dead or in a menu).
    ///   - DeathScreenController.Bind(health) and HUDManager.Bind(health, inventory, scanner), so both screens
    ///     follow the real player even if the Inspector references are empty.
    ///   - Puts the GameManager in the Playing state, so the player can move as soon as the scene starts
    ///     (also when you press Play directly in the map without going through the main menu).
    ///
    /// RESPAWN (done by a teammate)
    ///   PlayerController.Respawn(Vector3) already teleports Gino and restores his health. When
    ///   LevelManager.RespawnPlayer() is written, it only has to call player.Respawn(checkpoint.position)
    ///   and subscribe to GameManager.Instance.OnRetryRequested.
    ///
    /// HOT-METAL DAMAGE
    ///   To test it: scan the metal, copy a temperature of 70 C or more and paste it on another metal object;
    ///   a hot object that overlaps the player calls HealthComponent.TakeDamage().
    /// </summary>
    [DisallowMultipleComponent]
    public class HUDTestIntegration : MonoBehaviour
    {
        [Header("Player (auto-found if empty)")]
        [Tooltip("The PlayerController on Gino. Used to block input while paused or dead.")]
        [SerializeField] private PlayerController player;

        [Tooltip("HealthComponent on Gino (not on a test dummy).")]
        [SerializeField] private HealthComponent playerHealth;

        [Header("UI screens (auto-found if empty)")]
        [SerializeField] private DeathScreenController deathScreen;
        [SerializeField] private HUDManager hudManager;

        private bool eventsWired;

        // ---------- Unity lifecycle ----------

        private void Start()
        {
            ResolveReferences();
            WireEvents();
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        // ---------- reference resolution ----------

        private void ResolveReferences()
        {
            // Prefer the real player: its PlayerController is the only one in the scene.
            if (player == null)
                player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

            if (playerHealth == null && player != null)
                playerHealth = player.GetComponent<HealthComponent>();
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<HealthComponent>();

            if (deathScreen == null)
                deathScreen = FindFirstObjectByType<DeathScreenController>(FindObjectsInactive.Include);

            // The HUD is optional: a scene can run without it.
            if (hudManager == null)
                hudManager = FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);

            if (player == null)
                Debug.LogError("[HUDTestIntegration] No PlayerController found: input will not be blocked while paused.");
            if (playerHealth == null)
                Debug.LogError("[HUDTestIntegration] playerHealth not found. Add a HealthComponent to the player.");
            if (GameManager.Instance == null)
                Debug.LogError("[HUDTestIntegration] GameManager.Instance is null. " +
                               "Run Tools > Physics Thief > 1 - Setup Gameplay In Open Scene.");
            if (deathScreen == null)
                Debug.LogWarning("[HUDTestIntegration] DeathScreenController not found: dying will freeze the game with no screen.");
        }

        private void WireEvents()
        {
            if (eventsWired) return;

            if (playerHealth != null)
            {
                playerHealth.OnDied -= OnPlayerDied;
                playerHealth.OnDied += OnPlayerDied;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRetryRequested -= HandleRetry;
                GameManager.Instance.OnRetryRequested += HandleRetry;
                GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
                GameManager.Instance.OnGameStateChanged += HandleStateChanged;
            }

            if (deathScreen != null && playerHealth != null)
                deathScreen.Bind(playerHealth);

            if (hudManager != null && playerHealth != null)
            {
                // The inventory sits on the player root; the scanner sits on the camera (a child of the player).
                Transform root = player != null ? player.transform : playerHealth.transform;
                BriefcaseInventory inventory = root.GetComponent<BriefcaseInventory>();
                PlayerScanner scanner = root.GetComponentInChildren<PlayerScanner>(true);
                hudManager.Bind(playerHealth, inventory, scanner);
            }

            // Start in the Playing state so the player can move immediately.
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.State != GameState.Playing)
                    GameManager.Instance.SetState(GameState.Playing);
                else
                    HandleStateChanged(GameState.Playing);
            }

            eventsWired = true;
            Debug.Log("[HUDTestIntegration] Wired: health -> death screen, retry, game state -> player input.");
        }

        private void UnwireEvents()
        {
            if (playerHealth != null) playerHealth.OnDied -= OnPlayerDied;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRetryRequested -= HandleRetry;
                GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
            }
        }

        // ---------- event handlers ----------

        private void OnPlayerDied()
        {
            // GameManager.SetState(GameOver) freezes time and shows the cursor;
            // the death screen listens to OnDied on its own and fades in.
            GameManager.Instance?.ReportPlayerDeath();
            Debug.Log("[HUDTestIntegration] Player reached 0 HP -> ReportPlayerDeath()");
        }

        private void HandleRetry()
        {
            // The GameManager is already back in Playing when this fires; the input is re-enabled by
            // HandleStateChanged. Here we only restore the health (the teleport belongs to LevelManager).
            if (playerHealth != null) playerHealth.ResetHealth();
            Debug.Log("[HUDTestIntegration] Retry: health restored to full.");
        }

        private void HandleStateChanged(GameState state)
        {
            // Keep the player frozen while any screen has focus.
            if (player != null) player.SetInputEnabled(state == GameState.Playing);
        }
    }
}
