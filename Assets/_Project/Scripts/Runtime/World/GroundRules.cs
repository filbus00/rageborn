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
        /// <summary>Standing water in the town's mud and between its cobbles (2026-10-11), drawn from the dirt, dark.</summary>
        Puddle,
    }

    /// <summary>
    /// What the ground is made of, point by point, in ground space (the owner, 2026-10-04: "Make the town just dirt with
    /// some grassy spots and some paths" (the town remade 2026-10-11: paved square and roads, mud, puddles). Make the dungeon mostly dirt and some cobble stones showing through. Do not
    /// make each room in the dungeon have its own floor texture ... Take inspiration from the first act in Diablo 2").
    /// Broad smooth noise says how much of a layer belongs at a point; the layer's own texture strength (a stone's
    /// height, a grass tuft's density, a path's wear) decides pixel by pixel, so stone tops show through the dirt first
    /// and grass frays into tufts at its edges. The dungeon ignores room styles: one ground runs through every room.
    /// Pure.
    /// </summary>
    public static class GroundRules
    {
        /// <summary>The town's paved roads, as pairs of cells (start, end) (the walled town, 2026-10-12): from the square
        /// through the back gate to the mausoleum's door (<see cref="TownLayout.StairsCell"/>), and from the square out
        /// of the front gate. The square itself is paved all over (<see cref="SquareCover"/>).</summary>
        public static readonly Vector2Int[] TownPaths =
        {
            new Vector2Int(0, 0), new Vector2Int(22, 4),
            new Vector2Int(22, 4), new Vector2Int(31, 3),
            new Vector2Int(31, 3), new Vector2Int(31, 6),
            new Vector2Int(0, 0), new Vector2Int(-22, -4),
            new Vector2Int(-22, -4), new Vector2Int(-46, -4),
        };

        /// <summary>How far the paved square reaches from the town's middle, ground units, before its ragged edge.</summary>
        public const float SquareRadius = 9.5f;

        /// <summary>How much of the paved square is at a point, 1 inside, 0 beyond its ragged edge.</summary>
        public static float SquareCover(Vector2 ground)
        {
            var edge = SquareRadius * (0.85f + 0.3f * Smooth(ground.x / 3f, ground.y / 3f, 14));
            return Mathf.Clamp01((edge - ground.magnitude) / 1.5f);
        }

        /// <summary>A road's half width in ground units, before its wobble.</summary>
        public const float PathHalfWidth = 1.3f;

        /// <summary>How much cobble belongs at a point of the dungeon, 0 to 1: mostly none, patches here and there.</summary>
        public static float CobbleCover(Vector2 ground)
        {
            var n = Smooth(ground.x / 5f, ground.y / 5f, 3) * 0.7f + Smooth(ground.x / 1.7f, ground.y / 1.7f, 4) * 0.3f;
            return Mathf.Clamp01((n - 0.52f) * 2.6f);
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
        /// the stones are worn away; the burnt library paved under ash; the caves are bare earth, a few stones here and
        /// there.</summary>
        public static GroundLayer DungeonPick(Vector2 ground, float cobbleHeight, DungeonTheme theme) =>
            theme == DungeonTheme.Prison
                // The prison (2026-10-12): rough flagstones, more worn through to the earth than the crypt's.
                ? cobbleHeight > 0.25f + 0.45f * Smooth(ground.x / 3.5f, ground.y / 3.5f, 19) ? GroundLayer.Cobble : GroundLayer.Dirt
                : theme == DungeonTheme.Crypt
                ? cobbleHeight > 0.15f + 0.4f * Smooth(ground.x / 4f, ground.y / 4f, 11) ? GroundLayer.Cobble : GroundLayer.Dirt
                // The burnt library (2026-10-11): flagstones under drifts of ash (the dirt, painted grey by the painter).
                : theme == DungeonTheme.Library
                    ? cobbleHeight > 0.05f + 1.1f * Smooth(ground.x / 3f, ground.y / 3f, 16) ? GroundLayer.Cobble : GroundLayer.Dirt
                    : cobbleHeight > 1.25f - CobbleCover(ground) * 0.6f ? GroundLayer.Cobble : GroundLayer.Dirt;

        /// <summary>
        /// The layer at a town point (remade dark 2026-10-11, the owner's concept art: wet cobbles and mud): cobbles where
        /// the square or a road is paved, their tops first, mud between and beyond, puddles where the ground dips
        /// (<paramref name="cobbleHeight"/> is the cobble texture's stone height there), and patches of dark grass on the
        /// unpaved ground (2026-10-12, the town concept art).
        /// </summary>
        public static GroundLayer TownPick(Vector2 ground, float roadCover, float cobbleHeight)
        {
            var paved = Mathf.Max(SquareCover(ground), roadCover);
            var dip = Smooth(ground.x / 2.2f, ground.y / 2.2f, 13) * 0.75f + Smooth(ground.x / 0.9f, ground.y / 0.9f, 15) * 0.25f;
            if (paved > 0f && cobbleHeight > 1.0f - paved * 0.85f)
                return dip > 0.7f && cobbleHeight < 0.35f ? GroundLayer.Puddle : GroundLayer.Cobble;
            if (dip > 0.66f)
                return GroundLayer.Puddle;
            var grass = Smooth(ground.x / 3.5f, ground.y / 3.5f, 17) * 0.8f + Smooth(ground.x / 1.1f, ground.y / 1.1f, 18) * 0.2f;
            return paved <= 0f && grass > 0.6f ? GroundLayer.Grass : GroundLayer.Dirt;
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
