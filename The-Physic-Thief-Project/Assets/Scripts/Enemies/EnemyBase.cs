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
        [SerializeField] protected EnemyData data;
        protected EnemyStateMachine stateMachine = new EnemyStateMachine();
        protected Transform target;
        protected Rigidbody rb;

        public EnemyData Data=> data;
        public Transform Target => target;
        public EnemyStateMachine StateMachine => stateMachine;

        public Vector3 HomePosition {get; private set;}

        public bool IsDead { get; protected set; }

        /// <summary>TODO: recupera i riferimenti mancanti con GetComponent.</summary>
        protected virtual void Awake() { 
            if(healthComp == null) healthComp = GetComponent<HealthComponent>();
            if (physObj == null) physObj = GetComponent<PhysicalObject>();
            rb = GetComponent<Rigidbody>();
            HomePosition = transform.position;

            if(data == null){
                Debug.LogError("No EnemyData");
                enabled = false;
                return;
            }

            healthComp.Initialize(data.MaxHP);
        }

        /// <summary>TODO: iscriviti a physObj.OnPropertyValueChanged e healthComp.OnDied.</summary>
        protected virtual void OnEnable() { 
            if(healthComp != null) healthComp.OnDied += Die; 
            // TODO: iscriviti a physObj.OnPropertyValueChanged
        }

        protected virtual void OnDisable() { 
            if(healthComp != null) healthComp.OnDied -=Die; 
            // TODO: iscriviti a physObj.OnPropertyValueChanged
        }

        /// <summary>TODO: stateMachine.Tick(this).</summary>
        protected virtual void Update() { 
            stateMachine.Tick(this);
        }

        /// <summary>TODO: applica eventuali riduzioni (scudo, armatura) e poi healthComp.TakeDamage.</summary>
        public virtual void TakeDamage(float amount) { 
            if(IsDead) return;
            healthComp.TakeDamage(data.ApplyDamageReduction(amount));
        }

        public void MoveTowards(Vector3 destination, float speed){
            Vector3 dir = destination - transform.position;
            dir.y = 0f;
            if(dir.sqrMagnitude < 0.0001f){
                Stop(); 
                return;
            }
            dir.Normalize();

            if(rb != null){
                Vector3 v = dir*speed;
                v.y = rb.linearVelocity.y;
            }
            else{
                transform.position += dir*speed*Time.deltaTime;
            }

            transform.rotation = Quaternion.LookRotation(dir);   
        }

        public void Stop()
        {
            if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        public abstract void Attack();

        /// <summary>TODO: disattiva IA, SFX spaziale di morte, distruggi o pooling.</summary>
        protected virtual void Die() { 
            IsDead = true;
            //TODO: disattiva IA, death SFX and VFX 
        }

        /// <summary>Chiamato dai Reactor o da PhysicalObject quando una proprieta' del nemico cambia.</summary>
        public virtual void OnPhysicsAltered(AttributeType type, float newValue) { }

        protected virtual void OnDrawGizmosSelected(){
            if(data == null) return;
            Vector3 center = Application.isPlaying ? HomePosition : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, data.RoamRadius);
        }
    }
}
