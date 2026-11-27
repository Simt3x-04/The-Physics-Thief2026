using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Enemies
{
    /// <summary>Generic projectile (kernels, darts, exam grades). The player can reflect it.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float Speed = 12f;
        [SerializeField] private float Lifetime = 6f;

        private Rigidbody rb;
        private float damage;
        public float Damage => damage;
        public GameObject Owner {get; private set;}
        public bool IsReflected {get; private set;}
        private Vector3 newDirection;

        private void Awake(){
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        public void Launch(Vector3 direction, GameObject owner, float damage){
            Owner = owner;
            this.damage = damage;

            direction.Normalize();
            rb.linearVelocity = direction * Speed;
            transform.rotation = Quaternion.LookRotation(direction);
            Destroy(gameObject, Lifetime);
        }

        public void Reflect(Vector3 direction){
            IsReflected = true;
            direction.Normalize();
            rb.linearVelocity = direction * Speed;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void OnCollisionEnter(Collision collision){
            GameObject other = collision.gameObject;

            if(!IsReflected && Owner != null && other.transform.root == Owner.transform.root) return;
            IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
            if(target != null)
                OnHitTarget(target);
            
            Destroy(gameObject);
        }


        protected virtual void OnHitTarget(IDamageable target){
            target.TakeDamage(damage);
        }
    }
}
