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
    }
}
