using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Core;
using PhysicsThief.Player;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>
    /// View: si iscrive agli eventi di salute, valigetta e scanner. Nessuno chiama l'HUD direttamente.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Fonti dati")]
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private BriefcaseInventory inventory;
        [SerializeField] private PlayerScanner scanner;

        [Header("Elementi UI")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Image[] slotIcons;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private GameObject scannerOverlay;
        [SerializeField] private TMP_Text scannerTitle;
        [SerializeField] private TMP_Text scannerProperties;
        [SerializeField] private GameObject expiryWarning;

        /// <summary>TODO: iscriviti a playerHealth.OnHealthChanged, inventory.OnSlotsChanged, scanner.OnTargetChanged.</summary>
        private void OnEnable() { }

        private void OnDisable() { }

        public void UpdateHealthBar(float current, float max) { }
        public void UpdateInventorySlots(IReadOnlyList<PropertyValue> slots) { }
        public void ShowScannerOverlay(PhysicalObject target) { }
        public void HideScannerOverlay() { }
        public void DisplayExpiryWarning(AttributeType type) { }
    }
}
