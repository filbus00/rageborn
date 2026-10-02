using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The merchant's prices and sales, the code-drawn item icons and the layout of names on the ground
    /// (the owner, 2026-10-02).</summary>
    public class SellAndIconTests
    {
        [Test]
        public void Price_GrowsWithRarityAndItemLevel()
        {
            var previous = 0;
            foreach (ItemRarity rarity in System.Enum.GetValues(typeof(ItemRarity)))
            {
                var price = SellRules.Price(new Item(ItemSlot.Helm, rarity, 10));
                Assert.Greater(price, previous, $"{rarity} pays more than the rarity below");
                previous = price;
            }
            Assert.Greater(SellRules.Price(new Item(ItemSlot.Helm, ItemRarity.Magic, 30)), SellRules.Price(new Item(ItemSlot.Helm, ItemRarity.Magic, 5)));
            Assert.Greater(SellRules.Price(new Item(ItemSlot.Weapon, ItemRarity.Legendary, 10, null, 0, 0, LegendaryId.Galeheart)),
                SellRules.Price(new Item(ItemSlot.Weapon, ItemRarity.Legendary, 10)), "a named legendary is worth more");
            Assert.AreEqual(1, Mathf.Min(1, SellRules.Price(new Item(ItemSlot.Ring, ItemRarity.Common, 0))), "never less than 1");
            Assert.AreEqual(0, SellRules.Price(null));
        }

        [Test]
        public void Sell_TakesTheItemAndPaysItsPrice()
        {
            var session = new GameSession();
            var helm = new Item(ItemSlot.Helm, ItemRarity.Magic, 8);
            Assert.IsTrue(session.PickUp(helm));
            var gold = session.Gold;

            var paid = session.Sell(helm);
            Assert.AreEqual(SellRules.Price(helm), paid);
            Assert.AreEqual(gold + paid, session.Gold);
            Assert.IsFalse(session.Inventory.Contains(helm));
            Assert.AreEqual(0, session.Sell(helm), "it is gone; nothing more is paid");
        }

        [Test]
        public void SellAll_KeepsUpgradesNamedLegendariesAndOtherRarities()
        {
            var session = new GameSession();
            // Nothing is worn on the chest, so a chest with life is an upgrade and is kept.
            var upgrade = new Item(ItemSlot.Chest, ItemRarity.Magic, 10, new[] { new AffixRoll(AffixId.Life, 5, 40f) });
            // A good helm is worn, so two plain low helms are not upgrades and go.
            var worn = new Item(ItemSlot.Helm, ItemRarity.Rare, 20, new[] { new AffixRoll(AffixId.Life, 5, 60f) });
            Assert.IsTrue(session.PickUp(worn));
            Assert.IsTrue(session.EquipFromInventory(worn));
            var junk1 = new Item(ItemSlot.Helm, ItemRarity.Magic, 1);
            var junk2 = new Item(ItemSlot.Helm, ItemRarity.Magic, 2);
            var rare = new Item(ItemSlot.Amulet, ItemRarity.Rare, 4);
            foreach (var item in new[] { upgrade, junk1, junk2, rare })
                Assert.IsTrue(session.PickUp(item));
            Assert.IsTrue(PowerScore.IsUpgrade(session.Equipment, upgrade, session.Level, session.PassiveTree.Bonuses), "the chest is an upgrade");
            Assert.IsTrue(!PowerScore.IsUpgrade(session.Equipment, junk1, session.Level, session.PassiveTree.Bonuses));
            Assert.IsTrue(!PowerScore.IsUpgrade(session.Equipment, junk2, session.Level, session.PassiveTree.Bonuses));

            var listed = new List<Item>();
            var quoted = session.BulkSale(ItemRarity.Magic, listed);
            CollectionAssert.AreEquivalent(new[] { junk1, junk2 }, listed);
            Assert.AreEqual(SellRules.Price(junk1) + SellRules.Price(junk2), quoted);

            var gold = session.Gold;
            Assert.AreEqual(quoted, session.SellAll(ItemRarity.Magic));
            Assert.AreEqual(gold + quoted, session.Gold);
            Assert.IsTrue(session.Inventory.Contains(upgrade));
            Assert.IsTrue(session.Inventory.Contains(rare));
            Assert.IsFalse(session.Inventory.Contains(junk1));
            Assert.AreEqual(0, session.SellAll(ItemRarity.Common), "nothing to sell, nothing paid");

            var named = new Item(ItemSlot.Ring, ItemRarity.Legendary, 10, null, 0, 0, LegendaryId.PackLeadersSignet);
            Assert.IsFalse(SellRules.InBulkSale(named, ItemRarity.Legendary, false), "named legendaries are sold one by one");
            CollectionAssert.DoesNotContain(SellRules.BulkRarities, ItemRarity.Legendary);
        }

        [Test]
        public void Icons_AreTheSizeOfTheirBlock_Outlined_WithTheRarityAccentOnlyAboveCommon()
        {
            foreach (ItemSlot kind in System.Enum.GetValues(typeof(ItemSlot)))
            {
                var cells = ItemSize.Of(kind);
                var common = ItemIconArt.Draw(kind, ItemRarity.Common, out var width, out var height);
                Assert.AreEqual(cells.x * ItemIconArt.PixelsPerCell, width, $"{kind} width");
                Assert.AreEqual(cells.y * ItemIconArt.PixelsPerCell, height, $"{kind} height");
                Assert.AreEqual(width * height, common.Length);

                var opaque = 0;
                var outline = 0;
                foreach (var pixel in common)
                {
                    if (pixel.a > 0)
                        opaque++;
                    if (pixel.a > 0 && pixel.r == PixelArt.Outline.r && pixel.g == PixelArt.Outline.g && pixel.b == PixelArt.Outline.b)
                        outline++;
                }
                Assert.Greater(opaque, width * height / 10, $"{kind} draws something");
                Assert.Greater(outline, 0, $"{kind} is outlined");
                Assert.IsFalse(Contains(common, LootColors.Of(ItemRarity.Magic)), $"{kind}: a Common has no accent");

                foreach (var rarity in new[] { ItemRarity.Magic, ItemRarity.Rare, ItemRarity.Legendary })
                    Assert.IsTrue(Contains(ItemIconArt.Draw(kind, rarity, out _, out _), LootColors.Of(rarity)), $"{kind} {rarity} shows its colour");
            }
        }

        [Test]
        public void Icons_TellTheKindsApart()
        {
            var seen = new HashSet<string>();
            foreach (var kind in new[] { ItemSlot.Weapon, ItemSlot.TwoHandWeapon, ItemSlot.Shield, ItemSlot.Chest, ItemSlot.Helm,
                         ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt, ItemSlot.Amulet, ItemSlot.Ring })
            {
                var pixels = ItemIconArt.Draw(kind, ItemRarity.Common, out _, out _);
                var key = new System.Text.StringBuilder();
                foreach (var pixel in pixels)
                    key.Append(pixel.a > 0 ? '1' : '0');
                Assert.IsTrue(seen.Add(key.ToString()), $"{kind} has its own shape");
            }
        }

        [Test]
        public void Labels_ThatWouldOverlap_AreLifted_BetterOnesKeepTheirSpot()
        {
            var layout = new LabelLayout();
            var size = new Vector2(1f, 0.2f);
            var junk = layout.Add(new Vector2(0f, 0f), size, 0);
            var rare = layout.Add(new Vector2(0.2f, 0.05f), size, 2);
            var apart = layout.Add(new Vector2(5f, 0f), size, 0);
            var third = layout.Add(new Vector2(-0.1f, 0f), size, 1);
            var lifts = new List<float>();
            layout.Solve(lifts);

            Assert.AreEqual(4, lifts.Count);
            Assert.AreEqual(0f, lifts[rare], "the rare keeps its spot");
            Assert.AreEqual(0f, lifts[apart], "a label far away is not moved");
            Assert.Greater(lifts[third], 0f);
            Assert.Greater(lifts[junk], lifts[third], "the common goes above the magic");

            var boxes = new List<Rect>();
            for (var i = 0; i < 4; i++)
            {
                var anchor = i == junk ? new Vector2(0f, 0f) : i == rare ? new Vector2(0.2f, 0.05f) : i == apart ? new Vector2(5f, 0f) : new Vector2(-0.1f, 0f);
                boxes.Add(new Rect(anchor.x - 0.5f, anchor.y + lifts[i], 1f, 0.2f));
            }
            for (var a = 0; a < boxes.Count; a++)
                for (var b = a + 1; b < boxes.Count; b++)
                    Assert.IsFalse(boxes[a].Overlaps(boxes[b]), $"labels {a} and {b} overlap");

            layout.Clear();
            Assert.AreEqual(0, layout.Count);
            layout.Solve(lifts);
            Assert.AreEqual(0, lifts.Count);
        }

        static bool Contains(Color32[] pixels, Color color)
        {
            Color32 c = color;
            foreach (var pixel in pixels)
                if (pixel.a > 0 && pixel.r == c.r && pixel.g == c.g && pixel.b == c.b)
                    return true;
            return false;
        }
    }
}
