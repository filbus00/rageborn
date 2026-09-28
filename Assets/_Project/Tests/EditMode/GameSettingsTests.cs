using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class GameSettingsTests
    {
        [Test]
        public void Defaults_MatchTheDocsAndTheBuiltLevels()
        {
            var s = new GameSettings();

            Assert.AreEqual(64f, s.stickSize);
            Assert.AreEqual(0.08f, s.DeadZoneFraction, 1e-6f);
            Assert.AreEqual(AutoPotion.DefaultTriggerFraction, s.PotionTriggerFraction, 1e-6f);
            Assert.IsFalse(s.leftHanded);
            Assert.IsFalse(s.reduceFlashing);
            Assert.IsFalse(s.reduceMotion);
            // The default music setting plays the music at the level it was mixed at before the setting existed.
            Assert.AreEqual(0.3f, MusicDirector.FullVolume * s.MusicLevel, 1e-6f);
            Assert.AreEqual(1f, s.EffectsLevel);
        }

        [Test]
        public void Clamp_KeepsEveryValueInItsRange_OnItsStep()
        {
            var s = new GameSettings
            {
                stickSize = 200f, deadZone = -3f, potionThreshold = 61f, musicVolume = 37f, effectsVolume = float.NaN,
            };

            s.Clamp();

            Assert.AreEqual(96f, s.stickSize);
            Assert.AreEqual(4f, s.deadZone);
            Assert.AreEqual(60f, s.potionThreshold);
            Assert.AreEqual(40f, s.musicVolume);
            Assert.AreEqual(0f, s.effectsVolume);
        }

        [Test]
        public void Step_MovesOneStep_AndStopsAtTheEnds()
        {
            Assert.AreEqual(72f, GameSettings.Step(64f, 48f, 96f, 8f, 1));
            Assert.AreEqual(56f, GameSettings.Step(64f, 48f, 96f, 8f, -1));
            Assert.AreEqual(96f, GameSettings.Step(96f, 48f, 96f, 8f, 1));
            Assert.AreEqual(20f, GameSettings.Step(20f, 20f, 60f, 5f, -1));
        }

        [Test]
        public void Json_RoundTrips()
        {
            var s = new GameSettings { stickSize = 80f, leftHanded = true, potionThreshold = 50f, musicVolume = 30f, reduceMotion = true };

            Assert.IsTrue(GameSettings.TryParse(GameSettings.ToJson(s), out var back, out var error), error);

            Assert.AreEqual(80f, back.stickSize);
            Assert.IsTrue(back.leftHanded);
            Assert.AreEqual(50f, back.potionThreshold);
            Assert.AreEqual(30f, back.musicVolume);
            Assert.IsTrue(back.reduceMotion);
            Assert.IsFalse(back.reduceFlashing);
        }

        [Test]
        public void MissingFields_KeepTheirDefaults()
        {
            Assert.IsTrue(GameSettings.TryParse("{\"version\":1,\"leftHanded\":true}", out var s, out var error), error);

            Assert.IsTrue(s.leftHanded);
            Assert.AreEqual(64f, s.stickSize);
            Assert.AreEqual(100f, s.effectsVolume);
        }

        [Test]
        public void ANewerFile_OrNonsense_IsRefused()
        {
            Assert.IsFalse(GameSettings.TryParse("{\"version\":99}", out _, out _));
            Assert.IsFalse(GameSettings.TryParse("not json", out _, out _));
            Assert.IsFalse(GameSettings.TryParse("", out _, out _));
            Assert.IsFalse(GameSettings.TryParse("{\"version\":0}", out _, out _));
        }

        [Test]
        public void TheAccountFile_FallsBackToABackup_WhenTheMainFileIsBroken()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ARPG-SettingsTests-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveStore(directory, SettingsDirector.FileName);
                store.Write(GameSettings.ToJson(new GameSettings { stickSize = 88f }));
                store.Write(GameSettings.ToJson(new GameSettings { stickSize = 56f }));
                File.WriteAllText(store.MainPath, "{ broken");

                var failures = new List<string>();
                Assert.IsTrue(store.TryLoad<GameSettings>(GameSettings.TryParse, out var loaded, out _, out var backup, failures));

                Assert.AreEqual(88f, loaded.stickSize);
                Assert.AreEqual(1, backup);
                Assert.AreEqual(1, failures.Count);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        }
    }
}
