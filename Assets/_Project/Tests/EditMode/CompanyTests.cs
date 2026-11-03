using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class CompanyTests
    {
        [Test]
        public void EveryArcher_HasAFirstNameAndASurname_TheSameForTheSameSeed()
        {
            // The owner, 2026-11-03: "Make a random name generator. Each character gets a first and surname".
            var names = new HashSet<string>();
            for (var seed = 0; seed < 200; seed++)
            {
                var name = MercenaryNames.Full(seed);
                var parts = name.Split(' ');
                Assert.AreEqual(2, parts.Length, name);
                Assert.IsTrue(char.IsUpper(parts[0][0]) && char.IsUpper(parts[1][0]), name);
                Assert.AreEqual(name, MercenaryNames.Full(seed));
                names.Add(name);
            }
            Assert.Greater(names.Count, 180, "names hardly repeat");
        }

        [Test]
        public void Contracts_OpenOneAfterAnother_AsEachIsCleared()
        {
            var cleared = new HashSet<string>();
            Assert.IsTrue(ContractRules.IsOpen(ContractRules.All[0], cleared), "the Thornwood is open from the start");
            Assert.IsFalse(ContractRules.IsOpen(ContractRules.All[1], cleared));
            cleared.Add(ContractRules.All[0].Id);
            Assert.IsTrue(ContractRules.IsOpen(ContractRules.All[1], cleared));
            Assert.IsFalse(ContractRules.IsOpen(ContractRules.All[2], cleared));
        }

        [Test]
        public void Contracts_CoverTheDepthsInOrder_EachEndingOnABoss()
        {
            var next = 1;
            foreach (var contract in ContractRules.All)
            {
                Assert.AreEqual(next, contract.FirstDepth, contract.Name);
                Assert.IsTrue(DungeonRules.IsBossDepth(contract.LastDepth), contract.Name + " ends on a boss");
                Assert.AreEqual(2, contract.Bosses.Length);
                next = contract.LastDepth + 1;
            }
            Assert.AreEqual(DungeonRules.Depths + 1, next);
            Assert.IsTrue(ContractRules.All[0].Forest, "the Thornwood is the forest");
            Assert.AreEqual("iron_gaol", ContractRules.At(18).Id);
        }

        [Test]
        public void TheFallen_AreListedDeepestFirst_AndANewArcherSignsOn()
        {
            var company = new CompanyRecord();
            var random = new Random(3);
            company.Hire(random);
            var first = company.Mercenary;
            company.Bury(4, new RunStats { Kills = 30 }, 20000, random);
            Assert.AreNotEqual(first, company.Mercenary);
            company.Bury(9, new RunStats { Kills = 10 }, 20001, random);
            company.Bury(9, new RunStats { Kills = 50 }, 20002, random);
            Assert.AreEqual(first, company.Fallen[2].Name);
            Assert.AreEqual(50, company.Fallen[0].Kills, "deepest first, then the most kills");
            Assert.AreEqual(9, company.Fallen[1].Depth);
        }

        [Test]
        public void TheCompanyRecord_SurvivesASave_AndADeathButNotItsArcher()
        {
            var session = new GameSession(1);
            session.Company.ContractId = "iron_gaol";
            session.Company.Cleared.Add("thornwood");
            session.Company.Bury(15, new RunStats { Kills = 3 }, 20000, new Random(1));
            var data = SaveCodec.Capture(session, 1);
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data), out var parsed, out var error), error);
            var restored = SaveCodec.Restore(parsed, 2);
            Assert.AreEqual(session.Company.Mercenary, restored.Company.Mercenary);
            Assert.AreEqual("iron_gaol", restored.Company.ContractId);
            CollectionAssert.Contains(restored.Company.Cleared, "thornwood");
            Assert.AreEqual(1, restored.Company.Fallen.Count);
            Assert.AreEqual(15, restored.Company.Fallen[0].Depth);
            var next = restored.NextRun(5, true);
            Assert.AreSame(restored.Company, next.Company, "the company goes on into the next run");
        }

        [Test]
        public void AFormat21Save_Loads_WithANewArcherAndNoFallen()
        {
            var json = "{\"version\":21,\"savedAtUnixMs\":1,\"gold\":10,\"lifeFraction\":1.0,\"level\":1,\"equipped\":[],\"backpack\":[]}";
            Assert.IsTrue(SaveCodec.TryParse(json, out var data, out var error), error);
            Assert.AreEqual(21, data.readFromVersion);
            var session = SaveCodec.Restore(data, 1);
            Assert.IsFalse(string.IsNullOrEmpty(session.Company.Mercenary));
            Assert.AreEqual(0, session.Company.Fallen.Count);
            Assert.AreEqual("thornwood", session.Company.ContractId);
        }
    }
}
