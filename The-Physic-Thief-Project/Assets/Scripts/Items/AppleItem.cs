using UnityEngine;
using PhysicsThief.Player;

namespace PhysicsThief.Items
{
    /// <summary>Mela dagli alberi (tasto E): ridona 25 HP (GDD 3.6 e 4.2).</summary>
    public class AppleItem : PowerUpItem
    {
        [SerializeField] private float healAmount = 25f;

        public float HealAmount => healAmount;

        /// <summary>TODO: chiama player.EatApple(this).</summary>
        public override void OnCollect(PlayerController player) { }
    }
}
