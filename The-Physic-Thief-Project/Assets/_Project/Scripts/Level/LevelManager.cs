using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Player;

namespace PhysicsThief.Level
{
    /// <summary>It manages a single level scene: checkpoints, puzzle rooms, and gates. (The overall flow is handled by the GameManager.)</summary>
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
