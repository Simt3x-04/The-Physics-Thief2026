using System;
using System.Collections.Generic;
using PhysicsThief.Properties;

namespace PhysicsThief.Player
{
    /// <summary>Esito di un tentativo di copia nella valigetta (la UI lo trasforma in un messaggio).</summary>
    public enum CopyResult
    {
        Success,
        AttributeLocked,
        NoFreeSlot
    }

    /// <summary>
    /// Regole della valigetta (GDD 3.4-3.5) in C# puro: nessun MonoBehaviour, quindi testabile.
    /// - Slot fissi: la copia va nel PRIMO slot libero; se non ce ne sono viene RIFIUTATA.
    /// - Usare una proprieta' lascia lo slot vuoto (i buchi sono ammessi).
    /// - Gli attributi si sbloccano durante il gioco (start: Mass, Velocity, Hardness).
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

        /// <summary>Scatta a ogni modifica (slot, selezione, sblocchi, durata).</summary>
        public event Action Changed;

        public BriefcaseModel(int startingSlots = 2)
        {
            UnlockedSlots = Math.Max(1, Math.Min(startingSlots, MaxSlotCount));
            unlockedAttributes.Add(AttributeType.Mass);
            unlockedAttributes.Add(AttributeType.Velocity);
            unlockedAttributes.Add(AttributeType.Hardness);
        }

        // ---------- sblocchi ----------

        public bool IsAttributeUnlocked(AttributeType type)
        {
            return unlockedAttributes.Contains(type);
        }

        public void UnlockAttribute(AttributeType type)
        {
            if (unlockedAttributes.Add(type)) Changed?.Invoke();
        }

        /// <summary>Imposta il NUMERO TOTALE di slot (es. 3 a fine tutorial, 5 uscendo dal laboratorio). Non diminuisce mai.</summary>
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

        // ---------- lettura slot ----------

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

        // ---------- copia / uso ----------

        /// <summary>Mette la proprieta' nel primo slot libero. Rifiuta se l'attributo e' bloccato o non ci sono slot liberi.</summary>
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

        /// <summary>Svuota lo slot e ritorna il suo contenuto (null se vuoto o bloccato).</summary>
        public PropertyValue? Consume(int index)
        {
            if (!IsSlotUnlocked(index) || !slots[index].HasValue) return null;

            PropertyValue taken = slots[index].Value;
            slots[index] = null;
            selected[index] = false;
            Changed?.Invoke();
            return taken;
        }

        /// <summary>Consuma gli slot selezionati; se non ce n'e' nessuno consuma quello attivo. Azzera la selezione.</summary>
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

        /// <summary>Tasto X: scarta la proprieta' dello slot attivo. True se c'era qualcosa da scartare.</summary>
        public bool TrashActive()
        {
            return Consume(ActiveSlotIndex).HasValue;
        }

        // ---------- selezione ----------

        public void SetActiveSlot(int index)
        {
            if (!IsSlotUnlocked(index) || index == ActiveSlotIndex) return;
            ActiveSlotIndex = index;
            Changed?.Invoke();
        }

        /// <summary>Rotella del mouse: +1 / -1 tra gli slot sbloccati, con giro completo.</summary>
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
}
