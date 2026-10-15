using System.Collections.Generic;

namespace ARPG
{
    /// <summary>What a run has done so far (2026-10-15, for the run summary): kills, bosses, coins picked up, items and
    /// blueprints found, time in the dungeon. A new run starts at zero. Saved, so a resumed run keeps them. Pure.</summary>
    public sealed class RunStats
    {
        public int Kills;
        public int Bosses;
        public int Coins;
        public int Items;
        public int Blueprints;
        public float Seconds;

        /// <summary>The deepest depth ever when the run began, to tell a new record at its end.</summary>
        public int RecordBefore;
    }

    /// <summary>How a run ended.</summary>
    public enum RunEnd
    {
        Died,
        Portal,
        Won,
    }

    /// <summary>
    /// The run summary (the owner, 2026-10-15, building it while the owner plays): what a run came to, shown in town when
    /// it ends, in place of the one-line notice. Taken from the ending run's session before the next replaces it. Pure.
    /// </summary>
    public sealed class RunSummary
    {
        public RunEnd End;
        public int Depth;
        public int DeepestEver;
        public bool NewRecord;
        public RunStats Stats;
        public int CoinsNow;
        public int ItemsHome;
        public int BlueprintsLearned;
        public readonly List<(string name, int rank)> Boons = new List<(string, int)>();

        public static RunSummary Of(GameSession ending, RunEnd end, int blueprintsLearned, GameSession next)
        {
            var summary = new RunSummary
            {
                End = end,
                Depth = ending.DeepestDepth,
                DeepestEver = ending.Meta.DeepestEver,
                NewRecord = ending.DeepestDepth > ending.Run.RecordBefore,
                Stats = ending.Run,
                CoinsNow = next.Gold,
                BlueprintsLearned = blueprintsLearned,
            };
            if (end != RunEnd.Died)
            {
                summary.ItemsHome = next.Inventory.Items.Count;
                foreach (ItemSlot place in System.Enum.GetValues(typeof(ItemSlot)))
                    if (next.Equipment.Get(place) != null)
                        summary.ItemsHome++;
            }
            foreach (var node in TalentRules.Nodes)
            {
                var rank = ending.Talents.RankOf(node.Id);
                if (rank > 0)
                    summary.Boons.Add((node.Name, rank));
            }
            return summary;
        }

        public string Title => End switch
        {
            RunEnd.Won => "The Run Is Won",
            RunEnd.Portal => "Home Through the Portal",
            _ => "The Run Is Over",
        };

        /// <summary>The line under the title.</summary>
        public string Headline => End switch
        {
            RunEnd.Won => "The First Watchman has fallen.",
            RunEnd.Portal => $"Out of depth {Depth} with everything you carried.",
            _ => $"Fallen at depth {Depth}. All you wore and carried is lost.",
        } + (NewRecord ? "  A new deepest depth." : "");

        public static string Duration(float seconds)
        {
            var total = (int)seconds;
            return total >= 3600 ? $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60}:{total % 60:00}";
        }

        /// <summary>The numbers, one line each.</summary>
        public IEnumerable<string> Lines()
        {
            yield return $"Depth reached  {Depth}   ·   deepest ever  {DeepestEver}";
            yield return $"Time  {Duration(Stats.Seconds)}";
            yield return $"Enemies killed  {Stats.Kills:N0}" + (Stats.Bosses > 0 ? $"   ·   bosses  {Stats.Bosses}" : "");
            yield return $"Coins found  {Stats.Coins:N0}   ·   coins now  {CoinsNow:N0}";
            yield return $"Items found  {Stats.Items}" + (End != RunEnd.Died ? $"   ·   brought home  {ItemsHome}" : "");
            if (Stats.Blueprints > 0 || BlueprintsLearned > 0)
                yield return $"Blueprints found  {Stats.Blueprints}" + (End != RunEnd.Died ? $"   ·   new for Thorin  {BlueprintsLearned}" : "   ·   lost");
        }
    }
}
