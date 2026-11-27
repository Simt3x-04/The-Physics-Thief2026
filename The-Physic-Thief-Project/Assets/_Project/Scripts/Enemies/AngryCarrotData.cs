using UnityEngine;
 
namespace PhysicsThief.Enemies
{
    /// <summary>Parameters specific to AngryCarrot.</summary>
    [CreateAssetMenu(menuName = "Physics Thief/AngryCarrotData", fileName = "AngryCarrotData")]
    public class AngryCarrotData : EnemyData
    {
        [Header("Charge")]
        [SerializeField] private float chargeSpeed = 9f;
        [SerializeField] private float chargeCooldown = 3f;
        [SerializeField] private float dashTurnSpeed = 90f;
        [SerializeField] private float dashDuration = 3f;
         [SerializeField] private float meleeHalfAngle = 60f;
        
 
        public float ChargeSpeed => chargeSpeed;
        public float ChargeCooldown => chargeCooldown;
        public float DashTurnSpeed=>dashTurnSpeed;
        public float DashDuration => dashDuration;
        public float MeleeHalfAngle => meleeHalfAngle;
 
        protected override void OnValidate()
        {
            base.OnValidate();
            chargeSpeed = Mathf.Max(0f, chargeSpeed);
            chargeCooldown = Mathf.Max(0f, chargeCooldown);
            dashTurnSpeed = Mathf.Max(0f, dashTurnSpeed);
            dashDuration = Mathf.Max(0f, dashDuration);
            meleeHalfAngle = Mathf.Max(0f, meleeHalfAngle);
        }
    }
}
