using UnityEngine;
using UnityEngine.UI;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Paints a procedural forest backdrop that approximates the Eternal Garden palette
    /// using only layered solid-colour Images and a gradient sprite — no external textures.
    ///
    /// Layer stack (back to front):
    ///   0. Sky / upper atmosphere  — very dark teal  #0D2B2A
    ///   1. Mid-forest              — dark teal        #0F3833
    ///   2. Canopy glow             — mid teal radial  #1A5C52  (gradient sprite)
    ///   3. Lower clearing          — bright lime      #4CB84B  (bottom 32%)
    ///   4. Foreground vignette     — near-black sides (two vertical strips + top)
    ///   5. Character placeholder   — small warm shape at 30%, 78%
    ///
    /// When real forest artwork is available:
    ///   1. Add an Image component to the Backdrop GameObject with the art sprite.
    ///   2. Disable or delete this ForestBackdrop component.
    ///   The scene wiring, title and buttons are unaffected.
    ///
    /// This component runs in Edit Mode so the preview is live in the Scene view.
    /// </summary>
    [ExecuteAlways]
    public class ForestBackdrop : MonoBehaviour
    {
        // ── Inspector-visible palette so the team can tweak colours without code ──
        [Header("Sky / Forest")]
        [SerializeField] private Color skyDeep      = new Color(0.051f, 0.169f, 0.165f, 1f); // #0D2B2A
        [SerializeField] private Color forestMid    = new Color(0.059f, 0.220f, 0.212f, 1f); // #0F3833
        [SerializeField] private Color canopyLight  = new Color(0.102f, 0.361f, 0.322f, 1f); // #1A5C52
        [SerializeField] private Color clearingLime = new Color(0.298f, 0.722f, 0.294f, 1f); // #4CB84B
        [SerializeField] private Color vignetteCol  = new Color(0.016f, 0.055f, 0.047f, 0.88f);

        [Header("Character placeholder")]
        [SerializeField] private Color charBody     = new Color(0.839f, 0.729f, 0.561f, 1f); // warm tan
        [SerializeField] private Color charAccent   = new Color(0.698f, 0.329f, 0.133f, 1f); // brown

        // ── Lazy-init flag ─────────────────────────────────────────────────────
        private bool _built;

        private void OnEnable()  => Build();
        private void Awake()     => Build();

        private void Build()
        {
            if (_built) return;
            _built = true;

            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            // Clear any previously auto-generated children (safe re-run).
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("_Forest_"))
                    DestroyImmediate(child.gameObject);
            }

            // Helper: add a full-stretch Image layer.
            Image AddLayer(string nm, Color col, bool raycast = false)
            {
                var go = new GameObject(nm);
                go.transform.SetParent(transform, false);
                var cRT = go.AddComponent<RectTransform>();
                cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
                cRT.offsetMin = cRT.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.color         = col;
                img.raycastTarget = raycast;
                return img;
            }

            // Helper: add a partial Image layer by anchor.
            Image AddAnchoredLayer(string nm, Vector2 ancMin, Vector2 ancMax, Color col)
            {
                var go = new GameObject(nm);
                go.transform.SetParent(transform, false);
                var cRT = go.AddComponent<RectTransform>();
                cRT.anchorMin = ancMin; cRT.anchorMax = ancMax;
                cRT.offsetMin = cRT.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.color         = col;
                img.raycastTarget = false;
                return img;
            }

            // 0. Sky base.
            AddLayer("_Forest_Sky", skyDeep);

            // 1. Mid forest (adds depth).
            AddLayer("_Forest_Mid", new Color(forestMid.r, forestMid.g, forestMid.b, 0.72f));

            // 2. Canopy turquoise bloom — upper 55%, centred, partial alpha for glow feel.
            AddAnchoredLayer("_Forest_Canopy",
                new Vector2(0.15f, 0.35f), new Vector2(0.85f, 1.0f),
                new Color(canopyLight.r, canopyLight.g, canopyLight.b, 0.42f));

            // 3. Bright lime clearing — bottom 32%.
            AddAnchoredLayer("_Forest_Clearing",
                new Vector2(0f, 0f), new Vector2(1f, 0.32f),
                clearingLime);

            // 4. Vignette — left strip.
            AddAnchoredLayer("_Forest_VigLeft",
                new Vector2(0f, 0f), new Vector2(0.12f, 1f),
                vignetteCol);

            // 5. Vignette — right strip.
            AddAnchoredLayer("_Forest_VigRight",
                new Vector2(0.88f, 0f), new Vector2(1f, 1f),
                vignetteCol);

            // 6. Vignette — top bar (frames the top like dark branches).
            AddAnchoredLayer("_Forest_VigTop",
                new Vector2(0f, 0.72f), new Vector2(1f, 1f),
                new Color(vignetteCol.r, vignetteCol.g, vignetteCol.b, vignetteCol.a * 0.7f));

            // 7. Dark mid-ground shadow strip (adds forest floor depth).
            AddAnchoredLayer("_Forest_MidShadow",
                new Vector2(0f, 0.28f), new Vector2(1f, 0.42f),
                new Color(0.02f, 0.08f, 0.07f, 0.55f));

            // 8. Character placeholder — small rounded shape at 30% x, 78% y.
            {
                var charGO = new GameObject("_Forest_CharPlaceholder");
                charGO.transform.SetParent(transform, false);
                var cRT = charGO.AddComponent<RectTransform>();
                cRT.anchorMin = new Vector2(0.26f, 0.17f);
                cRT.anchorMax = new Vector2(0.34f, 0.30f);
                cRT.offsetMin = cRT.offsetMax = Vector2.zero;
                var cImg = charGO.AddComponent<Image>();
                cImg.color         = charBody;
                cImg.raycastTarget = false;

                // Small accent dot (head-ish shape).
                var headGO = new GameObject("_Forest_CharHead");
                headGO.transform.SetParent(charGO.transform, false);
                var hRT = headGO.AddComponent<RectTransform>();
                hRT.anchorMin = new Vector2(0.3f, 0.7f); hRT.anchorMax = new Vector2(0.7f, 1.0f);
                hRT.offsetMin = hRT.offsetMax = Vector2.zero;
                var hImg = headGO.AddComponent<Image>();
                hImg.color = charAccent; hImg.raycastTarget = false;
            }
        }
    }
}
