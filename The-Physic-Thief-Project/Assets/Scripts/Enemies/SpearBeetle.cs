using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Coleottero con lancia che pattuglia i corridoi (mischia).</summary>
    public class SpearBeetle : EnemyBase
    {
        private SpearBeetleData spearBeetleData;
        [SerializeField] private Transform firePoint;
        private bool isArmorShattered;
        public bool IsArmorShattered => isArmorShattered;

        private float HealthPercentage => healthComp.CurrentHP / healthComp.MaxHP;

        protected override void Awake()
        {
            base.Awake();
            spearBeetleData = data as SpearBeetleData;
            if(spearBeetleData == null){
                Debug.LogError($"No spearBeetleData", this);
                enabled = false;
            }
        }

        public override IEnemyState CreateIdleState()
        {
            return new RoamState();
        }

        public override void TakeDamage(float damage)
        {
            if (IsDead) return;
            float reduction = isArmorShattered ? spearBeetleData.ShatteredDamageReduction : spearBeetleData.ArmorDamageReduction;
            base.TakeDamage(damage * (1f - reduction));
            if(!IsDead && !isArmorShattered && HealthPercentage <= spearBeetleData.ArmorBreakHealthPercentage)
            {
                isArmorShattered = true;
            }
        }

        public override void Attack(){
            if(target == null||spearBeetleData.ProjectilePrefab == null|| firePoint == null) return;
            if(DistanceToTarget() > spearBeetleData.ShootRange|| !HasLineOfSight())return;

            Vector3 aimPoint = target.position + Vector3.up * eyeHeight;
            Vector3 direction = (aimPoint - firePoint.position).normalized;
            Projectile projectile = Instantiate(spearBeetleData.ProjectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
            projectile.Launch(direction,gameObject ,spearBeetleData.ProjectileDamage);
        }

        public void ShatterArmor()
        {
            if(isArmorShattered) return;
            isArmorShattered = true;
        }
    }
}
