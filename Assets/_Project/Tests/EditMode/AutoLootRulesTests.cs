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

        [Test]
        public void AnItem_LiesOnTheGround_ForASecondAndAHalf_First()
        {
            Assert.IsFalse(AutoLootRules.ItemReady(0f));
            Assert.IsFalse(AutoLootRules.ItemReady(1.4f));
            Assert.IsTrue(AutoLootRules.ItemReady(1.5f));
        }

        [Test]
        public void ThePickupRule_SetsARarityFloor()
        {
            Assert.IsTrue(AutoLootRules.Wants(PickupRule.Everything, ItemRarity.Common, false));
            Assert.IsFalse(AutoLootRules.Wants(PickupRule.MagicAndBetter, ItemRarity.Common, false));
            Assert.IsTrue(AutoLootRules.Wants(PickupRule.MagicAndBetter, ItemRarity.Magic, false));
            Assert.IsFalse(AutoLootRules.Wants(PickupRule.RareAndBetter, ItemRarity.Magic, false));
            Assert.IsTrue(AutoLootRules.Wants(PickupRule.RareAndBetter, ItemRarity.Rare, false));
            Assert.IsFalse(AutoLootRules.Wants(PickupRule.UpgradesOnly, ItemRarity.Rare, false));
        }

        [Test]
        public void Legendaries_AndUpgrades_AreAlwaysTaken()
        {
            foreach (PickupRule rule in System.Enum.GetValues(typeof(PickupRule)))
            {
                Assert.IsTrue(AutoLootRules.Wants(rule, ItemRarity.Legendary, false), rule.ToString());
                Assert.IsTrue(AutoLootRules.Wants(rule, ItemRarity.Common, true), rule + ": an upgrade");
                Assert.IsFalse(string.IsNullOrEmpty(AutoLootRules.Describe(rule)), rule + " has a name");
            }
        }
    }
}
