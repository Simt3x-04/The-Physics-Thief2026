using System;
using UnityEngine;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Componente riutilizzabile per i punti vita (Player e Nemici). Evita di duplicare
    /// la logica della salute in ogni classe.
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

        /// <summary>TODO: sottrai HP, notifica OnHealthChanged, chiama Die() se arriva a 0.</summary>
        public void TakeDamage(float amount) { }

        /// <summary>TODO: aggiungi HP (clamp a maxHP) e notifica OnHealthChanged.</summary>
        public void Heal(float amount) { }

        /// <summary>TODO: imposta IsDead e invoca OnDied.</summary>
        public void Die() { }

        /// <summary>TODO: ripristina HP al massimo e IsDead = false (respawn).</summary>
        public void ResetHealth() { }
    }
}
