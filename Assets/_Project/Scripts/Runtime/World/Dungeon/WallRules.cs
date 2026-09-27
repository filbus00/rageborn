namespace ARPG
{
    /// <summary>
    /// Which walls are drawn cut down (the user's decision of 2026-09-27, Docs/09-art-brief.md 14.3): as in Diablo 2, a
    /// wall between the camera and the room never hides the character. On screen a cell sits higher the larger its x + y
    /// (<see cref="IsoMath.CellToGround"/>), so a wall hides what stands on the floor cells behind it: at x + 1, y + 1, or
    /// both. A wall with floor there is on the camera side and drawn low; the far walls, with only void or wall behind,
    /// stay full height and frame the room. Pure.
    /// </summary>
    public static class WallRules
    {
        public static bool IsCameraSide(System.Func<int, int, bool> isFloor, int x, int y) =>
            isFloor(x + 1, y) || isFloor(x, y + 1) || isFloor(x + 1, y + 1);
    }
}
