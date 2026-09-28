using NUnit.Framework;

namespace ARPG.Tests
{
    public class RenderResolutionTests
    {
        [Test]
        public void TheWorldRendersAtAboutDiablo2sResolution_InWholePixels()
        {
            Assert.AreEqual(3, RenderResolution.Factor(2622), "iPhone 17: 874 px tall, its point resolution");
            Assert.AreEqual(3, RenderResolution.Factor(2532), "iPhone 12");
            Assert.AreEqual(3, RenderResolution.Factor(2868), "Pro Max");
            Assert.AreEqual(2, RenderResolution.Factor(1792), "iPhone 11, a 2x screen");
            Assert.AreEqual(2, RenderResolution.Factor(1334), "iPhone SE");
            Assert.AreEqual(1, RenderResolution.Factor(600), "never below 1");
        }

        [Test]
        public void PixelsPerUnit_KeepTheScenesFraming()
        {
            var ppu = RenderResolution.PixelsPerUnit(874, 7.5f);
            Assert.AreEqual(58, ppu);
            Assert.AreEqual(7.5f, 874 * 0.5f / ppu, 0.05f, "the view stays 15 units tall");
        }
    }
}
