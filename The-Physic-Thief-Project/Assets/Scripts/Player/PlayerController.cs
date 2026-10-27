using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Items;

namespace PhysicsThief.Player
{
    /// <summary>
    /// Gino: movimento in prima persona, salto, attacco corpo a corpo, mangiare mele, respawn.
    /// Non gestisce scansione (PlayerScanner) ne' inventario (BriefcaseInventory).
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(HealthComponent))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Movimento")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 6f;
        [SerializeField] private float lookSensitivity = 0.1f;

        [Header("Combattimento")]
        [SerializeField] private float meleeDamage = 10f;
        [SerializeField] private float meleeRange = 2f;
        [SerializeField] private LayerMask meleeLayers;

        [Header("Riferimenti")]
        [SerializeField] private HealthComponent healthComp;
        [SerializeField] private PlayerScanner scanner;
        [SerializeField] private BriefcaseInventory briefcase;
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private Transform cameraRoot;

        public HealthComponent Health => healthComp;
        public BriefcaseInventory Briefcase => briefcase;
        public PlayerScanner Scanner => scanner;

        /// <summary>TODO: iscriviti agli eventi di input e a healthComp.OnDied.</summary>
        private void OnEnable() { }

        /// <summary>TODO: disiscriviti dagli stessi eventi.</summary>
        private void OnDisable() { }

        /// <summary>TODO: chiama HandleMovement e HandleLook solo se GameManager e' in Playing/Sandbox.</summary>
        private void Update() { }

        private void HandleMovement() { }
        private void HandleLook() { }

        public void Jump() { }

        /// <summary>TODO: raycast/overlap davanti al player e applica meleeDamage agli IDamageable.</summary>
        public void MeleeAttack() { }

        /// <summary>TODO: applica gli effetti della mela (cura, +slot, +durata) e avvia la voce TTS.</summary>
        public void EatApple(AppleItem apple) { }

        /// <summary>TODO: teletrasporta al checkpoint e ripristina la salute.</summary>
        public void Respawn(Vector3 checkpointPosition) { }

        /// <summary>TODO: disabilita input, voce TTS di morte, avvisa il GameManager.</summary>
        private void OnDeath() { }

        public void TakeDamage(float amount) { }
    }
}
