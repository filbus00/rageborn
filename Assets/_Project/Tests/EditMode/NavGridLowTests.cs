using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>Low cells, a dais's or pit's rim (2026-10-20): they block walking, not sight or shots.</summary>
    public class NavGridLowTests
    {
        static NavGrid Corridor()
        {
            var nav = new NavGrid(new Vector2Int(0, 0), 10, 3);
            for (var x = 0; x < 10; x++)
                for (var y = 0; y < 3; y++)
                    nav.SetWalkable(new Vector2Int(x, y), true);
            return nav;
        }

        [Test]
        public void ARim_BlocksTheWalk_NotTheSight()
        {
            var nav = Corridor();
            var a = IsoMath.CellToGround(new Vector2Int(1, 1));
            var b = IsoMath.CellToGround(new Vector2Int(8, 1));
            for (var y = 0; y < 3; y++)
            {
                nav.SetWalkable(new Vector2Int(5, y), false);
                nav.SetLow(new Vector2Int(5, y), true);
            }
            Assert.IsTrue(nav.HasLineOfSight(a, b));
            Assert.IsFalse(nav.HasClearWalk(a, b));
            Assert.IsTrue(nav.IsOpenToShots(new Vector2Int(5, 1)));
            Assert.IsFalse(nav.IsWalkable(new Vector2Int(5, 1)));
        }

        [Test]
        public void AWall_BlocksBoth()
        {
            var nav = Corridor();
            for (var y = 0; y < 3; y++)
                nav.SetWalkable(new Vector2Int(5, y), false);
            var a = IsoMath.CellToGround(new Vector2Int(1, 1));
            var b = IsoMath.CellToGround(new Vector2Int(8, 1));
            Assert.IsFalse(nav.HasLineOfSight(a, b));
            Assert.IsFalse(nav.HasClearWalk(a, b));
        }

        [Test]
        public void AnEnemyArrow_FliesOverARim()
        {
            var nav = Corridor();
            for (var y = 0; y < 3; y++)
            {
                nav.SetWalkable(new Vector2Int(5, y), false);
                nav.SetLow(new Vector2Int(5, y), true);
            }
            var position = IsoMath.CellToGround(new Vector2Int(1, 1));
            var target = IsoMath.CellToGround(new Vector2Int(8, 1));
            var velocity = (target - position).normalized * 10f;
            var travelled = 0f;
            var outcome = EnemyProjectileFlight.Outcome.Flying;
            for (var i = 0; i < 60 && outcome == EnemyProjectileFlight.Outcome.Flying; i++)
                outcome = EnemyProjectileFlight.Step(ref position, ref travelled, velocity, 1f / 60f, 20f, nav, target, 0.3f);
            Assert.AreEqual(EnemyProjectileFlight.Outcome.HitPlayer, outcome);
        }
    }
}
