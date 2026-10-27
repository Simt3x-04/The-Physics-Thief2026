using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Proiettile generico (chicchi, dardi, voti d'esame). Puo' essere riflesso dal player.</summary>
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

        /// <summary>TODO: inverte la direzione e imposta IsReflected = true.</summary>
        public void Reflect(Vector3 newDirection) { }

        /// <summary>TODO: applica danno agli IDamageable, gestisce la riflessione e distruggi il proiettile.</summary>
        private void OnCollisionEnter(Collision collision) { }
    }
}
