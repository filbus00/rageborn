using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The Wild Arrow's stat points (Docs/02, decided 2026-09-30): 5 a level on Strength, Agility, Vitality,
    /// Speed and Focus.</summary>
    public class AttributeTests
    {


        [Test]
        public void Bonuses_FollowTheDocs_FromSpentPointsOnly()
        {
            var none = new CharacterAttributes(0, 0, 0, 0, 0);
            Assert.AreEqual(CharacterAttributes.StartingValue, none.Shown(Attribute.Speed));
            Assert.AreEqual(0f, none.IncreasedDamage);

            var ten = new CharacterAttributes(10, 10, 10, 10, 10);
            Assert.AreEqual(20, ten.Shown(Attribute.Focus));
            Assert.AreEqual(0.05f, ten.IncreasedDamage, 1e-5f);
            Assert.AreEqual(10f, ten.Armor, 1e-5f);
            Assert.AreEqual(2f, ten.CriticalChance, 1e-5f);
            Assert.AreEqual(0.02f, ten.Dodge, 1e-5f);
            Assert.AreEqual(10f, ten.CriticalDamage, 1e-5f);
            Assert.AreEqual(80f, ten.Life, 1e-5f);
            Assert.AreEqual(0.04f, ten.AttackSpeed, 1e-5f);
            Assert.AreEqual(0.02f, ten.MoveSpeed, 1e-5f);
            Assert.AreEqual(0.05f, ten.FocusRegen, 1e-5f);
            Assert.AreEqual(0.03f, ten.CooldownReduction, 1e-5f);
            Assert.AreEqual(10f, ten.MaxFocus, 1e-5f);
        }


    }
}
