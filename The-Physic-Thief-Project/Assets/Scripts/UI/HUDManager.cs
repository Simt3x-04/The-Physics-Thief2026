using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    /// HUD view: health bar, briefcase slots, scanner panel, crosshair and short messages.
    /// It subscribes to events (health, briefcase, scanner): nobody calls the HUD directly.
    /// Data sources can be assigned in the Inspector OR from code with Bind(...) (e.g. when the player spawns).
    /// Fields marked "Optional" can be left empty.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        private const float ScannerRefreshInterval = 0.1f;
        private static readonly Color LockedTextColor = new Color(0.39f, 0.45f, 0.55f, 1f);

        // One color per attribute, in AttributeType order: Mass, Velocity, Temperature, Magnetism, Hardness.
        private static readonly Color[] DefaultAttributeColors =
        {
            new Color(0.96f, 0.62f, 0.04f),   // Mass: amber
            new Color(0.13f, 0.83f, 0.93f),   // Velocity: cyan
            new Color(0.94f, 0.27f, 0.27f),   // Temperature: red
            new Color(0.65f, 0.55f, 0.98f),   // Magnetism: violet
            new Color(0.58f, 0.64f, 0.72f)    // Hardness: steel
        };

        [Header("Data sources")]
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private BriefcaseInventory inventory;
        [SerializeField] private PlayerScanner scanner;

        [Header("Health")]
        [SerializeField] private Slider healthBar;
        [Tooltip("Optional: a second slider BEHIND the main one. It drains slowly after damage (the 'ghost' bar).")]
        [SerializeField] private Slider healthTrailBar;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private Color healthHighColor = new Color(0.30f, 0.85f, 0.40f, 1f);
        [SerializeField] private Color healthMidColor = new Color(0.98f, 0.80f, 0.20f, 1f);
        [SerializeField] private Color healthLowColor = new Color(0.92f, 0.25f, 0.25f, 1f);
        [Tooltip("How fast the main bar follows the real value.")]
        [SerializeField] private float healthBarSpeed = 8f;
        [Tooltip("Seconds before the ghost bar starts draining after damage.")]
        [SerializeField] private float trailDelay = 0.5f;
        [Tooltip("Ghost bar drain speed, as a fraction of max health per second.")]
        [SerializeField] private float trailDrainSpeed = 0.35f;

        [Header("Briefcase")]
        [SerializeField] private BriefcaseSlotView[] slotViews;
        [Tooltip("Five icons in this order: Mass, Velocity, Temperature, Magnetism, Hardness. Optional.")]
        [SerializeField] private Sprite[] attributeIcons;
        [Tooltip("Five colors in the same order. If empty, a default palette is used.")]
        [SerializeField] private Color[] attributeColors;

        [Header("Scanner")]
        [SerializeField] private GameObject scannerPanel;
        [SerializeField] private TMP_Text scannerTitle;
        [SerializeField] private TMP_Text scannerBody;
        [Tooltip("Optional: tinted while an object is being scanned.")]
        [SerializeField] private Image crosshair;
        [SerializeField] private Color crosshairIdleColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color crosshairScanColor = new Color(0.13f, 0.83f, 0.93f, 1f);

        [Header("Messages")]
        [SerializeField] private TMP_Text messageLabel;
        [Tooltip("Optional: add a CanvasGroup to the message object to get a fade in/out.")]
        [SerializeField] private CanvasGroup messageGroup;
        [SerializeField] private float messageDuration = 2f;
        [SerializeField] private float messageFadeTime = 0.25f;

        private Coroutine messageRoutine;
        private PhysicalObject shownTarget;
        private Image healthFill;
        private bool healthInitialized;
        private float maxHealth = 1f;
        private float targetHealth;
        private float shownHealth;
        private float trailHealth;
        private float timeSinceDamage;
        private float scannerRefreshTimer;

        private void OnEnable()
        {
            healthFill = healthBar != null && healthBar.fillRect != null ? healthBar.fillRect.GetComponent<Image>() : null;
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>Connects the HUD to a player (call it when the player is created).</summary>
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
            if (scanner != null)
            {
                scanner.OnTargetChanged += HandleTargetChanged;
                scanner.OnMessage += ShowMessage;
            }
        }

        private void Unsubscribe()
        {
            if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealthBar;
            if (inventory != null) inventory.Changed -= RefreshSlots;
            if (scanner != null)
            {
                scanner.OnTargetChanged -= HandleTargetChanged;
                scanner.OnMessage -= ShowMessage;
            }
            if (shownTarget != null) shownTarget.OnPropertyValueChanged -= HandleTargetValueChanged;
            shownTarget = null;
        }

        private void RefreshAll()
        {
            healthInitialized = false;   // snap the bar to the current value instead of animating from zero
            if (playerHealth != null) UpdateHealthBar(playerHealth.CurrentHP, playerHealth.MaxHP);
            RefreshSlots();
            HideScannerOverlay();
            if (crosshair != null) crosshair.color = crosshairIdleColor;
        }

        private void Update()
        {
            // Unscaled time: the HUD keeps animating while the game is paused.
            float dt = Time.unscaledDeltaTime;
            AnimateHealth(dt);
            RefreshScannerTimers(dt);
        }

        // ---------- health ----------

        public void UpdateHealthBar(float current, float max)
        {
            if (!healthInitialized)
            {
                shownHealth = current;
                trailHealth = current;
                healthInitialized = true;
            }

            if (current < targetHealth) timeSinceDamage = 0f;      // took damage: restart the ghost-bar delay
            if (current > trailHealth) trailHealth = current;      // healed: the ghost bar never stays behind

            targetHealth = current;
            maxHealth = Mathf.Max(1f, max);

            healthBar.maxValue = maxHealth;
            if (healthTrailBar != null) healthTrailBar.maxValue = maxHealth;
            if (healthLabel != null) healthLabel.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
        }

        private void AnimateHealth(float dt)
        {
            if (!healthInitialized) return;

            shownHealth = Mathf.Lerp(shownHealth, targetHealth, 1f - Mathf.Exp(-healthBarSpeed * dt));
            healthBar.value = shownHealth;

            if (healthFill != null) healthFill.color = EvaluateHealthColor(shownHealth / maxHealth);

            if (healthTrailBar != null)
            {
                timeSinceDamage += dt;
                if (timeSinceDamage > trailDelay && trailHealth > shownHealth)
                {
                    trailHealth = Mathf.Max(shownHealth, trailHealth - trailDrainSpeed * maxHealth * dt);
                }
                healthTrailBar.value = trailHealth;
            }
        }

        /// <summary>Green when healthy, yellow around half, red when low.</summary>
        private Color EvaluateHealthColor(float t)
        {
            t = Mathf.Clamp(t, 0f, 1f);
            return t < 0.5f
                ? Color.Lerp(healthLowColor, healthMidColor, t * 2f)
                : Color.Lerp(healthMidColor, healthHighColor, (t - 0.5f) * 2f);
        }

        // ---------- briefcase ----------

        public void RefreshSlots()
        {
            if (inventory == null) return;

            BriefcaseModel model = inventory.Model;
            for (int i = 0; i < slotViews.Length; i++)
            {
                PropertyValue? content = model.GetSlot(i);
                AttributeType type = content.HasValue ? content.Value.type : AttributeType.Mass;
                Sprite icon = content.HasValue ? IconFor(type) : null;

                slotViews[i].Render(i, content, icon, GetAttributeColor(type),
                    model.IsSlotUnlocked(i), i == model.ActiveSlotIndex, model.IsSelected(i));
            }
        }

        private Sprite IconFor(AttributeType type)
        {
            int index = (int)type;
            return attributeIcons != null && index < attributeIcons.Length ? attributeIcons[index] : null;
        }

        /// <summary>Color of an attribute (Inspector palette if complete, otherwise the default one).</summary>
        public Color GetAttributeColor(AttributeType type)
        {
            int index = (int)type;
            return attributeColors != null && index < attributeColors.Length ? attributeColors[index] : DefaultAttributeColors[index];
        }

        // ---------- scanner ----------

        private void HandleTargetChanged(PhysicalObject target)
        {
            if (shownTarget != null) shownTarget.OnPropertyValueChanged -= HandleTargetValueChanged;
            shownTarget = target;

            if (crosshair != null) crosshair.color = target != null ? crosshairScanColor : crosshairIdleColor;

            if (target == null)
            {
                HideScannerOverlay();
                return;
            }
            target.OnPropertyValueChanged += HandleTargetValueChanged;
            ShowScannerOverlay(target);
        }

        /// <summary>A value of the scanned object changed (pasted or expired): update the panel.</summary>
        private void HandleTargetValueChanged(AttributeType type, float value)
        {
            if (shownTarget != null) ShowScannerOverlay(shownTarget);
        }

        /// <summary>While a pasted property is active, refresh the panel a few times per second so its countdown is live.</summary>
        private void RefreshScannerTimers(float dt)
        {
            if (shownTarget == null) return;

            scannerRefreshTimer -= dt;
            if (scannerRefreshTimer > 0f) return;
            scannerRefreshTimer = ScannerRefreshInterval;

            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                if (shownTarget.HasOverride(type))
                {
                    ShowScannerOverlay(shownTarget);
                    return;
                }
            }
        }

        /// <summary>
        /// Lists the five attributes (key number, name, value). A temporarily modified attribute is highlighted
        /// and shows its original value and remaining seconds; locked attributes are greyed out.
        /// </summary>
        public void ShowScannerOverlay(PhysicalObject target)
        {
            scannerPanel.SetActive(true);
            scannerTitle.text = target.DisplayName;

            var sb = new StringBuilder();
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                bool locked = inventory != null && !inventory.Model.IsAttributeUnlocked(type);
                Color labelColor = locked ? LockedTextColor : GetAttributeColor(type);
                string value = PropertyValue.Format(type, target.GetValue(type));

                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(labelColor)).Append('>')
                  .Append((int)type + 1).Append("  ").Append(type.ToString().ToUpperInvariant()).Append("</color>");
                sb.Append("<pos=42%>");

                if (target.HasOverride(type))
                {
                    string seconds = target.GetRemainingTime(type).ToString("0.0", CultureInfo.InvariantCulture);
                    sb.Append("<color=#FACC15>").Append(value).Append("</color>")
                      .Append("  <color=#94A3B8>(").Append(PropertyValue.Format(type, target.GetBaseValue(type)))
                      .Append(")  ").Append(seconds).Append("s</color>");
                }
                else if (locked)
                {
                    sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(LockedTextColor)).Append('>').Append(value).Append("</color>");
                }
                else
                {
                    sb.Append(value);
                }
                sb.AppendLine();
            }
            scannerBody.text = sb.ToString();
        }

        public void HideScannerOverlay()
        {
            if (scannerPanel != null) scannerPanel.SetActive(false);
        }

        // ---------- messages ----------

        /// <summary>Short message (invalid actions, prompts). Fades out after messageDuration seconds.</summary>
        public void ShowMessage(string text)
        {
            if (messageLabel == null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(text));
        }

        /// <summary>Turns the result of a copy attempt into a message (nothing if it succeeded).</summary>
        public void ShowCopyResult(CopyResult result)
        {
            string message = result.ToMessage();
            if (message != null) ShowMessage(message);
        }

        private IEnumerator MessageRoutine(string text)
        {
            messageLabel.text = text;
            yield return FadeMessage(0f, 1f);
            yield return new WaitForSecondsRealtime(messageDuration);
            yield return FadeMessage(1f, 0f);
            messageLabel.text = string.Empty;
            messageRoutine = null;
        }

        private IEnumerator FadeMessage(float from, float to)
        {
            if (messageGroup == null) yield break;

            for (float t = 0f; t < messageFadeTime; t += Time.unscaledDeltaTime)
            {
                messageGroup.alpha = Mathf.Lerp(from, to, t / messageFadeTime);
                yield return null;
            }
            messageGroup.alpha = to;
        }
    }
}
