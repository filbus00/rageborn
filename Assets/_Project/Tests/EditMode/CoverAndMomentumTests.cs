using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>Momentum shots and cover (2026-10-17).</summary>
    public class CoverAndMomentumTests
    {
        [Test]
        public void Momentum_SpeedsHerShots_AndSendsArrowsFurther()
        {
            Assert.AreEqual(0f, MomentumShots.AttackSpeed(0));
            Assert.AreEqual(0.3f, MomentumShots.AttackSpeed(StanceStacks.MaxStacks), 1e-5f);
            Assert.AreEqual(1.2f, MomentumShots.ArrowFactor(StanceStacks.MaxStacks), 1e-5f);
        }

        [Test]
        public void AWallBeside_HerIsCover_OnItsSideOnly()
        {
            var here = IsoMath.CellToGround(new Vector2Int(0, 0));
            // A wall cell just to +x.
            System.Func<Vector2Int, bool> wall = c => c == new Vector2Int(1, 0);
            var cover = CoverRules.Direction(wall, here);
            Assert.AreNotEqual(Vector2.zero, cover);
            var towardWall = (IsoMath.CellToGround(new Vector2Int(1, 0)) - here).normalized;
            Assert.Greater(Vector2.Dot(cover, towardWall), 0.99f);
            // A shot from beyond the wall (flying toward -x through it) breaks; one from the open side does not.
            Assert.IsTrue(CoverRules.Blocks(cover, -towardWall * 10f));
            Assert.IsFalse(CoverRules.Blocks(cover, towardWall * 10f));
            Assert.AreEqual(Vector2.zero, CoverRules.Direction(c => false, here), "open floor is no cover");
        }

        [Test]
        public void AnAmbush_ComesAfterHalfASecond_ThenEveryTwo()
        {
            Assert.IsFalse(CoverRules.AmbushReady(0.3f, -1f));
            Assert.IsTrue(CoverRules.AmbushReady(0.6f, -1f));
            Assert.IsFalse(CoverRules.AmbushReady(1.5f, 0.6f));
            Assert.IsTrue(CoverRules.AmbushReady(2.7f, 0.6f));
        }
    }
}
