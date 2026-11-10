using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>
    /// One briefcase slot in the HUD: key number, value, attribute color, and locked / active / selected states.
    /// Fields marked "Optional" can be left empty.
    /// </summary>
    public class BriefcaseSlotView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text keyLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject activeHighlight;
        [SerializeField] private GameObject selectedMark;

        [Header("Optional polish")]
        [Tooltip("Optional: thin bar tinted with the attribute color.")]
        [SerializeField] private Image accentBar;
        [Tooltip("Optional: attribute name (MASS, VELOCITY...) tinted with the attribute color. Useful while there are no icons.")]
        [SerializeField] private TMP_Text nameLabel;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.06f, 0.09f, 0.16f, 0.45f);
        [SerializeField] private Color filledColor = new Color(0.06f, 0.09f, 0.16f, 0.85f);

        [Header("Animation")]
        [SerializeField] private float activeScale = 1.12f;
        [SerializeField] private float scaleSpeed = 14f;

        private float targetScale = 1f;

        /// <summary>Smoothly scales the slot towards its target (uses unscaled time so it also animates while paused).</summary>
        private void Update()
        {
            float current = transform.localScale.x;
            float next = Mathf.Lerp(current, targetScale, 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(next, next, 1f);
        }

        /// <summary>Empty slots are dimmed, locked slots show the padlock, the active slot is highlighted and enlarged.</summary>
        public void Render(int index, PropertyValue? content, Sprite iconSprite, Color accent,
            bool slotUnlocked, bool isActive, bool isSelected)
        {
            bool filled = slotUnlocked && content.HasValue;

            keyLabel.text = (index + 1).ToString();
            lockedOverlay.SetActive(!slotUnlocked);
            activeHighlight.SetActive(slotUnlocked && isActive);
            selectedMark.SetActive(slotUnlocked && isSelected);

            background.color = filled ? filledColor : emptyColor;
            valueLabel.text = filled ? content.Value.GetFormattedValue() : string.Empty;

            // Without a sprite the icon stays hidden (no white square).
            icon.sprite = iconSprite;
            icon.enabled = filled && iconSprite != null;

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
    }
}
