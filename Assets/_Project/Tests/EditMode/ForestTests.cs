using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>Every level a forest (2026-10-26).</summary>
    public class ForestTests
    {
        static List<RoomShape> Library() => PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();

        [Test]
        public void EveryDepth_IsAForest_OfClearingsWithNoTerraces()
        {
            Assert.IsTrue(DungeonRules.Forest);
            foreach (var depth in new[] { 1, 5, 6, 13, 30, 48 })
            {
                var layout = DungeonGenerator.Generate(7919, depth, Library());
                Assert.AreEqual(DungeonTheme.Cave, layout.Theme, "laid out as the caves");
                Assert.IsFalse(layout.Cathedral);
                Assert.AreEqual(0, layout.Raised.Count, "no terraces in the forest");
                Assert.Greater(layout.Packs.Count, 3);
            }
        }

        [Test]
        public void TreesStandForWalls_BushesForClumps()
        {
            static bool Nothing(int x, int y) => false;
            StringAssert.StartsWith("forest_tree_", ThemePieces.Wall(DungeonTheme.Cave, Nothing, 0, 0, false, false, false, 3));
            StringAssert.StartsWith("forest_bush_", ThemePieces.Wall(DungeonTheme.Cave, Nothing, 0, 0, false, true, false, 3));
            StringAssert.StartsWith("forest_tree_", ThemePieces.Rock(DungeonTheme.Cave, 5));
        }

        [Test]
        public void TheGround_IsMostlyGrass_WithDirtWornThrough()
        {
            var grass = 0;
            var dirt = 0;
            for (var x = 0; x < 60; x++)
                for (var y = 0; y < 60; y++)
                    if (GroundRules.ForestPick(new Vector2(x * 0.5f, y * 0.5f)) == GroundLayer.Grass)
                        grass++;
                    else
                        dirt++;
            Assert.Greater(grass, dirt);
            Assert.Greater(dirt, 0);
        }

        [Test]
        public void TheGrassAndPath_FrayIntoEachOther_AtTheirEdgeOnly()
        {
            // The owner, 2026-10-27: the edge looked "a bit too cut out".
            Assert.AreEqual(1f, GroundRules.ForestGrassShare(1f, 0.1f, 0.25f, 0f), "deep in the grass, even a dark gap is grass");
            Assert.AreEqual(0f, GroundRules.ForestGrassShare(0f, 0.5f, 0.25f, 1f), "out on the path, even a bright blade is earth");
            Assert.AreEqual(1f, GroundRules.ForestGrassShare(0.5f, 0.4f, 0.25f, 0.5f), "on the edge a bright blade reaches over");
            Assert.AreEqual(0f, GroundRules.ForestGrassShare(0.5f, 0.1f, 0.25f, 0.5f), "and a dark gap lets the earth through");
            Assert.AreEqual(0.5f, GroundRules.ForestGrassShare(0.5f, 0.25f, 0.25f, 0.5f), "an even pixel there is half of each");
            var mixed = 0;
            for (var x = 0; x < 200; x++)
            {
                var a = GroundRules.ForestGrassAmount(new Vector2(x * 0.05f, 3.3f));
                if (a > 0f && a < 1f)
                    mixed++;
            }
            Assert.Greater(mixed, 0, "the edge is a band, not a line");
            Assert.Less(mixed, 100, "and only a band");
        }

        [Test]
        public void TheForest_PlacesOnlyItsDrawnProps()
        {
            // The owner, 2026-10-27: "remove the old props now".
            foreach (var depth in new[] { 1, 4, 9, 20, 40 })
                foreach (var seed in new[] { 7919, 31, 4242 })
                {
                    var layout = DungeonGenerator.Generate(seed, depth, Library());
                    foreach (var prop in layout.Props.Values)
                        Assert.IsTrue(DungeonDressing.Allowed(layout, prop), $"seed {seed}, depth {depth}: {prop}");
                }
            Assert.IsFalse(DungeonDressing.Allowed(DungeonGenerator.Generate(7919, 1, Library()), PropKind.Stake));
        }
    }
}
