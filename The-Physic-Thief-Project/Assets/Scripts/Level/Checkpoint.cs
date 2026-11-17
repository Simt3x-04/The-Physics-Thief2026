using UnityEngine;

namespace PhysicsThief.Level
{
    /// <summary>Trigger that records the respawn point in the LevelManager.</summary>
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private LevelManager levelManager;
        private bool isActivated;

        public Transform RespawnPoint => respawnPoint;

        /// <summary>TODO: if the player enters, call levelManager.SetCheckpoint(respawnPoint).</summary>
        private void OnTriggerEnter(Collider other) { }
    }
}
