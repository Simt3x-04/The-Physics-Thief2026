using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsThief.Player
{
    /// <summary>
    ///The single point that reads keyboard and mouse input (as per GDD 3.7 specifications) and exposes it via properties and events.
    /// Developers writing the player controller can access `MoveInput`, `LookInput`, and `JumpPressed` from here.
    /// For now, it reads directly from the devices; transitioning to an `InputActionAsset` would not affect how it is consumed.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // read the devices before any script that uses them
    public class PlayerInputHandler : MonoBehaviour
    {
        private static readonly Key[] NumberKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool ScanHeld { get; private set; }
        public bool PasteHeld { get; private set; }
        public bool ResetHeld { get; private set; }

        public event Action JumpPressed;
        public event Action AttackPressed;
        public event Action InteractPressed;
        public event Action PastePressed;
        public event Action TrashPressed;
        public event Action PausePressed;

        /// <summary>Buttons 1-5: index 0-4.</summary>
        public event Action<int> NumberPressed;

        /// <summary>Wheel: -1 (up, previous slot) o +1 (down, next slot).</summary>
        public event Action<int> ScrollStepped;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb != null)
            {
                float x = (kb[Key.D].isPressed ? 1f : 0f) - (kb[Key.A].isPressed ? 1f : 0f);
                float y = (kb[Key.W].isPressed ? 1f : 0f) - (kb[Key.S].isPressed ? 1f : 0f);
                MoveInput = new Vector2(x, y);
                ResetHeld = kb[Key.R].isPressed;

                if (kb[Key.Space].wasPressedThisFrame) JumpPressed?.Invoke();
                if (kb[Key.V].wasPressedThisFrame) AttackPressed?.Invoke();
                if (kb[Key.E].wasPressedThisFrame) InteractPressed?.Invoke();
                if (kb[Key.X].wasPressedThisFrame) TrashPressed?.Invoke();
                if (kb[Key.Escape].wasPressedThisFrame) PausePressed?.Invoke();

                for (int i = 0; i < NumberKeys.Length; i++)
                {
                    if (kb[NumberKeys[i]].wasPressedThisFrame) NumberPressed?.Invoke(i);
                }
            }

            if (mouse != null)
            {
                LookInput = mouse.delta.ReadValue();
                ScanHeld = mouse.rightButton.isPressed;
                PasteHeld = mouse.leftButton.isPressed;

                if (mouse.leftButton.wasPressedThisFrame) PastePressed?.Invoke();

                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0f) ScrollStepped?.Invoke(scroll > 0f ? -1 : 1);
            }
        }
    }
}
