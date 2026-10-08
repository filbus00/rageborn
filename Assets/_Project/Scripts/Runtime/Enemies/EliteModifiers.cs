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

        // Attacks to dodge (2026-10-08, the owner: "interesting attacks that need to be dodged"; EliteAffixRules).
        /// <summary>Burning ground where it walks; bursts when it dies.</summary>
        Molten = 1 << 3,

        /// <summary>A dark circle forms under the player and erupts.</summary>
        Desecrator = 1 << 4,

        /// <summary>Lobs shells onto marked circles at the player.</summary>
        Mortar = 1 << 5,

        /// <summary>Poison pools spread under the player and linger.</summary>
        Plagued = 1 << 6,

        /// <summary>After a wind-up, ice bursts out around it and slows.</summary>
        FrostNova = 1 << 7,

        /// <summary>Lines streak across the floor, then lightning strikes along them.</summary>
        LightningLances = 1 << 8,

        /// <summary>The pack's elites are joined by burning chains; crossing one burns.</summary>
        FireChains = 1 << 9,

        /// <summary>Sets an arcane sentry whose beam sweeps round.</summary>
        ArcaneBeam = 1 << 10,
    }
}
