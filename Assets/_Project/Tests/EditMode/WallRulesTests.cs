using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class WallRulesTests
    {
        [Test]
        public void AWall_IsCameraSide_WhenFloorIsBehindItOnScreen()
        {
            var floor = new HashSet<Vector2Int> { new Vector2Int(1, 0) };
            bool IsFloor(int x, int y) => floor.Contains(new Vector2Int(x, y));
            // Floor at x + 1: that floor is higher on screen than the wall, so the wall is in front of it.
            Assert.IsTrue(WallRules.IsCameraSide(IsFloor, 0, 0));
            // Floor at x - 1 (lower on screen, in front of the wall): the wall is a far wall.
            Assert.IsFalse(WallRules.IsCameraSide(IsFloor, 2, 0));
            Assert.IsTrue(WallRules.IsCameraSide((x, y) => x == 0 && y == 1, 0, 0), "y + 1");
            Assert.IsTrue(WallRules.IsCameraSide((x, y) => x == 1 && y == 1, 0, 0), "the corner behind");
            Assert.IsFalse(WallRules.IsCameraSide((x, y) => false, 0, 0));
        }

        [Test]
        public void InARealLevel_EveryRoomHasLowAndFullWalls_AndOnlyCameraSideOnesAreLow()
        {
            var shapes = PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();
            var layout = DungeonGenerator.Generate(7, 2, shapes);
            bool IsFloor(int x, int y) => layout.IsFloor(new Vector2Int(x, y));
            int low = 0, full = 0;
            for (var x = layout.Bounds.xMin; x < layout.Bounds.xMax; x++)
                for (var y = layout.Bounds.yMin; y < layout.Bounds.yMax; y++)
                {
                    if (layout.Get(x, y) != DungeonCell.Wall)
                        continue;
                    if (WallRules.IsCameraSide(IsFloor, x, y))
                    {
                        low++;
                        // A low wall's screen height (x + y) is below some floor it touches.
                        Assert.IsTrue(IsFloor(x + 1, y) || IsFloor(x, y + 1) || IsFloor(x + 1, y + 1));
                    }
                    else
                        full++;
                }
            Assert.Greater(low, 0);
            Assert.Greater(full, 0, "the far walls stay full height and frame the rooms");
        }
    }
}
