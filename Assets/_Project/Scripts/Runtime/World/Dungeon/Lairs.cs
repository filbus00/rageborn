using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>What kind of home a pack has made of its corner of the dungeon (2026-10-08, the owner: "make it so that it
    /// looks like the monsters and demons are truly living in them").</summary>
    public enum LairTheme
    {
        None,

        /// <summary>Bandits: a fire, bedrolls, sacks and stores, a rack of weapons, a cage for a prisoner.</summary>
        Camp,

        /// <summary>Husks, ghouls, bloats: where they eat. Carcasses, bones, gore.</summary>
        Feeding,

        /// <summary>Wolves: a nest of straw and bones, a carcass.</summary>
        Den,

        /// <summary>Cultists, acolytes, pyre keepers: an altar, candles, braziers, sigils, a summoning circle.</summary>
        Shrine,

        /// <summary>Skeletons, knights, grave priests: open coffins, sarcophagi, urns, candles.</summary>
        Tomb,

        /// <summary>The drowned and their harpooners: puddles, nets, carcasses.</summary>
        Drowned,

        /// <summary>The hollowed, rift callers, void wraiths: flesh pods, spikes, hellfire, gore.</summary>
        Nest,
    }

    /// <summary>A pack's lair: which theme its members make, and what it holds. Pure.</summary>
    public static class Lairs
    {
        /// <summary>The theme most of a pack's members make (ties to the first in the list's order).</summary>
        public static LairTheme Theme(IReadOnlyList<PackMember> members)
        {
            var counts = new int[8];
            foreach (var member in members)
                counts[(int)ThemeOf(member)]++;
            var best = LairTheme.None;
            for (var i = 1; i < counts.Length; i++)
                if (counts[i] > counts[(int)best])
                    best = (LairTheme)i;
            return best;
        }

        /// <summary>An elite pack's lair, by depth: a shrine above the middle of the dungeon, a nest below.</summary>
        public static LairTheme EliteTheme(int depth) => depth >= 16 ? LairTheme.Nest : depth >= 7 ? LairTheme.Tomb : LairTheme.Shrine;

        public static LairTheme ThemeOf(PackMember member)
        {
            switch (member)
            {
                // The concept roster (2026-10-13): raiders camp, the hungry feed, beasts den, casters keep shrines,
                // the dead keep tombs, demons nest.
                case PackMember.Fallen:
                case PackMember.Ogre:
                case PackMember.Giant:
                    return LairTheme.Camp;
                case PackMember.Zombie:
                case PackMember.Ghoul:
                case PackMember.BileFiend:
                case PackMember.FleshGolem:
                case PackMember.QuillRat:
                    return LairTheme.Feeding;
                case PackMember.HellHound:
                case PackMember.Nightmare:
                case PackMember.Harpy:
                case PackMember.Arachnid:
                case PackMember.SandWorm:
                    return LairTheme.Den;
                case PackMember.DarkCultist:
                case PackMember.BoneMage:
                case PackMember.Lich:
                    return LairTheme.Shrine;
                case PackMember.SkeletonWarrior:
                case PackMember.Wight:
                case PackMember.Mummy:
                case PackMember.Wraith:
                    return LairTheme.Tomb;
                case PackMember.Imp:
                case PackMember.Succubus:
                case PackMember.StygianDemon:
                case PackMember.Gargoyle:
                case PackMember.BalrogLite:
                    return LairTheme.Nest;
                default:
                    return LairTheme.None;
            }
        }

        /// <summary>The thing a lair has in its middle, where the pack gathers (a fire, an altar), or none.</summary>
        public static PropKind? Heart(LairTheme theme)
        {
            switch (theme)
            {
                case LairTheme.Camp: return PropKind.Campfire;
                case LairTheme.Shrine: return PropKind.Altar;
                case LairTheme.Nest: return PropKind.Hellfire;
                default: return null;
            }
        }

        /// <summary>The things standing around a lair, each with how many: the most is twice the least.</summary>
        public static (PropKind kind, int min, int max)[] Props(LairTheme theme)
        {
            switch (theme)
            {
                case LairTheme.Camp:
                    return new[] { (PropKind.Sack, 2, 4), (PropKind.Crate, 1, 3), (PropKind.Barrel, 1, 2), (PropKind.WeaponRack, 1, 1), (PropKind.Cage, 0, 1) };
                case LairTheme.Feeding:
                    return new[] { (PropKind.Carcass, 1, 3), (PropKind.BonePile, 2, 4), (PropKind.Stake, 0, 2) };
                case LairTheme.Den:
                    return new[] { (PropKind.Carcass, 1, 2), (PropKind.BonePile, 1, 3) };
                case LairTheme.Shrine:
                    return new[] { (PropKind.Candles, 2, 4), (PropKind.Brazier, 1, 1), (PropKind.Stake, 1, 2), (PropKind.Urn, 0, 2) };
                case LairTheme.Tomb:
                    return new[] { (PropKind.Coffin, 2, 4), (PropKind.Sarcophagus, 0, 1), (PropKind.Urn, 1, 3), (PropKind.Candles, 1, 2) };
                case LairTheme.Drowned:
                    return new[] { (PropKind.Carcass, 1, 2), (PropKind.Bucket, 1, 2), (PropKind.Barrel, 0, 2) };
                case LairTheme.Nest:
                    return new[] { (PropKind.FleshPod, 2, 5), (PropKind.Spikes, 2, 4), (PropKind.Stake, 0, 2) };
                default:
                    return new (PropKind, int, int)[0];
            }
        }

        /// <summary>The marks on a lair's floor, and how many.</summary>
        public static (DecalKind kind, int min, int max)[] Decals(LairTheme theme)
        {
            switch (theme)
            {
                case LairTheme.Camp:
                    return new[] { (DecalKind.Bedroll, 2, 4), (DecalKind.Ash, 1, 2), (DecalKind.Bones, 0, 2) };
                case LairTheme.Feeding:
                    return new[] { (DecalKind.Gore, 3, 6), (DecalKind.Blood, 3, 6), (DecalKind.Bones, 2, 4) };
                case LairTheme.Den:
                    return new[] { (DecalKind.Straw, 4, 7), (DecalKind.Bones, 2, 4), (DecalKind.Blood, 1, 3) };
                case LairTheme.Shrine:
                    return new[] { (DecalKind.Sigil, 2, 4), (DecalKind.Blood, 1, 3), (DecalKind.Ritual, 1, 2) };
                case LairTheme.Tomb:
                    return new[] { (DecalKind.Bones, 2, 4), (DecalKind.Skull, 1, 3), (DecalKind.Cracks, 1, 3) };
                case LairTheme.Drowned:
                    return new[] { (DecalKind.Puddle, 4, 8), (DecalKind.Net, 2, 3), (DecalKind.Moss, 1, 3) };
                case LairTheme.Nest:
                    return new[] { (DecalKind.Gore, 4, 7), (DecalKind.Sigil, 1, 3), (DecalKind.Blood, 2, 4) };
                default:
                    return new (DecalKind, int, int)[0];
            }
        }
    }
}
