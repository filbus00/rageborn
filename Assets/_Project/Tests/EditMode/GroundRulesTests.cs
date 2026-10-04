using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>What the ground is made of (GroundRules): dirt with cobbles below, dirt, grass and paths in town.</summary>
    public class GroundRulesTests
    {
        static float Share(System.Func<Vector2, bool> test)
        {
            var hits = 0;
            var total = 0;
            for (var x = -40f; x < 40f; x += 0.37f)
                for (var y = -40f; y < 40f; y += 0.41f)
                {
                    total++;
                    if (test(new Vector2(x, y)))
                        hits++;
                }
            return hits / (float)total;
        }

        [Test]
        public void Dungeon_IsMostlyDirt_WithSomeCobble()
        {
            // A stone's top (height 0.9) over a stretch of the dungeon.
            var cobble = Share(g => GroundRules.DungeonPick(g, 0.9f) == GroundLayer.Cobble);
            Assert.Greater(cobble, 0.1f, "some cobbles show through");
            Assert.Less(cobble, 0.5f, "mostly dirt");
            // The grout between stones (height 0) almost never shows.
            Assert.Less(Share(g => GroundRules.DungeonPick(g, 0f) == GroundLayer.Cobble), 0.05f);
        }

        [Test]
        public void Town_HasGrassSpots_NotAField()
        {
            var grass = Share(g => GroundRules.TownPick(g, 0f, 0.8f, 0f) == GroundLayer.Grass);
            Assert.Greater(grass, 0.1f);
            Assert.Less(grass, 0.6f);
        }

        [Test]
        public void Town_PathRunsFromTheSquareToTheStairs()
        {
            var start = IsoMath.CellToGround(new Vector2Int(0, 0));
            var stairs = IsoMath.CellToGround(new Vector2Int(5, 5));
            var onPath = 0;
            for (var t = 0f; t <= 1f; t += 0.05f)
            {
                var g = Vector2.Lerp(start, stairs, t);
                var cover = GroundRules.PathCover(g, GroundRules.TownPaths);
                if (GroundRules.TownPick(g, cover, 1f, 0.5f) == GroundLayer.Path)
                    onPath++;
            }
            Assert.GreaterOrEqual(onPath, 17, "the path meanders but stays under the line, wearing grass away");
            // Far from every path there is none.
            Assert.AreEqual(0f, GroundRules.PathCover(IsoMath.CellToGround(new Vector2Int(-20, 25)), GroundRules.TownPaths));
        }

        [Test]
        public void SegmentDistance_MeasuresToTheNearestPoint()
        {
            Assert.AreEqual(1f, GroundRules.SegmentDistance(new Vector2(1, 1), Vector2.zero, new Vector2(2, 0)), 1e-5f);
            Assert.AreEqual(1f, GroundRules.SegmentDistance(new Vector2(3, 0), Vector2.zero, new Vector2(2, 0)), 1e-5f);
        }
    }
}
