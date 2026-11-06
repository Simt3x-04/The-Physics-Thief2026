using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Persistent singleton: manages the global game flow (state, scenes, pause, death, ending).
    /// Must be placed as a root GameObject in the MainMenu scene (the first one in the Scene List).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Scenes (must be in the Scene List of Build Profiles)")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string[] levelScenes;

        [Header("Status (read-only at runtime)")]
        [SerializeField] private int currentLevel;
        [SerializeField] private GameState gameState = GameState.MainMenu;

        public int CurrentLevel => currentLevel;
        public GameState State => gameState;

        /// <summary>Triggered on every state change: HUDs, screens, and audio register here.</summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>This is triggered when the player chooses "Snooze and retry": the LevelManager respawns the player and resets the area.</summary>
        public event Action OnRetryRequested;

        public void StartGame()
        {
            LoadLevel(0);
        }

        public void LoadLevel(int levelId)
        {
            if (gameState == GameState.Loading) return;

            if (levelScenes == null || levelId < 0 || levelId >= levelScenes.Length)
            {
                Debug.LogError($"[GameManager] Level {levelId} not configured");
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
                Debug.LogError($"[GameManager] The scene '{sceneName}' is not in the Scene List.");
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
            if (gameState == GameState.Playing) SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (gameState == GameState.Paused) SetState(GameState.Playing);
        }
        /// <summary>Called by the LevelManager when the player's HealthComponent.OnDied event fires: displays the death screen.</summary>
        public void ReportPlayerDeath()
        {
            if (gameState == GameState.Playing) SetState(GameState.GameOver);
        }

        /// <summary>Button "Snooze and retry" of the death screen.</summary>
        public void RetryFromCheckpoint()
        {
            if (gameState != GameState.GameOver) return;
            SetState(GameState.Playing);
            OnRetryRequested?.Invoke();
        }

        /// <summary>After the victory over the boss: alarm, waking up, and final screen. TODO: alarm sound and fade.</summary>
        public void TriggerAlarmAwakeSequence()
        {
            SetState(GameState.Ending);
        }

        /// <summary>The ONLY point where the state changes: it manages time, the cursor, and notifies listeners.</summary>
        public void SetState(GameState newState)
        {
            if (gameState == newState) return;
            gameState = newState;

            bool frozen = newState == GameState.Paused || newState == GameState.GameOver;
            bool inGameplay = newState == GameState.Playing;

            Time.timeScale = frozen ? 0f : 1f;
            Cursor.lockState = inGameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !inGameplay;

            OnGameStateChanged?.Invoke(newState);
        }
    }
}
