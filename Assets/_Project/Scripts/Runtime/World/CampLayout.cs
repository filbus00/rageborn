using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Wild Arrow Company's mercenary camp (the owner, 2026-11-18: "Lets revive the idea of a town. The town is the
    /// mercenary camp. The contracts are selected from a billboard. The trainers and vendors are characters in town"),
    /// a new layout in place of <see cref="TownLayout"/>'s walled town. Made bigger and loose after the owner's first
    /// look ("make it bigger, no need to have it so bunched up. make it much more spread out. add some single trees and
    /// then an impenetrable tree edge further out that does not need to be a perfect circle. add other archers training
    /// and firing at the dummies"): a wide clearing in the Thornwood with single trees standing about it, the fire at its
    /// heart, the contracts billboard, the forge, the kennel and the grave stone each in a quarter of its own, and at the
    /// left a firing range where other archers of the company train. The wood thickens toward an irregular edge and is
    /// closed there by a wall of trees. Pieces use <see cref="TownLayout.Piece"/>. Pure.
    /// </summary>
    public static class CampLayout
    {
        /// <summary>Where each point of the camp stands, in cells (the people stand just outside their pieces).</summary>
        public static readonly Vector2Int Start = new Vector2Int(0, 0);
        public static readonly Vector2Int Fire = new Vector2Int(0, 12);
        public static readonly Vector2Int Billboard = new Vector2Int(8, 9);
        public static readonly Vector2Int Yard = new Vector2Int(-14, -2);
        public static readonly Vector2Int Armoury = new Vector2Int(18, -4);
        public static readonly Vector2Int Kennel = new Vector2Int(-4, -18);
        public static readonly Vector2Int GraveStone = new Vector2Int(12, 20);

        /// <summary>An archer at the firing line, and the target it shoots at, in cells.</summary>
        public readonly struct Archer
        {
            public readonly Vector2Int Cell;
            public readonly Vector2Int Target;

            public Archer(int x, int y, int tx, int ty)
            {
                Cell = new Vector2Int(x, y);
                Target = new Vector2Int(tx, ty);
            }
        }

        /// <summary>The company's archers at the range, one at each target (the targets and dummies stand in <see cref="Pieces"/>).</summary>
        public static readonly Archer[] Archers =
        {
            new Archer(-18, -5, -26, -5), new Archer(-18, 0, -26, 0), new Archer(-18, 5, -26, 5),
            new Archer(-18, 10, -26, 10), new Archer(-18, 15, -26, 15),
        };

        /// <summary>The walks of the company's archers about the camp (ambient life, 2026-11-18): each loops through its
        /// cells, standing a while at each. They keep clear of every piece (a test walks each line).</summary>
        public static readonly Vector2Int[][] Strolls =
        {
            // About the fire, a wide round outside its benches, lamps and banners, in both senses.
            new[] { new Vector2Int(-8, 8), new Vector2Int(8, 8), new Vector2Int(8, 16), new Vector2Int(-8, 16) },
            new[] { new Vector2Int(8, 16), new Vector2Int(-8, 16), new Vector2Int(-8, 8), new Vector2Int(8, 8) },
            // From the billboard to the fire and back, reading the notices on the way.
            new[] { new Vector2Int(7, 6), new Vector2Int(2, 8) },
            // Between the forge and the cart.
            new[] { new Vector2Int(12, -8), new Vector2Int(20, -9) },
        };

        /// <summary>The camp's clear ground reaches this far (cells, the larger of the two axes).</summary>
        public const int ClearingReach = 30;

        /// <summary>The wood's irregular edge is this far from the camp's middle on average, and drifts by up to
        /// <see cref="EdgeDrift"/> either way; the thicket begins <see cref="ThicketDepth"/> cells in from it, the wall of
        /// trees (footprints touching, nothing passes) is <see cref="SolidDepth"/> deep outside it, and trees stand
        /// <see cref="OuterDepth"/> beyond that, where she never goes.</summary>
        public const float EdgeBase = 33f, EdgeDrift = 4f, ThicketDepth = 9f, SolidDepth = 4f, OuterDepth = 6f;

        /// <summary>How far from the camp's middle the wood's edge lies in a direction (radians): a ragged, drifting
        /// outline, not a circle.</summary>
        public static float EdgeAt(float angle) =>
            EdgeBase + EdgeDrift * (0.6f * Mathf.Sin(3f * angle + 1.3f) + 0.4f * Mathf.Sin(5f * angle + 0.4f));

        /// <summary>A cell's distance past the wood's edge in cells (negative inside it).</summary>
        public static float PastEdge(int x, int y) => Mathf.Sqrt(x * x + y * y) - EdgeAt(Mathf.Atan2(y, x));

        /// <summary>The cells the camp's own points need open.</summary>
        public static readonly Vector2Int[] KeptClear = { Start, Billboard, Yard, Armoury, Kennel, GraveStone };

        /// <summary>
        /// The trampled ground (bare earth the forest's grass does not cover): a round of it about the fire, a yard before
        /// each keeper and the billboard, and worn tracks joining them to the fire and the start. Cells the ground painter
        /// draws as the forest's path.
        /// </summary>
        public static System.Collections.Generic.HashSet<Vector2Int> TrampledCells()
        {
            var fire = (Vector2)Fire;
            var spots = new[] { Start, Billboard, Yard, Armoury, Kennel, GraveStone };
            var cells = new System.Collections.Generic.HashSet<Vector2Int>();
            for (var x = -34; x <= 34; x++)
                for (var y = -34; y <= 34; y++)
                {
                    var p = new Vector2(x, y);
                    var near = (p - fire).magnitude <= 6f;
                    foreach (var spot in spots)
                        if ((p - spot).magnitude <= 2.8f || DistanceToSegment(p, fire, spot) <= 1.1f)
                            near = true;
                    foreach (var archer in Archers)
                        if ((p - archer.Cell).magnitude <= 1.8f)
                            near = true;
                    foreach (var stroll in Strolls)
                        for (var i = 0; i < stroll.Length; i++)
                            if (stroll.Length > 2 || i == 0)
                                if (DistanceToSegment(p, stroll[i], stroll[(i + 1) % stroll.Length]) <= 0.9f)
                                    near = true;
                    if (near)
                        cells.Add(new Vector2Int(x, y));
                }
            return cells;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return (p - (a + ab * t)).magnitude;
        }

        public static readonly TownLayout.Piece[] Pieces = Build();

        static TownLayout.Piece[] Build()
        {
            var pieces = new System.Collections.Generic.List<TownLayout.Piece>
            {
                // The fire at the camp's heart with its benches, lamps and banners.
                new TownLayout.Piece("camp_fire", Fire.x, Fire.y, 1.0f, 0.8f, TownLayout.Glow.Fire),
                new TownLayout.Piece("camp_bench", Fire.x + 5, Fire.y, 1.3f, 0.4f),
                new TownLayout.Piece("camp_bench", Fire.x - 5, Fire.y, 1.3f, 0.4f),
                new TownLayout.Piece("camp_lamp", 4, 4, 0.2f, 0.2f, TownLayout.Glow.Lamp),
                new TownLayout.Piece("camp_lamp", -4, 4, 0.2f, 0.2f, TownLayout.Glow.Lamp),
                new TownLayout.Piece("camp_lamp", Fire.x + 9, Fire.y + 2, 0.2f, 0.2f, TownLayout.Glow.Lamp),
                new TownLayout.Piece("camp_lamp", Fire.x - 9, Fire.y + 2, 0.2f, 0.2f, TownLayout.Glow.Lamp),
                new TownLayout.Piece("camp_banner", Fire.x + 2, Fire.y + 5, 0.2f, 0.2f),
                new TownLayout.Piece("camp_banner", Fire.x - 2, Fire.y + 5, 0.2f, 0.2f),
                // The contracts billboard, behind its spot (the way in is from the front, the camera's side).
                new TownLayout.Piece("camp_billboard", Billboard.x + 3, Billboard.y + 3, 1.4f, 0.4f, TownLayout.Glow.Lamp),
                // The firing range at the left: targets and dummies in a column, a tent and crates behind the line.
                new TownLayout.Piece("camp_target", -26, -5, 0.6f, 0.5f),
                new TownLayout.Piece("camp_dummy", -26, 0, 0.5f, 0.5f),
                new TownLayout.Piece("camp_target", -26, 5, 0.6f, 0.5f),
                new TownLayout.Piece("camp_dummy", -26, 10, 0.5f, 0.5f),
                new TownLayout.Piece("camp_target", -26, 15, 0.6f, 0.5f),
                new TownLayout.Piece("camp_tent_1", -14, 20, 1.9f, 1.5f),
                new TownLayout.Piece("camp_crates", -14, 7, 0.9f, 0.7f),
                // The forge (Thorin), at the right, crates and a supply cart about it.
                new TownLayout.Piece("camp_forge", 23, -4, 1.7f, 1.3f, TownLayout.Glow.Fire),
                new TownLayout.Piece("camp_crates", 23, 1, 0.9f, 0.7f),
                new TownLayout.Piece("camp_cart", 14, -14, 1.8f, 1.0f),
                new TownLayout.Piece("camp_tent_2", 22, 12, 1.9f, 1.5f),
                // The kennel (Lyriel): a pen with its trough and the wolf, below the camp.
                new TownLayout.Piece("camp_fence_x", -10, -24, 2.0f, 0.15f),
                new TownLayout.Piece("camp_fence_x", -10, -18, 2.0f, 0.15f),
                new TownLayout.Piece("camp_fence_y", -12, -21, 0.15f, 2.0f),
                new TownLayout.Piece("camp_trough", -10, -21, 0.8f, 0.5f),
                new TownLayout.Piece("camp_wolf", -9, -22, 0.5f, 0.4f),
                new TownLayout.Piece("camp_tent_1", 6, -24, 1.9f, 1.5f),
                // The grave stone of the fallen, at the top right, with a tent behind.
                new TownLayout.Piece("camp_grave", 17, 22, 1.6f, 0.8f),
                new TownLayout.Piece("camp_tent_2", 0, 26, 1.9f, 1.5f),
            };
            var camp = pieces.Count;
            var trampled = TrampledCells();

            // Single trees standing about the clearing, well apart (a grid of six with a jitter, a coin toss each), never
            // on the camp, its spots, its archers or its worn ground.
            for (var gx = -30; gx <= 30; gx += 6)
                for (var gy = -30; gy <= 30; gy += 6)
                {
                    var h = Hash(gx, gy);
                    var x = gx + (int)(h % 3) - 1;
                    var y = gy + (int)(h / 3 % 3) - 1;
                    if (h / 9 % 100 >= 85 || PastEdge(x, y) > -ThicketDepth)
                        continue;
                    if (Overlaps(pieces, camp, x, y, 2.5f) || NearPoint(x, y, 2.5f) || trampled.Contains(new Vector2Int(x, y)))
                        continue;
                    pieces.Add(new TownLayout.Piece("forest_tree_" + (h / 2500 % 9 + 1), x, y, 0.6f, 0.6f));
                }

            // The wood: a thicket thinning out the way in, a wall of trees at the edge on every second cell (footprints
            // touching), then trees beyond it, which she never reaches.
            for (var x = -56; x <= 56; x += 2)
                for (var y = -56; y <= 56; y += 2)
                {
                    var past = PastEdge(x, y);
                    var h = Hash(x, y);
                    if (past >= 0f && past <= SolidDepth)
                    {
                        pieces.Add(new TownLayout.Piece("forest_tree_" + (h % 9 + 1), x, y, 0.99f, 0.99f));
                        continue;
                    }
                    if (past < -ThicketDepth || past > SolidDepth + OuterDepth || (x / 2 + y / 2) % 2 != 0)
                        continue;
                    // Every other grid point, kept with a chance that grows toward the edge, so the wood closes in.
                    var chance = past < 0f ? 0.1f + 0.75f * (past + ThicketDepth) / ThicketDepth : 0.7f;
                    if (h / 7 % 100 >= chance * 100f)
                        continue;
                    var bush = past < 0f && h / 101 % 3 == 0;
                    if (Overlaps(pieces, camp, x, y, (bush ? 0.4f : 0.6f) + 0.5f) || NearPoint(x, y, 1.5f))
                        continue;
                    pieces.Add(bush
                        ? new TownLayout.Piece("forest_bush_" + (h / 303 % 3 + 1), x, y, 0.4f, 0.4f)
                        : new TownLayout.Piece("forest_tree_" + (h / 909 % 9 + 1), x, y, 0.6f, 0.6f));
                }
            return pieces.ToArray();
        }

        static uint Hash(int x, int y) => (uint)(x * 73856093 ^ y * 19349663);

        // Whether a tree here would stand close to one of the camp's spots or archers, or in a line of fire.
        static bool NearPoint(int x, int y, float within)
        {
            var p = new Vector2(x, y);
            foreach (var spot in KeptClear)
                if ((p - spot).magnitude < within + 1.5f)
                    return true;
            foreach (var archer in Archers)
                if ((p - archer.Cell).magnitude < within + 1f || (p - archer.Target).magnitude < within
                    || DistanceToSegment(p, archer.Cell, archer.Target) < 1.8f)
                    return true;
            foreach (var stroll in Strolls)
                for (var i = 0; i < stroll.Length; i++)
                    if (DistanceToSegment(p, stroll[i], stroll[(i + 1) % stroll.Length]) < 2f)
                        return true;
            return false;
        }

        // Whether a tree at a cell, with this half size and a little room, would stand on one of the camp's own pieces.
        static bool Overlaps(System.Collections.Generic.List<TownLayout.Piece> pieces, int count, int x, int y, float half)
        {
            for (var i = 0; i < count; i++)
                if (Mathf.Abs(pieces[i].Cell.x - x) <= pieces[i].Half.x + half && Mathf.Abs(pieces[i].Cell.y - y) <= pieces[i].Half.y + half)
                    return true;
            return false;
        }
    }
}
