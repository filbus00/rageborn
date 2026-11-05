using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>Pixel art turned a pixel at a time (2026-11-05).</summary>
    public class TurnedArtTests
    {
        static readonly Color32 Red = new Color32(200, 20, 20, 255);
        static readonly Color32 Blue = new Color32(20, 20, 200, 255);

        // A line 9 pixels long and 1 high, red with a blue tip on the right, its pivot at the middle of the tip's pixel.
        static Color32[] Line()
        {
            var pixels = new Color32[9];
            for (var i = 0; i < 8; i++)
                pixels[i] = Red;
            pixels[8] = Blue;
            return pixels;
        }

        [Test]
        public void NotTurned_ItStaysAsDrawn()
        {
            var turned = PixelTurn.Turn(Line(), 9, 1, new Vector2(8.5f, 0.5f), 0f);
            Assert.AreEqual(9, turned.Width);
            Assert.AreEqual(1, turned.Height);
            Assert.AreEqual(Blue, turned.Pixels[8]);
            Assert.AreEqual(new Vector2(8.5f, 0.5f), turned.Pivot);
        }

        [Test]
        public void TurnedAQuarter_ItStandsUp_TipOnTop()
        {
            var turned = PixelTurn.Turn(Line(), 9, 1, new Vector2(8.5f, 0.5f), Mathf.PI / 2f);
            Assert.AreEqual(1, turned.Width);
            Assert.AreEqual(9, turned.Height);
            Assert.AreEqual(Blue, turned.Pixels[8], "the tip is the top pixel");
            Assert.AreEqual(Red, turned.Pixels[0]);
        }

        [Test]
        public void TurnedOnTheSlant_ALineStaysWhole_InItsOwnColours()
        {
            var turned = PixelTurn.Turn(Line(), 9, 1, new Vector2(8.5f, 0.5f), Mathf.PI / 4f);
            int filled = 0;
            foreach (var p in turned.Pixels)
            {
                if (p.a == 0)
                    continue;
                filled++;
                Assert.IsTrue(p.Equals(Red) || p.Equals(Blue), "no new colours");
            }
            Assert.That(filled, Is.InRange(5, 10), "about as many pixels as its length across the slant");
        }

        [Test]
        public void Directions_AreTheNearestOfSixteen()
        {
            Assert.AreEqual(0, TurnedArt.DirectionOf(5f));
            Assert.AreEqual(1, TurnedArt.DirectionOf(20f));
            Assert.AreEqual(4, TurnedArt.DirectionOf(90f));
            Assert.AreEqual(15, TurnedArt.DirectionOf(-20f));
            Assert.AreEqual(0, TurnedArt.DirectionOf(355f));
        }
    }
}
