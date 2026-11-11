using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Items;

namespace PhysicsThief.Player
{
    /// <summary>
    /// Gino: first-person movement, jumping, melee attack, eating apples, respawning.
    /// Does not handle scanning (PlayerScanner) or inventory (BriefcaseInventory).
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(HealthComponent))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 6f;
        [SerializeField] private float lookSensitivity = 0.1f;

        [Header("Fight")]
        [SerializeField] private float meleeDamage = 10f;
        [SerializeField] private float meleeRange = 2f;
        [SerializeField] private LayerMask meleeLayers;

        [Header("Sources")]
        [SerializeField] private HealthComponent healthComp;
        [SerializeField] private PlayerScanner scanner;
        [SerializeField] private BriefcaseInventory briefcase;
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private Transform cameraRoot;

        public HealthComponent Health => healthComp;
        public BriefcaseInventory Briefcase => briefcase;
        public PlayerScanner Scanner => scanner;

        /// <summary>TODO: Sign up for input events and healthComp.OnDied.</summary>
        private void OnEnable() { }

        /// <summary>TODO: unsubscribe from the same events.</summary>
        private void OnDisable() { }

        private void Update() { }

        public void Jump() { }

        /// <summary>TODO: Perform a raycast/overlap in front of the player and apply `meleeDamage` to any `IDamageable` objects.</summary>
        public void MeleeAttack() { }

        /// <summary>TODO: Apply the apple's effects (healing, +slot, +duration) and trigger the TTS voice line.</summary>
        public void EatApple(AppleItem apple) { }

        /// <summary>TODO: Teleport to the checkpoint and restore health.</summary>
        public void Respawn(Vector3 checkpointPosition) { }

        /// <summary>TODO: Disable input, play the death TTS voice line, and notify the `GameManager`.</summary>
        private void OnDeath() { }

        public void TakeDamage(float amount) { }
    }
}
