using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>The pets the Pet Vendor sells (Docs/02, Pets; decided 2026-09-30: a few kinds, one active). New kinds go at
    /// the end; saves store names.</summary>
    public enum PetKind
    {
        Wolf,
        Raven,
        Boar,
    }

    /// <summary>What the pet attacks (the owner: "customizable. Which enemy to attack"), one chosen at a time.</summary>
    public enum PetTargeting
    {
        /// <summary>The enemy the character is shooting at.</summary>
        WhatIAttack,

        /// <summary>The enemy nearest the pet.</summary>
        Nearest,

        /// <summary>An elite or boss when one is near, else the nearest.</summary>
        ElitesFirst,

        /// <summary>The most wounded, to finish it.</summary>
        Weakest,

        /// <summary>The enemy nearest the character: whatever is coming for it.</summary>
        WhatAttacksMe,
    }

    /// <summary>What the pet does depending on the situation (the owner: "what to do depending on context"), each on or
    /// off.</summary>
    [Flags]
    public enum PetBehaviour
    {
        None = 0,

        /// <summary>Below half life, the pet stays at the character's side and fights only what reaches it.</summary>
        GuardWhenLow = 1,

        /// <summary>The pet fetches loot only when no enemy is near; off, it fetches whenever it has no target.</summary>
        FetchOnlyWhenClear = 2,

        /// <summary>The pet never strays more than 3 units from the character.</summary>
        StayClose = 4,

        /// <summary>The pet keeps out of a boss's reach and fights its adds instead.</summary>
        HoldBackFromBosses = 8,
    }

    /// <summary>One kind's numbers. All are tuning (Docs/02's proposal).</summary>
    public readonly struct PetDefinition
    {
        public PetDefinition(string name, string bonusText, int price, float lifeFactor, float damageFactor, float attackSeconds,
            float speed, float fetchRadius, float aggroChance, Color color)
        {
            Name = name;
            BonusText = bonusText;
            Price = price;
            LifeFactor = lifeFactor;
            DamageFactor = damageFactor;
            AttackSeconds = attackSeconds;
            Speed = speed;
            FetchRadius = fetchRadius;
            AggroChance = aggroChance;
            Color = color;
        }

        public string Name { get; }
        public string BonusText { get; }
        public int Price { get; }

        /// <summary>Its life as a share of the character's life by level.</summary>
        public float LifeFactor { get; }

        /// <summary>Its bite as a share of the character's weapon damage.</summary>
        public float DamageFactor { get; }
        public float AttackSeconds { get; }
        public float Speed { get; }
        public float FetchRadius { get; }

        /// <summary>The chance that an enemy beside it hits it instead of the character.</summary>
        public float AggroChance { get; }
        public Color Color { get; }
    }

    /// <summary>
    /// The pets' rules and numbers (Docs/02, Pets; the owner's decisions of 2026-09-30, the numbers Claude's proposal,
    /// tuning): three kinds, each with a passive bonus while it is the active pet; a pet's level is always the
    /// character's, so it needs no XP of its own; it is knocked out, never killed, and comes back after
    /// <see cref="KnockedOutSeconds"/>. Pure.
    /// </summary>
    public static class PetRules
    {
        public const float KnockedOutSeconds = 20f;
        public const float BiteReach = 1f;

        // How far from the character the pet looks for enemies, and where it walks when there is nothing to do.
        public const float HuntRadius = 6f;
        public const float StayCloseRadius = 3f;

        static readonly PetDefinition[] Definitions =
        {
            new PetDefinition("Wolf", "+5% life", 500, 0.45f, 0.8f, 1f, 5.5f, 6f, 0.5f, new Color(0.55f, 0.6f, 0.7f)),
            new PetDefinition("Raven", "+10% Magic Find", 1500, 0.25f, 0.55f, 0.7f, 7f, 12f, 0.2f, new Color(0.25f, 0.22f, 0.3f)),
            new PetDefinition("Boar", "+10% armor", 3000, 0.7f, 0.7f, 1.4f, 4.5f, 6f, 0.8f, new Color(0.5f, 0.35f, 0.25f)),
        };

        public static PetDefinition Get(PetKind kind) => Definitions[(int)kind];

        public static IReadOnlyList<PetKind> All { get; } = (PetKind[])Enum.GetValues(typeof(PetKind));

        /// <summary>The active pet's bonuses, as fractions (0.05 for 5 percent); zero without one.</summary>
        public static float LifeBonus(PetKind? active) => active == PetKind.Wolf ? 0.05f : 0f;

        public static float MagicFindBonus(PetKind? active) => active == PetKind.Raven ? 0.1f : 0f;

        public static float ArmorBonus(PetKind? active) => active == PetKind.Boar ? 0.1f : 0f;

        public static float MaxLife(PetKind kind, int level) => CombatFormulas.CharacterLife(level) * Get(kind).LifeFactor;

        public static string TargetingText(PetTargeting targeting) => targeting switch
        {
            PetTargeting.WhatIAttack => "Attack what I attack",
            PetTargeting.Nearest => "Attack the nearest",
            PetTargeting.ElitesFirst => "Elites first",
            PetTargeting.Weakest => "Finish the weakest",
            _ => "Attack what comes for me",
        };

        public static string BehaviourText(PetBehaviour behaviour) => behaviour switch
        {
            PetBehaviour.GuardWhenLow => "Guard me when my life is below 50%",
            PetBehaviour.FetchOnlyWhenClear => "Fetch loot only when no enemy is near",
            PetBehaviour.StayClose => "Stay close to me",
            PetBehaviour.HoldBackFromBosses => "Hold back from bosses",
            _ => behaviour.ToString(),
        };

        /// <summary>
        /// The pet's target by its rule, from the enemies it may consider (each: where it is, its life fraction,
        /// whether it is an elite or boss). <paramref name="playerTarget"/> is the character's own target's index, or -1.
        /// Distances are measured from the pet, except for <see cref="PetTargeting.WhatAttacksMe"/> (from the
        /// character). Returns an index, or -1 for none. A rule that finds nothing falls back to the nearest.
        /// </summary>
        public static int PickTarget(PetTargeting rule, IReadOnlyList<Vector2> positions, IReadOnlyList<float> lifeFractions,
            IReadOnlyList<bool> elites, int playerTarget, Vector2 pet, Vector2 player)
        {
            if (positions.Count == 0)
                return -1;
            switch (rule)
            {
                case PetTargeting.WhatIAttack:
                    if (playerTarget >= 0 && playerTarget < positions.Count)
                        return playerTarget;
                    break;
                case PetTargeting.ElitesFirst:
                {
                    var best = -1;
                    var bestDistance = float.MaxValue;
                    for (var i = 0; i < positions.Count; i++)
                    {
                        var distance = Vector2.Distance(pet, positions[i]);
                        if (elites[i] && distance < bestDistance)
                        {
                            best = i;
                            bestDistance = distance;
                        }
                    }
                    if (best >= 0)
                        return best;
                    break;
                }
                case PetTargeting.Weakest:
                {
                    var best = 0;
                    for (var i = 1; i < positions.Count; i++)
                        if (lifeFractions[i] < lifeFractions[best])
                            best = i;
                    return best;
                }
                case PetTargeting.WhatAttacksMe:
                    return Nearest(positions, player);
            }
            return Nearest(positions, pet);
        }

        static int Nearest(IReadOnlyList<Vector2> positions, Vector2 from)
        {
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < positions.Count; i++)
            {
                var distance = Vector2.Distance(from, positions[i]);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }

    /// <summary>
    /// The character's pets (Docs/02, Pets): the kinds bought, the one following it, and its rules. Lives in
    /// <see cref="GameSession"/> and is saved. Buying goes through <see cref="GameSession.BuyPet"/>, which takes the
    /// gold. Pure.
    /// </summary>
    public sealed class PetState
    {
        public const PetTargeting DefaultTargeting = PetTargeting.WhatIAttack;
        public const PetBehaviour DefaultBehaviours = PetBehaviour.FetchOnlyWhenClear;

        readonly List<PetKind> owned = new List<PetKind>();

        public event Action Changed;

        public IReadOnlyList<PetKind> Owned => owned;

        /// <summary>The pet following the character, or null for none.</summary>
        public PetKind? Active { get; private set; }

        public PetTargeting Targeting { get; private set; } = DefaultTargeting;

        public PetBehaviour Behaviours { get; private set; } = DefaultBehaviours;

        public bool Owns(PetKind kind) => owned.Contains(kind);

        public bool Has(PetBehaviour behaviour) => (Behaviours & behaviour) != 0;

        internal void AddOwned(PetKind kind)
        {
            if (owned.Contains(kind))
                return;
            owned.Add(kind);
            Changed?.Invoke();
        }

        /// <summary>Makes an owned pet the one that follows; switching is free (Docs/02).</summary>
        public bool SetActive(PetKind kind)
        {
            if (!Owns(kind) || Active == kind)
                return false;
            Active = kind;
            Changed?.Invoke();
            return true;
        }

        public void SetTargeting(PetTargeting targeting)
        {
            if (Targeting == targeting)
                return;
            Targeting = targeting;
            Changed?.Invoke();
        }

        /// <summary>The next targeting rule, for the vendor's chip that cycles through them.</summary>
        public void CycleTargeting()
        {
            var count = Enum.GetValues(typeof(PetTargeting)).Length;
            SetTargeting((PetTargeting)(((int)Targeting + 1) % count));
        }

        public void Toggle(PetBehaviour behaviour)
        {
            Behaviours ^= behaviour;
            Changed?.Invoke();
        }

        /// <summary>Owned kinds by name, for the save.</summary>
        public List<string> OwnedNames()
        {
            var names = new List<string>(owned.Count);
            foreach (var kind in owned)
                names.Add(kind.ToString());
            return names;
        }

        /// <summary>The rules by name, for the save: the targeting first, then each behaviour that is on.</summary>
        public List<string> RuleNames()
        {
            var names = new List<string> { Targeting.ToString() };
            foreach (PetBehaviour behaviour in Enum.GetValues(typeof(PetBehaviour)))
                if (behaviour != PetBehaviour.None && Has(behaviour))
                    names.Add(behaviour.ToString());
            return names;
        }

        /// <summary>Loads saved pets. Unknown names are left out with a warning; the rest loads.</summary>
        public void Restore(IReadOnlyList<string> ownedNames, string active, IReadOnlyList<string> rules, List<string> warnings)
        {
            owned.Clear();
            Active = null;
            Targeting = DefaultTargeting;
            Behaviours = DefaultBehaviours;
            if (ownedNames != null)
                foreach (var name in ownedNames)
                {
                    if (Enum.TryParse(name, out PetKind kind) && Enum.IsDefined(typeof(PetKind), kind))
                    {
                        if (!owned.Contains(kind))
                            owned.Add(kind);
                    }
                    else
                    {
                        warnings?.Add($"Left out an unknown pet ({name}).");
                    }
                }
            if (!string.IsNullOrEmpty(active) && Enum.TryParse(active, out PetKind activeKind) && owned.Contains(activeKind))
                Active = activeKind;
            if (rules != null && rules.Count > 0)
            {
                if (Enum.TryParse(rules[0], out PetTargeting targeting) && Enum.IsDefined(typeof(PetTargeting), targeting))
                    Targeting = targeting;
                var behaviours = PetBehaviour.None;
                for (var i = 1; i < rules.Count; i++)
                    if (Enum.TryParse(rules[i], out PetBehaviour behaviour) && behaviour != PetBehaviour.None)
                        behaviours |= behaviour;
                Behaviours = behaviours;
            }
            Changed?.Invoke();
        }
    }
}
