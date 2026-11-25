namespace PhysicsThief.Enemies
{
    /// <summary>State pattern for the enemy AI: each state (patrol, chase, attack...) is a class.</summary>
    public interface IEnemyState
    {
        void Enter(EnemyBase owner);
        void Tick(EnemyBase owner);
        void Exit(EnemyBase owner);
    }
}
