using NUnit.Framework;

namespace ARPG.Tests
{
    public class SkillLevelTests
    {
        [Test]
        public void Points_AreOneALevelTo30_ThenOneEveryTwo()
        {
            Assert.AreEqual(0, SkillLevels.EarnedPoints(1));
            Assert.AreEqual(1, SkillLevels.EarnedPoints(2));
            Assert.AreEqual(29, SkillLevels.EarnedPoints(30));
            Assert.AreEqual(30, SkillLevels.EarnedPoints(32));
            Assert.AreEqual(44, SkillLevels.EarnedPoints(60), "Docs/02's proposal: 44 by level 60");
        }

        [Test]
        public void ASkill_CannotOutgrowTheCharacter()
        {
            var levels = new SkillLevels();
            // Hew unlocks at 1: at level 3 it may reach 3, and the character has 2 points.
            Assert.IsTrue(levels.Raise("Hew", 1, 3));
            Assert.IsTrue(levels.Raise("Hew", 1, 3));
            Assert.AreEqual(3, levels.LevelOf("Hew"));
            Assert.IsFalse(levels.Raise("Hew", 1, 3), "at its cap and out of points");
            Assert.IsFalse(levels.Raise("BattleRoar", 9, 3), "not unlocked");
            Assert.AreEqual(0, levels.Available(3));
            Assert.AreEqual(1, levels.Available(4));
            Assert.AreEqual(2, SkillLevels.Cap(6, 7), "Ground Breaker at character level 7");
            Assert.AreEqual(SkillLevels.MaxLevel, SkillLevels.Cap(1, 60));
        }

        [Test]
        public void Levels_ScaleDamageAndBuffs()
        {
            Assert.AreEqual(1.7f, SkillLevels.Damage(1.7f, 1), 1e-5f);
            Assert.AreEqual(2.771f, SkillLevels.Damage(1.7f, 10), 1e-3f, "Hew 170 percent at level 1, about 277 at 10");
            Assert.AreEqual(0.39f, SkillLevels.Buff(0.2f, 20), 1e-5f, "Battle Roar 20 to 39 percent");
            Assert.AreEqual(0f, SkillLevels.Buff(0f, 20), "a bonus the skill does not have stays nothing");
        }

        [Test]
        public void Levels_AreSaved_AndAVersionNineSaveStartsAtOne()
        {
            var session = new GameSession(1);
            while (session.Level < 5)
                session.GrantExperience(Experience.XpToNextLevel(session.Level));
            Assert.IsTrue(session.SkillLevels.Raise("Hew", 1, session.Level));
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(SaveCodec.Capture(session, 1)), out var data, out var error), error);
            Assert.AreEqual(2, SaveCodec.Restore(data, 1).SkillLevels.LevelOf("Hew"));

            const string versionNine =
                "{\"version\":9,\"savedAtUnixMs\":1,\"gold\":10,\"lifeFraction\":1.0,\"level\":12," +
                "\"equipped\":[],\"backpack\":[],\"corpses\":[],\"killed\":[],\"openedChests\":[],\"materials\":[]," +
                "\"loadoutSkills\":[\"GroundBreaker\",\"HurlAxe\",\"BullRush\",\"Hew\"],\"loadoutTriggers\":[],\"loadoutChosen\":false}";
            Assert.IsTrue(SaveCodec.TryParse(versionNine, out var old, out error), error);
            Assert.AreEqual(SaveData.CurrentVersion, old.version);
            var restored = SaveCodec.Restore(old, 1);
            Assert.AreEqual(1, restored.SkillLevels.LevelOf("Hew"));
            Assert.AreEqual(11, restored.SkillLevels.Available(12), "every point earned is there to spend");
        }
    }
}
