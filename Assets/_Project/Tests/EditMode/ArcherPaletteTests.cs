using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class ArcherPaletteTests
    {
        static float Light(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        [Test]
        public void TheKeyColoursAreSwapped_AndNothingElseIs()
        {
            var ink = new Color32(18, 14, 24, 255);
            var cloth = new Color32(192, 42, 46, 255);
            var pixels = new[]
            {
                ArcherPalette.HairKeys[0], ArcherPalette.HairKeys[2], ArcherPalette.SkinKeys[0], ArcherPalette.SkinKeys[1],
                ink, cloth, new Color32(0, 0, 0, 0),
            };
            var hair = ArcherPalette.HairRamp(new Color(0.07f, 0.06f, 0.06f));
            var skin = ArcherPalette.SkinRamp(new Color(0.34f, 0.21f, 0.12f));
            var changed = ArcherPalette.Recolour(pixels, hair, skin);
            Assert.AreEqual(4, changed);
            Assert.AreEqual(hair[0].r, pixels[0].r);
            Assert.AreEqual(hair[2].g, pixels[1].g);
            Assert.AreEqual(skin[0].b, pixels[2].b);
            Assert.AreEqual(skin[1].r, pixels[3].r);
            Assert.AreEqual(ink, pixels[4]);
            Assert.AreEqual(cloth, pixels[5]);
            Assert.AreEqual(0, pixels[6].a);
        }

        [Test]
        public void ARampKeepsItsShadingAndHitsThePortraitsColour()
        {
            // The sheets' pixels averaged over the ramp give back the portrait's colour, light shades lighter than dark.
            foreach (var target in new[] { new Color(0.62f, 0.48f, 0.26f), new Color(0.38f, 0.13f, 0.05f), new Color(0.58f, 0.57f, 0.56f) })
            {
                var ramp = ArcherPalette.HairRamp(target);
                Assert.Greater(Light(ramp[0]), Light(ramp[2]), "the light key is lighter than the dark");
                float r = (ramp[0].r * 888f + ramp[1].r * 196f + ramp[2].r * 2432f) / 3516f / 255f;
                Assert.AreEqual(target.r, r, 0.03f);
            }
            var skin = ArcherPalette.SkinRamp(new Color(0.66f, 0.49f, 0.39f));
            float sr = (skin[0].r * 920f + skin[1].r * 48f + skin[2].r * 136f) / 1104f / 255f;
            Assert.AreEqual(0.66f, sr, 0.03f);
        }

        [Test]
        public void TheSkinOfADarkArcherIsDarkAndOfAPaleOneIsLight()
        {
            var dark = ArcherPalette.SkinRamp(new Color(0.24f, 0.14f, 0.08f));
            var pale = ArcherPalette.SkinRamp(new Color(0.66f, 0.49f, 0.39f));
            Assert.Less(Light(dark[0]), Light(pale[0]) * 0.5f);
        }

        [Test]
        public void TheTintCacheLetsGoOfTheLongestUnusedNotTheOnesInUse()
        {
            // The camp has nine archers and the player is a tenth: with the cache at 8 the first two lost their sheets
            // while standing there (2026-11-19).
            var recent = new System.Collections.Generic.List<string>();
            var evicted = new System.Collections.Generic.List<string>();
            for (var i = 0; i < 10; i++)
                evicted.AddRange(ArcherTint.Touch(recent, "archer " + i, ArcherTint.Kept));
            Assert.IsEmpty(evicted, "ten archers at once are all kept");
            Assert.GreaterOrEqual(ArcherTint.Kept, 24);

            // Asking again for the first moves it to the back; the oldest goes when the list overflows.
            var small = new System.Collections.Generic.List<string> { "a", "b", "c" };
            Assert.IsEmpty(ArcherTint.Touch(small, "a", 3));
            CollectionAssert.AreEqual(new[] { "b", "c", "a" }, small);
            CollectionAssert.AreEqual(new[] { "b" }, ArcherTint.Touch(small, "d", 3));
            CollectionAssert.AreEqual(new[] { "c", "a", "d" }, small);
        }
    }
}
