using System;
using UnityEngine;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Shared settings store: master volume and mouse-sensitivity.
    /// Persisted via PlayerPrefs; apply immediately on change.
    ///
    /// Defaults:
    ///   MasterVolume  : 0.8  (range 0..1)
    ///   MouseSensitivity: 1.0 (range 0.1..10)
    ///
    /// ADAPTER CONTRACT FOR PLAYERCONTROLLER OWNER
    /// -------------------------------------------
    /// PlayerController.lookSensitivity is currently a private serialized float stub.
    /// When that field is wired to real camera-look code, connect it like this:
    ///
    ///   void Start() { UISettings.OnSensitivityChanged += ApplySensitivity; }
    ///   void OnDestroy() { UISettings.OnSensitivityChanged -= ApplySensitivity; }
    ///   void ApplySensitivity(float s) { lookSensitivity = s; }
    ///
    /// Do NOT multiply the value again: UISettings.MouseSensitivity is already the
    /// final multiplier and matches the range the slider shows the player.
    /// </summary>
    public static class UISettings
    {
        // ---------- PlayerPrefs keys ----------
        private const string KeyVolume      = "MasterVolume";
        private const string KeySensitivity = "MouseSensitivity";

        // ---------- defaults / ranges ----------
        public const float DefaultVolume      = 0.8f;
        public const float MinVolume          = 0f;
        public const float MaxVolume          = 1f;

        public const float DefaultSensitivity = 1.0f;
        public const float MinSensitivity     = 0.1f;
        public const float MaxSensitivity     = 10f;

        // ---------- events ----------
        /// <summary>Fired whenever master volume changes; value is 0..1.</summary>
        public static event Action<float> OnVolumeChanged;

        /// <summary>Fired whenever mouse sensitivity changes; value is 0.1..10.</summary>
        public static event Action<float> OnSensitivityChanged;

        // ---------- properties (lazy-load from PlayerPrefs) ----------
        private static float? _volume;
        private static float? _sensitivity;

        public static float MasterVolume
        {
            get
            {
                if (_volume == null)
                    _volume = Mathf.Clamp(PlayerPrefs.GetFloat(KeyVolume, DefaultVolume), MinVolume, MaxVolume);
                return _volume.Value;
            }
            set
            {
                float clamped = Mathf.Clamp(value, MinVolume, MaxVolume);
                _volume = clamped;
                PlayerPrefs.SetFloat(KeyVolume, clamped);
                OnVolumeChanged?.Invoke(clamped);
                ApplyVolume(clamped);
            }
        }

        public static float MouseSensitivity
        {
            get
            {
                if (_sensitivity == null)
                    _sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeySensitivity, DefaultSensitivity), MinSensitivity, MaxSensitivity);
                return _sensitivity.Value;
            }
            set
            {
                float clamped = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
                _sensitivity = clamped;
                PlayerPrefs.SetFloat(KeySensitivity, clamped);
                OnSensitivityChanged?.Invoke(clamped);
            }
        }

        /// <summary>Call once at startup to apply persisted preferences.</summary>
        public static void Apply()
        {
            _volume      = null;  // force re-read from PlayerPrefs
            _sensitivity = null;
            ApplyVolume(MasterVolume);
            OnSensitivityChanged?.Invoke(MouseSensitivity);
        }

        /// <summary>Persist all currently cached values (called automatically on set, but explicit save on quit is good practice).</summary>
        public static void Save() => PlayerPrefs.Save();

        // ---------- private ----------
        private static void ApplyVolume(float linear)
        {
            // AudioListener.volume is the simplest master-volume with no AudioMixer dependency.
            // If AudioManager.masterMixer is wired in the future, convert linear -> dB there instead.
            AudioListener.volume = linear;
        }
    }
}
