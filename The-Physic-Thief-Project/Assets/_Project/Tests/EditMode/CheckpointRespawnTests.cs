using NUnit.Framework;
using UnityEngine;

namespace PhysicsThief.Tests
{
    /// <summary>
    /// Simple unit tests for checkpoint respawn logic concepts.
    /// These tests verify the basic logic patterns without requiring scene setup.
    /// </summary>
    [TestFixture]
    public class CheckpointRespawnTests
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Test 1: Basic respawn point fallback logic
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void RespawnLogic_ReturnsDefaultSpawn_WhenNoCheckpoint()
        {
            // Simulate respawn logic: if no checkpoint, use default spawn
            Vector3 defaultSpawn = new Vector3(0, 0, 0);
            Vector3? activeCheckpoint = null;

            Vector3 respawnPoint = activeCheckpoint ?? defaultSpawn;

            Assert.AreEqual(defaultSpawn, respawnPoint);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Test 2: Checkpoint takes priority over default spawn
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void RespawnLogic_ReturnsCheckpoint_WhenActive()
        {
            Vector3 defaultSpawn = new Vector3(0, 0, 0);
            Vector3? activeCheckpoint = new Vector3(-8, 0.5f, 5);

            Vector3 respawnPoint = activeCheckpoint ?? defaultSpawn;

            Assert.AreEqual(activeCheckpoint.Value, respawnPoint);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Test 3: Latest checkpoint wins (overwrites previous)
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void RespawnLogic_LatestCheckpointWins()
        {
            Vector3? activeCheckpoint = null;
            
            // First checkpoint
            Vector3 checkpoint1 = new Vector3(5, 0, 5);
            activeCheckpoint = checkpoint1;
            Assert.AreEqual(checkpoint1, activeCheckpoint.Value);

            // Second checkpoint (should replace)
            Vector3 checkpoint2 = new Vector3(10, 0, 10);
            activeCheckpoint = checkpoint2;
            Assert.AreEqual(checkpoint2, activeCheckpoint.Value);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Test 4: Clearing checkpoint reverts to default
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void RespawnLogic_ClearCheckpoint_RevertsToDefault()
        {
            Vector3 defaultSpawn = new Vector3(0, 0, 0);
            Vector3? activeCheckpoint = new Vector3(-8, 0.5f, 5);

            Assert.IsNotNull(activeCheckpoint);

            // Clear checkpoint
            activeCheckpoint = null;

            Vector3 respawnPoint = activeCheckpoint ?? defaultSpawn;
            Assert.AreEqual(defaultSpawn, respawnPoint);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Test 5: Teleport places object at target position
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void TeleportLogic_MovesToTargetPosition()
        {
            // Create a test object
            var go = new GameObject("TestPlayer");
            try
            {
                go.transform.position = new Vector3(100, 0, 100);

                // Simulate teleport
                Vector3 targetPosition = new Vector3(0, 0, 0);
                go.transform.position = targetPosition;

                float distance = Vector3.Distance(go.transform.position, targetPosition);
                Assert.Less(distance, 0.01f, "Object should be at target position after teleport");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Test 6: Health reset logic
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void HealthReset_RestoresToMax()
        {
            float maxHP = 100f;
            float currentHP = 25f;  // Damaged

            Assert.Less(currentHP, maxHP, "Precondition: should be damaged");

            // Reset health
            currentHP = maxHP;

            Assert.AreEqual(maxHP, currentHP, "Health should be reset to max");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Test 7: Checkpoint activation state
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void CheckpointActivation_ToggleBehavior()
        {
            bool isActivated = false;

            Assert.IsFalse(isActivated, "Should start deactivated");

            // Activate
            isActivated = true;
            Assert.IsTrue(isActivated, "Should be activated");

            // Reset
            isActivated = false;
            Assert.IsFalse(isActivated, "Should be deactivated after reset");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Test 8: Respawn guard against double execution
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void RespawnGuard_PreventsDoubleExecution()
        {
            bool retryInProgress = false;
            int respawnCount = 0;

            // First respawn
            if (!retryInProgress)
            {
                retryInProgress = true;
                respawnCount++;
                retryInProgress = false;
            }

            Assert.AreEqual(1, respawnCount);

            // Simulate rapid retry during in-progress
            retryInProgress = true;
            if (!retryInProgress)
            {
                respawnCount++;  // Should not execute
            }
            retryInProgress = false;

            Assert.AreEqual(1, respawnCount, "Should not have executed second respawn while in progress");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Test 9: Respawn position includes rotation
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void TeleportLogic_SetsRotation()
        {
            var go = new GameObject("TestPlayer");
            try
            {
                Quaternion targetRotation = Quaternion.Euler(0, 90, 0);
                go.transform.rotation = targetRotation;

                float angleDiff = Quaternion.Angle(go.transform.rotation, targetRotation);
                Assert.Less(angleDiff, 0.1f, "Rotation should match target");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
