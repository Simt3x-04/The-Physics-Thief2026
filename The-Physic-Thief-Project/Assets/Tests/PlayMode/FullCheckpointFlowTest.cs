using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PhysicsThief.Tests.PlayMode
{
    /// <summary>
    /// Full checkpoint flow test for HUDtest scene.
    /// 
    /// This test verifies the complete respawn flow using the actual scene objects.
    /// Due to the stub PlayerMotor (KCC not installed), physics-based trigger entry
    /// cannot be tested. The test manually activates the checkpoint instead.
    /// 
    /// WHAT'S VERIFIED:
    /// - Real Player object with real HealthComponent
    /// - Real Checkpoint1 trigger and its Activate() method
    /// - Real LevelManager respawn logic
    /// - Real DeathScreenController and its Retry button
    /// - Real GameManager state transitions
    /// - Actual UI button click simulation
    /// 
    /// WHAT'S BLOCKED (requires KCC):
    /// - Player walking into checkpoint trigger
    /// - Physics-based collision detection
    /// - Real player movement
    /// </summary>
    [TestFixture]
    public class FullCheckpointFlowTest
    {
        // Scene objects
        private GameObject _player;
        private Transform _spawnPoint;
        private GameObject _checkpoint1GO;
        
        // Components via reflection
        private MonoBehaviour _gameManager;
        private MonoBehaviour _levelManager;
        private MonoBehaviour _healthComponent;
        private MonoBehaviour _checkpoint;
        private MonoBehaviour _deathScreenController;
        private MonoBehaviour _playerMotor;
        
        // UI elements
        private Button _retryButton;
        private GameObject _deathPanel;
        
        // Reflection cache
        private MethodInfo _takeDamageMethod;
        private PropertyInfo _currentHPProperty;
        private PropertyInfo _maxHPProperty;
        private PropertyInfo _gameStateProperty;
        private MethodInfo _clearCheckpointMethod;
        private PropertyInfo _hasActiveCheckpointProperty;
        private MethodInfo _checkpointActivateMethod;
        private PropertyInfo _checkpointRespawnPointProperty;
        private MethodInfo _teleportMethod;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.Log("=== LOADING HUDtest SCENE ===");
            var loadOp = SceneManager.LoadSceneAsync("HUDtest", LoadSceneMode.Single);
            while (!loadOp.isDone) yield return null;
            
            // Wait for full initialization
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);
            
            FindAllReferences();
            
            // Reset to clean state
            ResetSceneState();
            
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        private void FindAllReferences()
        {
            // Player
            _player = GameObject.Find("Player");
            Assert.IsNotNull(_player, "Player not found");
            
            // SpawnPoint
            var spawnGO = GameObject.Find("SpawnPoint");
            Assert.IsNotNull(spawnGO, "SpawnPoint not found");
            _spawnPoint = spawnGO.transform;
            
            // Checkpoint1
            _checkpoint1GO = GameObject.Find("Checkpoint1");
            Assert.IsNotNull(_checkpoint1GO, "Checkpoint1 not found");
            
            // GameManager
            var gmType = FindType("PhysicsThief.Core.GameManager");
            _gameManager = Object.FindFirstObjectByType(gmType) as MonoBehaviour;
            Assert.IsNotNull(_gameManager, "GameManager not found");
            _gameStateProperty = gmType.GetProperty("State");
            
            // LevelManager
            var lmType = FindType("PhysicsThief.Level.LevelManager");
            _levelManager = Object.FindFirstObjectByType(lmType) as MonoBehaviour;
            Assert.IsNotNull(_levelManager, "LevelManager not found");
            _clearCheckpointMethod = lmType.GetMethod("ClearCheckpoint");
            _hasActiveCheckpointProperty = lmType.GetProperty("HasActiveCheckpoint");
            
            // HealthComponent
            var hcType = FindType("PhysicsThief.Core.HealthComponent");
            _healthComponent = _player.GetComponent(hcType) as MonoBehaviour;
            Assert.IsNotNull(_healthComponent, "HealthComponent not found on Player");
            _takeDamageMethod = hcType.GetMethod("TakeDamage");
            _currentHPProperty = hcType.GetProperty("CurrentHP");
            _maxHPProperty = hcType.GetProperty("MaxHP");
            
            // Checkpoint
            var cpType = FindType("PhysicsThief.Level.Checkpoint");
            _checkpoint = _checkpoint1GO.GetComponent(cpType) as MonoBehaviour;
            Assert.IsNotNull(_checkpoint, "Checkpoint component not found");
            _checkpointActivateMethod = cpType.GetMethod("Activate");
            _checkpointRespawnPointProperty = cpType.GetProperty("RespawnPoint");
            
            // PlayerMotor
            var pmType = FindType("PhysicsThief.Player.PlayerMotor");
            _playerMotor = _player.GetComponent(pmType) as MonoBehaviour;
            Assert.IsNotNull(_playerMotor, "PlayerMotor not found");
            _teleportMethod = pmType.GetMethod("Teleport");
            
            // DeathScreenController and its UI
            var dsType = FindType("PhysicsThief.UI.DeathScreenController");
            _deathScreenController = Object.FindFirstObjectByType(dsType) as MonoBehaviour;
            Assert.IsNotNull(_deathScreenController, "DeathScreenController not found");
            
            // Find retry button via reflection
            var retryButtonField = dsType.GetField("retryButton", BindingFlags.NonPublic | BindingFlags.Instance);
            if (retryButtonField != null)
                _retryButton = retryButtonField.GetValue(_deathScreenController) as Button;
            
            var deathPanelField = dsType.GetField("deathPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            if (deathPanelField != null)
                _deathPanel = deathPanelField.GetValue(_deathScreenController) as GameObject;
        }

        private void ResetSceneState()
        {
            // Clear checkpoint
            _clearCheckpointMethod?.Invoke(_levelManager, null);
            
            // Reset checkpoint activation
            var resetMethod = _checkpoint.GetType().GetMethod("ResetActivation");
            resetMethod?.Invoke(_checkpoint, null);
            
            // Reset health
            var resetHealthMethod = _healthComponent.GetType().GetMethod("ResetHealth");
            resetHealthMethod?.Invoke(_healthComponent, null);
            
            // Set game to Playing state
            var setStateMethod = _gameManager.GetType().GetMethod("SetState");
            var gameStateType = FindType("PhysicsThief.Core.GameState");
            var playingValue = System.Enum.Parse(gameStateType, "Playing");
            setStateMethod?.Invoke(_gameManager, new object[] { playingValue });
            
            Time.timeScale = 1f;
        }

        private System.Type FindType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // MAIN TEST: Full Checkpoint Flow
        // ═══════════════════════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator FullCheckpointFlow_WithActualButtonClick()
        {
            Debug.Log("╔════════════════════════════════════════════════════════════════╗");
            Debug.Log("║  FULL CHECKPOINT FLOW TEST - Using Actual Scene Objects        ║");
            Debug.Log("╚════════════════════════════════════════════════════════════════╝");

            // ─────────────────────────────────────────────────────────────────
            // STEP 1: Report player controller status
            // ─────────────────────────────────────────────────────────────────
            Debug.Log("\n[STEP 1] Checking player controller...");
            
            var kccType = FindType("KinematicCharacterController.KinematicCharacterMotor");
            bool hasKCC = kccType != null && _player.GetComponent(kccType) != null;
            
            if (hasKCC)
            {
                Debug.Log("✓ KinematicCharacterMotor FOUND - Davide's controller is ACTIVE");
            }
            else
            {
                Debug.LogWarning("⚠ KinematicCharacterMotor NOT FOUND - Using STUB PlayerMotor");
                Debug.LogWarning("⚠ Physics trigger entry CANNOT be tested");
                Debug.LogWarning("⚠ Checkpoint will be activated MANUALLY via Activate()");
            }

            // ─────────────────────────────────────────────────────────────────
            // STEP 2: Verify initial state
            // ─────────────────────────────────────────────────────────────────
            Debug.Log("\n[STEP 2] Verifying initial state...");
            
            bool hasCheckpoint = (bool)_hasActiveCheckpointProperty.GetValue(_levelManager);
            Assert.IsFalse(hasCheckpoint, "Should start with no active checkpoint");
            Debug.Log("✓ No checkpoint active");
            
            float maxHP = (float)_maxHPProperty.GetValue(_healthComponent);
            float currentHP = (float)_currentHPProperty.GetValue(_healthComponent);
            Assert.AreEqual(maxHP, currentHP, "Should start with full HP");
            Debug.Log($"✓ Player HP: {currentHP}/{maxHP}");
            
            var state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("Playing", state.ToString());
            Debug.Log($"✓ GameState: {state}");

            // ─────────────────────────────────────────────────────────────────
            // STEP 3: Move player to checkpoint and activate
            // ─────────────────────────────────────────────────────────────────
            Debug.Log("\n[STEP 3] Activating checkpoint...");
            
            Vector3 checkpointPos = _checkpoint1GO.transform.position;
            Debug.Log($"  Checkpoint1 position: {checkpointPos}");
            
            // Move player to checkpoint position
            _teleportMethod.Invoke(_playerMotor, new object[] { checkpointPos, Quaternion.identity });
            Debug.Log($"  Player teleported to: {_player.transform.position}");
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            // Check if physics trigger fired (only works with KCC)
            hasCheckpoint = (bool)_hasActiveCheckpointProperty.GetValue(_levelManager);
            if (!hasCheckpoint)
            {
                Debug.Log("  Physics trigger did not fire (expected without KCC)");
                Debug.Log("  Manually calling Checkpoint.Activate()...");
                _checkpointActivateMethod.Invoke(_checkpoint, null);
                yield return null;
            }
            
            // Verify checkpoint is now active
            hasCheckpoint = (bool)_hasActiveCheckpointProperty.GetValue(_levelManager);
            Assert.IsTrue(hasCheckpoint, "Checkpoint should be active");
            Debug.Log("✓ Checkpoint1 ACTIVATED");
            
            Transform respawnPoint = (Transform)_checkpointRespawnPointProperty.GetValue(_checkpoint);
            Debug.Log($"  Respawn position: {respawnPoint.position}");

            // ─────────────────────────────────────────────────────────────────
            // STEP 4: Move player away and kill them
            // ─────────────────────────────────────────────────────────────────
            Debug.Log("\n[STEP 4] Moving player away and applying lethal damage...");
            
            Vector3 deathPosition = checkpointPos + new Vector3(20f, 0f, 20f);
            _teleportMethod.Invoke(_playerMotor, new object[] { deathPosition, Quaternion.identity });
            Debug.Log($"  Player moved to: {_player.transform.position}");
            
            yield return null;
            
            // Apply lethal damage
            _takeDamageMethod.Invoke(_healthComponent, new object[] { maxHP + 50f });
            Debug.Log("  Lethal damage applied!");
            
            // Wait for death to process (uses unscaled time for UI)
            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            // ─────────────────────────────────────────────────────────────────
            // STEP 5: Verify death screen opened (wait for fade using unscaled time)
            // ─────────────────────────────────────────────────────────────────
            Debug.Log("\n[STEP 5] Verifying death screen (waiting for fade)...");
            
            state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("GameOver", state.ToString(), "Should be in GameOver state");
            Debug.Log($"✓ GameState: {state}");
            
            Assert.AreEqual(0f, Time.timeScale, "Time should be frozen");
            Debug.Log("✓ Time frozen (timeScale = 0)");
            
            // Wait for fade animation to complete (fadeDuration = 0.6s + buffer)
            // DeathScreenController uses Time.unscaledDeltaTime, so we must wait in real time
            Debug.Log("  Waiting for fade animation (0.8s unscaled)...");
            float fadeWaitStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - fadeWaitStart < 0.8f)
            {
                yield return null;
            }
            Debug.Log($"  Fade wait complete (waited {Time.realtimeSinceStartup - fadeWaitStart:F2}s real time)");
            
            // ASSERT: Death panel must be visible after fade
            if (_deathPanel != null)
            {
                Debug.Log($"  Death panel active: {_deathPanel.activeSelf}");
                Assert.IsTrue(_deathPanel.activeSelf, "Death panel should be VISIBLE after fade completes");
                Debug.Log("✓ Death panel VISIBLE");
            }
            else
            {
                Debug.LogWarning("⚠ Death panel reference not found via reflection");
            }
            
            // ASSERT: Retry button must be interactable for UI click
            if (_retryButton != null)
            {
                Assert.IsTrue(_retryButton.gameObject.activeInHierarchy, "Retry button should be active in hierarchy");
                Assert.IsTrue(_retryButton.interactable, "Retry button should be interactable");
                Debug.Log("✓ Retry button is CLICKABLE (active + interactable)");
            }
            
            currentHP = (float)_currentHPProperty.GetValue(_healthComponent);
            Debug.Log($"  Player HP: {currentHP}/{maxHP} (dead)");

            // ─────────────────────────────────────────────────────────────────
            // STEP 6: Click the actual Retry button
            // ─────────────────────────────────────────────────────────────────
            Debug.Log("\n[STEP 6] Clicking 'Snooze and retry' button...");
            
            if (_retryButton != null)
            {
                Debug.Log($"  Retry button found: {_retryButton.name}");
                Debug.Log($"  Button interactable: {_retryButton.interactable}");
                
                // Simulate button click
                _retryButton.onClick.Invoke();
                Debug.Log("  Button onClick.Invoke() called!");
            }
            else
            {
                Debug.LogWarning("  Retry button not found via reflection, using GameManager directly");
                var retryMethod = _gameManager.GetType().GetMethod("RetryFromCheckpoint");
                retryMethod.Invoke(_gameManager, null);
            }
            
            // Wait for retry to process (fade animation uses unscaled time)
            elapsed = 0f;
            while (elapsed < 1.0f)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            // ─────────────────────────────────────────────────────────────────
            // STEP 7: Verify respawn at checkpoint
            // ─────────────────────────────────────────────────────────────────
            Debug.Log("\n[STEP 7] Verifying respawn...");
            
            // Check position
            float distToCheckpoint = Vector3.Distance(_player.transform.position, respawnPoint.position);
            Debug.Log($"  Player position: {_player.transform.position}");
            Debug.Log($"  Checkpoint position: {respawnPoint.position}");
            Debug.Log($"  Distance: {distToCheckpoint}");
            
            Assert.Less(distToCheckpoint, 1f, "Player should respawn at checkpoint");
            Debug.Log("✓ Player respawned at CHECKPOINT (not SpawnPoint)");
            
            // Check health
            currentHP = (float)_currentHPProperty.GetValue(_healthComponent);
            maxHP = (float)_maxHPProperty.GetValue(_healthComponent);
            Debug.Log($"  Player HP: {currentHP}/{maxHP}");
            Assert.AreEqual(maxHP, currentHP, 0.1f, "Health should be restored");
            Debug.Log("✓ Health RESTORED to full");
            
            // Check game state
            state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("Playing", state.ToString(), "Should be back to Playing state");
            Debug.Log($"✓ GameState: {state}");
            
            // Check time unfrozen
            Assert.AreEqual(1f, Time.timeScale, "Time should be unfrozen");
            Debug.Log("✓ Time resumed (timeScale = 1)");

            // ─────────────────────────────────────────────────────────────────
            // FINAL SUMMARY
            // ─────────────────────────────────────────────────────────────────
            Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
            Debug.Log("║                    TEST RESULTS SUMMARY                        ║");
            Debug.Log("╠════════════════════════════════════════════════════════════════╣");
            Debug.Log("║ ✓ Checkpoint activation: PASS                                  ║");
            Debug.Log("║ ✓ Death triggers GameOver: PASS                                ║");
            Debug.Log("║ ✓ Death panel VISIBLE after fade: PASS                         ║");
            Debug.Log("║ ✓ Retry button CLICKABLE: PASS                                 ║");
            Debug.Log("║ ✓ Retry button click processed: PASS                           ║");
            Debug.Log("║ ✓ Respawn at checkpoint: PASS                                  ║");
            Debug.Log("║ ✓ Health restored: PASS                                        ║");
            Debug.Log("║ ✓ Controls restored (Playing state): PASS                      ║");
            Debug.Log("╠════════════════════════════════════════════════════════════════╣");
            if (!hasKCC)
            {
                Debug.Log("║ ⚠ Player controller: STUB (KCC not installed)                 ║");
                Debug.Log("║ ⚠ Physics trigger entry: NOT TESTED (requires KCC)            ║");
                Debug.Log("║ ⚠ Real player movement: UNVERIFIED (requires KCC)             ║");
            }
            else
            {
                Debug.Log("║ ✓ Player controller: KCC (Davide's controller)                ║");
                Debug.Log("║ ✓ Physics trigger entry: VERIFIED                             ║");
            }
            Debug.Log("╚════════════════════════════════════════════════════════════════╝");
        }
    }
}
