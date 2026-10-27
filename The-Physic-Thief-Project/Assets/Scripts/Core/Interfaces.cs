namespace PhysicsThief.Core
{
    /// <summary>Qualunque entita' che puo' subire danno (player, nemici).</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }

    /// <summary>Oggetti che LevelManager/PuzzleRoom possono riportare allo stato iniziale.</summary>
    public interface IResettable
    {
        void ResetState();
    }
}
