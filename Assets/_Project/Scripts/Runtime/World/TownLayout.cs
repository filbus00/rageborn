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

        /// <summary>Where the stairs down stand: in the mausoleum's doorway, in the graveyard out of the town's back gate
        /// behind the church (2026-10-12; in a graveyard off the square from 2026-10-11).</summary>
        public static readonly Vector2Int StairsCell = new Vector2Int(31, 6);

        /// <summary>The mausoleum's footprint middle; its door faces -y, toward <see cref="StairsCell"/>.</summary>
        public static readonly Vector2Int MausoleumCell = new Vector2Int(31, 10);

        /// <summary>The town wall's corners are at plus and minus this, in cells (the courtyard inside is 40 across).</summary>
        public const int WallReach = 22;

        /// <summary>The front gate (the bottom left of the screen, the road out) and the back gate (the top right,
        /// behind the church, to the graveyard).</summary>
        public static readonly Vector2Int FrontGate = new Vector2Int(-WallReach, -4);
        public static readonly Vector2Int BackGate = new Vector2Int(WallReach, 4);

        /// <summary>The cells the town's own points need: the start, the stairs, the Waystone, the portal back, the
        /// merchant and the Pet Vendor, the newcomers (<see cref="TownRules"/>), the quest people and the Vigil fire
        /// (<see cref="QuestTown"/>) and the DEV stairs.</summary>
        public static readonly Vector2Int[] KeptClear =
        {
            new Vector2Int(0, 0), StairsCell, new Vector2Int(8, -12), new Vector2Int(5, -15),
            new Vector2Int(-6, 12), new Vector2Int(-7, 1), new Vector2Int(-13, -8), new Vector2Int(-17, -8),
            new Vector2Int(1, 10), new Vector2Int(8, 12), new Vector2Int(2, -8), new Vector2Int(-12, -3),
            new Vector2Int(8, 4), new Vector2Int(-10, 10), new Vector2Int(6, -8), new Vector2Int(-18, -3), new Vector2Int(8, 11),
            new Vector2Int(0, 3),
        };

        /// <summary>
        /// The town after the owner's concept art (2026-10-12, "Rebuild the town in accordance to the concept art"; walled
        /// from concept 2 the same day): a stone wall 44 cells square with corner towers; the main gate at the bottom left
        /// of the screen (-x) under red banners, the back gate at the top right (+x) behind the church, out to the
        /// graveyard and its mausoleum. Inside, a cobbled plaza round the Vigil fire ringed with stalls (the merchant's
        /// goods, the gambler's dice, Pellam's lamps); the church at the top right (the healer and Sister Ivy at its
        /// door), the stables at the top left (the Pet Vendor) with a paddock, the smithy at the left with the trainer's
        /// yard, the stash keeper's hall and vault at the top, the well at the right, the Waystone's glowing dais at the
        /// bottom right, houses at the bottom; dark forest all round outside.
        /// </summary>
        public static readonly Piece[] Pieces = Build();

        static Piece[] Build()
        {
            var pieces = new System.Collections.Generic.List<Piece>
            {
                // The church, its door (-x) toward the plaza; the healer's herb stall and a house beside it.
                new Piece("church", 14, 8, 3.7f, 2.3f, Glow.Lamp),
                new Piece("shop_herbs", 12, 13, 1.6f, 1.0f),
                new Piece("house_2", 18, 15, 1.9f, 2.4f),
                // The stables (the Pet Vendor) and their paddock.
                new Piece("stables", -6, 16, 3.3f, 1.8f),
                new Piece("fence_x", -12, 12, 2.0f, 0.15f),
                new Piece("fence_x", -12, 20, 2.0f, 0.15f),
                new Piece("fence_y", -14, 14, 0.15f, 2.0f),
                new Piece("fence_y", -14, 18, 0.15f, 2.0f),
                new Piece("trough", -12, 16, 0.8f, 0.5f),
                // The smithy, its forge lit, and the trainer's yard before it.
                new Piece("smithy", -15, 8, 2.5f, 2.6f, Glow.Fire),
                new Piece("training_yard", -15, 1, 1.9f, 1.5f),
                // The stash keeper: the long hall, the vault before it.
                new Piece("house_3", 4, 17, 3.6f, 2.2f),
                new Piece("vault", 4, 13, 1.3f, 0.9f),
                // The stash chest (the owner, 2026-10-17: "Render a nice chest in town"), beside Elara.
                new Piece("stash_chest", 1, 13, 0.8f, 0.6f),
                // The plaza's stalls: the merchant's goods, the gambler's dice (his tent behind), Pellam's lamps.
                new Piece("shop_goods", -7, 4, 1.6f, 1.0f),
                new Piece("shop_dice", -4, -6, 1.6f, 1.2f),
                new Piece("tent_2", -5, -13, 1.9f, 1.5f),
                new Piece("shop_lamps", 6, -5, 1.6f, 1.0f, Glow.Lamp),
                new Piece("lamp_post", -4, 7, 0.2f, 0.2f, Glow.Lamp),
                new Piece("lamp_post", 5, 8, 0.2f, 0.2f, Glow.Lamp),
                new Piece("lamp_post", -5, -4, 0.2f, 0.2f, Glow.Lamp),
                new Piece("lamp_post", 4, -4, 0.2f, 0.2f, Glow.Lamp),
                // The well at the right, houses at the bottom, a cart and crates by the gate, its banners.
                new Piece("well", 14, -6, 1.0f, 1.0f),
                new Piece("house_4", 16, -15, 2.4f, 2.2f),
                new Piece("house_1", -12, -14, 2.9f, 2.1f),
                new Piece("cart", -16, -18, 1.6f, 0.9f),
                new Piece("crate_stack", -12, -18, 0.9f, 0.9f),
                new Piece("banner", -20, -1, 0.2f, 0.2f),
                new Piece("banner", -20, -7, 0.2f, 0.2f),
                new Piece("pine", 18, -19, 0.6f, 0.6f),
                new Piece("pine", -18, 18, 0.6f, 0.6f),
                // The way to the back gate and beyond, lit.
                new Piece("torch_post", 20, 2, 0.15f, 0.15f, Glow.Fire),
                new Piece("torch_post", 20, 6, 0.15f, 0.15f, Glow.Fire),
                new Piece("torch_post", 24, 2, 0.15f, 0.15f, Glow.Fire),
                new Piece("torch_post", 24, 6, 0.15f, 0.15f, Glow.Fire),
                // The graveyard outside the back gate: an iron railing open toward the gate, graves, the mausoleum.
                new Piece("iron_fence_y", 25, 1, 0.15f, 2.0f),
                new Piece("iron_fence_y", 25, 9, 0.15f, 2.0f),
                new Piece("iron_fence_y", 25, 13, 0.15f, 2.0f),
                new Piece("iron_fence_y", 37, 1, 0.15f, 2.0f),
                new Piece("iron_fence_y", 37, 5, 0.15f, 2.0f),
                new Piece("iron_fence_y", 37, 9, 0.15f, 2.0f),
                new Piece("iron_fence_y", 37, 13, 0.15f, 2.0f),
                new Piece("iron_fence_x", 27, -1, 2.0f, 0.15f),
                new Piece("iron_fence_x", 31, -1, 2.0f, 0.15f),
                new Piece("iron_fence_x", 35, -1, 2.0f, 0.15f),
                new Piece("iron_fence_x", 27, 15, 2.0f, 0.15f),
                new Piece("iron_fence_x", 31, 15, 2.0f, 0.15f),
                new Piece("iron_fence_x", 35, 15, 2.0f, 0.15f),
                new Piece("mausoleum", MausoleumCell.x, MausoleumCell.y, 2.6f, 2.4f, Glow.Fire),
                new Piece("grave_cross", 27, 12, 0.9f, 0.4f),
                new Piece("graves", 35, 13, 1.4f, 0.5f),
                new Piece("tomb", 35, 2, 1.1f, 0.6f),
                new Piece("grave_cross", 35, 7, 0.9f, 0.4f),
                new Piece("graves", 28, 1, 1.4f, 0.5f),
                new Piece("dead_tree", 35, 11, 0.3f, 0.3f),
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
                    var graveyard = cx >= WallReach + 1 && cx <= 40 && cy >= -4 && cy <= 18;
                    var road = Mathf.Abs(cy - FrontGate.y) <= 4 && cx < -WallReach;
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
