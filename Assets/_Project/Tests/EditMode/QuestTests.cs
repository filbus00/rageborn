using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class QuestTests
    {
        [Test]
        public void Aldis_OffersTheFirstFire_AndTheNextOnlyAfterIt()
        {
            var quests = new QuestState();
            Assert.AreEqual("first_fire", quests.OfferFrom(QuestGiver.Aldis, 0)?.Id);
            quests.Accept("first_fire");
            Assert.IsNull(quests.OfferFrom(QuestGiver.Aldis, 24), "the second waits for the first");
            Finish(quests, "first_fire");
            Assert.AreEqual("drowned_watch", quests.OfferFrom(QuestGiver.Aldis, 6)?.Id);
            Assert.IsNull(quests.OfferFrom(QuestGiver.Aldis, 5), "not before depth 6");
        }

        [Test]
        public void SideQuests_WaitForTheirGiver_AndTheirDepth()
        {
            var quests = new QuestState();
            Assert.IsNull(quests.OfferFrom(QuestGiver.Healer, 3), "the healer arrives at 4");
            Assert.AreEqual("lamp_in_the_dark", quests.OfferFrom(QuestGiver.Healer, 4)?.Id);
            Assert.IsNull(quests.OfferFrom(QuestGiver.Ivy, 20), "Ivy is below until rescued");
            Assert.AreEqual("apprentice", quests.OfferFrom(QuestGiver.Merchant, 1)?.Id);
        }

        [Test]
        public void Steps_GoInOrder_AndAReturnHandsTheQuestIn()
        {
            var quests = new QuestState();
            quests.Accept("first_fire");
            Assert.AreEqual(1, quests.StepsAt(4).Count);
            Assert.IsFalse(quests.CompleteStep("first_fire", "warden"), "not the current step");
            Assert.IsTrue(quests.CompleteStep("first_fire", "watch_log"));
            Assert.AreEqual(QuestStepKind.Boss, quests.CurrentStep("first_fire").Kind);
            Assert.IsNull(quests.TurnInAt(QuestGiver.Aldis));
            Assert.IsTrue(quests.CompleteStep("first_fire", "warden"));
            Assert.AreEqual("first_fire", quests.TurnInAt(QuestGiver.Aldis)?.Id);
            Assert.IsTrue(quests.TurnIn("first_fire"));
            Assert.IsTrue(quests.IsDone("first_fire"));
            Assert.AreEqual(0.05f, quests.Boons.LifePercent, 1e-5f);
        }

        [Test]
        public void ARescuedPerson_IsInTown_FromTheirRescueOn()
        {
            var quests = new QuestState();
            Assert.IsFalse(quests.IsRescued("tobin"));
            quests.Accept("apprentice");
            quests.CompleteStep("apprentice", "tobin");
            Assert.IsTrue(quests.IsRescued("tobin"), "rescued before the quest is handed in");
            Assert.IsTrue(quests.GiverPresent(QuestGiver.Merchant, 1));
        }

        [Test]
        public void EveryQuest_GivesItsBoon()
        {
            var quests = new QuestState();
            foreach (var quest in QuestRules.All)
                quests.Restore(quest.Id, quest.Steps.Length, true);
            var boons = quests.Boons;
            Assert.AreEqual(0.1f, boons.LifePercent, 1e-5f);
            Assert.AreEqual(0.1f, boons.DamagePercent, 1e-5f);
            Assert.AreEqual(1.25f, boons.SellPrice, 1e-5f);
            Assert.AreEqual(1.25f, boons.PetPower, 1e-5f);
            Assert.AreEqual(1.6f, boons.Light, 1e-5f);
            Assert.IsTrue(boons.MapStairs);
            Assert.AreEqual(2f, boons.LegendaryGamble, 1e-5f);
            Assert.AreEqual(0.05f, boons.AttackSpeed, 1e-5f);
            Assert.AreEqual(1.25f, boons.PotionHeal, 1e-5f);
            Assert.AreEqual(1, boons.PotionCharges);
        }

        [Test]
        public void TheQuests_AreWellFormed()
        {
            var ids = new HashSet<string>();
            Assert.AreEqual(4, QuestRules.All.Count(q => q.Main));
            Assert.AreEqual(8, QuestRules.All.Count(q => !q.Main));
            foreach (var quest in QuestRules.All)
            {
                Assert.IsTrue(ids.Add(quest.Id), quest.Id);
                Assert.Greater(quest.Offer.Length, 0, quest.Id);
                Assert.Greater(quest.Complete.Length, 0, quest.Id);
                Assert.AreEqual(QuestStepKind.Return, quest.Steps.Last().Kind, quest.Id + " ends at its giver");
                Assert.IsTrue(quest.After == null || QuestRules.Get(quest.After) != null, quest.Id);
                foreach (var step in quest.Steps.Where(s => s.Kind != QuestStepKind.Return))
                {
                    Assert.That(step.Depth, Is.InRange(1, DungeonRules.Depths), quest.Id);
                    if (step.Kind == QuestStepKind.Boss)
                        Assert.IsTrue(DungeonRules.IsBossDepth(step.Depth), quest.Id);
                    else
                        Assert.AreNotEqual(step.Target, QuestRules.TargetName(step.Target), quest.Id + ": a name for " + step.Target);
                }
            }
        }

        [Test]
        public void Quests_SurviveASave_AndAVersion15SaveHasNone()
        {
            var session = new GameSession(1);
            session.Quests.Accept("apprentice");
            session.Quests.CompleteStep("apprentice", "tobin");
            session.Quests.Accept("first_fire");
            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);
            Assert.IsTrue(restored.Quests.IsRescued("tobin"));
            Assert.AreEqual(QuestStepKind.Return, restored.Quests.CurrentStep("apprentice").Kind);
            Assert.AreEqual("watch_log", restored.Quests.CurrentStep("first_fire").Target);

            var old = SaveCodec.Capture(session, 0);
            old.version = 15;
            old.quests = null;
            var json = JsonUtility.ToJson(old);
            Assert.IsTrue(SaveCodec.TryParse(json, out var data, out _), "an old save loads");
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            Assert.AreEqual(0, SaveCodec.Restore(data, 1).Quests.All.Count());
        }

        [Test]
        public void EveryDungeonStep_FindsAPlaceOnItsLevel()
        {
            var library = PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();
            foreach (var quest in QuestRules.All)
                foreach (var step in quest.Steps.Where(s => s.Kind != QuestStepKind.Return && s.Kind != QuestStepKind.Boss))
                    for (var seed = 1; seed <= 6; seed++)
                    {
                        var layout = DungeonGenerator.Generate(seed * 7919, step.Depth, library);
                        var cells = QuestSites.Cells(layout, step);
                        var label = $"{quest.Id}, seed {seed}";
                        Assert.AreEqual(QuestSites.Count(step.Target), cells.Count, label);
                        foreach (var cell in cells)
                            Assert.IsTrue(layout.IsFloor(cell), label + $": {cell} is not floor");
                        Assert.AreEqual(cells.Count, cells.Distinct().Count(), label);
                    }
        }

        [Test]
        public void TheQuestPeople_StandClearOfTheTownsBuildings()
        {
            foreach (QuestPerson who in System.Enum.GetValues(typeof(QuestPerson)))
                foreach (var piece in TownLayout.Pieces)
                    Assert.IsFalse(TownLayout.Covers(piece, QuestNpc.Cell(who), 0.5f), $"{who} stands on {piece.Name}");
            foreach (var piece in TownLayout.Pieces)
                Assert.IsFalse(TownLayout.Covers(piece, VigilFire.Cell, 0.8f), $"the Vigil fire stands on {piece.Name}");
        }

        static void Finish(QuestState quests, string id)
        {
            foreach (var step in QuestRules.Get(id).Steps)
                if (step.Kind != QuestStepKind.Return)
                    quests.CompleteStep(id, step.Target);
            quests.TurnIn(id);
        }
    }
}
