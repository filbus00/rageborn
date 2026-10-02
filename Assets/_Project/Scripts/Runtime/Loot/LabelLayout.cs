using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Keeps the names of drops on the ground from lying on top of each other after a big pack dies: each label starts
    /// over its drop, and one that would overlap a label already placed is lifted a line at a time until it is clear.
    /// Better items are placed first, so they keep their spot and the junk moves. Labels are boxes centred on x with
    /// their bottom at y, in world units. Pure, and allocation free once its lists have grown.
    /// </summary>
    public sealed class LabelLayout
    {
        /// <summary>Space kept between two stacked labels, in world units.</summary>
        public const float Gap = 0.02f;

        /// <summary>A label is never lifted more than this many lines; a pile bigger than that overlaps.</summary>
        public const int MaxLift = 8;

        readonly List<int> order = new List<int>();
        readonly List<Rect> placed = new List<Rect>();
        readonly List<Entry> entries = new List<Entry>();
        readonly System.Comparison<int> compare;

        public LabelLayout() => compare = Compare;

        struct Entry
        {
            public Vector2 Anchor;
            public Vector2 Size;
            public int Priority;
        }

        public int Count => entries.Count;

        public void Clear() => entries.Clear();

        /// <summary>Adds a label: its anchor (bottom centre over the drop), its size, and its priority (higher keeps
        /// its spot). Returns its index.</summary>
        public int Add(Vector2 anchor, Vector2 size, int priority)
        {
            entries.Add(new Entry { Anchor = anchor, Size = size, Priority = priority });
            return entries.Count - 1;
        }

        /// <summary>Places every label; <paramref name="lifts"/> gets each one's lift above its anchor, by index.</summary>
        public void Solve(List<float> lifts)
        {
            lifts.Clear();
            order.Clear();
            placed.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                lifts.Add(0f);
                order.Add(i);
            }
            // Higher priority first; equal ones by the lower anchor first (nearer the camera), then by index.
            order.Sort(compare);

            foreach (var index in order)
            {
                var entry = entries[index];
                var line = entry.Size.y + Gap;
                var lift = 0f;
                for (var step = 0; step <= MaxLift; step++)
                {
                    lift = step * line;
                    if (!Overlaps(Box(entry, lift)))
                        break;
                }
                lifts[index] = lift;
                placed.Add(Box(entry, lift));
            }
        }

        int Compare(int a, int b)
        {
            Entry ea = entries[a], eb = entries[b];
            if (ea.Priority != eb.Priority)
                return eb.Priority.CompareTo(ea.Priority);
            if (!Mathf.Approximately(ea.Anchor.y, eb.Anchor.y))
                return ea.Anchor.y.CompareTo(eb.Anchor.y);
            return a.CompareTo(b);
        }

        static Rect Box(Entry entry, float lift) =>
            new Rect(entry.Anchor.x - entry.Size.x * 0.5f, entry.Anchor.y + lift, entry.Size.x, entry.Size.y);

        bool Overlaps(Rect box)
        {
            foreach (var other in placed)
                if (box.Overlaps(other))
                    return true;
            return false;
        }
    }
}
