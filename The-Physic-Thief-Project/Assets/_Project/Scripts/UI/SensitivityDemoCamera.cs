using UnityEngine;
using PhysicsThief.UI;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Minimal camera-look demonstration that responds to UISettings.MouseSensitivity.
    /// Attach to the Main Camera in the UIFlowTest scene to show that the sensitivity
    /// slider has a visible, immediate effect.
    ///
    /// This is intentionally a DEMO STUB. The real PlayerController.HandleLook() will
    /// replace this once the movement teammate wires the adapter contract below.
    ///
    /// ─── INTEGRATION CONTRACT FOR MOVEMENT TEAMMATE ───────────────────────────────
    /// When real camera-look code is active, remove this component and instead subscribe
    /// to UISettings.OnSensitivityChanged inside PlayerController:
    ///
    ///   void OnEnable()  { UISettings.OnSensitivityChanged += ApplySensitivity;
    ///                      ApplySensitivity(UISettings.MouseSensitivity); }
    ///   void OnDisable() { UISettings.OnSensitivityChanged -= ApplySensitivity; }
    ///   void ApplySensitivity(float s) { lookSensitivity = s; }
    ///
    /// Do NOT multiply the value again — UISettings.MouseSensitivity is already the
    /// final multiplier in degrees-per-pixel (range 0.1 .. 10).
    /// ───────────────────────────────────────────────────────────────────────────────
    ///
    /// While GameState == Playing this component rotates the camera with mouse delta.
    /// It is automatically disabled in menus (cursor visible, time frozen).
    /// </summary>
    [AddComponentMenu("Physics Thief/UI/Sensitivity Demo Camera (TEST ONLY)")]
    public class SensitivityDemoCamera : MonoBehaviour
    {
        [Tooltip("Invert vertical look. Real PlayerController will have its own setting.")]
        [SerializeField] private bool invertY = false;

        private float _pitch; // accumulated vertical rotation
        private float _yaw;   // accumulated horizontal rotation

        // ─────────────────────────────────────────────────────────────────────
        private void Start()
        {
            // Initialise from current world rotation so there is no snap on enable.
            Vector3 euler = transform.eulerAngles;
            _yaw   = euler.y;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x; // unwrap to -180..180
        }

        private void Update()
        {
            // Only rotate when in Playing state and the cursor is locked.
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (Time.timeScale == 0f) return;                     // paused / frozen

            float sens  = UISettings.MouseSensitivity;            // live value, no extra multiply
            float mouseX = Input.GetAxisRaw("Mouse X") * sens;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sens;

            _yaw   += mouseX;
            _pitch -= invertY ? -mouseY : mouseY;
            _pitch  = Mathf.Clamp(_pitch, -89f, 89f);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
