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
    public class CrazyMachineBoss : BossBase
    {

        private CrazyMachineData bossData;

        [SerializeField] private BossPhase currentPhase = BossPhase.Joints;
        private float radiatorTemperature;
        private bool areJointsDestroyed;
        private bool isCoreExposed;

        protected void Awake()
        {
            base.Awake();
            bossData = data as CrazyMachineData;
            if(bossData == null)
            {
                Debug.LogError($"No bossData");
                enabled = false;
                return;
            }
            radiatorTemperature = bossData.RadiatorStartTemperature;
        }

        public override void Attack() { }
        public void SwitchPhase(BossPhase newPhase) { 
            //TODO: switch phase if it gets to a certain hp
        }
        public void RedirectProjectilesPhase() { }
        public void CoolRadiatorPhase() { }
        public void CoreImpactPhase() { }
        public void VentHeat(){
            //TODO: radiatorTemperature -=bossData.VentHeatAmount
        }
        public void ExposeCore(){ 
            // if isCoreExposed then amount*bossData.CoreDamageMultiplier -> healthComp.TakeDamage
        }
        public void TakeCoreDamage(float amount) { }
    }
}
