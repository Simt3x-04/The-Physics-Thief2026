using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Properties;
using PhysicsThief.Properties.Reactors;

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
        [SerializeField] protected EnemyData data;

        [SerializeField] protected LayerMask obstacleMask;
        [SerializeField] protected float eyeHeight = 1f;
        [SerializeField] protected string targetTag ="Player";

        [Header("Pasted properties (GDD 3.2 and 4.2)")]
        [Tooltip("A pasted mass of at least this much pins the enemy down: it cannot walk or charge.")]
        [SerializeField] protected float groundedMass = 200f;
        [Tooltip("A pasted temperature of at most this freezes the enemy: no movement and no attacks.")]
        [SerializeField] protected float freezeTemperature = 0f;
        [Tooltip("A pasted temperature of at least this sets a flammable enemy (Organic, Wood, Paper) on fire. It burns until it dies.")]
        [SerializeField] protected float ignitionTemperature = 100f;
        [SerializeField] protected float burnDamagePerSecond = 10f;
        [Tooltip("Seconds between death and the enemy disappearing.")]
        [SerializeField] protected float deathDelay = 1.5f;
        [SerializeField] private Color frozenTint = new Color(0.65f, 0.88f, 1f, 1f);
        [SerializeField] private Color burningTint = new Color(1f, 0.55f, 0.1f, 1f);

        private readonly RaycastHit[] sightHits = new RaycastHit[16];
        private float nextAttackTime;

        public bool IsFrozen { get; private set; }
        /// <summary>Too heavy to move, because of a pasted mass.</summary>
        public bool IsPinned { get; private set; }
        public bool IsBurning { get; private set; }
        /// <summary>A velocity is pasted: physics (RigidbodyReactor) moves the enemy, so the AI must not touch it.</summary>
        public bool IsLaunched => physObj != null && physObj.HasOverride(AttributeType.Velocity);

        protected EnemyStateMachine stateMachine = new EnemyStateMachine();
        protected Transform target;
        protected Rigidbody rb;

        public EnemyData Data=> data;
        public Transform Target => target;
        public EnemyStateMachine StateMachine => stateMachine;

        public Vector3 HomePosition {get; private set;}

        public bool IsDead { get; protected set; }

        /// <summary>Fetches the missing references and sets up the health from the enemy data.</summary>
        protected virtual void Awake() { 
            if(healthComp == null) healthComp = GetComponent<HealthComponent>();
            if (physObj == null) physObj = GetComponent<PhysicalObject>();
            rb = GetComponent<Rigidbody>();
            physObj.LiveVelocity = CurrentSpeed;   // copying the Velocity of an enemy takes how fast it is going right now
            HomePosition = transform.position;

            if(data == null){
                Debug.LogError("No EnemyData");
                enabled = false;
                return;
            }

            healthComp.Initialize(data.MaxHP);
        }

        protected virtual void Start(){
            IEnemyState idle = CreateIdleState();
            if(idle!=null) stateMachine.ChangeState(idle, this);
        }

        public virtual IEnemyState CreateIdleState(){
            return null;
        }

        public virtual IEnemyState CreateCombatState(){
            return new CombatState();
        }

        protected virtual void OnEnable()
        {
            if (healthComp != null) healthComp.OnDied += Die;
            if (physObj != null) physObj.OnPropertyValueChanged += OnPhysicsAltered;
        }

        protected virtual void OnDisable()
        {
            if (healthComp != null) healthComp.OnDied -= Die;
            if (physObj != null) physObj.OnPropertyValueChanged -= OnPhysicsAltered;
        }

        /// <summary>Runs the AI. It pauses while the enemy is frozen or flying with a pasted velocity.</summary>
        protected virtual void Update()
        {
            if (IsDead) return;
            if (IsBurning) healthComp.TakeDamage(burnDamagePerSecond * Time.deltaTime);   // fire ignores armor
            if (IsDead || IsFrozen || IsLaunched) return;
            stateMachine.Tick(this);
        }

        /// <summary>Applies the damage reduction of the enemy data (armor), then damages the health.</summary>
        public virtual void TakeDamage(float amount)
        {
            if (IsDead) return;
            healthComp.TakeDamage(data.ApplyDamageReduction(amount));
        }

        // Horizontal speed (falling does not count), rounded so that the scanner shows a clean number.
        private float CurrentSpeed()
        {
            if (rb == null || IsDead) return 0f;
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            return Mathf.Round(velocity.magnitude * 10f) / 10f;
        }

        protected void AcquireTarget(){
            if(target!=null) return;
            GameObject found = GameObject.FindGameObjectWithTag(targetTag);
            if(found != null) target = found.transform;
        }

        public float DistanceToTarget(){
            return target == null ? float.MaxValue : Vector3.Distance(transform.position, target.position);
        }

        /// <summary>True if nothing on the obstacle layers stands between the eyes. The enemy and the target never block.</summary>
        public bool HasLineOfSight()
        {
            if (target == null) return false;
            Vector3 from = transform.position + Vector3.up * eyeHeight;
            Vector3 toEyes = target.position + Vector3.up * eyeHeight - from;
            float distance = toEyes.magnitude;
            if (distance < 0.0001f) return true;

            int count = Physics.RaycastNonAlloc(from, toEyes / distance, sightHits, distance, obstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = sightHits[i].collider.transform;
                if (!hit.IsChildOf(transform) && !hit.IsChildOf(target)) return false;
            }
            return true;
        }

        public bool TrySpotTarget(){
            AcquireTarget();
            if(target == null) return false;
            if(DistanceToTarget() > data.DetectionRange) return false;

            Vector3 toTarget = target. position - transform.position;
            toTarget.y = 0f;
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if(toTarget.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f){
                if(Vector3.Angle(forward, toTarget) > data.ViewAngle*0.5f) return false;
            }

            return HasLineOfSight();
        }

        public void FaceTarget(float turnSpeed){
            if(target == null || IsFrozen) return;

            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if(dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), turnSpeed*Time.deltaTime);
        }

        public bool TryAttack(){
            if(IsDead || IsFrozen || IsLaunched || Time.time < nextAttackTime) return false;
            nextAttackTime = Time.time + data.AttackCooldown;
            Attack();
            return true;
        }

        public void MoveTowards(Vector3 destination, float speed){
            if(IsLaunched) return;
            if(IsFrozen || IsPinned){
                Stop();
                return;
            }
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
                rb.linearVelocity = v;
            }
            else{
                transform.position += dir*speed*Time.deltaTime;
            }

            transform.rotation = Quaternion.LookRotation(dir);   
        }

        public void Stop()
        {
            if (rb != null && !IsLaunched) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        public abstract void Attack();

        /// <summary>Switches the AI off, lets the body drop and removes the enemy after 'deathDelay'.</summary>
        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;
            IsBurning = false;
            IsFrozen = false;
            IsPinned = false;
            UpdateTint();

            stateMachine.ChangeState(null, this);
            StopAllCoroutines();
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                rb.useGravity = true;
            }
            enabled = false;
            Destroy(gameObject, deathDelay);
            // TODO: death sound and effects (EnemyData.DeathSfx / DeathVfx).
        }

        /// <summary>
        /// Called whenever a property of this enemy changes (pasted, or back to its base value when the paste expires).
        /// As with the reactors, only a PASTED value has an effect. Mass and velocity also reach the Rigidbody through
        /// RigidbodyReactor; magnetism and hardness are handled by their reactors.
        /// </summary>
        public virtual void OnPhysicsAltered(AttributeType type, float newValue)
        {
            if (IsDead) return;
            bool pasted = physObj.HasOverride(type);

            if (type == AttributeType.Mass)
            {
                IsPinned = pasted && newValue >= groundedMass;
                if (IsPinned) Stop();
            }
            else if (type == AttributeType.Temperature)
            {
                IsFrozen = pasted && newValue <= freezeTemperature;
                if (IsFrozen) Stop();
                if (pasted && newValue >= ignitionTemperature && IsFlammable) IsBurning = true;
                UpdateTint();
            }
        }

        private bool IsFlammable
        {
            get
            {
                MaterialTag material = physObj.GetMaterial();
                return material == MaterialTag.Organic || material == MaterialTag.Wood || material == MaterialTag.Paper;
            }
        }

        // Placeholder visuals until there are real frost and fire effects.
        private void UpdateTint()
        {
            if (IsBurning) ObjectTint.For(gameObject).Set(this, burningTint);
            else if (IsFrozen) ObjectTint.For(gameObject).Set(this, frozenTint);
            else
            {
                ObjectTint tint = GetComponent<ObjectTint>();
                if (tint != null) tint.Clear(this);
            }
        }

        protected virtual void OnDrawGizmosSelected(){
            if(data == null) return;
            Vector3 center = Application.isPlaying ? HomePosition : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, data.RoamRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.DetectionRange);
            Vector3 left = Quaternion.Euler(0f, -data.ViewAngle*0.5f, 0f)*transform.forward;
            Vector3 right = Quaternion.Euler(0f, data.ViewAngle*0.5f, 0f)*transform.forward;
            Gizmos.DrawRay(transform.position, left*data.DetectionRange);
            Gizmos.DrawRay(transform.position, right*data.DetectionRange);
        }
    }
}