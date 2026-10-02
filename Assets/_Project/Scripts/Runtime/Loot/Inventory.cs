using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// How many backpack cells an item takes, by its kind (the owner, 2026-10-02: "Make gear take up realistic space",
    /// Diablo style): bows 2 x 4, quivers and chests 2 x 3, helms, gloves and boots 2 x 2, belts 2 x 1, amulets and
    /// rings 1 x 1. Width then height, in cells. Pure.
    /// </summary>
    public static class ItemSize
    {
        public static Vector2Int Of(ItemSlot kind)
        {
            switch (kind)
            {
                case ItemSlot.Weapon:
                case ItemSlot.TwoHandWeapon:
                    return new Vector2Int(2, 4);
                case ItemSlot.Shield:
                case ItemSlot.OffHand:
                case ItemSlot.Chest:
                    return new Vector2Int(2, 3);
                case ItemSlot.Helm:
                case ItemSlot.Gloves:
                case ItemSlot.Boots:
                    return new Vector2Int(2, 2);
                case ItemSlot.Belt:
                    return new Vector2Int(2, 1);
                default: // amulet and rings
                    return new Vector2Int(1, 1);
            }
        }
    }

    /// <summary>
    /// The backpack: a grid of cells (10 x 6, the owner's choice of 2026-10-02) where each item takes the space its kind
    /// needs (<see cref="ItemSize"/>). Packing is automatic, so nothing is dragged with one thumb: a new item goes in the
    /// first place it fits, scanning rows from the top left; when it fits nowhere as things lie, everything is repacked,
    /// largest first, and the item is taken only if the repacked grid holds it. <see cref="Items"/> keeps the order
    /// items came in; <see cref="PlacementOf"/> says where each lies. Pure.
    /// </summary>
    public sealed class Inventory
    {
        public const int DefaultColumns = 10;
        public const int DefaultRows = 6;

        readonly List<Item> items = new List<Item>();
        readonly Dictionary<Item, RectInt> placements = new Dictionary<Item, RectInt>();
        // Sizes already refused since the backpack last changed: auto-loot asks again every frame while a drop is in
        // reach, and a full grid would otherwise rerun the repack (which allocates) each time.
        readonly HashSet<Vector2Int> refused = new HashSet<Vector2Int>();

        public Inventory(int columns = DefaultColumns, int rows = DefaultRows)
        {
            Columns = columns;
            Rows = rows;
        }

        public int Columns { get; }

        public int Rows { get; }

        /// <summary>Cells in the grid.</summary>
        public int CellCount => Columns * Rows;

        /// <summary>Cells the items take.</summary>
        public int CellsUsed
        {
            get
            {
                var used = 0;
                foreach (var item in items)
                {
                    var size = ItemSize.Of(item.Slot);
                    used += size.x * size.y;
                }
                return used;
            }
        }

        public int Count => items.Count;

        public IReadOnlyList<Item> Items => items;

        /// <summary>Where an item lies, in cells from the top left (x right, y down).</summary>
        public bool PlacementOf(Item item, out RectInt cells) => placements.TryGetValue(item, out cells);

        /// <summary>Whether an item would be taken: it fits as things lie, or after a repack.</summary>
        public bool CanFit(Item item)
        {
            if (item == null || refused.Contains(ItemSize.Of(item.Slot)))
                return false;
            return FindSpace(ItemSize.Of(item.Slot), Occupancy()).HasValue || Pack(With(item)) != null;
        }

        /// <summary>Whether, with <paramref name="leaving"/> taken out, every item in <paramref name="arriving"/> would
        /// also fit (the swap when equipping). Changes nothing.</summary>
        public bool CanSwap(Item leaving, IList<Item> arriving)
        {
            var after = new List<Item>(items);
            if (leaving != null)
                after.Remove(leaving);
            after.AddRange(arriving);
            return Pack(after) != null;
        }

        /// <summary>
        /// Takes <paramref name="leaving"/> out (may be null) and puts every item of <paramref name="arriving"/> in, all
        /// or nothing: each where it first fits as things lie, or, when one does not, the whole backpack repacked. Returns
        /// false, changing nothing, when they cannot all fit.
        /// </summary>
        public bool TrySwap(Item leaving, IList<Item> arriving)
        {
            if (leaving != null && !items.Contains(leaving))
                return false;
            var used = new bool[Columns, Rows];
            foreach (var pair in placements)
                if (pair.Key != leaving)
                    Mark(used, pair.Value);
            var spots = new List<RectInt>(arriving.Count);
            foreach (var item in arriving)
            {
                var size = ItemSize.Of(item.Slot);
                var spot = FindSpace(size, used);
                if (!spot.HasValue)
                {
                    spots = null;
                    break;
                }
                var cells = new RectInt(spot.Value, size);
                Mark(used, cells);
                spots.Add(cells);
            }

            Dictionary<Item, RectInt> repacked = null;
            if (spots == null)
            {
                var after = new List<Item>(items);
                if (leaving != null)
                    after.Remove(leaving);
                after.AddRange(arriving);
                repacked = Pack(after);
                if (repacked == null)
                    return false;
            }

            if (leaving != null)
                Remove(leaving);
            for (var i = 0; i < arriving.Count; i++)
            {
                items.Add(arriving[i]);
                if (spots != null)
                    placements[arriving[i]] = spots[i];
            }
            if (repacked != null)
                Apply(repacked);
            refused.Clear();
            return true;
        }

        /// <summary>Adds the item where it fits, repacking if it must. Returns false, changing nothing, when there is no
        /// room for it.</summary>
        public bool TryAdd(Item item)
        {
            if (item == null || items.Contains(item))
                return false;

            var size = ItemSize.Of(item.Slot);
            if (refused.Contains(size))
                return false;
            var spot = FindSpace(size, Occupancy());
            if (spot.HasValue)
            {
                items.Add(item);
                placements[item] = new RectInt(spot.Value, size);
                refused.Clear();
                return true;
            }

            var repacked = Pack(With(item));
            if (repacked == null)
            {
                refused.Add(size);
                return false;
            }
            items.Add(item);
            Apply(repacked);
            refused.Clear();
            return true;
        }

        /// <summary>Takes an item out, leaving its cells empty (nothing moves until space is needed).</summary>
        public bool Remove(Item item)
        {
            if (!items.Remove(item))
                return false;
            placements.Remove(item);
            refused.Clear();
            return true;
        }

        /// <summary>Swaps an item for another of the same kind in the same place, as the Forge does. Returns false when
        /// the old item is not here or the new one is of another size.</summary>
        public bool Replace(Item old, Item replacement)
        {
            var index = items.IndexOf(old);
            if (index < 0 || replacement == null || ItemSize.Of(old.Slot) != ItemSize.Of(replacement.Slot))
                return false;
            items[index] = replacement;
            placements[replacement] = placements[old];
            placements.Remove(old);
            return true;
        }

        public bool Contains(Item item) => items.Contains(item);

        List<Item> With(Item item)
        {
            var all = new List<Item>(items) { item };
            return all;
        }

        bool[,] Occupancy()
        {
            var used = new bool[Columns, Rows];
            foreach (var placed in placements.Values)
                Mark(used, placed);
            return used;
        }

        static void Mark(bool[,] used, RectInt cells)
        {
            for (var y = cells.y; y < cells.y + cells.height; y++)
                for (var x = cells.x; x < cells.x + cells.width; x++)
                    used[x, y] = true;
        }

        /// <summary>The first top left corner, row by row, where a block of this size fits on empty cells.</summary>
        Vector2Int? FindSpace(Vector2Int size, bool[,] used)
        {
            for (var y = 0; y + size.y <= Rows; y++)
                for (var x = 0; x + size.x <= Columns; x++)
                {
                    var free = true;
                    for (var dy = 0; dy < size.y && free; dy++)
                        for (var dx = 0; dx < size.x && free; dx++)
                            free = !used[x + dx, y + dy];
                    if (free)
                        return new Vector2Int(x, y);
                }
            return null;
        }

        /// <summary>
        /// Places a whole set afresh: tallest first, then widest, then in the order they came, each in the first place it
        /// fits. Returns null when one does not fit. A heuristic: it can miss a packing that exists, never invents one.
        /// </summary>
        Dictionary<Item, RectInt> Pack(List<Item> set)
        {
            var order = new List<Item>(set);
            var arrival = new Dictionary<Item, int>();
            for (var i = 0; i < set.Count; i++)
                arrival[set[i]] = i;
            order.Sort((a, b) =>
            {
                Vector2Int sa = ItemSize.Of(a.Slot), sb = ItemSize.Of(b.Slot);
                if (sa.y != sb.y)
                    return sb.y.CompareTo(sa.y);
                if (sa.x != sb.x)
                    return sb.x.CompareTo(sa.x);
                return arrival[a].CompareTo(arrival[b]);
            });

            var used = new bool[Columns, Rows];
            var result = new Dictionary<Item, RectInt>();
            foreach (var item in order)
            {
                var size = ItemSize.Of(item.Slot);
                var spot = FindSpace(size, used);
                if (!spot.HasValue)
                    return null;
                var cells = new RectInt(spot.Value, size);
                Mark(used, cells);
                result[item] = cells;
            }
            return result;
        }

        void Apply(Dictionary<Item, RectInt> packed)
        {
            placements.Clear();
            foreach (var pair in packed)
                placements[pair.Key] = pair.Value;
        }
    }
}
