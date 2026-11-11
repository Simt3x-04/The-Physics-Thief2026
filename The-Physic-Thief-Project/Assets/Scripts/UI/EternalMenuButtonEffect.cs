using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Main-menu button effect matching the Eternal Garden reference:
    ///
    ///   FOCUSED  — white rounded-rectangle outline with transparent fill wraps the button.
    ///              The outline Image uses a procedurally generated rounded-rect border sprite
    ///              created once at runtime (no external texture needed).
    ///   HOVERED  — same outline at reduced opacity (0.55), so mouse hover is distinct.
    ///   NORMAL   — outline hidden; label stays white.
    ///   PRESSED  — slight text scale pulse handled by the Button's normal transition.
    ///
    /// Setup (done by scene generators):
    ///   focusImage  — a child Image stretched to match the button's RectTransform.
    ///                 Its sprite is set at runtime.
    ///   label       — the TMP_Text inside the button.
    ///
    /// The Button component's own ColorBlock is overridden to Transparent so this
    /// script owns all visual feedback.
    ///
    /// Keyboard navigation: Up/Down arrow keys cycle through buttons via EventSystem.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class EternalMenuButtonEffect : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("References (set by generator or Init())")]
        [SerializeField] private Image    focusImage;
        [SerializeField] private TMP_Text label;

        // ── Rounded-rect outline sprite ───────────────────────────────────────
        // Generated once per border thickness + corner radius combination.
        private static Sprite _outlineSprite;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _hovered;
        private bool _focused;   // keyboard / EventSystem selection
        private bool _pressed;

        // ── Colours ───────────────────────────────────────────────────────────
        private static readonly Color White       = new Color(1f, 1f, 1f, 1.00f);
        private static readonly Color WhiteHover  = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color Transparent = new Color(0f, 0f, 0f, 0.00f);

        // ── Unity lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            // Neutralise the Button's built-in color block.
            var btn = GetComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor     = Color.white;
            cb.selectedColor    = Color.white;
            cb.colorMultiplier  = 1f;
            btn.colors          = cb;
            btn.transition      = Selectable.Transition.None;

            EnsureOutlineSprite();
            if (focusImage != null)
                focusImage.sprite = _outlineSprite;

            ApplyVisuals();
        }

        // ── IPointer / ISelect ─────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData _) { _hovered = true;  ApplyVisuals(); }
        public void OnPointerExit (PointerEventData _) { _hovered = false; _pressed = false; ApplyVisuals(); }
        public void OnPointerDown (PointerEventData _) { _pressed = true;  ApplyVisuals(); }
        public void OnPointerUp   (PointerEventData _) { _pressed = false; ApplyVisuals(); }
        public void OnSelect      (BaseEventData    _) { _focused = true;  ApplyVisuals(); }
        public void OnDeselect    (BaseEventData    _) { _focused = false; ApplyVisuals(); }

        // ── Visuals ────────────────────────────────────────────────────────────
        private void ApplyVisuals()
        {
            if (focusImage != null)
            {
                if (_focused || _hovered)
                    focusImage.color = _pressed ? WhiteHover : White;
                else
                    focusImage.color = Transparent;
            }

            // Label is always pure white — the outline provides the selection cue.
            if (label != null)
                label.color = White;
        }

        // ── Public API ─────────────────────────────────────────────────────────
        /// <summary>Wire up references. Called right after AddComponent by the generator.</summary>
        public void Init(Image focus, TMP_Text lbl)
        {
            focusImage = focus;
            label      = lbl;
            EnsureOutlineSprite();
            if (focusImage != null)
                focusImage.sprite = _outlineSprite;
            ApplyVisuals();
        }

        // ── Rounded-rect sprite generation ────────────────────────────────────
        /// <summary>
        /// Generates (once) a 9-slice rounded-rectangle outline sprite that matches
        /// the Eternal Garden focus box: white border, transparent interior.
        /// Uses only Texture2D — no external assets required.
        /// </summary>
        private static void EnsureOutlineSprite()
        {
            if (_outlineSprite != null) return;

            const int W      = 128;
            const int H      = 64;
            const int border = 4;    // px border thickness
            const int radius = 14;   // corner radius in px

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode   = WrapMode.Clamp;

            var pixels = new Color32[W * H];

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    // Is this pixel inside the outer rounded rect?
                    bool insideOuter = InsideRoundedRect(x, y, 0, 0, W, H, radius);
                    // Is this pixel inside the inner rounded rect (the transparent fill)?
                    bool insideInner = InsideRoundedRect(x, y, border, border, W - border, H - border,
                                                         Mathf.Max(0, radius - border));

                    if (insideOuter && !insideInner)
                        pixels[y * W + x] = new Color32(255, 255, 255, 255); // white border
                    else
                        pixels[y * W + x] = new Color32(0, 0, 0, 0);        // transparent
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            // Build a 9-slice sprite so the Image component stretches the flat sides
            // while keeping corners crisp.
            _outlineSprite = Sprite.Create(
                tex,
                new Rect(0, 0, W, H),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius + 2, radius + 2, radius + 2, radius + 2)
            );
        }

        /// <summary>Returns true when (px,py) lies inside a rounded rect defined by its corners and radius.</summary>
        private static bool InsideRoundedRect(int px, int py, int x0, int y0, int x1, int y1, int r)
        {
            if (px < x0 || px >= x1 || py < y0 || py >= y1) return false;

            // Check corners.
            int dx = 0, dy = 0;
            if      (px < x0 + r && py < y0 + r) { dx = px - (x0 + r); dy = py - (y0 + r); }
            else if (px >= x1 - r && py < y0 + r) { dx = px - (x1 - r); dy = py - (y0 + r); }
            else if (px < x0 + r && py >= y1 - r) { dx = px - (x0 + r); dy = py - (y1 - r); }
            else if (px >= x1 - r && py >= y1 - r){ dx = px - (x1 - r); dy = py - (y1 - r); }
            else return true;

            return (dx * dx + dy * dy) < (r * r);
        }
    }
}
