using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
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
    [Tooltip("眼睛位置的锚点。可以拖入头部骨骼（如 mixamorig:Head），或在头部下新建一个空物体放在两眼之间。留空则使用下面的高度/前移数值。")]
    public Transform eyeAnchor;

    [Tooltip("相对锚点的偏移（人物本地坐标：X 左右、Y 上下、Z 前后）。")]
    public Vector3 eyeAnchorOffset = Vector3.zero;

    [Tooltip("没有设置 eyeAnchor 时使用：镜头离脚底的高度。运行时可以在 Inspector 里直接调。")]
    public float cameraHeight = 1.43f;

    [Tooltip("没有设置 eyeAnchor 时使用：镜头向前的偏移。")]
    public float cameraForwardOffset = 0.15f;

    [Tooltip("会挡住视线的模型（头、头发等）。这些模型只投射影子，自己看不到。")]
    public Renderer[] hideFromFirstPerson;

    [Header("Look Up / Down")]
    [Tooltip("最大抬头角度。")]
    public float maxLookUp = 85f;

    [Tooltip("最大低头角度。太大会从领口看进身体里面。")]
    public float maxLookDown = 70f;

    [Tooltip("低头到底时，眼睛向前移动的距离（模拟弯脖子），这样低头看到的是胸口和脚，而不是领口里面。")]
    public float lookDownForwardShift = 0.25f;

    [Tooltip("低头到底时，眼睛向下移动的距离。")]
    public float lookDownDropShift = 0.05f;

    private Transform cameraTransform;
    private float verticalRotation;
    private float normalCameraHeight;

    [Header("Action Lock (copy / paste)")]
    [Tooltip("为 true 时角色不能移动（播放复制/粘贴动画时由 FirstPersonHandsAction 设置）。")]
    public bool inputLocked;
    [Tooltip("为 true 时不能转视角。")]
    public bool lookLocked;

    [HideInInspector] public float pitchOverrideWeight;   // 0 = 正常视角, 1 = 使用 pitchOverride
    [HideInInspector] public float pitchOverride;
    [HideInInspector] public float actionForwardShift = 0.03f;

    private Vector2 lastMoveInput;

    /// <summary>站在地上且没有移动输入、没有滑铲。</summary>
    public bool IsIdleOnGround =>
        controller != null && controller.isGrounded && !isSliding && lastMoveInput.sqrMagnitude < 0.01f;

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

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError(
                "找不到启用的 MainCamera，请检查 ExampleCamera。",
                this
            );

            enabled = false;
            return;
        }

        cameraTransform = mainCamera.transform;

        // 镜头跟随人物移动和左右转向。
        cameraTransform.SetParent(transform, false);
        cameraTransform.localPosition =
            new Vector3(0f, cameraHeight, cameraForwardOffset);
        cameraTransform.localRotation = Quaternion.identity;

        mainCamera.nearClipPlane = 0.03f;

        normalHeight = controller.height;
        normalCameraHeight = cameraHeight;

        // 隐藏会挡住第一人称视线的模型，但保留影子。
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
        // 鼠标上下移动只旋转镜头。
        float mouseX = lookLocked ? 0f : Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = lookLocked ? 0f : Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookUp, maxLookDown);
        // 镜头的最终角度在 LateUpdate 里设置（动作时可能被临时覆盖）。

        // 鼠标左右移动旋转整个人物。
        transform.Rotate(Vector3.up * mouseX);

        groundedPlayer = controller.isGrounded;

        if (groundedPlayer && playerVelocity.y < 0f)
        {
            playerVelocity.y = 0f;
        }

        if (!inputLocked
            && currentSpeed.Equals(sprintSpeed)
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

            float moveX = inputLocked ? 0f : Input.GetAxis("Horizontal");
            float moveZ = inputLocked ? 0f : Input.GetAxis("Vertical");
            lastMoveInput = new Vector2(moveX, moveZ);

            move = transform.right * moveX + transform.forward * moveZ;
        }

        controller.Move(move * (currentSpeed * Time.deltaTime));

        if (!inputLocked
            && Input.GetButtonDown("Jump")
            && groundedPlayer
            && !isSliding)
        {
            playerVelocity.y +=
                Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    void LateUpdate()
    {
        // 最终镜头上下角度：平时用鼠标的 verticalRotation，动作时混合到 pitchOverride。
        float effectivePitch = Mathf.Lerp(verticalRotation, pitchOverride, pitchOverrideWeight);
        cameraTransform.localRotation = Quaternion.Euler(effectivePitch, 0f, 0f);

        if (eyeAnchor != null)
        {
            // 镜头跟随眼睛锚点（会跟着动画一起动，下蹲动画也会自动带低镜头）。
            Vector3 anchorLocal =
                transform.InverseTransformPoint(eyeAnchor.position);

            // 低头时像弯脖子一样，把眼睛往前、往下带。
            float lookDown01 = maxLookDown > 0f
                ? Mathf.Clamp01(effectivePitch / maxLookDown)
                : 0f;
            float bend = Mathf.Sin(lookDown01 * Mathf.PI * 0.5f);
            float forwardShift = Mathf.Lerp(lookDownForwardShift, actionForwardShift, pitchOverrideWeight);
            Vector3 neckBend = new Vector3(
                0f,
                -lookDownDropShift * bend,
                forwardShift * bend
            );

            cameraTransform.localPosition = anchorLocal + eyeAnchorOffset + neckBend;
            return;
        }

        // 下蹲和滑铲时降低镜头。
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
}