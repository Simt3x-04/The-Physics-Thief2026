using UnityEngine;

namespace PhysicsThief.Enemies
{
    [CreateAssetMenu(menuName = "Physics Thief/CornSoldierData", fileName = "CornSoldierData")]

    public class CornSoldierData : EnemyData
    {
        [Header("Mais")]
        [SerializeField] private Projectile kernelPrefab;
        [SerializeField] private int kernelsPerBurst = 3;
        [SerializeField] private float timeBetweenKernels = 0.15f;
        [SerializeField] private float burstCooldown = 2.5f;

        [Header("Shield")]
        [SerializeField] private bool startsWithShield = true;
        [SerializeField] private float shieldBreakMagnetism = 50f;

        public Projectile KernelPrefab => kernelPrefab;
        public int KernelsPerBurst => kernelsPerBurst;
        public float TimeBetweenKernels => timeBetweenKernels;
        public float BurstCooldown => burstCooldown;
        public bool StartsWithShield => startsWithShield;
        public float ShieldBreakMagnetism => shieldBreakMagnetism;

        protected override void OnValidate(){
            base.OnValidate();
            kernelsPerBurst = Mathf.Max(1, kernelsPerBurst);
            timeBetweenKernels = Mathf.Max(0f, timeBetweenKernels);
            burstCooldown = Mathf.Max(0f, burstCooldown);
            shieldBreakMagnetism = Mathf.Max(0f, shieldBreakMagnetism);
        }
    }
}
