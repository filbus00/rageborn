using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class LightingTests
    {
        [Test]
        public void TheEmber_BreathesWithinItsBand()
        {
            var low = float.MaxValue;
            var high = float.MinValue;
            for (var t = 0f; t < 60f; t += 0.01f)
            {
                var value = LightingRules.Flicker(t);
                low = Mathf.Min(low, value);
                high = Mathf.Max(high, value);
            }
            Assert.GreaterOrEqual(low, 0.88f);
            Assert.LessOrEqual(high, 1.06f);
            Assert.Greater(high - low, 0.08f, "it does move");
        }

        [Test]
        public void AFlash_IsFullAtFirst_ThenFades()
        {
            Assert.AreEqual(1f, LightingRules.FlashAmount(0.1f, 0.1f));
            Assert.AreEqual(1f, LightingRules.FlashAmount(0.07f, 0.1f));
            Assert.AreEqual(0.45f, LightingRules.FlashAmount(0.03f, 0.1f), 1e-4f);
            Assert.AreEqual(0f, LightingRules.FlashAmount(0f, 0.1f));
            Assert.AreEqual(0f, LightingRules.FlashAmount(0.05f, 0f));
        }

        [Test]
        public void FloorShade_IsStablePerCell_InRange_AndVaries()
        {
            var distinct = new HashSet<int>();
            for (var x = -30; x < 30; x++)
                for (var y = -30; y < 30; y++)
                {
                    var cell = new Vector2Int(x, y);
                    var shade = LightingRules.FloorShade(cell);
                    Assert.AreEqual(shade, LightingRules.FloorShade(cell));
                    Assert.GreaterOrEqual(shade.g, 0.82f);
                    Assert.LessOrEqual(shade.g, 1f);
                    Assert.LessOrEqual(Mathf.Abs(shade.r - shade.b), 0.081f, "only a touch warmer or cooler");
                    distinct.Add(Mathf.RoundToInt(shade.g * 100f));
                }
            Assert.Greater(distinct.Count, 12, "not one flat shade");
        }

        [Test]
        public void TheDungeon_IsDarkerThanTheTown()
        {
            Assert.Less(LightingRules.Dungeon.AmbientIntensity, LightingRules.Town.AmbientIntensity);
            Assert.Greater(LightingRules.EmberOuterRadius, 7f, "a pack is lit before it wakes (aggro 7)");
        }
    }
}
