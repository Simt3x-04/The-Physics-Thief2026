using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Properties;

namespace PhysicsThief.Enemies
{
    /// <summary>
    /// Base class of the enemies. It USES (does not inherit from) PhysicalObject and HealthComponent: composition
    /// instead of inheritance, so enemies stay flexible.
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

        /// <summary>TODO: fetch the missing references with GetComponent.</summary>
        protected virtual void Awake() { }

        /// <summary>TODO: subscribe to physObj.OnPropertyValueChanged and healthComp.OnDied.</summary>
        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        /// <summary>TODO: stateMachine.Tick(this).</summary>
        protected virtual void Update() { }

        /// <summary>TODO: apply any reductions (shield, armor), then healthComp.TakeDamage.</summary>
        public virtual void TakeDamage(float amount) { }

        public abstract void Attack();

        /// <summary>TODO: disable the AI, play the spatial death SFX, destroy or pool.</summary>
        protected virtual void Die() { }

        /// <summary>Called by the reactors or by PhysicalObject when a property of the enemy changes.</summary>
        public virtual void OnPhysicsAltered(AttributeType type, float newValue) { }
    }
}
