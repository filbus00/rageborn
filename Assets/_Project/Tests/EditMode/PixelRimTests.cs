using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The telegraph circle's outline on the screen's grid (2026-11-05).</summary>
    public class PixelRimTests
    {
        [Test]
        public void TheRim_IsBrightAtItsEdge_FaintInside_ClearOutside()
        {
            int w = PoolArt.WidthFor(1.5f), h = w / 2;
            var pixels = TelegraphArt.PixelRimAlpha(w, h);
            Assert.AreEqual(0, pixels[0].a, "a corner is clear");
            Assert.AreEqual(255, pixels[0 + h / 2 * w].a, "the left edge is bright");
            Assert.AreEqual(255, pixels[w / 2 + (h - 1) * w].a, "the top edge is bright");
            Assert.Less(pixels[w / 2 + h / 2 * w].a, 60, "the middle is a faint floor");
            Assert.Greater(pixels[w / 2 + h / 2 * w].a, 0);
        }
    }
}
