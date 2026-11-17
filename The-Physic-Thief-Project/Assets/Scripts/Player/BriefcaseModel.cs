using System;
using System.Collections.Generic;
using PhysicsThief.Properties;

namespace PhysicsThief.Player
{
    /// <summary>Result of an attempt to copy to the clipboard (the UI turns this into a message).</summary>
    public enum CopyResult
    {
        Success,
        AttributeLocked,
        NoFreeSlot
    }

    /// <summary>
    /// Briefcase rules (GDD 3.4–3.5) implemented in pure C#: no `MonoBehaviour` dependencies, ensuring testability.
    /// - Fixed slots: an item is placed into the FIRST available slot; if none are free, it is REJECTED.
    /// - Removing an item leaves its slot empty (gaps are permitted).
    /// - Attributes are unlocked during gameplay (starting with: Mass, Velocity, Hardness).
    /// </summary>
    public class BriefcaseModel
    {
        public const int MaxSlotCount = 5;
        public const float BaseDuration = 3f;
        public const float MaxDuration = 7f;

        private readonly PropertyValue?[] slots = new PropertyValue?[MaxSlotCount];
        private readonly bool[] selected = new bool[MaxSlotCount];
        private readonly HashSet<AttributeType> unlockedAttributes = new HashSet<AttributeType>();

        public int UnlockedSlots { get; private set; }
        public float PropertyDuration { get; private set; } = BaseDuration;
        public int ActiveSlotIndex { get; private set; }

        /// <summary>Triggered by any change (slot, selection, unlocks, duration).</summary>
        public event Action Changed;

        public BriefcaseModel(int startingSlots = 2)
        {
            UnlockedSlots = Math.Max(1, Math.Min(startingSlots, MaxSlotCount));
            unlockedAttributes.Add(AttributeType.Mass);
            unlockedAttributes.Add(AttributeType.Velocity);
            unlockedAttributes.Add(AttributeType.Hardness);
        }

        public bool IsAttributeUnlocked(AttributeType type)
        {
            return unlockedAttributes.Contains(type);
        }

        //Unlock attributes

        public void UnlockAttribute(AttributeType type)
        {
            if (unlockedAttributes.Add(type)) Changed?.Invoke();
        }

        /// <summary>Set the TOTAL NUMBER of slots (e.g., 3 at the end of the tutorial, 5 upon leaving the laboratory). This never decreases.</summary>
        public void UnlockSlots(int totalSlots)
        {
            int target = Math.Min(totalSlots, MaxSlotCount);
            if (target <= UnlockedSlots) return;
            UnlockedSlots = target;
            Changed?.Invoke();
        }

        public void UpgradeDuration(float addedSeconds)
        {
            if (addedSeconds <= 0f) return;
            PropertyDuration = Math.Min(PropertyDuration + addedSeconds, MaxDuration);
            Changed?.Invoke();
        }

        //Slot reading

        public bool IsSlotUnlocked(int index)
        {
            return index >= 0 && index < UnlockedSlots;
        }

        public PropertyValue? GetSlot(int index)
        {
            return IsSlotUnlocked(index) ? slots[index] : null;
        }

        public bool IsSelected(int index)
        {
            return IsSlotUnlocked(index) && selected[index];
        }

        // copy/paste

        /// <summary>Places the property in the first available slot. Rejects if the attribute is locked or there are no available slots.</summary>
        public CopyResult TryAdd(PropertyValue property)
        {
            if (!IsAttributeUnlocked(property.type)) return CopyResult.AttributeLocked;

            for (int i = 0; i < UnlockedSlots; i++)
            {
                if (!slots[i].HasValue)
                {
                    slots[i] = property;
                    Changed?.Invoke();
                    return CopyResult.Success;
                }
            }
            return CopyResult.NoFreeSlot;
        }

        /// <summary>Empties the slot and returns its contents (null if empty or locked).</summary>
        public PropertyValue? Consume(int index)
        {
            if (!IsSlotUnlocked(index) || !slots[index].HasValue) return null;

            PropertyValue taken = slots[index].Value;
            slots[index] = null;
            selected[index] = false;
            Changed?.Invoke();
            return taken;
        }

        /// <summary>Consumes the selected slots; if none are selected, it consumes the active one. Clears the selection.</summary>
        public List<PropertyValue> ConsumeSelected()
        {
            var result = new List<PropertyValue>();

            bool anySelected = false;
            for (int i = 0; i < UnlockedSlots; i++)
            {
                if (selected[i]) anySelected = true;
            }

            if (!anySelected)
            {
                PropertyValue? single = Consume(ActiveSlotIndex);
                if (single.HasValue) result.Add(single.Value);
                return result;
            }

            for (int i = 0; i < UnlockedSlots; i++)
            {
                if (!selected[i]) continue;
                PropertyValue? taken = Consume(i);
                if (taken.HasValue) result.Add(taken.Value);
            }
            ClearSelection();
            return result;
        }

        /// <summary>X key: discards the item in the active slot. Returns true if there was something to discard.</summary>
        public bool TrashActive()
        {
            return Consume(ActiveSlotIndex).HasValue;
        }

        //Selection

        public void SetActiveSlot(int index)
        {
            if (!IsSlotUnlocked(index) || index == ActiveSlotIndex) return;
            ActiveSlotIndex = index;
            Changed?.Invoke();
        }

        /// <summary>Mouse wheel: +1 / -1 between unlocked slots, with full rotation.</summary>
        public void CycleActiveSlot(int direction)
        {
            int count = UnlockedSlots;
            ActiveSlotIndex = ((ActiveSlotIndex + direction) % count + count) % count;
            Changed?.Invoke();
        }

        public void ToggleSelected(int index)
        {
            if (!IsSlotUnlocked(index)) return;
            selected[index] = !selected[index];
            Changed?.Invoke();
        }

        public void ClearSelection()
        {
            bool any = false;
            for (int i = 0; i < MaxSlotCount; i++)
            {
                if (selected[i]) any = true;
                selected[i] = false;
            }
            if (any) Changed?.Invoke();
        }
    }

    public static class CopyResultExtensions
    {
        /// <summary>Message for the player (null if the copy was successful).</summary>
        public static string ToMessage(this CopyResult result)
        {
            switch (result)
            {
                case CopyResult.AttributeLocked: return "Attribute still locked";
                case CopyResult.NoFreeSlot: return "No free slot - press X to trash one";
                default: return null;
            }
        }
    }
}
