using UnityEngine;
using KinematicCharacterController;

namespace PhysicsThief.Player
{
    /// <summary>
    /// First-person movement built on Kinematic Character Controller (KCC).
    /// The KCC motor "asks" this class which velocity and rotation the character should have
    /// (ICharacterController callbacks), so this class never moves the transform directly.
    /// It only reads PlayerInputHandler: it knows nothing about scanning, health or the HUD.
    /// </summary>
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class PlayerMotor : MonoBehaviour, ICharacterController
    {
        [Header("References")]
        [SerializeField] private KinematicCharacterMotor motor;
        [SerializeField] private PlayerInputHandler input;
        [Tooltip("Child object at eye height that holds the camera. It receives the vertical look (pitch).")]
        [SerializeField] private Transform cameraPivot;

        [Header("Ground movement")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("Higher = snappier acceleration and braking on the ground.")]
        [SerializeField] private float groundSharpness = 15f;

        [Header("Air movement")]
        [Tooltip("Steering strength while airborne, in m/s^2.")]
        [SerializeField] private float airAcceleration = 12f;
        [SerializeField] private float airDrag = 0.1f;
        [SerializeField] private Vector3 gravity = new Vector3(0f, -25f, 0f);

        [Header("Jump (GDD 3.7: small jump with limited reach)")]
        [SerializeField] private float jumpHeight = 0.9f;
        [Tooltip("A jump pressed slightly before landing still counts.")]
        [SerializeField] private float jumpBufferTime = 0.1f;
        [Tooltip("A jump is still allowed for a moment after walking off a ledge.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Header("Look")]
        [Tooltip("Degrees per mouse pixel.")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float maxPitch = 85f;
        [Tooltip("Locks the cursor on Start. Keep it on in test scenes; in the real game GameManager.SetState handles the cursor.")]
        [SerializeField] private bool lockCursorOnStart = true;

        private float yaw;
        private float pitch;
        private Vector2 moveInput;
        private bool jumpRequested;
        private bool inputEnabled = true;
        private float timeSinceJumpRequested = Mathf.Infinity;
        private float timeSinceLastGrounded = Mathf.Infinity;

        public bool IsGrounded => motor.GroundingStatus.IsStableOnGround;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<KinematicCharacterMotor>();
            if (input == null) input = GetComponent<PlayerInputHandler>();

            // Tell the motor to call our ICharacterController methods.
            motor.CharacterController = this;
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnEnable()
        {
            input.JumpPressed += HandleJumpPressed;
        }

        private void OnDisable()
        {
            input.JumpPressed -= HandleJumpPressed;
        }

        /// <summary>Reads input every frame. The actual movement happens later, in the KCC callbacks.</summary>
        private void Update()
        {
            if (!inputEnabled)
            {
                moveInput = Vector2.zero;
                return;
            }

            moveInput = input.MoveInput;

            // Only look around while the cursor is locked (not in menus or pause).
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = input.LookInput;
                yaw += look.x * lookSensitivity;
                pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, -maxPitch, maxPitch);
            }

            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleJumpPressed()
        {
            if (!inputEnabled) return;
            jumpRequested = true;
            timeSinceJumpRequested = 0f;
        }

        // ---------- public API ----------

        /// <summary>Moves the player instantly (respawn at a checkpoint) and clears its velocity.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            motor.SetPositionAndRotation(position, rotation);
            motor.BaseVelocity = Vector3.zero;
            yaw = rotation.eulerAngles.y;
            pitch = 0f;
        }

        /// <summary>Disables movement and look (pause, death screen, cutscenes).</summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled) jumpRequested = false;
        }

        // ---------- ICharacterController (called by the KCC motor) ----------

        /// <summary>"Which rotation should I have?" Face the look direction (yaw only).</summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>"Which velocity should I have?" Ground/air movement, gravity and jumping.</summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 up = motor.CharacterUp;
            Vector3 wishDirection = Vector3.ClampMagnitude(
                motor.CharacterRight * moveInput.x + motor.CharacterForward * moveInput.y, 1f);

            if (motor.GroundingStatus.IsStableOnGround)
            {
                // Keep the velocity parallel to the slope, then blend towards the target velocity.
                Vector3 groundNormal = motor.GroundingStatus.GroundNormal;
                currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, groundNormal) * currentVelocity.magnitude;

                Vector3 inputRight = Vector3.Cross(wishDirection, up);
                Vector3 reorientedInput = Vector3.Cross(groundNormal, inputRight).normalized * wishDirection.magnitude;
                Vector3 targetVelocity = reorientedInput * moveSpeed;

                currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-groundSharpness * deltaTime));
            }
            else
            {
                // Airborne: limited steering on the horizontal plane, plus gravity.
                Vector3 vertical = Vector3.Project(currentVelocity, up);
                Vector3 horizontal = currentVelocity - vertical;

                float speedCap = Mathf.Max(moveSpeed, horizontal.magnitude);   // never speed the player up beyond what it already had
                horizontal += wishDirection * airAcceleration * deltaTime;
                horizontal = Vector3.ClampMagnitude(horizontal, speedCap);

                vertical += gravity * deltaTime;

                currentVelocity = horizontal + vertical;
                currentVelocity *= 1f / (1f + airDrag * deltaTime);
            }

            // Jump: buffered input + coyote time. Replaces the vertical velocity.
            if (jumpRequested && timeSinceJumpRequested <= jumpBufferTime && timeSinceLastGrounded <= coyoteTime)
            {
                float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
                motor.ForceUnground();
                currentVelocity += up * jumpSpeed - Vector3.Project(currentVelocity, up);
                jumpRequested = false;
                timeSinceLastGrounded = Mathf.Infinity;   // no double jump through coyote time
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            timeSinceJumpRequested += deltaTime;
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround) timeSinceLastGrounded = 0f;
            else timeSinceLastGrounded += deltaTime;
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // Forget a buffered jump that was never used.
            if (jumpRequested && timeSinceJumpRequested > jumpBufferTime) jumpRequested = false;
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;   // collide with everything; later: ignore the player's own colliders or triggers if needed
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    }
}
