using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// How the player's sprite moves with the character (<see cref="PlayerSpriteAnimator"/>), pure so it is tested. The
    /// fixes for "running on ice" (the user, 2026-09-27): the run cycle plays at the speed the character actually moves
    /// (Mixamo's run looks right at about 2.5 ground units a second, the character moves 4 to 5), the facing changes only
    /// past a margin so it does not flicker between two rows, a one-row change plays a turn, and retreating from what the
    /// character fights shows a backward run facing it.
    /// </summary>
    public static class LocomotionRules
    {
        /// <summary>How far past the natural switch (halfway between two rows) the facing must be before it changes, so a
        /// wobbling stick does not flip it back and forth: with 8 rows it switches at 30 degrees off a row, not 22.5.</summary>
        public const float RowSwitchMarginDegrees = 7.5f;

        /// <summary>Moving more than this far from the aim counts as retreating from the target.</summary>
        public const float BackpedalDegrees = 112.5f;

        /// <summary>Limits on how much faster or slower than recorded a run cycle plays, so a crawl or a dash does not
        /// turn it into a blur or a freeze.</summary>
        public const float MinRate = 0.6f;
        public const float MaxRate = 2.2f;

        /// <summary>A change of facing this size, either way, is a turn: the turn clip curves the body about 36 to 40
        /// degrees, one row of 8 or two of 16.</summary>
        public const float TurnDegrees = 45f;

        /// <summary>The ground direction a sheet row faces (row 0 south, then clockwise seen from above).</summary>
        public static Vector2 RowDirection(int row, int count = 8)
        {
            var radians = (270f - 360f / count * row) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        /// <summary>The row to show for a movement direction: the current one until the direction is past the margin.</summary>
        public static int ChooseRow(int current, Vector2 direction, int count = 8)
        {
            if (direction.sqrMagnitude < 1e-6f)
                return current;
            var limit = 180f / count + RowSwitchMarginDegrees;
            return Vector2.Angle(RowDirection(current, count), direction) > limit ? AppearanceRules.DirectionRow(direction, count) : current;
        }

        /// <summary>
        /// A change of facing of about <see cref="TurnDegrees"/> is a turn: +1 when the new row is clockwise seen from
        /// above (the character's right), -1 for its left, 0 for no change, a smaller one or a bigger one (which just
        /// switch).
        /// </summary>
        public static int TurnStep(int from, int to, int count = 8)
        {
            var delta = ((to - from) % count + count) % count;
            var degrees = delta * 360f / count;
            if (degrees > 180f)
                degrees -= 360f;
            if (Mathf.Abs(degrees) < TurnDegrees - 10f || Mathf.Abs(degrees) > TurnDegrees + 10f)
                return 0;
            return degrees > 0f ? 1 : -1;
        }

        /// <summary>The suffix of an action's moving variant: the run's hips and legs under the action's torso, baked by
        /// the sprite bake, so the character swings while it runs.</summary>
        public const string MovingSuffix = "_move";

        /// <summary>Which legs a moving action plays over: the character always faces what it strikes (the owner,
        /// 2026-09-28), and its legs run the way it actually moves relative to that facing.</summary>
        public enum ActionLegs
        {
            Forward,
            StrafeRight,
            StrafeLeft,
            Back,
        }

        /// <summary>The suffix of each leg set's baked moving variant: the run's, the backward run's and the two
        /// strafes' hips and legs under the action's torso.</summary>
        public const string MovingBackSuffix = "_move_back";
        public const string MovingRightSuffix = "_move_right";
        public const string MovingLeftSuffix = "_move_left";

        /// <summary>Up to this angle between the facing and the motion the legs run forward; past it they strafe.</summary>
        public const float ActionStrafeDegrees = 45f;

        /// <summary>Past this angle the legs run backward.</summary>
        public const float ActionBackDegrees = 135f;

        /// <summary>How far past a boundary the legs keep their set, so a target near the edge does not flip them every
        /// few frames (which read as jerking left and right, the owner, 2026-09-28).</summary>
        public const float ActionLegsMargin = 10f;

        /// <summary>The margin the run keeps before switching back from the backward run.</summary>
        public const float BackpedalMargin = 20f;

        /// <summary>
        /// The legs for a moving action facing <paramref name="aim"/> while moving along <paramref name="velocity"/>:
        /// forward within 45 degrees, strafing right or left to 135 (the character's right is clockwise from its facing,
        /// the way <see cref="TurnStep"/> counts), backward beyond; the previous set holds 10 degrees past its edge.
        /// </summary>
        public static ActionLegs LegsFor(Vector2 aim, Vector2 velocity, ActionLegs previous = ActionLegs.Forward)
        {
            if (aim.sqrMagnitude < 1e-6f || velocity.sqrMagnitude < 1e-6f)
                return ActionLegs.Forward;
            var signed = Vector2.SignedAngle(aim, velocity);
            var angle = Mathf.Abs(signed);
            var side = signed < 0f ? ActionLegs.StrafeRight : ActionLegs.StrafeLeft;
            switch (previous)
            {
                case ActionLegs.Forward when angle <= ActionStrafeDegrees + ActionLegsMargin:
                    return ActionLegs.Forward;
                case ActionLegs.Back when angle >= ActionBackDegrees - ActionLegsMargin:
                    return ActionLegs.Back;
                case ActionLegs.StrafeRight:
                case ActionLegs.StrafeLeft:
                    if (previous == side && angle >= ActionStrafeDegrees - ActionLegsMargin && angle <= ActionBackDegrees + ActionLegsMargin)
                        return previous;
                    break;
            }
            return angle <= ActionStrafeDegrees ? ActionLegs.Forward : angle >= ActionBackDegrees ? ActionLegs.Back : side;
        }

        /// <summary>The sheet an action plays: standing, the action itself; moving, its variant for the legs.</summary>
        public static string ActionSheet(string action, bool moving, ActionLegs legs = ActionLegs.Forward) =>
            !moving ? action : action + LegsSuffix(legs);

        public static string LegsSuffix(ActionLegs legs) =>
            legs == ActionLegs.Back ? MovingBackSuffix : legs == ActionLegs.StrafeRight ? MovingRightSuffix
            : legs == ActionLegs.StrafeLeft ? MovingLeftSuffix : MovingSuffix;

        /// <summary>The legs a playing sheet uses, read from its name.</summary>
        public static ActionLegs LegsOf(string sheet) =>
            sheet == null ? ActionLegs.Forward
            : sheet.EndsWith(MovingBackSuffix) ? ActionLegs.Back
            : sheet.EndsWith(MovingRightSuffix) ? ActionLegs.StrafeRight
            : sheet.EndsWith(MovingLeftSuffix) ? ActionLegs.StrafeLeft : ActionLegs.Forward;

        /// <summary>Retreating from the aim: past <see cref="BackpedalDegrees"/>, and once backpedalling until
        /// <see cref="BackpedalMargin"/> under it, so the run and the backward run do not flicker at the edge.</summary>
        public static bool IsBackpedal(Vector2 aim, Vector2 velocity, bool wasBackpedalling = false) =>
            aim.sqrMagnitude > 1e-6f && velocity.sqrMagnitude > 1e-6f &&
            Vector2.Angle(aim, velocity) > BackpedalDegrees - (wasBackpedalling ? BackpedalMargin : 0f);

        /// <summary>How fast to play a locomotion cycle: the actual speed over the speed it was recorded at, within limits.
        /// 1 when the recorded speed is unknown.</summary>
        /// <summary>Below this ground speed she walks instead of running (the owner, 2026-10-10: a walk when moving
        /// slowly). Her full speed is about 4 to 5 a second; this is about a third of the stick's push or slower.</summary>
        public const float WalkBelow = 2.2f;

        /// <summary>How far past <see cref="WalkBelow"/> the walk or run holds, so a speed at the edge does not flip them.</summary>
        public const float WalkMargin = 0.3f;

        /// <summary>Whether to show the walk at this speed: below <see cref="WalkBelow"/>, holding either way by
        /// <see cref="WalkMargin"/>.</summary>
        public static bool Walks(float speed, bool wasWalking) =>
            speed < WalkBelow + (wasWalking ? WalkMargin : -WalkMargin);

        public static float PlaybackRate(float speed, float recordedSpeed) =>
            recordedSpeed <= 0f ? 1f : Mathf.Clamp(speed / recordedSpeed, MinRate, MaxRate);
    }
}
