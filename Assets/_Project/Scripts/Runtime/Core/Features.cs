namespace ARPG
{
    /// <summary>
    /// Whole systems switched off for now, kept in the code for later (the owner, 2026-10-02: "lets drop the Smith and all
    /// the special crafting for now. Lets focus on the core. Just killing mobs, looting gear"). Turning one back on is
    /// this one line; nothing else was removed.
    /// </summary>
    public static class Features
    {
        /// <summary>The smith in town, the Forge screen, crafting materials from elites and the boss, the Forge hint and
        /// the materials row in the Bag. Off: the code and saved materials stay, nothing shows.</summary>
        public static readonly bool Forge = false;

        /// <summary>The roguelite (the owner, 2026-10-13): runs of the whole descent, boons a depth, coins for the town's
        /// upgrades; no waypoints, town portal, stairs up, corpses, experience or stat and talent points.</summary>
        public static readonly bool Roguelite = true;

        /// <summary>Quests (the owner, 2026-10-13: "Drop the quests"). Off: no offers, no objectives, no tracker. Settable
        /// so the quest tests still run.</summary>
        public static bool Quests = false;

        // The look reset (the owner, 2026-10-20: "lets reset the visuals. remove shaders and filters and lets start over";
        // asked how far: "Everything, lighting too"). Each part kept, to bring back one at a time.

        /// <summary>The grim colour grade: URP post-processing (saturation, contrast, split toning, vignette), GrimGrade.
        /// </summary>
        public static readonly bool ColourGrade = false;

        /// <summary>The pixel filter: the world rendered at the art's pixels (1.5 times heavier since 2026-10-18) and
        /// scaled up, RenderResolution's Pixel Perfect Camera. Off: the full screen resolution, the same view.</summary>
        public static readonly bool PixelFilter = false;

        /// <summary>The scenes' moods: the dark dungeon and dusk town, the ember round her, torches and other point
        /// lights, the shade on characters (LightingDirector). Off: plain full white light everywhere.</summary>
        public static readonly bool MoodLighting = false;
    }
}
