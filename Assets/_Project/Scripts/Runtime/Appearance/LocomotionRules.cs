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

        public static bool IsBackpedal(Vector2 aim, Vector2 velocity) =>
            aim.sqrMagnitude > 1e-6f && velocity.sqrMagnitude > 1e-6f && Vector2.Angle(aim, velocity) > BackpedalDegrees;

        /// <summary>How fast to play a locomotion cycle: the actual speed over the speed it was recorded at, within limits.
        /// 1 when the recorded speed is unknown.</summary>
        public static float PlaybackRate(float speed, float recordedSpeed) =>
            recordedSpeed <= 0f ? 1f : Mathf.Clamp(speed / recordedSpeed, MinRate, MaxRate);
    }
}
