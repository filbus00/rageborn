using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class MinimapRevealTests
    {
        static DungeonLayout Level() =>
            DungeonGenerator.Generate(4242, 1, PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList());

        static MinimapReveal NewReveal(DungeonLayout layout) => new MinimapReveal(layout, new bool[MinimapReveal.CellCount(layout)]);

        [Test]
        public void NothingIsExplored_BeforeThePlayerMoves()
        {
            var layout = Level();
            var reveal = NewReveal(layout);

            Assert.IsFalse(reveal.IsExplored(layout.ArrivalFromAbove));
            Assert.IsFalse(reveal.IsExplored(layout.StairsUp));
        }

        [Test]
        public void StandingInARoom_RevealsTheWholeRoomAndItsWalls()
        {
            var layout = Level();
            var reveal = NewReveal(layout);
            var start = layout.Rooms[0].Interior;

            Assert.IsTrue(reveal.RevealAround(layout.ArrivalFromAbove));

            for (var x = start.xMin - 1; x <= start.xMax; x++)
                for (var y = start.yMin - 1; y <= start.yMax; y++)
                    Assert.IsTrue(reveal.IsExplored(new Vector2Int(x, y)), $"({x}, {y})");
        }

        [Test]
        public void OtherRooms_StayHidden_UntilVisited()
        {
            var layout = Level();
            var reveal = NewReveal(layout);
            reveal.RevealAround(layout.ArrivalFromAbove);

            var exit = layout.Rooms.First(r => r.Kind == RoomKind.Exit).Interior;
            Assert.IsFalse(reveal.IsExplored(new Vector2Int(exit.xMin + exit.width / 2, exit.yMin + exit.height / 2)));
        }

        [Test]
        public void TheRevealRadius_UncoversCorridors_ButNeverTheVoid()
        {
            var layout = Level();
            var reveal = NewReveal(layout);
            var cell = layout.ArrivalFromAbove;

            reveal.RevealAround(cell);

            // A void cell inside the radius stays unexplored; every floor or wall cell inside it is explored.
            var b = layout.Bounds;
            for (var dx = -MinimapReveal.RevealRadius; dx <= MinimapReveal.RevealRadius; dx++)
                for (var dy = -MinimapReveal.RevealRadius; dy <= MinimapReveal.RevealRadius; dy++)
                {
                    if (dx * dx + dy * dy > MinimapReveal.RevealRadius * MinimapReveal.RevealRadius)
                        continue;
                    var c = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (!b.Contains(c))
                        continue;
                    Assert.AreEqual(layout.Get(c) != DungeonCell.Void, reveal.IsExplored(c), $"{c}");
                }
        }

        [Test]
        public void StayingInOneCell_ReportsNoChange_SoTheMapIsNotRedrawn()
        {
            var layout = Level();
            var reveal = NewReveal(layout);

            Assert.IsTrue(reveal.RevealAround(layout.ArrivalFromAbove));
            Assert.IsFalse(reveal.RevealAround(layout.ArrivalFromAbove));
        }

        [Test]
        public void TheSessionKeepsALevelsFlags_AcrossVisits()
        {
            var layout = Level();
            var session = new GameSession();
            var count = MinimapReveal.CellCount(layout);

            new MinimapReveal(layout, session.ExploredCells("Dungeon 1", count)).RevealAround(layout.ArrivalFromAbove);
            var again = new MinimapReveal(layout, session.ExploredCells("Dungeon 1", count));

            Assert.IsTrue(again.IsExplored(layout.ArrivalFromAbove));
            Assert.IsFalse(new MinimapReveal(layout, session.ExploredCells("Dungeon 2", count)).IsExplored(layout.ArrivalFromAbove));
        }
    }
}
