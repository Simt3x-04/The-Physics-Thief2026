using UnityEngine;
using PhysicsThief.Player;

namespace PhysicsThief.Items
{
    /// <summary>A foundation of collectibles that boost the player.</summary>
    public abstract class PowerUpItem : MonoBehaviour
    {
        [SerializeField] protected string itemName;

        public string ItemName => itemName;

        /// <summary>TODO: If 'other' is the player, call OnCollect and destroy/deactivate the object.</summary>
        protected virtual void OnTriggerEnter(Collider other) { }

        public abstract void OnCollect(PlayerController player);
    }
}
