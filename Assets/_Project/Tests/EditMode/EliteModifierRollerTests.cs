using System;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class EliteModifierRollerTests
    {
        [Test]
        public void Roll_NeverReturnsNone()
        {
            for (var seed = 0; seed < 200; seed++)
                Assert.AreNotEqual(EliteModifiers.None, EliteModifierRoller.Roll(new Random(seed)));
        }

        [Test]
        public void Roll_GivesOneOrTwoModifiers_NeverMore()
        {
            // Also the real check against picking the same modifier twice: OR-ing one flag with itself leaves the
            // bit count at 1, so a "two" roll that secretly duplicated would never show up as count 2 here.
            var sawOne = false;
            var sawTwo = false;

            for (var seed = 0; seed < 200; seed++)
            {
                var count = CountFlags(EliteModifierRoller.Roll(new Random(seed)));
                Assert.GreaterOrEqual(count, 1);
                Assert.LessOrEqual(count, 2);
                sawOne |= count == 1;
                sawTwo |= count == 2;
            }

            Assert.IsTrue(sawOne, "some rolls should give exactly one modifier");
            Assert.IsTrue(sawTwo, "some rolls should give exactly two modifiers");
        }

        [Test]
        public void TheSameSeed_RollsTheSameModifiers()
        {
            var a = EliteModifierRoller.Roll(new Random(42));
            var b = EliteModifierRoller.Roll(new Random(42));

            Assert.AreEqual(a, b);
        }

        [Test]
        public void DifferentSeeds_CanGiveDifferentModifiers()
        {
            var seen = new System.Collections.Generic.HashSet<EliteModifiers>();
            for (var seed = 0; seed < 50; seed++)
                seen.Add(EliteModifierRoller.Roll(new Random(seed)));

            Assert.Greater(seen.Count, 1);
        }

        static int CountFlags(EliteModifiers modifiers)
        {
            var count = 0;
            foreach (EliteModifiers value in Enum.GetValues(typeof(EliteModifiers)))
                if (value != EliteModifiers.None && (modifiers & value) == value)
                    count++;
            return count;
        }
    }
}
