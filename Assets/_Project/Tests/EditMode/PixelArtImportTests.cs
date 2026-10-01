using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The names and pixel steps of `Tools > ARPG > Import Pixel Art` (ArtNames, PixelArt).</summary>
    public class PixelArtImportTests
    {
        [Test]
        public void Names_ReadEveryKind()
        {
            Assert.AreEqual(ArtKind.Floor, ArtNames.Parse("floor_brick_3", out var index, out var variant));
            Assert.AreEqual(1, index);
            Assert.AreEqual(2, variant);

            Assert.AreEqual(ArtKind.Decal, ArtNames.Parse("decal_puddle_1", out index, out variant));
            Assert.AreEqual((int)DecalKind.Puddle, index);
            Assert.AreEqual(0, variant);

            Assert.AreEqual(ArtKind.Prop, ArtNames.Parse("prop_bone_pile", out index, out _));
            Assert.AreEqual((int)PropKind.BonePile, index);
            Assert.AreEqual("prop_broken_column", ArtNames.PropName(PropKind.BrokenColumn));

            Assert.AreEqual(ArtKind.Wall, ArtNames.Parse("Wall", out _, out _), "case does not matter");
            Assert.AreEqual(ArtKind.LowWall, ArtNames.Parse("wall_low", out _, out _));
        }

        [Test]
        public void Names_RefuseWhatTheGameCannotPlace()
        {
            foreach (var name in new[] { "", "floor_brick", "floor_brick_0", "floor_brick_17", "floor_marble_1", "prop_table", "decal_bones_x", "walls" })
                Assert.AreEqual(ArtKind.Unknown, ArtNames.Parse(name, out _, out _), name);
        }

        [Test]
        public void EveryPropAndDecal_HasAName()
        {
            foreach (PropKind kind in System.Enum.GetValues(typeof(PropKind)))
            {
                Assert.AreEqual(ArtKind.Prop, ArtNames.Parse(ArtNames.PropName(kind), out var index, out _));
                Assert.AreEqual((int)kind, index);
            }
            foreach (DecalKind kind in System.Enum.GetValues(typeof(DecalKind)))
            {
                Assert.AreEqual(ArtKind.Decal, ArtNames.Parse($"decal_{ArtNames.Snake(kind.ToString())}_2", out var index, out _));
                Assert.AreEqual((int)kind, index);
            }
        }

        [Test]
        public void Downscale_AveragesWholeBlocks_ByCoverage()
        {
            // 4 x 2 to 2 x 1: the left block is two reds and two clear, the right all blue.
            var red = new Color32(200, 0, 0, 255);
            var blue = new Color32(0, 0, 200, 255);
            var clear = new Color32(0, 0, 0, 0);
            var src = new[] { red, clear, blue, blue, clear, red, blue, blue };
            var dst = PixelArt.Downscale(src, 4, 2, 2, 1);
            Assert.AreEqual(red.r, dst[0].r, "clear pixels do not darken the colour");
            Assert.AreEqual(128, dst[0].a, 1, "half covered");
            Assert.AreEqual(blue, dst[1]);
        }

        [Test]
        public void Enlarge_RepeatsPixels()
        {
            var a = new Color32(10, 20, 30, 255);
            var b = new Color32(40, 50, 60, 255);
            var big = PixelArt.Enlarge(new[] { a, b }, 2, 1, 4, 2);
            CollectionAssert.AreEqual(new[] { a, a, b, b, a, a, b, b }, big);
        }

        [Test]
        public void CutDiamond_ClearsTheCorners_AndFillsTheInside()
        {
            const int w = ArtNames.Width;
            var pixels = new Color32[w * w / 2];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(100, 90, 80, 255);
            pixels[1 + (w / 4) * w] = new Color32(0, 0, 0, 0); // a hole near the left point
            PixelArt.CutDiamond(pixels, w);
            Assert.AreEqual(0, pixels[0].a, "bottom left corner");
            Assert.AreEqual(0, pixels[w - 1 + (w / 2 - 1) * w].a, "top right corner");
            Assert.AreEqual(255, pixels[w / 2 + (w / 4) * w].a, "middle");
            Assert.AreEqual(new Color32(100, 90, 80, 255), pixels[1 + (w / 4) * w], "the hole takes its row's colour");
        }

        [Test]
        public void PaletteDistance_IsZeroOnThePalette()
        {
            var on = PixelArt.Snap(new Color32(120, 60, 40, 255));
            Assert.AreEqual(0f, PixelArt.PaletteDistance(new[] { on, on }), 1e-4f);
            Assert.Greater(PixelArt.PaletteDistance(new[] { new Color32(0, 255, 0, 255) }), 25f, "pure green is far off");
        }
    }
}
