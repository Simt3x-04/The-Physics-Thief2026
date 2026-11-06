using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsThief.DebugTools
{
    /// <summary>
    /// TEMPORARY: Rotate the camera with the mouse to aim at test objects until the player controller is implemented.
    /// Tab locks/unlocks the cursor. REMOVE THIS once the actual controller is in place.
    /// </summary>
    public class DebugCameraLook : MonoBehaviour
    {
        [SerializeField] private float sensitivity = 0.1f;

        private float yaw;
        private float pitch;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb != null && kb[Key.Tab].wasPressedThisFrame)
            {
                bool lockIt = Cursor.lockState != CursorLockMode.Locked;
                Cursor.lockState = lockIt ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !lockIt;
            }

            if (mouse == null || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -85f, 85f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
