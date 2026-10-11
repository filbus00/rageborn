using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Where the town's buildings and dressing stand (the owner, 2026-10-04: "the town screen needs house assets, tents,
    /// camp fires etc."; remade dark 2026-10-11, the stairs moved into a mausoleum in a graveyard off the square). Each piece names its sprite in
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

        /// <summary>Where the stairs down stand (2026-10-11): in the mausoleum's doorway, off the square to the north.</summary>
        public static readonly Vector2Int StairsCell = new Vector2Int(16, 15);

        /// <summary>The mausoleum's footprint middle; its door faces -y, toward <see cref="StairsCell"/>.</summary>
        public static readonly Vector2Int MausoleumCell = new Vector2Int(16, 19);

        /// <summary>The cells the town's own points need: the start, the stairs, the Waystone, the portal back, the
        /// merchant and the Pet Vendor, the newcomers (<see cref="TownRules"/>), the quest people and the Vigil fire
        /// (<see cref="QuestTown"/>) and the DEV stairs.</summary>
        public static readonly Vector2Int[] KeptClear =
        {
            new Vector2Int(0, 0), StairsCell, new Vector2Int(3, -3), new Vector2Int(-2, -4),
            new Vector2Int(5, 1), new Vector2Int(-4, 1), new Vector2Int(9, 5), new Vector2Int(12, 0),
            new Vector2Int(-3, -3), new Vector2Int(1, 4), new Vector2Int(-7, 6), new Vector2Int(8, -2),
            new Vector2Int(2, 10), new Vector2Int(-6, -1), new Vector2Int(5, -6), new Vector2Int(1, -11), new Vector2Int(-3, 12),
            new Vector2Int(0, 9),
        };

        // The town remade dark (2026-10-11, the owner's concept art: "the town is way too vibrant. remake it"): soot-dark
        // houses close round a paved square, burnt ruins, muted canvas, the Pet Vendor's cages, a smith's forge, and off
        // the square to the north a fenced graveyard with the mausoleum the stairs go down in. Tall pieces stand behind
        // and beside the square, only low ones toward the camera, so nothing hides the square.
        public static readonly Piece[] Pieces =
        {
            // Behind the square.
            new Piece("house_1", -4, 17, 2.9f, 2.1f),
            new Piece("house_2", 3, 19, 1.9f, 2.4f),
            new Piece("house_4", -12, 13, 2.4f, 2.2f),
            new Piece("woodpile", -7, 13, 0.9f, 0.9f),
            // Left of it.
            new Piece("ruin_1", -18, 5, 2.6f, 2.2f),
            new Piece("tent_2", -12, 2, 1.9f, 1.5f),
            new Piece("forge", -18, -3, 1.9f, 1.3f, Glow.Fire),
            new Piece("tent_1", -16, -10, 1.9f, 1.5f),
            new Piece("market_stall", -8, -5, 1.7f, 0.9f),
            new Piece("crate_stack", -10, -8, 0.9f, 0.9f),
            // Right of it.
            new Piece("house_3", 20, 2, 3.6f, 2.2f),
            new Piece("house_1", 19, -7, 2.9f, 2.1f),
            new Piece("cage_stall", 12, -3, 2.1f, 1.2f),
            // Toward the camera: low pieces only.
            new Piece("ruin_2", 12, -15, 2.0f, 1.9f),
            new Piece("well", -2, -15, 1.0f, 1.0f),
            new Piece("cart", 6, -14, 1.6f, 0.9f),
            // The square.
            new Piece("campfire", -4, 8, 0.7f, 0.7f, Glow.Fire),
            new Piece("signpost", 6, 7, 0.2f, 0.2f),
            new Piece("lamp_post", -3, 3, 0.2f, 0.2f, Glow.Lamp),
            new Piece("lamp_post", 12, 3, 0.2f, 0.2f, Glow.Lamp),
            new Piece("lamp_post", 2, -8, 0.2f, 0.2f, Glow.Lamp),
            new Piece("lamp_post", -9, -1, 0.2f, 0.2f, Glow.Lamp),
            new Piece("dead_tree", -11, 8, 0.3f, 0.3f),
            new Piece("dead_tree", 16, -13, 0.3f, 0.3f),
            // The way to the graveyard, lit.
            new Piece("torch_post", 9, 9, 0.15f, 0.15f, Glow.Fire),
            new Piece("torch_post", 14, 11, 0.15f, 0.15f, Glow.Fire),
            new Piece("torch_post", 19, 11, 0.15f, 0.15f, Glow.Fire),
            // The graveyard: an iron railing round it, open to the south where the way comes in.
            new Piece("iron_fence_x", 12, 12, 2.0f, 0.15f),
            new Piece("iron_fence_x", 22, 12, 2.0f, 0.15f),
            new Piece("iron_fence_y", 10, 14, 0.15f, 2.0f),
            new Piece("iron_fence_y", 10, 18, 0.15f, 2.0f),
            new Piece("iron_fence_y", 10, 22, 0.15f, 2.0f),
            new Piece("iron_fence_x", 12, 26, 2.0f, 0.15f),
            new Piece("iron_fence_x", 16, 26, 2.0f, 0.15f),
            new Piece("iron_fence_x", 20, 26, 2.0f, 0.15f),
            new Piece("iron_fence_y", 24, 14, 0.15f, 2.0f),
            new Piece("iron_fence_y", 24, 18, 0.15f, 2.0f),
            new Piece("iron_fence_y", 24, 22, 0.15f, 2.0f),
            new Piece("mausoleum", MausoleumCell.x, MausoleumCell.y, 2.6f, 2.4f, Glow.Fire),
            new Piece("grave_cross", 12, 19, 0.9f, 0.4f),
            new Piece("graves", 13, 23, 1.4f, 0.5f),
            new Piece("graves", 21, 16, 1.4f, 0.5f),
            new Piece("tomb", 21, 21, 1.1f, 0.6f),
            new Piece("grave_cross", 21, 14, 0.9f, 0.4f),
            new Piece("dead_tree", 22, 24, 0.3f, 0.3f),
            new Piece("dead_tree", 12, 24, 0.3f, 0.3f),
            // The edges, dark pines.
            new Piece("pine", 0, 27, 0.6f, 0.6f),
            new Piece("pine", 28, 4, 0.6f, 0.6f),
            new Piece("pine", -24, 13, 0.6f, 0.6f),
            new Piece("pine", -27, -2, 0.6f, 0.6f),
            new Piece("pine", 28, 16, 0.6f, 0.6f),
            new Piece("pine", -9, 24, 0.6f, 0.6f),
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
