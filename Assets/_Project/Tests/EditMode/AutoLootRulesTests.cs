using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class AutoLootRulesTests
    {
        [Test]
        public void TheConstants_MatchTheDocs()
        {
            Assert.AreEqual(2.5f, AutoLootRules.PickupRadius, 1e-6f);
            Assert.AreEqual(1.5f, AutoLootRules.SafeSeconds, 1e-6f);
        }

        [Test]
        public void InRange_IsInclusiveAtTwoAndAHalfUnits()
        {
            var origin = new Vector2(3f, -1f);

            Assert.IsTrue(AutoLootRules.InRange(origin, origin));
            Assert.IsTrue(AutoLootRules.InRange(origin, origin + new Vector2(2.5f, 0f)));
            Assert.IsTrue(AutoLootRules.InRange(origin, origin + new Vector2(0f, -2.4f)));
            Assert.IsFalse(AutoLootRules.InRange(origin, origin + new Vector2(2.6f, 0f)));
        }

        [Test]
        public void InRange_UsesGroundDistance_InEveryDirection()
        {
            var origin = Vector2.zero;
            for (var degrees = 0; degrees < 360; degrees += 30)
            {
                var direction = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

                Assert.IsTrue(AutoLootRules.InRange(origin, direction * 2.49f), $"{degrees} degrees, just inside");
                Assert.IsFalse(AutoLootRules.InRange(origin, direction * 2.51f), $"{degrees} degrees, just outside");
            }
        }

        [Test]
        public void ItemsAreBlocked_WhileHitRecentlyAndEnemiesAreEngaged()
        {
            Assert.IsFalse(AutoLootRules.CanPickUpItems(0.2f, 5));
            Assert.IsFalse(AutoLootRules.CanPickUpItems(1.49f, 1));
        }

        [Test]
        public void ItemsAreAllowed_OnceNotHitFor1500Milliseconds_EvenInAFight()
        {
            Assert.IsTrue(AutoLootRules.CanPickUpItems(1.5f, 8));
            Assert.IsTrue(AutoLootRules.CanPickUpItems(30f, 8));
        }

        [Test]
        public void ItemsAreAllowed_WhenTheRoomIsClear_EvenRightAfterAHit()
        {
            Assert.IsTrue(AutoLootRules.CanPickUpItems(0f, 0));
            Assert.IsTrue(AutoLootRules.CanPickUpItems(0.4f, 0));
        }

        [Test]
        public void AFreshCharacter_CountsAsNotRecentlyHit()
        {
            Assert.IsTrue(AutoLootRules.CanPickUpItems(float.MaxValue, 12));
        }
    }
}
