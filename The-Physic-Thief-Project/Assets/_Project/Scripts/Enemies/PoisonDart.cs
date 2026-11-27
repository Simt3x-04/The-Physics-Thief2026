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
            base.Launch(direction, owner, damage);
        }

        protected override void OnHitTarget(IDamageable target)
        {
            base.OnHitTarget(target);
            if (target is Component c)
            {
                HealthComponent hc = c.GetComponentInParent<HealthComponent>();
                if (hc != null) hc.PoisonDamage(poisonDps, poisonDuration);
            }
        }
    }
}
