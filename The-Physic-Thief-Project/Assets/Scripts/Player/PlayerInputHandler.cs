using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsThief.Player
{
    /// <summary>
    /// Unico punto che legge il nuovo Input System e lo traduce in eventi C#.
    /// Cosi' PlayerController e PlayerScanner non dipendono dai tasti.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }

        public event Action JumpPressed;
        public event Action AttackPressed;
        public event Action CopyPressed;
        public event Action PastePressed;
        public event Action SequencePastePressed;
        public event Action<int> SlotSelected;
        public event Action PausePressed;

        /// <summary>TODO: abilita le action e registra i callback.</summary>
        private void OnEnable() { }

        /// <summary>TODO: rimuovi i callback e disabilita le action.</summary>
        private void OnDisable() { }

        /// <summary>TODO: aggiorna MoveInput e LookInput a ogni frame.</summary>
        private void Update() { }
    }
}
