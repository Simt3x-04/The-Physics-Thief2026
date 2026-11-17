using UnityEngine;
using PhysicsThief.Player;

namespace PhysicsThief.Items
{
    /// <summary>Apple from trees (E key): restores 25 HP (GDD 3.6 and 4.2).</summary>
    public class AppleItem : PowerUpItem
    {
        [SerializeField] private float healAmount = 25f;

        public float HealAmount => healAmount;

        /// <summary>TODO: Call player.EatApple(this).</summary>
        public override void OnCollect(PlayerController player) { }
    }
}
