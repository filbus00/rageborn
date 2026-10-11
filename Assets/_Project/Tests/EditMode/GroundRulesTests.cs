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
        public void Town_SquareIsPaved_TheRestIsMud_WithSomePuddles()
        {
            // Stones of middling height: the square shows cobbles, far out there is only mud and water.
            var square = 0;
            var mud = 0;
            var puddles = 0;
            var samples = 0;
            for (var x = -40; x <= 40; x += 2)
                for (var y = -40; y <= 40; y += 2)
                {
                    var g = new Vector2(x * 0.25f, y * 0.25f);
                    if (g.magnitude < 5f && GroundRules.TownPick(g, 0f, 0.6f) == GroundLayer.Cobble)
                        square++;
                    var far = new Vector2(30f + x * 0.25f, 30f + y * 0.25f);
                    var layer = GroundRules.TownPick(far, 0f, 0.6f);
                    Assert.AreNotEqual(GroundLayer.Cobble, layer, "no cobbles far from the square and roads");
                    Assert.AreNotEqual(GroundLayer.Grass, layer, "no grass in the dark town");
                    if (layer == GroundLayer.Puddle)
                        puddles++;
                    else
                        mud++;
                    samples++;
                }
            Assert.Greater(square, 0);
            Assert.That(puddles / (float)samples, Is.InRange(0.02f, 0.3f), "puddles here and there");
        }

        [Test]
        public void Town_RoadRunsFromTheSquareToTheMausoleum()
        {
            // Along the road's first three pieces, from the square to the mausoleum's door.
            var paved = 0;
            var steps = 0;
            Assert.AreEqual(TownLayout.StairsCell, GroundRules.TownPaths[5]);
            for (var k = 0; k < 6; k += 2)
                for (var t = 0f; t <= 1f; t += 0.1f)
                {
                    var g = Vector2.Lerp(IsoMath.CellToGround(GroundRules.TownPaths[k]), IsoMath.CellToGround(GroundRules.TownPaths[k + 1]), t);
                    var cover = GroundRules.PathCover(g, GroundRules.TownPaths);
                    if (GroundRules.TownPick(g, cover, 0.6f) != GroundLayer.Dirt)
                        paved++;
                    steps++;
                }
            Assert.GreaterOrEqual(paved, steps - 3, "the road meanders but stays under the line");
            // Far from every road there is none.
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
