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
 
        [Header("Fisica")]
        [SerializeField] private float mass;
 
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
 
        public float Mass => mass;
 
        public AudioClip HitSfx => hitSfx;
        public AudioClip DeathSfx => deathSfx;
        public GameObject DeathVfx => deathVfx;
 
        /// <summary>TODO: restituisci il danno ridotto da damageReduction. Funzione pura, senza stato.</summary>
        public float ApplyDamageReduction(float incomingDamage) { return 0f; }
 
        /// <summary>TODO: clamp dei valori (HP >= 1, cooldown >= 0, reduction 0-1, loseTargetRange >= detectionRange).</summary>
        protected virtual void OnValidate() { }
    }
}
