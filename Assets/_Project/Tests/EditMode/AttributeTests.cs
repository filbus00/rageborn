using NUnit.Framework;

namespace ARPG.Tests
{
    public class AttributeTests
    {
        [Test]
        public void Attributes_GrowByThemselves_EachLevelFromTwo()
        {
            var one = CharacterAttributes.At(1);
            Assert.AreEqual(0, one.Might + one.Agility + one.Will + one.Vitality);
            var seven = CharacterAttributes.At(7);
            Assert.AreEqual(48, seven.Vitality, "the stand-in's 8 a level");
            Assert.AreEqual(12, seven.Might);
            Assert.AreEqual(12, seven.Agility);
            Assert.AreEqual(6, seven.Will);
            Assert.AreEqual(0.06f, seven.IncreasedDamage, 1e-5f, "act 1's end: +6 percent damage");
            Assert.AreEqual(12f, seven.Armor, 1e-5f);
            Assert.AreEqual(0.048f, seven.AttackSpeed, 1e-5f);
            Assert.AreEqual(2.4f, seven.CriticalChance, 1e-5f);
            Assert.AreEqual(0.018f, seven.Dodge, 1e-5f);
            Assert.AreEqual(0.03f, seven.RageGain, 1e-5f);
            Assert.AreEqual(0.018f, seven.CooldownReduction, 1e-5f);
            var sixty = CharacterAttributes.At(60);
            Assert.AreEqual(118, sixty.Might);
            Assert.AreEqual(59, sixty.Will);
        }
    }
}
