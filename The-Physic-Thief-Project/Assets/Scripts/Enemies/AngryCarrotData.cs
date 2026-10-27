using UnityEngine;
 
namespace PhysicsThief.Enemies
{
    /// <summary>Parametri specifici di AngryCarrot.</summary>
    [CreateAssetMenu(menuName = "Physics Thief/AngryCarrotData", fileName = "AngryCarrotData")]
    public class AngryCarrotData : EnemyData
    {
        [Header("Carica")]
        [SerializeField] private float chargeSpeed = 9f;
        [SerializeField] private float chargeCooldown = 3f;
 
        public float ChargeSpeed => chargeSpeed;
        public float ChargeCooldown => chargeCooldown;
 
        protected override void OnValidate()
        {
            base.OnValidate();
            chargeSpeed = Mathf.Max(0f, chargeSpeed);
            chargeCooldown = Mathf.Max(0f, chargeCooldown);
        }
    }
}
