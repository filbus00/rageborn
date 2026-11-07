using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The archers' faces (2026-11-07): one look per name, and the painter keeping to its regions.</summary>
    public class PortraitTests
    {
        [Test]
        public void AName_AlwaysGivesTheSameLook()
        {
            var a = PortraitLooks.For("Kyme Vinemane", 3);
            var b = PortraitLooks.For("Kyme Vinemane", 3);
            Assert.AreEqual(a.Base, b.Base);
            Assert.AreEqual(a.Hair, b.Hair);
            Assert.AreEqual(a.Marks, b.Marks);
            Assert.AreEqual(PortraitLooks.Hash("Kyme Vinemane"), PortraitLooks.Hash("Kyme Vinemane"));
        }

        [Test]
        public void ArchersLooksDiffer()
        {
            var seen = new HashSet<string>();
            for (var seed = 0; seed < 200; seed++)
            {
                var look = PortraitLooks.For(MercenaryNames.Full(seed), 1);
                seen.Add($"{look.Skin}{look.Hair}{look.Eyes}{look.Cloak}{look.Marks}");
            }
            Assert.Greater(seen.Count, 190);
        }

        [Test]
        public void ThePainter_RecoloursOnlyItsRegions_AndMarksOnlySkin()
        {
            // Four pixels in a row: nothing, skin, hair, skin.
            var pixels = new Color32[] { new Color32(10, 200, 10, 255), new Color32(150, 100, 60, 255), new Color32(50, 35, 20, 255), new Color32(150, 100, 60, 255) };
            var regions = new Color32[] { new Color32(0, 255, 0, 255), new Color32(1, 255, 0, 255), new Color32(2, 255, 0, 255), new Color32(1, 255, 0, 255) };
            var look = new PortraitLook { Skin = new Color(0.3f, 0.2f, 0.1f), Hair = new Color(0.6f, 0.5f, 0.3f), Eyes = Color.blue, Cloak = Color.red };
            var painted = PortraitPainter.Paint(pixels, regions, 4, 1, new PortraitBase(), look);
            Assert.AreEqual(pixels[0], painted[0], "outside every region");
            Assert.AreEqual(77, painted[1].r, 1, "skin at its average lightness takes the skin colour");
            Assert.AreEqual(51, painted[1].g, 1);
            Assert.AreEqual(26, painted[1].b, 1);
            Assert.Greater(painted[2].r, 100, "the dark hair made blonde");

            look.Marks = FaceMarks.Freckles;
            var spots = new PortraitBase { cheeks = new[] { 4, 0 } };
            var marked = PortraitPainter.Paint(pixels, regions, 4, 1, spots, look);
            Assert.AreEqual(painted[0], marked[0]);
            Assert.AreEqual(painted[2], marked[2], "no freckles on hair");
            Assert.AreNotEqual(painted[1], marked[1], "freckles on skin");
        }
    }
}
