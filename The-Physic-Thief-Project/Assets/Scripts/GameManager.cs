using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public GameStateEnum gameState;
    public Transform lastCheckpoint;
    public bool isSandboxUnlocked;

    private void Awake()
    {
        // Implementation here
    }

    public void LoadLevel(int levelID)
    {
        // Implementation here
    }

    public void RespawnPlayer()
    {
        // Implementation here
    }

    public void TriggerDreamEnd()
    {
        // Implementation here
    }

    public void UnlockSandbox()
    {
        // Implementation here
    }
}
