using System;
using System.Collections.Generic;
using PhysicsThief.Properties;

namespace PhysicsThief.Player
{
    /// <summary>
    /// Regole della valigetta in C# puro (nessun MonoBehaviour): testabile con il Test Framework.
    /// BriefcaseInventory ne e' solo il "guscio" per Unity.
    /// </summary>
    public class BriefcaseModel
    {
        public const int MinSlots = 2;
        public const int MaxSlotsCap = 5;

        private readonly List<PropertyValue> slots = new List<PropertyValue>();

        public int MaxSlots { get; private set; } = MinSlots;
        public float PropertyDuration { get; private set; } = 5f;
        public int ActiveSlotIndex { get; private set; }
        public IReadOnlyList<PropertyValue> Slots => slots;

        public event Action Changed;

        /// <summary>TODO: aggiunge la proprieta'; se piena scarta la piu' vecchia (FIFO). Ritorna true se aggiunta.</summary>
        public bool Add(PropertyValue prop) { return default; }

        /// <summary>TODO: rimuove e ritorna la proprieta' dello slot indicato.</summary>
        public PropertyValue Consume(int index) { return default; }

        public void Trash(int index) { }
        public void SelectSlot(int index) { }

        /// <summary>TODO: aumenta MaxSlots senza superare MaxSlotsCap.</summary>
        public void UpgradeCapacity(int addedSlots) { }

        public void UpgradeDuration(float addedSeconds) { }
    }
}
