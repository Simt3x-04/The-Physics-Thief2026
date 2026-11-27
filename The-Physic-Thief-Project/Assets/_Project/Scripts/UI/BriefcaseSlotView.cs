using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>
    /// One briefcase slot in the HUD: key number, attribute name, value, attribute color, and the
    /// locked / active / paste-target states. Fields marked "Optional" can be left empty.
    /// </summary>
    public class BriefcaseSlotView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text keyLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject activeHighlight;
        [Tooltip("The green mark: this slot will be pasted on the next click.")]
        [SerializeField] private GameObject selectedMark;

        [Header("Optional polish")]
        [Tooltip("Optional: thin bar tinted with the attribute color.")]
        [SerializeField] private Image accentBar;
        [Tooltip("Optional: attribute name (MASS, VELOCITY...). If empty, a label is created automatically at the top of the slot.")]
        [SerializeField] private TMP_Text nameLabel;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.06f, 0.09f, 0.16f, 0.45f);
        [SerializeField] private Color filledColor = new Color(0.06f, 0.09f, 0.16f, 0.85f);

        [Header("Active slot")]
        [Tooltip("Opacity of the active-slot highlight (0 = invisible, 1 = solid). It is drawn BEHIND the texts and the icon.")]
        [Range(0f, 1f)]
        [SerializeField] private float activeHighlightAlpha = 0.25f;
        [SerializeField] private float activeScale = 1.12f;
        [SerializeField] private float scaleSpeed = 14f;

        [Header("Text")]
        [Tooltip("Shrinks the name and the value to fit the slot (e.g. TEMPERATURE, -125.5 C).")]
        [SerializeField] private bool autoSizeTexts = true;

        private float targetScale = 1f;

        private void Awake()
        {
            if (nameLabel == null) nameLabel = CreateNameLabel();

            if (autoSizeTexts)
            {
                EnableAutoSize(valueLabel, 10f);
                EnableAutoSize(nameLabel, 8f);
            }

            // Draw order, back to front: highlight, contents, selection mark, padlock.
            if (activeHighlight != null)
            {
                activeHighlight.transform.SetAsFirstSibling();
                Image highlightImage = activeHighlight.GetComponent<Image>();
                if (highlightImage != null)
                {
                    Color color = highlightImage.color;
                    color.a = activeHighlightAlpha;
                    highlightImage.color = color;
                }
            }
            if (selectedMark != null) selectedMark.transform.SetAsLastSibling();
            if (lockedOverlay != null) lockedOverlay.transform.SetAsLastSibling();
        }

        /// <summary>Smoothly scales the slot towards its target (unscaled time, so it also animates while paused).</summary>
        private void Update()
        {
            float current = transform.localScale.x;
            float next = Mathf.Lerp(current, targetScale, 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(next, next, 1f);
        }

        /// <summary>
        /// Empty slots are dimmed, locked slots show the padlock, the active slot is slightly enlarged and tinted.
        /// isPasteTarget shows the green mark: the slot is selected, or it is the active slot and nothing else is selected.
        /// </summary>
        public void Render(int index, PropertyValue? content, Sprite iconSprite, Color accent,
            bool slotUnlocked, bool isActive, bool isPasteTarget)
        {
            bool filled = slotUnlocked && content.HasValue;

            keyLabel.text = (index + 1).ToString();
            lockedOverlay.SetActive(!slotUnlocked);
            activeHighlight.SetActive(slotUnlocked && isActive);
            selectedMark.SetActive(slotUnlocked && isPasteTarget);

            background.color = filled ? filledColor : emptyColor;
            valueLabel.text = filled ? content.Value.GetFormattedValue() : string.Empty;

            // No sprite yet: hide the icon completely (no white square).
            bool showIcon = filled && iconSprite != null;
            icon.sprite = iconSprite;
            if (icon.gameObject != background.gameObject) icon.gameObject.SetActive(showIcon);
            else icon.enabled = showIcon;

            if (accentBar != null)
            {
                accentBar.enabled = filled;
                accentBar.color = accent;
            }
            if (nameLabel != null)
            {
                nameLabel.text = filled ? content.Value.type.ToString().ToUpperInvariant() : string.Empty;
                nameLabel.color = accent;
            }

            targetScale = slotUnlocked && isActive ? activeScale : 1f;
        }

        // ---------- helpers ----------

        /// <summary>Creates a centered title at the top of the slot (inset on the sides so it does not cover the key number).</summary>
        private TMP_Text CreateNameLabel()
        {
            var labelObject = new GameObject("NameLabel", typeof(RectTransform));
            var rect = (RectTransform)labelObject.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-28f, 24f);
            rect.anchoredPosition = new Vector2(0f, -4f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private static void EnableAutoSize(TMP_Text text, float minSize)
        {
            if (text == null) return;
            text.fontSizeMax = Mathf.Max(text.fontSize, minSize);
            text.fontSizeMin = minSize;
            text.enableAutoSizing = true;
        }
    }
}
