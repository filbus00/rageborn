using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The owner's drawn pools, picked by a pool's colour (2026-10-31).</summary>
    public class PoolLooksTests
    {
        [Test]
        public void EachPoolColour_ShowsItsDrawnPool()
        {
            Assert.AreEqual("pool_fire", PoolLooks.For(new Color(1f, 0.45f, 0.1f, 0.6f)), "burning ground");
            Assert.AreEqual("pool_blood", PoolLooks.For(new Color(0.9f, 0.18f, 0.12f, 0.9f)));
            Assert.AreEqual("pool_poison", PoolLooks.For(new Color(0.45f, 0.85f, 0.25f, 0.6f)), "plague");
            Assert.AreEqual("pool_poison", PoolLooks.For(new Color(0.45f, 0.9f, 0.6f, 0.85f)), "grave rot");
            Assert.AreEqual("pool_frost", PoolLooks.For(new Color(0.55f, 0.85f, 1f, 0.9f)));
            Assert.AreEqual("pool_void", PoolLooks.For(new Color(0.6f, 0.3f, 0.85f, 0.9f)));
            Assert.AreEqual("pool_void", PoolLooks.For(new Color(0.35f, 0.1f, 0.45f, 0.95f)), "desecration");
        }

        [Test]
        public void WaterBoneAndStone_KeepTheTintedBlob()
        {
            Assert.IsNull(PoolLooks.For(new Color(0.35f, 0.65f, 0.95f, 0.85f)), "water is not ice");
            Assert.IsNull(PoolLooks.For(new Color(0.92f, 0.88f, 0.75f, 0.85f)), "bone");
            Assert.IsNull(PoolLooks.For(new Color(0.55f, 0.5f, 0.45f, 1f)), "rock");
        }

        [Test]
        public void EveryDrawnPool_Moves_AndTheLivelyOnesThrowSpray()
        {
            // The owner, 2026-11-01: the pools "need to be animated".
            foreach (var name in new[] { "pool_fire", "pool_poison", "pool_frost", "pool_void", "pool_blood" })
            {
                var m = PoolLooks.MotionOf(name);
                Assert.AreNotEqual(0f, m.SpinA, name);
                if (name == "pool_void")
                    Assert.Greater(m.SpinA * m.SpinB, 0f, "the void's copies both swirl one way");
                else
                    Assert.Less(m.SpinA * m.SpinB, 0f, name + ": its copies turn opposite ways");
                Assert.Greater(m.PulseDepth, 0f, name);
            }
            Assert.AreEqual(PoolLooks.Spray.Embers, PoolLooks.MotionOf("pool_fire").Spray);
            Assert.AreEqual(PoolLooks.Spray.Bubbles, PoolLooks.MotionOf("pool_poison").Spray);
            Assert.AreEqual(PoolLooks.Spray.Glints, PoolLooks.MotionOf("pool_frost").Spray);
            Assert.AreEqual(PoolLooks.Spray.VoidSparks, PoolLooks.MotionOf("pool_void").Spray);
            Assert.Greater(PoolLooks.MotionOf("pool_fire").PulseSpeed, PoolLooks.MotionOf("pool_poison").PulseSpeed, "fire flickers quicker than slime");
        }
    }
}
