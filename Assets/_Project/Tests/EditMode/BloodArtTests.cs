using NUnit.Framework;

namespace ARPG.Tests
{
    public class BloodArtTests
    {
        [Test]
        public void Splats_HaveAWetMiddle_AndClearCorners()
        {
            for (var v = 0; v < BloodArt.Variants; v++)
            {
                var mid = BloodArt.SplatShade(v, BloodArt.SplatWidth / 2, BloodArt.SplatHeight / 2);
                Assert.That(mid, Is.EqualTo(2).Or.EqualTo(3), $"variant {v} middle");
                Assert.AreEqual(0, BloodArt.SplatShade(v, 0, 0), $"variant {v} corner");
                Assert.AreEqual(0, BloodArt.SplatShade(v, BloodArt.SplatWidth - 1, BloodArt.SplatHeight - 1), $"variant {v} corner");
            }
        }

        [Test]
        public void Splats_AreTheSameEveryTime_AndDifferFromEachOther()
        {
            var shapes = new string[BloodArt.Variants];
            for (var v = 0; v < BloodArt.Variants; v++)
            {
                var a = new System.Text.StringBuilder();
                var b = new System.Text.StringBuilder();
                for (var y = 0; y < BloodArt.SplatHeight; y++)
                    for (var x = 0; x < BloodArt.SplatWidth; x++)
                    {
                        a.Append(BloodArt.SplatShade(v, x, y));
                        b.Append(BloodArt.SplatShade(v, x, y));
                    }
                Assert.AreEqual(a.ToString(), b.ToString());
                shapes[v] = a.ToString();
            }
            for (var i = 0; i < shapes.Length; i++)
                for (var j = i + 1; j < shapes.Length; j++)
                    Assert.AreNotEqual(shapes[i], shapes[j], $"variants {i} and {j}");
        }

        [Test]
        public void Splats_FillAFairPartOfTheirArea_WithARim()
        {
            for (var v = 0; v < BloodArt.Variants; v++)
            {
                int filled = 0, rim = 0;
                for (var y = 0; y < BloodArt.SplatHeight; y++)
                    for (var x = 0; x < BloodArt.SplatWidth; x++)
                    {
                        var shade = BloodArt.SplatShade(v, x, y);
                        if (shade > 0)
                            filled++;
                        if (shade == 1)
                            rim++;
                    }
                var area = BloodArt.SplatWidth * BloodArt.SplatHeight;
                Assert.That(filled, Is.InRange(area / 5, area * 3 / 4), $"variant {v}");
                Assert.Greater(rim, 0, $"variant {v}");
            }
        }
    }
}
