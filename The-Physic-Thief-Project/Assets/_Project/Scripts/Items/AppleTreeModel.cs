using System;

namespace PhysicsThief.Items
{
    /// <summary>
    /// Which fruit positions of a tree still hold an apple, in pure C# (no MonoBehaviour), so it can be tested.
    /// - The first 'StartingCount' positions start with an apple; the others stay empty.
    /// - A position can be consumed once; consuming it again is refused.
    /// - Restore() puts back exactly the starting apples, so a reset never duplicates fruit.
    /// </summary>
    public class AppleTreeModel
    {
        private readonly bool[] available;

        public int Capacity => available.Length;
        public int StartingCount { get; }
        public int AvailableCount { get; private set; }

        public AppleTreeModel(int capacity, int startingCount)
        {
            available = new bool[Math.Max(0, capacity)];
            StartingCount = Math.Max(0, Math.Min(startingCount, available.Length));
            Restore();
        }

        public bool IsAvailable(int index)
        {
            return index >= 0 && index < available.Length && available[index];
        }

        /// <summary>Removes the apple at 'index'. False if there is none (already eaten, or never there).</summary>
        public bool TryConsume(int index)
        {
            if (!IsAvailable(index)) return false;
            available[index] = false;
            AvailableCount--;
            return true;
        }

        /// <summary>Back to the starting apples (area reset).</summary>
        public void Restore()
        {
            for (int i = 0; i < available.Length; i++)
            {
                available[i] = i < StartingCount;
            }
            AvailableCount = StartingCount;
        }
    }
}
