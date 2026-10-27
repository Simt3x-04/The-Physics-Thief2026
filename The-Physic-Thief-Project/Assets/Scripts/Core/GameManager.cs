using System;
using PhysicsThief.Level;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Singleton persistente: flusso globale del gioco (stato, caricamento livelli,
    /// pausa, sequenze di risveglio dal sogno, sblocco sandbox).
    /// Unisce GameManager (v2) e GameStateManager (UML 2).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [UnityEngine.SerializeField] private int currentLevel;
        [UnityEngine.SerializeField] private GameState gameState = GameState.MainMenu;
        [UnityEngine.SerializeField] private bool isSandboxUnlocked;

        public int CurrentLevel => currentLevel;
        public GameState State => gameState;
        public bool IsSandboxUnlocked => isSandboxUnlocked;

        public event Action<GameState> OnGameStateChanged;

        /// <summary>TODO: avvia una nuova partita (carica il primo livello).</summary>
        public void StartGame() { }

        /// <summary>TODO: Time.timeScale = 0 e stato Paused.</summary>
        public void PauseGame() { }

        /// <summary>TODO: ripristina Time.timeScale e stato Playing.</summary>
        public void ResumeGame() { }

        /// <summary>TODO: carica la scena del livello (async) e aggiorna currentLevel.</summary>
        public void LoadLevel(int levelId) { }

        /// <summary>TODO: sequenza "sveglia/allarme" che chiude un sogno e riporta alla realta'.</summary>
        public void TriggerAlarmAwakeSequence() { }

        /// <summary>TODO: fine del livello-sogno corrente, passa al successivo.</summary>
        public void TriggerDreamEnd() { }

        /// <summary>TODO: imposta isSandboxUnlocked = true e salva il progresso.</summary>
        public void UnlockSandbox() { }

        /// <summary>TODO: cambia stato, notifica OnGameStateChanged.</summary>
        public void SetState(GameState newState) { }

        /// <summary>TODO: reagisce alla morte del player (respawn al checkpoint o GameOver).</summary>
        private void HandlePlayerDied() { }
    }
}
