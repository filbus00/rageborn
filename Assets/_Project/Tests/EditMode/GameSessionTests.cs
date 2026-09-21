using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class GameSessionTests
    {
        static readonly Vector2 DeathSpot = new Vector2(4f, -3f);

        [Test]
        public void ANewSession_StartsWithTheStartingWeapon_AndNoCorpses()
        {
            var session = new GameSession();

            Assert.AreEqual(1, session.Equipment.WeaponItemLevel);
            Assert.IsFalse(session.Equipment.IsEmpty);
            Assert.IsEmpty(session.Corpses);
            Assert.AreEqual(1f, session.LifeFraction, 1e-6f);
        }

        [Test]
        public void Die_LeavesTheEquippedGearInACorpse_AndUnequipsTheCharacter()
        {
            var session = new GameSession();

            var corpse = session.Die("Sandbox", DeathSpot);

            Assert.IsNotNull(corpse);
            Assert.AreEqual("Sandbox", corpse.LevelId);
            Assert.AreEqual(DeathSpot, corpse.GroundPosition);
            Assert.AreEqual(1, corpse.Gear.WeaponItemLevel);
            Assert.IsTrue(session.Equipment.IsEmpty);
            Assert.AreEqual(1, session.Corpses.Count);
        }

        [Test]
        public void Die_RestoresFullLife()
        {
            var session = new GameSession { LifeFraction = 0f };

            session.Die("Sandbox", DeathSpot);

            Assert.AreEqual(1f, session.LifeFraction, 1e-6f);
        }

        [Test]
        public void ASecondDeath_BeforeReachingTheCorpse_LeavesTheFirstCorpseAndMakesNoNewOne()
        {
            // Docs: the first corpse stays where it is and the second holds nothing, because nothing is equipped.
            var session = new GameSession();
            var first = session.Die("Sandbox", DeathSpot);

            var second = session.Die("Sandbox", new Vector2(-6f, 2f));

            Assert.IsNull(second);
            Assert.AreEqual(1, session.Corpses.Count);
            Assert.AreSame(first, session.Corpses[0]);
            Assert.AreEqual(DeathSpot, session.Corpses[0].GroundPosition);
        }

        [Test]
        public void Retrieve_EquipsTheGear_AndRemovesTheCorpse()
        {
            var session = new GameSession();
            var corpse = session.Die("Sandbox", DeathSpot);

            var retrieved = session.Retrieve(corpse);

            Assert.IsTrue(retrieved);
            Assert.AreEqual(1, session.Equipment.WeaponItemLevel);
            Assert.IsEmpty(session.Corpses);
        }

        [Test]
        public void Retrieve_TheSameCorpseTwice_OnlyWorksOnce()
        {
            var session = new GameSession();
            var corpse = session.Die("Sandbox", DeathSpot);

            Assert.IsTrue(session.Retrieve(corpse));
            Assert.IsFalse(session.Retrieve(corpse));
        }

        [Test]
        public void Retrieve_DoesNotReplaceABetterWeaponTheCharacterAlreadyHas()
        {
            var session = new GameSession();
            var corpse = session.Die("Sandbox", DeathSpot);
            session.Equip(new EquipmentState(5));

            Assert.IsTrue(session.Retrieve(corpse), "the corpse is still picked up");

            Assert.AreEqual(5, session.Equipment.WeaponItemLevel);
            Assert.IsEmpty(session.Corpses);
        }

        [Test]
        public void Retrieve_ReplacesAWorseWeapon()
        {
            var session = new GameSession();
            session.Equip(new EquipmentState(4));
            var corpse = session.Die("Sandbox", DeathSpot);
            session.Equip(new EquipmentState(1));

            session.Retrieve(corpse);

            Assert.AreEqual(4, session.Equipment.WeaponItemLevel);
        }

        [Test]
        public void ACorpseNeverExpires_WhateverHappensToTheSession()
        {
            var session = new GameSession();
            session.Die("Sandbox", DeathSpot);

            session.RecordKill("Sandbox/Pack A", 3);
            session.LifeFraction = 0.4f;

            Assert.AreEqual(1, session.Corpses.Count);
        }

        [Test]
        public void KilledEnemies_AreRememberedPerPackAndSlot()
        {
            var session = new GameSession();

            session.RecordKill("Sandbox/Pack A", 2);
            session.RecordKill("Sandbox/Pack A", 7);
            session.RecordKill("Sandbox/Pack B", 2);

            Assert.IsTrue(session.IsKilled("Sandbox/Pack A", 2));
            Assert.IsTrue(session.IsKilled("Sandbox/Pack A", 7));
            Assert.IsTrue(session.IsKilled("Sandbox/Pack B", 2));
            Assert.IsFalse(session.IsKilled("Sandbox/Pack A", 3), "another slot of the same pack");
            Assert.IsFalse(session.IsKilled("Sandbox/Pack B", 7), "the same slot in another pack");
            Assert.IsFalse(session.IsKilled("Sandbox/Pack C", 2), "an unknown pack");
        }

        [Test]
        public void RecordKill_TwiceForTheSameSlot_IsHarmless()
        {
            var session = new GameSession();

            session.RecordKill("Sandbox/Pack A", 1);
            session.RecordKill("Sandbox/Pack A", 1);

            Assert.IsTrue(session.IsKilled("Sandbox/Pack A", 1));
        }
    }
}
