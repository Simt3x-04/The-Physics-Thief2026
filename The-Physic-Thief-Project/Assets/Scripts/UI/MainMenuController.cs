using UnityEngine;
using UnityEngine.UI;
using PhysicsThief.Core;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Main Menu screen controller.
    ///
    /// Required serialized fields:
    ///   mainPanel    — GameObject to hide when Options opens (can be the Canvas itself in a simple scene)
    ///   startButton  — Button that begins a new run
    ///   optionsButton
    ///   quitButton
    ///   optionsPanel — OptionsPanel component (optional; if null, Options button is hidden)
    ///
    /// The UIFlowTest scene has a full OptionsPanel. The legacy MainMenu scene may have optionsPanel=null,
    /// in which case the Options button is automatically hidden to prevent an unrecoverable state.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private OptionsPanel optionsPanel;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            UISettings.Apply();

            if (startButton  != null) startButton.onClick.AddListener(OnStartClicked);
            if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClicked);
            if (quitButton   != null) quitButton.onClick.AddListener(OnQuitClicked);

            // Hide Options button if there is no panel to show — prevents an unrecoverable blank state.
            if (optionsPanel == null && optionsButton != null)
            {
                optionsButton.gameObject.SetActive(false);
                Debug.Log("[MainMenu] optionsPanel not assigned — Options button hidden. " +
                          "Assign an OptionsPanel component to enable it.");
            }

            if (optionsPanel != null)
                optionsPanel.Close();

            ShowMainPanel();
            if (startButton != null) startButton.Select();
        }

        private void OnDestroy()
        {
            if (startButton  != null) startButton.onClick.RemoveListener(OnStartClicked);
            if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsClicked);
            if (quitButton   != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        // ─────────────────────────────────────────────────────────────────────
        private void OnStartClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[MainMenu] GameManager not found. " +
                               "Add a GameManager GameObject to this scene's root.");
                return;
            }

            // Double-click guard — re-enabled by ShowMainPanel() if load fails.
            SetButtonsInteractable(false);
            GameManager.Instance.StartGame();
        }

        private void OnOptionsClicked()
        {
            if (optionsPanel == null) return;   // guarded: button is hidden when panel is null
            if (mainPanel != null) mainPanel.SetActive(false);
            optionsPanel.Open(onBack: OnOptionsBack);
        }

        private void OnOptionsBack()
        {
            ShowMainPanel();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ─────────────────────────────────────────────────────────────────────
        private void ShowMainPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            SetButtonsInteractable(true);
            if (startButton != null) startButton.Select();
        }

        private void SetButtonsInteractable(bool on)
        {
            if (startButton  != null) startButton.interactable  = on;
            if (optionsButton != null) optionsButton.interactable = on;
            if (quitButton   != null) quitButton.interactable   = on;
        }
    }
}
