namespace PhysicsThief.Core
{
    /// <summary>Global game states managed by GameManager (changed only via SetState).</summary>
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
