using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Where the town's buildings and dressing stand (the owner, 2026-10-04: "the town screen needs house assets, tents,
    /// camp fires etc."): houses around the square, the merchant's tent and stall beside him, a pen beside the Pet
    /// Vendor, a campfire, lamp posts and banners by the stairs, trees at the edges. Each piece names its sprite in
    /// <see cref="WorldArt"/>, its footprint's middle cell and its half size in cells along the tilemap's axes (zero
    /// for pieces that do not block), and whether it gives light. The open cells the town's own points need are in
    /// <see cref="KeptClear"/>; a test checks nothing stands on them or on each other. Pure.
    /// </summary>
    public static class TownLayout
    {
        public enum Glow
        {
            None,
            Fire,
            Lamp,
        }

        public readonly struct Piece
        {
            public readonly string Name;
            public readonly Vector2Int Cell;
            public readonly Vector2 Half;
            public readonly Glow Light;

            public Piece(string name, int x, int y, float halfX, float halfY, Glow light = Glow.None)
            {
                Name = name;
                Cell = new Vector2Int(x, y);
                Half = new Vector2(halfX, halfY);
                Light = light;
            }
        }

        /// <summary>The start, the stairs, the Waystone, the portal back, the two NPCs and the DEV stairs.</summary>
        public static readonly Vector2Int[] KeptClear =
        {
            new Vector2Int(0, 0), new Vector2Int(5, 5), new Vector2Int(3, -3), new Vector2Int(-2, -4),
            new Vector2Int(5, 1), new Vector2Int(-4, 1), new Vector2Int(9, 1), new Vector2Int(9, 5),
        };

        public static readonly Piece[] Pieces =
        {
            // Houses around the square, and a long hall at the bottom left.
            new Piece("house_1", -2, 11, 3.0f, 2.2f),
            new Piece("house_2", 13, 6, 2.3f, 2.6f),
            new Piece("house_3", -18, -8, 3.6f, 2.3f),
            new Piece("woodpile", 2, 9, 0.9f, 0.9f),
            new Piece("tent_1", 6, 12, 1.9f, 1.5f),
            // The merchant's tent, stall and stores.
            new Piece("tent_2", -8, 2, 1.9f, 1.5f),
            new Piece("market_stall", -6, -3, 1.7f, 0.9f),
            new Piece("crate_stack", -5, -6, 0.9f, 0.9f),
            // The Pet Vendor's pen.
            new Piece("fence_x", 10, -5, 2.0f, 0.15f),
            new Piece("fence_y", 8, -3, 0.15f, 2.0f),
            new Piece("fence_y", 12, -3, 0.15f, 2.0f),
            new Piece("trough", 10, -2, 1.0f, 0.4f),
            new Piece("hay_bale", 11, -4, 0.6f, 0.4f),
            // The square.
            new Piece("campfire", -3, 6, 0.7f, 0.7f, Glow.Fire),
            new Piece("well", -1, -8, 1.0f, 1.0f),
            new Piece("cart", 4, -8, 1.6f, 0.9f),
            new Piece("signpost", 1, 3, 0.2f, 0.2f),
            new Piece("lamp_post", -2, 2, 0.2f, 0.2f, Glow.Lamp),
            new Piece("lamp_post", 7, 3, 0.2f, 0.2f, Glow.Lamp),
            new Piece("lamp_post", 1, -6, 0.2f, 0.2f, Glow.Lamp),
            new Piece("banner", 4, 7, 0.15f, 0.15f),
            new Piece("banner", 7, 4, 0.15f, 0.15f),
            // The edges.
            new Piece("dead_tree", -7, 6, 0.3f, 0.3f),
            new Piece("dead_tree", 10, -10, 0.3f, 0.3f),
            new Piece("graves", -10, 9, 1.4f, 0.5f),
            new Piece("pine", 0, 17, 0.6f, 0.6f),
            new Piece("pine", 17, -2, 0.6f, 0.6f),
            new Piece("pine", -15, 8, 0.6f, 0.6f),
            new Piece("pine", 8, -15, 0.6f, 0.6f),
            new Piece("pine", -11, -12, 0.6f, 0.6f),
            new Piece("pine", 12, 15, 0.6f, 0.6f),
            new Piece("pine", 18, 8, 0.6f, 0.6f),
            new Piece("pine", -4, -16, 0.6f, 0.6f),
        };

        /// <summary>
        /// A footprint's corners as world offsets from its middle: the rectangle of half size (cells) along the
        /// tilemap's axes, which the isometric view shows as a diamond-like parallelogram.
        /// </summary>
        public static Vector2[] Footprint(Vector2 half)
        {
            var corners = new Vector2[4];
            var signs = new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
            for (var i = 0; i < 4; i++)
            {
                float u = signs[i].x * half.x, v = signs[i].y * half.y;
                corners[i] = IsoMath.GroundToWorld(new Vector2((u - v) * 0.5f, (u + v) * 0.5f));
            }
            return corners;
        }

        /// <summary>Whether a cell lies on a piece's footprint, grown by a margin in cells.</summary>
        public static bool Covers(Piece piece, Vector2 cell, float margin = 0f)
        {
            var d = cell - piece.Cell;
            return Mathf.Abs(d.x) <= piece.Half.x + margin && Mathf.Abs(d.y) <= piece.Half.y + margin;
        }
    }
}
