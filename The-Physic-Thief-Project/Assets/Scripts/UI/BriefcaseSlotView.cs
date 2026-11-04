using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>Un singolo slot della valigetta nell'HUD: tasto, icona, valore, bloccato/attivo/selezionato.</summary>
    public class BriefcaseSlotView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text keyLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject activeHighlight;
        [SerializeField] private GameObject selectedMark;

        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color filledColor = new Color(1f, 1f, 1f, 0.9f);

        /// <summary>Slot vuoti in grigio, bloccati con lucchetto, attivo evidenziato.</summary>
        public void Render(int index, PropertyValue? content, Sprite iconSprite, bool slotUnlocked, bool isActive, bool isSelected)
        {
            keyLabel.text = (index + 1).ToString();
            lockedOverlay.SetActive(!slotUnlocked);
            activeHighlight.SetActive(slotUnlocked && isActive);
            selectedMark.SetActive(slotUnlocked && isSelected);

            bool filled = slotUnlocked && content.HasValue;
            background.color = filled ? filledColor : emptyColor;
            icon.enabled = filled;
            icon.sprite = filled ? iconSprite : null;
            valueLabel.text = filled ? content.Value.GetFormattedValue() : string.Empty;
        }
    }
}
