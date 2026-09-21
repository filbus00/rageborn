using System.Collections.Generic;

namespace ARPG
{
    /// <summary>The backpack: a fixed number of slots holding items. Docs/03-itemization.md gives 40. Pure.</summary>
    public sealed class Inventory
    {
        public const int DefaultCapacity = 40;

        readonly List<Item> items;

        public Inventory(int capacity = DefaultCapacity)
        {
            Capacity = capacity;
            items = new List<Item>(capacity);
        }

        public int Capacity { get; }

        public int Count => items.Count;

        public bool IsFull => items.Count >= Capacity;

        public IReadOnlyList<Item> Items => items;

        /// <summary>Adds the item if there is a free slot. Returns false, changing nothing, when the backpack is full.</summary>
        public bool TryAdd(Item item)
        {
            if (item == null || IsFull)
                return false;

            items.Add(item);
            return true;
        }

        public bool Remove(Item item) => items.Remove(item);

        public bool Contains(Item item) => items.Contains(item);
    }
}
