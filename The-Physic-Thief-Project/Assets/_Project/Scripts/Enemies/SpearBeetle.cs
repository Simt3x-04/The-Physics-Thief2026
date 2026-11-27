using PhysicsThief.Core;
using PhysicsThief.Properties;
using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Beetle with a spear that patrols the corridors (melee).</summary>
    public class SpearBeetle : EnemyBase
    {
        private SpearBeetleData spearBeetleData;
        private IDamageable targetDamageable;
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

        public override IEnemyState CreateIdleState() => new RoamState();

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
          if(target == null || DistanceToTarget() > data.AttackRange) return;

          if(targetDamageable == null) targetDamageable = target.GetComponentInParent<IDamageable>();
          targetDamageable?.TakeDamage(data.AttackDamage);
        }

        /// <summary>A pasted hardness of 2 or less shatters the shell (GDD 4.2).</summary>
        public override void OnPhysicsAltered(AttributeType type, float newValue)
        {
            base.OnPhysicsAltered(type, newValue);
            if (type == AttributeType.Hardness && physObj.HasOverride(type) && newValue <= 2f) ShatterArmor();
        }

        public void ShatterArmor()
        {
            if(isArmorShattered) return;
            isArmorShattered = true;
        }
    }
}
