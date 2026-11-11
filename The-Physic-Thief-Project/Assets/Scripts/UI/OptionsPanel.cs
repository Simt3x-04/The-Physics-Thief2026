using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Reusable options panel — works identically from Main Menu and Pause.
    ///
    /// Inspector slots:
    ///   volumeSlider     : Slider  (min 0, max 1)
    ///   volumeValueLabel : TMP_Text showing e.g. "80%"
    ///   sensitivitySlider: Slider  (min 0.1, max 10)
    ///   sensitivityLabel : TMP_Text showing e.g. "1.0"
    ///   backButton       : Button
    ///
    /// Usage:
    ///   Call Open(onBackCallback) to show the panel.
    ///   The back callback fires when Back is pressed, letting the caller
    ///   decide whether to return to Main Menu or to Pause.
    ///   Panel starts inactive; callers show/hide the parent GameObject.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Slider    volumeSlider;
        [SerializeField] private TMP_Text  volumeValueLabel;

        [Header("Mouse Sensitivity")]
        [SerializeField] private Slider    sensitivitySlider;
        [SerializeField] private TMP_Text  sensitivityLabel;

        [Header("Navigation")]
        [SerializeField] private Button    backButton;

        private System.Action _onBack;
        private bool          _initialising;  // guard against listener re-entry during init

        // ───────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Configure slider ranges.
            volumeSlider.minValue = UISettings.MinVolume;
            volumeSlider.maxValue = UISettings.MaxVolume;

            sensitivitySlider.minValue = UISettings.MinSensitivity;
            sensitivitySlider.maxValue = UISettings.MaxSensitivity;

            // Wire listeners.
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
            backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDestroy()
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivitySliderChanged);
            backButton.onClick.RemoveListener(OnBackClicked);
        }

        // ───────────────────────────────────────────────────────────────────────
        /// <summary>Show the panel and register the back callback.</summary>
        public void Open(System.Action onBack)
        {
            _onBack = onBack;
            gameObject.SetActive(true);
            RefreshFromSettings();
            backButton.Select();
        }

        /// <summary>Hide the panel (does NOT call the back callback).</summary>
        public void Close()
        {
            gameObject.SetActive(false);
            UISettings.Save();
        }

        // ───────────────────────────────────────────────────────────────────────
        private void RefreshFromSettings()
        {
            _initialising = true;

            volumeSlider.value      = UISettings.MasterVolume;
            sensitivitySlider.value = UISettings.MouseSensitivity;
            UpdateVolumeLabel(UISettings.MasterVolume);
            UpdateSensitivityLabel(UISettings.MouseSensitivity);

            _initialising = false;
        }

        private void OnVolumeSliderChanged(float value)
        {
            if (_initialising) return;
            UISettings.MasterVolume = value;    // applies immediately via AudioListener.volume
            UpdateVolumeLabel(value);
        }

        private void OnSensitivitySliderChanged(float value)
        {
            if (_initialising) return;
            UISettings.MouseSensitivity = value; // fires OnSensitivityChanged for PlayerController adapter
            UpdateSensitivityLabel(value);
        }

        private void OnBackClicked()
        {
            Close();
            _onBack?.Invoke();
        }

        // ───────────────────────────────────────────────────────────────────────
        private void UpdateVolumeLabel(float value)
        {
            if (volumeValueLabel != null)
                volumeValueLabel.text = Mathf.RoundToInt(value * 100) + "%";
        }

        private void UpdateSensitivityLabel(float value)
        {
            if (sensitivityLabel != null)
                sensitivityLabel.text = value.ToString("0.0");
        }
    }
}
