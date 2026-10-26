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
    }
}
