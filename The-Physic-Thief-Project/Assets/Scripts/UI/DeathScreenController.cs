using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Core;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Death screen – triggered only by the PLAYER's HealthComponent.OnDied.
    /// Enemy deaths must never call this.
    ///
    /// Hierarchy expected:
    ///   DeathScreenCanvas  (Canvas, GraphicRaycaster, this component)
    ///     FadeOverlay        (Image, full-screen black, alpha animated)
    ///     DeathPanel
    ///       GinoImage          (Image – assign spiral-eyes art, or leave default placeholder)
    ///       NightmareLostLabel (TMP_Text – "Nightmare lost…")
    ///       RetryButton        ("Snooze and retry")
    ///       MainMenuButton
    ///
    /// Wiring:
    ///   Assign playerHealth in the Inspector OR call Bind(healthComponent) from
    ///   LevelManager once the player is spawned.
    ///
    /// Retry integration:
    ///   - Calls GameManager.RetryFromCheckpoint() → fires OnRetryRequested.
    ///   - LevelManager must subscribe to OnRetryRequested and call
    ///     RespawnPlayer() → player.Respawn(checkpoint.position) + playerHealth.ResetHealth().
    ///   - If LevelManager.RespawnPlayer() is still a stub, retry WILL call the chain
    ///     but visually nothing moves; the death screen disappears and gameplay state is
    ///     restored so the player can at least see the stub is reached.
    ///   - NEVER falls back to whole-scene reload (that would lose unlocked attributes).
    ///
    /// FADE: uses unscaled time (Coroutine with WaitForSecondsRealtime / Time.unscaledDeltaTime).
    /// </summary>
    public class DeathScreenController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private Image      fadeOverlay;

        [Header("Content")]
        [SerializeField] private Image   ginoImage;          // assign spiral-eyes art; placeholder is acceptable
        [SerializeField] private TMP_Text nightmareLostLabel; // "Nightmare lost…"

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Animation")]
        [SerializeField] [Range(0f, 3f)] private float fadeDuration = 0.6f;

        [Header("Player Source (optional – can also use Bind())")]
        [SerializeField] private HealthComponent playerHealth;

        // ───────────────────────────────────────────────────────────────────────
        private Coroutine _fadeRoutine;

        // ───────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            retryButton.onClick.AddListener(OnRetryClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            // Start hidden; overlay starts transparent and non-blocking.
            deathPanel.SetActive(false);
            SetFadeAlpha(0f);
            SetFadeRaycast(false);  // only intercept clicks after the screen fades in

            // If health is pre-assigned in Inspector, bind immediately.
            if (playerHealth != null)
                SubscribeToHealth(playerHealth);
        }

        private void OnDestroy()
        {
            retryButton.onClick.RemoveListener(OnRetryClicked);
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);

            if (playerHealth != null)
                playerHealth.OnDied -= OnPlayerDied;
        }

        // ───────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Connect this screen to the PLAYER's health component.
        /// Safe to call multiple times (previous subscription is cleaned up first).
        /// Must be called from LevelManager when the player is instantiated,
        /// passing player.Health (not an enemy's HealthComponent).
        /// </summary>
        public void Bind(HealthComponent health)
        {
            if (playerHealth != null)
                playerHealth.OnDied -= OnPlayerDied;

            playerHealth = health;
            SubscribeToHealth(health);
        }

        private void SubscribeToHealth(HealthComponent health)
        {
            if (health == null) return;
            health.OnDied -= OnPlayerDied; // guard against double-subscribe
            health.OnDied += OnPlayerDied;
        }

        // ───────────────────────────────────────────────────────────────────────
        private void OnPlayerDied()
        {
            // Idempotent: if already in GameOver don't re-trigger.
            if (GameManager.Instance != null &&
                GameManager.Instance.State == GameState.GameOver) return;

            // Let GameManager freeze time and set state.
            GameManager.Instance?.ReportPlayerDeath();

            // Start fade-in; show panel after fade completes.
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeInAndShow());
        }

        private IEnumerator FadeInAndShow()
        {
            // Enable raycast blocking now that the screen is intentionally active.
            SetFadeRaycast(true);
            // Fade the overlay from transparent to semi-opaque (not fully black – art stays visible).
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(0f, 0.75f, t / fadeDuration));
                yield return null;
            }
            SetFadeAlpha(0.75f);

            deathPanel.SetActive(true);
            retryButton.Select();
            _fadeRoutine = null;
        }

        // ───────────────────────────────────────────────────────────────────────
        private void OnRetryClicked()
        {
            if (GameManager.Instance == null) return;

            // Fade out first, then trigger retry.
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOutAndRetry());
        }

        private IEnumerator FadeOutAndRetry()
        {
            deathPanel.SetActive(false);

            float t = 0f;
            while (t < fadeDuration * 0.5f)
            {
                t += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(0.75f, 0f, t / (fadeDuration * 0.5f)));
                yield return null;
            }
            SetFadeAlpha(0f);
            SetFadeRaycast(false);  // no longer blocking gameplay

            // RetryFromCheckpoint → sets state to Playing → fires OnRetryRequested
            // → LevelManager.RespawnPlayer() → player.Respawn() + playerHealth.ResetHealth()
            GameManager.Instance.RetryFromCheckpoint();
            _fadeRoutine = null;
        }

        private void OnMainMenuClicked()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOutAndLoadMainMenu());
        }

        private IEnumerator FadeOutAndLoadMainMenu()
        {
            deathPanel.SetActive(false);
            float t = 0f;
            while (t < fadeDuration * 0.5f)
            {
                t += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(0.75f, 0f, t / (fadeDuration * 0.5f)));
                yield return null;
            }
            SetFadeAlpha(0f);
            SetFadeRaycast(false);

            // GameManager restores timeScale, cursor and fires OnGameStateChanged.
            GameManager.Instance?.LoadMainMenu();
            _fadeRoutine = null;
        }

        // ───────────────────────────────────────────────────────────────────────
        private void SetFadeAlpha(float alpha)
        {
            if (fadeOverlay == null) return;
            Color c = fadeOverlay.color;
            c.a = alpha;
            fadeOverlay.color = c;
        }

        private void SetFadeRaycast(bool on)
        {
            if (fadeOverlay != null) fadeOverlay.raycastTarget = on;
        }
    }
}
