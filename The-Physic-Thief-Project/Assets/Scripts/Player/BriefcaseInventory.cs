using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Player
{
    /// <summary>MonoBehaviour sottile: delega le regole a BriefcaseModel e inoltra gli eventi alla UI.</summary>
    public class BriefcaseInventory : MonoBehaviour
    {
        private BriefcaseModel model = new BriefcaseModel();

        public int MaxSlots => model.MaxSlots;
        public float PropertyDuration => model.PropertyDuration;
        public int ActiveSlotIndex => model.ActiveSlotIndex;
        public IReadOnlyList<PropertyValue> Slots => model.Slots;

        public event Action<IReadOnlyList<PropertyValue>> OnSlotsChanged;
        public event Action<int> OnActiveSlotChanged;

        /// <summary>TODO: iscriviti a model.Changed e inoltra OnSlotsChanged.</summary>
        private void Awake() { }

        public bool AddProperty(PropertyValue prop) { return default; }
        public PropertyValue ConsumeSlot(int index) { return default; }
        public void TrashProperty(int index) { }
        public void SelectSlot(int index) { }
        public void UpgradeCapacity(int addedSlots) { }
        public void UpgradeDuration(float addedSeconds) { }
    }
}
