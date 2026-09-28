using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class ItemSlotTests
    {
        static readonly ItemSlot[] Droppable =
        {
            ItemSlot.Weapon, ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt, ItemSlot.Amulet, ItemSlot.Ring,
            ItemSlot.Shield, ItemSlot.TwoHandWeapon,
        };

        static Item Ring(params AffixRoll[] affixes) => new Item(ItemSlot.Ring, ItemRarity.Magic, 5, affixes);

        [Test]
        public void EverySlot_CanRollAtLeastOnePrefixOrSuffix()
        {
            foreach (var slot in Droppable)
            {
                var any = false;
                foreach (AffixId id in Enum.GetValues(typeof(AffixId)))
                    any |= AffixTable.CanRollOn(id, slot);
                Assert.IsTrue(any, slot.ToString());
            }
        }

        [Test]
        public void ArmorBase_FollowsTheSlotsShare()
        {
            var full = CombatFormulas.BaseArmorPerPiece(10);
            Assert.AreEqual(full, new Item(ItemSlot.Chest, ItemRarity.Common, 10).ArmorValue, 1e-4f);
            Assert.AreEqual(full * 0.6f, new Item(ItemSlot.Gloves, ItemRarity.Common, 10).ArmorValue, 1e-4f);
            Assert.AreEqual(full * 0.6f, new Item(ItemSlot.Boots, ItemRarity.Common, 10).ArmorValue, 1e-4f);
            Assert.AreEqual(full * 0.4f, new Item(ItemSlot.Belt, ItemRarity.Common, 10).ArmorValue, 1e-4f);
            Assert.AreEqual(0f, new Item(ItemSlot.Amulet, ItemRarity.Common, 10).ArmorValue);
            Assert.AreEqual(0f, Ring().ArmorValue);
        }

        [Test]
        public void Drops_ComeInEveryKind_NeverAsRing2()
        {
            var roller = new LootRoller(3);
            var seen = new HashSet<ItemSlot>();
            for (var i = 0; i < 400; i++)
            {
                var item = roller.RollItem(ItemRarity.Rare, 10);
                seen.Add(item.Slot);
                Assert.AreNotEqual(ItemSlot.Ring2, item.Slot);
                foreach (var affix in item.Affixes)
                    Assert.IsTrue(AffixTable.CanRollOn(affix.Id, item.Slot), $"{affix.Id} on {item.Slot}");
            }
            CollectionAssert.AreEquivalent(Droppable, seen);
        }

        [Test]
        public void ASecondRing_GoesOnTheOtherHand_AThirdReplacesTheWeaker()
        {
            var session = new GameSession();
            var weak = Ring(new AffixRoll(AffixId.CriticalChance, 5, 1f));
            var strong = Ring(new AffixRoll(AffixId.CriticalChance, 5, 5f));
            var best = Ring(new AffixRoll(AffixId.CriticalChance, 5, 7f));
            foreach (var ring in new[] { strong, weak, best })
                session.Inventory.TryAdd(ring);

            Assert.IsTrue(session.EquipFromInventory(strong));
            Assert.IsTrue(session.EquipFromInventory(weak));
            Assert.AreSame(strong, session.Equipment.Get(ItemSlot.Ring));
            Assert.AreSame(weak, session.Equipment.Get(ItemSlot.Ring2));

            Assert.IsTrue(session.EquipFromInventory(best));
            Assert.AreSame(strong, session.Equipment.Get(ItemSlot.Ring), "the stronger ring stays");
            Assert.AreSame(best, session.Equipment.Get(ItemSlot.Ring2));
            Assert.IsTrue(session.Inventory.Contains(weak));
            Assert.AreEqual(12f, session.Equipment.CriticalChancePercent, 1e-4f);
        }

        [Test]
        public void TwoRings_SurviveASave()
        {
            var session = new GameSession();
            var first = Ring(new AffixRoll(AffixId.AttackSpeed, 5, 2f));
            var second = Ring(new AffixRoll(AffixId.DodgeChance, 5, 1f));
            session.Inventory.TryAdd(first);
            session.Inventory.TryAdd(second);
            session.EquipFromInventory(first);
            session.EquipFromInventory(second);

            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);

            Assert.AreEqual(2f, restored.Equipment.Get(ItemSlot.Ring).AttackSpeedPercent);
            Assert.AreEqual(1f, restored.Equipment.Get(ItemSlot.Ring2).DodgePercent);
        }

        [Test]
        public void MovementSpeedAndDodge_SumAcrossGear_AndDodgeCountsInEffectiveLife()
        {
            var boots = new Item(ItemSlot.Boots, ItemRarity.Magic, 5, new[] { new AffixRoll(AffixId.MovementSpeed, 5, 3f) });
            var belt = new Item(ItemSlot.Belt, ItemRarity.Magic, 5, new[] { new AffixRoll(AffixId.DodgeChance, 5, 2f) });
            var equipment = EquipmentState.Empty.With(ItemSlot.Boots, boots);
            var withBelt = equipment.With(ItemSlot.Belt, belt);

            Assert.AreEqual(3f, withBelt.MovementSpeedPercent);
            Assert.AreEqual(2f, withBelt.DodgePercent);
            var beltOnlyArmorAndDodge = PowerScore.Evaluate(withBelt, 1).EffectiveLife;
            var noDodge = PowerScore.Evaluate(equipment.With(ItemSlot.Belt, new Item(ItemSlot.Belt, ItemRarity.Common, 5)), 1).EffectiveLife;
            Assert.AreEqual(noDodge / 0.98f, beltOnlyArmorAndDodge, 1e-2f);
        }
    }
}
