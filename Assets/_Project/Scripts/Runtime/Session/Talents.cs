using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>The Wild Arrow's three talent trees (the owner, 2026-10-07: "Hunter-style").</summary>
    public enum TalentTree
    {
        Marksmanship,
        BeastMastery,
        Survival,
    }

    /// <summary>
    /// One talent: a passive bonus with ranks, or a node that teaches an active skill (one rank). A skill's
    /// "Improved" talent raises that skill's damage instead (<see cref="SkillDamage"/>).
    /// </summary>
    public sealed class TalentNode
    {
        public string Id;
        public TalentTree Tree;

        /// <summary>The row, 0 at the bottom: row n opens with <see cref="TalentRules.PointsPerTier"/> times n points spent
        /// in the tree.</summary>
        public int Tier;

        /// <summary>The column the screen draws it in, 0 to 2.</summary>
        public int Column;

        public string Name;
        public int MaxRanks = 1;

        /// <summary>The passive bonus, with this much a rank (in <see cref="PassiveBonuses"/>' units).</summary>
        public PassiveEffect Effect;
        public float PerRank;

        /// <summary>A second bonus, for the capstones that give two things.</summary>
        public PassiveEffect Effect2;
        public float PerRank2;

        /// <summary>The active skill this node teaches (its asset name), or null.</summary>
        public string Skill;

        /// <summary>A skill whose damage this node raises by <see cref="SkillDamage"/> a rank, or null.</summary>
        public string ImprovesSkill;
        public float SkillDamage;

        /// <summary>A node that must be at its full rank first, or null.</summary>
        public string Requires;

        /// <summary>What it does, for the screen; {0} is the value at the current rank (or the first).</summary>
        public string Text;

        public bool IsSkill => Skill != null;
    }

    /// <summary>
    /// The talent trees' content and rules (Docs/02, 2026-10-07): she starts with only her bow; a point a level from level
    /// 2 is spent in three trees, mostly passive talents with ranks and a few nodes that teach the active skills, which
    /// then fire on their own (no loadout). A row opens with 5 points spent in its tree, the last (a capstone skill or
    /// bonus) with 25. Pure.
    /// </summary>
    public static class TalentRules
    {
        public const int PointsPerTier = 5;
        public const int Tiers = 6;

        /// <summary>Points earned by a character level: one a level from 2 (the owner: "1 per level from level 2").</summary>
        public static int EarnedPoints(int characterLevel) => Mathf.Max(0, characterLevel - 1);

        public static string TreeName(TalentTree tree) => tree switch
        {
            TalentTree.Marksmanship => "Marksmanship",
            TalentTree.BeastMastery => "Beast Mastery",
            _ => "Survival",
        };

        public static IReadOnlyList<TalentNode> Nodes { get; } = Build();

        static readonly Dictionary<string, TalentNode> ById = Index();

        public static TalentNode Get(string id) => id != null && ById.TryGetValue(id, out var node) ? node : null;

        static Dictionary<string, TalentNode> Index()
        {
            var map = new Dictionary<string, TalentNode>();
            foreach (var node in Nodes)
                map[node.Id] = node;
            return map;
        }

        static TalentNode Skill(TalentTree tree, int tier, int column, string id, string name, string skill, string text, string requires = null) =>
            new TalentNode { Id = id, Tree = tree, Tier = tier, Column = column, Name = name, Skill = skill, Text = text, Requires = requires };

        static TalentNode Passive(TalentTree tree, int tier, int column, string id, string name, int ranks, PassiveEffect effect, float perRank, string text,
            string requires = null) =>
            new TalentNode { Id = id, Tree = tree, Tier = tier, Column = column, Name = name, MaxRanks = ranks, Effect = effect, PerRank = perRank, Text = text, Requires = requires };

        static TalentNode Improved(TalentTree tree, int tier, int column, string skillNode, string skill, string skillName) =>
            new TalentNode
            {
                Id = "improved_" + skillNode, Tree = tree, Tier = tier, Column = column, Name = "Improved " + skillName, MaxRanks = 3,
                ImprovesSkill = skill, SkillDamage = 0.15f, Requires = skillNode, Text = skillName + " deals {0} more damage.",
            };

        // The numbers are Claude's, to review (Docs/08). Percent-point effects (crit chance, crit damage) are in points;
        // the rest are fractions.
        static List<TalentNode> Build()
        {
            const TalentTree M = TalentTree.Marksmanship, B = TalentTree.BeastMastery, S = TalentTree.Survival;
            return new List<TalentNode>
            {
                // Marksmanship: precision, crits, Pierce Arrow and Kill Shot, Barrage at the top.
                Skill(M, 0, 0, "pierce_arrow", "Pierce Arrow", "PierceArrow", "Learn Pierce Arrow: a heavy arrow through every enemy on its line."),
                Passive(M, 0, 2, "steady_aim", "Steady Aim", 5, PassiveEffect.CriticalChance, 1f, "{0} critical strike chance."),
                Improved(M, 1, 0, "pierce_arrow", "PierceArrow", "Pierce Arrow"),
                Passive(M, 1, 2, "lethal_shots", "Lethal Shots", 5, PassiveEffect.CriticalDamage, 6f, "{0} critical strike damage."),
                Skill(M, 2, 1, "split_arrow", "Split Arrow", "SplitArrow", "Learn Split Arrow: five arrows in a fan."),
                Passive(M, 2, 2, "mortal_shots", "Mortal Shots", 3, PassiveEffect.IncreasedDamage, 0.03f, "{0} more damage with every arrow."),
                Skill(M, 3, 0, "kill_shot", "Kill Shot", "KillShot", "Learn Kill Shot: a fast arrow that executes the wounded."),
                Improved(M, 3, 1, "split_arrow", "SplitArrow", "Split Arrow"),
                Improved(M, 4, 0, "kill_shot", "KillShot", "Kill Shot"),
                Passive(M, 4, 2, "bow_mastery", "Bow Mastery", 5, PassiveEffect.AttackSpeed, 0.02f, "{0} attack speed."),
                Skill(M, 5, 1, "barrage", "Barrage", "Barrage", "Learn Barrage: eight arrows over a second as she moves."),

                // Beast Mastery: the pet as a build, Homing Arrow, Hunter's Breath, Wild Frenzy.
                Skill(B, 0, 0, "homing_arrow", "Homing Arrow", "HomingArrow", "Learn Homing Arrow: three arrows that seek their targets."),
                Passive(B, 0, 2, "kindred_spirit", "Kindred Spirit", 5, PassiveEffect.PetDamage, 0.1f, "{0} pet damage."),
                Passive(B, 1, 1, "endurance_training", "Endurance Training", 5, PassiveEffect.LifePercent, 0.02f, "{0} maximum life."),
                Passive(B, 1, 2, "thick_hide", "Thick Hide", 3, PassiveEffect.PetLife, 0.15f, "{0} pet life.", "kindred_spirit"),
                Skill(B, 2, 1, "hunters_breath", "Hunter's Breath", "HuntersBreath", "Learn Hunter's Breath: refills Focus when it runs low."),
                Improved(B, 2, 0, "homing_arrow", "HomingArrow", "Homing Arrow"),
                Passive(B, 3, 0, "bestial_discipline", "Bestial Discipline", 3, PassiveEffect.FocusRegen, 0.1f, "{0} Focus regeneration."),
                Passive(B, 3, 2, "ferocity", "Ferocity", 3, PassiveEffect.IncreasedDamage, 0.03f, "{0} more damage with every arrow."),
                Skill(B, 4, 1, "wild_frenzy", "Wild Frenzy", "WildFrenzy", "Learn Wild Frenzy: a burst of attack speed in a fight."),
                Passive(B, 4, 2, "spirit_bond", "Spirit Bond", 3, PassiveEffect.LifeOnHit, 1f, "{0} life on every hit."),
                new TalentNode
                {
                    Id = "beast_within", Tree = B, Tier = 5, Column = 1, Name = "The Beast Within", Effect = PassiveEffect.PetDamage, PerRank = 0.5f,
                    Effect2 = PassiveEffect.AttackSpeed, PerRank2 = 0.1f, Text = "Pet damage +50 percent and attack speed +10 percent.",
                },

                // Survival: Momentum and dodge, fire and frost, Knockback Shot and Explosive Arrow.
                Skill(S, 0, 0, "knockback_shot", "Knockback Shot", "KnockbackShot", "Learn Knockback Shot: a heavy arrow that throws enemies back."),
                Passive(S, 0, 2, "fleet_foot", "Fleet Foot", 3, PassiveEffect.MoveSpeed, 0.02f, "{0} movement speed."),
                Passive(S, 1, 1, "evasion", "Evasion", 5, PassiveEffect.Dodge, 0.01f, "{0} chance to dodge."),
                Passive(S, 1, 2, "toughness", "Toughness", 5, PassiveEffect.ArmorPercent, 0.03f, "{0} armor."),
                Skill(S, 2, 0, "explosive_arrow", "Explosive Arrow", "ExplosiveArrow", "Learn Explosive Arrow: an arrow that bursts among a crowd."),
                Passive(S, 2, 2, "fire_arrows", "Fire Arrows", 3, PassiveEffect.IgniteChance, 0.04f, "{0} chance to set enemies burning."),
                Improved(S, 3, 0, "explosive_arrow", "ExplosiveArrow", "Explosive Arrow"),
                Passive(S, 3, 2, "frost_arrows", "Frost Arrows", 3, PassiveEffect.ChillChance, 0.05f, "{0} chance to chill enemies."),
                Improved(S, 4, 0, "knockback_shot", "KnockbackShot", "Knockback Shot"),
                Passive(S, 4, 2, "deterrence", "Deterrence", 3, PassiveEffect.LessDamageFromElites, 0.04f, "{0} less damage from elites and bosses."),
                Passive(S, 5, 1, "readiness", "Readiness", 1, PassiveEffect.CooldownReduction, 0.15f, "Skills come back 15 percent sooner."),
            };
        }

        /// <summary>A node's text with its value at a rank (the first rank when it has none yet).</summary>
        public static string Describe(TalentNode node, int rank)
        {
            if (node.Text == null || !node.Text.Contains("{0}"))
                return node.Text;
            var shown = Mathf.Max(1, rank);
            if (node.ImprovesSkill != null)
                return string.Format(node.Text, Percent(node.SkillDamage * shown));
            var value = node.PerRank * shown;
            var text = node.Effect == PassiveEffect.CriticalChance || node.Effect == PassiveEffect.CriticalDamage ? $"+{value:0.#} percent"
                : node.Effect == PassiveEffect.LifeOnHit ? $"+{value:0.#}"
                : node.Effect == PassiveEffect.LessDamageFromElites ? Percent(value).TrimStart('+')
                : Percent(value);
            return string.Format(node.Text, text);
        }

        static string Percent(float fraction) => $"+{fraction * 100f:0.#} percent";
    }

    /// <summary>The ranks bought in the talent trees (saved with the character), the skills they teach and the bonuses
    /// they give. Pure.</summary>
    public sealed class TalentState
    {
        readonly Dictionary<string, int> ranks = new Dictionary<string, int>();
        PassiveBonuses bonuses;

        public event Action Changed;

        public int RankOf(string id) => id != null && ranks.TryGetValue(id, out var rank) ? rank : 0;

        public IEnumerable<KeyValuePair<string, int>> All => ranks;

        public int Spent
        {
            get
            {
                var spent = 0;
                foreach (var rank in ranks.Values)
                    spent += rank;
                return spent;
            }
        }

        public int PointsIn(TalentTree tree)
        {
            var spent = 0;
            foreach (var pair in ranks)
            {
                var node = TalentRules.Get(pair.Key);
                if (node != null && node.Tree == tree)
                    spent += pair.Value;
            }
            return spent;
        }

        public int Available(int characterLevel) => Mathf.Max(0, TalentRules.EarnedPoints(characterLevel) - Spent);

        /// <summary>Why a rank cannot be bought now, or null when it can.</summary>
        public string Blocker(TalentNode node, int characterLevel)
        {
            if (node == null)
                return "Unknown talent.";
            if (RankOf(node.Id) >= node.MaxRanks)
                return "Already at its highest rank.";
            if (Available(characterLevel) <= 0)
                return "No talent points left: one comes with every level.";
            var needed = node.Tier * TalentRules.PointsPerTier;
            if (PointsIn(node.Tree) < needed)
                return $"Needs {needed} points in {TalentRules.TreeName(node.Tree)}.";
            var requires = TalentRules.Get(node.Requires);
            if (requires != null && RankOf(requires.Id) < requires.MaxRanks)
                return $"Needs {requires.Name} first.";
            return null;
        }

        public bool CanLearn(TalentNode node, int characterLevel) => Blocker(node, characterLevel) == null;

        /// <summary>Buys a rank. False, and nothing changes, when it cannot be bought.</summary>
        public bool Learn(string id, int characterLevel)
        {
            var node = TalentRules.Get(id);
            if (!CanLearn(node, characterLevel))
                return false;
            ranks[id] = RankOf(id) + 1;
            bonuses = null;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Every point back (the Trainer).</summary>
        public void ResetAll()
        {
            ranks.Clear();
            bonuses = null;
            Changed?.Invoke();
        }

        /// <summary>Sets a rank from a save (unknown ids and impossible ranks are dropped).</summary>
        public void Restore(string id, int rank)
        {
            var node = TalentRules.Get(id);
            if (node == null || rank <= 0)
                return;
            ranks[id] = Mathf.Min(rank, node.MaxRanks);
            bonuses = null;
        }

        /// <summary>Whether a node teaching this skill (its asset name) has been learned.</summary>
        public bool Knows(string skill)
        {
            if (skill == null)
                return false;
            foreach (var pair in ranks)
            {
                var node = TalentRules.Get(pair.Key);
                if (node != null && node.Skill == skill && pair.Value > 0)
                    return true;
            }
            return false;
        }

        /// <summary>How much more damage a skill deals from its "Improved" talent, as a fraction.</summary>
        public float SkillDamage(string skill)
        {
            var more = 0f;
            foreach (var pair in ranks)
            {
                var node = TalentRules.Get(pair.Key);
                if (node != null && node.ImprovesSkill == skill)
                    more += node.SkillDamage * pair.Value;
            }
            return more;
        }

        /// <summary>The passive bonuses of every rank bought (cached until a rank changes).</summary>
        public PassiveBonuses Bonuses
        {
            get
            {
                if (bonuses != null)
                    return bonuses;
                bonuses = new PassiveBonuses();
                foreach (var pair in ranks)
                {
                    var node = TalentRules.Get(pair.Key);
                    if (node == null)
                        continue;
                    if (node.Effect != PassiveEffect.None)
                        bonuses.Add(node.Effect, node.PerRank * pair.Value);
                    if (node.Effect2 != PassiveEffect.None)
                        bonuses.Add(node.Effect2, node.PerRank2 * pair.Value);
                }
                return bonuses;
            }
        }
    }
}
