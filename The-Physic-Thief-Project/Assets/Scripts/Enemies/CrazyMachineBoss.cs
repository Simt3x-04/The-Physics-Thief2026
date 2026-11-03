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
