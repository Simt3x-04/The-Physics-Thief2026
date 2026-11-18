// ──────────────────────────────────────────────────────────────────────────────
// PlayerMotor.cs
//
// Full implementation requires the Kinematic Character Controller (KCC) package.
// When KCC is installed, define KINEMATIC_CHARACTER_CONTROLLER in
//   Edit → Project Settings → Player → Other Settings → Scripting Define Symbols.
//
// Without the define, a lightweight stub is compiled so that:
//   • HUDTestIntegration.SetInputEnabled() compiles and runs.
//   • The scene references to PlayerMotor are preserved.
//   • Movement is handled by whatever other component is on the Player GO.
//
// DAVIDE: once the KCC package is in Packages/manifest.json, add
//         KINEMATIC_CHARACTER_CONTROLLER to the scripting define symbols
//         to unlock the full implementation below.
// ──────────────────────────────────────────────────────────────────────────────
using UnityEngine;

#if KINEMATIC_CHARACTER_CONTROLLER
using KinematicCharacterController;
#endif

namespace PhysicsThief.Player
{

#if KINEMATIC_CHARACTER_CONTROLLER

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

        private void OnEnable()  { input.JumpPressed += HandleJumpPressed; }
        private void OnDisable() { input.JumpPressed -= HandleJumpPressed; }

        private void Update()
        {
            if (!inputEnabled) { moveInput = Vector2.zero; return; }
            moveInput = input.MoveInput;
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

        // ── Public API ──────────────────────────────────────────────────────
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            motor.SetPositionAndRotation(position, rotation);
            motor.BaseVelocity = Vector3.zero;
            yaw = rotation.eulerAngles.y;
            pitch = 0f;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled) jumpRequested = false;
        }

        // ── ICharacterController ────────────────────────────────────────────
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 up = motor.CharacterUp;
            Vector3 wishDirection = Vector3.ClampMagnitude(
                motor.CharacterRight * moveInput.x + motor.CharacterForward * moveInput.y, 1f);

            if (motor.GroundingStatus.IsStableOnGround)
            {
                Vector3 groundNormal = motor.GroundingStatus.GroundNormal;
                currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, groundNormal) * currentVelocity.magnitude;
                Vector3 inputRight = Vector3.Cross(wishDirection, up);
                Vector3 reorientedInput = Vector3.Cross(groundNormal, inputRight).normalized * wishDirection.magnitude;
                currentVelocity = Vector3.Lerp(currentVelocity, reorientedInput * moveSpeed, 1f - Mathf.Exp(-groundSharpness * deltaTime));
            }
            else
            {
                Vector3 vertical = Vector3.Project(currentVelocity, up);
                Vector3 horizontal = currentVelocity - vertical;
                float speedCap = Mathf.Max(moveSpeed, horizontal.magnitude);
                horizontal += wishDirection * airAcceleration * deltaTime;
                horizontal = Vector3.ClampMagnitude(horizontal, speedCap);
                vertical += gravity * deltaTime;
                currentVelocity = (horizontal + vertical) / (1f + airDrag * deltaTime);
            }

            if (jumpRequested && timeSinceJumpRequested <= jumpBufferTime && timeSinceLastGrounded <= coyoteTime)
            {
                float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
                motor.ForceUnground();
                currentVelocity += up * jumpSpeed - Vector3.Project(currentVelocity, up);
                jumpRequested = false;
                timeSinceLastGrounded = Mathf.Infinity;
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)   { timeSinceJumpRequested += deltaTime; }

        public void PostGroundingUpdate(float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround) timeSinceLastGrounded = 0f;
            else timeSinceLastGrounded += deltaTime;
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            if (jumpRequested && timeSinceJumpRequested > jumpBufferTime) jumpRequested = false;
        }

        public bool IsColliderValidForCollisions(Collider coll) => true;

        public void OnGroundHit(Collider h, Vector3 n, Vector3 p, ref HitStabilityReport r) { }
        public void OnMovementHit(Collider h, Vector3 n, Vector3 p, ref HitStabilityReport r) { }
        public void ProcessHitStabilityReport(Collider h, Vector3 n, Vector3 p,
            Vector3 cp, Quaternion cr, ref HitStabilityReport r) { }
        public void OnDiscreteCollisionDetected(Collider h) { }
    }

#else

    /// <summary>
    /// Stub compiled when the Kinematic Character Controller package is not installed.
    /// Preserves the public API so HUDTestIntegration and scene references compile.
    ///
    /// DAVIDE: install KCC via Package Manager, then add
    ///   KINEMATIC_CHARACTER_CONTROLLER
    /// to Edit → Project Settings → Player → Scripting Define Symbols.
    /// </summary>
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Look (stub — no KCC movement)")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private bool  lockCursorOnStart = true;
        [SerializeField] private Transform cameraPivot;

        private bool  _inputEnabled = true;
        private float _yaw;
        private float _pitch;
        private const float MaxPitch = 85f;

        public bool IsGrounded => false;   // stub

        private void Start()
        {
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        private void Update()
        {
            // Minimal mouse-look so the camera responds in Play Mode even without KCC.
            if (!_inputEnabled) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            _yaw   += Input.GetAxisRaw("Mouse X") * lookSensitivity;
            _pitch  = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * lookSensitivity, -MaxPitch, MaxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        /// <summary>Disables movement and look input (pause, death screen, cutscenes).</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        /// <summary>Teleports the player (stub — just sets position/rotation).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _yaw   = rotation.eulerAngles.y;
            _pitch = 0f;
        }
    }

#endif

}
