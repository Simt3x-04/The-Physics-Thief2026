using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Properties;

namespace PhysicsThief.Enemies
{
    /// <summary>
    /// Base dei nemici. USA (non eredita da) PhysicalObject e HealthComponent: composizione al posto
    /// dell'ereditarieta', cosi' i nemici restano flessibili.
    /// </summary>
    [RequireComponent(typeof(HealthComponent), typeof(PhysicalObject))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] protected HealthComponent healthComp;
        [SerializeField] protected PhysicalObject physObj;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float detectionRange = 15f;

        protected EnemyStateMachine stateMachine = new EnemyStateMachine();
        protected Transform target;

        public bool IsDead { get; protected set; }

        /// <summary>TODO: recupera i riferimenti mancanti con GetComponent.</summary>
        protected virtual void Awake() { }

        /// <summary>TODO: iscriviti a physObj.OnPropertyValueChanged e healthComp.OnDied.</summary>
        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        /// <summary>TODO: stateMachine.Tick(this).</summary>
        protected virtual void Update() { }

        /// <summary>TODO: applica eventuali riduzioni (scudo, armatura) e poi healthComp.TakeDamage.</summary>
        public virtual void TakeDamage(float amount) { }

        public abstract void Attack();

        /// <summary>TODO: disattiva IA, SFX spaziale di morte, distruggi o pooling.</summary>
        protected virtual void Die() { }

        /// <summary>Chiamato dai Reactor o da PhysicalObject quando una proprieta' del nemico cambia.</summary>
        public virtual void OnPhysicsAltered(AttributeType type, float newValue) { }
    }
}
