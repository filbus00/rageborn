using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>Plain item names, blueprints and Thorin's forge, the stash chest and the portal's keeping (2026-10-15).</summary>
    public class BlueprintTests
    {
        [Test]
        public void Items_AreCalledByTheirLook_AndLegendariesKeepTheirNames()
        {
            Assert.AreEqual("Hunting Bow", ItemComparison.Name(new Item(ItemSlot.Weapon, ItemRarity.Rare, 1)));
            Assert.AreEqual("Horn Bow", ItemComparison.Name(new Item(ItemSlot.Weapon, ItemRarity.Common, 20)));
            Assert.AreEqual("Scale Mail", ItemComparison.Name(new Item(ItemSlot.Chest, ItemRarity.Magic, 9)));
            Assert.AreEqual("Silver Ring", ItemComparison.Name(new Item(ItemSlot.Ring, ItemRarity.Magic, 10)));
            Assert.AreEqual("Blueprint: Scale Mail", ItemComparison.Name(new Item(ItemSlot.Chest, ItemRarity.Rare, 9, blueprint: true)));
            var legendary = Legendaries.Create(LegendaryId.Gallowsreach, 10, new System.Random(1));
            Assert.AreEqual(legendary.LegendaryDefinition.Name, ItemComparison.Name(legendary));
        }

        [Test]
        public void EveryLook_HasANameAndALevelThatShowsIt()
        {
            foreach (var kind in BlueprintRules.Kinds)
                for (var look = 0; look < ItemNames.LookCount(kind); look++)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(ItemNames.LookName(kind, look)), $"{kind} {look}");
                    Assert.AreEqual(look, ItemNames.LookIndex(kind, ItemNames.LevelOfLook(kind, look)), $"{kind} {look}");
                    Assert.IsTrue(BlueprintRules.TryParse(BlueprintRules.Key(kind, look), out var parsed, out var index));
                    Assert.AreEqual(kind, parsed);
                    Assert.AreEqual(look, index);
                }
            Assert.IsFalse(BlueprintRules.TryParse("Chest/9", out _, out _));
            Assert.IsFalse(BlueprintRules.TryParse("nonsense", out _, out _));
        }

        [Test]
        public void ABlueprint_IsOneCell_NeverWorn_AndNoUpgrade()
        {
            var session = new GameSession(1);
            var blueprint = new Item(ItemSlot.Chest, ItemRarity.Rare, 5, blueprint: true);
            Assert.AreEqual(UnityEngine.Vector2Int.one, ItemSize.Of(blueprint));
            Assert.IsTrue(session.Inventory.TryAdd(blueprint));
            Assert.IsFalse(session.EquipFromInventory(blueprint));
            Assert.IsFalse(session.EquipFromInventory(blueprint, ItemSlot.Chest));
            Assert.IsFalse(PowerScore.IsUpgrade(session.Equipment, blueprint, 1));
        }

        [Test]
        public void ABlueprintRoll_TeachesOnlyWhatIsNotKnown()
        {
            var random = new System.Random(3);
            var known = new List<string>();
            foreach (var kind in BlueprintRules.Kinds)
                if (kind != ItemSlot.Boots)
                    known.Add(BlueprintRules.Key(kind, ItemNames.LookIndex(kind, 5)));
            var only = BlueprintRules.Roll(5, known, random);
            Assert.IsTrue(only.IsBlueprint);
            Assert.AreEqual(ItemSlot.Boots, only.Slot);
            known.Add(BlueprintRules.Key(ItemSlot.Boots, ItemNames.LookIndex(ItemSlot.Boots, 5)));
            Assert.IsNull(BlueprintRules.Roll(5, known, random));
            Assert.Greater(BlueprintRules.DropChance(LootSource.Boss), BlueprintRules.DropChance(LootSource.Elite));
            Assert.Greater(BlueprintRules.DropChance(LootSource.Elite), BlueprintRules.DropChance(LootSource.NormalEnemy));
        }

        [Test]
        public void HomeThroughThePortal_KeepsWhatSheCarries_AndThorinLearnsTheBlueprints()
        {
            var session = new GameSession(1);
            session.ReachDepth(1);
            var bow = new Item(ItemSlot.Weapon, ItemRarity.Rare, 4);
            var blueprint = new Item(ItemSlot.Chest, ItemRarity.Rare, 9, blueprint: true);
            Assert.IsTrue(session.Inventory.TryAdd(bow));
            Assert.IsTrue(session.Inventory.TryAdd(blueprint));
            var worn = session.Equipment;

            Assert.AreEqual(1, session.LearnCarriedBlueprints());
            var next = session.NextRun(2, true);
            Assert.IsTrue(next.Meta.Knows("Chest/2"));
            CollectionAssert.Contains(new List<Item>(next.Inventory.Items), bow);
            CollectionAssert.DoesNotContain(new List<Item>(next.Inventory.Items), blueprint);
            Assert.AreSame(worn.Weapon, next.Equipment.Weapon);

            var afterDeath = next.NextRun(3, false);
            Assert.AreEqual(0, afterDeath.Inventory.Items.Count);
            Assert.IsTrue(afterDeath.Meta.Knows("Chest/2"), "what Thorin knows is kept");
        }

        [Test]
        public void Thorin_ForgesAKnownBlueprint_IntoTheBag_AndTheKitAfterADeath()
        {
            var session = new GameSession(1);
            Assert.IsNull(session.Forge("Chest/2"), "unknown");
            session.Meta.Learn("Chest/2");
            Assert.IsNull(session.Forge("Chest/2"), "no coins");
            session.AddGold(100000);
            var item = session.Forge("Chest/2");
            Assert.IsNotNull(item);
            Assert.AreEqual(ItemSlot.Chest, item.Slot);
            Assert.AreEqual(ItemRarity.Rare, item.Rarity);
            Assert.AreEqual("Scale Mail", ItemComparison.Name(item));
            Assert.AreEqual(100000 - BlueprintRules.ForgeCost(2), session.Gold);
            CollectionAssert.Contains(new List<Item>(session.Inventory.Items), item);
            Assert.AreSame(item, session.NextRun(2, false).Equipment.Chest);
        }

        [Test]
        public void TheStashChest_StartsWithTenSlots_AndGrowsByTen()
        {
            var session = new GameSession(1);
            Assert.AreEqual(10, session.Meta.StashSlots);
            session.AddGold(100000);
            Assert.IsTrue(session.BuyUpgrade(MetaUpgradeId.StashSlot));
            Assert.AreEqual(20, session.Meta.StashSlots);
            var ring = new Item(ItemSlot.Ring, ItemRarity.Magic, 3);
            Assert.IsTrue(session.Inventory.TryAdd(ring));
            Assert.IsTrue(session.StoreInStash(ring));
            var next = session.NextRun(2, false);
            CollectionAssert.Contains(new List<Item>(next.Stash), ring);
        }

        [Test]
        public void TheSave_KeepsBlueprintsAndTheForge()
        {
            var session = new GameSession(1);
            session.AddGold(100000);
            session.Meta.Learn("Weapon/1");
            var forged = session.Forge("Weapon/1");
            Assert.IsTrue(session.Inventory.TryAdd(new Item(ItemSlot.Helm, ItemRarity.Rare, 6, blueprint: true)));
            var loaded = SaveCodec.Restore(SaveCodec.Capture(session, 0), 3);
            Assert.IsTrue(loaded.Meta.Knows("Weapon/1"));
            Assert.AreEqual(forged.ItemLevel, loaded.Meta.ForgedFor(ItemSlot.Weapon).ItemLevel);
            Assert.IsTrue(System.Linq.Enumerable.Any(loaded.Inventory.Items, i => i.IsBlueprint && i.Slot == ItemSlot.Helm));
        }

        [Test]
        public void Thorin_StandsClearOfTheTownsBuildings()
        {
            foreach (var piece in TownLayout.Pieces)
                Assert.IsFalse(TownLayout.Covers(piece, SmithNpc.Cell, 0.5f), $"Thorin stands on {piece.Name}");
        }

        [Test]
        public void EveryBoon_HasAnIcon_AndNoPlainStatBoonIsLeft()
        {
            foreach (var node in TalentRules.Nodes)
            {
                Assert.AreEqual(BoonIconArt.Size * BoonIconArt.Size, BoonIconArt.Draw(node).Length, node.Id);
                Assert.AreNotEqual(PassiveEffect.CriticalChance, node.Effect, node.Id);
                Assert.AreNotEqual(PassiveEffect.MoveSpeed, node.Effect, node.Id);
                Assert.AreNotEqual(PassiveEffect.ArmorPercent, node.Effect, node.Id);
            }
        }

        [Test]
        public void ABoss_FlinchesAtMostOnceEveryFiveSeconds()
        {
            Assert.AreEqual(5f, EnemyAnimationRules.FlinchGap(EnemyRank.Boss));
            Assert.AreEqual(0f, EnemyAnimationRules.FlinchGap(EnemyRank.Elite));
        }

        [Test]
        public void Garrett_BuysLoot_ButNotBlueprints()
        {
            var session = new GameSession(1);
            var bow = new Item(ItemSlot.Weapon, ItemRarity.Rare, 10);
            var blueprint = new Item(ItemSlot.Chest, ItemRarity.Rare, 9, blueprint: true);
            Assert.IsTrue(session.Inventory.TryAdd(bow));
            Assert.IsTrue(session.Inventory.TryAdd(blueprint));
            Assert.AreEqual(0, session.Sell(blueprint), "Thorin learns blueprints; they are not sold");
            Assert.AreEqual(UnityEngine.Mathf.RoundToInt(SellRules.Price(bow) * SellRules.RogueliteFactor), session.SellPrice(bow));
            var before = session.Gold;
            Assert.AreEqual(session.SellPrice(bow), session.Sell(bow));
            Assert.AreEqual(before + session.SellPrice(bow), session.Gold);
            Assert.IsFalse(SellRules.InBulkSale(blueprint, ItemRarity.Rare, false));
        }
    }
}
