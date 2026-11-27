namespace PhysicsThief.Core
{
    /// <summary>Any entity that can take damage (players, enemies).</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }

    /// <summary>Objects that LevelManager/PuzzleRoom can reset to their initial state.</summary>
    public interface IResettable
    {
        void ResetState();
    }
}
