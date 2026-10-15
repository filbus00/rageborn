using System.Linq;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The pets' skills (2026-10-15): four a pet, taught by Lyriel for coins, kept for good, working while the
    /// pet follows her, each pairing with a boon.</summary>
    public class PetSkillTests
    {
        [Test]
        public void EveryPet_HasFourSkills_EachPairingWithARealBoonOrNone()
        {
            foreach (var pet in PetRules.All)
                Assert.AreEqual(4, PetSkillRules.For(pet).Count(), pet.ToString());
            Assert.AreEqual(PetSkillRules.All.Count, PetSkillRules.All.Select(s => s.Id).Distinct().Count());
            foreach (var skill in PetSkillRules.All)
            {
                Assert.IsTrue(skill.Pairs == null || TalentRules.Get(skill.Pairs) != null, skill.Name);
                for (var rank = 0; rank <= PetSkillRules.MaxRanks; rank++)
                    Assert.IsFalse(PetSkillRules.Describe(skill, rank).Contains("{0}"), skill.Name);
            }
        }

        [Test]
        public void Lyriel_TeachesSkills_ForTheirPrice_OnlyForAnOwnedPet_UpToTheLastRank()
        {
            var session = new GameSession(1);
            session.AddGold(100000);
            Assert.IsFalse(session.TeachPetSkill(PetSkillId.Rend), "no wolf yet");
            Assert.IsTrue(session.BuyPet(PetKind.Wolf));
            var gold = session.Gold;
            Assert.IsTrue(session.TeachPetSkill(PetSkillId.Rend));
            Assert.AreEqual(gold - PetSkillRules.Cost(0), session.Gold);
            Assert.IsTrue(session.TeachPetSkill(PetSkillId.Rend));
            Assert.IsTrue(session.TeachPetSkill(PetSkillId.Rend));
            Assert.IsFalse(session.TeachPetSkill(PetSkillId.Rend), "mastered");
            Assert.AreEqual(3, session.Pets.Rank(PetSkillId.Rend));
            Assert.AreEqual(600 + 2400 + 5400, gold - session.Gold);
        }

        [Test]
        public void OnlyTheFollowingPetsSkills_Work()
        {
            var session = new GameSession(1);
            session.AddGold(100000);
            session.BuyPet(PetKind.Wolf);
            session.TeachPetSkill(PetSkillId.Rend);
            session.BuyPet(PetKind.Raven);
            session.TeachPetSkill(PetSkillId.HuntersMark);
            Assert.AreEqual(0, session.Pets.ActiveRank(PetSkillId.Rend), "the raven follows");
            Assert.AreEqual(1, session.Pets.ActiveRank(PetSkillId.HuntersMark));
            session.Pets.SetActive(PetKind.Wolf);
            Assert.AreEqual(1, session.Pets.ActiveRank(PetSkillId.Rend));
        }

        [Test]
        public void TheSkills_LastThroughDeathAndTheSave()
        {
            var session = new GameSession(1);
            session.AddGold(100000);
            session.BuyPet(PetKind.Boar);
            session.TeachPetSkill(PetSkillId.Trample);
            session.TeachPetSkill(PetSkillId.Trample);
            var next = session.NextRun(2, false);
            Assert.AreEqual(2, next.Pets.Rank(PetSkillId.Trample));
            var loaded = SaveCodec.Restore(SaveCodec.Capture(next, 0), 3);
            Assert.AreEqual(2, loaded.Pets.Rank(PetSkillId.Trample));
            Assert.AreEqual(2, loaded.Pets.ActiveRank(PetSkillId.Trample));
        }
    }
}
