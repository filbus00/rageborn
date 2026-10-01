using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>Named legendaries and ailments (Docs/03, the owner's decisions of 2026-10-01).</summary>
    public class LegendaryTests
    {
        [Test]
        public void EveryLegendary_CarriesAffixesItsKindCanRoll()
        {
            foreach (var legendary in Legendaries.All)
            {
                Assert.AreSame(legendary, Legendaries.Get(legendary.Id));
                foreach (var affix in legendary.FixedAffixes)
                    Assert.IsTrue(AffixTable.CanRollOn(affix, legendary.Slot), $"{legendary.Name}: {affix} on {legendary.Slot}");
            }
            Assert.IsNull(Legendaries.Get(LegendaryId.None));
        }

        [Test]
        public void AllTwentyFour_AreInTheTable_WithUniqueNames()
        {
            var names = new HashSet<string>();
            var count = 0;
            foreach (LegendaryId id in System.Enum.GetValues(typeof(LegendaryId)))
            {
                if (id == LegendaryId.None)
                    continue;
                var legendary = Legendaries.Get(id);
                Assert.IsNotNull(legendary, $"{id} has a definition");
                Assert.AreEqual(id, legendary.Id, "the table is in enum order");
                Assert.IsTrue(names.Add(legendary.Name), $"{legendary.Name} is unique");
                Assert.IsFalse(string.IsNullOrEmpty(legendary.Power));
                Assert.IsFalse(string.IsNullOrEmpty(legendary.HomeText));
                Assert.AreNotEqual(ItemSlot.OffHand, legendary.Slot, "an item's kind, never a place to wear it");
                Assert.AreNotEqual(ItemSlot.Ring2, legendary.Slot);
                count++;
            }
            Assert.AreEqual(24, count, "Docs/03's table");
        }

        [Test]
        public void EveryLegendary_CanDrop()
        {
            var seen = new HashSet<LegendaryId>();
            var random = new System.Random(11);
            for (var i = 0; i < 20000; i++)
                seen.Add(Legendaries.Pick(LootSource.NormalEnemy, 10, random));
            foreach (var legendary in Legendaries.All)
                Assert.IsTrue(seen.Contains(legendary.Id), legendary.Name);
        }

        [Test]
        public void Picking_IsNoneBelowTheMinimum_AndFavoursTheHome()
        {
            var random = new System.Random(7);
            Assert.AreEqual(LegendaryId.None, Legendaries.Pick(LootSource.Boss, Legendaries.MinItemLevel - 1, random));

            var counts = new Dictionary<LegendaryId, int>();
            for (var i = 0; i < 20000; i++)
            {
                var id = Legendaries.Pick(LootSource.Boss, 10, random);
                counts[id] = counts.TryGetValue(id, out var n) ? n + 1 : 1;
            }
            // Gallowsreach's home is the boss; Galeheart's is chests. At the boss the first should drop about twice as often.
            var ratio = counts[LegendaryId.Gallowsreach] / (float)counts[LegendaryId.Galeheart];
            Assert.AreEqual(Legendaries.HomeWeight, ratio, 0.3f);
            Assert.IsFalse(counts.ContainsKey(LegendaryId.None));
        }

        [Test]
        public void ALegendaryDrop_IsNamed_WithItsFixedAffixesFirst()
        {
            var roller = new LootRoller(3);
            Item named = null;
            for (var i = 0; i < 200 && named == null; i++)
                foreach (var item in roller.RollDrops(LootSource.Boss, 10, 0f))
                    if (item.Legendary != LegendaryId.None)
                        named = item;
            Assert.IsNotNull(named, "a boss drops a named legendary now and then");
            var legendary = named.LegendaryDefinition;
            Assert.AreEqual(legendary.Slot, named.Slot);
            Assert.AreEqual(ItemRarity.Legendary, named.Rarity);
            for (var i = 0; i < legendary.FixedAffixes.Count; i++)
                Assert.AreEqual(legendary.FixedAffixes[i], named.Affixes[i].Id);
            Assert.AreEqual(legendary.Name, ItemComparison.Name(named));
        }

        [Test]
        public void TheName_TheForge_AndTheSave_KeepTheLegendary()
        {
            var bow = new Item(ItemSlot.TwoHandWeapon, ItemRarity.Legendary, 10,
                new[] { new AffixRoll(AffixId.FlatWeaponDamage, 5, 10f), new AffixRoll(AffixId.CriticalDamage, 5, 6f) }, 0, 0, LegendaryId.Gallowsreach);
            Assert.AreEqual(LegendaryId.Gallowsreach, ForgeRules.Reroll(bow, new System.Random(1)).Legendary);
            Assert.AreEqual(LegendaryId.Gallowsreach, ForgeRules.Temper(bow, 0).Legendary);

            var session = new GameSession();
            session.PickUp(bow);
            Assert.IsTrue(session.HasSeen(LegendaryId.Gallowsreach), "picking one up records it in the Codex");
            session.EquipFromInventory(bow);
            Assert.IsTrue(session.Equipment.Wears(LegendaryId.Gallowsreach));

            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);
            Assert.AreEqual(LegendaryId.Gallowsreach, restored.Equipment.Weapon.Legendary);
            Assert.IsTrue(restored.HasSeen(LegendaryId.Gallowsreach));
            Assert.AreEqual("Magic Bow", ItemComparison.Name(new Item(ItemSlot.Weapon, ItemRarity.Magic, 3)));
        }

        [Test]
        public void Burn_StacksToThree_AndAFourthReplacesTheShortest()
        {
            var burn = new BurnStacks();
            burn.Apply(10f);
            burn.Tick(1f);
            burn.Apply(10f);
            burn.Apply(10f);
            Assert.AreEqual(3, burn.Count);
            burn.Apply(10f);
            Assert.AreEqual(3, burn.Count, "never more than three");
            var dealt = burn.Tick(0.5f);
            Assert.AreEqual(15f, dealt, 1e-3f, "three stacks of 10 a second for half a second");
            burn.Tick(10f);
            Assert.IsFalse(burn.IsBurning);
        }

        [Test]
        public void Chill_FreezesOnTheFourthWithinTwoSeconds_ButNeverABoss()
        {
            var chill = new ChillMeter();
            Assert.IsFalse(chill.Apply(true));
            Assert.IsFalse(chill.Apply(true));
            Assert.IsFalse(chill.Apply(true));
            Assert.IsTrue(chill.Apply(true));
            Assert.IsTrue(chill.IsFrozen);
            chill.Tick(ChillMeter.FreezeSeconds + 0.01f);
            Assert.IsFalse(chill.IsFrozen);

            var slowChill = new ChillMeter();
            for (var i = 0; i < 4; i++)
            {
                Assert.IsFalse(slowChill.Apply(true), "chills spread out do not add up");
                slowChill.Tick(ChillMeter.WindowSeconds + 0.1f);
            }

            var boss = new ChillMeter();
            for (var i = 0; i < 6; i++)
                Assert.IsFalse(boss.Apply(false));
        }

        [Test]
        public void TheAilmentAffixes_RollOnBowsQuiversAndGloves()
        {
            foreach (var slot in new[] { ItemSlot.Weapon, ItemSlot.TwoHandWeapon, ItemSlot.Shield, ItemSlot.Gloves })
            {
                Assert.IsTrue(AffixTable.CanRollOn(AffixId.IgniteChance, slot));
                Assert.IsTrue(AffixTable.CanRollOn(AffixId.ChillChance, slot));
            }
            Assert.IsFalse(AffixTable.CanRollOn(AffixId.IgniteChance, ItemSlot.Chest));
        }
    }
}
