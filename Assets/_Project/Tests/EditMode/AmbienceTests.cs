using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The crypt's small life (2026-10-20): rats run, drops fall, dust stays in view, flames waver.</summary>
    public class AmbienceTests
    {
        [Test]
        public void ARat_RunsAwayFromHer_OnlyWhenSheComesNear()
        {
            var rat = new Vector2(5f, 5f);
            Assert.IsTrue(AmbienceRules.ShouldFlee(rat, new Vector2(3f, 5f)));
            Assert.IsFalse(AmbienceRules.ShouldFlee(rat, new Vector2(0f, 5f)));
            Assert.AreEqual(Vector2.right, AmbienceRules.FleeDirection(rat, new Vector2(3f, 5f)));
        }

        [Test]
        public void ADrop_FallsAndLands()
        {
            Assert.AreEqual(AmbienceRules.DropStart, AmbienceRules.DropHeight(0f), 1e-5f);
            Assert.Greater(AmbienceRules.DropHeight(AmbienceRules.FallSeconds * 0.5f), 0f);
            Assert.LessOrEqual(AmbienceRules.DropHeight(AmbienceRules.FallSeconds), 0f);
        }

        [Test]
        public void Dust_WrapsRoundTheView_FlamesWaverWithinBounds()
        {
            var centre = Vector2.zero;
            var half = new Vector2(5f, 10f);
            Assert.AreEqual(-4.5f, AmbienceRules.Wrap(new Vector2(5.5f, 0f), centre, half).x, 1e-4f);
            Assert.AreEqual(9f, AmbienceRules.Wrap(new Vector2(0f, -11f), centre, half).y, 1e-4f);
            for (var t = 0f; t < 10f; t += 0.37f)
            {
                var f = AmbienceRules.Flicker(t, 1.3f);
                Assert.That(f, Is.InRange(0.55f, 1f));
            }
        }
    }
}
