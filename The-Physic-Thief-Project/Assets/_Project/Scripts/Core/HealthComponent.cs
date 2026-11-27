using System;
using System.Collections;
using UnityEngine;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Reusable health points (Gino and enemies). Gino starts with 100 HP; each apple restores 25 HP (GDD 4.2).
    /// Communication occurs solely via events: the UI and LevelManager subscribe to these events; no direct calls are made.
    /// </summary>
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float currentHP = 100f;

        public float MaxHP => maxHP;
        public float CurrentHP => currentHP;
        public bool IsDead { get; private set; }
        public bool IsPoisoned => poisonCoroutine != null;

        private Coroutine poisonCoroutine;

        /// <summary>Arguments: current HP, max HP.</summary>
        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;

        private void Awake()
        {
            currentHP = maxHP;
        }

        public void Initialize(float maxHealth)
        {
            maxHP = Mathf.Max(1f, maxHealth);
            currentHP = maxHP;
            IsDead = false;

            OnHealthChanged?.Invoke(currentHP, maxHP);
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

        /// <summary>Respawn: full health and alive.</summary>
        public void ResetHealth()
        {
            IsDead = false;
            currentHP = maxHP;
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

         public void PoisonDamage(float dps, float duration)
        {
            if (IsDead || dps <= 0f || duration <= 0f)
                return;

            if (!isActiveAndEnabled)
                return;

            StopPoison();

            poisonCoroutine = StartCoroutine(
                PoisonRoutine(dps, duration)
            );
        }

        private IEnumerator PoisonRoutine(
            float dps, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration && !IsDead)
            {
                yield return null;

                float delta = Mathf.Min(
                    Time.deltaTime,
                    duration - elapsed
                );

                elapsed += delta;

                TakeDamage(dps * delta);
            }

            poisonCoroutine = null;
        }

        /// <summary>
        /// Removes the active poison effect.
        /// </summary>
        public void StopPoison()
        {
            if (poisonCoroutine != null)
            {
                StopCoroutine(poisonCoroutine);
                poisonCoroutine = null;
            }
        }

        private void OnDisable()
        {
            StopPoison();
        }
    }
}
