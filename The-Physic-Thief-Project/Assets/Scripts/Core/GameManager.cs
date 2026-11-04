using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Singleton persistente: flusso globale del gioco (stato, scene, pausa, morte, finale).
    /// Va messo come GameObject root nella scena MainMenu (la prima della Scene List).
    ///
    /// NOTE FOR TEAMMATES:
    ///   OnGameStateChanged is an INSTANCE event — subscribe via GameManager.Instance.OnGameStateChanged.
    ///   Start() applies the initial state policy (cursor, timeScale) for the serialised gameState,
    ///   so whichever scene the manager lives in will be correctly initialised without an explicit
    ///   SetState call.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Scenes (must be in Build Profiles / Build Settings)")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string[] levelScenes;

        [Header("State (read-only at runtime)")]
        [SerializeField] private int currentLevel;
        [SerializeField] private GameState gameState = GameState.MainMenu;

        public int CurrentLevel => currentLevel;
        public GameState State => gameState;

        /// <summary>Fires on every state change. Subscribe via GameManager.Instance.OnGameStateChanged.</summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>Fires when the player chooses "Snooze and retry". LevelManager subscribes to respawn.</summary>
        public event Action OnRetryRequested;

        // ─────────────────────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            // Singleton.Awake destroys duplicates and returns early for them.
            // Only the surviving instance continues past this point.
            if (Instance != this) return;
        }

        private void Start()
        {
            // Apply cursor / timeScale policy for the initial serialised state so the first
            // frame is correct even when SetState has never been called.
            ApplyStatePolicy(gameState);
        }

        // ─────────────────────────────────────────────────────────────────────
        public void StartGame()
        {
            LoadLevel(0);
        }

        public void LoadLevel(int levelId)
        {
            if (gameState == GameState.Loading) return;

            if (levelScenes == null || levelId < 0 || levelId >= levelScenes.Length)
            {
                Debug.LogError(
                    $"[GameManager] Level {levelId} not configured. " +
                    "Open the GameManager GameObject and fill 'Level Scenes' in the Inspector.");
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
                Debug.LogError(
                    $"[GameManager] Scene '{sceneName}' is not in Build Settings " +
                    "(File → Build Profiles). Add it there and try again.");
                yield break;
            }

            SetState(GameState.Loading);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
                yield return null;

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

        /// <summary>Called when the player's HealthComponent.OnDied fires (via LevelManager or DeathScreenController).</summary>
        public void ReportPlayerDeath()
        {
            if (gameState == GameState.Playing) SetState(GameState.GameOver);
        }

        /// <summary>"Snooze and retry" button on the death screen.</summary>
        public void RetryFromCheckpoint()
        {
            if (gameState != GameState.GameOver) return;
            SetState(GameState.Playing);
            OnRetryRequested?.Invoke();
        }

        /// <summary>Call after the Crazy Machine boss is defeated to trigger the ending screen.</summary>
        public void TriggerAlarmAwakeSequence()
        {
            SetState(GameState.Ending);
        }

        /// <summary>
        /// Single point of state change: manages timeScale, cursor and notifies all listeners.
        /// SetState is a no-op when newState == current state.
        /// Use ApplyStatePolicy() to enforce policy without broadcasting a change event.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (gameState == newState) return;
            gameState = newState;
            ApplyStatePolicy(newState);
            OnGameStateChanged?.Invoke(newState);
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Applies cursor + timeScale for a given state without changing gameState or firing events.
        /// Called by Start() so the initial serialised value is enforced immediately.
        /// </summary>
        private void ApplyStatePolicy(GameState state)
        {
            bool frozen     = state == GameState.Paused
                           || state == GameState.GameOver
                           || state == GameState.Ending;
            bool inGameplay = state == GameState.Playing;

            // Loading restores timeScale=1 so async operations are never frozen mid-load.
            if (state == GameState.Loading) Time.timeScale = 1f;
            else                            Time.timeScale = frozen ? 0f : 1f;

            Cursor.lockState = inGameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !inGameplay;
        }
    }
}
