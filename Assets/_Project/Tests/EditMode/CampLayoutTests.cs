using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class CampLayoutTests
    {
        [Test]
        public void NothingStandsOnTheCampsPoints()
        {
            foreach (var piece in CampLayout.Pieces)
                foreach (var cell in CampLayout.KeptClear)
                    Assert.IsFalse(TownLayout.Covers(piece, cell, 1.5f), $"{piece.Name} at {piece.Cell} is too close to {cell}");
        }

        [Test]
        public void CampPiecesDoNotOverlap()
        {
            var pieces = CampLayout.Pieces;
            for (var i = 0; i < pieces.Length; i++)
                for (var j = i + 1; j < pieces.Length; j++)
                {
                    if (TownLayout.IsWallPart(pieces[i]) && TownLayout.IsWallPart(pieces[j]))
                        continue;
                    var d = pieces[i].Cell - pieces[j].Cell;
                    var apart = Mathf.Abs(d.x) > pieces[i].Half.x + pieces[j].Half.x || Mathf.Abs(d.y) > pieces[i].Half.y + pieces[j].Half.y;
                    Assert.IsTrue(apart, $"{pieces[i].Name} at {pieces[i].Cell} overlaps {pieces[j].Name} at {pieces[j].Cell}");
                }
        }

        [Test]
        public void TheCampsPeopleStandApart()
        {
            var cells = new[] { CampLayout.Billboard, CampLayout.Yard, CampLayout.Armoury, CampLayout.Kennel, CampLayout.GraveStone };
            for (var i = 0; i < cells.Length; i++)
                for (var j = i + 1; j < cells.Length; j++)
                    Assert.GreaterOrEqual(Vector2Int.Distance(cells[i], cells[j]), 5.5f, $"{cells[i]} and {cells[j]}");
        }

        [Test]
        public void TheCampIsInsideItsTrees()
        {
            foreach (var cell in CampLayout.KeptClear)
                Assert.Less(Mathf.Max(Mathf.Abs(cell.x), Mathf.Abs(cell.y)), CampLayout.ClearingReach - 3);
        }

        [Test]
        public void TheWoodAroundTheCampIsClosed()
        {
            // At every angle a ray from the middle crosses the wall of trees (footprints that touch), wherever the ragged
            // edge lies there: no gap a person could pass.
            for (var angle = 0f; angle < 360f; angle += 0.5f)
            {
                var a = angle * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var edge = CampLayout.EdgeAt(a);
                var blocked = false;
                for (var r = edge; r <= edge + CampLayout.SolidDepth && !blocked; r += 0.1f)
                {
                    var p = direction * r;
                    foreach (var piece in CampLayout.Pieces)
                        if (piece.Half.x > 0.9f && Mathf.Abs(piece.Cell.x - p.x) <= piece.Half.x && Mathf.Abs(piece.Cell.y - p.y) <= piece.Half.y)
                        {
                            blocked = true;
                            break;
                        }
                }
                Assert.IsTrue(blocked, $"the wall is open at {angle} degrees");
            }
        }

        [Test]
        public void TheWoodsEdgeIsNotACircle()
        {
            float min = float.MaxValue, max = float.MinValue;
            for (var angle = 0f; angle < 360f; angle += 1f)
            {
                var edge = CampLayout.EdgeAt(angle * Mathf.Deg2Rad);
                min = Mathf.Min(min, edge);
                max = Mathf.Max(max, edge);
            }
            Assert.Greater(max - min, 4f);
            // And the camp's clear ground lies inside it everywhere, and the ground (48 cells each way) reaches past it.
            Assert.Greater(min, CampLayout.ClearingReach - 2);
            Assert.Less(max + CampLayout.SolidDepth + CampLayout.OuterDepth, 48f);
        }

        [Test]
        public void TheWoodThickensTowardTheEdge()
        {
            int Count(float from, float to)
            {
                var n = 0;
                foreach (var piece in CampLayout.Pieces)
                {
                    if (!piece.Name.StartsWith("forest_"))
                        continue;
                    var past = CampLayout.PastEdge(piece.Cell.x, piece.Cell.y);
                    if (past >= from && past < to)
                        n++;
                }
                return n;
            }
            var outer = Count(-CampLayout.ThicketDepth, -CampLayout.ThicketDepth / 2f);
            var inner = Count(-CampLayout.ThicketDepth / 2f, 0f);
            Assert.Greater(outer, 0);
            Assert.Greater(inner, outer);
            Assert.Greater(Count(0f, CampLayout.SolidDepth), inner);
        }

        [Test]
        public void SingleTreesStandAboutTheClearing()
        {
            var singles = 0;
            foreach (var piece in CampLayout.Pieces)
                if (piece.Name.StartsWith("forest_tree") && CampLayout.PastEdge(piece.Cell.x, piece.Cell.y) < -CampLayout.ThicketDepth)
                    singles++;
            Assert.Greater(singles, 12);
        }

        [Test]
        public void TheArchersStandApartFromTheSpotsAndShootClear()
        {
            foreach (var archer in CampLayout.Archers)
            {
                foreach (var spot in CampLayout.KeptClear)
                    Assert.GreaterOrEqual(Vector2.Distance(archer.Cell, spot), 3f, $"archer {archer.Cell} and {spot}");
                // Its target is a piece of the camp, and no other piece stands between them.
                var target = System.Array.Find(CampLayout.Pieces, p => p.Cell == archer.Target && (p.Name == "camp_target" || p.Name == "camp_dummy"));
                Assert.IsNotNull(target.Name, $"no target at {archer.Target}");
                foreach (var piece in CampLayout.Pieces)
                {
                    if (piece.Cell == archer.Target)
                        continue;
                    var along = Vector2.Dot((Vector2)(piece.Cell - archer.Cell), ((Vector2)(archer.Target - archer.Cell)).normalized);
                    var across = Mathf.Abs(Vector2.Dot((Vector2)(piece.Cell - archer.Cell), new Vector2(-(archer.Target - archer.Cell).y, (archer.Target - archer.Cell).x).normalized));
                    Assert.IsFalse(along > 0f && along < (archer.Target - archer.Cell).magnitude && across < 1.2f + piece.Half.y, $"{piece.Name} at {piece.Cell} is in {archer.Cell}'s line");
                }
            }
        }

        [Test]
        public void TheStrollsKeepClearOfEveryPiece()
        {
            foreach (var stroll in CampLayout.Strolls)
                for (var i = 0; i < stroll.Length; i++)
                {
                    var a = (Vector2)stroll[i];
                    var b = (Vector2)stroll[(i + 1) % stroll.Length];
                    var length = (b - a).magnitude;
                    for (var t = 0f; t <= length; t += 0.25f)
                    {
                        var p = a + (b - a) * (t / Mathf.Max(length, 0.01f));
                        foreach (var piece in CampLayout.Pieces)
                            Assert.IsFalse(Mathf.Abs(piece.Cell.x - p.x) <= piece.Half.x + 0.7f && Mathf.Abs(piece.Cell.y - p.y) <= piece.Half.y + 0.7f,
                                $"{piece.Name} at {piece.Cell} is on the walk from {a} to {b}");
                    }
                }
        }

        [Test]
        public void TheStrollsStayInsideTheClearing()
        {
            foreach (var stroll in CampLayout.Strolls)
                foreach (var cell in stroll)
                    Assert.Less(Mathf.Max(Mathf.Abs(cell.x), Mathf.Abs(cell.y)), CampLayout.ClearingReach - 3);
        }
    }
}
