namespace PhysicsThief.Enemies
{
    /// <summary>Lightweight state machine (pure C#) used by EnemyBase and the bosses.</summary>
    public class EnemyStateMachine
    {
        public IEnemyState Current { get; private set; }

        /// <summary>TODO: Exit the current state, assign next, Enter next.</summary>
        public void ChangeState(IEnemyState next, EnemyBase owner) { }

        /// <summary>TODO: Current?.Tick(owner).</summary>
        public void Tick(EnemyBase owner) { }
    }
}
