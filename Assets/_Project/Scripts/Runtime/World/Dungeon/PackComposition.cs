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

        /// <summary>From depth 4 (2026-10-04): a slower, armoured melee fighter in the husk's place.</summary>
        Skeleton,

        /// <summary>From depth 4: a robed fire caster in the archer's place.</summary>
        Cultist,
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
    ///
    /// From depth 4 (<see cref="FirstUndeadDepth"/>, 2026-10-04) a second hash of the pack turns some of them undead: in
    /// 35 percent of packs the husks become skeletons, and in half of them the archers become cultists (each decided on
    /// its own). A Champion's slot 0 and elite packs stay husks.
    /// </summary>
    public static class PackComposition
    {
        public const int FirstMixedDepth = 2;

        /// <summary>The first depth with skeletons and cultists.</summary>
        public const int FirstUndeadDepth = 4;

        public const int SkeletonPercent = 35;
        public const int CultistPercent = 50;

        public static PackMember[] Roll(int depth, PackKind kind, int count, int levelSeed, int packIndex)
        {
            var members = RollAct1(depth, kind, count, levelSeed, packIndex);
            if (depth < FirstUndeadDepth || kind == PackKind.Elite)
                return members;
            var hash = (uint)DungeonRules.LevelSeed(levelSeed, 2000 + packIndex);
            var skeletons = hash % 100 < SkeletonPercent;
            var cultists = hash / 100 % 100 < CultistPercent;
            var first = kind == PackKind.WithChampion ? 1 : 0;
            for (var i = first; i < members.Length; i++)
            {
                if (skeletons && members[i] == PackMember.Husk)
                    members[i] = PackMember.Skeleton;
                else if (cultists && members[i] == PackMember.Archer)
                    members[i] = PackMember.Cultist;
            }
            return members;
        }

        static PackMember[] RollAct1(int depth, PackKind kind, int count, int levelSeed, int packIndex)
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
