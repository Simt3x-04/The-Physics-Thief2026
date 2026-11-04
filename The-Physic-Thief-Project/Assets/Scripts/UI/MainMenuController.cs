using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PhysicsThief.Core;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Main Menu screen.
    ///
    /// Hierarchy expected:
    ///   MainMenuCanvas
    ///     MainPanel
    ///       TitleLabel   (TMP_Text – "The Physics Thief")
    ///       StartButton
    ///       OptionsButton
    ///       QuitButton
    ///     OptionsPanel   (OptionsPanel component – starts inactive)
    ///
    /// Behaviour:
    ///   - Start begins a new run (level 0); never continues a save.
    ///   - Double-click protection: button disabled during load.
    ///   - Scene validation: shows a console error if the scene isn't in Build Settings.
    ///   - Options opens the shared OptionsPanel; Back returns to the main panel.
    ///   - Quit works in standalone and exits play mode in the Editor.
    ///   - Applies persisted UISettings (volume, sensitivity) on Awake.
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

        // ───────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Apply saved preferences (volume, sensitivity) immediately.
            UISettings.Apply();

            startButton.onClick.AddListener(OnStartClicked);
            optionsButton.onClick.AddListener(OnOptionsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);

            // Ensure options panel starts hidden.
            if (optionsPanel != null)
                optionsPanel.Close();

            ShowMainPanel();
            startButton.Select();
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            optionsButton.onClick.RemoveListener(OnOptionsClicked);
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        // ───────────────────────────────────────────────────────────────────────
        private void OnStartClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[MainMenu] GameManager not found in scene. " +
                               "Add a GameManager GameObject to the MainMenu scene root.");
                return;
            }

            // Double-click guard.
            startButton.interactable = false;
            optionsButton.interactable = false;
            quitButton.interactable = false;

            GameManager.Instance.StartGame();   // validates scenes internally and logs errors
        }

        private void OnOptionsClicked()
        {
            mainPanel.SetActive(false);
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

        // ───────────────────────────────────────────────────────────────────────
        private void ShowMainPanel()
        {
            mainPanel.SetActive(true);
            // Re-enable buttons in case we returned from a failed load.
            startButton.interactable   = true;
            optionsButton.interactable = true;
            quitButton.interactable    = true;
            startButton.Select();
        }
    }
}
