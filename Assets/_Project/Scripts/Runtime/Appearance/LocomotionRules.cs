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
        /// <summary>The 8 rows are 45 degrees apart, so the natural switch is at 22.5 degrees off a row; past 30 the
        /// facing switches, which leaves a margin a wobbling stick does not cross back and forth.</summary>
        public const float RowSwitchDegrees = 30f;

        /// <summary>Moving more than this far from the aim counts as retreating from the target.</summary>
        public const float BackpedalDegrees = 112.5f;

        /// <summary>Limits on how much faster or slower than recorded a run cycle plays, so a crawl or a dash does not
        /// turn it into a blur or a freeze.</summary>
        public const float MinRate = 0.6f;
        public const float MaxRate = 2.2f;

        /// <summary>The ground direction a sheet row faces (row 0 south, then clockwise seen from above: SW, W, NW...).</summary>
        public static Vector2 RowDirection(int row)
        {
            var radians = (270f - 45f * row) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        /// <summary>The row to show for a movement direction: the current one until the direction is past the margin.</summary>
        public static int ChooseRow(int current, Vector2 direction)
        {
            if (direction.sqrMagnitude < 1e-6f)
                return current;
            return Vector2.Angle(RowDirection(current), direction) > RowSwitchDegrees ? AppearanceRules.DirectionRow(direction) : current;
        }

        /// <summary>
        /// A change of one row is a turn: +1 when the new row is the next clockwise seen from above (the character's
        /// right), -1 for its left, 0 for no change or a bigger one (which just switches).
        /// </summary>
        public static int TurnStep(int from, int to)
        {
            var delta = ((to - from) % 8 + 8) % 8;
            return delta == 1 ? 1 : delta == 7 ? -1 : 0;
        }

        public static bool IsBackpedal(Vector2 aim, Vector2 velocity) =>
            aim.sqrMagnitude > 1e-6f && velocity.sqrMagnitude > 1e-6f && Vector2.Angle(aim, velocity) > BackpedalDegrees;

        /// <summary>How fast to play a locomotion cycle: the actual speed over the speed it was recorded at, within limits.
        /// 1 when the recorded speed is unknown.</summary>
        public static float PlaybackRate(float speed, float recordedSpeed) =>
            recordedSpeed <= 0f ? 1f : Mathf.Clamp(speed / recordedSpeed, MinRate, MaxRate);
    }
}
