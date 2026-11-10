using ARPG.Editor;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class PixelArtDoubleTests
    {
        [Test]
        public void DoubleUp_CropsDoublesCentresAndStandsSixUp()
        {
            const int w = 40, h = 65;
            var pixels = new Color32[w * h];
            var red = new Color32(200, 0, 0, 255);
            // Three pixels across, two up, off to the right and 8 rows up.
            for (var x = 30; x < 33; x++)
                for (var y = 8; y < 10; y++)
                    pixels[y * w + x] = red;
            var output = PixelArtImporter.DoubleUp(pixels, w, h, out var nw, out var nh);
            Assert.AreEqual(40, nw, "at least a cell wide");
            Assert.AreEqual(4 + 8, nh, "two rows doubled, 6 under and 2 over");
            var drawn = 0;
            for (var y = 0; y < nh; y++)
                for (var x = 0; x < nw; x++)
                    if (output[y * nw + x].a == 255)
                    {
                        drawn++;
                        Assert.That(x >= 17 && x < 23, $"centred across, x {x}");
                        Assert.That(y >= 6 && y < 10, $"six up, y {y}");
                    }
            Assert.AreEqual(24, drawn, "each pixel 2 x 2");
            Assert.That(PixelArtImporter.Doubled.Contains("prop_bone_pile_1") && PixelArtImporter.Doubled.Contains("prop_bone_pile_2"));
        }
    }
}
