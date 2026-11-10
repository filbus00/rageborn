using System;
using System.Collections.Generic;

namespace ARPG
{
    /// <summary>
    /// The Wild Arrow Company (the owner, 2026-11-03: "remove the town. Everything done in the town will now be done in a
    /// starting menu. The story changes. You are a mercenary. You select missions on a map ... You work for a mercenary
    /// company called wild arrow. They only hire female amazonian bow warriors"). A contract is an area of the old
    /// dungeon's depths taken as one mission: it is cleared when its last boss falls (every boss before it lies on the
    /// way down), and clearing it opens the next on the map. Pure.
    /// </summary>
    public sealed class Contract
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Region;
        public readonly int FirstDepth;
        public readonly int LastDepth;

        /// <summary>Laid out as the forest (<see cref="DungeonRules.Forest"/>); the others as the old themes by depth.</summary>
        public readonly bool Forest;

        public readonly string[] Bosses;
        public readonly string Story;

        public Contract(string id, string name, string region, int firstDepth, int lastDepth, bool forest, string[] bosses, string story)
        {
            Id = id;
            Name = name;
            Region = region;
            FirstDepth = firstDepth;
            LastDepth = lastDepth;
            Forest = forest;
            Bosses = bosses;
            Story = story;
        }

        public int Depths => LastDepth - FirstDepth + 1;
    }

    /// <summary>The company's contracts on the map, in the order they open, and the story behind them.</summary>
    public static class ContractRules
    {
        public const string CompanyName = "The Wild Arrow Company";

        /// <summary>The company, in a few lines (the menu's front page).</summary>
        public const string CompanyStory =
            "The Wild Arrow Company hires one kind of soldier: Amazon bow warriors, sworn to the string and paid in coin. " +
            "Each contract is taken by a single archer. Those who come back are paid; those who do not are honoured, " +
            "their names cut into the company's grave stone.";

        public static readonly Contract[] All =
        {
            new Contract("thornwood", "The Thornwood", "The Duchy of Vell", 1, 12, true, new[] { "The Bandit King", "The Brood Mother" },
                "The Thornwood was a woodcutters' road until a cutthroat crowned himself in the old logging camp. The Bandit " +
                "King's gang now robs every cart on it, and something older stirs under the canopy: the Brood Mother, whose " +
                "spiders have begun taking the villagers who wander too far. The Duchy of Vell pays the company to clear the " +
                "wood: break the King's gang, then burn out the Brood Mother's nest at the forest's heart."),
            new Contract("iron_gaol", "The Iron Gaol", "Beneath the Thornwood", 13, 24, false, new[] { "The Gaoler", "The Hanged Magistrate" },
                "Under the Brood Mother's nest the spiders had dug into the Iron Gaol, the dukes' old prison, sealed when its " +
                "warden went mad. The doors are open now. The Gaoler still walks his rounds with his keys, and below him the " +
                "Hanged Magistrate passes sentence on the living. Close the Gaol before what it held reaches Vell."),
            new Contract("burnt_library", "The Burnt Library", "The Cinder Archives", 25, 36, false, new[] { "Saint Marrow", "The Ash Archivist" },
                "The Magistrate's ledgers named a library below the Gaol, burnt a century ago and still smouldering. Its monks " +
                "never died; Saint Marrow leads them in prayer to something under the shelves, and the Ash Archivist guards " +
                "what they wrote down. The company has been offered a fortune for the last unburnt book."),
            new Contract("rift_caves", "The Rift Caves", "The World's Wound", 37, 48, false, new[] { "The Rift Mother", "The First Watchman" },
                "Beneath the library the rock splits open into the Rift, where the demons of the old stories climb out of the " +
                "dark. The Rift Mother births them; the First Watchman, who guarded the wound before there were dukes, has " +
                "turned. The last contract: close the Rift for good."),
        };

        public static Contract Get(string id)
        {
            foreach (var contract in All)
                if (contract.Id == id)
                    return contract;
            return All[0];
        }

        /// <summary>
        /// What a depth is called on screen (2026-11-10, the owner: "call it something else than depth. Maybe level.
        /// Maybe level 1:1 - 2 -3 etc."): "Level " the contract's number, a dash, and the level within it, so the
        /// Thornwood's depths are Level 1-1 to Level 1-12 and the Iron Gaol's Level 2-1 to 2-12.
        /// </summary>
        public static string LevelName(int depth) => "Level " + LevelCode(depth);

        /// <summary>The level's number alone: "1-1".</summary>
        public static string LevelCode(int depth)
        {
            var contract = At(depth);
            return $"{Array.IndexOf(All, contract) + 1}-{Math.Max(1, depth - contract.FirstDepth + 1)}";
        }

        /// <summary>The contract a depth belongs to.</summary>
        public static Contract At(int depth)
        {
            foreach (var contract in All)
                if (depth >= contract.FirstDepth && depth <= contract.LastDepth)
                    return contract;
            return All[All.Length - 1];
        }

        /// <summary>Open on the map: the first, and every one whose predecessor has been cleared.</summary>
        public static bool IsOpen(Contract contract, ICollection<string> cleared)
        {
            var index = Array.IndexOf(All, contract);
            return index <= 0 || cleared.Contains(All[index - 1].Id);
        }
    }

    /// <summary>
    /// Names for the company's archers (the owner: "Make a random name generator. Each character gets a first and
    /// surname"): an Amazon's first name, from the old stories or made from their sounds, and a surname the company gave
    /// her, two words run together (Ravenfletch, Thornstring). Pure, seeded.
    /// </summary>
    public static class MercenaryNames
    {
        static readonly string[] Storied =
        {
            "Hippolyta", "Penthesilea", "Antiope", "Melanippe", "Otrera", "Lysippe", "Myrina", "Thalestris", "Orithyia", "Aella",
            "Alkippe", "Antianeira", "Areto", "Asteria", "Bremusa", "Klonie", "Derinoe", "Evandre", "Harmothoe", "Hippothoe",
            "Iphito", "Kleite", "Molpadia", "Polemusa", "Thermodosa", "Toxaris", "Xanthippe", "Kyme", "Lampado", "Marpesia",
            "Okyale", "Pantariste", "Thraso", "Euryale", "Ainia", "Andro", "Glauke", "Philippis", "Iole", "Tecmessa",
        };

        static readonly string[] Starts =
        {
            "Hip", "Ant", "Mel", "Ly", "Thal", "Or", "Pen", "Ae", "Kle", "Mol", "Ther", "Tox", "Xan", "Kyr", "Lam", "Mar", "Ok",
            "Eur", "Ast", "Bre", "Der", "Ev", "Har", "Iph", "Pol", "Ser", "Zo", "Nik", "Phil", "Rho", "Kal", "Tal",
        };

        static readonly string[] Middles = { "a", "i", "o", "e", "y", "ae", "io", "ea" };

        static readonly string[] Ends =
        {
            "ppe", "lyta", "nippe", "tris", "ra", "sia", "the", "nia", "ne", "sa", "ope", "dra", "lia", "xa", "thea", "mache",
            "noe", "dike", "leia", "ris",
        };

        static readonly string[] SurnameFirst =
        {
            "Ash", "Thorn", "Storm", "Raven", "Iron", "Swift", "Red", "Wolf", "Bright", "Grey", "Black", "Hawk", "Oak", "Bone",
            "Moon", "Ember", "Frost", "Wild", "Stone", "Briar", "Crow", "Elk", "Dusk", "Gold", "Wind", "Rook", "Vine", "Flint",
        };

        static readonly string[] SurnameSecond =
        {
            "arrow", "string", "feather", "heart", "brand", "mane", "fletch", "bow", "blood", "eye", "quill", "shot", "hunter",
            "wind", "spear", "claw", "song", "fang", "stride", "draw", "mark", "flight", "watch", "thorn", "hide",
        };

        public static string First(Random random)
        {
            if (random.Next(2) == 0)
                return Storied[random.Next(Storied.Length)];
            return Starts[random.Next(Starts.Length)] + Middles[random.Next(Middles.Length)] + Ends[random.Next(Ends.Length)];
        }

        public static string Surname(Random random)
        {
            var first = SurnameFirst[random.Next(SurnameFirst.Length)];
            var second = SurnameSecond[random.Next(SurnameSecond.Length)];
            return first + second;
        }

        public static string Full(Random random) => First(random) + " " + Surname(random);

        public static string Full(int seed) => Full(new Random(seed));
    }

    /// <summary>An archer who fell on a contract, honoured on the company's grave list.</summary>
    public sealed class FallenMercenary
    {
        public string Name = "";
        public string Contract = "";
        public int Depth;
        public int Kills;
        public int Bosses;
        public float Seconds;

        /// <summary>The day she fell, in days since 1970 (the list shows the date).</summary>
        public int Day;
    }

    /// <summary>
    /// The company's record (kept for good, across runs): the archer now under contract, the contract she is on, the
    /// contracts cleared and the fallen (the owner: "When a character dies. It is honored by being displayed on a grave
    /// list 'leaderboard'"). Pure.
    /// </summary>
    public sealed class CompanyRecord
    {
        /// <summary>How many of the fallen the grave list keeps (the best of them).</summary>
        public const int GraveListSize = 50;

        public string Mercenary = "";
        public string ContractId = ContractRules.All[0].Id;
        public readonly HashSet<string> Cleared = new HashSet<string>();
        public readonly List<FallenMercenary> Fallen = new List<FallenMercenary>();

        public Contract Contract => ContractRules.Get(ContractId);

        /// <summary>A new archer signs on (the first, and after every death).</summary>
        public void Hire(Random random) => Mercenary = MercenaryNames.Full(random);

        /// <summary>The archer under contract fell: her name goes on the grave list, a new one is hired.</summary>
        public FallenMercenary Bury(int depth, RunStats run, int day, Random random)
        {
            var fallen = new FallenMercenary
            {
                Name = Mercenary,
                Contract = Contract.Name,
                Depth = depth,
                Kills = run.Kills,
                Bosses = run.Bosses,
                Seconds = run.Seconds,
                Day = day,
            };
            Fallen.Add(fallen);
            GraveRules.Rank(Fallen);
            if (Fallen.Count > GraveListSize)
                Fallen.RemoveRange(GraveListSize, Fallen.Count - GraveListSize);
            Hire(random);
            return fallen;
        }
    }

    /// <summary>The grave list's order: deepest first, then the most kills, then the most bosses.</summary>
    public static class GraveRules
    {
        public static void Rank(List<FallenMercenary> fallen) =>
            fallen.Sort((a, b) => a.Depth != b.Depth ? b.Depth.CompareTo(a.Depth)
                : a.Kills != b.Kills ? b.Kills.CompareTo(a.Kills)
                : b.Bosses.CompareTo(a.Bosses));
    }
}
