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
        public void HeavierPixels_KeepTheView_WithBiggerPixels()
        {
            // The owner, 2026-10-18: the 1.5 pixelation. iPhone 11: 3 x 3 screen pixels a rendered pixel, 27 art pixels a unit.
            Assert.AreEqual(3, RenderResolution.PixelatedFactor(1792));
            Assert.AreEqual(27, RenderResolution.PixelatedUnit(1792));
            foreach (var longSide in new[] { 1334, 1792, 2532, 2622, 2868 })
            {
                // The view in units (the long side over the factor, over the art's pixels a unit) within 6 percent of
                // what it was, and each rendered pixel at least 1.4 times bigger.
                var before = longSide / RenderResolution.Factor(longSide) / (float)PixelArt.PixelsPerUnit;
                var after = longSide / RenderResolution.PixelatedFactor(longSide) / (float)RenderResolution.PixelatedUnit(longSide);
                Assert.AreEqual(before, after, before * 0.06f, longSide.ToString());
                Assert.GreaterOrEqual(RenderResolution.PixelatedFactor(longSide) / (float)RenderResolution.Factor(longSide), 1.4f, longSide.ToString());
            }
        }
    }
}
