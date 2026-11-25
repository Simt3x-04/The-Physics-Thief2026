using PhysicsThief.Items;
using PhysicsThief.Core;
using PhysicsThief.UI;
using System.Collections;
using UnityEngine;

namespace PhysicsThief.Player
{
    /// <summary>
    /// Gino: first-person movement, jumping, melee attack, eating apples, respawning.
    /// Does not handle scanning (PlayerScanner) or inventory (BriefcaseInventory).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
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
        private CharacterController controller;
        private Vector3 playerVelocity;
        private bool groundedPlayer;

        [Header("Movement")]
        public float walkSpeed = 5.0f;
        public float sprintSpeed = 9.0f;
        public float slideSpeed = 12.0f;
        public float crouchSpeed = 2.5f;

        public float jumpHeight = 1.2f;
        public float gravityValue = -29.43f;
        public float mouseSensitivity = 2.0f;

        private float currentSpeed;

        [Header("First Person Camera")]
        [Tooltip("Anchor for the eye position. Drag in the head bone (e.g. mixamorig:Head), or create an empty object under the head, between the eyes. Leave empty to use the height / forward values below.")]
        public Transform eyeAnchor;

        [Tooltip("Offset from the anchor (character local space: X left/right, Y up/down, Z front/back).")]
        public Vector3 eyeAnchorOffset = Vector3.zero;

        [Tooltip("Used when eyeAnchor is not set: camera height above the feet. Can be changed in the Inspector at runtime.")]
        public float cameraHeight = 1.43f;

        [Tooltip("Used when eyeAnchor is not set: forward offset of the camera.")]
        public float cameraForwardOffset = 0.15f;

        [Tooltip("Models that block the view (head, hair...). They only cast shadows and the player does not see them.")]
        public Renderer[] hideFromFirstPerson;

        [Header("Look Up / Down")]
        [Tooltip("Maximum angle when looking up.")]
        public float maxLookUp = 85f;

        [Tooltip("Maximum angle when looking down. If too large, you see inside the body through the collar.")]
        public float maxLookDown = 70f;

        [Tooltip("How far the eyes move forward when looking all the way down (like bending the neck), so you see the chest and feet instead of the inside of the collar.")]
        public float lookDownForwardShift = 0.25f;

        [Tooltip("How far the eyes move down when looking all the way down.")]
        public float lookDownDropShift = 0.05f;

        private Transform cameraTransform;
        private float verticalRotation;
        private float normalCameraHeight;

        // False while the game is paused, the death screen is up, or a menu has focus (see SetInputEnabled).
        private bool inputEnabled = true;

        private const float GroundStickVelocity = -2f;
        private const float CoyoteTime = 0.12f;
        private float coyoteTimer;

        [Header("Crouch and Slide")]
        public float slideDuration = 0.75f;
        public float slideHeight = 1.0f;

        private bool isSliding;
        private bool isCrouching;
        private float slideTimer;
        private Vector3 slideDirection;
        private float normalHeight;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            if (healthComp == null) healthComp = GetComponent<HealthComponent>();

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError(
                    "No active camera tagged MainCamera found: check the Main Camera.",
                    this
                );

                enabled = false;
                return;
            }

            cameraTransform = mainCamera.transform;

            // The camera follows the character when it moves and turns left / right.
            cameraTransform.SetParent(transform, false);
            cameraTransform.localPosition =
                new Vector3(0f, cameraHeight, cameraForwardOffset);
            cameraTransform.localRotation = Quaternion.identity;

            mainCamera.nearClipPlane = 0.03f;

            normalHeight = controller.height;
            normalCameraHeight = cameraHeight;

            // Hide the models that block the first-person view, but keep their shadows.
            if (hideFromFirstPerson != null)
            {
                foreach (Renderer r in hideFromFirstPerson)
                {
                    if (r != null)
                    {
                        r.shadowCastingMode =
                            UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    }
                }
            }

            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            if (!inputEnabled)
            {
                // Paused / dead / in a menu: no look, no walking. Gravity still settles the body.
                SettleWithoutInput();
                return;
            }

            // Vertical mouse movement rotates only the camera.
            // UISettings.MouseSensitivity is the multiplier chosen in the Options panel (1.0 = the default feel).
            float sensitivity = mouseSensitivity * UISettings.MouseSensitivity;
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookUp, maxLookDown);

            cameraTransform.localRotation =
                Quaternion.Euler(verticalRotation, 0f, 0f);

            // Horizontal mouse movement rotates the whole character.
            transform.Rotate(Vector3.up * mouseX);

            groundedPlayer = controller.isGrounded;

            // A small constant push keeps the capsule touching the ground. With 0 the per-frame fall can be smaller
            // than CharacterController.minMoveDistance at high frame rates, and isGrounded starts flickering.
            if (groundedPlayer && playerVelocity.y < 0f)
            {
                playerVelocity.y = GroundStickVelocity;
            }

            // Short grace window: a jump still works if the ground contact is lost for a few frames (slopes, edges).
            if (groundedPlayer) coyoteTimer = CoyoteTime;
            else coyoteTimer -= Time.deltaTime;

            if (currentSpeed.Equals(sprintSpeed)
                && Input.GetKeyDown(KeyCode.C)
                && groundedPlayer
                && !isSliding)
            {
                StartCoroutine(StartSlide());
            }

            Vector3 move = Vector3.zero;

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;

                currentSpeed = Mathf.Lerp(
                    slideSpeed,
                    walkSpeed,
                    slideTimer / slideDuration
                );

                move = slideDirection;

                if (slideTimer <= 0f)
                {
                    EndSlide();
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.C))
                {
                    isCrouching = true;
                    controller.height = slideHeight;
                    currentSpeed = crouchSpeed;
                }
                else if (isCrouching)
                {
                    isCrouching = false;
                    controller.height = normalHeight;
                }

                if (!isCrouching)
                {
                    currentSpeed = Input.GetKey(KeyCode.LeftShift)
                        ? sprintSpeed
                        : walkSpeed;
                }

                float moveX = Input.GetAxis("Horizontal");
                float moveZ = Input.GetAxis("Vertical");

                move = transform.right * moveX + transform.forward * moveZ;
            }

            if (Input.GetButtonDown("Jump")
                && coyoteTimer > 0f
                && !isSliding)
            {
                // Assigned, not added: the jump height does not depend on the current fall speed.
                playerVelocity.y =
                    Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
                coyoteTimer = 0f;
            }

            playerVelocity.y += gravityValue * Time.deltaTime;

            // One single Move per frame: isGrounded reports the last call, so walking and falling go together.
            controller.Move((move * currentSpeed + playerVelocity) * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (eyeAnchor != null)
            {
                // The camera follows the eye anchor (it moves with the animation, so a crouch animation lowers the camera too).
                Vector3 anchorLocal =
                    transform.InverseTransformPoint(eyeAnchor.position);

                // When looking down, move the eyes forward and down, like bending the neck.
                float lookDown01 = maxLookDown > 0f
                    ? Mathf.Clamp01(verticalRotation / maxLookDown)
                    : 0f;
                float bend = Mathf.Sin(lookDown01 * Mathf.PI * 0.5f);
                Vector3 neckBend = new Vector3(
                    0f,
                    -lookDownDropShift * bend,
                    lookDownForwardShift * bend
                );

                cameraTransform.localPosition = anchorLocal + eyeAnchorOffset + neckBend;
                return;
            }

            // Lower the camera while crouching and sliding.
            float heightDifference = normalHeight - controller.height;
            float targetHeight = cameraHeight - heightDifference;

            cameraTransform.localPosition =
                new Vector3(0f, targetHeight, cameraForwardOffset);
        }

        private IEnumerator StartSlide()
        {
            isSliding = true;
            slideTimer = slideDuration;
            controller.height = slideHeight;

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            slideDirection =
                transform.right * moveX + transform.forward * moveZ;

            if (slideDirection == Vector3.zero)
            {
                slideDirection = transform.forward;
            }

            slideDirection.Normalize();

            yield return new WaitForSeconds(slideDuration);

            if (Input.GetKey(KeyCode.C))
            {
                isSliding = false;
                isCrouching = true;
            }
            else
            {
                EndSlide();
            }
        }

        private void EndSlide()
        {
            isSliding = false;
            isCrouching = false;
            controller.height = normalHeight;
        }

        /// <summary>TODO: Sign up for input events and healthComp.OnDied.</summary>
        private void OnEnable() { }

        /// <summary>TODO: unsubscribe from the same events.</summary>
        private void OnDisable() { }
        public void Jump() { }

        /// <summary>TODO: Perform a raycast/overlap in front of the player and apply `meleeDamage` to any `IDamageable` objects.</summary>
        public void MeleeAttack() { }

        /// <summary>TODO: Apply the apple's effects (healing, +slot, +duration) and trigger the TTS voice line.</summary>
        public void EatApple(AppleItem apple) { }

        /// <summary>Teleports Gino to a checkpoint and restores his health.</summary>
        public void Respawn(Vector3 checkpointPosition)
        {
            if (controller == null) controller = GetComponent<CharacterController>();

            // A CharacterController overwrites transform.position while it is enabled: switch it off for the teleport.
            controller.enabled = false;
            transform.position = checkpointPosition;
            controller.enabled = true;

            playerVelocity = Vector3.zero;
            if (isSliding || isCrouching) EndSlide();
            if (healthComp != null) healthComp.ResetHealth();
        }

        /// <summary>
        /// Blocks look and movement input (pause, death screen, menus). HUDTestIntegration calls it on every game-state change:
        /// the look code reads the mouse without Time.deltaTime, so without this the camera would keep turning while paused.
        /// </summary>
        public void SetInputEnabled(bool acceptInput)
        {
            inputEnabled = acceptInput;
        }

        private void SettleWithoutInput()
        {
            groundedPlayer = controller.isGrounded;
            if (groundedPlayer && playerVelocity.y < 0f) playerVelocity.y = 0f;

            playerVelocity.y += gravityValue * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }

        /// <summary>TODO: Disable input, play the death TTS voice line, and notify the `GameManager`.</summary>
        private void OnDeath() { }
        public void TakeDamage(float amount)
        {
            if (healthComp != null) healthComp.TakeDamage(amount);
        }
    }
}