using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class GameSessionTests
    {
        static readonly Vector2 DeathSpot = new Vector2(4f, -3f);

        static Item Weapon(int itemLevel, ItemRarity rarity = ItemRarity.Common) => new Item(ItemSlot.Weapon, rarity, itemLevel);

        static void FillBackpack(GameSession session)
        {
            while (!session.Inventory.IsFull)
                session.Inventory.TryAdd(Weapon(1));
        }

        [Test]
        public void ANewSession_StartsWithTheStartingWeapon_AndNoCorpses()
        {
            var session = new GameSession();

            Assert.IsFalse(session.Equipment.IsEmpty);
            Assert.AreEqual(1, session.Equipment.Weapon.ItemLevel);
            Assert.AreEqual(ItemRarity.Common, session.Equipment.Weapon.Rarity);
            Assert.IsEmpty(session.Corpses);
            Assert.AreEqual(0, session.Inventory.Count);
            Assert.AreEqual(0, session.Gold);
            Assert.AreEqual(1f, session.LifeFraction, 1e-6f);
        }

        [Test]
        public void EquipmentDamage_UsesTheWeapon_OrTheUnarmedValue()
        {
            var session = new GameSession();

            Assert.AreEqual(CombatFormulas.WeaponAverageDamage(1), session.Equipment.WeaponDamage, 1e-4f);
            Assert.AreEqual(6f, EquipmentState.Empty.WeaponDamage, 1e-4f, "unarmed is the weapon curve at item level 0");
        }

        [Test]
        public void Die_LeavesTheEquippedWeaponInACorpse_AndUnequipsTheCharacter()
        {
            var session = new GameSession();
            var worn = session.Equipment.Weapon;

            var corpse = session.Die("Sandbox", DeathSpot);

            Assert.IsNotNull(corpse);
            Assert.AreEqual("Sandbox", corpse.LevelId);
            Assert.AreEqual(DeathSpot, corpse.GroundPosition);
            Assert.AreSame(worn, corpse.Gear.Weapon);
            Assert.IsTrue(session.Equipment.IsEmpty);
            Assert.AreEqual(1, session.Corpses.Count);
        }

        [Test]
        public void Die_KeepsTheBackpackAndTheGold()
        {
            // Docs: only equipped gear is lost. The backpack, gold and stash are kept.
            var session = new GameSession();
            session.PickUp(Weapon(1, ItemRarity.Rare));
            session.AddGold(50);

            session.Die("Sandbox", DeathSpot);

            Assert.AreEqual(1, session.Inventory.Count);
            Assert.AreEqual(50, session.Gold);
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
            var session = new GameSession();
            var first = session.Die("Sandbox", DeathSpot);

            var second = session.Die("Sandbox", new Vector2(-6f, 2f));

            Assert.IsNull(second);
            Assert.AreEqual(1, session.Corpses.Count);
            Assert.AreSame(first, session.Corpses[0]);
            Assert.AreEqual(DeathSpot, session.Corpses[0].GroundPosition);
        }

        [Test]
        public void ADeathAfterEquippingAFoundWeapon_MakesASecondCorpseWithThatWeapon()
        {
            var session = new GameSession();
            session.Die("Sandbox", DeathSpot);
            session.PickUp(Weapon(3));
            var found = session.Equipment.Weapon;

            var second = session.Die("Sandbox", new Vector2(-6f, 2f));

            Assert.IsNotNull(second);
            Assert.AreSame(found, second.Gear.Weapon);
            Assert.AreEqual(2, session.Corpses.Count);
        }

        [Test]
        public void Retrieve_EquipsTheWeapon_WhenUnarmed_AndRemovesTheCorpse()
        {
            var session = new GameSession();
            var worn = session.Equipment.Weapon;
            var corpse = session.Die("Sandbox", DeathSpot);

            var retrieved = session.Retrieve(corpse);

            Assert.IsTrue(retrieved);
            Assert.AreSame(worn, session.Equipment.Weapon);
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
        public void Retrieve_PutsAWorseWeaponInTheBackpack_WhenAlreadyWearingABetterOne()
        {
            var session = new GameSession();
            var old = session.Equipment.Weapon;
            var corpse = session.Die("Sandbox", DeathSpot);
            session.PickUp(Weapon(5));

            Assert.IsTrue(session.Retrieve(corpse));

            Assert.AreEqual(5, session.Equipment.Weapon.ItemLevel, "the better weapon stays equipped");
            Assert.IsTrue(session.Inventory.Contains(old), "the retrieved weapon is kept, not destroyed");
            Assert.IsEmpty(session.Corpses);
        }

        [Test]
        public void Retrieve_EquipsABetterWeapon_AndKeepsTheOneWorn()
        {
            var session = new GameSession();
            session.Equip(new EquipmentState(Weapon(6)));
            var corpse = session.Die("Sandbox", DeathSpot);
            var worn = Weapon(2);
            session.Equip(new EquipmentState(worn));

            Assert.IsTrue(session.Retrieve(corpse));

            Assert.AreEqual(6, session.Equipment.Weapon.ItemLevel);
            Assert.IsTrue(session.Inventory.Contains(worn));
        }

        [Test]
        public void Retrieve_ChangesNothing_WhenTheBackpackHasNoRoomForWhatMustMove()
        {
            var session = new GameSession();
            var corpse = session.Die("Sandbox", DeathSpot);
            session.PickUp(Weapon(5));
            FillBackpack(session);

            var retrieved = session.Retrieve(corpse);

            Assert.IsFalse(retrieved);
            Assert.AreEqual(1, session.Corpses.Count, "the corpse stays where it is");
            Assert.AreEqual(5, session.Equipment.Weapon.ItemLevel);
            Assert.AreEqual(Inventory.DefaultCapacity, session.Inventory.Count);
        }

        [Test]
        public void ACorpseNeverExpires_WhateverHappensToTheSession()
        {
            var session = new GameSession();
            session.Die("Sandbox", DeathSpot);

            session.RecordKill("Sandbox/Pack A", 3);
            session.AddGold(10);
            session.LifeFraction = 0.4f;

            Assert.AreEqual(1, session.Corpses.Count);
        }

        [Test]
        public void PickUp_PutsTheItemInTheBackpack_AndRaisesChanged()
        {
            var session = new GameSession();
            var raised = 0;
            session.Changed += () => raised++;
            var item = Weapon(1, ItemRarity.Magic);

            var ok = session.PickUp(item);

            Assert.IsTrue(ok);
            Assert.Greater(raised, 0);
        }

        [Test]
        public void PickUp_EquipsAWeaponThatBeatsTheWornOne_AndKeepsTheOldOne()
        {
            var session = new GameSession();
            var old = session.Equipment.Weapon;
            var better = Weapon(4, ItemRarity.Rare);

            session.PickUp(better);

            Assert.AreSame(better, session.Equipment.Weapon);
            Assert.IsTrue(session.Inventory.Contains(old));
            Assert.IsFalse(session.Inventory.Contains(better), "it left the backpack to be worn");
            Assert.AreEqual(1, session.Inventory.Count);
        }

        [Test]
        public void PickUp_KeepsTheWornWeapon_WhenTheFoundOneIsNoBetter()
        {
            var session = new GameSession();
            var worn = session.Equipment.Weapon;
            var same = Weapon(1, ItemRarity.Magic);

            session.PickUp(same);

            Assert.AreSame(worn, session.Equipment.Weapon, "with no affixes yet, the same item level means the same damage");
            Assert.IsTrue(session.Inventory.Contains(same));
        }

        [Test]
        public void PickUp_EquipsAnyWeaponWhenUnarmed()
        {
            var session = new GameSession();
            session.Die("Sandbox", DeathSpot);
            var found = Weapon(1);

            session.PickUp(found);

            Assert.AreSame(found, session.Equipment.Weapon);
            Assert.AreEqual(0, session.Inventory.Count);
        }

        [Test]
        public void PickUp_Fails_WhenTheBackpackIsFull()
        {
            var session = new GameSession();
            FillBackpack(session);

            Assert.IsFalse(session.PickUp(Weapon(9)));
            Assert.AreEqual(Inventory.DefaultCapacity, session.Inventory.Count);
        }

        [Test]
        public void AddGold_AddsUp_AndIgnoresNonPositiveAmounts()
        {
            var session = new GameSession();

            session.AddGold(12);
            session.AddGold(0);
            session.AddGold(-5);

            Assert.AreEqual(12, session.Gold);
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
