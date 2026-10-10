using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Which of the crypt and cave pieces (2026-10-10, ArtSource/tools/props/themes.py, imported to Resources/Art/World)
    /// a cell of a level shows. Pure: the level asks with the cell's neighbourhood and a hash, and draws the piece if it
    /// was imported, else the older walls.
    /// </summary>
    public static class ThemePieces
    {
        /// <summary>
        /// The piece for a wall cell beside open floor (<paramref name="isOpen"/>): in the crypt a column for a pillar, a
        /// diagonal wall where the floor meets it on two touching sides (cut corners, round rooms), else a straight wall,
        /// low on the camera side; in the caves a stalagmite for a free-standing clump, a ledge for a terrace's rim, else
        /// boulders, low on the camera side.
        /// </summary>
        public static string Wall(DungeonTheme theme, System.Func<int, int, bool> isOpen, int x, int y, bool cameraSide, bool freeStanding, bool ledge, int hash)
        {
            hash = Mathf.Abs(hash);
            if (theme == DungeonTheme.Cave)
            {
                if (ledge)
                    return "cave_ledge_" + (1 + hash % 3);
                if (freeStanding)
                    return "stalagmite_" + (1 + hash % 3);
                return cameraSide ? "cave_wall_low_" + (1 + hash % 3) : "cave_wall_" + (1 + hash % 6);
            }
            if (freeStanding)
                return "crypt_pillar_" + (1 + hash % 2);
            var diagonal = Diagonal(isOpen, x, y);
            if (diagonal != null)
                return "crypt_diag_" + diagonal + (cameraSide ? "_low" : "");
            if (cameraSide)
                return "crypt_wall_low_" + (1 + hash % 2);
            // Plain stone about half the time, else a pilaster, a niche on either face or cobwebs.
            var roll = hash % 10;
            return "crypt_wall_" + (roll < 6 ? 1 : roll - 4);
        }

        /// <summary>
        /// The diagonal wall a cell takes when open floor meets it on exactly two touching sides: "b" when the floor is
        /// toward -x and -y (the wall fills the far half and faces the camera), "f" toward +x and +y, "l" toward +x and
        /// -y, "r" toward -x and +y. Null otherwise.
        /// </summary>
        public static string Diagonal(System.Func<int, int, bool> isOpen, int x, int y)
        {
            var w = isOpen(x - 1, y);
            var e = isOpen(x + 1, y);
            var s = isOpen(x, y - 1);
            var n = isOpen(x, y + 1);
            if (w && s && !e && !n)
                return "b";
            if (e && n && !w && !s)
                return "f";
            if (e && s && !w && !n)
                return "l";
            if (w && n && !e && !s)
                return "r";
            return null;
        }

        /// <summary>Rock further from the floor, darkened by the level: the theme's plain wall.</summary>
        public static string Rock(DungeonTheme theme, int hash) =>
            theme == DungeonTheme.Cave ? "cave_wall_" + (1 + Mathf.Abs(hash) % 6) : "crypt_wall_1";

        /// <summary>The steps through a terrace's rim, by the way down.</summary>
        public static string Stairs(Vector2Int down) =>
            down.x > 0 ? "cave_stairs_px" : down.x < 0 ? "cave_stairs_nx" : down.y > 0 ? "cave_stairs_py" : "cave_stairs_ny";
    }
}
