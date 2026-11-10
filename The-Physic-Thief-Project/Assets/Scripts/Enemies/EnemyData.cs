using UnityEngine;
 
namespace PhysicsThief.Enemies
{
    /// <summary>
    /// Dati condivisi da tutte le istanze di un tipo di nemico (sola lettura a runtime).
    /// Lo stato mutabile (HP correnti, ecc.) NON va qui.
    /// </summary>
    [CreateAssetMenu(menuName = "Physics Thief/EnemyData", fileName = "NewEnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identita'")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;
 
        [Header("Statistiche di base")]
        [SerializeField] private float maxHP;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float damageReduction; // 0-1
 
        [Header("Combattimento")]
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackRange;
        [SerializeField] private float attackCooldown;
 
        [Header("Percezione")]
        [SerializeField] private float detectionRange;
        [SerializeField] private float loseTargetRange; // >= detectionRange
        [SerializeField] private float viewAngle;
        [SerializeField] private float loseSightTime;
 
        [Header("Fisica")]
        [SerializeField] private float mass;

        [Header("Roaming")]
        [SerializeField] private float roamRadius;
        [SerializeField] private float roamPauseTime;
        [SerializeField] private float turnSpeed;
 
        [Header("Feedback")]
        [SerializeField] private AudioClip hitSfx;
        [SerializeField] private AudioClip deathSfx;
        [SerializeField] private GameObject deathVfx;
 
        // Proprieta' pubbliche in sola lettura
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
 
        /// <summary>TODO: restituisci il danno ridotto da damageReduction. Funzione pura, senza stato.</summary>
        public float ApplyDamageReduction(float incomingDamage) { return 0f; }
 
        /// <summary>TODO: clamp dei valori (HP >= 1, cooldown >= 0, reduction 0-1, loseTargetRange >= detectionRange).</summary>
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
