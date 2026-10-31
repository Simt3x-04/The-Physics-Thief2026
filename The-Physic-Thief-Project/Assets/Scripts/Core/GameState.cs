namespace PhysicsThief.Core
{
    /// <summary>Stati globali del gioco gestiti da GameManager (cambiano solo tramite SetState).</summary>
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        Cutscene,
        GameOver,
        Ending
    }
}
