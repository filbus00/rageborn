using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class GripTests
    {
        static Item Axe(int level = 5, params AffixRoll[] affixes) => new Item(ItemSlot.Weapon, ItemRarity.Magic, level, affixes);
        static Item GreatAxe(int level = 5) => new Item(ItemSlot.TwoHandWeapon, ItemRarity.Magic, level);
        static Item Shield(int level = 5) => new Item(ItemSlot.Shield, ItemRarity.Magic, level);

        [Test]
        public void GripsFollowTheHands()
        {
            var one = EquipmentState.Empty.Equip(ItemSlot.Weapon, Axe());
            Assert.AreEqual(CharacterGrip.OneHand, AppearanceRules.For(one).Grip);
            Assert.AreEqual(CharacterGrip.DualWield, AppearanceRules.For(one.Equip(ItemSlot.OffHand, Axe())).Grip);
            Assert.AreEqual(CharacterGrip.Shield, AppearanceRules.For(one.Equip(ItemSlot.OffHand, Shield())).Grip);
            Assert.AreEqual(CharacterGrip.TwoHand, AppearanceRules.For(one.Equip(ItemSlot.Weapon, GreatAxe())).Grip);
            Assert.AreEqual("round_shield", AppearanceRules.For(one.Equip(ItemSlot.OffHand, Shield())).OffHand);
        }

        [Test]
        public void ATwoHander_EmptiesTheOffHand_AndAnOffHandItem_TakesTheTwoHanderOut()
        {
            var shielded = EquipmentState.Empty.Equip(ItemSlot.Weapon, Axe()).Equip(ItemSlot.OffHand, Shield());
            var displaced = new List<Item>();
            var twoHanded = shielded.Equip(ItemSlot.Weapon, GreatAxe(), displaced);
            Assert.IsNull(twoHanded.OffHand);
            Assert.AreEqual(2, displaced.Count, "the axe and the shield come off");

            displaced.Clear();
            var back = twoHanded.Equip(ItemSlot.OffHand, Shield(), displaced);
            Assert.IsNull(back.Weapon);
            Assert.AreEqual(ItemSlot.TwoHandWeapon, displaced[0].Slot);
        }

        [Test]
        public void EquippingATwoHander_NeedsRoomForBothHands()
        {
            var session = new GameSession();
            var axe = Axe();
            var shield = Shield();
            var great = GreatAxe();
            session.Inventory.TryAdd(axe);
            session.Inventory.TryAdd(shield);
            session.Inventory.TryAdd(great);
            session.EquipFromInventory(axe);
            session.EquipFromInventory(shield);
            Assert.AreEqual(ItemSlot.OffHand, session.Equipment.PlaceOf(shield));

            Assert.IsTrue(session.EquipFromInventory(great));
            Assert.AreSame(great, session.Equipment.Weapon);
            Assert.IsNull(session.Equipment.OffHand);
            Assert.IsTrue(session.Inventory.Contains(axe) && session.Inventory.Contains(shield));
        }

        [Test]
        public void TheNumbers_FollowTheDocs()
        {
            Assert.AreEqual(1.6f * CombatFormulas.WeaponAverageDamage(10), GreatAxe(10).WeaponAverageDamage, 1e-3f);
            Assert.AreEqual(12f, Shield(2).BlockPercent);
            Assert.AreEqual(16f, Shield(5).BlockPercent);
            Assert.AreEqual(20f, Shield(9).BlockPercent);
            Assert.AreEqual(0f, Axe().BlockPercent);

            var main = Axe(10);
            var dual = EquipmentState.Empty.Equip(ItemSlot.Weapon, main).Equip(ItemSlot.OffHand, Axe(4));
            Assert.AreEqual(GripRules.DualWieldAttackSpeed, dual.GripAttackSpeedBonus);
            Assert.AreEqual(Axe(4).WeaponAverageDamage, dual.OffHandWeaponDamage, 1e-4f);
            var two = EquipmentState.Empty.Equip(ItemSlot.Weapon, GreatAxe());
            Assert.AreEqual(0.85f, two.GripAttackSpeedFactor);
            Assert.AreEqual(0.3f, two.GripReach, 1e-5f);
            Assert.AreEqual(0.16f, EquipmentState.Empty.Equip(ItemSlot.OffHand, Shield(5)).BlockChance, 1e-5f);
        }

        [Test]
        public void TheScore_CountsBlock_AndTheTwoHandersDps()
        {
            var one = EquipmentState.Empty.Equip(ItemSlot.Weapon, Axe(10));
            var shielded = one.Equip(ItemSlot.OffHand, Shield(10));
            Assert.Greater(PowerScore.Evaluate(shielded, 1).EffectiveLife, PowerScore.Evaluate(one, 1).EffectiveLife);

            var two = EquipmentState.Empty.Equip(ItemSlot.Weapon, GreatAxe(10));
            Assert.AreEqual(PowerScore.Evaluate(one, 1).DamagePerSecond * 1.6f * 0.85f, PowerScore.Evaluate(two, 1).DamagePerSecond, 0.01f);
        }

        [Test]
        public void ASecondAxe_ReplacesTheMainOrJoinsIt_ByScore()
        {
            var worn = EquipmentState.Empty.Equip(ItemSlot.Weapon, Axe(10));
            // A far weaker axe is worth more beside the main one (dual wield's speed) than instead of it.
            Assert.AreEqual(ItemSlot.OffHand, PowerScore.PlaceFor(worn, Axe(8), 1));
            // Into an empty main hand it always goes there.
            Assert.AreEqual(ItemSlot.Weapon, PowerScore.PlaceFor(EquipmentState.Empty, Axe(8), 1));
        }

        [Test]
        public void DualWield_SurvivesASave()
        {
            var session = new GameSession();
            var main = Axe(9);
            var off = Axe(3);
            session.Inventory.TryAdd(main);
            session.Inventory.TryAdd(off);
            session.EquipFromInventory(main);
            session.EquipFromInventory(off);
            Assert.IsTrue(session.Equipment.IsDualWield);

            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);
            Assert.IsTrue(restored.Equipment.IsDualWield);
            Assert.AreEqual(9, restored.Equipment.Weapon.ItemLevel);
        }
    }
}
