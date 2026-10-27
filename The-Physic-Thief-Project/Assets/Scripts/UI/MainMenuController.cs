using UnityEngine;
using UnityEngine.UI;
using PhysicsThief.Core;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Schermata iniziale. Non conosce livelli o scene: chiede tutto al GameManager.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject optionsPanel;

        private void Start()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            optionsButton.onClick.AddListener(ToggleOptions);
            quitButton.onClick.AddListener(OnQuitClicked);

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
            optionsButton.onClick.RemoveListener(ToggleOptions);
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnPlayClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[MainMenu] Nessun GameManager nella scena: aggiungilo come GameObject root.");
                return;
            }
            GameManager.Instance.StartGame();
        }

        private void ToggleOptions()
        {
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(!optionsPanel.activeSelf);
            }
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
