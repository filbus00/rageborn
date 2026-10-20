using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The floor's height under a point (the owner, 2026-10-20: the heights must read as heights).</summary>
    public class ElevationTests
    {
        [Test]
        public void ADais_IsOneLevelUp_APit_OneDown_AndStepsRunBetween()
        {
            var raised = new HashSet<Vector2Int> { new Vector2Int(5, 5) };
            var sunken = new HashSet<Vector2Int> { new Vector2Int(10, 10) };
            // A dais step at (4, 5) going down toward -x; a pit step at (11, 10) going down toward -x into the pit.
            var stairs = new Dictionary<Vector2Int, Vector2Int>
            {
                { new Vector2Int(4, 5), Vector2Int.left },
                { new Vector2Int(11, 10), Vector2Int.left },
            };
            var map = new ElevationMap(raised, sunken, stairs);
            Assert.AreEqual(1f, map.LevelAt(IsoMath.CellToGround(new Vector2Int(5, 5))));
            Assert.AreEqual(-1f, map.LevelAt(IsoMath.CellToGround(new Vector2Int(10, 10))));
            Assert.AreEqual(0f, map.LevelAt(IsoMath.CellToGround(new Vector2Int(0, 0))));
            // The middle of a step is half way; its upper edge (toward the dais) is the dais's level.
            Assert.AreEqual(0.5f, map.LevelAt(IsoMath.CellToGround(new Vector2Int(4, 5))), 1e-4f);
            var top = (IsoMath.CellToGround(new Vector2Int(4, 5)) + IsoMath.CellToGround(new Vector2Int(5, 5))) / 2f;
            Assert.AreEqual(1f, map.LevelAt(top), 1e-3f);
            Assert.AreEqual(-0.5f, map.LevelAt(IsoMath.CellToGround(new Vector2Int(11, 10))), 1e-4f);
            Assert.AreEqual(ElevationMap.LevelHeight, map.HeightAt(IsoMath.CellToGround(new Vector2Int(5, 5))));
        }
    }
}
