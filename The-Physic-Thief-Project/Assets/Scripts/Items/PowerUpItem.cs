using UnityEngine;
using PhysicsThief.Player;

namespace PhysicsThief.Items
{
    /// <summary>Base dei collezionabili che potenziano il player.</summary>
    public abstract class PowerUpItem : MonoBehaviour
    {
        [SerializeField] protected string itemName;

        public string ItemName => itemName;

        /// <summary>TODO: se other e' il player chiama OnCollect e distruggi/disattiva l'oggetto.</summary>
        protected virtual void OnTriggerEnter(Collider other) { }

        public abstract void OnCollect(PlayerController player);
    }
}
