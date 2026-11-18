using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Attach to any menu button (Main Menu, Pause, Options Back) to get:
    ///
    ///   • A 5 px amber bar on the left edge that is visible only when the button is
    ///     focused (keyboard/gamepad) or hovered by the mouse — matches the Zelda /
    ///     From Dust selection-indicator style.
    ///   • Text colour transitions:
    ///       Normal   → warm off-white (TextPrimary)
    ///       Hover    → full amber (AccentAmber)
    ///       Pressed  → PressedTint
    ///       Selected → AccentAmber  (keyboard focus)
    ///
    /// Setup (done by UIFlowTestSceneSetup, not manually):
    ///   markerImage — a child Image stretched to the full button height, anchored left,
    ///                 exactly MenuStyle.MarkerWidth wide.
    ///   label       — the TMP_Text inside the button.
    ///
    /// The Button component's own ColorBlock is overridden to Transparent so this script
    /// owns all visual feedback and there is no double-tinting.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButtonEffect : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("References (set by generator)")]
        [SerializeField] private Image    markerImage;
        [SerializeField] private TMP_Text label;

        // ── state ─────────────────────────────────────────────────────────
        private bool _hovered;
        private bool _pressed;
        private bool _selected;

        // ── unity ─────────────────────────────────────────────────────────
        private void Awake()
        {
            // Neutralise the Button's built-in color block so it doesn't fight us.
            var btn = GetComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor     = Color.white;
            cb.selectedColor    = Color.white;
            cb.disabledColor    = new Color(1, 1, 1, 0.5f);
            cb.colorMultiplier  = 1f;
            btn.colors          = cb;
            btn.transition      = Selectable.Transition.None;   // we handle visuals ourselves

            ApplyVisuals();
        }

        // ── IPointer / ISelect ─────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData _) { _hovered = true;  ApplyVisuals(); }
        public void OnPointerExit (PointerEventData _) { _hovered = false; _pressed = false; ApplyVisuals(); }
        public void OnPointerDown (PointerEventData _) { _pressed = true;  ApplyVisuals(); }
        public void OnPointerUp   (PointerEventData _) { _pressed = false; ApplyVisuals(); }
        public void OnSelect      (BaseEventData    _) { _selected = true;  ApplyVisuals(); }
        public void OnDeselect    (BaseEventData    _) { _selected = false; ApplyVisuals(); }

        // ── visuals ────────────────────────────────────────────────────────
        private void ApplyVisuals()
        {
            bool active = _hovered || _selected;

            // Marker bar
            if (markerImage != null)
                markerImage.color = active
                    ? (_pressed ? MenuStyle.PressedTint : MenuStyle.AccentAmber)
                    : MenuStyle.Transparent;

            // Label colour
            if (label != null)
            {
                if (_pressed)
                    label.color = MenuStyle.PressedTint;
                else if (active)
                    label.color = MenuStyle.AccentAmber;
                else
                    label.color = MenuStyle.TextPrimary;
            }
        }

        // ── public API used by generator ───────────────────────────────────
        /// <summary>Wire up the two visual references. Called right after AddComponent in the generator.</summary>
        public void Init(Image marker, TMP_Text lbl)
        {
            markerImage = marker;
            label       = lbl;
            ApplyVisuals();
        }
    }
}
