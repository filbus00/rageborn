using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class HudIconTests
    {
        [Test]
        public void EveryIcon_IsARectangle_WithAnOutlinedShape()
        {
            foreach (HudIcons.Kind kind in System.Enum.GetValues(typeof(HudIcons.Kind)))
            {
                var size = HudIcons.Size(kind);
                var drawn = 0;
                for (var row = 0; row < size.y; row++)
                    for (var x = 0; x < size.x; x++)
                        if (HudIcons.Pixel(kind, x, row).a > 0)
                            drawn++;
                Assert.Greater(drawn, size.x * size.y / 3, kind.ToString());
                // The edge pixels are either clear or outline: nothing is cut off at the border.
                for (var x = 0; x < size.x; x++)
                {
                    Assert.That(IsEdge(HudIcons.Pixel(kind, x, 0)), kind + " top");
                    Assert.That(IsEdge(HudIcons.Pixel(kind, x, size.y - 1)), kind + " bottom");
                }
            }
        }

        static bool IsEdge(Color32 c) => c.a == 0 || (c.r == 20 && c.g == 14 && c.b == 12);
    }
}
