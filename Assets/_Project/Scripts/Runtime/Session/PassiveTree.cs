using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    public enum PassiveBranch { Start, Wrath, Stampede, Scar }

    public enum PassiveKind { Start, Minor, Notable, Gateway, Keystone }

    /// <summary>What a node does. New effects go at the end: nothing stores the number.</summary>
    public enum PassiveEffect
    {
        None,
        IncreasedDamage, CriticalChance, CriticalDamage, MoveSpeed, AttackSpeed, Dodge, LifePercent, ArmorPercent, LifeOnHit,
        RagePerBasicHit, DamageVsWounded, RageDrainDelay, RageOnKill, CritDamageVsBleeding, DamagePerTenRage,
        MomentumStepSeconds, MomentumGraceSeconds, MovementSkillDamage, AttackSpeedPerMomentum, MomentumCap, BullRushRefund,
        LessDamageFromElites, PotionHeal, LowLifeReduction, MoreLifeOnHit, StillnessCap,
        Berserker, Juggernaut,
    }

    public sealed class PassiveNode
    {
        public string Id;
        public PassiveBranch Branch;
        public PassiveKind Kind;
        public string Name;
        public string Text;
        public int Cost = 1;
        public (PassiveEffect effect, float value)[] Effects = Array.Empty<(PassiveEffect, float)>();
    }

    /// <summary>
    /// The Wrathborn's passive tree (Docs/02, Passive tree, proposed: 60 nodes over the Wrath, Stampede and Scar branches;
    /// a start node, 36 minor, 18 notable, 3 gateways, 2 keystones). Each branch is a chain from the start node: an inner
    /// half of 6 minor and 3 notable, a gateway that opens after 15 points in the whole tree, and an outer half of the same
    /// (and the keystone for Wrath and Stampede). A node needs the one before it in its branch (Claude's reading of the
    /// proposal's layout, 2026-09-28). Points: one a level from level 2 (Docs/02: 59 at 60); a keystone costs 3 and opens
    /// at level 20 (Q4, Q5), and both can be bought, the loadout picking the active one (Q5). The numbers are Docs/02's
    /// proposals. Pure, saved with the character. No respec until the Trainer exists.
    /// </summary>
    public sealed class PassiveTree
    {
        public const int GatewayPoints = 15;
        public const int KeystoneLevel = 20;

        public static readonly IReadOnlyList<PassiveNode> Nodes = BuildNodes();

        readonly HashSet<string> bought = new HashSet<string> { "start" };
        PassiveBonuses bonuses;
        bool dirty = true;

        public event Action Changed;

        /// <summary>The keystone in effect: one bought keystone at a time (Q5). Null for none.</summary>
        public string ActiveKeystone { get; private set; }

        /// <summary>Points a character of this level has earned: one a level from 2 (Docs/02).</summary>
        public static int EarnedPoints(int characterLevel) => Mathf.Max(0, characterLevel - 1);

        public bool IsBought(string id) => bought.Contains(id);

        public IEnumerable<string> Bought => bought;

        public int Spent
        {
            get
            {
                var spent = 0;
                foreach (var node in Nodes)
                    if (node.Kind != PassiveKind.Start && bought.Contains(node.Id))
                        spent += node.Cost;
                return spent;
            }
        }

        public int Available(int characterLevel) => Mathf.Max(0, EarnedPoints(characterLevel) - Spent);

        public static PassiveNode Find(string id)
        {
            foreach (var node in Nodes)
                if (node.Id == id)
                    return node;
            return null;
        }

        /// <summary>The node before this one in its branch (the start node for a branch's first), or null for the start.</summary>
        public static PassiveNode Before(PassiveNode node)
        {
            if (node.Kind == PassiveKind.Start)
                return null;
            PassiveNode previous = Nodes[0];
            foreach (var candidate in Nodes)
            {
                if (candidate == node)
                    return previous;
                if (candidate.Branch == node.Branch)
                    previous = candidate;
            }
            return null;
        }

        /// <summary>Why a node cannot be bought now, or null when it can.</summary>
        public string Blocker(PassiveNode node, int characterLevel)
        {
            if (node == null || bought.Contains(node.Id))
                return "bought";
            var before = Before(node);
            if (before != null && !bought.Contains(before.Id))
                return "needs " + before.Name;
            if (node.Kind == PassiveKind.Gateway && Spent < GatewayPoints)
                return $"opens after {GatewayPoints} points";
            if (node.Kind == PassiveKind.Keystone && characterLevel < KeystoneLevel)
                return $"opens at level {KeystoneLevel}";
            if (Available(characterLevel) < node.Cost)
                return node.Cost == 1 ? "needs a point" : $"needs {node.Cost} points";
            return null;
        }

        public bool Buy(string id, int characterLevel)
        {
            var node = Find(id);
            if (Blocker(node, characterLevel) != null)
                return false;
            bought.Add(id);
            // The first keystone bought is in effect at once; a second waits for the loadout to choose it.
            if (node.Kind == PassiveKind.Keystone && ActiveKeystone == null)
                ActiveKeystone = id;
            dirty = true;
            Changed?.Invoke();
            return true;
        }

        public void SetActiveKeystone(string id)
        {
            var node = Find(id);
            if (node == null || node.Kind != PassiveKind.Keystone || !bought.Contains(id) || ActiveKeystone == id)
                return;
            ActiveKeystone = id;
            dirty = true;
            Changed?.Invoke();
        }

        /// <summary>Sets the tree from a save. Unknown ids are skipped.</summary>
        public void Restore(IEnumerable<string> ids, string activeKeystone)
        {
            foreach (var id in ids ?? Array.Empty<string>())
                if (Find(id) != null)
                    bought.Add(id);
            ActiveKeystone = activeKeystone != null && bought.Contains(activeKeystone) ? activeKeystone : null;
            dirty = true;
        }

        /// <summary>Everything the bought nodes and the active keystone add up to.</summary>
        public PassiveBonuses Bonuses
        {
            get
            {
                if (!dirty)
                    return bonuses;
                var b = new PassiveBonuses();
                foreach (var node in Nodes)
                {
                    if (!bought.Contains(node.Id))
                        continue;
                    if (node.Kind == PassiveKind.Keystone && node.Id != ActiveKeystone)
                        continue;
                    foreach (var (effect, value) in node.Effects)
                        b.Add(effect, value);
                }
                bonuses = b;
                dirty = false;
                return bonuses;
            }
        }

        static List<PassiveNode> BuildNodes()
        {
            var nodes = new List<PassiveNode>
            {
                new PassiveNode { Id = "start", Branch = PassiveBranch.Start, Kind = PassiveKind.Start, Name = "Wrathborn", Text = "Where the tree begins", Cost = 0 },
            };
            // Minor nodes: 3 kinds a branch, 2 of each per half (Docs/02's table), in the order kind 1, 2, notable, 3, 1,
            // notable, 2, 3, notable.
            void Half(PassiveBranch branch, string prefix, (string name, string text, PassiveEffect effect, float value)[] minors,
                (string name, string text, (PassiveEffect, float)[] effects)[] notables)
            {
                var order = new[] { 0, 1, -1, 2, 0, -2, 1, 2, -3 };
                var count = new int[3];
                foreach (var slot in order)
                {
                    if (slot >= 0)
                    {
                        var minor = minors[slot];
                        count[slot]++;
                        nodes.Add(new PassiveNode
                        {
                            Id = $"{prefix}_minor_{slot}_{count[slot]}", Branch = branch, Kind = PassiveKind.Minor,
                            Name = minor.name, Text = minor.text, Effects = new[] { (minor.effect, minor.value) },
                        });
                    }
                    else
                    {
                        var notable = notables[-slot - 1];
                        nodes.Add(new PassiveNode
                        {
                            Id = $"{prefix}_{notable.name.ToLowerInvariant().Replace(' ', '_')}", Branch = branch,
                            Kind = PassiveKind.Notable, Name = notable.name, Text = notable.text, Effects = notable.effects,
                        });
                    }
                }
            }

            var wrathMinors = new[]
            {
                ("Fury", "+4% damage", PassiveEffect.IncreasedDamage, 0.04f),
                ("Keen Edge", "+1.5% critical chance", PassiveEffect.CriticalChance, 1.5f),
                ("Brutality", "+10% critical damage", PassiveEffect.CriticalDamage, 10f),
            };
            var stampedeMinors = new[]
            {
                ("Stride", "+3% move speed", PassiveEffect.MoveSpeed, 0.03f),
                ("Quick Hands", "+3% attack speed", PassiveEffect.AttackSpeed, 0.03f),
                ("Sidestep", "+1.5% dodge", PassiveEffect.Dodge, 0.015f),
            };
            var scarMinors = new[]
            {
                ("Vigour", "+4% life", PassiveEffect.LifePercent, 0.04f),
                ("Hide", "+8% armor", PassiveEffect.ArmorPercent, 0.08f),
                ("Bloodletting", "+2 Life on Hit", PassiveEffect.LifeOnHit, 2f),
            };

            Half(PassiveBranch.Wrath, "wrath_in", wrathMinors, new[]
            {
                ("Red Mist", "Basic hits give 8 Rage instead of 6", new[] { (PassiveEffect.RagePerBasicHit, 2f) }),
                ("Bloodied Edge", "+15% damage to enemies below half life", new[] { (PassiveEffect.DamageVsWounded, 0.15f) }),
                ("Short Fuse", "Rage drains after 5 s out of combat, not 3", new[] { (PassiveEffect.RageDrainDelay, 2f) }),
            });
            Gateway(PassiveBranch.Wrath, "wrath");
            Half(PassiveBranch.Wrath, "wrath_out", wrathMinors, new[]
            {
                ("Carnage", "Kills give 3 Rage", new[] { (PassiveEffect.RageOnKill, 3f) }),
                ("Butcher", "Critical hits deal +30% damage to bleeding enemies", new[] { (PassiveEffect.CritDamageVsBleeding, 0.3f) }),
                ("Hatred", "+1% damage per 10 Rage held", new[] { (PassiveEffect.DamagePerTenRage, 0.01f) }),
            });
            nodes.Add(new PassiveNode
            {
                Id = "berserker", Branch = PassiveBranch.Wrath, Kind = PassiveKind.Keystone, Cost = 3, Name = "Berserker",
                Text = "Below half life: +30% damage and double Rage gain", Effects = new[] { (PassiveEffect.Berserker, 1f) },
            });

            Half(PassiveBranch.Stampede, "stampede_in", stampedeMinors, new[]
            {
                ("Road Runner", "Momentum builds every 0.45 s instead of 0.6", new[] { (PassiveEffect.MomentumStepSeconds, 0.45f) }),
                ("Sure Footed", "Momentum lasts 2 s after stopping, not 1.2", new[] { (PassiveEffect.MomentumGraceSeconds, 2f) }),
                ("Battering Ram", "Movement skills deal +25% damage", new[] { (PassiveEffect.MovementSkillDamage, 0.25f) }),
            });
            Gateway(PassiveBranch.Stampede, "stampede");
            Half(PassiveBranch.Stampede, "stampede_out", stampedeMinors, new[]
            {
                ("Hit and Run", "Each Momentum stack also gives 2% attack speed", new[] { (PassiveEffect.AttackSpeedPerMomentum, 0.02f) }),
                ("Tailwind", "+1 Momentum cap", new[] { (PassiveEffect.MomentumCap, 1f) }),
                ("Crashing Wave", "Bull Rush's cooldown is 1 s shorter per enemy hit", new[] { (PassiveEffect.BullRushRefund, 1f) }),
            });
            nodes.Add(new PassiveNode
            {
                Id = "juggernaut", Branch = PassiveBranch.Stampede, Kind = PassiveKind.Keystone, Cost = 3, Name = "Juggernaut",
                Text = "Each Momentum stack also gives 3% less damage taken; stopping loses half the stacks, not all",
                Effects = new[] { (PassiveEffect.Juggernaut, 1f) },
            });

            Half(PassiveBranch.Scar, "scar_in", scarMinors, new[]
            {
                ("Thick Hide", "+20% armor", new[] { (PassiveEffect.ArmorPercent, 0.2f) }),
                ("Scar Tissue", "5% less damage from elites and bosses", new[] { (PassiveEffect.LessDamageFromElites, 0.05f) }),
                ("Iron Lungs", "The potion heals 50% instead of 40%", new[] { (PassiveEffect.PotionHeal, 0.5f) }),
            });
            Gateway(PassiveBranch.Scar, "scar");
            Half(PassiveBranch.Scar, "scar_out", scarMinors, new[]
            {
                ("Unbroken", "Below 35% life, 15% less damage taken", new[] { (PassiveEffect.LowLifeReduction, 0.15f) }),
                ("Blood Price", "Life on Hit +50%", new[] { (PassiveEffect.MoreLifeOnHit, 0.5f) }),
                ("Old Wounds", "+1 Stillness cap and +10% life", new[] { (PassiveEffect.StillnessCap, 1f), (PassiveEffect.LifePercent, 0.1f) }),
            });
            return nodes;

            void Gateway(PassiveBranch branch, string prefix) => nodes.Add(new PassiveNode
            {
                Id = prefix + "_gateway", Branch = branch, Kind = PassiveKind.Gateway, Name = "Gateway",
                Text = $"Opens the outer branch; needs {GatewayPoints} points in the tree",
            });
        }
    }

    /// <summary>The passive tree's totals. Percentages as fractions (0.04 for 4 percent) except critical chance and
    /// damage, which are in percent like the gear's.</summary>
    public sealed class PassiveBonuses
    {
        public float IncreasedDamage, CriticalChance, CriticalDamage, MoveSpeed, AttackSpeed, Dodge, LifePercent, ArmorPercent, LifeOnHit;
        public float RagePerBasicHit, DamageVsWounded, RageDrainDelay, RageOnKill, CritDamageVsBleeding, DamagePerTenRage;
        public float MomentumStepSeconds, MomentumGraceSeconds, MovementSkillDamage, AttackSpeedPerMomentum, MomentumCap, BullRushRefund;
        public float LessDamageFromElites, PotionHeal, LowLifeReduction, MoreLifeOnHit, StillnessCap;
        public bool Berserker, Juggernaut;

        public void Add(PassiveEffect effect, float value)
        {
            switch (effect)
            {
                case PassiveEffect.IncreasedDamage: IncreasedDamage += value; break;
                case PassiveEffect.CriticalChance: CriticalChance += value; break;
                case PassiveEffect.CriticalDamage: CriticalDamage += value; break;
                case PassiveEffect.MoveSpeed: MoveSpeed += value; break;
                case PassiveEffect.AttackSpeed: AttackSpeed += value; break;
                case PassiveEffect.Dodge: Dodge += value; break;
                case PassiveEffect.LifePercent: LifePercent += value; break;
                case PassiveEffect.ArmorPercent: ArmorPercent += value; break;
                case PassiveEffect.LifeOnHit: LifeOnHit += value; break;
                case PassiveEffect.RagePerBasicHit: RagePerBasicHit += value; break;
                case PassiveEffect.DamageVsWounded: DamageVsWounded += value; break;
                case PassiveEffect.RageDrainDelay: RageDrainDelay += value; break;
                case PassiveEffect.RageOnKill: RageOnKill += value; break;
                case PassiveEffect.CritDamageVsBleeding: CritDamageVsBleeding += value; break;
                case PassiveEffect.DamagePerTenRage: DamagePerTenRage += value; break;
                case PassiveEffect.MomentumStepSeconds: MomentumStepSeconds = MomentumStepSeconds > 0f ? Mathf.Min(MomentumStepSeconds, value) : value; break;
                case PassiveEffect.MomentumGraceSeconds: MomentumGraceSeconds = Mathf.Max(MomentumGraceSeconds, value); break;
                case PassiveEffect.MovementSkillDamage: MovementSkillDamage += value; break;
                case PassiveEffect.AttackSpeedPerMomentum: AttackSpeedPerMomentum += value; break;
                case PassiveEffect.MomentumCap: MomentumCap += value; break;
                case PassiveEffect.BullRushRefund: BullRushRefund += value; break;
                case PassiveEffect.LessDamageFromElites: LessDamageFromElites += value; break;
                case PassiveEffect.PotionHeal: PotionHeal = Mathf.Max(PotionHeal, value); break;
                case PassiveEffect.LowLifeReduction: LowLifeReduction += value; break;
                case PassiveEffect.MoreLifeOnHit: MoreLifeOnHit += value; break;
                case PassiveEffect.StillnessCap: StillnessCap += value; break;
                case PassiveEffect.Berserker: Berserker = true; break;
                case PassiveEffect.Juggernaut: Juggernaut = true; break;
            }
        }
    }
}
