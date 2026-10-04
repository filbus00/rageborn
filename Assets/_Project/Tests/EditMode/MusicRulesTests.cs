using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>Which music plays where (MusicRules).</summary>
    public class MusicRulesTests
    {
        [Test]
        public void TownDungeonAndBoss_EachHaveTheirTrack()
        {
            Assert.AreEqual(MusicTrack.Town, MusicRules.For(SceneTravel.TownScene, false));
            Assert.AreEqual(MusicTrack.Town, MusicRules.For(SceneTravel.TownScene, true), "no boss music in town");
            Assert.AreEqual(MusicTrack.Dungeon, MusicRules.For(SceneTravel.DungeonScene, false));
            Assert.AreEqual(MusicTrack.Boss, MusicRules.For(SceneTravel.DungeonScene, true));
            Assert.AreEqual(MusicTrack.Dungeon, MusicRules.For("Sandbox", false));
        }

        [Test]
        public void Paths_NameTheMusicFiles()
        {
            Assert.AreEqual("Audio/Music/town", MusicRules.ResourcePath(MusicTrack.Town));
            Assert.AreEqual("Audio/Music/boss", MusicRules.ResourcePath(MusicTrack.Boss));
            Assert.Less(MusicRules.FadeSeconds(MusicTrack.Boss), MusicRules.FadeSeconds(MusicTrack.Dungeon), "the fight's music comes in fast");
        }
    }
}
