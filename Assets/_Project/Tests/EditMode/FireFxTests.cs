using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The fire particles and the lights back on (2026-11-15).</summary>
    public class FireFxTests
    {
        [Test]
        public void ASpark_GoesFromPaleToDark_InFlatSteps_AndShrinks()
        {
            var young = FireFxRules.Colour(0f, false, false);
            var old = FireFxRules.Colour(0.99f, false, false);
            Assert.Greater(young.r + young.g + young.b, old.r + old.g + old.b);
            Assert.AreEqual(FireFxRules.Colour(0.01f, false, false), young, "the same step");
            Assert.AreEqual(old, FireFxRules.Colour(5f, false, false), "past its life it keeps the last step");
            Assert.AreEqual(2, FireFxRules.Size(0.1f, false));
            Assert.AreEqual(1, FireFxRules.Size(0.9f, false));
            Assert.AreEqual(2, FireFxRules.Size(0.9f, true), "smoke spreads");
            Assert.Less(FireFxRules.Colour(0.5f, false, true).a, 255, "smoke is thin");
        }

        [Test]
        public void ACampfire_ThrowsMoreThanATorch_AndATorchMoreThanCandles()
        {
            Assert.Greater(FireFxRules.Rate(FireKind.Campfire), FireFxRules.Rate(FireKind.Torch));
            Assert.Greater(FireFxRules.Rate(FireKind.Torch), FireFxRules.Rate(FireKind.Candles));
            Assert.Greater(FireFxRules.SmokeEvery(FireKind.Campfire), 0);
            Assert.AreEqual(0, FireFxRules.SmokeEvery(FireKind.Candles));
        }

        [Test]
        public void Sparks_SitOnTheGridOfDrawnPixels()
        {
            var p = FireFxRules.Snap(new Vector2(1.03f, -0.51f));
            Assert.AreEqual(Mathf.Round(p.x / FireFxRules.Block), p.x / FireFxRules.Block, 1e-4f);
            Assert.AreEqual(Mathf.Round(p.y / FireFxRules.Block), p.y / FireFxRules.Block, 1e-4f);
            Assert.Less(Vector2.Distance(p, new Vector2(1.03f, -0.51f)), FireFxRules.Block);
        }

        [Test]
        public void TheForestIsDimmer_TheGaolDark_AndLightsAreWeakerWhereItIsBright()
        {
            Assert.IsTrue(Features.MoodLighting);
            Assert.Less(LightingRules.Forest.AmbientIntensity, 1f);
            Assert.Less(LightingRules.Gaol.AmbientIntensity, LightingRules.Forest.AmbientIntensity);
            Assert.AreEqual(LightingRules.Forest.AmbientIntensity, LightingRules.DungeonAt(3).AmbientIntensity, "every level a forest");
            DungeonRules.Forest = false;
            try
            {
                Assert.AreEqual(LightingRules.Gaol.AmbientIntensity, LightingRules.DungeonAt(15).AmbientIntensity);
                Assert.AreEqual(LightingRules.Library.AmbientIntensity, LightingRules.DungeonAt(30).AmbientIntensity);
                Assert.AreEqual(LightingRules.Rift.AmbientIntensity, LightingRules.DungeonAt(40).AmbientIntensity, "the Rift Caves (2026-11-16)");
                Assert.Less(LightingRules.DungeonAt(10).AmbientIntensity, LightingRules.Gaol.AmbientIntensity, "the old crypt's dark");
            }
            finally
            {
                DungeonRules.Forest = true;
            }
            Assert.AreEqual(1f, LightingRules.LightScale(0.05f), 1e-4f);
            Assert.Less(LightingRules.LightScale(LightingRules.Forest.AmbientIntensity), LightingRules.LightScale(LightingRules.Gaol.AmbientIntensity));
            // Beside her the ground is never much over full light.
            foreach (var mood in new[] { LightingRules.Forest, LightingRules.Gaol })
                Assert.Less(mood.AmbientIntensity + LightingRules.EmberIntensity * LightingRules.LightScale(mood.AmbientIntensity), 1.3f);
            Assert.Less(LightingRules.EmberReach(LightingRules.Forest.AmbientIntensity), 1f);
            Assert.AreEqual(1f, LightingRules.EmberReach(0.2f));
        }
    }
}
