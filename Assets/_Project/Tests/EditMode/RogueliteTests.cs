using System.Linq;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class RogueliteTests
    {
        [Test]
        public void ANewDepth_OwesABoon_AndSetsHerLevel_NoPointsAreEarned()
        {
            var session = new GameSession(3);
            Assert.AreEqual(0, session.PendingBoons);
            Assert.IsTrue(session.ReachDepth(1));
            Assert.IsTrue(session.ReachDepth(10));
            Assert.IsFalse(session.ReachDepth(5), "an old depth owes nothing");
            Assert.AreEqual(2, session.PendingBoons);
            Assert.AreEqual(BoonRules.LevelAt(10), session.Level);
            Assert.AreEqual(0, session.Talents.Available(session.Level));
            Assert.AreEqual(0, AttributePoints.Earned(session.Level));
            Assert.AreEqual(0, session.GrantExperience(100000));
        }

        [Test]
        public void TakingABoon_GrantsARank_AndPaysTheChoice()
        {
            var session = new GameSession(3);
            session.ReachDepth(1);
            var offer = BoonRules.Offer(session.Talents, session.Meta.MaxBoonTier, session.Meta.BoonChoices, new System.Random(1));
            Assert.AreEqual(3, offer.Count);
            Assert.AreEqual(offer.Count, offer.Distinct().Count());
            Assert.IsTrue(session.TakeBoon(offer[0]));
            Assert.AreEqual(1, session.Talents.RankOf(offer[0]));
            Assert.AreEqual(0, session.PendingBoons);
            Assert.IsFalse(session.TakeBoon(offer[1]), "no choice owed");
        }

        [Test]
        public void TheOffer_KeepsToOpenRows_AndRequirements()
        {
            var talents = new TalentState();
            for (var seed = 0; seed < 200; seed++)
                foreach (var id in BoonRules.Offer(talents, MetaProgress.BaseBoonTier, 4, new System.Random(seed)))
                {
                    var node = TalentRules.Get(id);
                    Assert.Less(node.Tier, MetaProgress.BaseBoonTier, id);
                    Assert.IsTrue(node.Requires == null || talents.RankOf(node.Requires) > 0, id + " needs " + node.Requires);
                }
        }

        [Test]
        public void Upgrades_CostCoins_AndAddTheirBonus()
        {
            var session = new GameSession(3);
            Assert.IsFalse(session.BuyUpgrade(MetaUpgradeId.Might), "no coins");
            session.AddGold(MetaRules.Cost(MetaUpgradeId.Might, 0));
            var before = session.Talents.Bonuses.IncreasedDamage;
            Assert.IsTrue(session.BuyUpgrade(MetaUpgradeId.Might));
            Assert.AreEqual(0, session.Gold);
            Assert.AreEqual(before + 0.05f, session.Talents.Bonuses.IncreasedDamage, 1e-5f);
            Assert.Greater(MetaRules.Cost(MetaUpgradeId.Might, 1), MetaRules.Cost(MetaUpgradeId.Might, 0));
        }

        [Test]
        public void TheNextRun_KeepsCoinsUpgradesPetsAndStash_AndResetsTheRest()
        {
            var session = new GameSession(3);
            session.AddGold(100000);
            session.BuyUpgrade(MetaUpgradeId.StashSlot);
            session.BuyUpgrade(MetaUpgradeId.StartBow);
            session.BuyPet(PetKind.Wolf);
            session.ReachDepth(4);
            session.TakeBoon(BoonRules.Offer(session.Talents, 2, 3, new System.Random(2))[0]);
            var item = session.Loot.RollItemOf(ItemSlot.Helm, ItemRarity.Rare, 5);
            Assert.IsTrue(session.PickUp(item));
            Assert.IsTrue(session.SendToStash(item));
            Assert.IsFalse(session.SendToStash(session.Equipment.Weapon), "one item a run");
            var gold = session.Gold;

            var next = session.NextRun(9);
            Assert.AreEqual(gold, next.Gold);
            Assert.AreEqual(1, next.Meta.Rank(MetaUpgradeId.StashSlot));
            Assert.IsTrue(next.Pets.Owns(PetKind.Wolf));
            Assert.AreEqual(1, next.Stash.Count);
            Assert.AreEqual(0, next.Talents.All.Count(), "boons reset");
            Assert.AreEqual(1, next.Level);
            Assert.AreEqual(0, next.DeepestDepth);
            Assert.IsFalse(next.RunStarted);
            Assert.AreEqual(ItemRarity.Magic, next.Equipment.Weapon.Rarity, "the bought starting bow");
            Assert.AreEqual(1, next.Meta.Runs);
        }

        [Test]
        public void TheMeta_AndTheRun_SurviveASave()
        {
            var session = new GameSession(3);
            session.AddGold(50000);
            session.BuyUpgrade(MetaUpgradeId.Vitality);
            session.BuyUpgrade(MetaUpgradeId.Reroll);
            session.ReachDepth(1);
            session.ReachDepth(2);
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(SaveCodec.Capture(session, 1)), out var data, out var error), error);
            var loaded = SaveCodec.Restore(data, 1);
            Assert.AreEqual(1, loaded.Meta.Rank(MetaUpgradeId.Vitality));
            Assert.AreEqual(2, loaded.PendingBoons);
            Assert.AreEqual(1, loaded.RerollsLeft);
            Assert.IsTrue(loaded.RunStarted);
            Assert.AreEqual(2, loaded.Meta.DeepestEver);
        }

        [Test]
        public void ASaveFromBeforeTheRoguelite_IsSetAside()
        {
            Assert.IsTrue(SaveCodec.IsRetired(new SaveData { readFromVersion = 16 }));
            Assert.IsFalse(SaveCodec.IsRetired(new SaveData { readFromVersion = SaveData.CurrentVersion }));
        }
    }
}
