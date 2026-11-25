using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Generic projectile (kernels, darts, exam grades). The player can reflect it.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 6f;

        public GameObject Owner { get; private set; }
        public bool IsReflected { get; private set; }
        public float Damage => damage;

        public void Launch(Vector3 direction, GameObject owner) { }

        /// <summary>TODO: reverse the direction and set IsReflected = true.</summary>
        public void Reflect(Vector3 newDirection) { }

        /// <summary>TODO: apply damage to IDamageable targets, handle the reflection and destroy the projectile.</summary>
        private void OnCollisionEnter(Collision collision) { }
    }
}
