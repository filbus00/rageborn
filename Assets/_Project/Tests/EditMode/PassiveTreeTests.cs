using System.Linq;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class PassiveTreeTests
    {
        [Test]
        public void TheTree_HasDocs02sShape()
        {
            var nodes = PassiveTree.Nodes;
            Assert.AreEqual(60, nodes.Count);
            Assert.AreEqual(36, nodes.Count(n => n.Kind == PassiveKind.Minor));
            Assert.AreEqual(18, nodes.Count(n => n.Kind == PassiveKind.Notable));
            Assert.AreEqual(3, nodes.Count(n => n.Kind == PassiveKind.Gateway));
            Assert.AreEqual(2, nodes.Count(n => n.Kind == PassiveKind.Keystone));
            Assert.AreEqual(63, nodes.Sum(n => n.Cost), "buying everything costs 63; 59 points at level 60 leave 4 nodes out");
            Assert.AreEqual(59, PassiveTree.EarnedPoints(60));
            Assert.AreEqual(nodes.Count, nodes.Select(n => n.Id).Distinct().Count(), "ids are unique");
        }

        [Test]
        public void ABranch_IsBoughtInOrder_TheGatewayWaitsForFifteenPoints_AndKeystonesForLevelTwenty()
        {
            var tree = new PassiveTree();
            var wrath = PassiveTree.Nodes.Where(n => n.Branch == PassiveBranch.Wrath).ToList();
            Assert.AreEqual("needs " + wrath[0].Name, tree.Blocker(wrath[1], 30), "the second node needs the first");
            for (var i = 0; i < 9; i++)
                Assert.IsTrue(tree.Buy(wrath[i].Id, 30), wrath[i].Name);
            var gateway = wrath[9];
            Assert.AreEqual(PassiveKind.Gateway, gateway.Kind);
            Assert.AreEqual($"opens after {PassiveTree.GatewayPoints} points", tree.Blocker(gateway, 30), "9 points spent");
            var scar = PassiveTree.Nodes.Where(n => n.Branch == PassiveBranch.Scar).ToList();
            for (var i = 0; i < 6; i++)
                Assert.IsTrue(tree.Buy(scar[i].Id, 30));
            Assert.IsTrue(tree.Buy(gateway.Id, 30), "15 points anywhere open it");
            for (var i = 10; i < 19; i++)
                Assert.IsTrue(tree.Buy(wrath[i].Id, 40));
            var berserker = PassiveTree.Find("berserker");
            Assert.AreEqual($"opens at level {PassiveTree.KeystoneLevel}", tree.Blocker(berserker, 19));
            Assert.AreEqual("needs 3 points", tree.Blocker(berserker, 26), "25 points earned, all 25 spent... then one more level each");
            Assert.IsTrue(tree.Buy("berserker", 29));
            Assert.AreEqual("berserker", tree.ActiveKeystone, "the first keystone bought is in effect");
            Assert.IsTrue(tree.Bonuses.Berserker);
        }

        [Test]
        public void Bonuses_AddUp_AndOnlyTheActiveKeystoneCounts()
        {
            var tree = new PassiveTree();
            var wrath = PassiveTree.Nodes.Where(n => n.Branch == PassiveBranch.Wrath).ToList();
            tree.Buy(wrath[0].Id, 10); // Fury, +4% damage
            tree.Buy(wrath[1].Id, 10); // Keen Edge, +1.5% crit
            tree.Buy(wrath[2].Id, 10); // Red Mist
            Assert.AreEqual(0.04f, tree.Bonuses.IncreasedDamage, 1e-5f);
            Assert.AreEqual(1.5f, tree.Bonuses.CriticalChance, 1e-5f);
            Assert.AreEqual(2f, tree.Bonuses.RagePerBasicHit, 1e-5f, "8 Rage a basic hit instead of 6");

            tree.Restore(new[] { "berserker", "juggernaut" }, "juggernaut");
            Assert.IsTrue(tree.Bonuses.Juggernaut);
            Assert.IsFalse(tree.Bonuses.Berserker, "a bought keystone waits until the loadout picks it");
            tree.SetActiveKeystone("berserker");
            Assert.IsTrue(tree.Bonuses.Berserker);
            Assert.IsFalse(tree.Bonuses.Juggernaut);
        }

        [Test]
        public void Juggernaut_KeepsHalfTheMomentum_AndTailwindRaisesTheCap()
        {
            var stance = new StanceStacks { MomentumCap = 6, KeepHalfOnStop = true };
            for (var i = 0; i < 40; i++)
                stance.Tick(0.1f, true);
            Assert.AreEqual(6, stance.Momentum, "Tailwind's cap of 6");
            for (var i = 0; i < 20; i++)
                stance.Tick(0.1f, false);
            Assert.AreEqual(3, stance.Momentum, "a stop loses half, once");
            var plain = new StanceStacks();
            for (var i = 0; i < 40; i++)
                plain.Tick(0.1f, true);
            for (var i = 0; i < 20; i++)
                plain.Tick(0.1f, false);
            Assert.AreEqual(0, plain.Momentum, "without Juggernaut a stop loses all");
        }

        [Test]
        public void Berserker_DoublesRageGains()
        {
            var rage = new RagePool { GainMultiplier = 2f };
            rage.Gain(6f);
            Assert.AreEqual(12f, rage.Current, 1e-5f);
        }

        [Test]
        public void TheTree_IsSaved_AndAVersionTenSaveStartsEmpty()
        {
            var session = new GameSession(1);
            while (session.Level < 3)
                session.GrantExperience(Experience.XpToNextLevel(session.Level));
            var first = PassiveTree.Nodes.First(n => n.Branch == PassiveBranch.Scar);
            Assert.IsTrue(session.PassiveTree.Buy(first.Id, session.Level));
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(SaveCodec.Capture(session, 1)), out var data, out var error), error);
            Assert.IsTrue(SaveCodec.Restore(data, 1).PassiveTree.IsBought(first.Id));

            const string versionTen =
                "{\"version\":10,\"savedAtUnixMs\":1,\"gold\":10,\"lifeFraction\":1.0,\"level\":7," +
                "\"equipped\":[],\"backpack\":[],\"corpses\":[],\"killed\":[],\"openedChests\":[],\"materials\":[],\"skillLevels\":[]}";
            Assert.IsTrue(SaveCodec.TryParse(versionTen, out var old, out error), error);
            Assert.AreEqual(SaveData.CurrentVersion, old.version);
            var restored = SaveCodec.Restore(old, 1);
            Assert.AreEqual(6, restored.PassiveTree.Available(7), "every point earned is there to spend");
            Assert.IsNull(restored.PassiveTree.ActiveKeystone);
        }
    }
}
