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

        [Test]
        public void SixteenDirections_RoundTrip_TurnOnTwoRows_AndSwitchPastTheirOwnMargin()
        {
            var codes = AppearanceRules.DirectionCodes(16);
            Assert.AreEqual(16, codes.Length);
            for (var row = 0; row < 16; row++)
            {
                Assert.AreEqual(row, AppearanceRules.DirectionRow(LocomotionRules.RowDirection(row, 16), 16), codes[row]);
                var facing = SpriteBakeMath.Facing(row, 16);
                Assert.AreEqual(row, AppearanceRules.DirectionRow(new Vector2(facing.x, facing.z), 16), "the bake renders row " + codes[row] + " that way");
            }
            for (var row = 0; row < 8; row++)
                Assert.AreEqual(AppearanceRules.DirectionCodes(8)[row], codes[row * 2], "every second of the 16 is one of the 8");

            Assert.AreEqual(1, LocomotionRules.TurnStep(0, 2, 16), "45 degrees to the right is a turn");
            Assert.AreEqual(-1, LocomotionRules.TurnStep(0, 14, 16));
            Assert.AreEqual(0, LocomotionRules.TurnStep(0, 1, 16), "22.5 just switches");
            Assert.AreEqual(0, LocomotionRules.TurnStep(0, 4, 16), "90 just switches");

            // Rows are 22.5 apart: the switch is at 11.25 plus the 7.5 margin.
            Assert.AreEqual(0, LocomotionRules.ChooseRow(0, Rotate(Vector2.down, -15f), 16));
            Assert.AreEqual(1, LocomotionRules.ChooseRow(0, Rotate(Vector2.down, -22f), 16));
        }

        [Test]
        public void ActiveWindow_IsTheBusyStretch_BridgingShortPauses()
        {
            // 10 samples a second over 5 s: sway everywhere, a swing at 2 to 2.6 s, a pause, a step back at 3 to 3.4 s.
            var motion = new float[50];
            for (var i = 0; i < 50; i++)
                motion[i] = 0.1f;
            for (var i = 20; i < 26; i++)
                motion[i] = 1f;
            for (var i = 30; i < 34; i++)
                motion[i] = 0.6f;
            var window = SpriteBakeMath.ActiveWindow(motion, 10f, 5f, 0.2f, false);
            Assert.AreEqual(1.8f, window.x, 1e-4f);
            Assert.AreEqual(3.6f, window.y, 1e-4f);
            Assert.AreEqual(5f, SpriteBakeMath.ActiveWindow(motion, 10f, 5f, 0.2f, true).y, 1e-4f, "a death keeps its last pose");

            // A second burst after a pause longer than 0.8 s is not part of it.
            for (var i = 30; i < 34; i++)
                motion[i] = 0.1f;
            for (var i = 40; i < 44; i++)
                motion[i] = 0.6f;
            Assert.AreEqual(2.8f, SpriteBakeMath.ActiveWindow(motion, 10f, 5f, 0.2f, false).y, 1e-4f);
            Assert.AreEqual(new Vector2(0f, 5f), SpriteBakeMath.ActiveWindow(new float[50], 10f, 5f, 0.2f, false), "no motion keeps it all");
        }

        [Test]
        public void Actions_PlayOverTheForwardOrBackwardRun_ByWhereTheTargetIs()
        {
            Assert.AreEqual("hew", LocomotionRules.ActionSheet("hew", false));
            Assert.AreEqual("hew", LocomotionRules.ActionSheet("hew", false, true), "standing has no legs to choose");
            Assert.AreEqual("hew_move", LocomotionRules.ActionSheet("hew", true));
            Assert.AreEqual("hew_move_back", LocomotionRules.ActionSheet("hew", true, true));

            Assert.IsFalse(LocomotionRules.IsActionBackward(Rotate(Vector2.up, 30f), Vector2.up), "ahead: forward legs");
            Assert.IsFalse(LocomotionRules.IsActionBackward(Rotate(Vector2.up, 105f), Vector2.up), "just past the side: stays forward");
            Assert.IsTrue(LocomotionRules.IsActionBackward(Rotate(Vector2.up, 115f), Vector2.up), "well behind: backward legs");
            Assert.IsTrue(LocomotionRules.IsActionBackward(Rotate(Vector2.up, 75f), Vector2.up, true), "backward stays until 70");
            Assert.IsFalse(LocomotionRules.IsActionBackward(Rotate(Vector2.up, 65f), Vector2.up, true), "then forward again");
            Assert.IsTrue(LocomotionRules.IsActionBackward(Vector2.down, Vector2.up), "behind: backward legs");
            Assert.IsFalse(LocomotionRules.IsActionBackward(Vector2.down, Vector2.zero), "standing still");
        }

        [Test]
        public void MovingActionLegs_KeepPaceWithTheGround()
        {
            // A 0.7 s run loop at 1.5 times its pace under an action played over 1 s, 11 frames with both ends.
            var times = SpriteBakeMath.LegTimes(11, true, 1f, 1.5f, 0.7f);
            Assert.AreEqual(0f, times[0], 1e-5f);
            Assert.AreEqual(0.15f, times[1], 1e-5f, "0.1 s of play is 0.15 s of the loop");
            Assert.AreEqual(Mathf.Repeat(1.5f, 0.7f), times[10], 1e-5f, "wraps around the loop");
            Assert.AreEqual(0f, SpriteBakeMath.LegTimes(3, true, 1f, 1.5f, 0f)[2], "no loop, no legs");
        }

        static Vector2 Rotate(Vector2 v, float degrees) => Quaternion.Euler(0f, 0f, degrees) * v;
    }
}
