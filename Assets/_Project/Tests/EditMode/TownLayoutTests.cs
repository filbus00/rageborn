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
                    // The pen's fences meet at its corners.
                    if (pieces[i].Name.StartsWith("fence") && pieces[j].Name.StartsWith("fence"))
                        continue;
                    var d = pieces[i].Cell - pieces[j].Cell;
                    var apart = Mathf.Abs(d.x) > pieces[i].Half.x + pieces[j].Half.x || Mathf.Abs(d.y) > pieces[i].Half.y + pieces[j].Half.y;
                    Assert.IsTrue(apart, $"{pieces[i].Name} at {pieces[i].Cell} overlaps {pieces[j].Name} at {pieces[j].Cell}");
                }
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
    }
}
