using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Singleton persistente: flusso globale del gioco (stato, caricamento scene, pausa).
    /// Va messo nella scena MainMenu (la prima della Scene List).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Scene (devono essere nella Scene List di Build Profiles)")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string[] levelScenes;

        [Header("Stato (solo lettura a runtime)")]
        [SerializeField] private int currentLevel;
        [SerializeField] private GameState gameState = GameState.MainMenu;

        private GameState stateBeforePause;

        public int CurrentLevel => currentLevel;
        public GameState State => gameState;

        /// <summary>Scatta a ogni cambio di stato: HUD, audio e player si iscrivono qui.</summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>Nuova partita: carica il primo livello.</summary>
        public void StartGame()
        {
            LoadLevel(0);
        }

        public void LoadLevel(int levelId)
        {
            if (gameState == GameState.Loading) return;   // ignora doppi clic durante il caricamento

            if (levelScenes == null || levelId < 0 || levelId >= levelScenes.Length)
            {
                Debug.LogError($"[GameManager] Livello {levelId} non configurato: controlla 'Level Scenes' nell'Inspector.");
                return;
            }

            currentLevel = levelId;
            StartCoroutine(LoadSceneRoutine(levelScenes[levelId], GameState.Playing));
        }

        public void LoadMainMenu()
        {
            if (gameState == GameState.Loading) return;
            StartCoroutine(LoadSceneRoutine(mainMenuScene, GameState.MainMenu));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, GameState stateAfterLoad)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[GameManager] La scena '{sceneName}' non e' nella Scene List (File > Build Profiles).");
                yield break;
            }

            SetState(GameState.Loading);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
            {
                yield return null;
            }

            SetState(stateAfterLoad);
        }

        public void PauseGame()
        {
            if (gameState != GameState.Playing) return;
            stateBeforePause = gameState;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (gameState == GameState.Paused)
            {
                SetState(stateBeforePause);
            }
        }

        /// <summary>
        /// UNICO punto in cui cambia lo stato: gestisce tempo, cursore e notifica gli ascoltatori.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (gameState == newState) return;
            gameState = newState;

            bool inGameplay = newState == GameState.Playing;

            Time.timeScale = newState == GameState.Paused ? 0f : 1f;
            Cursor.lockState = inGameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !inGameplay;

            OnGameStateChanged?.Invoke(newState);
        }

        /// <summary>TODO: sequenza "sveglia/allarme" che chiude un sogno e riporta alla realta'.</summary>
        public void TriggerAlarmAwakeSequence() { }

        /// <summary>TODO: fine del livello-sogno corrente, passa al successivo.</summary>
        public void TriggerDreamEnd() { }
    }
}
