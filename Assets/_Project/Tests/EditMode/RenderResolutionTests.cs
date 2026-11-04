using NUnit.Framework;

namespace ARPG.Tests
{
    public class RenderResolutionTests
    {
        [Test]
        public void TheWorldRendersInBigWholePixels()
        {
            Assert.AreEqual(3, RenderResolution.Factor(2622), "iPhone 17: 874 px tall");
            Assert.AreEqual(3, RenderResolution.Factor(2532), "iPhone 12");
            Assert.AreEqual(3, RenderResolution.Factor(2868), "Pro Max");
            Assert.AreEqual(2, RenderResolution.Factor(1792), "iPhone 11, a 2x screen: 896 px tall");
            Assert.AreEqual(2, RenderResolution.Factor(1334), "iPhone SE");
            Assert.AreEqual(1, RenderResolution.Factor(500), "never below 1");
        }

        [Test]
        public void AtTheArtsPixelsPerUnit_TheViewIsAbout16To24UnitsTall()
        {
            foreach (var (longSide, name) in new[] { (2622, "iPhone 17"), (1792, "iPhone 11"), (2868, "Pro Max"), (1334, "iPhone SE") })
            {
                var height = longSide / RenderResolution.Factor(longSide);
                Assert.That(height / (float)PixelArt.PixelsPerUnit, Is.InRange(16f, 24.5f), name);
            }
        }

        [Test]
        public void ThePixelFilter_ShowsTheFartherView_OnOneGrid()
        {
            // The owner, 2026-11-04: "the camera could be even further away" and "I want it to be pixel art". iPhone 11:
            // 2 x 2 screen pixels a rendered pixel, 27 rendered pixels a unit, a view about 33 units tall.
            Assert.AreEqual(2, RenderResolution.PixelatedFactor(1792));
            Assert.AreEqual(27, RenderResolution.RenderedUnit(1792));
            foreach (var longSide in new[] { 1334, 1792, 2532, 2622, 2868 })
            {
                // The view in units: the art's own (the long side over the factor, over the art's pixels a unit) times
                // the camera's distance, within 6 percent.
                var art = longSide / RenderResolution.Factor(longSide) / (float)PixelArt.PixelsPerUnit;
                var shown = longSide / RenderResolution.PixelatedFactor(longSide) / (float)RenderResolution.RenderedUnit(longSide);
                Assert.AreEqual(art * RenderResolution.ViewScale, shown, art * 0.06f, longSide.ToString());
            }
        }
    }
}
