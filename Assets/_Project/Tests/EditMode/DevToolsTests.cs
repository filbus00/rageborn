using NUnit.Framework;

namespace ARPG.Tests
{
    public class DevToolsTests
    {
        [Test]
        public void EquipLegendaries_PutsALegendaryInEveryPlaceThatHasOne()
        {
            var session = new GameSession(7);
            var put = DevTools.EquipLegendaries(session);
            Assert.AreEqual(9, put);
            foreach (var place in new[] { ItemSlot.Weapon, ItemSlot.OffHand, ItemSlot.Helm, ItemSlot.Chest, ItemSlot.Gloves,
                         ItemSlot.Boots, ItemSlot.Belt, ItemSlot.Amulet, ItemSlot.Ring })
                Assert.AreEqual(ItemRarity.Legendary, session.Equipment.Get(place)?.Rarity, place.ToString());
        }

        [Test]
        public void MaxCharacter_ReachesTheLevelCap_WithEveryTalent()
        {
            var session = new GameSession(7);
            DevTools.MaxCharacter(session);
            Assert.AreEqual(Experience.MaxLevel, session.Level);
            foreach (var node in TalentRules.Nodes)
                Assert.AreEqual(node.MaxRanks, session.Talents.RankOf(node.Id), node.Id);
            Assert.AreEqual(0, session.Attributes.Available(session.Level));
            foreach (Attribute stat in System.Enum.GetValues(typeof(Attribute)))
                Assert.AreEqual(AttributePoints.Earned(session.Level) / 5, session.Attributes.Spent(stat), stat.ToString());
        }
    }
}
