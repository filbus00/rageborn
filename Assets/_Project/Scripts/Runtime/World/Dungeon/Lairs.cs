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

        /// <summary>Spiders (2026-10-26): egg sacs, webs, bones.</summary>
        Web,

        /// <summary>The Iron Gaol's warders (2026-11-14): a brazier, a rack of weapons, stocks, barrels and crates.</summary>
        GuardPost,

        /// <summary>The Iron Gaol's confessors (2026-11-14): an altar, candles, a brazier, a rack to stretch a prisoner on.</summary>
        Confessional,

        /// <summary>The Burnt Library's monks (2026-11-16): lecterns, reading desks, candles, a few piled books.</summary>
        Scriptorium,

        /// <summary>The Burnt Library's burning stacks (2026-11-16): fallen shelves, burning books, ash.</summary>
        Pyre,
    }

    /// <summary>A pack's lair: which theme its members make, and what it holds. Pure.</summary>
    public static class Lairs
    {
        /// <summary>The theme most of a pack's members make (ties to the first in the list's order).</summary>
        public static LairTheme Theme(IReadOnlyList<PackMember> members)
        {
            var counts = new int[System.Enum.GetValues(typeof(LairTheme)).Length];
            foreach (var member in members)
                counts[(int)ThemeOf(member)]++;
            var best = LairTheme.None;
            for (var i = 1; i < counts.Length; i++)
                if (counts[i] > counts[(int)best])
                    best = (LairTheme)i;
            return best;
        }

        /// <summary>An elite pack's lair, by depth: a shrine above the middle of the dungeon, a nest below.</summary>
        public static LairTheme EliteTheme(int depth) =>
            DungeonRules.LibraryAt(depth) ? LairTheme.Pyre : DungeonRules.GaolAt(depth) ? LairTheme.GuardPost : depth >= 16 ? LairTheme.Nest : depth >= 7 ? LairTheme.Tomb : LairTheme.Shrine;

        public static LairTheme ThemeOf(PackMember member)
        {
            switch (member)
            {
                // The concept roster (2026-10-13): raiders camp, the hungry feed, beasts den, casters keep shrines,
                // the dead keep tombs, demons nest.
                case PackMember.Bandit:
                case PackMember.BanditArcher:
                case PackMember.Woodcutter:
                    return LairTheme.Camp;
                case PackMember.Wolf:
                case PackMember.Werewolf:
                case PackMember.TuskedBoar:
                    return LairTheme.Den;
                case PackMember.Rotcap:
                case PackMember.BriarThrall:
                case PackMember.BellHermit:
                case PackMember.HedgeWitch:
                    return LairTheme.Shrine;
                case PackMember.Spider:
                    return LairTheme.Web;
                // The Iron Gaol's (2026-11-14): warders keep a guard post, the priest and the lamp a confessional, the
                // kept and the beasts a den of straw and bones.
                case PackMember.Turnkey:
                case PackMember.CrossbowWarder:
                case PackMember.RackBrute:
                    return LairTheme.GuardPost;
                case PackMember.BrandPriest:
                case PackMember.LampWarden:
                    return LairTheme.Confessional;
                case PackMember.ShackledWretch:
                case PackMember.CellRat:
                case PackMember.IronMaiden:
                case PackMember.GaolMastiff:
                case PackMember.GibbetCrow:
                    return LairTheme.Den;
                // The Burnt Library's (2026-11-16): the monks and their scribes keep a scriptorium, what burns keeps a pyre.
                case PackMember.AshPenitent:
                case PackMember.CenserMonk:
                case PackMember.QuillScribe:
                case PackMember.FlameCantor:
                case PackMember.CandleBearer:
                    return LairTheme.Scriptorium;
                case PackMember.TomeGolem:
                case PackMember.ReliquaryGuard:
                case PackMember.Bookworm:
                case PackMember.AshMoth:
                case PackMember.EmberHound:
                    return LairTheme.Pyre;
                case PackMember.Fallen:
                case PackMember.Ogre:
                case PackMember.Giant:
                    return LairTheme.Camp;
                case PackMember.Zombie:
                case PackMember.Ghoul:
                case PackMember.BileFiend:
                case PackMember.Retcher:
                case PackMember.FleshGolem:
                case PackMember.QuillRat:
                case PackMember.Rat:
                case PackMember.Blob:
                    return LairTheme.Feeding;
                case PackMember.HellHound:
                case PackMember.Nightmare:
                case PackMember.Harpy:
                case PackMember.Arachnid:
                case PackMember.SandWorm:
                case PackMember.Bat:
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
                case LairTheme.GuardPost: return PropKind.Brazier;
                case LairTheme.Scriptorium: return PropKind.Lectern;
                case LairTheme.Pyre: return PropKind.BurningBooks;
                case LairTheme.Confessional: return PropKind.Altar;
                default: return null;
            }
        }

        /// <summary>The things standing around a lair, each with how many: the most is twice the least.</summary>
        public static (PropKind kind, int min, int max)[] Props(LairTheme theme)
        {
            switch (theme)
            {
                case LairTheme.Camp:
                    return new[] { (PropKind.BanditTent, 1, 2), (PropKind.Sack, 2, 4), (PropKind.Crate, 1, 3), (PropKind.Barrel, 1, 2), (PropKind.WeaponRack, 1, 1), (PropKind.Cage, 0, 1) };
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
                case LairTheme.Web:
                    return new[] { (PropKind.WebSac, 2, 4), (PropKind.BonePile, 1, 2) };
                case LairTheme.GuardPost:
                    return new[] { (PropKind.WeaponRack, 1, 1), (PropKind.Barrel, 1, 2), (PropKind.Crate, 1, 2), (PropKind.Stocks, 0, 1), (PropKind.Bucket, 0, 1) };
                case LairTheme.Confessional:
                    return new[] { (PropKind.Candles, 2, 4), (PropKind.Brazier, 1, 1), (PropKind.TortureBench, 0, 1) };
                case LairTheme.Scriptorium:
                    return new[] { (PropKind.ReadingDesk, 1, 2), (PropKind.Candles, 2, 4), (PropKind.BookPile, 1, 3), (PropKind.Urn, 0, 1) };
                case LairTheme.Pyre:
                    return new[] { (PropKind.FallenShelf, 1, 2), (PropKind.AshHeap, 1, 3), (PropKind.BookPile, 1, 2) };
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
                case LairTheme.Web:
                    return new[] { (DecalKind.Net, 3, 6), (DecalKind.Bones, 2, 4), (DecalKind.Blood, 1, 2) };
                case LairTheme.GuardPost:
                    return new[] { (DecalKind.Straw, 1, 3), (DecalKind.Blood, 1, 2) };
                case LairTheme.Confessional:
                    return new[] { (DecalKind.Blood, 2, 4), (DecalKind.Skull, 1, 2) };
                case LairTheme.Scriptorium:
                    return new[] { (DecalKind.Ash, 1, 3), (DecalKind.Rubble, 0, 2) };
                case LairTheme.Pyre:
                    return new[] { (DecalKind.Ash, 3, 6), (DecalKind.Blood, 0, 1) };
                default:
                    return new (DecalKind, int, int)[0];
            }
        }
    }
}
