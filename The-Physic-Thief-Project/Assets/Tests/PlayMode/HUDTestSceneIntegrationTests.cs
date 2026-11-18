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
    /// PlayMode integration tests for checkpoint respawn flow in HUDtest scene.
    /// Uses reflection to access game types since test assembly can't directly reference Assembly-CSharp.
    /// 
    /// Tests the actual flow:
    /// 1. No checkpoint → die → retry → respawn at SpawnPoint
    /// 2. Checkpoint activated → die → retry → respawn at checkpoint
    /// 3. Fresh run → retry uses SpawnPoint before any checkpoint
    /// </summary>
    [TestFixture]
    public class HUDTestSceneIntegrationTests
    {
        private const string HUDTestScenePath = "Assets/Scenes/HUDtest.unity";
        private const float TestTimeout = 10f;

        // Cached scene objects (found at runtime)
        private GameObject _player;
        private Transform _spawnPoint;
        private GameObject _checkpoint1GO;
        private MonoBehaviour _gameManager;
        private MonoBehaviour _levelManager;
        private MonoBehaviour _healthComponent;
        private MonoBehaviour _checkpoint;
        private MonoBehaviour _deathScreenController;

        // Reflection-cached methods/properties
        private MethodInfo _takeDamageMethod;
        private MethodInfo _resetHealthMethod;
        private PropertyInfo _currentHPProperty;
        private PropertyInfo _maxHPProperty;
        private PropertyInfo _isDeadProperty;
        private PropertyInfo _gameStateProperty;
        private MethodInfo _retryFromCheckpointMethod;
        private MethodInfo _clearCheckpointMethod;
        private MethodInfo _respawnPlayerMethod;
        private PropertyInfo _hasActiveCheckpointProperty;
        private MethodInfo _checkpointActivateMethod;
        private MethodInfo _checkpointResetMethod;
        private PropertyInfo _checkpointIsActivatedProperty;
        private PropertyInfo _checkpointRespawnPointProperty;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Load HUDtest scene
            Debug.Log("[SetUp] Loading HUDtest scene...");
            var loadOp = SceneManager.LoadSceneAsync("HUDtest", LoadSceneMode.Single);
            while (!loadOp.isDone)
                yield return null;

            // Wait several frames for scene initialization (Awake, Start, etc.)
            Debug.Log("[SetUp] Scene loaded, waiting for initialization...");
            yield return null;
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            // Find scene objects
            Debug.Log("[SetUp] Finding scene references...");
            FindSceneReferences();

            // Log what we found
            Debug.Log($"[SetUp] Player: {_player != null}");
            Debug.Log($"[SetUp] SpawnPoint: {_spawnPoint != null}");
            Debug.Log($"[SetUp] Checkpoint1: {_checkpoint1GO != null}");
            Debug.Log($"[SetUp] GameManager: {_gameManager != null}");
            Debug.Log($"[SetUp] LevelManager: {_levelManager != null}");
            Debug.Log($"[SetUp] HealthComponent: {_healthComponent != null}");

            // Ensure clean state - clear any checkpoint and reset health
            if (_levelManager != null && _clearCheckpointMethod != null)
            {
                _clearCheckpointMethod.Invoke(_levelManager, null);
            }

            if (_healthComponent != null && _resetHealthMethod != null)
            {
                _resetHealthMethod.Invoke(_healthComponent, null);
            }

            // Reset checkpoint activation state
            if (_checkpoint != null && _checkpointResetMethod != null)
            {
                _checkpointResetMethod.Invoke(_checkpoint, null);
            }

            // Ensure game is in Playing state
            if (_gameManager != null)
            {
                var setStateMethod = _gameManager.GetType().GetMethod("SetState");
                if (setStateMethod != null)
                {
                    // GameState.Playing = 2 (enum value)
                    var gameStateType = _gameManager.GetType().Assembly.GetType("PhysicsThief.Core.GameState");
                    var playingValue = System.Enum.Parse(gameStateType, "Playing");
                    setStateMethod.Invoke(_gameManager, new object[] { playingValue });
                    Debug.Log("[SetUp] Set GameState to Playing");
                }
            }

            // Restore time scale (in case previous test left it frozen)
            Time.timeScale = 1f;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Restore time scale
            Time.timeScale = 1f;
            yield return null;
        }

        private void FindSceneReferences()
        {
            // Find Player
            _player = GameObject.Find("Player");
            Assert.IsNotNull(_player, "Player not found in scene");

            // Find SpawnPoint
            var spawnGO = GameObject.Find("SpawnPoint");
            Assert.IsNotNull(spawnGO, "SpawnPoint not found in scene");
            _spawnPoint = spawnGO.transform;

            // Find Checkpoint1
            _checkpoint1GO = GameObject.Find("Checkpoint1");
            Assert.IsNotNull(_checkpoint1GO, "Checkpoint1 not found in scene");

            // Find GameManager - try multiple methods
            var gmType = FindTypeByName("PhysicsThief.Core.GameManager");
            if (gmType != null)
            {
                // First try the static Instance property
                var instanceProp = gmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp != null)
                {
                    _gameManager = instanceProp.GetValue(null) as MonoBehaviour;
                }
                
                // If not found via singleton, try FindFirstObjectByType
                if (_gameManager == null)
                {
                    _gameManager = Object.FindFirstObjectByType(gmType) as MonoBehaviour;
                    Debug.Log($"[FindRefs] GameManager found via FindFirstObjectByType: {_gameManager != null}");
                }
                
                // If still not found, look for GameObject named "GameManager"
                if (_gameManager == null)
                {
                    var gmGO = GameObject.Find("GameManager");
                    if (gmGO != null)
                    {
                        _gameManager = gmGO.GetComponent(gmType) as MonoBehaviour;
                        Debug.Log($"[FindRefs] GameManager found via GameObject.Find: {_gameManager != null}");
                    }
                }
            }

            // Find LevelManager
            var lmType = FindTypeByName("PhysicsThief.Level.LevelManager");
            if (lmType != null)
            {
                _levelManager = Object.FindFirstObjectByType(lmType) as MonoBehaviour;
                if (_levelManager != null)
                {
                    _clearCheckpointMethod = lmType.GetMethod("ClearCheckpoint");
                    _respawnPlayerMethod = lmType.GetMethod("RespawnPlayer");
                    _hasActiveCheckpointProperty = lmType.GetProperty("HasActiveCheckpoint");
                }
            }

            // Find HealthComponent on Player
            var hcType = FindTypeByName("PhysicsThief.Core.HealthComponent");
            if (hcType != null && _player != null)
            {
                _healthComponent = _player.GetComponent(hcType) as MonoBehaviour;
                if (_healthComponent != null)
                {
                    _takeDamageMethod = hcType.GetMethod("TakeDamage");
                    _resetHealthMethod = hcType.GetMethod("ResetHealth");
                    _currentHPProperty = hcType.GetProperty("CurrentHP");
                    _maxHPProperty = hcType.GetProperty("MaxHP");
                    _isDeadProperty = hcType.GetProperty("IsDead");
                }
            }

            // Find Checkpoint component on Checkpoint1
            var cpType = FindTypeByName("PhysicsThief.Level.Checkpoint");
            if (cpType != null && _checkpoint1GO != null)
            {
                _checkpoint = _checkpoint1GO.GetComponent(cpType) as MonoBehaviour;
                if (_checkpoint != null)
                {
                    _checkpointActivateMethod = cpType.GetMethod("Activate");
                    _checkpointResetMethod = cpType.GetMethod("ResetActivation");
                    _checkpointIsActivatedProperty = cpType.GetProperty("IsActivated");
                    _checkpointRespawnPointProperty = cpType.GetProperty("RespawnPoint");
                }
            }

            // Find DeathScreenController
            var dsType = FindTypeByName("PhysicsThief.UI.DeathScreenController");
            if (dsType != null)
            {
                _deathScreenController = Object.FindFirstObjectByType(dsType) as MonoBehaviour;
            }

            // GameManager reflection
            if (_gameManager != null)
            {
                var gmType2 = _gameManager.GetType();
                _gameStateProperty = gmType2.GetProperty("State");
                _retryFromCheckpointMethod = gmType2.GetMethod("RetryFromCheckpoint");
            }
        }

        private System.Type FindTypeByName(string fullName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 1: No checkpoint → die → retry → respawn at SpawnPoint
        // ═══════════════════════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator Scenario1_NoCheckpoint_DieAndRetry_RespawnsAtSpawnPoint()
        {
            Debug.Log("[Scenario1] Starting: No checkpoint → die → retry → respawn at SpawnPoint");

            // Preconditions
            Assert.IsNotNull(_player, "Player required");
            Assert.IsNotNull(_spawnPoint, "SpawnPoint required");
            Assert.IsNotNull(_levelManager, "LevelManager required");
            Assert.IsNotNull(_healthComponent, "HealthComponent required");
            Assert.IsNotNull(_gameManager, "GameManager required");

            // 1. Ensure no checkpoint is active
            if (_clearCheckpointMethod != null)
                _clearCheckpointMethod.Invoke(_levelManager, null);

            bool hasCheckpoint = (bool)_hasActiveCheckpointProperty.GetValue(_levelManager);
            Assert.IsFalse(hasCheckpoint, "No checkpoint should be active");
            Debug.Log("[Scenario1] Confirmed: No active checkpoint");

            // 2. Move player away from SpawnPoint
            Vector3 awayPosition = _spawnPoint.position + new Vector3(10f, 0f, 10f);
            _player.transform.position = awayPosition;
            Debug.Log($"[Scenario1] Player moved to: {_player.transform.position}");

            yield return null;

            // 3. Apply lethal damage to kill the player
            float maxHP = (float)_maxHPProperty.GetValue(_healthComponent);
            _takeDamageMethod.Invoke(_healthComponent, new object[] { maxHP + 10f });
            
            Debug.Log("[Scenario1] Applied lethal damage");

            // Wait for death screen to appear (uses unscaled time)
            float waitTime = 0f;
            while (waitTime < 2f)
            {
                yield return null;
                waitTime += Time.unscaledDeltaTime;
            }

            // 4. Verify GameOver state
            var state = _gameStateProperty.GetValue(_gameManager);
            Debug.Log($"[Scenario1] GameState: {state}");
            Assert.AreEqual("GameOver", state.ToString(), "Game should be in GameOver state after death");

            // 5. Invoke retry (simulates clicking "Snooze and retry" button)
            _retryFromCheckpointMethod.Invoke(_gameManager, null);
            Debug.Log("[Scenario1] RetryFromCheckpoint invoked");

            // Wait for retry to complete
            yield return null;
            yield return null;

            // 6. Verify player respawned at SpawnPoint
            float distanceToSpawn = Vector3.Distance(_player.transform.position, _spawnPoint.position);
            Debug.Log($"[Scenario1] Player position after retry: {_player.transform.position}");
            Debug.Log($"[Scenario1] SpawnPoint position: {_spawnPoint.position}");
            Debug.Log($"[Scenario1] Distance to spawn: {distanceToSpawn}");

            Assert.Less(distanceToSpawn, 1f, $"Player should respawn at SpawnPoint. Distance: {distanceToSpawn}");

            // 7. Verify HP is restored
            float currentHP = (float)_currentHPProperty.GetValue(_healthComponent);
            maxHP = (float)_maxHPProperty.GetValue(_healthComponent);
            Debug.Log($"[Scenario1] HP after retry: {currentHP}/{maxHP}");
            Assert.AreEqual(maxHP, currentHP, 0.1f, "HP should be fully restored");

            // 8. Verify gameplay resumed (Playing state)
            state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("Playing", state.ToString(), "Game should be in Playing state after retry");

            Debug.Log("[Scenario1] PASSED: Player respawned at SpawnPoint with full HP");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 2: Checkpoint activated → die → retry → respawn at checkpoint
        // ═══════════════════════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator Scenario2_CheckpointActivated_DieAndRetry_RespawnsAtCheckpoint()
        {
            Debug.Log("[Scenario2] Starting: Checkpoint activated → die → retry → respawn at checkpoint");

            // Preconditions
            Assert.IsNotNull(_player, "Player required");
            Assert.IsNotNull(_checkpoint1GO, "Checkpoint1 required");
            Assert.IsNotNull(_checkpoint, "Checkpoint component required");
            Assert.IsNotNull(_levelManager, "LevelManager required");
            Assert.IsNotNull(_healthComponent, "HealthComponent required");
            Assert.IsNotNull(_gameManager, "GameManager required");

            // 1. Clear any previous checkpoint
            if (_clearCheckpointMethod != null)
                _clearCheckpointMethod.Invoke(_levelManager, null);
            if (_checkpointResetMethod != null)
                _checkpointResetMethod.Invoke(_checkpoint, null);

            yield return null;

            // 2. Move player into checkpoint trigger to activate it
            _player.transform.position = _checkpoint1GO.transform.position;
            Debug.Log($"[Scenario2] Player moved to checkpoint: {_player.transform.position}");

            // Wait for trigger to process
            yield return new WaitForFixedUpdate();
            yield return null;

            // If trigger didn't fire automatically, activate manually
            bool isActivated = (bool)_checkpointIsActivatedProperty.GetValue(_checkpoint);
            if (!isActivated)
            {
                Debug.Log("[Scenario2] Trigger didn't fire, activating checkpoint manually");
                _checkpointActivateMethod.Invoke(_checkpoint, null);
                yield return null;
            }

            // 3. Verify checkpoint is activated
            isActivated = (bool)_checkpointIsActivatedProperty.GetValue(_checkpoint);
            Assert.IsTrue(isActivated, "Checkpoint should be activated");

            bool hasCheckpoint = (bool)_hasActiveCheckpointProperty.GetValue(_levelManager);
            Assert.IsTrue(hasCheckpoint, "LevelManager should have active checkpoint");
            Debug.Log("[Scenario2] Checkpoint activated successfully");

            // Get checkpoint respawn position
            Transform respawnPoint = (Transform)_checkpointRespawnPointProperty.GetValue(_checkpoint);
            Vector3 checkpointRespawnPos = respawnPoint.position;
            Debug.Log($"[Scenario2] Checkpoint respawn position: {checkpointRespawnPos}");

            // 4. Move player away from checkpoint
            Vector3 awayPosition = checkpointRespawnPos + new Vector3(15f, 0f, 15f);
            _player.transform.position = awayPosition;
            Debug.Log($"[Scenario2] Player moved away to: {_player.transform.position}");

            yield return null;

            // 5. Apply lethal damage
            float maxHP = (float)_maxHPProperty.GetValue(_healthComponent);
            _takeDamageMethod.Invoke(_healthComponent, new object[] { maxHP + 10f });
            Debug.Log("[Scenario2] Applied lethal damage");

            // Wait for death screen
            float waitTime = 0f;
            while (waitTime < 2f)
            {
                yield return null;
                waitTime += Time.unscaledDeltaTime;
            }

            // 6. Verify GameOver state
            var state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("GameOver", state.ToString(), "Game should be in GameOver state");

            // 7. Invoke retry
            _retryFromCheckpointMethod.Invoke(_gameManager, null);
            Debug.Log("[Scenario2] RetryFromCheckpoint invoked");

            yield return null;
            yield return null;

            // 8. Verify player respawned at checkpoint
            float distanceToCheckpoint = Vector3.Distance(_player.transform.position, checkpointRespawnPos);
            Debug.Log($"[Scenario2] Player position after retry: {_player.transform.position}");
            Debug.Log($"[Scenario2] Distance to checkpoint: {distanceToCheckpoint}");

            Assert.Less(distanceToCheckpoint, 1f, $"Player should respawn at checkpoint. Distance: {distanceToCheckpoint}");

            // 9. Verify HP is restored
            float currentHP = (float)_currentHPProperty.GetValue(_healthComponent);
            Debug.Log($"[Scenario2] HP after retry: {currentHP}/{maxHP}");
            Assert.AreEqual(maxHP, currentHP, 0.1f, "HP should be fully restored");

            // 10. Verify gameplay resumed
            state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("Playing", state.ToString(), "Game should be in Playing state");

            Debug.Log("[Scenario2] PASSED: Player respawned at Checkpoint with full HP");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 3: Fresh run → retry uses SpawnPoint before checkpoint
        // ═══════════════════════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator Scenario3_FreshRun_RetryUsesSpawnPointBeforeCheckpoint()
        {
            Debug.Log("[Scenario3] Starting: Fresh run → retry uses SpawnPoint before any checkpoint");

            // Preconditions
            Assert.IsNotNull(_player, "Player required");
            Assert.IsNotNull(_spawnPoint, "SpawnPoint required");
            Assert.IsNotNull(_checkpoint1GO, "Checkpoint1 required");
            Assert.IsNotNull(_checkpoint, "Checkpoint component required");
            Assert.IsNotNull(_levelManager, "LevelManager required");
            Assert.IsNotNull(_healthComponent, "HealthComponent required");
            Assert.IsNotNull(_gameManager, "GameManager required");

            // 1. Simulate fresh run - clear checkpoint and reset state
            if (_clearCheckpointMethod != null)
                _clearCheckpointMethod.Invoke(_levelManager, null);
            if (_checkpointResetMethod != null)
                _checkpointResetMethod.Invoke(_checkpoint, null);
            if (_resetHealthMethod != null)
                _resetHealthMethod.Invoke(_healthComponent, null);

            yield return null;

            // 2. Verify fresh state - no checkpoint activated
            bool hasCheckpoint = (bool)_hasActiveCheckpointProperty.GetValue(_levelManager);
            Assert.IsFalse(hasCheckpoint, "Fresh run should have no active checkpoint");

            bool checkpointActivated = (bool)_checkpointIsActivatedProperty.GetValue(_checkpoint);
            Assert.IsFalse(checkpointActivated, "Checkpoint should not be activated on fresh run");
            Debug.Log("[Scenario3] Confirmed: Fresh run state - no checkpoint");

            // 3. Move player to a position that's NOT the checkpoint (between spawn and checkpoint)
            Vector3 midPosition = (_spawnPoint.position + _checkpoint1GO.transform.position) / 2f;
            _player.transform.position = midPosition;
            Debug.Log($"[Scenario3] Player at mid-level position: {_player.transform.position}");

            yield return null;

            // 4. Apply lethal damage BEFORE reaching any checkpoint
            float maxHP = (float)_maxHPProperty.GetValue(_healthComponent);
            _takeDamageMethod.Invoke(_healthComponent, new object[] { maxHP + 10f });
            Debug.Log("[Scenario3] Applied lethal damage (before checkpoint)");

            // Wait for death screen
            float waitTime = 0f;
            while (waitTime < 2f)
            {
                yield return null;
                waitTime += Time.unscaledDeltaTime;
            }

            // 5. Verify still no checkpoint active (player didn't reach it)
            hasCheckpoint = (bool)_hasActiveCheckpointProperty.GetValue(_levelManager);
            Assert.IsFalse(hasCheckpoint, "Should still have no checkpoint (player died before reaching it)");

            // 6. Invoke retry
            var state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("GameOver", state.ToString(), "Game should be in GameOver state");

            _retryFromCheckpointMethod.Invoke(_gameManager, null);
            Debug.Log("[Scenario3] RetryFromCheckpoint invoked");

            yield return null;
            yield return null;

            // 7. Verify player respawned at SpawnPoint (not checkpoint)
            float distanceToSpawn = Vector3.Distance(_player.transform.position, _spawnPoint.position);
            float distanceToCheckpoint = Vector3.Distance(_player.transform.position, _checkpoint1GO.transform.position);

            Debug.Log($"[Scenario3] Player position after retry: {_player.transform.position}");
            Debug.Log($"[Scenario3] Distance to SpawnPoint: {distanceToSpawn}");
            Debug.Log($"[Scenario3] Distance to Checkpoint: {distanceToCheckpoint}");

            Assert.Less(distanceToSpawn, 1f, $"Player should respawn at SpawnPoint. Distance: {distanceToSpawn}");
            Assert.Greater(distanceToCheckpoint, 1f, "Player should NOT be at checkpoint position");

            // 8. Verify HP restored and gameplay resumed
            float currentHP = (float)_currentHPProperty.GetValue(_healthComponent);
            Assert.AreEqual(maxHP, currentHP, 0.1f, "HP should be fully restored");

            state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("Playing", state.ToString(), "Game should be in Playing state");

            Debug.Log("[Scenario3] PASSED: Fresh run retry correctly uses SpawnPoint");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Additional test: Report player controller type
        // ═══════════════════════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator ReportPlayerControllerType()
        {
            Debug.Log("[ControllerCheck] Checking player controller type...");

            Assert.IsNotNull(_player, "Player required");

            // Check for KCC
            var kccType = FindTypeByName("KinematicCharacterController.KinematicCharacterMotor");
            bool hasKCC = kccType != null && _player.GetComponent(kccType) != null;

            // Check for CharacterController
            var cc = _player.GetComponent<CharacterController>();
            bool hasCC = cc != null;

            // Check for Rigidbody
            var rb = _player.GetComponent<Rigidbody>();
            bool hasRB = rb != null;

            // Check PlayerMotor type
            var motorType = FindTypeByName("PhysicsThief.Player.PlayerMotor");
            string motorInfo = "Not found";
            if (motorType != null)
            {
                var motor = _player.GetComponent(motorType);
                if (motor != null)
                {
                    // Check if it's the stub or full implementation
                    var isGroundedProp = motorType.GetProperty("IsGrounded");
                    if (isGroundedProp != null)
                    {
                        bool isGrounded = (bool)isGroundedProp.GetValue(motor);
                        motorInfo = $"PlayerMotor present (IsGrounded: {isGrounded}, likely {(hasKCC ? "KCC" : "STUB")})";
                    }
                }
            }

            Debug.Log($"[ControllerCheck] KinematicCharacterMotor: {hasKCC}");
            Debug.Log($"[ControllerCheck] CharacterController: {hasCC}");
            Debug.Log($"[ControllerCheck] Rigidbody: {hasRB}");
            Debug.Log($"[ControllerCheck] PlayerMotor: {motorInfo}");

            if (!hasKCC)
            {
                Debug.LogWarning("[ControllerCheck] KINEMATIC_CHARACTER_CONTROLLER not defined - using STUB PlayerMotor");
                Debug.LogWarning("[ControllerCheck] Real movement with Davide's controller: UNVERIFIED");
            }
            else
            {
                Debug.Log("[ControllerCheck] Full KCC PlayerMotor active");
            }

            yield return null;

            // This test always passes - it's informational
            Assert.Pass($"Player controller: {(hasKCC ? "KCC" : hasCC ? "CharacterController" : hasRB ? "Rigidbody" : "Stub/Transform only")}");
        }
    }
}
