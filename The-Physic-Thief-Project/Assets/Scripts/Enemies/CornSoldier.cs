using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Enemies
{
    /// <summary>Guerriero con scudo di foglie: spara chicchi di mais.</summary>
    public class CornSoldier : EnemyBase
    {
        [SerializeField] private Transform firePoint;
        private CornSoldierData cornData;
        private bool hasShield;
        private float nextBurstTime;

        protected override void Awake()
        {
            base.Awake();

            cornData = data as CornSoldierData;
            if(cornData == null){
                Debug.LogError($"{name}: serve un CornSoldierData!", this);
                enabled = false;
                return;
            }
            hasShield = cornData.StartsWithShield;
        }

        public override void Attack(){
            //TODO: if in range (cornData.attackRange) e Time.time >= nextBurstTime ==> FireKernelBurst
        }

        public void FireKernelBurst(){
            //TODO: spara cornData.KernelsPerBurst proiettili da firePoint(cornData.KernelPrefab),
            // distanziati di cornData.TimeBetweenKernels
            //nextBurstTime = Time.time + cornData.BurstCooldown
        }

        public void Defend(){
            //TODO: comportamento diensivo quando hasShield è true
        }

        public override void OnPhysicsAltered(AttributeType type, float newValue){
            //TODO: se type è Magnetism e newValue > cornData.ShieldBreakMagnetism, has shield = false
        } 
    }         
}
