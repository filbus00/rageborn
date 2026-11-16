using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>
    /// Every level of every contract (2026-11-16, the code review): the forest's 1 to 12, and with the forest off the
    /// crypt's old levels, the Iron Gaol, the Burnt Library and the Rift Caves, depths 1 to 48. The other generator tests
    /// sample a few depths of the forest only; the contracts built since (the gaol, the library, the Rift Caves) ran in
    /// the game and the editor but were never swept. Each level is reachable, enclosed, stocked with its own roster and
    /// every pack in a lair of its kind.
    /// </summary>
    public class ContractLevelsTests
    {
        static readonly Vector2Int[] Steps = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        static List<RoomShape> Library() => PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();

        [TearDown]
        public void ForestAgain() => DungeonRules.Forest = true;

        static IEnumerable<(bool forest, int depth)> Levels()
        {
            for (var depth = 1; depth <= DungeonRules.Depths; depth++)
            {
                if (depth <= 12)
                    yield return (true, depth);
                yield return (false, depth);
            }
        }

        static HashSet<Vector2Int> Flood(DungeonLayout layout, Vector2Int start)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                    if (layout.IsFloor(cell + step) && seen.Add(cell + step))
                        queue.Enqueue(cell + step);
            }
            return seen;
        }

        [Test]
        public void EveryDepth_OfEveryContract_BuildsAReachableLevel()
        {
            foreach (var (forest, depth) in Levels())
            {
                DungeonRules.Forest = forest;
                for (var seed = 1; seed <= 6; seed++)
                {
                    var label = $"forest {forest}, depth {depth}, seed {seed}";
                    var layout = DungeonGenerator.Generate(seed * 104729 + depth, depth, Library());
                    var reached = Flood(layout, layout.ArrivalFromAbove);
                    var floor = 0;
                    for (var x = layout.Bounds.xMin; x < layout.Bounds.xMax; x++)
                        for (var y = layout.Bounds.yMin; y < layout.Bounds.yMax; y++)
                            if (layout.Get(x, y) == DungeonCell.Floor)
                                floor++;
                    Assert.AreEqual(floor, reached.Count, label + ": floor cut off from the arrival point");
                    Assert.IsTrue(reached.Contains(layout.StairsUp), label + ": stairs up");
                    if (layout.HasStairsDown)
                    {
                        Assert.IsTrue(reached.Contains(layout.StairsDown), label + ": stairs down");
                        Assert.IsTrue(reached.Contains(layout.ArrivalFromBelow), label + ": arrival from below");
                    }
                    foreach (var chest in layout.Chests)
                        Assert.IsTrue(reached.Contains(chest), label + ": chest");
                    foreach (var pack in layout.Packs)
                        Assert.IsTrue(reached.Contains(pack.Cell) || layout.Props.ContainsKey(pack.Cell) && Steps.Any(s => reached.Contains(pack.Cell + s)),
                            label + ": a pack off the floor");
                    Assert.AreEqual(DungeonRules.IsBossDepth(depth), layout.HasBossArena, label + ": the boss arena");
                }
            }
        }

        [Test]
        public void EveryPack_OfEveryLevel_IsOfItsContractsOwnKinds_AndLivesInALairOfItsKind()
        {
            foreach (var (forest, depth) in Levels())
            {
                DungeonRules.Forest = forest;
                var table = new HashSet<PackMember>(PackComposition.RosterAt(depth).Select(e => e.Member));
                for (var seed = 1; seed <= 3; seed++)
                {
                    var levelSeed = seed * 7919 + depth;
                    var layout = DungeonGenerator.Generate(levelSeed, depth, Library());
                    var label = $"forest {forest}, depth {depth}, seed {seed}";
                    Assert.AreEqual(layout.Packs.Count, layout.Lairs.Count, label);
                    for (var p = 0; p < layout.Packs.Count; p++)
                    {
                        var pack = layout.Packs[p];
                        var members = PackComposition.Roll(depth, pack.Kind, pack.Count, levelSeed, p);
                        Assert.AreEqual(pack.Count, members.Length, label + $": pack {p}");
                        Assert.IsTrue(members.Any(m => m != PackMember.None), label + $": pack {p} is empty");
                        foreach (var member in members)
                            Assert.IsTrue(member == PackMember.None || table.Contains(member), label + $": {member} is in no table here");
                        var expected = pack.Roams ? LairTheme.None : pack.Kind == PackKind.Elite ? Lairs.EliteTheme(depth) : Lairs.Theme(members);
                        Assert.AreEqual(expected, layout.Lairs[p].theme, label + $": pack {p}'s lair");
                    }
                }
            }
        }

        [Test]
        public void EveryDepth_HasItsRosterFrontFightersBackRowsAndSwarms()
        {
            foreach (var (forest, depth) in Levels())
            {
                DungeonRules.Forest = forest;
                Assert.IsTrue(PackComposition.Has(PackRole.Front, depth), $"forest {forest}, depth {depth}: no front fighters");
                // The first level has only front fighters and swarms (a pack has no back row before depth 2).
                if (depth >= 2)
                    foreach (var role in new[] { PackRole.Back, PackRole.Swarm })
                        Assert.IsTrue(PackComposition.Has(role, depth), $"forest {forest}, depth {depth}: nobody for {role}");
            }
        }
    }
}
