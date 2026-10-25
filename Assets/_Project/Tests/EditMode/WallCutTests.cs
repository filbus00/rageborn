using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>Curved crypt walls (2026-10-25): a wall cell cut along where its corners' openness crosses one half.</summary>
    public class WallCutTests
    {
        [Test]
        public void AStraightWall_StaysWhole_ACornerIsCutAcross()
        {
            // Open along its -x side only, the edge exactly on the boundary (one half): whole.
            Assert.IsFalse(WallCutRules.TryCut(0.5f, 0f, 0f, 0.5f, out _, out _));
            // No open corner, or all open: whole.
            Assert.IsFalse(WallCutRules.TryCut(0f, 0f, 0f, 0f, out _, out _));
            Assert.IsFalse(WallCutRules.TryCut(1f, 1f, 1f, 1f, out _, out _));
            // The -x -y corner well open, the rest closed: cut from the middle of the -y side to the middle of the -x side,
            // the wall the cell less that corner.
            Assert.IsTrue(WallCutRules.TryCut(1f, 0f, 0f, 0f, out var a, out var b));
            Assert.AreEqual(1, a);
            Assert.AreEqual(7, b);
        }

        [Test]
        public void CutsRunThroughMiddlesAndCorners()
        {
            // The -x side open, the +x side closed, crossing half way along both: a cut from the middle of the -y side to
            // the middle of the +y side, the wall the +x half.
            Assert.IsTrue(WallCutRules.TryCut(1f, 0.25f, 0f, 0.75f, out var a, out var b));
            Assert.AreEqual(1, a);
            Assert.AreEqual(5, b);
            // Crossing near the far corner on one side and half way on the other: a slope of one in two.
            Assert.IsTrue(WallCutRules.TryCut(1f, 0.4f, 0f, 0.5f, out a, out b));
            Assert.AreEqual(2, a, "from the +x -y corner");
            Assert.AreEqual(6, b, "to the -x +y corner");
        }

        [Test]
        public void CornerOpenness_IsTheShareOfOpenCells()
        {
            Assert.AreEqual(0.5f, WallCutRules.CornerOpen(true, false, true, false));
            Assert.AreEqual(0.25f, WallCutRules.CornerOpen(false, false, false, true));
        }
    }
}
