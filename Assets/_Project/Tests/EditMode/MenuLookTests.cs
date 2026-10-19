using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The menus after the owner's concept art (2026-10-19, menuconcept.png).</summary>
    public class MenuLookTests
    {
        [Test]
        public void EveryMenuIcon_DrawsAShape_InsideItsGrid()
        {
            foreach (MenuIcons.Kind kind in System.Enum.GetValues(typeof(MenuIcons.Kind)))
            {
                var size = MenuIcons.Size(kind);
                var drawn = 0;
                for (var row = 0; row < size.y; row++)
                    for (var x = 0; x < size.x; x++)
                        if (MenuIcons.Drawn(kind, x, row))
                            drawn++;
                Assert.Greater(drawn, 8, kind.ToString());
                Assert.Less(drawn, size.x * size.y, kind + " is not a solid block");
            }
        }

        [Test]
        public void Coins_AreWrittenWithASpaceBetweenThousands()
        {
            Assert.AreEqual("0", MenuChrome.Coins(0));
            Assert.AreEqual("890", MenuChrome.Coins(890));
            Assert.AreEqual("1 890", MenuChrome.Coins(1890));
            Assert.AreEqual("100 000", MenuChrome.Coins(100000));
        }
    }
}
