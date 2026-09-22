using System;

namespace ARPG
{
    /// <summary>
    /// Docs/01-core-gameplay.md: each elite carries one or two of these, chosen at spawn. A curated slice of the
    /// docs' eight (Molten, Frozen, Vampiric, Shielded, Teleporting, Splitting, Hasted, Cursing) - the three that
    /// hook into stats and systems this project already has, without needing a new hazard-area, shield/absorb,
    /// steering-override or resistance system yet.
    /// </summary>
    [Flags]
    public enum EliteModifiers
    {
        None = 0,

        /// <summary>Plus 30 percent move and attack speed.</summary>
        Hasted = 1 << 0,

        /// <summary>Heals on a landed hit.</summary>
        Vampiric = 1 << 1,

        /// <summary>Slows the player on a landed hit.</summary>
        Frozen = 1 << 2,
    }
}
