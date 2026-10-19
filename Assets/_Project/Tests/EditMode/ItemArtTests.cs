using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>Which picture an item shows in the Bag (the owner's pixel art, 2026-10-19).</summary>
    public class ItemArtTests
    {
        [Test]
        public void AnItem_ShowsItsLook_ALegendaryItsOwn_ABlueprintTheScroll()
        {
            Assert.AreEqual("hunting_bow", ItemArtNames.For(new Item(ItemSlot.Weapon, ItemRarity.Magic, 1)));
            Assert.AreEqual("great_bow", ItemArtNames.For(new Item(ItemSlot.TwoHandWeapon, ItemRarity.Rare, 30)));
            Assert.AreEqual("iron_ring", ItemArtNames.For(new Item(ItemSlot.Ring, ItemRarity.Common, 1)));
            Assert.AreEqual("jewelled_amulet", ItemArtNames.For(new Item(ItemSlot.Amulet, ItemRarity.Rare, 40)));
            Assert.AreEqual("splinterbough", ItemArtNames.ForLegendary(LegendaryId.Splinterbough));
            Assert.AreEqual("eye_of_the_storm", ItemArtNames.ForLegendary(LegendaryId.EyeOfTheStorm));
            Assert.AreEqual("pack_leaders_signet", ItemArtNames.ForLegendary(LegendaryId.PackLeadersSignet));
        }

        [Test]
        public void EveryLookAndLegendary_HasAName()
        {
            foreach (ItemSlot kind in System.Enum.GetValues(typeof(ItemSlot)))
                for (var i = 0; i < 5; i++)
                    Assert.IsNotNull(ItemArtNames.ForLook(kind, i), kind + " " + i);
            foreach (var legendary in Legendaries.All)
                Assert.IsNotNull(ItemArtNames.ForLegendary(legendary.Id), legendary.Name);
        }
    }
}
