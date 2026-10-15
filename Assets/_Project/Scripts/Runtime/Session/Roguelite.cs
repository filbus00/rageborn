using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    // The roguelite (the owner, 2026-10-13: "I want to make this more into a roguelite"; answers one at a time): a run is
    // the whole 48-depth descent from depth 1, straight down (no town portal, no waypoints), and ends at death or at the
    // last boss. There is no experience: every new depth offers three random boons and she takes one. Gear drops and is
    // lost when the run ends, but one item a run can be sent to the stash, whose slots are bought dearly. The coins she
    // picks up are kept, and in town they buy permanent upgrades: base stats, the boon pool, pets and the starting kit.
    // No quests. Pure.

    /// <summary>The town's permanent upgrades.</summary>
    public enum MetaUpgradeId
    {
        Vitality,
        Might,
        Precision,
        Swiftness,
        Evasion,
        PotionCharges,
        PotionStrength,
        Reroll,
        FourthChoice,
        HiddenLore,
        StartBow,
        StartQuiver,
        StartArmour,
        StashSlot,
    }

    /// <summary>Who sells an upgrade.</summary>
    public enum MetaShop
    {
        /// <summary>Brunhilde, the guard captain (the Trainer's place).</summary>
        Training,

        /// <summary>Miranda, the innkeeper (the Healer's place).</summary>
        Potions,

        /// <summary>Sylthara, the informant (the Gambler's place): the boon pool.</summary>
        Secrets,

        /// <summary>Garrett, the merchant: the starting kit.</summary>
        Outfitting,

        /// <summary>Elara, the scholar (the Stash Keeper's place).</summary>
        Stash,
    }

    public sealed class MetaUpgrade
    {
        public MetaUpgradeId Id;
        public MetaShop Shop;
        public string Name;

        /// <summary>What a rank gives, with {0} for the total at the next rank.</summary>
        public string Text;

        public int MaxRanks = 1;
        public int BaseCost;

        /// <summary>Each rank costs this much more than the last (a factor).</summary>
        public float Growth = 2f;

        /// <summary>The bonus a rank adds, in <see cref="PassiveBonuses"/>' units, when <see cref="Effect"/> is set.</summary>
        public PassiveEffect Effect;
        public float PerRank;
    }

    /// <summary>The upgrades and their prices. Claude's numbers, to review (Docs/08). Pure.</summary>
    public static class MetaRules
    {
        public static readonly IReadOnlyList<MetaUpgrade> All = new List<MetaUpgrade>
        {
            new MetaUpgrade { Id = MetaUpgradeId.Vitality, Shop = MetaShop.Training, Name = "Vitality", Text = "{0} maximum life.", MaxRanks = 5, BaseCost = 300, Effect = PassiveEffect.LifePercent, PerRank = 0.06f },
            new MetaUpgrade { Id = MetaUpgradeId.Might, Shop = MetaShop.Training, Name = "Might", Text = "{0} more damage.", MaxRanks = 5, BaseCost = 300, Effect = PassiveEffect.IncreasedDamage, PerRank = 0.05f },
            new MetaUpgrade { Id = MetaUpgradeId.Precision, Shop = MetaShop.Training, Name = "Precision", Text = "{0} critical strike chance.", MaxRanks = 5, BaseCost = 400, Effect = PassiveEffect.CriticalChance, PerRank = 1.5f },
            new MetaUpgrade { Id = MetaUpgradeId.Swiftness, Shop = MetaShop.Training, Name = "Swiftness", Text = "{0} movement speed.", MaxRanks = 3, BaseCost = 500, Effect = PassiveEffect.MoveSpeed, PerRank = 0.03f },
            new MetaUpgrade { Id = MetaUpgradeId.Evasion, Shop = MetaShop.Training, Name = "Evasion", Text = "{0} chance to dodge.", MaxRanks = 3, BaseCost = 600, Effect = PassiveEffect.Dodge, PerRank = 0.02f },
            new MetaUpgrade { Id = MetaUpgradeId.PotionCharges, Shop = MetaShop.Potions, Name = "Deeper Flasks", Text = "{0} potion charges.", MaxRanks = 3, BaseCost = 500, Growth = 2.5f },
            new MetaUpgrade { Id = MetaUpgradeId.PotionStrength, Shop = MetaShop.Potions, Name = "Strong Brew", Text = "Potions heal {0} more.", MaxRanks = 3, BaseCost = 400, Effect = PassiveEffect.PotionHeal, PerRank = 0.15f },
            new MetaUpgrade { Id = MetaUpgradeId.Reroll, Shop = MetaShop.Secrets, Name = "Second Thoughts", Text = "{0} rerolls of the boon choice a run.", MaxRanks = 3, BaseCost = 800, Growth = 2.5f },
            new MetaUpgrade { Id = MetaUpgradeId.FourthChoice, Shop = MetaShop.Secrets, Name = "A Fourth Path", Text = "Four boons to choose from, not three.", MaxRanks = 1, BaseCost = 3000 },
            new MetaUpgrade { Id = MetaUpgradeId.HiddenLore, Shop = MetaShop.Secrets, Name = "Hidden Lore", Text = "The rarer boons can be offered (talent rows up to {0}).", MaxRanks = 2, BaseCost = 2500, Growth = 2.4f },
            new MetaUpgrade { Id = MetaUpgradeId.StartBow, Shop = MetaShop.Outfitting, Name = "A Better Bow", Text = "Start each run with a {0} bow.", MaxRanks = 2, BaseCost = 1000, Growth = 4f },
            new MetaUpgrade { Id = MetaUpgradeId.StartQuiver, Shop = MetaShop.Outfitting, Name = "A Better Quiver", Text = "Start each run with a {0} quiver.", MaxRanks = 2, BaseCost = 800, Growth = 4f },
            new MetaUpgrade { Id = MetaUpgradeId.StartArmour, Shop = MetaShop.Outfitting, Name = "Travelling Armour", Text = "Start each run wearing a {0} chest.", MaxRanks = 2, BaseCost = 1200, Growth = 4f },
            // The stash chest (the owner, 2026-10-15: "A stash (a chest) in town is available and can be expanded with
            // coin"): ten slots free, ten more a rank.
            new MetaUpgrade { Id = MetaUpgradeId.StashSlot, Shop = MetaShop.Stash, Name = "A Bigger Chest", Text = "{0} stash slots.", MaxRanks = 5, BaseCost = 500, Growth = 2f },
        };

        public static MetaUpgrade Get(MetaUpgradeId id)
        {
            foreach (var upgrade in All)
                if (upgrade.Id == id)
                    return upgrade;
            return null;
        }

        /// <summary>The price of the next rank after <paramref name="rank"/> ranks owned.</summary>
        public static int Cost(MetaUpgradeId id, int rank)
        {
            var upgrade = Get(id);
            return upgrade == null ? 0 : Mathf.RoundToInt(upgrade.BaseCost * Mathf.Pow(upgrade.Growth, rank) / 10f) * 10;
        }

        /// <summary>A rarity's word for the starting kit's ranks.</summary>
        public static ItemRarity KitRarity(int rank) => rank >= 2 ? ItemRarity.Rare : rank == 1 ? ItemRarity.Magic : ItemRarity.Common;

        /// <summary>An upgrade's text at a total rank (what it gives with that many ranks).</summary>
        public static string Describe(MetaUpgrade upgrade, int rank)
        {
            string value;
            switch (upgrade.Id)
            {
                case MetaUpgradeId.PotionCharges:
                case MetaUpgradeId.Reroll:
                    value = "+" + rank;
                    break;
                case MetaUpgradeId.StashSlot:
                    value = (MetaProgress.BaseStashSlots + MetaProgress.StashSlotsPerRank * rank).ToString();
                    break;
                case MetaUpgradeId.HiddenLore:
                    value = (MetaProgress.BaseBoonTier + 2 * rank).ToString();
                    break;
                case MetaUpgradeId.StartBow:
                case MetaUpgradeId.StartQuiver:
                case MetaUpgradeId.StartArmour:
                    value = KitRarity(rank).ToString().ToLowerInvariant();
                    break;
                default:
                    var total = upgrade.PerRank * rank;
                    value = upgrade.Effect == PassiveEffect.CriticalChance ? $"+{total:0.#}%" : $"+{total * 100f:0}%";
                    break;
            }
            return string.Format(upgrade.Text, value);
        }
    }

    /// <summary>What survives every run: the upgrades bought, how many runs and the deepest depth ever reached. The coins
    /// are her gold, carried from one run to the next (<see cref="GameSession.NextRun"/>). Saved.</summary>
    public sealed class MetaProgress
    {
        /// <summary>Boons come from talent rows below this (rows 0 and 1) until Hidden Lore opens deeper ones.</summary>
        public const int BaseBoonTier = 2;

        readonly Dictionary<MetaUpgradeId, int> ranks = new Dictionary<MetaUpgradeId, int>();

        public event Action Changed;

        public int Runs { get; set; }
        public int DeepestEver { get; set; }

        public int Rank(MetaUpgradeId id) => ranks.TryGetValue(id, out var rank) ? rank : 0;

        public IEnumerable<KeyValuePair<MetaUpgradeId, int>> Ranks => ranks;

        public bool IsMaxed(MetaUpgradeId id) => Rank(id) >= (MetaRules.Get(id)?.MaxRanks ?? 0);

        public int NextCost(MetaUpgradeId id) => MetaRules.Cost(id, Rank(id));

        /// <summary>Sets a rank from a save (clamped to the upgrade's ranks).</summary>
        public void Restore(MetaUpgradeId id, int rank)
        {
            var upgrade = MetaRules.Get(id);
            if (upgrade == null || rank <= 0)
                return;
            ranks[id] = Mathf.Min(rank, upgrade.MaxRanks);
        }

        /// <summary>Raises a rank (the buying is the session's, which holds the coins).</summary>
        public bool Raise(MetaUpgradeId id)
        {
            if (IsMaxed(id))
                return false;
            ranks[id] = Rank(id) + 1;
            Changed?.Invoke();
            return true;
        }

        /// <summary>The upgrades' passive bonuses, added on top of the boons'.</summary>
        public void AddTo(PassiveBonuses bonuses)
        {
            foreach (var pair in ranks)
            {
                var upgrade = MetaRules.Get(pair.Key);
                if (upgrade != null && upgrade.Effect != PassiveEffect.None)
                    bonuses.Add(upgrade.Effect, upgrade.PerRank * pair.Value);
            }
        }

        public int ExtraPotionCharges => Rank(MetaUpgradeId.PotionCharges);
        public int RerollsPerRun => Rank(MetaUpgradeId.Reroll);
        public int BoonChoices => 3 + Rank(MetaUpgradeId.FourthChoice);
        public int MaxBoonTier => BaseBoonTier + 2 * Rank(MetaUpgradeId.HiddenLore);
        public const int BaseStashSlots = 10;
        public const int StashSlotsPerRank = 10;
        public int StashSlots => BaseStashSlots + StashSlotsPerRank * Rank(MetaUpgradeId.StashSlot);

        // Thorin's blueprints (2026-10-15): the keys he knows (BlueprintRules.Key), and the item forged for each kind,
        // which she starts a run wearing after a death. Saved.
        readonly HashSet<string> blueprints = new HashSet<string>();
        readonly Dictionary<ItemSlot, Item> forged = new Dictionary<ItemSlot, Item>();

        public IReadOnlyCollection<string> Blueprints => blueprints;
        public bool Knows(string key) => key != null && blueprints.Contains(key);

        public bool Learn(string key)
        {
            if (!BlueprintRules.TryParse(key, out _, out _) || !blueprints.Add(key))
                return false;
            Changed?.Invoke();
            return true;
        }

        public IEnumerable<KeyValuePair<ItemSlot, Item>> Forged => forged;
        public Item ForgedFor(ItemSlot kind) => forged.TryGetValue(kind, out var item) ? item : null;

        public void SetForged(Item item)
        {
            if (item == null)
                return;
            forged[item.Slot] = item;
            Changed?.Invoke();
        }

        public void RecordDepth(int depth)
        {
            if (depth > DeepestEver)
                DeepestEver = depth;
        }
    }

    /// <summary>Which boons a choice offers. A boon is a rank of a talent node (Talents.cs: a skill, an improvement of a
    /// learned skill, or a passive), so every boon's effect is the talent's. Pure.</summary>
    public static class BoonRules
    {
        /// <summary>Up to <paramref name="count"/> different nodes she can take another rank of: not maxed, its required
        /// node learned, its row below the meta's limit.</summary>
        public static List<string> Offer(TalentState talents, int maxTier, int count, System.Random random)
        {
            var open = new List<string>();
            foreach (var node in TalentRules.Nodes)
                if (CanTake(talents, node, maxTier))
                    open.Add(node.Id);
            var picked = new List<string>();
            while (picked.Count < count && open.Count > 0)
            {
                var i = random.Next(open.Count);
                picked.Add(open[i]);
                open.RemoveAt(i);
            }
            return picked;
        }

        public static bool CanTake(TalentState talents, TalentNode node, int maxTier) =>
            node.Tier < maxTier && talents.RankOf(node.Id) < node.MaxRanks &&
            (node.Requires == null || talents.RankOf(node.Requires) > 0);

        /// <summary>The level she fights at on a depth: the depth's enemy level (no experience; she keeps pace).</summary>
        public static int LevelAt(int depth) => Mathf.Max(1, DungeonRules.EnemyLevel(depth));
    }
}
