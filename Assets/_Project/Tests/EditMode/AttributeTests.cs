using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The Wild Arrow's stat points (Docs/02, decided 2026-09-30): 5 a level on Strength, Agility, Vitality,
    /// Speed and Focus.</summary>
    public class AttributeTests
    {
        [Test]
        public void Points_AreEarnedFiveALevel_FromLevelTwo()
        {
            Assert.AreEqual(0, AttributePoints.Earned(1));
            Assert.AreEqual(5, AttributePoints.Earned(2));
            Assert.AreEqual(295, AttributePoints.Earned(60));
        }

        [Test]
        public void Spending_TakesAPoint_AndStopsWhenNoneAreLeft()
        {
            var points = new AttributePoints();
            var changes = 0;
            points.Changed += () => changes++;
            Assert.IsFalse(points.Spend(Attribute.Strength, 1), "nothing to spend at level 1");
            for (var i = 0; i < 5; i++)
                Assert.IsTrue(points.Spend(Attribute.Agility, 2));
            Assert.IsFalse(points.Spend(Attribute.Agility, 2));
            Assert.AreEqual(5, points.Spent(Attribute.Agility));
            Assert.AreEqual(0, points.Available(2));
            Assert.AreEqual(5, points.Available(3));
            Assert.AreEqual(5, changes);
        }

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

        [Test]
        public void TheTypicalBuild_PutsOneInFiveInEach()
        {
            var seven = CharacterAttributes.At(7);
            Assert.AreEqual(6, seven.Strength);
            Assert.AreEqual(6, seven.Focus);
            Assert.AreEqual(AttributePoints.Earned(7), seven.Strength + seven.Agility + seven.Vitality + seven.Speed + seven.Focus);
        }

        [Test]
        public void SpentPoints_SurviveASave_AndRaiseMaxLife()
        {
            var session = new GameSession();
            session.GrantExperience(100000);
            Assert.Greater(session.Level, 2);
            var before = PowerScore.Evaluate(session).EffectiveLife;
            Assert.IsTrue(session.Attributes.Spend(Attribute.Vitality, session.Level));
            Assert.Greater(PowerScore.Evaluate(session).EffectiveLife, before);

            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);
            Assert.AreEqual(1, restored.Attributes.Spent(Attribute.Vitality));
        }
    }
}
