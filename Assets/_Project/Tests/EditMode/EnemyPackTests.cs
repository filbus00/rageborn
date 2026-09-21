using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class EnemyPackTests
    {
        [TestCase(3)]
        [TestCase(8)]
        [TestCase(12)]
        public void SlotOffsets_StayWithinTheRadius(int count)
        {
            const float radius = 2.2f;

            for (var i = 0; i < count; i++)
                Assert.LessOrEqual(EnemyPack.SlotOffset(i, count, radius).magnitude, radius, $"slot {i} of {count}");
        }

        [TestCase(3)]
        [TestCase(10)]
        [TestCase(12)]
        public void SlotOffsets_KeepMembersApart_AtTheDefaultRadius(int count)
        {
            // Enemies closer than the 0.8 separation radius push each other, so a fresh pack must not start inside it.
            const float radius = 2.2f;
            const float separationRadius = 0.8f;

            for (var a = 0; a < count; a++)
            for (var b = a + 1; b < count; b++)
            {
                var distance = Vector2.Distance(EnemyPack.SlotOffset(a, count, radius), EnemyPack.SlotOffset(b, count, radius));
                Assert.GreaterOrEqual(distance, separationRadius, $"slots {a} and {b} of {count}");
            }
        }

        [Test]
        public void SlotOffsets_AreCenteredOnThePack()
        {
            const int count = 10;
            var sum = Vector2.zero;

            for (var i = 0; i < count; i++)
                sum += EnemyPack.SlotOffset(i, count, 2f);

            Assert.Less((sum / count).magnitude, 0.3f);
        }

        [Test]
        public void SlotOffsets_AreDeterministic()
        {
            for (var i = 0; i < 12; i++)
                Assert.AreEqual(EnemyPack.SlotOffset(i, 12, 2f), EnemyPack.SlotOffset(i, 12, 2f));
        }

        [Test]
        public void SlotOffsets_ScaleWithTheRadius()
        {
            for (var i = 0; i < 8; i++)
            {
                var small = EnemyPack.SlotOffset(i, 8, 1f);
                var large = EnemyPack.SlotOffset(i, 8, 3f);

                Assert.AreEqual(3f * small.x, large.x, 1e-5f);
                Assert.AreEqual(3f * small.y, large.y, 1e-5f);
            }
        }
    }
}
