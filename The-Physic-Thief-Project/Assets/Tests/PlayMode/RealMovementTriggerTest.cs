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
    /// Tests checkpoint activation using simulated movement with KCC.
    /// 
    /// Since InputTestFixture has state conflicts in Play mode, this test
    /// directly manipulates the PlayerMotor's velocity to simulate walking.
    /// </summary>
    [TestFixture]
    public class RealMovementTriggerTest
    {
        private GameObject _player;
        private Transform _spawnPoint;
        private GameObject _checkpoint1GO;
        
        private MonoBehaviour _gameManager;
        private MonoBehaviour _levelManager;
        private MonoBehaviour _healthComponent;
        private MonoBehaviour _checkpoint;
        private MonoBehaviour _playerMotor;
        private MonoBehaviour _kccMotor;
        
        private Button _retryButton;
        
        private PropertyInfo _checkpointIsActivatedProperty;
        private PropertyInfo _hasActiveCheckpointProperty;
        private PropertyInfo _currentHPProperty;
        private PropertyInfo _maxHPProperty;
        private PropertyInfo _gameStateProperty;
        private MethodInfo _clearCheckpointMethod;
        private MethodInfo _resetHealthMethod;
        private MethodInfo _setStateMethod;
        private MethodInfo _takeDamageMethod;
        private System.Type _gameStateType;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var loadOp = SceneManager.LoadSceneAsync("HUDtest", LoadSceneMode.Single);
            while (!loadOp.isDone) yield return null;
            
            yield return new WaitForSeconds(0.5f);
            
            FindAllReferences();
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
            _player = GameObject.Find("Player");
            Assert.IsNotNull(_player, "Player not found");
            
            var spawnGO = GameObject.Find("SpawnPoint");
            Assert.IsNotNull(spawnGO, "SpawnPoint not found");
            _spawnPoint = spawnGO.transform;
            
            _checkpoint1GO = GameObject.Find("Checkpoint1");
            Assert.IsNotNull(_checkpoint1GO, "Checkpoint1 not found");
            
            // GameManager
            var gmType = FindType("PhysicsThief.Core.GameManager");
            _gameManager = Object.FindFirstObjectByType(gmType) as MonoBehaviour;
            _gameStateProperty = gmType.GetProperty("State");
            _setStateMethod = gmType.GetMethod("SetState");
            _gameStateType = FindType("PhysicsThief.Core.GameState");
            
            // LevelManager
            var lmType = FindType("PhysicsThief.Level.LevelManager");
            _levelManager = Object.FindFirstObjectByType(lmType) as MonoBehaviour;
            _clearCheckpointMethod = lmType.GetMethod("ClearCheckpoint");
            _hasActiveCheckpointProperty = lmType.GetProperty("HasActiveCheckpoint");
            
            // HealthComponent
            var hcType = FindType("PhysicsThief.Core.HealthComponent");
            _healthComponent = _player.GetComponent(hcType) as MonoBehaviour;
            _currentHPProperty = hcType.GetProperty("CurrentHP");
            _maxHPProperty = hcType.GetProperty("MaxHP");
            _resetHealthMethod = hcType.GetMethod("ResetHealth");
            _takeDamageMethod = hcType.GetMethod("TakeDamage");
            
            // Checkpoint
            var cpType = FindType("PhysicsThief.Level.Checkpoint");
            _checkpoint = _checkpoint1GO.GetComponent(cpType) as MonoBehaviour;
            _checkpointIsActivatedProperty = cpType.GetProperty("IsActivated");
            
            // PlayerMotor
            var pmType = FindType("PhysicsThief.Player.PlayerMotor");
            _playerMotor = _player.GetComponent(pmType) as MonoBehaviour;
            
            // KCC Motor
            var kccType = FindType("KinematicCharacterController.KinematicCharacterMotor");
            if (kccType != null)
                _kccMotor = _player.GetComponent(kccType) as MonoBehaviour;
            
            // Retry button
            var dsType = FindType("PhysicsThief.UI.DeathScreenController");
            var deathScreen = Object.FindFirstObjectByType(dsType) as MonoBehaviour;
            if (deathScreen != null)
            {
                var retryButtonField = dsType.GetField("retryButton", BindingFlags.NonPublic | BindingFlags.Instance);
                if (retryButtonField != null)
                    _retryButton = retryButtonField.GetValue(deathScreen) as Button;
            }
        }

        private void ResetSceneState()
        {
            _clearCheckpointMethod?.Invoke(_levelManager, null);
            
            var resetMethod = _checkpoint.GetType().GetMethod("ResetActivation");
            resetMethod?.Invoke(_checkpoint, null);
            
            _resetHealthMethod?.Invoke(_healthComponent, null);
            
            var playingValue = System.Enum.Parse(_gameStateType, "Playing");
            _setStateMethod?.Invoke(_gameManager, new object[] { playingValue });
            
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
        // TEST: Walk into checkpoint using KCC motor velocity
        // ═══════════════════════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator RealMovement_WalkIntoCheckpoint_TriggerFires()
        {
            Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
            Debug.Log("║  TEST: KCC Movement → Checkpoint Trigger (with polling fix)     ║");
            Debug.Log("╚════════════════════════════════════════════════════════════════╝");

            Assert.IsNotNull(_kccMotor, "KinematicCharacterMotor required");
            Debug.Log("✓ KinematicCharacterMotor found");

            // Verify checkpoint starts deactivated
            bool isActivated = (bool)_checkpointIsActivatedProperty.GetValue(_checkpoint);
            Assert.IsFalse(isActivated, "Checkpoint should start deactivated");
            Debug.Log("✓ Checkpoint1 starts deactivated");

            // Position player outside checkpoint
            Vector3 checkpointPos = _checkpoint1GO.transform.position;
            Vector3 startPos = new Vector3(checkpointPos.x - 6f, checkpointPos.y, checkpointPos.z);
            Vector3 moveDirection = (checkpointPos - startPos).normalized;
            
            // Teleport player to start position, facing toward checkpoint
            var teleportMethod = _playerMotor.GetType().GetMethod("Teleport");
            teleportMethod.Invoke(_playerMotor, new object[] { startPos, Quaternion.LookRotation(moveDirection) });
            Debug.Log($"  Player at start: {startPos}");
            Debug.Log($"  Checkpoint at: {checkpointPos}");
            Debug.Log($"  Move direction: {moveDirection}");
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            // Verify checkpoint still not activated
            isActivated = (bool)_checkpointIsActivatedProperty.GetValue(_checkpoint);
            Assert.IsFalse(isActivated, "Checkpoint should not be activated yet");
            Debug.Log("✓ Checkpoint still deactivated after teleport");

            // Get KCC motor's BaseVelocity FIELD (not property!) to set movement velocity
            var baseVelocityField = _kccMotor.GetType().GetField("BaseVelocity");
            Assert.IsNotNull(baseVelocityField, "BaseVelocity field not found on KCC motor");
            float moveSpeed = 5f; // meters per second
            
            Debug.Log("\n[WALKING] Setting KCC velocity to walk through checkpoint...");
            
            float walkTime = 0f;
            float maxWalkTime = 5f;
            bool triggerFired = false;
            
            while (walkTime < maxWalkTime && !triggerFired)
            {
                // Set velocity toward checkpoint each frame (simulates holding W)
                Vector3 velocity = moveDirection * moveSpeed;
                baseVelocityField.SetValue(_kccMotor, velocity);
                
                yield return new WaitForFixedUpdate();
                walkTime += Time.fixedDeltaTime;
                
                // Check if trigger fired
                isActivated = (bool)_checkpointIsActivatedProperty.GetValue(_checkpoint);
                if (isActivated)
                {
                    triggerFired = true;
                    Debug.Log($"✓ TRIGGER FIRED after {walkTime:F2}s of walking!");
                    Debug.Log($"  Player position: {_player.transform.position}");
                }
                
                // Log progress every 0.5 seconds
                if (walkTime % 0.5f < Time.fixedDeltaTime)
                {
                    float dist = Vector3.Distance(_player.transform.position, checkpointPos);
                    Debug.Log($"  Walking... {walkTime:F1}s, pos: {_player.transform.position}, dist: {dist:F1}m");
                }
            }
            
            // Stop movement
            baseVelocityField.SetValue(_kccMotor, Vector3.zero);
            yield return null;

            // Final verification
            isActivated = (bool)_checkpointIsActivatedProperty.GetValue(_checkpoint);
            bool hasCheckpoint = (bool)_hasActiveCheckpointProperty.GetValue(_levelManager);
            
            Debug.Log($"\n  Final state:");
            Debug.Log($"    Player position: {_player.transform.position}");
            Debug.Log($"    Checkpoint.IsActivated: {isActivated}");
            Debug.Log($"    LevelManager.HasActiveCheckpoint: {hasCheckpoint}");
            
            if (!triggerFired)
            {
                Debug.LogError("✗ TRIGGER DID NOT FIRE during KCC movement!");
                Debug.LogError("  Diagnosing trigger setup...");
                
                // Check collider setup
                var checkpointCollider = _checkpoint1GO.GetComponent<Collider>();
                Debug.Log($"  Checkpoint collider type: {checkpointCollider?.GetType().Name}");
                Debug.Log($"  Checkpoint IsTrigger: {checkpointCollider?.isTrigger}");
                Debug.Log($"  Checkpoint enabled: {checkpointCollider?.enabled}");
                Debug.Log($"  Checkpoint gameObject active: {_checkpoint1GO.activeInHierarchy}");
                Debug.Log($"  Checkpoint bounds: {checkpointCollider?.bounds}");
                
                // Check player setup
                var capsuleCollider = _player.GetComponent<CapsuleCollider>();
                Debug.Log($"  Player capsule collider: {capsuleCollider}");
                Debug.Log($"  Player layer: {LayerMask.LayerToName(_player.layer)}");
                Debug.Log($"  Player tag: {_player.tag}");
                
                // Manual overlap check
                Debug.Log("\n  Manual overlap check:");
                Collider[] overlaps = Physics.OverlapSphere(_player.transform.position, 0.5f);
                foreach (var col in overlaps)
                {
                    Debug.Log($"    Overlapping: {col.name} (trigger: {col.isTrigger})");
                }
            }
            
            Assert.IsTrue(triggerFired, "Checkpoint trigger should fire when walking through with KCC");
            Assert.IsTrue(hasCheckpoint, "LevelManager should register checkpoint");

            // ═══════════════════════════════════════════════════════════════════
            // PART 2: Die and respawn at checkpoint
            // ═══════════════════════════════════════════════════════════════════
            
            Debug.Log("\n[PART 2] Testing death and respawn...");
            
            // Move player away
            Vector3 farPos = checkpointPos + new Vector3(20f, 0f, 20f);
            teleportMethod.Invoke(_playerMotor, new object[] { farPos, Quaternion.identity });
            Debug.Log($"  Player moved to: {farPos}");
            
            yield return new WaitForFixedUpdate();
            
            // Kill player
            float maxHP = (float)_maxHPProperty.GetValue(_healthComponent);
            _takeDamageMethod.Invoke(_healthComponent, new object[] { maxHP + 50f });
            Debug.Log("  Lethal damage applied");
            
            // Wait for death screen
            float elapsed = 0f;
            while (elapsed < 1.0f)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            
            var state = _gameStateProperty.GetValue(_gameManager);
            Assert.AreEqual("GameOver", state.ToString());
            Debug.Log("✓ GameState: GameOver");
            
            // Click retry
            if (_retryButton != null)
            {
                _retryButton.onClick.Invoke();
                Debug.Log("  Retry button clicked");
            }
            
            // Wait for respawn
            elapsed = 0f;
            while (elapsed < 1.0f)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            
            // Verify respawn at checkpoint
            float distToCheckpoint = Vector3.Distance(_player.transform.position, checkpointPos);
            float currentHP = (float)_currentHPProperty.GetValue(_healthComponent);
            state = _gameStateProperty.GetValue(_gameManager);
            
            Debug.Log($"  Player respawned at: {_player.transform.position}");
            Debug.Log($"  Distance to checkpoint: {distToCheckpoint:F2}m");
            Debug.Log($"  HP: {currentHP}/{maxHP}");
            Debug.Log($"  GameState: {state}");
            
            Assert.Less(distToCheckpoint, 2f, "Should respawn at checkpoint");
            Assert.AreEqual(maxHP, currentHP, 0.1f, "HP should be restored");
            Assert.AreEqual("Playing", state.ToString());
            Assert.AreEqual(1f, Time.timeScale);
            Debug.Log("✓ Respawned at checkpoint with full HP");

            // Test movement works after respawn using KCC velocity
            Debug.Log("\n[PART 3] Testing movement after respawn...");
            Vector3 posBeforeMove = _player.transform.position;
            
            // Set velocity forward for a bit using the FIELD
            var baseVelocityField2 = _kccMotor.GetType().GetField("BaseVelocity");
            baseVelocityField2.SetValue(_kccMotor, Vector3.forward * 5f);
            yield return new WaitForSeconds(0.3f);
            baseVelocityField2.SetValue(_kccMotor, Vector3.zero);
            yield return new WaitForFixedUpdate();
            
            Vector3 posAfterMove = _player.transform.position;
            float moveDistance = Vector3.Distance(posBeforeMove, posAfterMove);
            Debug.Log($"  Movement distance: {moveDistance:F2}m");
            
            Assert.Greater(moveDistance, 0.1f, "Player should be able to move after respawn");
            Debug.Log("✓ Player can move after respawn");

            Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
            Debug.Log("║  RESULT: ALL CHECKS PASSED                                      ║");
            Debug.Log("║  ✓ KCC movement activates checkpoint trigger                    ║");
            Debug.Log("║  ✓ Death and retry flow works                                   ║");
            Debug.Log("║  ✓ Respawn at checkpoint with full HP                           ║");
            Debug.Log("║  ✓ Movement works after respawn                                 ║");
            Debug.Log("╚════════════════════════════════════════════════════════════════╝");
        }
    }
}
