using UnityEngine;
using PhysicsThief.Properties;
using System.Collections;

namespace PhysicsThief.Enemies
{
    /// <summary>Guerriero con scudo di foglie: spara chicchi di mais.</summary>
    public class CornSoldier : EnemyBase
    {
        [SerializeField] private Transform firePoint;
        private CornSoldierData cornData;
        private bool isBursting;
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
        }

        public override IEnemyState CreateIdleState() => new RoamState();

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAllCoroutines();
            isBursting = false;
        }

        public override void Attack(){
           if(IsDead || isBursting) return;
           if(target == null || DistanceToTarget() > data.AttackRange) return;
           if(Time.time < nextBurstTime) return;

           FireKernelBurst();
        }

        public void FireKernelBurst(){
           if(isBursting) return;
           if(cornData.KernelPrefab == null || firePoint == null)
            {
                Debug.LogWarning($"{name}:KernelPrefab o firePoint missing");
                return;
            }
            isBursting = true;
            StartCoroutine(BurstRoutine());
        }

        private IEnumerator BurstRoutine()
        {
            for(int i=0; i<cornData.KernelsPerBurst; i++)
            {
                if(IsDead || target == null) break;
                shootKernel();
                bool isLast = i == cornData.KernelsPerBurst-1;
                if(!isLast && cornData.TimeBetweenKernels >0f)
                    yield return new WaitForSeconds(cornData.TimeBetweenKernels);
            }
            nextBurstTime = Time.time + cornData.BurstCooldown;
            isBursting = false;
        }

        private void shootKernel()
        {
            Vector3 aimPoint = target.position + Vector3.up*eyeHeight;
            Vector3 direction = (aimPoint - firePoint.position).normalized;

            Projectile kernel = Instantiate(cornData.KernelPrefab, firePoint.position, Quaternion.LookRotation(direction));
            kernel.Launch(direction, gameObject, data.AttackDamage);
        }
    }         
}
