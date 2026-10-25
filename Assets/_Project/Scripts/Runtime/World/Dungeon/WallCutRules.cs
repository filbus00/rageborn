using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Curved walls (2026-10-25, the owner: "is it possible to make the map edges more curved or does it att need to be in
    /// 45 degrees? It looks kind of bad", then "the dungeon walls on screen. Do your recommendation"): every grid corner
    /// gets how open it is (the share of the four cells round it that are open floor), smoothed over its neighbours; a
    /// wall cell beside the floor is cut where that crosses one half, the cut's ends snapped to the cell's corners or the
    /// middles of its sides. The cell's eight points run counterclockwise from its -x -y corner (corners even, middles
    /// odd); a cut is the wall from point a counterclockwise to point b (themes.py <c>crypt_cut_ab</c>). Pure.
    /// </summary>
    public static class WallCutRules
    {
        /// <summary>
        /// The cut of a wall cell from how open its corners are (-x -y, +x -y, +x +y, -x +y; 0 closed, 1 open); false
        /// when the cell stays a whole wall: no open corner, every corner open, two open corners across from each other,
        /// or a cut that would leave a sliver or the whole cell.
        /// </summary>
        public static bool TryCut(float c0, float c2, float c4, float c6, out int a, out int b)
        {
            a = b = 0;
            var v = new[] { c0, c2, c4, c6 };
            var open = new bool[4];
            var count = 0;
            for (var k = 0; k < 4; k++)
            {
                open[k] = v[k] >= 0.5f;
                if (open[k])
                    count++;
            }
            if (count == 0 || count == 4)
                return false;
            if (count == 2 && open[0] == open[2])
                return false;
            var start = -1;
            var end = -1;
            for (var k = 0; k < 4; k++)
            {
                var j = (k + 1) % 4;
                if (open[k] == open[j])
                    continue;
                // Where one half falls along the side from corner k to corner j, snapped to a corner or the middle.
                var t = (0.5f - v[k]) / (v[j] - v[k]);
                var point = t < 0.25f ? 2 * k : t < 0.75f ? 2 * k + 1 : 2 * j;
                if (open[k])
                    start = point;
                else
                    end = point;
            }
            var span = ((end - start) % 8 + 8) % 8;
            var valid = span >= 3 && span <= 5 || (span == 2 || span == 6) && start % 2 == 1;
            if (!valid)
                return false;
            a = start;
            b = end;
            return true;
        }

        /// <summary>How open a grid corner is: the share of the four cells round it that are open, given in the order
        /// (-1, -1), (0, -1), (-1, 0), (0, 0) from the corner.</summary>
        public static float CornerOpen(bool a, bool b, bool c, bool d) => ((a ? 1 : 0) + (b ? 1 : 0) + (c ? 1 : 0) + (d ? 1 : 0)) / 4f;
    }
}
