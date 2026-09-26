namespace ARPG
{
    /// <summary>Which act 1 enemy fills a pack slot (Docs/05-world-and-content.md: husks, ghouls, bandit archers).</summary>
    public enum PackMember
    {
        /// <summary>Nobody: the pack is smaller than its placement.</summary>
        None,
        Husk,
        Ghoul,
        Archer,
    }

    /// <summary>
    /// Decides what a generated pack is made of, slot by slot. It is not part of the generator's random stream: it is
    /// hashed from the level's seed and the pack's number, so adding enemy types left every layout, and so every saved
    /// kill, where it was. The mixes and shares are tuning values; the docs name the act's enemies but not how they mix.
    /// Pure.
    ///
    /// Depth 1 is husks only, so the first level teaches the basics. Deeper, a pack is husks only (40 percent), husks
    /// with 2 or 3 ghouls (25), husks with 2 or 3 archers (20), or a small band of 3 ghouls and 2 archers (15). Ghouls
    /// and archers take the last slots, which the pack's spiral puts on its outside, so archers start at the back. Elite
    /// packs stay husks, and a Champion keeps slot 0.
    /// </summary>
    public static class PackComposition
    {
        public const int FirstMixedDepth = 2;

        public static PackMember[] Roll(int depth, PackKind kind, int count, int levelSeed, int packIndex)
        {
            var members = new PackMember[count];
            for (var i = 0; i < count; i++)
                members[i] = PackMember.Husk;

            if (depth < FirstMixedDepth || kind == PackKind.Elite || count < 3)
                return members;

            var hash = (uint)DungeonRules.LevelSeed(levelSeed, 1000 + packIndex);
            var roll = hash % 100;
            var extra = 2 + (int)(hash / 100 % 2); // 2 or 3
            extra = System.Math.Min(extra, count - 1);

            if (roll < 40)
                return members;

            if (roll < 65)
            {
                Fill(members, count - extra, count, PackMember.Ghoul);
            }
            else if (roll < 85)
            {
                Fill(members, count - extra, count, PackMember.Archer);
            }
            else
            {
                // A band: 3 ghouls in front, 2 archers behind, nobody else. A Champion still leads from slot 0.
                var first = kind == PackKind.WithChampion ? 1 : 0;
                for (var i = first; i < count; i++)
                    members[i] = PackMember.None;
                Fill(members, first, System.Math.Min(first + 3, count), PackMember.Ghoul);
                Fill(members, System.Math.Min(first + 3, count), System.Math.Min(first + 5, count), PackMember.Archer);
            }
            return members;
        }

        static void Fill(PackMember[] members, int from, int to, PackMember member)
        {
            for (var i = System.Math.Max(0, from); i < to; i++)
                members[i] = member;
        }
    }
}
