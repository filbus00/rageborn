using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>Bows only (Docs/03, 2026-09-30): the short bow, the longbow and the quiver, in the old weapon,
    /// two-handed and shield kinds.</summary>
    public class GripTests
    {
        static Item Bow(int level = 5, params AffixRoll[] affixes) => new Item(ItemSlot.Weapon, ItemRarity.Magic, level, affixes);
        static Item Longbow(int level = 5) => new Item(ItemSlot.TwoHandWeapon, ItemRarity.Magic, level);
        static Item Quiver(int level = 5, params AffixRoll[] affixes) => new Item(ItemSlot.Shield, ItemRarity.Magic, level, affixes);

        [Test]
        public void EveryBow_IsOneGrip_WithTheQuiverInTheOffHand()
        {
            var bow = EquipmentState.Empty.Equip(ItemSlot.Weapon, Bow()).Equip(ItemSlot.OffHand, Quiver());
            var look = AppearanceRules.For(bow);
            Assert.AreEqual(CharacterGrip.OneHand, look.Grip);
            Assert.AreEqual("recurve_bow", look.Weapon);
            Assert.AreEqual("studded_quiver", look.OffHand);
            var longbow = bow.Equip(ItemSlot.Weapon, Longbow(8));
            Assert.AreEqual(CharacterGrip.OneHand, AppearanceRules.For(longbow).Grip);
            Assert.AreEqual("great_bow", AppearanceRules.For(longbow).Weapon);
        }

        [Test]
        public void ALongbow_KeepsTheQuiver()
        {
            var quivered = EquipmentState.Empty.Equip(ItemSlot.Weapon, Bow()).Equip(ItemSlot.OffHand, Quiver());
            var displaced = new List<Item>();
            var longbow = quivered.Equip(ItemSlot.Weapon, Longbow(), displaced);
            Assert.IsNotNull(longbow.OffHand, "both bows leave the off-hand to the quiver");
            Assert.AreEqual(1, displaced.Count, "only the short bow comes off");
            Assert.AreEqual(ItemSlot.Weapon, displaced[0].Slot);
        }

        [Test]
        public void ABow_GoesOnlyInTheWeaponPlace()
        {
            CollectionAssert.AreEqual(new[] { ItemSlot.Weapon }, EquipmentState.PlacesFor(ItemSlot.Weapon));
            CollectionAssert.AreEqual(new[] { ItemSlot.Weapon }, EquipmentState.PlacesFor(ItemSlot.TwoHandWeapon));
            CollectionAssert.AreEqual(new[] { ItemSlot.OffHand }, EquipmentState.PlacesFor(ItemSlot.Shield));
            var worn = EquipmentState.Empty.Equip(ItemSlot.Weapon, Bow(10));
            Assert.AreEqual(ItemSlot.Weapon, PowerScore.PlaceFor(worn, Bow(8), 1));
            Assert.IsFalse(worn.IsDualWield);
        }

        [Test]
        public void TheNumbers_FollowTheDocs()
        {
            Assert.AreEqual(1.3f * CombatFormulas.WeaponAverageDamage(10), Longbow(10).WeaponAverageDamage, 1e-3f);
            Assert.AreEqual(5f, Quiver(2).QuiverAttackSpeedPercent);
            Assert.AreEqual(7f, Quiver(5).QuiverAttackSpeedPercent);
            Assert.AreEqual(9f, Quiver(9).QuiverAttackSpeedPercent);
            Assert.AreEqual(0f, Bow().QuiverAttackSpeedPercent);
            Assert.AreEqual(0f, Quiver(9).ArmorValue, "a quiver is no armor");

            var longbow = EquipmentState.Empty.Equip(ItemSlot.Weapon, Longbow());
            Assert.AreEqual(0.8f, longbow.GripAttackSpeedFactor, 1e-5f);
            Assert.AreEqual(1.5f, longbow.GripReach, 1e-5f);
            Assert.AreEqual(0f, EquipmentState.Empty.Equip(ItemSlot.Weapon, Bow()).GripReach);
            Assert.AreEqual(0.07f, EquipmentState.Empty.Equip(ItemSlot.OffHand, Quiver(5)).GripAttackSpeedBonus, 1e-5f);
            Assert.AreEqual(0f, EquipmentState.Empty.Equip(ItemSlot.OffHand, Quiver(5)).BlockChance, "nothing blocks");
        }

        [Test]
        public void TheScore_CountsTheQuiver_TheExtraArrow_AndTheLongbowsDps()
        {
            var one = EquipmentState.Empty.Equip(ItemSlot.Weapon, Bow(10));
            var quivered = one.Equip(ItemSlot.OffHand, Quiver(10));
            Assert.Greater(PowerScore.Evaluate(quivered, 1).DamagePerSecond, PowerScore.Evaluate(one, 1).DamagePerSecond);

            var extra = one.Equip(ItemSlot.OffHand, Quiver(10, new AffixRoll(AffixId.ExtraArrowChance, 5, 10f)));
            Assert.AreEqual(PowerScore.Evaluate(quivered, 1).DamagePerSecond * 1.1f, PowerScore.Evaluate(extra, 1).DamagePerSecond, 0.01f);

            var longbow = EquipmentState.Empty.Equip(ItemSlot.Weapon, Longbow(10));
            Assert.AreEqual(PowerScore.Evaluate(one, 1).DamagePerSecond * 1.3f * 0.8f, PowerScore.Evaluate(longbow, 1).DamagePerSecond, 0.01f);
        }

        [Test]
        public void TheExtraArrow_RollsOnlyOnQuivers()
        {
            Assert.IsTrue(AffixTable.CanRollOn(AffixId.ExtraArrowChance, ItemSlot.Shield));
            Assert.IsFalse(AffixTable.CanRollOn(AffixId.ExtraArrowChance, ItemSlot.Weapon));
            Assert.IsFalse(AffixTable.CanRollOn(AffixId.Armor, ItemSlot.Shield), "a quiver rolls no armor");
        }

        [Test]
        public void ABowAndQuiver_SurviveASave()
        {
            var session = new GameSession();
            var bow = Longbow(9);
            var quiver = Quiver(3);
            session.Inventory.TryAdd(bow);
            session.Inventory.TryAdd(quiver);
            session.EquipFromInventory(bow);
            session.EquipFromInventory(quiver);

            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);
            Assert.IsTrue(restored.Equipment.IsLongbow);
            Assert.IsTrue(restored.Equipment.HasQuiver);
            Assert.AreEqual(9, restored.Equipment.Weapon.ItemLevel);
        }
    }
}
