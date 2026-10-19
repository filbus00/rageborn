using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The Bag opens when iOS takes the screen (the owner, 2026-10-19).</summary>
    public class FocusPauseTests
    {
        [Test]
        public void LosingTheScreen_OpensTheBag_OnlyWhileThePlayGoesOn()
        {
            Assert.IsTrue(FocusPauseRules.OpensBag(true, false, 1f, true, false));
            Assert.IsFalse(FocusPauseRules.OpensBag(false, false, 1f, true, false), "no Bag in the scene");
            Assert.IsFalse(FocusPauseRules.OpensBag(true, true, 0f, true, false), "the Bag already open");
            Assert.IsFalse(FocusPauseRules.OpensBag(true, false, 0f, true, false), "already paused (a boon choice)");
            Assert.IsFalse(FocusPauseRules.OpensBag(true, false, 1f, false, false), "dead");
            Assert.IsFalse(FocusPauseRules.OpensBag(true, false, 1f, true, true), "Settings open from the HUD");
        }
    }
}
