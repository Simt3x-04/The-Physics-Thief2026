using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Hides underground and charges at Gino head first.</summary>
    public class AngryCarrot : EnemyBase
    {
        [SerializeField] private float chargeSpeed = 9f;
        [SerializeField] private float chargeCooldown = 3f;
        private bool isBurrowed;

        public override void Attack() { }
        public void BurrowAndCharge() { }
    }
}
