using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using PhysicsThief.Core;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Pause screen – lives on a persistent Canvas that exists in every gameplay scene.
    ///
    /// Hierarchy expected:
    ///   PauseMenuCanvas (this component)
    ///     PausePanel
    ///       ResumeButton
    ///       OptionsButton
    ///     OptionsPanel  (OptionsPanel component)
    ///
    /// Esc ordering (matches spec):
    ///   Playing          → Esc → Paused  (shows PausePanel)
    ///   Paused/PausePanel → Esc → Playing (resumes)
    ///   Paused/OptionsOpen → Esc → back to PausePanel (options close, still paused)
    ///
    /// Input: reads UI/Cancel from the InputSystem asset.
    /// Never disables the whole InputActionAsset (that would kill UI navigation too).
    /// The Player action map is enabled/disabled by this controller only.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private OptionsPanel optionsPanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        // ───────────────────────────────────────────────────────────────────────
        private InputAction _cancelAction;
        private bool        _optionsOpen;

        // ───────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Get UI/Cancel action (bound to Escape by default in the asset).
            _cancelAction = inputActions.FindAction("UI/Cancel", throwIfNotFound: true);

            resumeButton.onClick.AddListener(OnResumeClicked);
            optionsButton.onClick.AddListener(OnOptionsClicked);

            // Ensure pause and options panels start hidden.
            pausePanel.SetActive(false);
            optionsPanel.Close();
        }

        private void Start()
        {
            // Subscribe after Awake so GameManager.Instance is guaranteed to exist.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnStateChanged;
                ApplyState(GameManager.Instance.State);
            }
            else
            {
                Debug.LogWarning("[PauseMenuController] GameManager.Instance is null in Start. " +
                                 "Ensure GameManager is in the MainMenu scene and persists.");
            }

            _cancelAction.Enable();
            _cancelAction.performed += OnCancelPerformed;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnStateChanged;

            _cancelAction.performed -= OnCancelPerformed;
            resumeButton.onClick.RemoveListener(OnResumeClicked);
            optionsButton.onClick.RemoveListener(OnOptionsClicked);
        }

        // ───────────────────────────────────────────────────────────────────────
        private void OnStateChanged(GameState state)  => ApplyState(state);

        private void ApplyState(GameState state)
        {
            bool isPaused = state == GameState.Paused;

            // Keep options closed whenever we re-enter paused from a non-paused state.
            if (!isPaused)
            {
                _optionsOpen = false;
                optionsPanel.Close();
            }

            pausePanel.SetActive(isPaused && !_optionsOpen);
        }

        // ───────────────────────────────────────────────────────────────────────
        // Cancel / Esc handling – single place, no double-fire.
        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (GameManager.Instance == null) return;
            GameState state = GameManager.Instance.State;

            if (state == GameState.Playing)
            {
                // Esc while playing → pause
                GameManager.Instance.PauseGame();
                ShowPausePanel();
                return;
            }

            if (state == GameState.Paused)
            {
                if (_optionsOpen)
                {
                    // Esc while options open → close options, stay paused
                    CloseOptions();
                }
                else
                {
                    // Esc while pause panel open → resume
                    OnResumeClicked();
                }
            }
            // In other states (MainMenu, GameOver, Ending, Loading) we do nothing.
        }

        // ───────────────────────────────────────────────────────────────────────
        private void OnResumeClicked()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.ResumeGame();
            // ApplyState(Playing) will hide everything via OnStateChanged.
        }

        private void OnOptionsClicked()
        {
            _optionsOpen = true;
            pausePanel.SetActive(false);
            optionsPanel.Open(onBack: CloseOptions);
        }

        private void CloseOptions()
        {
            _optionsOpen = false;
            pausePanel.SetActive(true);
            resumeButton.Select();
        }

        private void ShowPausePanel()
        {
            _optionsOpen = false;
            pausePanel.SetActive(true);
            resumeButton.Select();
        }
    }
}
