using UnityEngine;
 
namespace PhysicsThief.Enemies
{
    /// <summary>
    /// Data shared by every instance of an enemy type (read-only at runtime).
    /// Mutable state (current HP and so on) does NOT belong here.
    /// </summary>
    [CreateAssetMenu(menuName = "Physics Thief/EnemyData", fileName = "NewEnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;
 
        [Header("Base stats")]
        [SerializeField] private float maxHP;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float damageReduction; // 0-1
 
        [Header("Combat")]
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackRange;
        [SerializeField] private float attackCooldown;
 
        [Header("Perception")]
        [SerializeField] private float detectionRange;
        [SerializeField] private float loseTargetRange; // >= detectionRange
        [SerializeField] private float viewAngle;
        [SerializeField] private float loseSightTime;
 
        [Header("Physics")]
        [SerializeField] private float mass;

        [Header("Roaming")]
        [SerializeField] private float roamRadius;
        [SerializeField] private float roamPauseTime;
        [SerializeField] private float turnSpeed;
 
        [Header("Feedback")]
        [SerializeField] private AudioClip hitSfx;
        [SerializeField] private AudioClip deathSfx;
        [SerializeField] private GameObject deathVfx;
 
        // Read-only public properties
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
 
        public float MaxHP => maxHP;
        public float MoveSpeed => moveSpeed;
        public float DamageReduction => damageReduction;
 
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
 
        public float DetectionRange => detectionRange;
        public float LoseTargetRange => loseTargetRange;
        public float ViewAngle => viewAngle;
        public float LoseSightTime => loseSightTime;
 
        public float Mass => mass;

        public float RoamRadius => roamRadius;
        public float RoamPauseTime => roamPauseTime;
        public float TurnSpeed => turnSpeed;
 
        public AudioClip HitSfx => hitSfx;
        public AudioClip DeathSfx => deathSfx;
        public GameObject DeathVfx => deathVfx;
 
        /// <summary>Returns the damage after damageReduction. Pure function, no state.</summary>
        public float ApplyDamageReduction(float incomingDamage)
        {
            return Mathf.Max(0f, incomingDamage) * (1f - damageReduction);
        }
 
        /// <summary>Clamps the values (HP >= 1, cooldown >= 0, reduction 0-1, loseTargetRange >= detectionRange).</summary>
        protected virtual void OnValidate() {
            maxHP = Mathf.Max(1f, maxHP);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            damageReduction = Mathf.Clamp01(damageReduction);
 
            attackDamage = Mathf.Max(0f, attackDamage);
            attackRange = Mathf.Max(0f, attackRange);
            attackCooldown = Mathf.Max(0f, attackCooldown);
 
            detectionRange = Mathf.Max(0f, detectionRange);
            loseTargetRange = Mathf.Max(detectionRange, loseTargetRange);
 
            mass = Mathf.Max(0.01f, mass);
 
            roamRadius = Mathf.Max(0.5f, roamRadius);
            roamPauseTime = Mathf.Max(0f, roamPauseTime);
            turnSpeed = Mathf.Max(1f, turnSpeed);
        }
    }
}
