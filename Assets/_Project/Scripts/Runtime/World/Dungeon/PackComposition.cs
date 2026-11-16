namespace ARPG
{
    /// <summary>
    /// The dungeon's monsters (2026-10-13, the owner's "enemies concept art", "Replace the roster": the sheet's 25 in
    /// place of the old types). Values are saved nowhere; their names name the enemy definitions (Data/Enemies) and the
    /// special attacks (<see cref="EnemySpecialRules"/>).
    /// </summary>
    public enum PackMember
    {
        /// <summary>Nobody: the pack is smaller than its placement.</summary>
        None,
        Zombie,
        SkeletonWarrior,
        Fallen,
        QuillRat,
        Ghoul,
        DarkCultist,
        BileFiend,
        Wight,
        Mummy,
        BoneMage,
        FleshGolem,
        Wraith,
        Ogre,
        Gargoyle,
        Lich,
        Imp,
        Succubus,
        Harpy,
        StygianDemon,
        HellHound,
        Arachnid,
        SandWorm,
        Giant,
        Nightmare,
        BalrogLite,

        /// <summary>The Retcher (2026-10-17, the owner: "Make a puking enemy that spews bile at the character").</summary>
        Retcher,

        // The start of the game's critters (2026-10-25, the owner: "add enemies: rats, bats that can swipe (close distance
        // quickly and blobs that split when killed", "add them at the start of the game").
        Rat,
        Bat,
        Blob,

        // The forest's own (2026-10-26, the owner: "generate new enemy types, wolfes, spiders and bandits").
        Wolf,
        Spider,
        Bandit,
        BanditArcher,

        // The forest redone (2026-11-02, the owner: "About 10 unique enemies for the forest"; Docs/08). The Bandit is the
        // Cutthroat, the BanditArcher the Bandit Bowman; the Tusked Boar comes with the beasts' rigs.
        Woodcutter,
        HedgeWitch,
        Rotcap,
        BriarThrall,
        Werewolf,
        TuskedBoar,

        /// <summary>The Bell Hermit (2026-11-07, the owner: "design a totally new and original enemy in 16x16"): drawn as
        /// pixel art (ArtSource/pixel/sixteen/bell_hermit_sheets.py), a support whose bell makes the pack hit harder.</summary>
        BellHermit,

        // The Iron Gaol's own (2026-11-14, the owner: "lets start building the next stage. enemies, assets, levels";
        // ten new kinds drawn in 16x16, ArtSource/pixel/sixteen/enemies): warders and what they kept.
        ShackledWretch,
        Turnkey,
        RackBrute,
        IronMaiden,
        CellRat,
        GibbetCrow,
        GaolMastiff,
        CrossbowWarder,
        BrandPriest,
        LampWarden,

        // The Burnt Library's own (2026-11-16, the owner: "start building the third level, the one after the gaol. build
        // enemies, assets, etc."): its monks, what they kept and what burns there.
        AshPenitent,
        CenserMonk,
        TomeGolem,
        ReliquaryGuard,
        Bookworm,
        AshMoth,
        EmberHound,
        QuillScribe,
        FlameCantor,
        CandleBearer,
    }

    /// <summary>Where a kind stands in a pack: in front, at the back, one behind the rest helping them, or in a pack
    /// of its own kind only.</summary>
    public enum PackRole
    {
        Front,
        Back,
        Support,
        Swarm,
    }

    /// <summary>
    /// Decides what a generated pack is made of, slot by slot (2026-10-13, the concept roster). Hashed from the level's
    /// seed and the pack's number, not the generator's stream, so the layout never depends on it. Each kind has a role,
    /// a span of depths and a weight (<see cref="Table"/>): the crypt's dead and fallen to 12, the prison's mummies,
    /// golems and mages from 13, the library's demons from 25, the caves' beasts from 37; each fades in over three
    /// levels from its first depth and is gone after its last, so neighbouring bands mix at their seam. A pack is one
    /// kind of swarm (20 percent, 4 or more slots), or front fighters with 2 or 3 at the back (60 percent of the rest)
    /// and a support behind them (30 percent of packs of 4 or more). Depth 1 has no back row or support: zombies and blobs, or
    /// a swarm of rats or bats (2026-10-25). A Champion keeps slot 0;
    /// elite packs are made elsewhere. Pure.
    /// </summary>
    public static class PackComposition
    {
        public readonly struct Entry
        {
            public readonly PackMember Member;
            public readonly PackRole Role;
            public readonly int First, Last, Weight;

            public Entry(PackMember member, PackRole role, int first, int last, int weight)
            {
                Member = member;
                Role = role;
                First = first;
                Last = last;
                Weight = weight;
            }
        }

        /// <summary>The roster: who, in which role, from which depth to which, how common. Tuning.</summary>
        public static readonly Entry[] Table =
        {
            // The crypt (1 to 12).
            new Entry(PackMember.Zombie, PackRole.Front, 1, 15, 10),
            new Entry(PackMember.SkeletonWarrior, PackRole.Front, 3, 18, 8),
            new Entry(PackMember.Ghoul, PackRole.Front, 4, 16, 6),
            new Entry(PackMember.BileFiend, PackRole.Front, 5, 16, 4),
            new Entry(PackMember.DarkCultist, PackRole.Back, 2, 16, 8),
            new Entry(PackMember.Wight, PackRole.Support, 7, 18, 5),
            new Entry(PackMember.Fallen, PackRole.Swarm, 2, 14, 6),
            new Entry(PackMember.QuillRat, PackRole.Swarm, 3, 15, 5),
            // The Retcher (2026-10-17): at the back, spewing; from the crypt's middle into the prison.
            new Entry(PackMember.Retcher, PackRole.Back, 4, 20, 5),
            // The critters (2026-10-25): from the first level. Claude's numbers, to review.
            new Entry(PackMember.Blob, PackRole.Front, 1, 14, 6),
            new Entry(PackMember.Rat, PackRole.Swarm, 1, 14, 8),
            new Entry(PackMember.Bat, PackRole.Swarm, 1, 16, 6),
            // The forest's (2026-10-26): wolves and spiders in swarms, bandits up front with their bowmen behind,
            // through the whole forest. Claude's numbers, to review.
            new Entry(PackMember.Wolf, PackRole.Swarm, 1, 48, 8),
            new Entry(PackMember.Spider, PackRole.Swarm, 1, 48, 7),
            new Entry(PackMember.Bandit, PackRole.Front, 1, 30, 9),
            new Entry(PackMember.BanditArcher, PackRole.Back, 2, 30, 8),
            // The prison (13 to 24).
            new Entry(PackMember.Mummy, PackRole.Front, 13, 28, 8),
            new Entry(PackMember.FleshGolem, PackRole.Front, 14, 30, 5),
            new Entry(PackMember.Ogre, PackRole.Front, 16, 30, 5),
            new Entry(PackMember.Gargoyle, PackRole.Front, 15, 30, 4),
            new Entry(PackMember.Wraith, PackRole.Back, 13, 30, 6),
            new Entry(PackMember.BoneMage, PackRole.Back, 14, 30, 6),
            new Entry(PackMember.Lich, PackRole.Support, 18, 32, 5),
            // The burnt library (25 to 36).
            new Entry(PackMember.StygianDemon, PackRole.Front, 25, 42, 8),
            new Entry(PackMember.Succubus, PackRole.Back, 26, 42, 7),
            new Entry(PackMember.Imp, PackRole.Swarm, 25, 40, 6),
            new Entry(PackMember.Harpy, PackRole.Swarm, 27, 42, 5),
            new Entry(PackMember.HellHound, PackRole.Swarm, 25, 44, 6),
            // The caves (37 to 48).
            new Entry(PackMember.SandWorm, PackRole.Front, 38, 48, 5),
            new Entry(PackMember.Giant, PackRole.Front, 39, 48, 6),
            new Entry(PackMember.Arachnid, PackRole.Back, 37, 48, 7),
            new Entry(PackMember.BalrogLite, PackRole.Support, 42, 48, 5),
            new Entry(PackMember.Nightmare, PackRole.Swarm, 40, 48, 5),
        };

        /// <summary>
        /// The forest's own roster, alone while every level is a forest (2026-11-03, <see cref="DungeonRules.Forest"/>; the
        /// owner: the forest is depths 1 to 12): wolves and spiders in swarms, cutthroats, woodcutters, rotcaps and briar
        /// thralls up front, bowmen and hedge witches behind, a werewolf leading from depth 4. Claude's numbers, to review.
        /// </summary>
        public static readonly Entry[] ForestTable =
        {
            new Entry(PackMember.Wolf, PackRole.Swarm, 1, 48, 8),
            new Entry(PackMember.Spider, PackRole.Swarm, 1, 48, 7),
            new Entry(PackMember.Rotcap, PackRole.Swarm, 2, 48, 5),
            new Entry(PackMember.Bandit, PackRole.Front, 1, 48, 10),
            new Entry(PackMember.Woodcutter, PackRole.Front, 2, 48, 5),
            new Entry(PackMember.BriarThrall, PackRole.Front, 3, 48, 5),
            // The boar charges alone or in pairs (the owner, 2026-11-02): a swarm of its own kind, made small below.
            new Entry(PackMember.TuskedBoar, PackRole.Swarm, 3, 48, 3),
            new Entry(PackMember.BanditArcher, PackRole.Back, 2, 48, 8),
            new Entry(PackMember.HedgeWitch, PackRole.Back, 3, 48, 5),
            new Entry(PackMember.Werewolf, PackRole.Support, 4, 48, 4),
            new Entry(PackMember.BellHermit, PackRole.Support, 2, 48, 4),
        };

        /// <summary>
        /// The Iron Gaol's own roster (2026-11-14), alone on its levels (13 to 24) when the level is not a forest: shackled
        /// wretches, turnkeys, rack brutes and iron maidens up front, crossbow warders and brand priests behind, a lamp
        /// warden leading, cell rats, gibbet crows and gaol mastiffs in swarms. Each kind arrives a level or two after
        /// the last, so the gaol changes on the way down. Claude's numbers, to review.
        /// </summary>
        public static readonly Entry[] GaolTable =
        {
            new Entry(PackMember.ShackledWretch, PackRole.Front, 13, 24, 10),
            new Entry(PackMember.Turnkey, PackRole.Front, 13, 24, 7),
            new Entry(PackMember.RackBrute, PackRole.Front, 15, 24, 5),
            new Entry(PackMember.IronMaiden, PackRole.Front, 17, 24, 3),
            new Entry(PackMember.CellRat, PackRole.Swarm, 13, 24, 8),
            new Entry(PackMember.GibbetCrow, PackRole.Swarm, 14, 24, 6),
            new Entry(PackMember.GaolMastiff, PackRole.Swarm, 15, 24, 6),
            new Entry(PackMember.CrossbowWarder, PackRole.Back, 13, 24, 8),
            new Entry(PackMember.BrandPriest, PackRole.Back, 14, 24, 6),
            new Entry(PackMember.LampWarden, PackRole.Support, 14, 24, 5),
        };

        /// <summary>
        /// The Burnt Library's own roster (2026-11-16), alone on its levels (25 to 36) when the level is not a forest: ash
        /// penitents, censer monks, tome golems and reliquary guards up front, quill scribes and flame cantors behind, a
        /// candle bearer leading, bookworms, ash moths and ember hounds in swarms. Each kind arrives a level or two after
        /// the last. Claude's numbers, to review.
        /// </summary>
        public static readonly Entry[] LibraryTable =
        {
            new Entry(PackMember.AshPenitent, PackRole.Front, 25, 36, 10),
            new Entry(PackMember.CenserMonk, PackRole.Front, 25, 36, 7),
            new Entry(PackMember.TomeGolem, PackRole.Front, 27, 36, 5),
            new Entry(PackMember.ReliquaryGuard, PackRole.Front, 29, 36, 3),
            new Entry(PackMember.Bookworm, PackRole.Swarm, 25, 36, 8),
            new Entry(PackMember.AshMoth, PackRole.Swarm, 26, 36, 6),
            new Entry(PackMember.EmberHound, PackRole.Swarm, 27, 36, 6),
            new Entry(PackMember.QuillScribe, PackRole.Back, 25, 36, 8),
            new Entry(PackMember.FlameCantor, PackRole.Back, 26, 36, 6),
            new Entry(PackMember.CandleBearer, PackRole.Support, 26, 36, 5),
        };

        /// <summary>The roster in force at a depth: the forest's on a forest level, the gaol's on the Iron Gaol's levels,
        /// the library's on the Burnt Library's, else the old concept roster by band.</summary>
        public static Entry[] RosterAt(int depth) =>
            DungeonRules.Forest ? ForestTable : DungeonRules.GaolAt(depth) ? GaolTable : DungeonRules.LibraryAt(depth) ? LibraryTable : Table;

        /// <summary>The front fighter a level falls back on when a role has nobody.</summary>
        static PackMember Fallback(int depth) =>
            DungeonRules.Forest ? PackMember.Bandit : DungeonRules.GaolAt(depth) ? PackMember.ShackledWretch :
            DungeonRules.LibraryAt(depth) ? PackMember.AshPenitent : PackMember.Zombie;

        public const int SwarmPercent = 20;
        // More packs with a ranged back row, and a bigger one (2026-10-17, steady pressure; was 60 percent, 2 or 3).
        public const int BackPercent = 85;
        public const int MinBack = 3;
        public const int SupportPercent = 30;
        public const int MinSwarm = 4;
        public const int MaxSwarm = 6;

        /// <summary>A kind's weight at a depth: none outside its span, rising over its first three levels.</summary>
        public static int WeightAt(Entry entry, int depth) =>
            depth < entry.First || depth > entry.Last ? 0 : entry.Weight * System.Math.Min(3, depth - entry.First + 1);

        /// <summary>The kinds of a role met at a depth.</summary>
        public static bool Has(PackRole role, int depth)
        {
            foreach (var entry in RosterAt(depth))
                if (entry.Role == role && WeightAt(entry, depth) > 0)
                    return true;
            return false;
        }

        public static PackMember[] Roll(int depth, PackKind kind, int count, int levelSeed, int packIndex)
        {
            depth = System.Math.Max(1, depth);
            var members = new PackMember[count];
            var hash = (uint)DungeonRules.LevelSeed(levelSeed, 1000 + packIndex);
            var first = kind == PackKind.WithChampion ? 1 : 0;
            var front = Pick(PackRole.Front, depth, hash);
            for (var i = 0; i < count; i++)
                members[i] = front;
            if (kind == PackKind.Elite || count < 3)
                return members;

            if (kind == PackKind.Normal && count >= MinSwarm && hash / 7 % 100 < SwarmPercent && Has(PackRole.Swarm, depth))
            {
                var swarm = Pick(PackRole.Swarm, depth, hash / 11);
                // Boars come alone or in pairs (the owner, 2026-11-02).
                var size = swarm == PackMember.TuskedBoar ? 1 + (int)(hash / 31 % 2) : MaxSwarm;
                for (var i = 0; i < count; i++)
                    members[i] = i < size ? swarm : PackMember.None;
                return members;
            }
            if (depth < 2)
                return members;
            var end = count;
            if (count >= 4 && hash / 13 % 100 < SupportPercent && Has(PackRole.Support, depth))
            {
                members[count - 1] = Pick(PackRole.Support, depth, hash / 17);
                end = count - 1;
            }
            if (hash / 19 % 100 < BackPercent && Has(PackRole.Back, depth))
            {
                var back = Pick(PackRole.Back, depth, hash / 23);
                var extra = System.Math.Min(MinBack + (int)(hash / 29 % 2), end - first - 1);
                for (var i = System.Math.Max(first, end - extra); i < end; i++)
                    members[i] = back;
            }
            return members;
        }

        /// <summary>A kind of a role at a depth, by weight from a hash; zombies when the role has nobody there.</summary>
        static PackMember Pick(PackRole role, int depth, uint hash)
        {
            var total = 0;
            foreach (var entry in RosterAt(depth))
                if (entry.Role == role)
                    total += WeightAt(entry, depth);
            if (total == 0)
                return Fallback(depth);
            var roll = (int)(hash % (uint)total);
            foreach (var entry in RosterAt(depth))
            {
                if (entry.Role != role)
                    continue;
                roll -= WeightAt(entry, depth);
                if (roll < 0)
                    return entry.Member;
            }
            return Fallback(depth);
        }

        /// <summary>The role of a kind (swarms are their own packs).</summary>
        public static PackRole RoleOf(PackMember member)
        {
            foreach (var entry in DungeonRules.Forest ? ForestTable : GaolTable)
                if (entry.Member == member)
                    return entry.Role;
            foreach (var entry in GaolTable)
                if (entry.Member == member)
                    return entry.Role;
            foreach (var entry in LibraryTable)
                if (entry.Member == member)
                    return entry.Role;
            foreach (var entry in Table)
                if (entry.Member == member)
                    return entry.Role;
            return PackRole.Front;
        }
    }
}
