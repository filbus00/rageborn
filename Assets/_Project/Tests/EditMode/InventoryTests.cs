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

        [Test]
        public void OnlyChestAndHelm_HaveABaseArmorValue()
        {
            Assert.AreEqual(CombatFormulas.BaseArmorPerPiece(20), new Item(ItemSlot.Chest, ItemRarity.Common, 20).ArmorValue, 1e-3f);
            Assert.AreEqual(CombatFormulas.BaseArmorPerPiece(20), new Item(ItemSlot.Helm, ItemRarity.Common, 20).ArmorValue, 1e-3f);
            Assert.AreEqual(0f, new Item(ItemSlot.Weapon, ItemRarity.Common, 20).ArmorValue);
        }

        [Test]
        public void AffixSum_AddsUpEveryMatchingRoll_AndIgnoresOthers()
        {
            var affixes = new[]
            {
                new AffixRoll(AffixId.CriticalChance, 3, 5f),
                new AffixRoll(AffixId.CriticalChance, 5, 2f),
                new AffixRoll(AffixId.AttackSpeed, 1, 10f),
            };
            var item = new Item(ItemSlot.Weapon, ItemRarity.Rare, 30, affixes);

            Assert.AreEqual(7f, item.CriticalChancePercent, 1e-4f);
            Assert.AreEqual(10f, item.AttackSpeedPercent, 1e-4f);
            Assert.AreEqual(0f, item.LifeOnHit);
        }

        [Test]
        public void AnItemWithNoAffixesGiven_HasAnEmptyAffixList()
        {
            var item = new Item(ItemSlot.Weapon, ItemRarity.Common, 1);

            Assert.IsEmpty(item.Affixes);
        }
    }
}
