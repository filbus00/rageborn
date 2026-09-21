using NUnit.Framework;

namespace ARPG.Tests
{
    public class InventoryTests
    {
        static Item AnItem() => new Item(ItemSlot.Weapon, ItemRarity.Common, 1);

        [Test]
        public void TheDefaultCapacity_IsFortySlots()
        {
            Assert.AreEqual(40, new Inventory().Capacity);
        }

        [Test]
        public void TryAdd_AddsUntilFull_ThenRefuses()
        {
            var inventory = new Inventory(3);

            Assert.IsTrue(inventory.TryAdd(AnItem()));
            Assert.IsTrue(inventory.TryAdd(AnItem()));
            Assert.IsFalse(inventory.IsFull);
            Assert.IsTrue(inventory.TryAdd(AnItem()));
            Assert.IsTrue(inventory.IsFull);

            Assert.IsFalse(inventory.TryAdd(AnItem()));
            Assert.AreEqual(3, inventory.Count);
        }

        [Test]
        public void TryAdd_RefusesNull()
        {
            var inventory = new Inventory(3);

            Assert.IsFalse(inventory.TryAdd(null));
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void Remove_FreesASlot()
        {
            var inventory = new Inventory(1);
            var item = AnItem();
            inventory.TryAdd(item);

            Assert.IsTrue(inventory.Remove(item));

            Assert.AreEqual(0, inventory.Count);
            Assert.IsTrue(inventory.TryAdd(AnItem()));
        }

        [Test]
        public void Remove_OfAnItemThatIsNotThere_DoesNothing()
        {
            var inventory = new Inventory(2);
            inventory.TryAdd(AnItem());

            Assert.IsFalse(inventory.Remove(AnItem()), "a different item, even with the same stats");
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void Items_AreComparedByReference()
        {
            var inventory = new Inventory(2);
            var a = AnItem();
            var b = AnItem();
            inventory.TryAdd(a);

            Assert.IsTrue(inventory.Contains(a));
            Assert.IsFalse(inventory.Contains(b));
        }

        [Test]
        public void AnItem_HasAtLeastItemLevelOne()
        {
            Assert.AreEqual(1, new Item(ItemSlot.Weapon, ItemRarity.Rare, 0).ItemLevel);
            Assert.AreEqual(1, new Item(ItemSlot.Weapon, ItemRarity.Rare, -4).ItemLevel);
        }

        [Test]
        public void AWeapon_DoesTheDamageOfItsItemLevel()
        {
            var weapon = new Item(ItemSlot.Weapon, ItemRarity.Legendary, 30);

            Assert.AreEqual(CombatFormulas.WeaponAverageDamage(30), weapon.WeaponAverageDamage, 1e-3f);
        }
    }
}
