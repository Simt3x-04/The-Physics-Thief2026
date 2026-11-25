// ──────────────────────────────────────────────────────────────────────────────
// UIFlowTestHarness.cs
//
// TEST HARNESS – NOT FOR RELEASE BUILDS
// Attach to a GameObject in UIFlowTest scene to drive every UI screen without
// needing enemies, levels, or a real player.
//
// Controls (keyboard):
//   D  – simulate player death (fires playerHealth.Die())
//   E  – simulate ending (calls GameManager.TriggerAlarmAwakeSequence())
//   R  – call RetryFromCheckpoint directly (jumps to Playing state)
//   P  – toggle pause (PauseGame / ResumeGame)
//   M  – load main menu
//   F1 – run quick smoke test (prints results to console)
// ──────────────────────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.InputSystem;
using PhysicsThief.Core;

namespace PhysicsThief.Tests
{
    /// <summary>
    /// Isolated test harness for all five UI screens.
    /// Keep this GameObject inactive in release builds, or exclude via scripting define.
    /// </summary>
    public class UIFlowTestHarness : MonoBehaviour
    {
        [Header("Test Player Health (assign a dummy HealthComponent)")]
        [SerializeField] private HealthComponent testPlayerHealth;

        [Header("Death Screen (assign to verify Bind() wiring)")]
        [SerializeField] private UI.DeathScreenController deathScreen;

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[UIFlowTest] GameManager not found. " +
                               "Add GameManager prefab to this scene.");
                return;
            }

            // Wire death screen to test health source.
            if (deathScreen != null && testPlayerHealth != null)
                deathScreen.Bind(testPlayerHealth);

            // Subscribe to retry so test checkpoint (just resets health) works.
            GameManager.Instance.OnRetryRequested += OnTestRetryRequested;

            Debug.Log("[UIFlowTest] Harness ready. D=Death  E=Ending  R=Retry  P=Pause  M=Menu  F1=SmokeTest");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRetryRequested -= OnTestRetryRequested;
        }

        // ───────────────────────────────────────────────────────────────────────
        private void Update()
        {
            if (GameManager.Instance == null) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb[Key.D].wasPressedThisFrame)
            {
                Debug.Log("[UIFlowTest] Simulating player death…");
                testPlayerHealth?.Die();
            }
            if (kb[Key.E].wasPressedThisFrame)
            {
                Debug.Log("[UIFlowTest] Triggering alarm/ending sequence…");
                GameManager.Instance.TriggerAlarmAwakeSequence();
            }
            if (kb[Key.R].wasPressedThisFrame)
            {
                Debug.Log("[UIFlowTest] Calling RetryFromCheckpoint…");
                GameManager.Instance.RetryFromCheckpoint();
            }
            if (kb[Key.P].wasPressedThisFrame)
            {
                if (GameManager.Instance.State == GameState.Playing)
                    GameManager.Instance.PauseGame();
                else if (GameManager.Instance.State == GameState.Paused)
                    GameManager.Instance.ResumeGame();
            }
            if (kb[Key.M].wasPressedThisFrame)
            {
                Debug.Log("[UIFlowTest] Loading main menu…");
                GameManager.Instance.LoadMainMenu();
            }
            if (kb[Key.F1].wasPressedThisFrame)
                RunSmokeTest();
        }

        // ───────────────────────────────────────────────────────────────────────
        private void OnTestRetryRequested()
        {
            // Minimal retry: just reset health so the test is demonstrable.
            // Real game: LevelManager.RespawnPlayer() + player.Respawn(checkpoint) goes here.
            Debug.Log("[UIFlowTest] OnRetryRequested received – resetting test health.");
            testPlayerHealth?.ResetHealth();
        }

        // ───────────────────────────────────────────────────────────────────────
        private void RunSmokeTest()
        {
            Debug.Log("=== UIFlowTest SMOKE TEST ===");

            bool pass = true;

            // 1. GameManager exists and is in Playing (set up in scene).
            if (GameManager.Instance == null)
            { Debug.LogError("FAIL: GameManager.Instance is null"); pass = false; }
            else
            { Debug.Log("PASS: GameManager.Instance exists, state=" + GameManager.Instance.State); }

            // 2. UISettings loads without throwing.
            try
            {
                float v = UI.UISettings.MasterVolume;
                float s = UI.UISettings.MouseSensitivity;
                Debug.Log($"PASS: UISettings loaded — volume={v:0.##}, sensitivity={s:0.##}");
            }
            catch (System.Exception ex)
            { Debug.LogError("FAIL: UISettings threw " + ex.Message); pass = false; }

            // 3. HealthComponent exists and is alive.
            if (testPlayerHealth == null)
            { Debug.LogWarning("SKIP: testPlayerHealth not assigned"); }
            else if (testPlayerHealth.IsDead)
            { Debug.LogError("FAIL: testPlayerHealth already dead at test start"); pass = false; }
            else
            { Debug.Log($"PASS: testPlayerHealth alive, HP={testPlayerHealth.CurrentHP}/{testPlayerHealth.MaxHP}"); }

            // 4. DeathScreen bound.
            if (deathScreen == null)
            { Debug.LogWarning("SKIP: deathScreen not assigned in harness"); }
            else
            { Debug.Log("PASS: deathScreen reference present"); }

            Debug.Log(pass ? "=== SMOKE TEST PASSED ===" : "=== SMOKE TEST FAILED (see errors above) ===");
        }
    }
}
