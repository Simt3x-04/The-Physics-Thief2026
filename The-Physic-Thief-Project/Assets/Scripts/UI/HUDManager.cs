using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Core;
using PhysicsThief.Player;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>
    /// View dell'HUD: barra vita, 5 slot della valigetta, pannello scanner, messaggi.
    /// Si iscrive agli eventi (salute, valigetta, scanner): nessuno la chiama direttamente.
    /// Le sorgenti si assegnano nell'Inspector OPPURE da codice con Bind(...) (es. dal LevelManager).
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Fonti dati")]
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private BriefcaseInventory inventory;
        [SerializeField] private PlayerScanner scanner;

        [Header("Salute")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TMP_Text healthLabel;

        [Header("Valigetta")]
        [SerializeField] private BriefcaseSlotView[] slotViews;
        [Tooltip("Cinque icone nell'ordine: Mass, Velocity, Temperature, Magnetism, Hardness")]
        [SerializeField] private Sprite[] attributeIcons;

        [Header("Scanner")]
        [SerializeField] private GameObject scannerPanel;
        [SerializeField] private TMP_Text scannerTitle;
        [SerializeField] private TMP_Text scannerBody;

        [Header("Messaggi")]
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private float messageDuration = 2f;

        private Coroutine messageRoutine;

        private void OnEnable()
        {
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>Collega l'HUD a un player (chiamato quando il player viene creato).</summary>
        public void Bind(HealthComponent health, BriefcaseInventory briefcase, PlayerScanner playerScanner)
        {
            Unsubscribe();
            playerHealth = health;
            inventory = briefcase;
            scanner = playerScanner;

            if (isActiveAndEnabled)
            {
                Subscribe();
                RefreshAll();
            }
        }

        private void Subscribe()
        {
            if (playerHealth != null) playerHealth.OnHealthChanged += UpdateHealthBar;
            if (inventory != null) inventory.Changed += RefreshSlots;
            if (scanner != null) scanner.OnTargetChanged += HandleTargetChanged;
        }

        private void Unsubscribe()
        {
            if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealthBar;
            if (inventory != null) inventory.Changed -= RefreshSlots;
            if (scanner != null) scanner.OnTargetChanged -= HandleTargetChanged;
        }

        private void RefreshAll()
        {
            if (playerHealth != null) UpdateHealthBar(playerHealth.CurrentHP, playerHealth.MaxHP);
            RefreshSlots();
            HideScannerOverlay();
        }

        // ---------- salute ----------

        public void UpdateHealthBar(float current, float max)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
            if (healthLabel != null)
            {
                healthLabel.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
            }
        }

        // ---------- valigetta ----------

        public void RefreshSlots()
        {
            if (inventory == null) return;

            BriefcaseModel model = inventory.Model;
            for (int i = 0; i < slotViews.Length; i++)
            {
                PropertyValue? content = model.GetSlot(i);
                Sprite icon = content.HasValue ? IconFor(content.Value.type) : null;
                slotViews[i].Render(i, content, icon, model.IsSlotUnlocked(i), i == model.ActiveSlotIndex, model.IsSelected(i));
            }
        }

        private Sprite IconFor(AttributeType type)
        {
            int index = (int)type;
            return attributeIcons != null && index < attributeIcons.Length ? attributeIcons[index] : null;
        }

        // ---------- scanner ----------

        private void HandleTargetChanged(PhysicalObject target)
        {
            if (target == null) HideScannerOverlay();
            else ShowScannerOverlay(target);
        }

        /// <summary>Elenca i 5 attributi (con tasto 1-5). Se modificato mostra anche il valore originale; i bloccati in grigio.</summary>
        public void ShowScannerOverlay(PhysicalObject target)
        {
            scannerPanel.SetActive(true);
            scannerTitle.text = target.Definition != null ? target.Definition.displayName : target.name;

            var sb = new StringBuilder();
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                string line = (int)type + 1 + "  " + type + ": " + PropertyValue.Format(type, target.GetValue(type));
                if (target.HasOverride(type))
                {
                    line += "  (base " + PropertyValue.Format(type, target.GetBaseValue(type)) + ")";
                }
                if (inventory != null && !inventory.Model.IsAttributeUnlocked(type))
                {
                    line = "<color=#777777>" + line + "</color>";
                }
                sb.AppendLine(line);
            }
            scannerBody.text = sb.ToString();
        }

        public void HideScannerOverlay()
        {
            if (scannerPanel != null) scannerPanel.SetActive(false);
        }

        // ---------- messaggi ----------

        /// <summary>Messaggio breve (azioni non valide, prompt). Sparisce dopo messageDuration secondi.</summary>
        public void ShowMessage(string text)
        {
            if (messageLabel == null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(text));
        }

        /// <summary>Trasforma l'esito di una copia in un messaggio (nessun messaggio se riuscita).</summary>
        public void ShowCopyResult(CopyResult result)
        {
            string message = result switch
            {
                CopyResult.AttributeLocked => "Attribute still locked",
                CopyResult.NoFreeSlot => "No free slot - press X to trash one",
                _ => null
            };
            if (message != null) ShowMessage(message);
        }

        private IEnumerator MessageRoutine(string text)
        {
            messageLabel.text = text;
            yield return new WaitForSecondsRealtime(messageDuration);
            messageLabel.text = string.Empty;
            messageRoutine = null;
        }
    }
}
