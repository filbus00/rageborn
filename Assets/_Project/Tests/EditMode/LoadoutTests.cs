using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class LoadoutTests
    {
        // The Wrathborn's class order and unlock levels (Docs/02, Q4).
        static readonly List<(string, int)> Wrathborn = new List<(string, int)>
        {
            ("GroundBreaker", 6), ("HurlAxe", 2), ("BullRush", 4), ("Hew", 1),
            ("BattleRoar", 9), ("RendingSpin", 12), ("BloodFrenzy", 15), ("Skullsplitter", 18),
        };

        [Test]
        public void Unchosen_ItFillsWithTheUnlockedSkills_InClassOrder()
        {
            var loadout = new SkillLoadout();
            loadout.Fill(Wrathborn, 1);
            Assert.AreEqual("Hew", loadout.SkillAt(0), "level 1: Hew alone, in the first slot");
            Assert.IsNull(loadout.SkillAt(1));
            loadout.Fill(Wrathborn, 6);
            CollectionAssert.AreEqual(new[] { "GroundBreaker", "HurlAxe", "BullRush", "Hew" },
                new[] { loadout.SkillAt(0), loadout.SkillAt(1), loadout.SkillAt(2), loadout.SkillAt(3) },
                "level 6: the M1 slice's order, which is cast priority");
            loadout.Fill(Wrathborn, 12);
            Assert.AreEqual(-1, loadout.SlotOf("BattleRoar"), "the four slots are full; later skills wait for the player");
            Assert.IsFalse(loadout.Chosen);
        }

        [Test]
        public void Chosen_ANewSkillOnlyTakesAnEmptySlot()
        {
            var loadout = new SkillLoadout();
            loadout.Fill(Wrathborn, 9);
            loadout.Clear(2);
            Assert.IsTrue(loadout.Chosen);
            loadout.Fill(Wrathborn, 9, 9);
            Assert.IsNull(loadout.SkillAt(2), "a slot the player emptied stays empty");
            loadout.Fill(Wrathborn, 9, 8);
            Assert.AreEqual("BattleRoar", loadout.SkillAt(2), "a skill unlocked by this level up takes the free slot");
            loadout.Equip(0, "Hew");
            Assert.AreEqual("Hew", loadout.SkillAt(0));
            Assert.AreEqual("GroundBreaker", loadout.SkillAt(3), "equipping a carried skill swaps the two slots");
        }

        [Test]
        public void Triggers_MoveWithTheirSkill_AndResetOnANewOne()
        {
            var loadout = new SkillLoadout();
            loadout.Fill(Wrathborn, 6);
            loadout.SetTrigger(3, SkillTrigger.Always);
            loadout.Swap(3, 0);
            Assert.AreEqual("Hew", loadout.SkillAt(0));
            Assert.AreEqual(SkillTrigger.Always, loadout.TriggerAt(0));
            loadout.Equip(0, "BattleRoar");
            Assert.AreEqual(SkillTrigger.Default, loadout.TriggerAt(0));
        }

        [Test]
        public void TheAlternativeTriggers_ReadTheirConditions()
        {
            var context = new TriggerContext { EnemiesInArea = 2, LifeFraction = 0.4f, StillSeconds = 0.5f };
            Assert.IsTrue(SkillTriggerRules.Passes(SkillTrigger.Always, context));
            Assert.IsFalse(SkillTriggerRules.Passes(SkillTrigger.EnemiesThreePlus, context));
            Assert.IsFalse(SkillTriggerRules.Passes(SkillTrigger.ElitePresent, context));
            Assert.IsTrue(SkillTriggerRules.Passes(SkillTrigger.LifeBelowHalf, context));
            Assert.IsFalse(SkillTriggerRules.Passes(SkillTrigger.LifeAboveHalf, context));
            Assert.IsTrue(SkillTriggerRules.Passes(SkillTrigger.Standing, context), "still for 0.4 s or more");
            Assert.IsFalse(SkillTriggerRules.Passes(SkillTrigger.Moving, context));
            Assert.IsFalse(SkillTriggerRules.Passes(SkillTrigger.Default, context), "the skill's own is checked by the combat");
        }

        [Test]
        public void TheLoadout_IsSaved_AndAVersionEightSaveFillsItself()
        {
            var session = new GameSession(1);
            session.Loadout.Fill(Wrathborn, 9);
            session.Loadout.Equip(1, "BattleRoar");
            session.Loadout.SetTrigger(1, SkillTrigger.LifeBelowHalf);
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(SaveCodec.Capture(session, 1)), out var data, out var error), error);
            var loaded = SaveCodec.Restore(data, 1);
            Assert.AreEqual("BattleRoar", loaded.Loadout.SkillAt(1));
            Assert.AreEqual(SkillTrigger.LifeBelowHalf, loaded.Loadout.TriggerAt(1));
            Assert.IsTrue(loaded.Loadout.Chosen);

            const string versionEight =
                "{\"version\":8,\"savedAtUnixMs\":1790193596882,\"gold\":10,\"lifeFraction\":1.0,\"level\":6," +
                "\"equipped\":[],\"backpack\":[],\"corpses\":[],\"killed\":[],\"openedChests\":[],\"materials\":[]," +
                "\"stickTaught\":true,\"forgeIntroduced\":true,\"waypoints\":[3],\"hasPortalTome\":true}";
            Assert.IsTrue(SaveCodec.TryParse(versionEight, out var old, out error), error);
            Assert.AreEqual(SaveData.CurrentVersion, old.version);
            var restored = SaveCodec.Restore(old, 1);
            Assert.IsFalse(restored.Loadout.Chosen);
            restored.Loadout.Fill(Wrathborn, restored.Level);
            Assert.AreEqual("GroundBreaker", restored.Loadout.SkillAt(0), "an old character carries the slice's four as before");
        }
    }
}
