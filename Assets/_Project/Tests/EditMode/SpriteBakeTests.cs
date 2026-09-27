using ARPG.Editor;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class SpriteBakeTests
    {
        [Test]
        public void Directions_RunSouthFirst_AroundTheCompass()
        {
            CollectionAssert.AreEqual(new[] { "s", "sw", "w", "nw", "n", "ne", "e", "se" }, SpriteBakeMath.DirectionCodes);
            AssertNear(new Vector3(0f, 0f, -1f), SpriteBakeMath.Facing(0), "S faces the camera");
            AssertNear(new Vector3(-1f, 0f, 0f), SpriteBakeMath.Facing(2), "W faces screen left");
            AssertNear(new Vector3(0f, 0f, 1f), SpriteBakeMath.Facing(4), "N faces away");
            AssertNear(new Vector3(1f, 0f, 0f), SpriteBakeMath.Facing(6), "E faces screen right");
            AssertNear(new Vector3(-1f, 0f, -1f).normalized, SpriteBakeMath.Facing(1), "SW is between");
        }

        [Test]
        public void TheCamera_PutsATileAt128By64_AndTheFeetOnThePivot()
        {
            // A 256 px cell is 2 m across the screen: 1 m, a tile's diagonal, is 128 px.
            Assert.AreEqual(1f, SpriteBakeMath.OrthographicSize(256), 1e-5f);
            // The 30 degree look-down halves a length along the view: the tile's other diagonal shows as 64 px.
            Assert.AreEqual(0.5f, Mathf.Sin(SpriteBakeMath.ElevationDegrees * Mathf.Deg2Rad), 1e-5f);
            // Feet at (128, 40): the view's centre is 88 px (0.6875 m) above them.
            var center = SpriteBakeMath.CenterFromPivot(256, new Vector2(128f, 40f));
            Assert.AreEqual(0f, center.x, 1e-5f);
            Assert.AreEqual(0.6875f, center.y, 1e-5f);
        }

        [Test]
        public void ModelScale_MakesTheStandingHeightTheTargetPixels()
        {
            // 1.9 m tall and 0.4 m deep: 1.645 m of height and 0.2 m of depth on screen.
            var screen = SpriteBakeMath.ScreenHeight(1.9f, 0.4f);
            Assert.AreEqual(1.9f * Mathf.Cos(30f * Mathf.Deg2Rad) + 0.2f, screen, 1e-4f);
            var scale = SpriteBakeMath.ModelScale(screen, 170f);
            Assert.AreEqual(170f, screen * scale * SpriteBakeMath.PixelsPerMeter, 1e-3f);
            Assert.AreEqual(1f, SpriteBakeMath.ModelScale(screen, 0f), "0 keeps the model's size");
        }

        [Test]
        public void SampleTimes_LoopsWithoutRepeating_OneShotsHitBothEnds()
        {
            CollectionAssert.AreEqual(new[] { 0f, 0.25f, 0.5f, 0.75f }, SpriteBakeMath.SampleTimes(1f, 4, true, 12f));
            CollectionAssert.AreEqual(new[] { 0f, 0.5f, 1f }, SpriteBakeMath.SampleTimes(1f, 3, false, 12f));
            Assert.AreEqual(12, SpriteBakeMath.SampleTimes(1f, 0, true, 12f).Length, "from the clip length");
            Assert.AreEqual(13, SpriteBakeMath.SampleTimes(1f, 0, false, 12f).Length, "a one-shot also keeps its last pose");
        }

        [Test]
        public void Sheets_SplitByDirection_OnlyWhenTooBig()
        {
            Assert.IsFalse(SpriteBakeMath.NeedsSplit(16, 256), "the player's longest sheet is 4096 wide");
            Assert.IsTrue(SpriteBakeMath.NeedsSplit(17, 256));
            Assert.IsTrue(SpriteBakeMath.NeedsSplit(12, 512), "12 boss frames are 6144 wide");
            Assert.AreEqual(new Vector2Int(8, 3), SpriteBakeMath.SplitGrid(24, 512), "the boss death: 3 rows of 8");
        }

        [Test]
        public void Matte_RecoversColourAndAlpha_FromBlackAndWhiteRenders()
        {
            var colour = new Color(0.8f, 0.4f, 0.2f);
            const float alpha = 0.25f;
            var overBlack = new[] { colour * alpha, Color.black, colour };
            var overWhite = new[] { colour * alpha + Color.white * (1f - alpha), Color.white, colour };
            for (var i = 0; i < 3; i++)
            {
                overBlack[i].a = 1f;
                overWhite[i].a = 1f;
            }

            var matte = SpriteBakeMath.Matte(overBlack, overWhite);
            Assert.AreEqual(alpha, matte[0].a, 1e-4f);
            Assert.AreEqual(colour.r, matte[0].r, 1e-4f);
            Assert.AreEqual(colour.b, matte[0].b, 1e-4f);
            Assert.AreEqual(0f, matte[1].a, 1e-4f, "background only");
            Assert.AreEqual(1f, matte[2].a, 1e-4f, "solid");
        }

        [Test]
        public void Downsample_AveragesByCoverage_WithoutADarkFringe()
        {
            var red = new Color(1f, 0f, 0f, 1f);
            var clear = new Color(0f, 0f, 0f, 0f);
            var result = SpriteBakeMath.Downsample(new[] { red, clear, clear, clear }, 2, 2, 2);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0.25f, result[0].a, 1e-5f);
            Assert.AreEqual(1f, result[0].r, 1e-5f, "the colour stays red, not darkened by the clear pixels");
        }

        static void AssertNear(Vector3 expected, Vector3 actual, string message) =>
            Assert.Less((expected - actual).magnitude, 1e-4f, $"{message}: expected {expected}, got {actual}");
    }
}
