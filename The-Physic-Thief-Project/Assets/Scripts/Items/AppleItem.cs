using UnityEngine;
using PhysicsThief.Player;

namespace PhysicsThief.Items
{
    /// <summary>Mela: cura e/o potenzia la valigetta (valori da definire col GDD).</summary>
    public class AppleItem : PowerUpItem
    {
        [SerializeField] private float healAmount = 25f;
        [SerializeField] private int extraSlots;
        [SerializeField] private float extraDurationSeconds;

        public float HealAmount => healAmount;
        public int ExtraSlots => extraSlots;
        public float ExtraDurationSeconds => extraDurationSeconds;

        /// <summary>TODO: chiama player.EatApple(this).</summary>
        public override void OnCollect(PlayerController player) { }
    }
}
