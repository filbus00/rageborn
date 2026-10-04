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
    }
}
