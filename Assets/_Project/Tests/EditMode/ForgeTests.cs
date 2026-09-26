using System;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class ForgeTests
    {
        static Item RareWeapon(int reforges = 0) => new Item(ItemSlot.Weapon, ItemRarity.Rare, 12, new[]
        {
            new AffixRoll(AffixId.FlatWeaponDamage, 5, 15f),
            new AffixRoll(AffixId.IncreasedDamage, 4, 6f),
            new AffixRoll(AffixId.AttackSpeed, 5, 2f),
            new AffixRoll(AffixId.CriticalChance, 4, 1.8f),
        }, reforges);

        static Item LegendaryChest() => new Item(ItemSlot.Chest, ItemRarity.Legendary, 12, new[]
        {
            new AffixRoll(AffixId.Life, 4, 70f),
            new AffixRoll(AffixId.Armor, 5, 20f),
        });

        static GameSession RichSession()
        {
            var session = new GameSession(1);
            session.AddGold(100000);
            foreach (CraftingMaterial material in Enum.GetValues(typeof(CraftingMaterial)))
                session.AddMaterial(material, 100);
            return session;
        }

        [TestCase(ItemRarity.Common, CraftingMaterial.Ash, 2)]
        [TestCase(ItemRarity.Magic, CraftingMaterial.Cinders, 2)]
        [TestCase(ItemRarity.Rare, CraftingMaterial.Bloodstone, 2)]
        [TestCase(ItemRarity.Legendary, CraftingMaterial.Soulglass, 1)]
        public void Salvage_GivesTheRaritysMaterial(ItemRarity rarity, CraftingMaterial material, int amount)
        {
            var session = new GameSession();
            var item = new Item(ItemSlot.Helm, rarity, 5);
            session.PickUp(item);

            Assert.IsTrue(session.Salvage(item));

            Assert.AreEqual(amount, session.Materials(material));
            Assert.IsFalse(session.Inventory.Contains(item));
        }

        [Test]
        public void Salvage_LeavesWornItemsAlone()
        {
            var session = new GameSession();
            var weapon = session.Equipment.Weapon;

            Assert.IsFalse(session.Salvage(weapon));
            Assert.AreSame(weapon, session.Equipment.Weapon);
            Assert.AreEqual(0, session.Materials(CraftingMaterial.Ash));
        }

        [Test]
        public void Reforge_ReplacesOnlyTheChosenAffix_WithOneOfTheSameKindAndTier()
        {
            var random = new Random(3);
            for (var run = 0; run < 200; run++)
            {
                var item = RareWeapon();
                var index = run % item.Affixes.Count;
                var result = ForgeRules.Reforge(item, index, random);

                Assert.AreEqual(1, result.Reforges);
                Assert.AreEqual(item.Affixes.Count, result.Affixes.Count);
                for (var i = 0; i < item.Affixes.Count; i++)
                {
                    if (i == index)
                        continue;
                    Assert.AreEqual(item.Affixes[i].Id, result.Affixes[i].Id);
                    Assert.AreEqual(item.Affixes[i].Value, result.Affixes[i].Value);
                }

                var fresh = result.Affixes[index];
                Assert.AreEqual(item.Affixes[index].Tier, fresh.Tier);
                Assert.AreEqual(AffixTable.Get(item.Affixes[index].Id).Kind, AffixTable.Get(fresh.Id).Kind);
                Assert.IsTrue(AffixTable.CanRollOn(fresh.Id, ItemSlot.Weapon));
                var (min, max) = AffixRoller.ValueRange(fresh.Id, fresh.Tier);
                Assert.That(fresh.Value, Is.InRange(min, max));
                for (var i = 0; i < result.Affixes.Count; i++)
                    if (i != index)
                        Assert.AreNotEqual(result.Affixes[i].Id, fresh.Id, "an affix twice on one item");
            }
        }

        [Test]
        public void Reforge_CanChangeTheStat()
        {
            // A weapon has four suffixes; with Critical chance taken, reforging Attack speed picks from it and the other two.
            var random = new Random(9);
            var changed = false;
            for (var run = 0; run < 50 && !changed; run++)
                changed = ForgeRules.Reforge(RareWeapon(), 2, random).Affixes[2].Id != AffixId.AttackSpeed;
            Assert.IsTrue(changed);
        }

        [Test]
        public void OnlyRares_AreReforged()
        {
            Assert.IsFalse(ForgeRules.CanReforge(LegendaryChest()));
            Assert.IsFalse(ForgeRules.CanReforge(new Item(ItemSlot.Weapon, ItemRarity.Magic, 5, new[] { new AffixRoll(AffixId.Life, 5, 40f) })));
            Assert.Throws<InvalidOperationException>(() => ForgeRules.Reforge(LegendaryChest(), 0, new Random(1)));
        }

        [Test]
        public void EachReforge_CostsAQuarterMore()
        {
            var first = ForgeRules.ReforgeCost(RareWeapon(0));
            var second = ForgeRules.ReforgeCost(RareWeapon(1));
            var third = ForgeRules.ReforgeCost(RareWeapon(2));

            Assert.AreEqual(CraftingMaterial.Bloodstone, first.Material);
            Assert.AreEqual(2, first.Amount);
            Assert.AreEqual(3, second.Amount); // 2.5, rounded up
            Assert.AreEqual(4, third.Amount);  // 3.125
            // Item level 12: 0.6 x 12^1.3 = 15.2, times 10.
            Assert.AreEqual(152, first.Gold);
            Assert.AreEqual(190, second.Gold);
            Assert.AreEqual(238, third.Gold);
        }

        [Test]
        public void Reroll_KeepsAffixesAndTiers_AndRollsValuesInRange()
        {
            var item = LegendaryChest();
            var random = new Random(4);
            var anyChanged = false;
            for (var run = 0; run < 20; run++)
            {
                var result = ForgeRules.Reroll(item, random);
                for (var i = 0; i < item.Affixes.Count; i++)
                {
                    Assert.AreEqual(item.Affixes[i].Id, result.Affixes[i].Id);
                    Assert.AreEqual(item.Affixes[i].Tier, result.Affixes[i].Tier);
                    var (min, max) = AffixRoller.ValueRange(result.Affixes[i].Id, result.Affixes[i].Tier);
                    Assert.That(result.Affixes[i].Value, Is.InRange(min, max));
                    anyChanged |= Math.Abs(result.Affixes[i].Value - item.Affixes[i].Value) > 0.01f;
                }
            }
            Assert.IsTrue(anyChanged);
            Assert.IsFalse(ForgeRules.CanReroll(RareWeapon()));
            Assert.AreEqual(CraftingMaterial.Soulglass, ForgeRules.RerollCost(item).Material);
        }

        [Test]
        public void Temper_RaisesATier_KeepingTheValuesPlaceInItsRange()
        {
            var item = LegendaryChest();
            var (oldMin, oldMax) = AffixRoller.ValueRange(AffixId.Life, 4);
            var place = (70f - oldMin) / (oldMax - oldMin);

            var result = ForgeRules.Temper(item, 0);

            Assert.AreEqual(3, result.Affixes[0].Tier);
            var (newMin, newMax) = AffixRoller.ValueRange(AffixId.Life, 3);
            Assert.AreEqual(newMin + place * (newMax - newMin), result.Affixes[0].Value, 0.001f);
            Assert.Greater(result.Affixes[0].Value, 70f);
            Assert.AreEqual(item.Affixes[1].Value, result.Affixes[1].Value);
            Assert.AreEqual(1, result.Tempers);
        }

        [Test]
        public void Temper_StopsAtTierOne_AndAfterThreeUses()
        {
            var best = new Item(ItemSlot.Helm, ItemRarity.Rare, 60, new[] { new AffixRoll(AffixId.Life, 1, 200f), new AffixRoll(AffixId.Armor, 5, 20f) });
            Assert.IsFalse(ForgeRules.CanTemper(best, 0), "already T1");
            Assert.IsTrue(ForgeRules.CanTemper(best, 1));

            var item = best;
            for (var i = 0; i < ForgeRules.MaxTempers; i++)
                item = ForgeRules.Temper(item, 1);

            Assert.AreEqual(2, item.Affixes[1].Tier);
            Assert.IsFalse(ForgeRules.CanTemper(item));
            Assert.IsFalse(ForgeRules.CanTemper(item, 1));
        }

        [Test]
        public void ApplyForge_ReplacesABackpackItemInPlace_AndPays()
        {
            var session = RichSession();
            var first = new Item(ItemSlot.Helm, ItemRarity.Common, 1);
            var item = RareWeapon();
            var last = new Item(ItemSlot.Helm, ItemRarity.Common, 1);
            session.PickUp(first);
            session.PickUp(item);
            session.PickUp(last);
            var cost = ForgeRules.ReforgeCost(item);
            var result = ForgeRules.Reforge(item, 0, session.ForgeRandom);

            Assert.IsTrue(session.ApplyForge(item, result, cost));

            Assert.AreSame(result, session.Inventory.Items[1]);
            Assert.IsFalse(session.Inventory.Contains(item));
            Assert.AreEqual(100000 - cost.Gold, session.Gold);
            Assert.AreEqual(100 - cost.Amount, session.Materials(CraftingMaterial.Bloodstone));
        }

        [Test]
        public void ApplyForge_ReplacesAWornItem()
        {
            var session = RichSession();
            var item = LegendaryChest();
            session.Equip(session.Equipment.With(ItemSlot.Chest, item));
            var result = ForgeRules.Temper(item, 0);

            Assert.IsTrue(session.ApplyForge(item, result, ForgeRules.TemperCost(item)));
            Assert.AreSame(result, session.Equipment.Chest);
        }

        [Test]
        public void ApplyForge_ChangesNothing_WhenItCannotBePaid()
        {
            var session = new GameSession();
            session.AddGold(10000);
            var item = RareWeapon();
            session.PickUp(item);
            var result = ForgeRules.Reforge(item, 0, session.ForgeRandom);

            Assert.IsFalse(session.ApplyForge(item, result, ForgeRules.ReforgeCost(item)), "no Bloodstone");
            Assert.AreSame(item, session.Inventory.Items[0]);
            Assert.AreEqual(10000, session.Gold);
        }

        [Test]
        public void ApplyForge_ChangesNothing_ForAnItemTheCharacterDoesNotHave()
        {
            var session = RichSession();
            var item = RareWeapon();
            var result = ForgeRules.Reforge(item, 0, session.ForgeRandom);

            Assert.IsFalse(session.ApplyForge(item, result, ForgeRules.ReforgeCost(item)));
            Assert.AreEqual(100000, session.Gold);
        }

        [Test]
        public void ForgeActions_RaiseModified_SoTheyAreSaved()
        {
            var session = RichSession();
            var item = RareWeapon();
            session.PickUp(item);
            var count = 0;
            session.Modified += () => count++;

            session.ApplyForge(item, ForgeRules.Reforge(item, 0, session.ForgeRandom), ForgeRules.ReforgeCost(item));
            Assert.AreEqual(1, count, "forge");
            session.Salvage(session.Inventory.Items[0]);
            Assert.AreEqual(2, count, "salvage");
            session.AddMaterial(CraftingMaterial.Soulglass, 3);
            Assert.AreEqual(3, count, "material drop");
        }

        [Test]
        public void ARoundTrip_KeepsMaterials_AndForgeCounts()
        {
            var session = new GameSession(2);
            session.AddMaterial(CraftingMaterial.Bloodstone, 7);
            session.AddMaterial(CraftingMaterial.Soulglass, 2);
            var worn = new Item(ItemSlot.Chest, ItemRarity.Legendary, 10, new[] { new AffixRoll(AffixId.Life, 4, 60f) }, reforges: 0, tempers: 2);
            session.Equip(session.Equipment.With(ItemSlot.Chest, worn));
            session.PickUp(RareWeapon(reforges: 3));

            var json = SaveCodec.ToJson(SaveCodec.Capture(session, 1));
            Assert.IsTrue(SaveCodec.TryParse(json, out var data, out var error), error);
            var loaded = SaveCodec.Restore(data, 1);

            Assert.AreEqual(7, loaded.Materials(CraftingMaterial.Bloodstone));
            Assert.AreEqual(2, loaded.Materials(CraftingMaterial.Soulglass));
            Assert.AreEqual(0, loaded.Materials(CraftingMaterial.Ash));
            Assert.AreEqual(2, loaded.Equipment.Chest.Tempers);
            Assert.AreEqual(3, loaded.Inventory.Items[0].Reforges);
        }

        [Test]
        public void AVersionFiveSave_LoadsWithNoMaterials_AndItsItemsNeverForged()
        {
            const string versionFive =
                "{\"version\":5,\"savedAtUnixMs\":1790193596882,\"gold\":60,\"lifeFraction\":1.0,\"killsSinceLegendary\":0," +
                "\"level\":4,\"experience\":10,\"potionCharges\":3,\"potionKillProgress\":0,\"dungeonSeed\":42,\"dungeonVersion\":3," +
                "\"equipped\":[{\"slot\":\"Weapon\",\"rarity\":\"Rare\",\"itemLevel\":8,\"affixes\":[{\"id\":\"Life\",\"tier\":5,\"value\":40.0}]}]," +
                "\"backpack\":[],\"corpses\":[],\"killed\":[],\"openedChests\":[]}";

            Assert.IsTrue(SaveCodec.TryParse(versionFive, out var data, out var error), error);
            var session = SaveCodec.Restore(data, 1);

            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            foreach (CraftingMaterial material in Enum.GetValues(typeof(CraftingMaterial)))
                Assert.AreEqual(0, session.Materials(material));
            Assert.AreEqual(0, session.Equipment.Weapon.Reforges);
            Assert.AreEqual(0, session.Equipment.Weapon.Tempers);
            Assert.AreEqual(4, session.Level);
            Assert.AreEqual(60, session.Gold);
        }
    }
}
