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
            if (theme == DungeonTheme.Library)
                return LibraryWall(isOpen, x, y, cameraSide, freeStanding, hash);
            if (theme == DungeonTheme.Prison)
                return PrisonWall(isOpen, x, y, cameraSide, freeStanding, hash);
            // A dais's rim (2026-10-20): plain stone mostly, now and then a balustrade or a candle stand.
            if (ledge)
            {
                var pick = hash % 10;
                return "crypt_ledge_" + (pick < 6 ? 1 : pick < 9 ? 2 : 3);
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
        /// The burnt library's piece (2026-10-11): built like the crypt, its walls lined with charred bookshelves. A
        /// free-standing row of shelves (an aisle, <see cref="DungeonGenerator"/>) shows a double-sided shelf along its
        /// row, a lone block a stone column; cut corners and camera-side walls as the crypt's, burnt.
        /// </summary>
        static string LibraryWall(System.Func<int, int, bool> isOpen, int x, int y, bool cameraSide, bool freeStanding, int hash)
        {
            if (freeStanding)
            {
                if (!isOpen(x - 1, y) || !isOpen(x + 1, y))
                    return "library_shelf_x_" + (1 + hash % 2);
                if (!isOpen(x, y - 1) || !isOpen(x, y + 1))
                    return "library_shelf_y_" + (1 + hash % 2);
                return "library_pillar";
            }
            var diagonal = Diagonal(isOpen, x, y);
            if (diagonal != null)
                return "library_diag_" + diagonal + (cameraSide ? "_low" : "");
            if (cameraSide)
                return "library_wall_low_" + (1 + hash % 2);
            // Full shelves most often, then burnt-out ones, a fallen shelf, and bare scorched stone.
            var roll = hash % 10;
            return "library_wall_" + (roll < 4 ? 1 : roll < 7 ? 2 : roll < 9 ? 3 : 4);
        }

        /// <summary>
        /// The prison's piece (2026-10-12): built like the crypt in dark rough stone. A cage standing in a hall
        /// (<see cref="DungeonGenerator"/>) shows bars along its run, a post where its runs meet; cut corners and
        /// camera-side walls as the crypt's; the walls barred cells most often, then shackles, cell doors, bloodied stone.
        /// </summary>
        static string PrisonWall(System.Func<int, int, bool> isOpen, int x, int y, bool cameraSide, bool freeStanding, int hash)
        {
            if (freeStanding)
            {
                // The runs out of the cell, to each side with bars: w (-x), e (+x), s (-y), n (+y), in that order.
                var sides = (isOpen(x - 1, y) ? "" : "w") + (isOpen(x + 1, y) ? "" : "e") + (isOpen(x, y - 1) ? "" : "s") + (isOpen(x, y + 1) ? "" : "n");
                return "prison_bars_" + (sides.Length > 0 ? sides : "post");
            }
            var diagonal = Diagonal(isOpen, x, y);
            if (diagonal != null)
                return "prison_diag_" + diagonal + (cameraSide ? "_low" : "");
            if (cameraSide)
                return "prison_wall_low_" + (1 + hash % 2);
            // Barred cells most often, then plain stone, shackles, a cell door, bloodied stone with a chain.
            var roll = hash % 10;
            return "prison_wall_" + (roll < 4 ? 2 : roll < 6 ? 1 : roll < 8 ? 3 : roll < 9 ? 4 : 5);
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
            theme == DungeonTheme.Cave ? "cave_wall_" + (1 + Mathf.Abs(hash) % 6) : theme == DungeonTheme.Library ? "library_wall_4" :
            theme == DungeonTheme.Prison ? "prison_wall_1" : "crypt_wall_1";

        /// <summary>The steps through a terrace's or a dais's rim, by the way down: rough in the caves, dressed stone
        /// elsewhere.</summary>
        public static string Stairs(DungeonTheme theme, Vector2Int down) =>
            (theme == DungeonTheme.Cave ? "cave_stairs_" : "crypt_stairs_") +
            (down.x > 0 ? "px" : down.x < 0 ? "nx" : down.y > 0 ? "py" : "ny");
    }
}
