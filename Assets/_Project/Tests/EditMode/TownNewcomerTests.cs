using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The town's newcomers by depth reached, the stash, the Gambler and the Trainer (2026-10-05), and the save
    /// of format 14.</summary>
    public class TownNewcomerTests
    {
        [Test]
        public void Newcomers_ArriveAtTheOwnersDepths()
        {
            Assert.AreEqual(2, Newcomers.ArrivalDepth(Newcomer.StashKeeper));
            Assert.AreEqual(4, Newcomers.ArrivalDepth(Newcomer.Healer));
            Assert.AreEqual(8, Newcomers.ArrivalDepth(Newcomer.Gambler));
            Assert.AreEqual(12, Newcomers.ArrivalDepth(Newcomer.Trainer));
            Assert.IsFalse(Newcomers.HasArrived(Newcomer.StashKeeper, 1));
            Assert.IsTrue(Newcomers.HasArrived(Newcomer.StashKeeper, 2));
            Assert.IsTrue(Newcomers.TryArrivingAt(8, out var who));
            Assert.AreEqual(Newcomer.Gambler, who);
            Assert.IsFalse(Newcomers.TryArrivingAt(5, out _));
        }

        [Test]
        public void NewcomersStand_OffTheTownsBuildings_AndItsPoints()
        {
            foreach (var who in Newcomers.All)
            {
                var cell = Newcomers.Cell(who);
                foreach (var piece in TownLayout.Pieces)
                    Assert.IsFalse(TownLayout.Covers(piece, cell, 0.5f), $"{who} stands on {piece.Name}");
                foreach (var kept in TownLayout.KeptClear)
                    Assert.AreNotEqual(kept, cell, who.ToString());
            }
        }

        [Test]
        public void TheDeepestDepth_OnlyGrows()
        {
            var session = new GameSession(1);
            Assert.IsTrue(session.ReachDepth(3));
            Assert.IsFalse(session.ReachDepth(2));
            Assert.AreEqual(3, session.DeepestDepth);
            Assert.IsTrue(session.HasArrived(Newcomer.StashKeeper));
            Assert.IsFalse(session.HasArrived(Newcomer.Healer));
        }

        [Test]
        public void TheStash_TakesFromTheBackpack_AndGivesBack()
        {
            var session = new GameSession(1);
            var bow = new Item(ItemSlot.Weapon, ItemRarity.Magic, 5);
            Assert.IsTrue(session.PickUp(bow));
            Assert.IsTrue(session.StoreInStash(bow));
            Assert.IsFalse(session.Inventory.Contains(bow));
            CollectionAssert.Contains(session.Stash, bow);
            Assert.IsFalse(session.StoreInStash(bow), "not in the backpack any more");
            Assert.IsTrue(session.TakeFromStash(bow));
            Assert.IsTrue(session.Inventory.Contains(bow));
            Assert.AreEqual(0, session.Stash.Count);
        }

        [Test]
        public void TheGambler_SellsTheKindAsked_ForItsPrice()
        {
            var session = new GameSession(1);
            session.ReachDepth(8);
            var price = GambleRules.Price(ItemSlot.Boots, GambleRules.ItemLevel(8));
            session.AddGold(price);
            Assert.IsTrue(session.Gamble(ItemSlot.Boots, new System.Random(4), out var boots));
            Assert.AreEqual(ItemSlot.Boots, boots.Slot);
            Assert.AreNotEqual(ItemRarity.Common, boots.Rarity);
            Assert.AreEqual(0, session.Gold);
            Assert.IsFalse(session.Gamble(ItemSlot.Boots, new System.Random(4), out _), "no gold left");
        }

        [Test]
        public void GambleOdds_AreMostlyMagic_SomeRare_FewLegendary()
        {
            int magic = 0, rare = 0, legendary = 0;
            var random = new System.Random(7);
            for (var i = 0; i < 10000; i++)
            {
                switch (GambleRules.Rarity(random.NextDouble()))
                {
                    case ItemRarity.Magic: magic++; break;
                    case ItemRarity.Rare: rare++; break;
                    case ItemRarity.Legendary: legendary++; break;
                }
            }
            Assert.AreEqual(0.78f, magic / 10000f, 0.02f);
            Assert.AreEqual(0.20f, rare / 10000f, 0.02f);
            Assert.AreEqual(0.02f, legendary / 10000f, 0.01f);
        }

        [Test]
        public void TheTrainer_GivesEveryPointBack_ForAFee()
        {
            // A level 10 character, made the way a save makes one.
            var session = SaveCodec.Restore(new SaveData { version = SaveData.CurrentVersion, level = 10 }, 1);
            Assert.IsTrue(session.Attributes.Spend(Attribute.Strength, session.Level));
            session.Talents.Restore("split_arrow", 1);
            Assert.IsFalse(session.RespecStats(), "no gold");
            session.AddGold(RespecRules.StatsPrice(10) + RespecRules.SkillsPrice(10));
            Assert.IsTrue(session.RespecStats());
            Assert.AreEqual(0, session.Attributes.TotalSpent);
            Assert.IsTrue(session.RespecSkills());
            Assert.AreEqual(0, session.Talents.Spent);
            Assert.AreEqual(0, session.Gold);
        }

        [Test]
        public void ASave_KeepsTheDeepestDepthAndTheStash()
        {
            var session = new GameSession(1);
            session.ReachDepth(9);
            var helm = new Item(ItemSlot.Helm, ItemRarity.Rare, 7);
            session.PickUp(helm);
            session.StoreInStash(helm);
            var json = SaveCodec.ToJson(SaveCodec.Capture(session, 1));
            Assert.IsTrue(SaveCodec.TryParse(json, out var data, out var error), error);
            var loaded = SaveCodec.Restore(data, 1);
            Assert.AreEqual(9, loaded.DeepestDepth);
            Assert.AreEqual(1, loaded.Stash.Count);
            Assert.AreEqual(ItemSlot.Helm, loaded.Stash[0].Slot);
            Assert.AreEqual(ItemRarity.Rare, loaded.Stash[0].Rarity);
        }

        [Test]
        public void AFormat13Save_Loads_WithItsDeepestWaypointAsTheDeepestDepth()
        {
            var json = "{\"version\":13,\"savedAtUnixMs\":1,\"gold\":10,\"lifeFraction\":1.0,\"level\":8," +
                       "\"waypoints\":[1,3,6],\"equipped\":[],\"backpack\":[]}";
            Assert.IsTrue(SaveCodec.TryParse(json, out var data, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            Assert.AreEqual(13, data.readFromVersion);
            var session = SaveCodec.Restore(data, 1);
            Assert.AreEqual(6, session.DeepestDepth);
            Assert.AreEqual(0, session.Stash.Count);
            Assert.IsTrue(session.HasArrived(Newcomer.Healer));
        }
    }
}
