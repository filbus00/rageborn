using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class SaveCodecTests
    {
        static Item RareWeapon() => new Item(ItemSlot.Weapon, ItemRarity.Rare, 12, new[]
        {
            new AffixRoll(AffixId.FlatWeaponDamage, 5, 17.25f),
            new AffixRoll(AffixId.CriticalChance, 4, 1.5f),
        });

        static GameSession RoundTrip(GameSession session, List<string> warnings = null)
        {
            var json = SaveCodec.ToJson(SaveCodec.Capture(session, 1234));
            Assert.IsTrue(SaveCodec.TryParse(json, out var data, out var error), error);
            return SaveCodec.Restore(data, lootSeed: 7, warnings);
        }

        static void AssertSameItem(Item expected, Item actual)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Slot, actual.Slot);
            Assert.AreEqual(expected.Rarity, actual.Rarity);
            Assert.AreEqual(expected.ItemLevel, actual.ItemLevel);
            Assert.AreEqual(expected.Affixes.Count, actual.Affixes.Count);
            for (var i = 0; i < expected.Affixes.Count; i++)
            {
                Assert.AreEqual(expected.Affixes[i].Id, actual.Affixes[i].Id);
                Assert.AreEqual(expected.Affixes[i].Tier, actual.Affixes[i].Tier);
                Assert.AreEqual(expected.Affixes[i].Value, actual.Affixes[i].Value, 1e-5f);
            }
        }

        [Test]
        public void ARoundTrip_KeepsEverythingASaveMustHold()
        {
            var session = new GameSession(1, killsSinceLegendary: 123);
            var weapon = RareWeapon();
            var helm = new Item(ItemSlot.Helm, ItemRarity.Magic, 8, new[] { new AffixRoll(AffixId.CooldownReduction, 5, 2f) });
            var bagged = new Item(ItemSlot.Chest, ItemRarity.Common, 3);
            session.Equip(EquipmentState.Empty.With(ItemSlot.Weapon, weapon).With(ItemSlot.Helm, helm));
            session.PickUp(bagged);
            session.AddGold(345);
            session.LifeFraction = 0.4f;
            session.RecordKill("Sandbox/Pack West", 2);
            session.RecordKill("Sandbox/Pack West", 5);
            session.RecordKill("Sandbox/Pack East", 0);

            // A corpse holding the gear, then new gear on the character.
            session.Die("Sandbox", new Vector2(4.5f, -3.25f));
            session.Equip(new EquipmentState(new Item(ItemSlot.Weapon, ItemRarity.Common, 1)));
            session.LifeFraction = 0.4f;

            var loaded = RoundTrip(session);

            Assert.AreEqual(345, loaded.Gold);
            Assert.AreEqual(0.4f, loaded.LifeFraction, 1e-5f);
            Assert.AreEqual(123, loaded.Loot.KillsSinceLegendary);
            AssertSameItem(session.Equipment.Weapon, loaded.Equipment.Weapon);
            Assert.IsNull(loaded.Equipment.Helm);
            Assert.AreEqual(1, loaded.Inventory.Count);
            AssertSameItem(bagged, loaded.Inventory.Items[0]);

            Assert.AreEqual(1, loaded.Corpses.Count);
            var corpse = loaded.Corpses[0];
            Assert.AreEqual("Sandbox", corpse.LevelId);
            Assert.AreEqual(new Vector2(4.5f, -3.25f), corpse.GroundPosition);
            AssertSameItem(weapon, corpse.Gear.Weapon);
            AssertSameItem(helm, corpse.Gear.Helm);

            Assert.IsTrue(loaded.IsKilled("Sandbox/Pack West", 2));
            Assert.IsTrue(loaded.IsKilled("Sandbox/Pack West", 5));
            Assert.IsTrue(loaded.IsKilled("Sandbox/Pack East", 0));
            Assert.IsFalse(loaded.IsKilled("Sandbox/Pack West", 3));
        }

        [Test]
        public void ARoundTrip_KeepsLevelAndXp()
        {
            var session = new GameSession();
            session.GrantExperience(Experience.XpToNextLevel(1) + Experience.XpToNextLevel(2) + 123);

            var loaded = RoundTrip(session);

            Assert.AreEqual(3, loaded.Level);
            Assert.AreEqual(123, loaded.Progress.Xp);
        }

        [Test]
        public void ARoundTrip_KeepsPotionCharges()
        {
            var session = new GameSession();
            // Fire two potions (the second once the first heal has run out), then 7 kills toward the next charge.
            session.Potion.Tick(AutoPotion.HealSeconds, 0.1f, true, out _);
            session.Potion.Tick(AutoPotion.HealSeconds, 0.1f, true, out _);
            for (var i = 0; i < 7; i++)
                session.Potion.RegisterKill();

            var loaded = RoundTrip(session);

            Assert.AreEqual(1, loaded.Potion.Charges);
            Assert.AreEqual(7, loaded.Potion.KillProgress);
        }

        [Test]
        public void ARoundTrip_KeepsTheDungeonSeed_AndOpenedChests()
        {
            var session = new GameSession(5);
            session.RecordOpened("Dungeon 2/Chest 0");
            session.RecordOpened("Dungeon 4/Chest 1");

            var loaded = RoundTrip(session);

            Assert.AreEqual(session.DungeonSeed, loaded.DungeonSeed);
            Assert.IsTrue(loaded.IsOpened("Dungeon 2/Chest 0"));
            Assert.IsTrue(loaded.IsOpened("Dungeon 4/Chest 1"));
            Assert.IsFalse(loaded.IsOpened("Dungeon 1/Chest 0"));
        }

        [Test]
        public void AVersionThreeSave_LoadsWithADungeonSeed_AndNoOpenedChests()
        {
            const string versionThree =
                "{\"version\":3,\"savedAtUnixMs\":1790193596882,\"gold\":60,\"lifeFraction\":1.0,\"killsSinceLegendary\":0," +
                "\"level\":3,\"experience\":10,\"potionCharges\":2,\"potionKillProgress\":4," +
                "\"equipped\":[],\"backpack\":[],\"corpses\":[],\"killed\":[]}";

            Assert.IsTrue(SaveCodec.TryParse(versionThree, out var data, out var error), error);
            var session = SaveCodec.Restore(data, 1);

            Assert.AreNotEqual(0, session.DungeonSeed);
            Assert.IsEmpty(session.OpenedChests);
            Assert.AreEqual(3, session.Level);
            Assert.AreEqual(2, session.Potion.Charges);
        }

        [Test]
        public void OpeningAChest_IsSaved_OnceEach()
        {
            var session = new GameSession();
            var modified = 0;
            session.Modified += () => modified++;

            session.RecordOpened("Dungeon 1/Chest 0");
            session.RecordOpened("Dungeon 1/Chest 0");

            Assert.AreEqual(1, modified);
            Assert.IsTrue(session.IsOpened("Dungeon 1/Chest 0"));
        }

        [Test]
        public void AVersionTwoSave_LoadsWithFullPotions_AndItsLevelKept()
        {
            // Written by save format 2, before potions existed.
            const string versionTwo =
                "{\"version\":2,\"savedAtUnixMs\":1,\"gold\":101,\"lifeFraction\":0.6,\"killsSinceLegendary\":25," +
                "\"level\":2,\"experience\":24,\"equipped\":[],\"backpack\":[],\"corpses\":[],\"killed\":[]}";

            Assert.IsTrue(SaveCodec.TryParse(versionTwo, out var data, out var error), error);
            var session = SaveCodec.Restore(data, 1);

            Assert.AreEqual(AutoPotion.MaxCharges, session.Potion.Charges);
            Assert.AreEqual(0, session.Potion.KillProgress);
            Assert.AreEqual(2, session.Level);
            Assert.AreEqual(24, session.Progress.Xp);
            Assert.AreEqual(101, session.Gold);
        }

        [Test]
        public void AVersionOneSave_LoadsAsLevelOne_WithEverythingElseKept()
        {
            // Written by the first save format, before level and XP existed.
            const string versionOne =
                "{\"version\":1,\"savedAtUnixMs\":1790193348575,\"gold\":17,\"lifeFraction\":0.5,\"killsSinceLegendary\":17," +
                "\"equipped\":[],\"backpack\":[{\"slot\":\"Helm\",\"rarity\":\"Magic\",\"itemLevel\":2,\"affixes\":[]}]," +
                "\"corpses\":[{\"levelId\":\"Sandbox\",\"x\":3.5,\"y\":-3.0,\"gear\":[{\"slot\":\"Weapon\",\"rarity\":\"Common\",\"itemLevel\":1,\"affixes\":[]}]}]," +
                "\"killed\":[{\"packKey\":\"Sandbox/Pack East (near)\",\"slots\":[0,1,2]}]}";

            Assert.IsTrue(SaveCodec.TryParse(versionOne, out var data, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            var session = SaveCodec.Restore(data, 1);

            Assert.AreEqual(1, session.Level);
            Assert.AreEqual(0, session.Progress.Xp);
            Assert.AreEqual(17, session.Gold);
            Assert.AreEqual(0.5f, session.LifeFraction, 1e-5f);
            Assert.AreEqual(ItemSlot.Helm, session.Inventory.Items[0].Slot);
            Assert.AreEqual(1, session.Corpses.Count);
            Assert.IsTrue(session.IsKilled("Sandbox/Pack East (near)", 2));
        }

        [Test]
        public void ARoundTrip_OfACharacterWithNothingEquipped_StaysUnequipped()
        {
            var session = new GameSession();
            session.Equip(EquipmentState.Empty);

            Assert.IsTrue(RoundTrip(session).Equipment.IsEmpty);
        }

        [Test]
        public void Enums_AreSavedByName_NotByNumber()
        {
            var session = new GameSession();
            session.Equip(new EquipmentState(RareWeapon()));

            var json = SaveCodec.ToJson(SaveCodec.Capture(session, 0));

            StringAssert.Contains("\"Weapon\"", json);
            StringAssert.Contains("\"Rare\"", json);
            StringAssert.Contains("\"FlatWeaponDamage\"", json);
        }

        [Test]
        public void UnknownNames_AreLeftOut_WithAWarning_AndTheRestLoads()
        {
            var data = new SaveData { version = SaveData.CurrentVersion, gold = 10 };
            data.backpack.Add(new ItemData { slot = "Boots", rarity = "Magic", itemLevel = 5 });
            data.backpack.Add(new ItemData { slot = "7", rarity = "Magic", itemLevel = 5 });
            var chest = new ItemData { slot = "Chest", rarity = "Magic", itemLevel = 5 };
            chest.affixes.Add(new AffixData { id = "Thorns", tier = 5, value = 3f });
            chest.affixes.Add(new AffixData { id = "Life", tier = 5, value = 40f });
            data.backpack.Add(chest);
            var warnings = new List<string>();

            var session = SaveCodec.Restore(data, 1, warnings);

            Assert.AreEqual(10, session.Gold);
            Assert.AreEqual(1, session.Inventory.Count);
            Assert.AreEqual(ItemSlot.Chest, session.Inventory.Items[0].Slot);
            Assert.AreEqual(40f, session.Inventory.Items[0].LifeBonus);
            Assert.AreEqual(3, warnings.Count);
        }

        [Test]
        public void ASavedLifeOfZero_LoadsAsFullLife()
        {
            var data = new SaveData { version = SaveData.CurrentVersion, lifeFraction = 0f };

            Assert.AreEqual(1f, SaveCodec.Restore(data, 1).LifeFraction);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not json at all")]
        [TestCase("{\"version\":1,\"gold\":5,\"equipped\":[{\"slot\":\"Wea")]
        [TestCase("{\"gold\":5}")]
        [TestCase("{\"version\":0}")]
        public void TryParse_RejectsWhatIsNotASave(string json)
        {
            Assert.IsFalse(SaveCodec.TryParse(json, out var data, out var error));
            Assert.IsNull(data);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void TryParse_RejectsASaveFromANewerBuild()
        {
            var json = "{\"version\":" + (SaveData.CurrentVersion + 1) + ",\"gold\":5}";

            Assert.IsFalse(SaveCodec.TryParse(json, out _, out var error));
            StringAssert.Contains("newer", error);
        }

        [Test]
        public void TryParse_FillsInMissingLists()
        {
            Assert.IsTrue(SaveCodec.TryParse("{\"version\":1,\"gold\":5}", out var data, out _));

            Assert.AreEqual(5, data.gold);
            Assert.IsNotNull(data.equipped);
            Assert.IsNotNull(data.backpack);
            Assert.IsNotNull(data.corpses);
            Assert.IsNotNull(data.killed);
        }

        [Test]
        public void Modified_IsRaised_ForEverythingASaveCaptures()
        {
            var session = new GameSession();
            var count = 0;
            session.Modified += () => count++;

            session.AddGold(5);
            Assert.AreEqual(1, count, "gold");
            session.PickUp(new Item(ItemSlot.Chest, ItemRarity.Common, 1));
            Assert.AreEqual(2, count, "pickup");
            session.RecordKill("pack", 0);
            Assert.AreEqual(3, count, "kill");
            session.RecordKill("pack", 0);
            Assert.AreEqual(3, count, "the same kill twice is not a change");
            session.Equip(EquipmentState.Empty);
            session.Die("Sandbox", Vector2.zero);
            Assert.AreEqual(5, count, "a death with nothing equipped still changes life and must be saved");
        }

        [Test]
        public void Kills_DoNotRaiseChanged_WhichTheHudListensTo()
        {
            var session = new GameSession();
            var changed = 0;
            session.Changed += () => changed++;

            session.RecordKill("pack", 0);

            Assert.AreEqual(0, changed);
        }
    }
}
