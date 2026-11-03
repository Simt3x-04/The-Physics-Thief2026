namespace PhysicsThief.Enemies
{
    public class EnemyStateMachine{
        public IEnemyState Current{
            get; private set;
        }

        public void ChangeState(IEnemyState next, EnemyBase owner)
        {
            Current?.Exit(owner);
            Current = next;
            Current?.Enter(owner);
        }

        public void Tick(EnemyBase owner)
        {
            Current?.Tick(owner);
        }
    }
}