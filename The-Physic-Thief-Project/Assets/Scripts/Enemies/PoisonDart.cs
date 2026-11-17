using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Enemies
{

    public interface Ipoisonable
    {
        void ApplyPoison(float dps, float duration);
    }

    /// <summary>Proiettile di veleno sparato dall'ape.</summary>
    public class PoisonDart : Projectile
    {
        private float poisonDps;
        private float poisonDuration;

        public void Launch(Vector3 direction, float damage, float poisonDps, float poisonDuration, GameObject owner)
        {
            this.poisonDps = poisonDps;
            this.poisonDuration = poisonDuration;
            //base.Launch(direction, damage, owner);
        }

        protected override void OnHitTarget(IDamageable target)
        {
            base.OnHitTarget(target);
            if (target is HealthComponent healthComp)
            {
                healthComp.PoisonDamage(poisonDps, poisonDuration);
            }
        }
    }
}
