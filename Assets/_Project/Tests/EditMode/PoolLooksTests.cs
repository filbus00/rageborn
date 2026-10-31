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
    }
}
