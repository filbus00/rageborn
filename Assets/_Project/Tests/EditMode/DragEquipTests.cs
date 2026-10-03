using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>Equipping into a chosen place, as when gear is dragged onto a slot in the Bag (2026-10-03).</summary>
    public class DragEquipTests
    {
        static Item Ring(int level) => new Item(ItemSlot.Ring, ItemRarity.Magic, level);

        [Test]
        public void ARingDraggedOntoTheSecondHand_IsWornThere()
        {
            var session = new GameSession();
            var ring = Ring(3);
            session.PickUp(ring);

            Assert.IsTrue(session.EquipFromInventory(ring, ItemSlot.Ring2));
            Assert.AreSame(ring, session.Equipment.Get(ItemSlot.Ring2));
            Assert.IsNull(session.Equipment.Get(ItemSlot.Ring));
            Assert.IsFalse(session.Inventory.Contains(ring));
        }

        [Test]
        public void GearDraggedOntoAPlaceItDoesNotFit_ChangesNothing()
        {
            var session = new GameSession();
            var ring = Ring(3);
            session.PickUp(ring);

            Assert.IsFalse(session.EquipFromInventory(ring, ItemSlot.Helm));
            Assert.IsNull(session.Equipment.Get(ItemSlot.Helm));
            Assert.IsTrue(session.Inventory.Contains(ring));
        }

        [Test]
        public void ABowDraggedOntoTheWeaponPlace_SendsTheOldBowToTheBackpack()
        {
            var session = new GameSession();
            var old = session.Equipment.Weapon;
            var bow = new Item(ItemSlot.Weapon, ItemRarity.Rare, 4);
            session.PickUp(bow);

            Assert.IsTrue(session.EquipFromInventory(bow, ItemSlot.Weapon));
            Assert.AreSame(bow, session.Equipment.Weapon);
            Assert.IsTrue(session.Inventory.Contains(old));
        }
    }
}
