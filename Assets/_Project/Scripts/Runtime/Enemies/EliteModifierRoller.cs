using System.Collections.Generic;

namespace ARPG
{
    /// <summary>
    /// Rolls an elite's modifiers at spawn (Docs/01-core-gameplay.md: one or two, never repeated). Pure and seeded
    /// through the caller's <see cref="System.Random"/>, so it can be unit tested without a scene.
    /// </summary>
    public static class EliteModifierRoller
    {
        static readonly EliteModifiers[] Pool = { EliteModifiers.Hasted, EliteModifiers.Vampiric, EliteModifiers.Frozen };

        public static EliteModifiers Roll(System.Random random)
        {
            var count = 1 + random.Next(2); // 1 or 2

            // Partial Fisher-Yates over indices, so the same modifier is never picked twice for one elite.
            var indices = new List<int>(Pool.Length);
            for (var i = 0; i < Pool.Length; i++)
                indices.Add(i);

            var result = EliteModifiers.None;
            for (var i = 0; i < count; i++)
            {
                var pick = random.Next(indices.Count);
                result |= Pool[indices[pick]];
                indices.RemoveAt(pick);
            }
            return result;
        }
    }
}
