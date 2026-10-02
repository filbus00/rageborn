using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The backpack grid (the owner, 2026-10-02: gear takes realistic space; auto-packed, 10 x 6).</summary>
    public class InventoryTests
    {
        static Item Of(ItemSlot kind) => new Item(kind, ItemRarity.Common, 1);

        [Test]
        public void TheGrid_IsTenBySix()
        {
            var inventory = new Inventory();
            Assert.AreEqual(10, inventory.Columns);
            Assert.AreEqual(6, inventory.Rows);
            Assert.AreEqual(60, inventory.CellCount);
        }

        [Test]
        public void Sizes_FollowTheKind()
        {
            Assert.AreEqual(new Vector2Int(2, 4), ItemSize.Of(ItemSlot.Weapon));
            Assert.AreEqual(new Vector2Int(2, 4), ItemSize.Of(ItemSlot.TwoHandWeapon));
            Assert.AreEqual(new Vector2Int(2, 3), ItemSize.Of(ItemSlot.Shield));
            Assert.AreEqual(new Vector2Int(2, 3), ItemSize.Of(ItemSlot.Chest));
            Assert.AreEqual(new Vector2Int(2, 2), ItemSize.Of(ItemSlot.Helm));
            Assert.AreEqual(new Vector2Int(2, 2), ItemSize.Of(ItemSlot.Gloves));
            Assert.AreEqual(new Vector2Int(2, 2), ItemSize.Of(ItemSlot.Boots));
            Assert.AreEqual(new Vector2Int(2, 1), ItemSize.Of(ItemSlot.Belt));
            Assert.AreEqual(new Vector2Int(1, 1), ItemSize.Of(ItemSlot.Amulet));
            Assert.AreEqual(new Vector2Int(1, 1), ItemSize.Of(ItemSlot.Ring));
        }

        [Test]
        public void Items_GoInTheFirstPlaceTheyFit_AndTakeTheirCells()
        {
            var inventory = new Inventory();
            var bow = Of(ItemSlot.Weapon);
            var ring = Of(ItemSlot.Ring);
            Assert.IsTrue(inventory.TryAdd(bow));
            Assert.IsTrue(inventory.TryAdd(ring));

            Assert.IsTrue(inventory.PlacementOf(bow, out var bowCells));
            Assert.AreEqual(new RectInt(0, 0, 2, 4), bowCells);
            Assert.IsTrue(inventory.PlacementOf(ring, out var ringCells));
            Assert.AreEqual(new RectInt(2, 0, 1, 1), ringCells, "beside the bow on the top row");
            Assert.AreEqual(9, inventory.CellsUsed);
        }

        [Test]
        public void BigItems_RunOutOfRoom_BeforeSmallOnes()
        {
            var inventory = new Inventory();
            var bows = 0;
            while (inventory.TryAdd(Of(ItemSlot.Weapon)))
                bows++;
            Assert.AreEqual(5, bows, "a row of five 2 x 4 bows; the two rows under them are too short for another");
            Assert.IsTrue(inventory.TryAdd(Of(ItemSlot.Helm)), "a 2 x 2 helm still fits under them");
            Assert.IsFalse(inventory.CanFit(Of(ItemSlot.Weapon)));
        }

        [Test]
        public void ANewItem_ThatFitsNowhere_RepacksTheRest_WhenThatMakesRoom()
        {
            // Rings scattered along the top row leave no 2 x 4 gap; packed together they leave plenty.
            var inventory = new Inventory(4, 4);
            var a = Of(ItemSlot.Ring);
            var b = Of(ItemSlot.Ring);
            var c = Of(ItemSlot.Ring);
            var d = Of(ItemSlot.Ring);
            foreach (var ring in new[] { a, b, c, d })
                inventory.TryAdd(ring);
            inventory.Remove(b);
            inventory.Remove(d);
            // a at (0,0), c at (2,0): no 2 wide, 4 tall column is free.
            var bow = Of(ItemSlot.Weapon);
            Assert.IsTrue(inventory.TryAdd(bow), "repacking puts the bow first and the rings beside it");
            Assert.IsTrue(inventory.PlacementOf(bow, out var cells));
            Assert.AreEqual(new Vector2Int(2, 4), cells.size);
            Assert.AreEqual(3, inventory.Count);
        }

        [Test]
        public void ATakenOutItem_FreesItsCells()
        {
            var inventory = new Inventory(2, 4);
            var bow = Of(ItemSlot.Weapon);
            Assert.IsTrue(inventory.TryAdd(bow));
            Assert.IsFalse(inventory.TryAdd(Of(ItemSlot.Ring)), "full");
            Assert.IsTrue(inventory.Remove(bow));
            Assert.IsTrue(inventory.TryAdd(Of(ItemSlot.Ring)), "a refused size is asked again once the grid changes");
        }

        [Test]
        public void Null_AndTheSameItemTwice_AreRefused()
        {
            var inventory = new Inventory();
            var ring = Of(ItemSlot.Ring);
            Assert.IsFalse(inventory.TryAdd(null));
            Assert.IsTrue(inventory.TryAdd(ring));
            Assert.IsFalse(inventory.TryAdd(ring));
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void TrySwap_IsAllOrNothing()
        {
            var inventory = new Inventory(2, 4);
            var bow = Of(ItemSlot.Weapon);
            inventory.TryAdd(bow);

            // Out goes the bow, in come a quiver (2 x 3) and a belt (2 x 1): exactly the bow's space.
            var quiver = Of(ItemSlot.Shield);
            var belt = Of(ItemSlot.Belt);
            Assert.IsTrue(inventory.TrySwap(bow, new[] { quiver, belt }));
            Assert.IsFalse(inventory.Contains(bow));
            Assert.AreEqual(8, inventory.CellsUsed);

            // One cell too many: nothing changes.
            var helm = Of(ItemSlot.Helm);
            Assert.IsFalse(inventory.TrySwap(belt, new[] { helm, Of(ItemSlot.Ring) }));
            Assert.IsTrue(inventory.Contains(belt));
            Assert.IsFalse(inventory.Contains(helm));
        }

        [Test]
        public void Replace_KeepsThePlace_ForAnItemOfTheSameSize()
        {
            var inventory = new Inventory();
            var first = Of(ItemSlot.Helm);
            var second = Of(ItemSlot.Ring);
            inventory.TryAdd(first);
            inventory.TryAdd(second);
            inventory.PlacementOf(first, out var before);

            var tempered = new Item(ItemSlot.Helm, ItemRarity.Rare, 5);
            Assert.IsTrue(inventory.Replace(first, tempered));
            Assert.AreSame(tempered, inventory.Items[0]);
            Assert.IsTrue(inventory.PlacementOf(tempered, out var after));
            Assert.AreEqual(before, after);
            Assert.IsFalse(inventory.Replace(second, Of(ItemSlot.Weapon)), "a different size cannot take the place");
        }
    }
}
