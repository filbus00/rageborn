using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The company's gear (2026-11-26): Thorin's armour, the fletcher's bows and quivers, the jeweller's rings,
    /// amulet and trinkets, raised a rank at a time with materials picked up in the levels.</summary>
    public class CompanyGearTests
    {
        [Test]
        public void APiece_IsAlwaysTheSameItem_AndGrowsWithItsRank()
        {
            var first = CompanyGearRules.Make(ItemSlot.Chest, 4);
            var again = CompanyGearRules.Make(ItemSlot.Chest, 4);
            Assert.AreEqual(first.ItemLevel, again.ItemLevel);
            Assert.AreEqual(first.Affixes.Count, again.Affixes.Count);
            for (var i = 0; i < first.Affixes.Count; i++)
                Assert.AreEqual(first.Affixes[i].Value, again.Affixes[i].Value);

            var low = CompanyGearRules.Make(ItemSlot.Weapon, 1);
            Assert.AreEqual(ItemRarity.Magic, low.Rarity, "no Common pieces");
            Assert.AreEqual(1, low.ItemLevel);
            Assert.AreEqual(1, low.Affixes.Count);
            Assert.AreEqual(1, low.Issue);
            var previous = low;
            for (var rank = 2; rank <= CompanyGearRules.MaxRank; rank++)
            {
                var piece = CompanyGearRules.Make(ItemSlot.Weapon, rank);
                Assert.Greater(piece.ItemLevel, previous.ItemLevel, "rank " + rank);
                Assert.GreaterOrEqual(piece.Affixes.Count, previous.Affixes.Count);
                Assert.GreaterOrEqual(piece.FlatWeaponDamageBonus, previous.FlatWeaponDamageBonus);
                previous = piece;
            }
            Assert.AreEqual(ItemRarity.Rare, previous.Rarity);
            Assert.AreEqual(33, previous.ItemLevel, "the top rank matches the last depth's enemies");
        }

        [Test]
        public void ATrinket_IsFound_Mythic_AndFarStrongerThanGear()
        {
            var random = new System.Random(3);
            foreach (var design in TrinketRules.Designs)
            {
                var trinket = TrinketRules.Make(design, 10, random);
                Assert.AreEqual(ItemSlot.Trinket, trinket.Slot);
                Assert.AreEqual(ItemRarity.Mythic, trinket.Rarity);
                Assert.AreEqual(2, trinket.Affixes.Count, design.Name);
                Assert.AreSame(design, TrinketRules.Of(trinket));
                Assert.AreEqual(design.Name, ItemComparison.Name(trinket));
                for (var i = 0; i < 2; i++)
                {
                    var id = design.Affixes[i];
                    if (!AffixTable.Get(id).Fixed)
                        Assert.Greater(trinket.Affixes[i].Value, AffixTable.Get(id).T1Max, design.Name + " " + id + " beats the best gear roll");
                }
            }
            Assert.AreEqual(new UnityEngine.Vector2Int(1, 1), ItemSize.Of(ItemSlot.Trinket));
            Assert.IsTrue(AutoLootRules.Wants(PickupRule.UpgradesOnly, ItemRarity.Mythic, false), "always picked up");
        }

        [Test]
        public void Trinkets_DropFarMoreRarelyThanLegendaries()
        {
            foreach (LootSource source in System.Enum.GetValues(typeof(LootSource)))
                Assert.Less(TrinketRules.DropChance(source), source == LootSource.Boss ? LootRoller.BossLegendaryChance : 0.01f, source.ToString());
            var roller = new LootRoller(5);
            var found = 0;
            for (var i = 0; i < 20000; i++)
                if (roller.RollTrinket(LootSource.Elite, 10) != null)
                    found++;
            Assert.That(found, Is.InRange(15, 70), "about one elite in five hundred");
        }

        [Test]
        public void KillsDropNoCommonItems()
        {
            var roller = new LootRoller(9);
            for (var i = 0; i < 40000; i++)
                foreach (var item in roller.RollDrops(LootSource.NormalEnemy, 5, 0f))
                    Assert.AreNotEqual(ItemRarity.Common, item.Rarity);
        }

        [Test]
        public void TheKeepers_RecycleFoundGear_IntoTheirMaterial()
        {
            var session = new GameSession(1);
            var chest = new Item(ItemSlot.Chest, ItemRarity.Rare, 13);
            var bow = new Item(ItemSlot.Weapon, ItemRarity.Magic, 1);
            var ring = new Item(ItemSlot.Ring, ItemRarity.Legendary, 1);
            session.PickUp(chest);
            session.PickUp(bow);
            session.PickUp(ring);
            Assert.AreEqual(4, session.Recycle(chest), "Rare 3, and 1 for item level 12 and over");
            Assert.AreEqual(4, session.Meta.Material(CompanyMaterial.Iron));
            Assert.AreEqual(1, session.Recycle(bow));
            Assert.AreEqual(1, session.Meta.Material(CompanyMaterial.Heartwood));
            Assert.AreEqual(8, session.Recycle(ring));
            Assert.AreEqual(8, session.Meta.Material(CompanyMaterial.Gems));
            Assert.AreEqual(0, session.Inventory.Items.Count);
            Assert.AreEqual(0, session.Recycle(chest), "gone already");
            Assert.AreEqual(0, CompanyGearRules.RecycleYield(CompanyGearRules.Make(ItemSlot.Chest, 3)), "company pieces give nothing");
            Assert.AreEqual(20, CompanyGearRules.RecycleYield(TrinketRules.Make(TrinketRules.Designs[0], 1, new System.Random(1))));
        }

        [Test]
        public void TheKitAfterADeath_IsTheCompanysGear_InEveryPlace()
        {
            var session = new GameSession(1);
            var next = session.NextRun(2, false);
            foreach (var place in GameSession.CompanyPlaces)
            {
                var piece = next.Equipment.Get(place);
                Assert.IsNotNull(piece, place.ToString());
                Assert.IsTrue(piece.IsCompanyIssue, place.ToString());
            }
            Assert.AreEqual(ItemSlot.Weapon, next.Equipment.Weapon.Slot);
            Assert.AreEqual(ItemSlot.Shield, next.Equipment.OffHand.Slot);
            Assert.IsNull(next.Equipment.Get(ItemSlot.Trinket), "trinkets are found, never issued");
        }

        [Test]
        public void RaisingARank_SpendsMaterialAndCoins_AndIssuesTheNewPieceAtOnce()
        {
            var session = new GameSession(1);
            session.IssueCompanyKit();
            Assert.AreEqual(1, session.Equipment.Chest.Issue);
            Assert.IsFalse(session.RaiseCompanyGear(ItemSlot.Chest), "no iron");
            session.AddCompanyMaterial(CompanyMaterial.Iron, 5);
            Assert.IsFalse(session.RaiseCompanyGear(ItemSlot.Chest), "no coins");
            session.AddGold(1000);
            Assert.IsTrue(session.RaiseCompanyGear(ItemSlot.Chest));
            Assert.AreEqual(2, session.Meta.GearRank(ItemSlot.Chest));
            Assert.AreEqual(5 - CompanyGearRules.MaterialCost(1), session.Meta.Material(CompanyMaterial.Iron));
            Assert.AreEqual(1000 - CompanyGearRules.CoinCost(1), session.Gold);
            Assert.AreEqual(2, session.Equipment.Chest.Issue, "the worn piece is issued again");
            Assert.AreEqual(2, session.NextRun(3, false).Equipment.Chest.Issue, "and every archer after her starts in it");
        }

        [Test]
        public void TheCompany_FillsEmptyPlaces_ButLeavesLootSheWears()
        {
            var session = new GameSession(1);
            var found = new Item(ItemSlot.Helm, ItemRarity.Rare, 10);
            session.PickUp(found);
            Assert.IsTrue(session.EquipFromInventory(found, ItemSlot.Helm));
            session.IssueCompanyKit();
            Assert.AreSame(found, session.Equipment.Helm);
            Assert.IsTrue(session.Equipment.Chest.IsCompanyIssue);

            Assert.IsTrue(session.WearCompanyPiece(ItemSlot.Helm));
            Assert.IsTrue(session.Equipment.Helm.IsCompanyIssue);
            CollectionAssert.Contains(new List<Item>(session.Inventory.Items), found);
        }

        [Test]
        public void TheBowIssued_IsChosen()
        {
            var session = new GameSession(1);
            session.IssueCompanyKit();
            session.ChooseIssuedBow(ItemSlot.TwoHandWeapon);
            Assert.AreEqual(ItemSlot.TwoHandWeapon, session.Equipment.Weapon.Slot);
        }

        [Test]
        public void CompanyPieces_AreNotSold()
        {
            Assert.AreEqual(0, SellRules.Price(CompanyGearRules.Make(ItemSlot.Chest, 5)));
            Assert.IsFalse(SellRules.InBulkSale(CompanyGearRules.Make(ItemSlot.Chest, 5), ItemRarity.Rare, false));
        }

        [Test]
        public void Materials_DropMostFromBossesAndChests_AndMoreDeeper()
        {
            var random = new System.Random(4);
            Assert.IsTrue(CompanyGearRules.RollDrop(LootSource.Boss, 6, random, out _, out var amount));
            Assert.AreEqual(6, amount);
            Assert.AreEqual(1, CompanyGearRules.DropAmount(LootSource.NormalEnemy, 12));
            Assert.AreEqual(2, CompanyGearRules.DropAmount(LootSource.NormalEnemy, 13));
            var iron = 0;
            for (var i = 0; i < 1000; i++)
                if (CompanyGearRules.PickMaterial(random) == CompanyMaterial.Iron)
                    iron++;
            Assert.That(iron, Is.InRange(400, 600), "five pieces in ten are armour");
        }

        [Test]
        public void TheCompanysGear_SurvivesASave_AndAFormat24SaveTakesRanksFromBlueprints()
        {
            var session = new GameSession(1);
            session.Meta.SetGearRank(ItemSlot.Boots, 4);
            session.Meta.AddMaterial(CompanyMaterial.Gems, 7);
            session.Meta.IssuedBow = ItemSlot.TwoHandWeapon;
            session.Meta.IssuedTrinket = "frost_bead";
            session.IssueCompanyKit();
            var data = SaveCodec.Capture(session, 1);
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data), out var parsed, out var error), error);
            var restored = SaveCodec.Restore(parsed, 2);
            Assert.AreEqual(4, restored.Meta.GearRank(ItemSlot.Boots));
            Assert.AreEqual(7, restored.Meta.Material(CompanyMaterial.Gems));
            Assert.AreEqual(ItemSlot.TwoHandWeapon, restored.Meta.IssuedBow);
            Assert.AreEqual("frost_bead", restored.Meta.IssuedTrinket);
            Assert.AreEqual(4, restored.Equipment.Get(ItemSlot.Boots).Issue, "a worn company piece keeps its rank");

            var old = "{\"version\":24,\"savedAtUnixMs\":1,\"gold\":10,\"lifeFraction\":1.0,\"level\":1,\"equipped\":[],\"backpack\":[]," +
                      "\"metaBlueprints\":[\"Chest/2\",\"Ring/1\"]}";
            Assert.IsTrue(SaveCodec.TryParse(old, out var oldData, out error), error);
            var fromOld = SaveCodec.Restore(oldData, 3);
            Assert.AreEqual(5, fromOld.Meta.GearRank(ItemSlot.Chest));
            Assert.AreEqual(3, fromOld.Meta.GearRank(ItemSlot.Ring));
            Assert.AreEqual(1, fromOld.Meta.GearRank(ItemSlot.Helm));
        }
    }
}
