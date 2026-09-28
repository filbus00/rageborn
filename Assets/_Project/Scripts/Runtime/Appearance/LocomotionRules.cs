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

        /// <summary>The suffix of an action's backward moving variant: the backward run's hips and legs under the action's
        /// torso, for a blow struck at an enemy behind the way the character runs.</summary>
        public const string MovingBackSuffix = "_move_back";

        /// <summary>Beyond this angle between the aim and the run, a moving action plays over the backward run.</summary>
        public const float ActionBackDegrees = 90f;

        /// <summary>
        /// The sheet an action plays: standing, the action itself; moving, its forward moving variant, or its backward one
        /// when the target is more than <see cref="ActionBackDegrees"/> off the way the character runs. The character
        /// always faces what it strikes during an action (the owner, 2026-09-28: a swing turned at most 45 degrees off the
        /// run "still swings their axe pointing away from monster sometimes"), so the legs are at most 90 degrees off the
        /// ground's motion.
        /// </summary>
        public static string ActionSheet(string action, bool moving, bool backward = false) =>
            !moving ? action : backward ? action + MovingBackSuffix : action + MovingSuffix;

        public static bool IsActionBackward(Vector2 aim, Vector2 velocity) =>
            aim.sqrMagnitude > 1e-6f && velocity.sqrMagnitude > 1e-6f && Vector2.Angle(aim, velocity) > ActionBackDegrees;

        public static bool IsBackpedal(Vector2 aim, Vector2 velocity) =>
            aim.sqrMagnitude > 1e-6f && velocity.sqrMagnitude > 1e-6f && Vector2.Angle(aim, velocity) > BackpedalDegrees;

        /// <summary>How fast to play a locomotion cycle: the actual speed over the speed it was recorded at, within limits.
        /// 1 when the recorded speed is unknown.</summary>
        public static float PlaybackRate(float speed, float recordedSpeed) =>
            recordedSpeed <= 0f ? 1f : Mathf.Clamp(speed / recordedSpeed, MinRate, MaxRate);
    }
}
