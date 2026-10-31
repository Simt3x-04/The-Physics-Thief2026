using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Player;

namespace PhysicsThief.Level
{
    /// <summary>Gestisce UNA scena di livello: checkpoint, stanze-puzzle e cancelli. (Il flusso globale e' del GameManager.)</summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform currentCheckpoint;
        [SerializeField] private List<Gate> gates = new List<Gate>();

        public Transform CurrentCheckpoint => currentCheckpoint;

        public void SetCheckpoint(Transform checkpoint) { }

        /// <summary>TODO: player.Respawn(currentCheckpoint.position).</summary>
        public void RespawnPlayer() { }

        public void ResetPuzzleRoom(int roomID) { }
        public void UnlockGate(int gateID) { }
    }
}
