using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class WallFadeTests
    {
        static readonly Vector2Int Her = new Vector2Int(20, 20);

        [Test]
        public void AWallJustInFrontOfHer_IsSeenThrough()
        {
            Assert.AreEqual(WallFadeRules.Faded, WallFadeRules.Alpha(new Vector2Int(19, 20), Her), 1e-5f);
            Assert.AreEqual(WallFadeRules.Faded, WallFadeRules.Alpha(new Vector2Int(17, 17), Her), 1e-5f);
        }

        [Test]
        public void WallsBehindHer_BesideHer_OrFarInFront_StaySolid()
        {
            Assert.AreEqual(1f, WallFadeRules.Alpha(new Vector2Int(21, 20), Her), "behind");
            Assert.AreEqual(1f, WallFadeRules.Alpha(new Vector2Int(22, 18), Her), "level with her");
            Assert.AreEqual(1f, WallFadeRules.Alpha(new Vector2Int(10, 10), Her), "too far in front to cover her");
            Assert.AreEqual(1f, WallFadeRules.Alpha(new Vector2Int(19, 9), Her), "far to the side");
        }

        [Test]
        public void TheFadedPatch_FadesBackInAtItsSides()
        {
            var edge = WallFadeRules.Alpha(new Vector2Int(19 - WallFadeRules.Width / 2 - 1, 19 + WallFadeRules.Width / 2), Her);
            Assert.Greater(edge, WallFadeRules.Faded);
            Assert.Less(edge, 1f);
        }
    }
}
