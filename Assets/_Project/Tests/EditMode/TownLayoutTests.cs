using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class TownLayoutTests
    {
        [Test]
        public void NothingStandsOnTheTownsPoints()
        {
            foreach (var piece in TownLayout.Pieces)
                foreach (var cell in TownLayout.KeptClear)
                    Assert.IsFalse(TownLayout.Covers(piece, cell, 1.5f), $"{piece.Name} at {piece.Cell} is too close to {cell}");
        }

        [Test]
        public void PiecesDoNotOverlap()
        {
            var pieces = TownLayout.Pieces;
            for (var i = 0; i < pieces.Length; i++)
                for (var j = i + 1; j < pieces.Length; j++)
                {
                    // Walls, gate footprints and fences meet at their ends and corners.
                    if (TownLayout.IsWallPart(pieces[i]) && TownLayout.IsWallPart(pieces[j]))
                        continue;
                    var d = pieces[i].Cell - pieces[j].Cell;
                    var apart = Mathf.Abs(d.x) > pieces[i].Half.x + pieces[j].Half.x || Mathf.Abs(d.y) > pieces[i].Half.y + pieces[j].Half.y;
                    Assert.IsTrue(apart, $"{pieces[i].Name} at {pieces[i].Cell} overlaps {pieces[j].Name} at {pieces[j].Cell}");
                }
        }

        [Test]
        public void TheStairs_StandBeforeTheMausoleumsDoor_OffTheSquare()
        {
            // The door faces -y: the stairs are a few cells straight out from it, outside its footprint.
            var mausoleum = System.Array.Find(TownLayout.Pieces, p => p.Name == "mausoleum");
            Assert.AreEqual(TownLayout.MausoleumCell, mausoleum.Cell);
            Assert.AreEqual(mausoleum.Cell.x, TownLayout.StairsCell.x);
            Assert.That(mausoleum.Cell.y - TownLayout.StairsCell.y, Is.InRange(3, 5));
            Assert.IsFalse(TownLayout.Covers(mausoleum, TownLayout.StairsCell));
            // Well away from the square's middle.
            Assert.Greater(Vector2Int.Distance(Vector2Int.zero, TownLayout.StairsCell), 15f);
        }

        [Test]
        public void FootprintOfOneCellIsItsDiamond()
        {
            var corners = TownLayout.Footprint(new Vector2(0.5f, 0.5f));
            // A cell's diamond: 1 wide, 0.5 tall, corners at the bottom, right, top and left.
            Assert.AreEqual(new Vector2(0f, -0.25f), corners[0]);
            Assert.AreEqual(new Vector2(0.5f, 0f), corners[1]);
            Assert.AreEqual(new Vector2(0f, 0.25f), corners[2]);
            Assert.AreEqual(new Vector2(-0.5f, 0f), corners[3]);
        }
    
        [Test]
        public void TheTownsfolk_StandApart()
        {
            // The owner, 2026-10-17: "All the townspeople are standing too close".
            var cells = new System.Collections.Generic.List<UnityEngine.Vector2Int>
            {
                Newcomers.Cell(Newcomer.StashKeeper), Newcomers.Cell(Newcomer.Healer), Newcomers.Cell(Newcomer.Gambler),
                Newcomers.Cell(Newcomer.Trainer), QuestNpc.Cell(QuestPerson.Aldis), SmithNpc.Cell,
                new UnityEngine.Vector2Int(-6, 12), new UnityEngine.Vector2Int(-7, 1),
            };
            for (var i = 0; i < cells.Count; i++)
                for (var j = i + 1; j < cells.Count; j++)
                    Assert.GreaterOrEqual(UnityEngine.Vector2Int.Distance(cells[i], cells[j]), 5.5f, $"{cells[i]} and {cells[j]}");
        }
    }
}
