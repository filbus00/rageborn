using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ARPG.Tests
{
    public class SaveStoreTests
    {
        string directory;
        SaveStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ARPG-SaveStoreTests-" + System.Guid.NewGuid().ToString("N"));
            store = new SaveStore(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        static string SaveWithGold(int gold) => SaveCodec.ToJson(new SaveData { version = SaveData.CurrentVersion, gold = gold });

        [Test]
        public void WithNoFiles_NothingLoads_AndNothingFailed()
        {
            var result = store.Load();

            Assert.IsFalse(result.Loaded);
            Assert.IsEmpty(result.Failures);
            Assert.IsFalse(store.AnyExists());
        }

        [Test]
        public void AWrittenSave_LoadsFromTheMainFile()
        {
            store.Write(SaveWithGold(42));

            var result = store.Load();

            Assert.IsTrue(result.Loaded);
            Assert.AreEqual(42, result.Data.gold);
            Assert.AreEqual(0, result.BackupNumber);
            Assert.AreEqual(store.MainPath, result.Path);
        }

        [Test]
        public void Writes_KeepTheLastThreeVersions_AsBackups_NewestFirst()
        {
            for (var gold = 1; gold <= 5; gold++)
                store.Write(SaveWithGold(gold));

            Assert.AreEqual(5, Gold(store.MainPath));
            Assert.AreEqual(4, Gold(store.BackupPath(1)));
            Assert.AreEqual(3, Gold(store.BackupPath(2)));
            Assert.AreEqual(2, Gold(store.BackupPath(3)));
            Assert.IsFalse(File.Exists(store.BackupPath(4)));
            Assert.IsFalse(File.Exists(store.MainPath + ".tmp"), "the temporary file is renamed, not left behind");
        }

        [Test]
        public void ACorruptMainFile_FallsBackToTheNewestGoodBackup_AndSaysWhich()
        {
            store.Write(SaveWithGold(1));
            store.Write(SaveWithGold(2));
            store.Write(SaveWithGold(3));
            File.WriteAllText(store.MainPath, "{\"version\":1,\"gold\":3,\"equ");
            File.WriteAllText(store.BackupPath(1), "");

            var result = store.Load();

            Assert.IsTrue(result.Loaded);
            Assert.AreEqual(1, result.Data.gold);
            Assert.AreEqual(2, result.BackupNumber);
            Assert.AreEqual(2, result.Failures.Count);
        }

        [Test]
        public void ALeftoverTemporaryFile_FromACrashMidWrite_DoesNotAffectLoading()
        {
            store.Write(SaveWithGold(7));
            File.WriteAllText(store.MainPath + ".tmp", "{\"vers");

            Assert.AreEqual(7, store.Load().Data.gold);

            store.Write(SaveWithGold(8));
            Assert.AreEqual(8, store.Load().Data.gold);
        }

        [Test]
        public void LoadAndInstall_MakesTheLoadedSessionCurrent()
        {
            var previous = GameSession.Current;
            try
            {
                store.Write(SaveWithGold(99));

                var result = SaveDirector.LoadAndInstall(store, lootSeed: 1);

                Assert.IsTrue(result.Loaded);
                Assert.AreEqual(99, GameSession.Current.Gold);
            }
            finally
            {
                GameSession.Install(previous);
            }
        }

        [Test]
        public void LoadAndInstall_WithNothingReadable_SetsTheFilesAside_AndKeepsTheNewGame()
        {
            var previous = GameSession.Current;
            try
            {
                store.Write(SaveWithGold(1));
                store.Write(SaveWithGold(2));
                File.WriteAllText(store.MainPath, "garbage");
                File.WriteAllText(store.BackupPath(1), "{\"version\":" + (SaveData.CurrentVersion + 1) + "}");
                var fresh = new GameSession();
                GameSession.Install(fresh);
                LogAssert.Expect(LogType.Error, new Regex("No save could be read"));

                var result = SaveDirector.LoadAndInstall(store, lootSeed: 1);

                Assert.IsFalse(result.Loaded);
                Assert.AreSame(fresh, GameSession.Current);
                Assert.IsFalse(store.AnyExists(), "nothing left for a new game to save over");
                Assert.AreEqual(2, Directory.GetFiles(directory, "*.unreadable-*").Length, "both files kept for recovery");
            }
            finally
            {
                GameSession.Install(previous);
            }
        }

        [Test]
        public void LoadAndInstall_AWrathbornSave_IsSetAside_ForACleanStart()
        {
            var previous = GameSession.Current;
            try
            {
                store.Write(SaveCodec.ToJson(new SaveData { version = SaveCodec.FirstBowsVersion - 1, gold = 500, level = 7 }));
                var fresh = new GameSession();
                GameSession.Install(fresh);

                var result = SaveDirector.LoadAndInstall(store, lootSeed: 1);

                Assert.IsFalse(result.Loaded);
                Assert.AreSame(fresh, GameSession.Current, "bows only starts a new game");
                Assert.IsFalse(store.AnyExists());
                Assert.AreEqual(1, Directory.GetFiles(directory, "*.wrathborn-*").Length, "the old save is kept, not deleted");
            }
            finally
            {
                GameSession.Install(previous);
            }
        }

        static int Gold(string path)
        {
            Assert.IsTrue(SaveCodec.TryParse(File.ReadAllText(path), out var data, out var error), error);
            return data.gold;
        }
    }
}
