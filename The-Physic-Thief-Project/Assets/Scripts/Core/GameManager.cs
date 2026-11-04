using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Singleton persistente: flusso globale del gioco (stato, scene, pausa, morte, finale).
    /// Va messo come GameObject root nella scena MainMenu (la prima della Scene List).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Scene (devono essere nella Scene List di Build Profiles)")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string[] levelScenes;

        [Header("Stato (solo lettura a runtime)")]
        [SerializeField] private int currentLevel;
        [SerializeField] private GameState gameState = GameState.MainMenu;

        public int CurrentLevel => currentLevel;
        public GameState State => gameState;

        /// <summary>Scatta a ogni cambio di stato: HUD, schermate e audio si iscrivono qui.</summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>Scatta quando il giocatore sceglie "Snooze and retry": il LevelManager respawna e resetta l'area.</summary>
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
            if (gameState == GameState.Playing) SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (gameState == GameState.Paused) SetState(GameState.Playing);
        }

        /// <summary>Chiamato dal LevelManager quando HealthComponent.OnDied del player scatta: mostra la death screen.</summary>
        public void ReportPlayerDeath()
        {
            if (gameState == GameState.Playing) SetState(GameState.GameOver);
        }

        /// <summary>Bottone "Snooze and retry" della death screen.</summary>
        public void RetryFromCheckpoint()
        {
            if (gameState != GameState.GameOver) return;
            SetState(GameState.Playing);
            OnRetryRequested?.Invoke();
        }

        /// <summary>Dopo la vittoria sul boss: allarme, risveglio e schermata finale. TODO: suono della sveglia e fade.</summary>
        public void TriggerAlarmAwakeSequence()
        {
            SetState(GameState.Ending);
        }

        /// <summary>UNICO punto in cui cambia lo stato: gestisce tempo, cursore e notifica gli ascoltatori.</summary>
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
