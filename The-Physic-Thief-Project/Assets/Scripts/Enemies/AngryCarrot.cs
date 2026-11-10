using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Si nasconde sotto terra e carica Gino a testa bassa.</summary>
    public class AngryCarrot : EnemyBase
    {
        [SerializeField] private float chargeSpeed = 9f;
        [SerializeField] private float chargeCooldown = 3f;
        private bool isBurrowed;

        public override IEnemyState CreateIdleState(){
            return new RoamState();
        }

        public override void Attack() {Debug.Log("AngryCarrot attacca!"); }
        public void BurrowAndCharge() { }
    }
}
