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

        /// <summary>Wall parts meet at their ends and corners (the test lets them).</summary>
        public static bool IsWallPart(Piece piece) =>
            piece.Name.Length == 0 || piece.Name.StartsWith("town_") || piece.Name.Contains("fence");

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

        /// <summary>Where the stairs down stand: in the mausoleum's doorway, out of the town's back gate (2026-10-12; in
        /// a graveyard off the square from 2026-10-11).</summary>
        public static readonly Vector2Int StairsCell = new Vector2Int(8, 29);

        /// <summary>The mausoleum's footprint middle; its door faces -y, toward <see cref="StairsCell"/>.</summary>
        public static readonly Vector2Int MausoleumCell = new Vector2Int(8, 33);

        /// <summary>The town wall's corners are at plus and minus this, in cells (the courtyard inside is 40 across).</summary>
        public const int WallReach = 22;

        /// <summary>The front gate (toward the camera, the road out) and the back gate (to the graveyard).</summary>
        public static readonly Vector2Int FrontGate = new Vector2Int(0, -WallReach);
        public static readonly Vector2Int BackGate = new Vector2Int(8, WallReach);

        /// <summary>The cells the town's own points need: the start, the stairs, the Waystone, the portal back, the
        /// merchant and the Pet Vendor, the newcomers (<see cref="TownRules"/>), the quest people and the Vigil fire
        /// (<see cref="QuestTown"/>) and the DEV stairs.</summary>
        public static readonly Vector2Int[] KeptClear =
        {
            new Vector2Int(0, 0), StairsCell, new Vector2Int(-5, -15), new Vector2Int(-9, -17),
            new Vector2Int(-12, 0), new Vector2Int(-12, 9), new Vector2Int(-5, -19), new Vector2Int(6, -19),
            new Vector2Int(-2, 9), new Vector2Int(13, 7), new Vector2Int(10, -2), new Vector2Int(9, -11),
            new Vector2Int(3, 5), new Vector2Int(-9, 9), new Vector2Int(-13, -10), new Vector2Int(5, -16), new Vector2Int(16, 7),
            new Vector2Int(0, 3),
        };

        /// <summary>
        /// The walled town (2026-10-12, the owner's concept 2: "Make a wall around the town, space the NPCs further, they
        /// should all have their own areas/shops in town"): a stone wall 44 cells square with corner towers, the main
        /// gate in front and a back gate to the graveyard and its mausoleum outside; inside, the Vigil fire in the
        /// middle of the courtyard and a plot for each of the town's people round its edge, a building and a stall:
        /// the merchant and his apprentice, the stash keeper's vault, the healer (and Sister Ivy), the gambler's
        /// table, the trainer's yard, Pellam's lamps, the Pet Vendor's cages; dark forest all round outside.
        /// </summary>
        public static readonly Piece[] Pieces = Build();

        static Piece[] Build()
        {
            var pieces = new System.Collections.Generic.List<Piece>
            {
                // The merchant: his house, his stall of goods.
                new Piece("house_1", -12, 17, 2.9f, 2.1f),
                new Piece("shop_goods", -12, 13, 1.6f, 1.0f),
                // The stash keeper: the long hall, the vault before it.
                new Piece("house_3", -2, 17, 3.6f, 2.2f),
                new Piece("vault", -2, 13, 1.3f, 0.9f),
                // The healer: her house and herb stall (Sister Ivy beside her).
                new Piece("house_2", 17, 15, 1.9f, 2.4f),
                new Piece("shop_herbs", 16, 10, 1.6f, 1.0f),
                // The gambler: his tent and dice table.
                new Piece("tent_2", 17, -2, 1.9f, 1.5f),
                new Piece("shop_dice", 14, -6, 1.6f, 1.2f),
                // The trainer: the yard and his house.
                new Piece("training_yard", 13, -14, 1.9f, 1.5f),
                new Piece("house_4", 18, -15, 2.4f, 2.2f),
                // Pellam the lampwright: a narrow house and his lamp stall.
                new Piece("house_2", -18, -14, 1.9f, 2.4f),
                new Piece("shop_lamps", -13, -14, 1.6f, 1.0f, Glow.Lamp),
                // The Pet Vendor: her cages, and the smith's cold forge beside them.
                new Piece("cage_stall", -16, 1, 2.1f, 1.2f),
                new Piece("tent_1", -17, -6, 1.9f, 1.5f),
                new Piece("forge", -17, 7, 1.9f, 1.3f, Glow.Fire),
                // The courtyard: lamps round the fire, a well, crates, a ruin in the corner.
                new Piece("lamp_post", -5, 5, 0.2f, 0.2f, Glow.Lamp),
                new Piece("lamp_post", 6, 6, 0.2f, 0.2f, Glow.Lamp),
                new Piece("lamp_post", -5, -6, 0.2f, 0.2f, Glow.Lamp),
                new Piece("lamp_post", 6, -6, 0.2f, 0.2f, Glow.Lamp),
                new Piece("well", 4, -10, 1.0f, 1.0f),
                new Piece("crate_stack", -8, 15, 0.9f, 0.9f),
                new Piece("cart", -15, -19, 1.6f, 0.9f),
                new Piece("ruin_2", -18, 18, 2.0f, 1.9f),
                new Piece("signpost", 3, -19, 0.2f, 0.2f),
                // The way to the back gate, lit.
                new Piece("torch_post", 6, 19, 0.15f, 0.15f, Glow.Fire),
                new Piece("torch_post", 10, 19, 0.15f, 0.15f, Glow.Fire),
                new Piece("torch_post", 6, 25, 0.15f, 0.15f, Glow.Fire),
                new Piece("torch_post", 10, 25, 0.15f, 0.15f, Glow.Fire),
                // The graveyard outside the back gate: an iron railing open toward the gate, graves, the mausoleum.
                new Piece("iron_fence_x", 3, 27, 2.0f, 0.15f),
                new Piece("iron_fence_x", 13, 27, 2.0f, 0.15f),
                new Piece("iron_fence_y", 1, 29, 0.15f, 2.0f),
                new Piece("iron_fence_y", 1, 33, 0.15f, 2.0f),
                new Piece("iron_fence_y", 1, 37, 0.15f, 2.0f),
                new Piece("iron_fence_x", 3, 39, 2.0f, 0.15f),
                new Piece("iron_fence_x", 7, 39, 2.0f, 0.15f),
                new Piece("iron_fence_x", 11, 39, 2.0f, 0.15f),
                new Piece("iron_fence_y", 15, 29, 0.15f, 2.0f),
                new Piece("iron_fence_y", 15, 33, 0.15f, 2.0f),
                new Piece("iron_fence_y", 15, 37, 0.15f, 2.0f),
                new Piece("mausoleum", MausoleumCell.x, MausoleumCell.y, 2.6f, 2.4f, Glow.Fire),
                new Piece("grave_cross", 3, 31, 0.9f, 0.4f),
                new Piece("graves", 4, 36, 1.4f, 0.5f),
                new Piece("tomb", 13, 36, 1.1f, 0.6f),
                new Piece("grave_cross", 13, 31, 0.9f, 0.4f),
                new Piece("dead_tree", 13, 33, 0.3f, 0.3f),
            };

            // The wall: runs of four cells between corner towers, a gatehouse in place of a run at each gate. A gate's
            // towers block; its arch is open (pieces with no name are footprints only).
            for (var t = -WallReach + 2; t <= WallReach - 2; t += 4)
                foreach (var (cell, alongX) in new[]
                         {
                             (new Vector2Int(t, -WallReach), true), (new Vector2Int(t, WallReach), true),
                             (new Vector2Int(-WallReach, t), false), (new Vector2Int(WallReach, t), false),
                         })
                {
                    if (cell == FrontGate || cell == BackGate)
                    {
                        pieces.Add(new Piece(alongX ? "town_gate_x" : "town_gate_y", cell.x, cell.y, 0f, 0f));
                        foreach (var side in new[] { -1, 1 })
                            pieces.Add(alongX
                                ? new Piece("", cell.x + side * 2, cell.y, 0.8f, 0.6f)
                                : new Piece("", cell.x, cell.y + side * 2, 0.6f, 0.8f));
                        continue;
                    }
                    pieces.Add(alongX ? new Piece("town_wall_x", cell.x, cell.y, 2.0f, 0.35f) : new Piece("town_wall_y", cell.x, cell.y, 0.35f, 2.0f));
                }
            foreach (var x in new[] { -WallReach, WallReach })
                foreach (var y in new[] { -WallReach, WallReach })
                    pieces.Add(new Piece("town_tower", x, y, 1.0f, 1.0f));

            // The forest outside: dark pines and dead trees, scattered by a hash, clear of the graveyard and the road.
            for (var x = -44; x <= 44; x += 5)
                for (var y = -44; y <= 44; y += 5)
                {
                    var h = (uint)(x * 73856093 ^ y * 19349663);
                    var cx = x + (int)(h % 3) - 1;
                    var cy = y + (int)(h / 3 % 3) - 1;
                    var outside = Mathf.Max(Mathf.Abs(cx), Mathf.Abs(cy)) >= WallReach + 4;
                    var graveyard = cx >= -2 && cx <= 18 && cy >= 23 && cy <= 43;
                    var road = Mathf.Abs(cx) <= 4 && cy < -WallReach;
                    if (!outside || graveyard || road || h / 9 % 4 == 0)
                        continue;
                    pieces.Add(h / 36 % 3 == 0 ? new Piece("dead_tree", cx, cy, 0.3f, 0.3f) : new Piece("pine", cx, cy, 0.6f, 0.6f));
                }
            return pieces.ToArray();
        }

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
