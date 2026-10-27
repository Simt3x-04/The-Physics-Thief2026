namespace PhysicsThief.Enemies
{
    /// <summary>Macchina a stati leggera (C# puro) usata da EnemyBase e dai boss.</summary>
    public class EnemyStateMachine
    {
        public IEnemyState Current { get; private set; }

        /// <summary>TODO: Exit dello stato corrente, assegna next, Enter di next.</summary>
        public void ChangeState(IEnemyState next, EnemyBase owner) { }

        /// <summary>TODO: Current?.Tick(owner).</summary>
        public void Tick(EnemyBase owner) { }
    }
}
