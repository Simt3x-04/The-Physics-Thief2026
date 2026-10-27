namespace PhysicsThief.Enemies
{
    /// <summary>State pattern per l'IA dei nemici: ogni stato (pattuglia, inseguimento, attacco...) e' una classe.</summary>
    public interface IEnemyState
    {
        void Enter(EnemyBase owner);
        void Tick(EnemyBase owner);
        void Exit(EnemyBase owner);
    }
}
