using NUnit.Framework;

namespace ARPG.Tests
{
    public class OnboardingTests
    {
        static Item Of(ItemRarity rarity) => new Item(ItemSlot.Helm, rarity, 3);

        [Test]
        public void ANewCharacter_HasBeenTaughtNothing()
        {
            var onboarding = new GameSession().Onboarding;
            Assert.IsFalse(onboarding.StickTaught);
            Assert.IsFalse(onboarding.ForgeIntroduced);
            Assert.IsFalse(onboarding.SeenLegendary);
            Assert.AreEqual(0f, onboarding.PlaySeconds);
        }

        [Test]
        public void TheForge_IsIntroducedOnTheFirstRare_Once()
        {
            var onboarding = new Onboarding();
            Assert.AreEqual(OnboardingHint.None, onboarding.OnItemFound(Of(ItemRarity.Common)));
            Assert.AreEqual(OnboardingHint.None, onboarding.OnItemFound(Of(ItemRarity.Magic)));
            Assert.AreEqual(OnboardingHint.Forge, onboarding.OnItemFound(Of(ItemRarity.Rare)));
            Assert.AreEqual(OnboardingHint.None, onboarding.OnItemFound(Of(ItemRarity.Rare)));
        }

        [Test]
        public void TheFirstLegendary_GetsItsHint_AndTheForgeWaitsForTheNextRare()
        {
            var onboarding = new Onboarding();
            Assert.AreEqual(OnboardingHint.Legendary, onboarding.OnItemFound(Of(ItemRarity.Legendary)));
            Assert.IsTrue(onboarding.SeenLegendary);
            Assert.AreEqual(OnboardingHint.Forge, onboarding.OnItemFound(Of(ItemRarity.Rare)));
            Assert.AreEqual(OnboardingHint.None, onboarding.OnItemFound(Of(ItemRarity.Legendary)));
        }

        [Test]
        public void TheGuaranteedLegendary_IsDueAtMinuteTwenty_Once()
        {
            var onboarding = new Onboarding();
            onboarding.Tick(19f * 60f);
            Assert.IsFalse(onboarding.LegendaryDue);
            onboarding.Tick(60f);
            Assert.IsTrue(onboarding.LegendaryDue);

            onboarding.MarkGuaranteeDropped();
            Assert.IsFalse(onboarding.LegendaryDue, "a pack of elites does not all drop one");
        }

        [Test]
        public void ALegendaryFoundEarlier_MakesTheGuaranteeMoot()
        {
            var onboarding = new Onboarding();
            onboarding.OnItemFound(Of(ItemRarity.Legendary));
            onboarding.Tick(30f * 60f);
            Assert.IsFalse(onboarding.LegendaryDue);
        }

        [Test]
        public void PickingUp_RaisesPickedUp_AndLessonsAreSaved()
        {
            var session = new GameSession();
            Item seen = null;
            session.PickedUp += item => seen = item;
            var modified = 0;
            session.Modified += () => modified++;

            var rare = Of(ItemRarity.Rare);
            session.PickUp(rare);
            Assert.AreSame(rare, seen);

            session.Onboarding.MarkStickTaught();
            Assert.AreEqual(2, modified, "the pickup and the lesson");
        }

        [Test]
        public void ARoundTrip_KeepsWhatWasTaught_AndPlayTime()
        {
            var session = new GameSession(4);
            session.Onboarding.MarkStickTaught();
            session.Onboarding.OnItemFound(Of(ItemRarity.Rare));
            session.Onboarding.Tick(600f);

            var json = SaveCodec.ToJson(SaveCodec.Capture(session, 1));
            Assert.IsTrue(SaveCodec.TryParse(json, out var data, out var error), error);
            var loaded = SaveCodec.Restore(data, 1).Onboarding;

            Assert.IsTrue(loaded.StickTaught);
            Assert.IsTrue(loaded.ForgeIntroduced);
            Assert.IsFalse(loaded.SeenLegendary);
            Assert.AreEqual(600f, loaded.PlaySeconds, 0.01f);
        }

        [Test]
        public void AVersionSixSave_SkipsTheLessons_AndKnowsItsLegendaries()
        {
            const string versionSix =
                "{\"version\":6,\"savedAtUnixMs\":1790193596882,\"gold\":60,\"lifeFraction\":1.0,\"killsSinceLegendary\":0," +
                "\"level\":4,\"experience\":10,\"potionCharges\":3,\"potionKillProgress\":0,\"dungeonSeed\":42,\"dungeonVersion\":3," +
                "\"equipped\":[],\"backpack\":[{\"slot\":\"Helm\",\"rarity\":\"Legendary\",\"itemLevel\":5,\"affixes\":[]}]," +
                "\"corpses\":[],\"killed\":[],\"openedChests\":[],\"materials\":[]}";

            Assert.IsTrue(SaveCodec.TryParse(versionSix, out var data, out var error), error);
            var onboarding = SaveCodec.Restore(data, 1).Onboarding;

            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            Assert.IsTrue(onboarding.StickTaught);
            Assert.IsTrue(onboarding.ForgeIntroduced);
            Assert.IsTrue(onboarding.SeenLegendary);
            Assert.IsFalse(onboarding.LegendaryDue);
        }

        [Test]
        public void AVersionSixSave_WithoutALegendary_StillGetsTheGuarantee()
        {
            const string versionSix =
                "{\"version\":6,\"savedAtUnixMs\":1790193596882,\"gold\":0,\"lifeFraction\":1.0,\"level\":2," +
                "\"equipped\":[],\"backpack\":[],\"corpses\":[],\"killed\":[],\"openedChests\":[],\"materials\":[]}";

            Assert.IsTrue(SaveCodec.TryParse(versionSix, out var data, out var error), error);
            var onboarding = SaveCodec.Restore(data, 1).Onboarding;
            Assert.IsFalse(onboarding.SeenLegendary);
            onboarding.Tick(Onboarding.GuaranteedLegendarySeconds);
            Assert.IsTrue(onboarding.LegendaryDue);
        }
    }
}
