using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Le tre fasi del boss: giunti, radiatore, nucleo.</summary>
    public enum BossPhase
    {
        Joints,
        Radiator,
        Core
    }

    /// <summary>Boss a 3 fasi. Ogni fase puo' diventare un IEnemyState dedicato quando serve.</summary>
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
