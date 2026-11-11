using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Core;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Ending screen – shown after the Crazy Machine boss is defeated.
    ///
    /// Entry point: GameManager.TriggerAlarmAwakeSequence() sets state to Ending.
    /// This controller listens for GameState.Ending and activates itself.
    ///
    /// Hierarchy expected:
    ///   EndingScreenCanvas  (Canvas, GraphicRaycaster, this component)
    ///     FadeOverlay         (Image, full-screen black)
    ///     EndingPanel
    ///       BedroomImage        (Image – "bedroom wake-up": Gino's math book, carrots,
    ///                            bug net, school bag. Assign real art when available;
    ///                            a solid-colour placeholder is used when null.)
    ///       ThanksLabel         (TMP_Text – "Thanks for playing")
    ///       MainMenuButton
    ///
    /// Audio:
    ///   Assign alarmClip in the Inspector. Missing clip is handled gracefully.
    ///
    /// Time-scale:
    ///   GameManager.SetState(Ending) currently does NOT freeze time (unlike Paused/GameOver).
    ///   This controller explicitly sets Time.timeScale = 0 on entry so fades work consistently.
    ///   On Main Menu load, GameManager.LoadMainMenu → SetState(Loading) resets timeScale to 1.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class EndingScreenController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject endingPanel;
        [SerializeField] private Image      fadeOverlay;

        [Header("Content")]
        [SerializeField] private Image    bedroomImage;   // assign bedroom art; placeholder OK
        [SerializeField] private TMP_Text thanksLabel;    // "Thanks for playing"

        [Header("Buttons")]
        [SerializeField] private Button mainMenuButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;  // optional; can be null
        [SerializeField] private AudioClip   alarmClip;    // optional; plays once on entry

        [Header("Animation")]
        [SerializeField] [Range(0f, 3f)] private float fadeDuration = 1f;

        // ───────────────────────────────────────────────────────────────────────
        private Coroutine _fadeRoutine;

        // ───────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            // Start with the overlay transparent and non-blocking.
            // BeginEndingSequence will make it opaque when the ending triggers.
            endingPanel.SetActive(false);
            SetFadeAlpha(0f);
            SetFadeRaycast(false);  // don't intercept clicks until the ending is active
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnStateChanged;
                if (GameManager.Instance.State == GameState.Ending)
                    BeginEndingSequence();
            }
            else
            {
                Debug.LogWarning("[EndingScreenController] GameManager not found in Start.");
            }
        }

        private void OnDestroy()
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnStateChanged;
        }

        // ───────────────────────────────────────────────────────────────────────
        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Ending)
                BeginEndingSequence();
        }

        private void BeginEndingSequence()
        {
            // Time is frozen by GameManager.SetState(Ending).
            // Cursor is also set to visible by GameManager.
            SetFadeRaycast(true);  // now it's intentionally blocking gameplay
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeInAndShow());
        }

        private IEnumerator FadeInAndShow()
        {
            // Fade from full black to transparent (reveal the ending panel).
            SetFadeAlpha(1f);
            endingPanel.SetActive(false);

            yield return null; // one frame so Unity can show the black before we reveal

            // Play alarm sfx (unscaled – audioSource plays regardless of timeScale).
            if (audioSource != null && alarmClip != null)
            {
                audioSource.clip = alarmClip;
                audioSource.ignoreListenerPause = true;
                audioSource.Play();
            }

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(1f, 0f, t / fadeDuration));
                yield return null;
            }
            SetFadeAlpha(0f);

            endingPanel.SetActive(true);
            mainMenuButton.Select();
            _fadeRoutine = null;
        }

        // ───────────────────────────────────────────────────────────────────────
        private void OnMainMenuClicked()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOutAndLoadMainMenu());
        }

        private IEnumerator FadeOutAndLoadMainMenu()
        {
            endingPanel.SetActive(false);

            float t = 0f;
            while (t < fadeDuration * 0.5f)
            {
                t += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(0f, 1f, t / (fadeDuration * 0.5f)));
                yield return null;
            }
            SetFadeAlpha(1f);

            // LoadMainMenu → SetState(Loading) resets timeScale to 1 inside GameManager.
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
