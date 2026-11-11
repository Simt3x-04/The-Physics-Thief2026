using System;
using UnityEngine;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Punti vita riutilizzabili (Gino e nemici). Gino parte da 100 HP; ogni mela ne ridona 25 (GDD 4.2).
    /// Comunica solo tramite eventi: la UI e il LevelManager si iscrivono, nessuno viene chiamato direttamente.
    /// </summary>
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float currentHP = 100f;

        public float MaxHP => maxHP;
        public float CurrentHP => currentHP;
        public bool IsDead { get; private set; }

        /// <summary>Argomenti: HP correnti, HP massimi.</summary>
        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;

        private void Awake()
        {
            currentHP = maxHP;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            currentHP = Mathf.Max(0f, currentHP - amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);

            if (currentHP <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            currentHP = Mathf.Min(maxHP, currentHP + amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            currentHP = 0f;
            OnDied?.Invoke();
        }

        /// <summary>Respawn: salute piena e di nuovo vivo.</summary>
        public void ResetHealth()
        {
            IsDead = false;
            currentHP = maxHP;
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }
    }
}
