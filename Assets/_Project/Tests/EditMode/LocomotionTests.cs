using ARPG.Editor;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class LocomotionTests
    {
        [Test]
        public void RowDirections_MatchTheSheetRows()
        {
            for (var row = 0; row < 8; row++)
                Assert.AreEqual(row, AppearanceRules.DirectionRow(LocomotionRules.RowDirection(row)));
        }

        [Test]
        public void TheFacing_KeepsItsRow_UntilPastTheMargin()
        {
            var south = 0;
            // 25 degrees off south toward west: past the plain 22.5 switch, inside the 30 degree margin.
            Assert.AreEqual(south, LocomotionRules.ChooseRow(south, Rotate(Vector2.down, -25f)));
            Assert.AreEqual(1, LocomotionRules.ChooseRow(south, Rotate(Vector2.down, -35f)), "SW past the margin");
            Assert.AreEqual(south, LocomotionRules.ChooseRow(south, Vector2.zero), "standing keeps the row");
        }

        [Test]
        public void TurnSteps_AreOneRowEitherWay_OnlyWhenRunningClockwiseIsRight()
        {
            Assert.AreEqual(1, LocomotionRules.TurnStep(0, 1), "south to south-west is the character's right");
            Assert.AreEqual(-1, LocomotionRules.TurnStep(0, 7), "south to south-east is its left");
            Assert.AreEqual(1, LocomotionRules.TurnStep(7, 0), "wraps around");
            Assert.AreEqual(0, LocomotionRules.TurnStep(0, 2), "a bigger change just switches");
            Assert.AreEqual(0, LocomotionRules.TurnStep(3, 3));
        }

        [Test]
        public void Backpedal_IsMovingAwayFromTheAim()
        {
            Assert.IsTrue(LocomotionRules.IsBackpedal(Vector2.up, Vector2.down));
            Assert.IsTrue(LocomotionRules.IsBackpedal(Vector2.up, Rotate(Vector2.down, 60f)));
            Assert.IsFalse(LocomotionRules.IsBackpedal(Vector2.up, Vector2.right), "sideways is not retreating");
            Assert.IsFalse(LocomotionRules.IsBackpedal(Vector2.zero, Vector2.down));
        }

        [Test]
        public void TheRunCycle_PlaysAtTheActualSpeed_WithinLimits()
        {
            Assert.AreEqual(1.6f, LocomotionRules.PlaybackRate(4f, 2.5f), 1e-5f, "4 units a second on a 2.5 run");
            Assert.AreEqual(LocomotionRules.MaxRate, LocomotionRules.PlaybackRate(20f, 2.5f));
            Assert.AreEqual(LocomotionRules.MinRate, LocomotionRules.PlaybackRate(0.1f, 2.5f));
            Assert.AreEqual(1f, LocomotionRules.PlaybackRate(4f, 0f), "unknown recorded speed plays as recorded");
        }

        [Test]
        public void PlantedFootSpeed_IsTheSlideOfTheFootOnTheGround()
        {
            // One foot planted for 5 samples sliding back 0.05 a sample (3 units a second at 60), then lifted.
            var planted = new Vector3[10];
            for (var i = 0; i < 10; i++)
                planted[i] = i < 5 ? new Vector3(0f, 0f, -0.05f * i) : new Vector3(0f, 0.3f, 0f);
            var swinging = new Vector3[10];
            for (var i = 0; i < 10; i++)
                swinging[i] = new Vector3(0f, 0.3f, 0.1f * i);
            Assert.AreEqual(3f, SpriteBakeMath.PlantedFootSpeed(new[] { planted, swinging }, 60f, 0.03f), 1e-4f);
            Assert.AreEqual(0f, SpriteBakeMath.PlantedFootSpeed(new[] { new Vector3[10], new Vector3[10] }, 60f, 0.03f),
                "feet that never move: an idle");
        }

        static Vector2 Rotate(Vector2 v, float degrees) => Quaternion.Euler(0f, 0f, degrees) * v;
    }
}
