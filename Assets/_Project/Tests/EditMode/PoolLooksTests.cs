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
        public void WaterHasItsOwnPool_BoneAndStoneKeepTheTintedBlob()
        {
            Assert.AreEqual("pool_water", PoolLooks.For(new Color(0.35f, 0.65f, 0.95f, 0.85f)), "water is not ice");
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

        [Test]
        public void EveryPool_IsDrawnInCode_RoundWithAnEdgeAndAllItsTones()
        {
            // The owner, 2026-11-03: "remove the damage pool assets that are imported. Use your own".
            foreach (var name in new[] { "pool_fire", "pool_poison", "pool_frost", "pool_void", "pool_blood", "pool_water" })
            {
                var pixels = PoolArt.Pixels(name);
                Assert.AreEqual(PoolArt.Size * PoolArt.Size, pixels.Length);
                Assert.AreEqual(0, pixels[0].a, name + ": a corner is clear");
                Assert.AreEqual(255, pixels[PoolArt.Size / 2 + PoolArt.Size / 2 * PoolArt.Size].a, name + ": the middle is filled");
                var tones = new System.Collections.Generic.HashSet<Color32>();
                foreach (var p in pixels)
                    if (p.a > 0)
                        tones.Add(p);
                Assert.GreaterOrEqual(tones.Count, 3, name + " has a pattern, not a flat fill");
            }
        }
    
        [Test]
        public void ABorderAndItsInside_MakeUpThePool_WithNothingInBoth()
        {
            // 2026-11-16: a pool's border and its inside are two pictures, so the borders of overlapping pools of one kind
            // can lie under the fills and only the outer edge of the whole shows.
            foreach (var name in new[] { "pool_fire", "pool_poison", "pool_frost", "pool_void", "pool_blood", "pool_water" })
            {
                var all = PoolArt.Frame(name, 1.2f, 3);
                var rim = PoolArt.Frame(name, 1.2f, 3, PoolArt.PoolLayer.Rim);
                var fill = PoolArt.Frame(name, 1.2f, 3, PoolArt.PoolLayer.Fill);
                var rims = 0;
                var fills = 0;
                for (var i = 0; i < all.Length; i++)
                {
                    Assert.IsFalse(rim[i].a > 0 && fill[i].a > 0, name + ": a pixel is the border's or the inside's");
                    Assert.AreEqual(all[i].a > 0, rim[i].a > 0 || fill[i].a > 0, name + ": together they are the pool");
                    if (rim[i].a > 0)
                        rims++;
                    if (fill[i].a > 0)
                        fills++;
                }
                Assert.Greater(rims, 20, name + " has a border");
                Assert.Greater(fills, rims, name + " is mostly inside");
                int w = PoolArt.WidthFor(1.2f), h = PoolArt.HeightFor(1.2f);
                Assert.AreEqual(0, fill[w / 2 + 1 + 1 * w].a, name + ": the inside stops short of the edge");
                Assert.AreEqual(255, fill[w / 2 + h / 2 * w].a, name + ": the middle is the inside's");
            }
        }

        [Test]
        public void EveryKind_DrawsItsBorderUnderItsFill_AndAboveTheKindsBeforeIt()
        {
            var names = new[] { "pool_fire", "pool_poison", "pool_frost", "pool_void", "pool_blood", "pool_water" };
            foreach (var name in names)
            {
                Assert.Less(PoolArt.GlowOrder, PoolArt.RimOrder(name));
                Assert.Less(PoolArt.RimOrder(name), PoolArt.FillOrder(name), name + ": the border under the fill");
                Assert.Less(PoolArt.EdgeOrder(name), PoolArt.FillOrder(name));
            }
            for (var i = 1; i < names.Length; i++)
                Assert.Greater(PoolArt.RimOrder(names[i]), PoolArt.FillOrder(names[i - 1]), "each kind above the last");
            Assert.Less(PoolArt.FillOrder("pool_water"), 40, "under the telegraphs");
        }

        [Test]
        public void Pools_AreDrawnOnTheScreensGrid_AtTheirSize_AndMove()
        {
            // 2026-11-05: a pool of radius r is 2r units wide on screen at 27 a unit, half as tall, shown unscaled.
            Assert.AreEqual(54, PoolArt.WidthFor(1f));
            Assert.AreEqual(27, PoolArt.HeightFor(1f));
            Assert.AreEqual(0, PoolArt.WidthFor(1.3f) % 2, "even, so its middle is on a pixel edge");
            foreach (var name in new[] { "pool_fire", "pool_poison", "pool_frost", "pool_void", "pool_blood", "pool_water" })
            {
                var first = PoolArt.Frame(name, 1.2f, 0);
                int w = PoolArt.WidthFor(1.2f), h = PoolArt.HeightFor(1.2f);
                Assert.AreEqual(w * h, first.Length);
                Assert.AreEqual(0, first[0].a, name + ": a corner is clear");
                Assert.AreEqual(255, first[w / 2 + h / 2 * w].a, name + ": the middle is filled");
                var changed = false;
                var later = PoolArt.Frame(name, 1.2f, PoolArt.Frames / 4);
                for (var i = 0; i < first.Length && !changed; i++)
                    changed = !first[i].Equals(later[i]);
                Assert.IsTrue(changed, name + " moves from frame to frame");
            }
        }
    }
}
