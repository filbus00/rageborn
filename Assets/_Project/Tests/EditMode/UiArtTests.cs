using NUnit.Framework;

namespace ARPG.Tests
{
    public class UiArtTests
    {
        [Test]
        public void TheFrameIsClearInsideItsBorder()
        {
            for (var x = UiArt.FrameBorder; x < 12 - UiArt.FrameBorder; x++)
                for (var y = UiArt.FrameBorder; y < 12 - UiArt.FrameBorder; y++)
                    Assert.Less(UiArt.FrameValue(x, y), 0f);
        }

        [Test]
        public void EveryCornerHasARivet()
        {
            Assert.AreEqual(1f, UiArt.FrameValue(1, 1));
            Assert.AreEqual(1f, UiArt.FrameValue(10, 1));
            Assert.AreEqual(1f, UiArt.FrameValue(1, 10));
            Assert.AreEqual(1f, UiArt.FrameValue(10, 10));
        }

        [Test]
        public void TheBevelIsLightTopLeftAndDarkBottomRight()
        {
            Assert.Greater(UiArt.FrameValue(0, 6), UiArt.FrameValue(11, 6));
            Assert.Greater(UiArt.FrameValue(6, 11), UiArt.FrameValue(6, 0));
        }
    }
}
