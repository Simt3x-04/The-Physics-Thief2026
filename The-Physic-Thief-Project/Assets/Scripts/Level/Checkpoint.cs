using UnityEngine;

namespace PhysicsThief.Level
{
    /// <summary>Trigger che registra il punto di respawn nel LevelManager.</summary>
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private LevelManager levelManager;
        private bool isActivated;

        public Transform RespawnPoint => respawnPoint;

        /// <summary>TODO: se entra il player chiama levelManager.SetCheckpoint(respawnPoint).</summary>
        private void OnTriggerEnter(Collider other) { }
    }
}
