using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>The three boss phases: joints, radiator, core.</summary>
    public enum BossPhase
    {
        Joints,
        Radiator,
        Core
    }

    /// <summary>3-phase boss. Each phase can become a dedicated IEnemyState when needed.</summary>
    public class CrazyMachineBoss : EnemyBase
    {
        [SerializeField] private BossPhase currentPhase = BossPhase.Joints;
        [SerializeField] private float radiatorTemperature = 200f;
        private bool areJointsDestroyed;
        private bool isCoreExposed;

        public override void Attack() { }
        public void SwitchPhase(BossPhase newPhase) { }
        public void RedirectProjectilesPhase() { }
        public void CoolRadiatorPhase() { }
        public void CoreImpactPhase() { }
        public void VentHeat() { }
        public void ExposeCore() { }
        public void TakeCoreDamage(float amount) { }
    }
}
