using UnityEngine;

namespace ARPG
{
    /// <summary>Which of the ground's four textures shows at a point (<see cref="GroundPainter"/>).</summary>
    public enum GroundLayer
    {
        Dirt,
        Cobble,
        Grass,
        Path,
    }

    /// <summary>
    /// What the ground is made of, point by point, in ground space (the owner, 2026-10-04: "Make the town just dirt with
    /// some grassy spots and some paths. Make the dungeon mostly dirt and some cobble stones showing through. Do not
    /// make each room in the dungeon have its own floor texture ... Take inspiration from the first act in Diablo 2").
    /// Broad smooth noise says how much of a layer belongs at a point; the layer's own texture strength (a stone's
    /// height, a grass tuft's density, a path's wear) decides pixel by pixel, so stone tops show through the dirt first
    /// and grass frays into tufts at its edges. The dungeon ignores room styles: one ground runs through every room.
    /// Pure.
    /// </summary>
    public static class GroundRules
    {
        /// <summary>The town's worn paths, as pairs of cells (start, end): from the square to the stairs, the merchant,
        /// the Pet Vendor, the Waystone, the portal, the campfire and the well, and two roads out of town.</summary>
        public static readonly Vector2Int[] TownPaths =
        {
            new Vector2Int(0, 0), new Vector2Int(5, 5),
            new Vector2Int(0, 0), new Vector2Int(-4, 1),
            new Vector2Int(-4, 1), new Vector2Int(-10, 2),
            new Vector2Int(0, 0), new Vector2Int(5, 1),
            new Vector2Int(5, 1), new Vector2Int(14, -3),
            new Vector2Int(0, 0), new Vector2Int(3, -3),
            new Vector2Int(3, -3), new Vector2Int(-2, -11),
            new Vector2Int(0, 0), new Vector2Int(-2, -4),
            new Vector2Int(-4, 1), new Vector2Int(-5, 6),
            new Vector2Int(-5, 6), new Vector2Int(-3, 15),
            new Vector2Int(5, 5), new Vector2Int(18, 8),
            new Vector2Int(14, -3), new Vector2Int(30, -6),
            new Vector2Int(-10, 2), new Vector2Int(-30, 4),
        };

        /// <summary>A path's half width in ground units, before its wobble.</summary>
        public const float PathHalfWidth = 0.8f;

        /// <summary>How much cobble belongs at a point of the dungeon, 0 to 1: mostly none, patches here and there.</summary>
        public static float CobbleCover(Vector2 ground)
        {
            var n = Smooth(ground.x / 5f, ground.y / 5f, 3) * 0.7f + Smooth(ground.x / 1.7f, ground.y / 1.7f, 4) * 0.3f;
            return Mathf.Clamp01((n - 0.52f) * 2.6f);
        }

        /// <summary>How much grass belongs at a point of the town, 0 to 1, before the paths wear it away.</summary>
        public static float GrassCover(Vector2 ground)
        {
            var n = Smooth(ground.x / 6f, ground.y / 6f, 5) * 0.7f + Smooth(ground.x / 2f, ground.y / 2f, 6) * 0.3f;
            return Mathf.Clamp01((n - 0.36f) * 2.2f);
        }

        /// <summary>How much path is at a point, 1 on a path's middle line, 0 beyond its edge (its edge wobbles).</summary>
        public static float PathCover(Vector2 ground, Vector2Int[] paths)
        {
            // The point is nudged by smooth noise first, so a path's line meanders instead of running ruler straight.
            var bent = ground + new Vector2(Smooth(ground.x / 3f, ground.y / 3f, 8) - 0.5f, Smooth(ground.x / 3f, ground.y / 3f, 9) - 0.5f) * 0.7f;
            var best = float.MaxValue;
            for (var i = 0; i + 1 < paths.Length; i += 2)
                best = Mathf.Min(best, SegmentDistance(bent, IsoMath.CellToGround(paths[i]), IsoMath.CellToGround(paths[i + 1])));
            var width = PathHalfWidth * (0.75f + 0.5f * Smooth(ground.x / 2.5f, ground.y / 2.5f, 7));
            return Mathf.Clamp01(1f - best / width);
        }

        /// <summary>The layer at a dungeon point: dirt, with stones showing through where cobble belongs (tops first).</summary>
        public static GroundLayer DungeonPick(Vector2 ground, float cobbleHeight) =>
            cobbleHeight > 1.05f - CobbleCover(ground) ? GroundLayer.Cobble : GroundLayer.Dirt;

        /// <summary>The layer at a dungeon point by theme (2026-10-10): the crypt is paved, with earth showing only where
        /// the stones are worn away; the caves are bare earth, a few stones here and there.</summary>
        public static GroundLayer DungeonPick(Vector2 ground, float cobbleHeight, DungeonTheme theme) =>
            theme == DungeonTheme.Crypt
                ? cobbleHeight > 0.15f + 0.4f * Smooth(ground.x / 4f, ground.y / 4f, 11) ? GroundLayer.Cobble : GroundLayer.Dirt
                : cobbleHeight > 1.25f - CobbleCover(ground) * 0.6f ? GroundLayer.Cobble : GroundLayer.Dirt;

        /// <summary>The layer at a town point: a path over all (its worn middle first), else grass tufts, else dirt.</summary>
        public static GroundLayer TownPick(Vector2 ground, float pathCover, float grassDensity, float pathWear)
        {
            if (pathCover + pathWear * 0.6f > 0.75f)
                return GroundLayer.Path;
            var grass = GrassCover(ground) * Mathf.Clamp01(1f - pathCover * 2.5f);
            return grassDensity > 1.05f - grass ? GroundLayer.Grass : GroundLayer.Dirt;
        }

        public static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return (p - (a + ab * t)).magnitude;
        }

        // Value noise: the lattice's hashed corners blended with a smoothstep.
        static float Smooth(float x, float y, int salt)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0 + salt * 1013, y0), b = Hash(x0 + 1 + salt * 1013, y0);
            float c = Hash(x0 + salt * 1013, y0 + 1), d = Hash(x0 + 1 + salt * 1013, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
