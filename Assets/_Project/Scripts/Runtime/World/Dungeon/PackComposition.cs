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

        // The rest of act 1's roster (2026-10-05, Docs/05).
        /// <summary>From depth 3: a charger, in packs of its own.</summary>
        AshWolf,

        /// <summary>From depth 3: a fast double striker among husks.</summary>
        Cutthroat,

        /// <summary>From depth 4: a caster of burning circles in an archer's place.</summary>
        EmberAcolyte,

        /// <summary>From depth 4: a support at the back of a pack, its aura making the others hit harder.</summary>
        PyreKeeper,

        /// <summary>From depth 5: a slow brute that bursts, in a ghoul's place.</summary>
        CarrionBloat,

        // The deep levels (2026-10-05), each fading in over the levels from its first (PackComposition.Deep).
        Drowned,
        Harpooner,
        DrownedWatchman,
        SkeletonKnight,
        GravePriest,
        Hollowed,
        VoidWraith,
        RiftCaller,
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

        // The rest of act 1's roster (2026-10-05, Docs/05; depths from its "From depth" column, shares are tuning).
        public const int FirstWolfDepth = 3;
        public const int FirstCutthroatDepth = 3;
        public const int FirstAcolyteDepth = 4;
        public const int FirstKeeperDepth = 4;
        public const int FirstBloatDepth = 5;
        public const int WolfPackPercent = 20;
        public const int CutthroatPercent = 30;
        public const int AcolytePercent = 35;
        public const int KeeperPercent = 30;
        public const int BloatPercent = 40;

        /// <summary>Docs/05: wolves run in packs of 4 to 6.</summary>
        public const int MinWolves = 4;
        public const int MaxWolves = 6;

        public static PackMember[] Roll(int depth, PackKind kind, int count, int levelSeed, int packIndex) =>
            Deep(depth, kind, Roster(depth, kind, RollUndead(depth, kind, count, levelSeed, packIndex), levelSeed, packIndex), levelSeed, packIndex);

        /// <summary>
        /// How likely a pack is to have a deep type in place of an older one: none before its first depth, then
        /// <paramref name="perLevel"/> percent more a level, up to <paramref name="max"/> (2026-10-05: the dungeon changes
        /// gradually, no hard sections).
        /// </summary>
        public static int DeepShare(int depth, int first, int perLevel, int max) =>
            depth < first ? 0 : System.Math.Min(max, (depth - first + 1) * perLevel);

        // Each deep type: the first depth it appears, how fast it spreads, and its cap. Tuning.
        public const int FirstDrownedDepth = 7, FirstHarpoonerDepth = 8, FirstWatchmanDepth = 9;
        public const int FirstKnightDepth = 13, FirstPriestDepth = 14;
        public const int FirstHollowedDepth = 19, FirstRiftCallerDepth = 19, FirstWraithDepth = 20;

        /// <summary>
        /// The deep levels' roster, from a fourth hash of the pack. From 7 the drowned take the husks' places, from 8
        /// harpooners the archers', from 9 drowned watchmen the ghouls'; from 13 skeleton knights the brutes', from 14 a
        /// grave priest stands at the back of some packs of 4 or more; from 19 the hollowed take the swarmers' places
        /// and rift callers the ranged ones', from 20 the wolf packs turn to void wraiths. Each spreads by its share
        /// (<see cref="DeepShare"/>), so a level has some old and some new. Elite packs and a Champion's slot 0 stay.
        /// </summary>
        static PackMember[] Deep(int depth, PackKind kind, PackMember[] members, int levelSeed, int packIndex)
        {
            if (kind == PackKind.Elite || depth < FirstDrownedDepth)
                return members;
            var a = (uint)DungeonRules.LevelSeed(levelSeed, 5000 + packIndex);
            var b = (uint)DungeonRules.LevelSeed(levelSeed, 6000 + packIndex);
            var first = kind == PackKind.WithChampion ? 1 : 0;
            bool Rolls(uint hash, int share) => hash % 100 < share;

            var drowned = Rolls(a, DeepShare(depth, FirstDrownedDepth, 15, 75));
            var harpoon = Rolls(a / 100, DeepShare(depth, FirstHarpoonerDepth, 15, 70));
            var watchman = Rolls(a / 10000, DeepShare(depth, FirstWatchmanDepth, 15, 70));
            var knight = Rolls(a / 1000000, DeepShare(depth, FirstKnightDepth, 15, 75));
            var priest = Rolls(b, DeepShare(depth, FirstPriestDepth, 10, 40));
            var hollowed = Rolls(b / 100, DeepShare(depth, FirstHollowedDepth, 15, 80));
            var rift = Rolls(b / 10000, DeepShare(depth, FirstRiftCallerDepth, 12, 60));
            var wraith = Rolls(b / 1000000, DeepShare(depth, FirstWraithDepth, 20, 100));

            for (var i = first; i < members.Length; i++)
            {
                var m = members[i];
                if (m == PackMember.Husk || m == PackMember.Skeleton || m == PackMember.Cutthroat)
                    m = hollowed ? PackMember.Hollowed : drowned ? PackMember.Drowned : m;
                else if (m == PackMember.Archer || m == PackMember.Cultist || m == PackMember.EmberAcolyte)
                    m = rift ? PackMember.RiftCaller : harpoon && m == PackMember.Archer ? PackMember.Harpooner : m;
                else if (m == PackMember.Ghoul || m == PackMember.CarrionBloat)
                    m = knight ? PackMember.SkeletonKnight : watchman ? PackMember.DrownedWatchman : m;
                else if (m == PackMember.AshWolf && wraith)
                    m = PackMember.VoidWraith;
                members[i] = m;
            }
            if (priest && members.Length >= 4 && System.Array.IndexOf(members, PackMember.PyreKeeper) < 0 &&
                System.Array.IndexOf(members, PackMember.AshWolf) < 0 && System.Array.IndexOf(members, PackMember.VoidWraith) < 0)
            {
                for (var i = members.Length - 1; i >= first; i--)
                    if (members[i] != PackMember.None)
                    {
                        members[i] = PackMember.GravePriest;
                        break;
                    }
            }
            return members;
        }

        /// <summary>
        /// The rest of act 1's roster, from a third hash of the pack so every earlier roll stays as it was. Normal packs
        /// from depth 3: one in five is a wolf pack (4 to 6 wolves and nobody else; a placement for fewer keeps its
        /// husks); in 30 percent every second husk is a cutthroat. From depth 4: in 35 percent the archers left are
        /// ember acolytes, and in 30 percent of packs of 4 or more the last slot (the back) is a pyre keeper. From depth 5,
        /// in 40 percent of packs the ghouls are carrion bloats. Elite packs and a Champion's slot 0 stay as they were.
        /// </summary>
        static PackMember[] Roster(int depth, PackKind kind, PackMember[] members, int levelSeed, int packIndex)
        {
            if (kind == PackKind.Elite || depth < FirstWolfDepth)
                return members;
            var hash = (uint)DungeonRules.LevelSeed(levelSeed, 3000 + packIndex);
            var first = kind == PackKind.WithChampion ? 1 : 0;
            if (kind == PackKind.Normal && hash % 100 < WolfPackPercent && members.Length >= MinWolves)
            {
                for (var i = 0; i < members.Length; i++)
                    members[i] = i < MaxWolves ? PackMember.AshWolf : PackMember.None;
                return members;
            }
            if (depth >= FirstCutthroatDepth && hash / 100 % 100 < CutthroatPercent)
                for (var i = first; i < members.Length; i++)
                    if (members[i] == PackMember.Husk && i % 2 == 1)
                        members[i] = PackMember.Cutthroat;
            if (depth >= FirstAcolyteDepth && hash / 10000 % 100 < AcolytePercent)
                for (var i = first; i < members.Length; i++)
                    if (members[i] == PackMember.Archer)
                        members[i] = PackMember.EmberAcolyte;
            if (depth >= FirstKeeperDepth && members.Length >= 4 && hash / 1000000 % 100 < KeeperPercent)
            {
                for (var i = members.Length - 1; i >= first; i--)
                    if (members[i] != PackMember.None)
                    {
                        members[i] = PackMember.PyreKeeper;
                        break;
                    }
            }
            if (depth >= FirstBloatDepth && (uint)DungeonRules.LevelSeed(levelSeed, 4000 + packIndex) % 100 < BloatPercent)
                for (var i = first; i < members.Length; i++)
                    if (members[i] == PackMember.Ghoul)
                        members[i] = PackMember.CarrionBloat;
            return members;
        }

        /// <summary>Below the first boss the undead spread: 3 points more of packs a level, up to 85 percent (2026-10-05:
        /// the dungeon changes gradually, no hard sections).</summary>
        public static int UndeadPercent(int basePercent, int depth) =>
            System.Math.Min(85, basePercent + 3 * System.Math.Max(0, depth - DungeonRules.BossEvery));

        static PackMember[] RollUndead(int depth, PackKind kind, int count, int levelSeed, int packIndex)
        {
            var members = RollAct1(depth, kind, count, levelSeed, packIndex);
            if (depth < FirstUndeadDepth || kind == PackKind.Elite)
                return members;
            var hash = (uint)DungeonRules.LevelSeed(levelSeed, 2000 + packIndex);
            var skeletons = hash % 100 < UndeadPercent(SkeletonPercent, depth);
            var cultists = hash / 100 % 100 < UndeadPercent(CultistPercent, depth);
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
