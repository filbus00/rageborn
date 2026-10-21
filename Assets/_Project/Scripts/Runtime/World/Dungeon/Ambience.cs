using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Small life in a crypt level (the owner, 2026-10-20, okayed: "dust motes, flickering candle groups, rats that
    /// scatter, bats, dripping water"): dust drifting in the view, a soft flicker over every candle, brazier and torch,
    /// rats along the walls that run from her and vanish, a bat now and then flapping across, drops falling into
    /// puddles. All drawn, none of it touches the game. Pooled; nothing is made after the level loads.
    /// </summary>
    public class Ambience : MonoBehaviour
    {
        const float PixelsPerUnit = 40f;
        const int Motes = 28, MaxRats = 10, Bats = 2, Drops = 3;

        sealed class Rat
        {
            public SpriteRenderer Renderer;
            public Vector2 Ground;
            public Vector2 Run;
            public float Timer;
            public float Fade;
            public bool Fleeing;
            public bool Gone;
        }

        sealed class Glow
        {
            public SpriteRenderer Renderer;
            public float Phase;
            public float Alpha;
            public float Scale;
        }

        DungeonLayout layout;
        Camera view;
        PlayerController player;
        readonly SpriteRenderer[] motes = new SpriteRenderer[Motes];
        readonly Vector2[] moteDrift = new Vector2[Motes];
        readonly float[] motePhase = new float[Motes];
        readonly List<Rat> rats = new List<Rat>();
        readonly List<Vector2Int> ratSpots = new List<Vector2Int>();
        readonly SpriteRenderer[] bats = new SpriteRenderer[Bats];
        readonly float[] batTime = new float[Bats];
        readonly Vector3[] batFrom = new Vector3[Bats];
        readonly Vector3[] batTo = new Vector3[Bats];
        float nextBat;
        readonly List<Vector2Int> puddles = new List<Vector2Int>();
        readonly SpriteRenderer[] drops = new SpriteRenderer[Drops];
        readonly SpriteRenderer[] splashes = new SpriteRenderer[Drops];
        readonly float[] dropTime = new float[Drops];
        readonly Vector3[] dropAt = new Vector3[Drops];
        float nextDrop;
        readonly List<Glow> glows = new List<Glow>();
        Sprite mote, ratA, ratB, batA, batB, drop, splash, glow;

        /// <summary>Starts the level's ambience (the crypt only).</summary>
        public static Ambience Begin(Transform parent, DungeonLayout layout, int seed)
        {
            var go = new GameObject("Ambience");
            go.transform.SetParent(parent, false);
            var ambience = go.AddComponent<Ambience>();
            ambience.Build(layout, seed);
            return ambience;
        }

        void Build(DungeonLayout level, int seed)
        {
            layout = level;
            var random = new System.Random(seed ^ 0x5eed);
            MakeSprites();
            for (var i = 0; i < Motes; i++)
            {
                motes[i] = NewRenderer("Mote", mote, GameSortingLayers.Effects);
                moteDrift[i] = new Vector2((float)random.NextDouble() * 0.1f - 0.05f, 0.02f + (float)random.NextDouble() * 0.04f);
                motePhase[i] = (float)random.NextDouble() * 10f;
                motes[i].enabled = false;
            }

            // Rats along the walls of the halls; puddles to drip into; a glow over everything that burns.
            var candidates = new List<Vector2Int>();
            foreach (var room in layout.Rooms)
            {
                if (room.Kind == RoomKind.Boss || room.Kind == RoomKind.Start)
                    continue;
                var r = room.Interior;
                for (var x = r.xMin; x < r.xMax; x++)
                    for (var y = r.yMin; y < r.yMax; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (layout.IsFloor(cell) && AgainstWall(cell))
                            candidates.Add(cell);
                    }
            }
            for (var i = 0; i < candidates.Count && ratSpots.Count < 60; i += Mathf.Max(1, candidates.Count / 60))
                ratSpots.Add(candidates[i]);
            for (var i = 0; i < MaxRats && ratSpots.Count > 0; i++)
            {
                var rat = new Rat { Renderer = NewRenderer("Rat", ratA, GameSortingLayers.Entities) };
                rat.Renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                Place(rat, ratSpots[random.Next(ratSpots.Count)]);
                rat.Timer = (float)random.NextDouble() * 3f;
                rats.Add(rat);
            }
            foreach (var pair in layout.Decals)
                if (pair.Value == DecalKind.Puddle)
                    puddles.Add(pair.Key);
            for (var i = 0; i < Bats; i++)
            {
                bats[i] = NewRenderer("Bat", batA, GameSortingLayers.Effects);
                bats[i].enabled = false;
            }
            for (var i = 0; i < Drops; i++)
            {
                drops[i] = NewRenderer("Drop", drop, GameSortingLayers.Effects);
                splashes[i] = NewRenderer("Splash", splash, GameSortingLayers.Decals);
                drops[i].enabled = splashes[i].enabled = false;
                dropTime[i] = -1f;
            }
            foreach (var pair in layout.Props)
            {
                if (!DungeonDressing.IsLit(pair.Value))
                    continue;
                var high = pair.Value == PropKind.Brazier || pair.Value == PropKind.Torch ? 0.75f : 0.3f;
                var g = new Glow
                {
                    Renderer = NewRenderer("Glow", glow, GameSortingLayers.Effects),
                    Phase = (float)random.NextDouble() * 20f,
                    Alpha = pair.Value == PropKind.Candles ? 0.22f : 0.3f,
                    Scale = pair.Value == PropKind.Candles ? 1.4f : 2.2f,
                };
                g.Renderer.transform.position = Elevation.World(IsoMath.CellToGround(pair.Key)) + new Vector3(0f, high, 0f);
                glows.Add(g);
            }
            nextBat = 4f + (float)random.NextDouble() * 6f;
            nextDrop = 1f;
        }

        bool AgainstWall(Vector2Int cell)
        {
            foreach (var side in RoomShape.Sides)
                if (layout.Get(cell + side) == DungeonCell.Wall && !layout.Ledges.Contains(cell + side) && !layout.Chasm.Contains(cell + side))
                    return true;
            return false;
        }

        void Place(Rat rat, Vector2Int cell)
        {
            rat.Ground = IsoMath.CellToGround(cell);
            rat.Fleeing = false;
            rat.Gone = false;
            rat.Fade = 1f;
            rat.Renderer.enabled = true;
            rat.Renderer.color = Color.white;
            rat.Renderer.transform.position = Elevation.World(rat.Ground);
        }

        void Update()
        {
            if (view == null)
                view = Camera.main;
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (view == null)
                return;
            var dt = Time.deltaTime;
            var time = Time.time;
            var centre = (Vector2)view.transform.position;
            var half = new Vector2(view.orthographicSize * view.aspect, view.orthographicSize);
            var her = player != null ? IsoMath.WorldToGround(player.transform.position) : new Vector2(1e6f, 1e6f);

            // Dust: drifting up and sideways, wrapped round the view, twinkling.
            for (var i = 0; i < Motes; i++)
            {
                var m = motes[i];
                var p = (Vector2)m.transform.position;
                if (!m.enabled)
                {
                    p = centre + new Vector2(AmbienceRules.Spread(i, 1) * half.x, AmbienceRules.Spread(i, 2) * half.y);
                    m.enabled = true;
                }
                p += (moteDrift[i] + new Vector2(Mathf.Sin(time * 0.7f + motePhase[i]) * 0.04f, 0f)) * dt;
                p = AmbienceRules.Wrap(p, centre, half);
                m.transform.position = p;
                m.color = new Color(0.95f, 0.9f, 0.78f, 0.12f + 0.2f * (0.5f + 0.5f * Mathf.Sin(time * 1.3f + motePhase[i])));
            }

            // Rats: twitch where they sit; run from her when she comes near, then are gone, and turn up again later out of
            // sight.
            foreach (var rat in rats)
            {
                rat.Timer -= dt;
                if (rat.Gone)
                {
                    if (rat.Timer <= 0f && ratSpots.Count > 0)
                    {
                        var spot = ratSpots[Random.Range(0, ratSpots.Count)];
                        if (!AmbienceRules.InView(Elevation.World(IsoMath.CellToGround(spot)), centre, half))
                            Place(rat, spot);
                        else
                            rat.Timer = 2f;
                    }
                    continue;
                }
                if (!rat.Fleeing && AmbienceRules.ShouldFlee(rat.Ground, her))
                {
                    rat.Fleeing = true;
                    rat.Run = AmbienceRules.FleeDirection(rat.Ground, her) * AmbienceRules.RatSpeed;
                    rat.Timer = AmbienceRules.FleeSeconds;
                }
                if (rat.Fleeing)
                {
                    var next = rat.Ground + rat.Run * dt;
                    if (layout.IsFloor(IsoMath.GroundToCell(next)))
                        rat.Ground = next;
                    else
                        rat.Run = new Vector2(-rat.Run.y, rat.Run.x);
                    rat.Renderer.sprite = Mathf.Repeat(time * 14f, 1f) < 0.5f ? ratA : ratB;
                    rat.Renderer.flipX = rat.Run.x < 0f;
                    if (rat.Timer <= 0f)
                    {
                        rat.Fade -= dt / 0.3f;
                        rat.Renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(rat.Fade));
                        if (rat.Fade <= 0f)
                        {
                            rat.Gone = true;
                            rat.Renderer.enabled = false;
                            rat.Timer = AmbienceRules.RatReturnSeconds;
                        }
                    }
                }
                else if (rat.Timer <= 0f)
                {
                    rat.Renderer.flipX = !rat.Renderer.flipX;
                    rat.Timer = 1.5f + Random.value * 3f;
                }
                rat.Renderer.transform.position = Elevation.World(rat.Ground);
            }

            // A bat now and then, flapping across the upper part of the view.
            nextBat -= dt;
            for (var i = 0; i < Bats; i++)
            {
                if (batTime[i] <= 0f && nextBat <= 0f)
                {
                    var fromLeft = Random.value < 0.5f;
                    var y = centre.y + half.y * Random.Range(0.1f, 0.7f);
                    batFrom[i] = new Vector3(centre.x + (fromLeft ? -1.2f : 1.2f) * half.x, y, 0f);
                    batTo[i] = new Vector3(centre.x + (fromLeft ? 1.2f : -1.2f) * half.x, y + Random.Range(-2f, 2f), 0f);
                    batTime[i] = AmbienceRules.BatSeconds;
                    bats[i].enabled = true;
                    bats[i].flipX = !fromLeft;
                    nextBat = Random.Range(AmbienceRules.MinBatGap, AmbienceRules.MaxBatGap);
                }
                if (batTime[i] <= 0f)
                    continue;
                batTime[i] -= dt;
                var t = 1f - batTime[i] / AmbienceRules.BatSeconds;
                var at = Vector3.Lerp(batFrom[i], batTo[i], t);
                at.y += Mathf.Sin(t * Mathf.PI * 3f) * 0.4f;
                bats[i].transform.position = at;
                bats[i].sprite = Mathf.Repeat(time * 9f, 1f) < 0.5f ? batA : batB;
                if (batTime[i] <= 0f)
                    bats[i].enabled = false;
            }

            // Drops falling into puddles in view.
            nextDrop -= dt;
            if (nextDrop <= 0f && puddles.Count > 0)
            {
                nextDrop = Random.Range(0.8f, 2.2f);
                var cell = puddles[Random.Range(0, puddles.Count)];
                var floor = Elevation.World(IsoMath.CellToGround(cell));
                if (AmbienceRules.InView(floor, centre, half))
                    for (var i = 0; i < Drops; i++)
                        if (dropTime[i] < 0f)
                        {
                            dropTime[i] = 0f;
                            dropAt[i] = floor;
                            drops[i].enabled = true;
                            break;
                        }
            }
            for (var i = 0; i < Drops; i++)
            {
                if (dropTime[i] < 0f)
                    continue;
                dropTime[i] += dt;
                var fall = AmbienceRules.DropHeight(dropTime[i]);
                if (fall > 0f)
                    drops[i].transform.position = dropAt[i] + new Vector3(0f, fall, 0f);
                else
                {
                    drops[i].enabled = false;
                    var since = dropTime[i] - AmbienceRules.FallSeconds;
                    splashes[i].enabled = since < 0.25f;
                    splashes[i].transform.position = dropAt[i];
                    splashes[i].transform.localScale = Vector3.one * (1f + since * 4f);
                    if (since >= 0.25f)
                        dropTime[i] = -1f;
                }
            }

            // Candles and fires flicker.
            foreach (var g in glows)
            {
                var p = g.Renderer.transform.position;
                if (!AmbienceRules.InView(p, centre, half))
                    continue;
                var f = AmbienceRules.Flicker(time, g.Phase);
                g.Renderer.color = new Color(1f, 0.78f, 0.42f, g.Alpha * f);
                g.Renderer.transform.localScale = Vector3.one * (g.Scale * (0.92f + 0.08f * f));
            }
        }

        SpriteRenderer NewRenderer(string name, Sprite sprite, string sortingLayer)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingLayerName = sortingLayer;
            return r;
        }

        // ---------- the little sprites, drawn in code at 40 pixels a unit ----------

        void MakeSprites()
        {
            var pale = new Color32(240, 232, 210, 255);
            mote = Draw(2, 2, (x, y) => pale, new Vector2(0.5f, 0.5f));
            var fur = new Color32(58, 46, 40, 255);
            var dark = new Color32(24, 18, 16, 255);
            var pink = new Color32(150, 96, 92, 255);
            // A rat side on, nose right, tail left; two frames of legs.
            string[] ratRowsA = { "....ffff..", "..fffffffe", ".lffffffff", "t.l.l..l.." };
            string[] ratRowsB = { "....ffff..", "..fffffffe", ".lffffffff", "t..l.l.l.." };
            ratA = DrawRows(ratRowsA, fur, dark, pink);
            ratB = DrawRows(ratRowsB, fur, dark, pink);
            string[] batRowsA = { "w...ww...w", "ww.wbbw.ww", ".wwwbbwww.", "...w..w..." };
            string[] batRowsB = { "....ww....", "...wbbw...", ".wwwbbwww.", "ww......ww" };
            batA = DrawRows(batRowsA, dark, dark, pink);
            batB = DrawRows(batRowsB, dark, dark, pink);
            var water = new Color32(150, 170, 186, 255);
            drop = Draw(1, 2, (x, y) => water, new Vector2(0.5f, 0f));
            splash = Draw(6, 2, (x, y) => (x == 0 || x == 5) && y == 1 || (x > 0 && x < 5 && y == 0) ? water : new Color32(0, 0, 0, 0), new Vector2(0.5f, 0.25f));
            // The glow: a soft disc (not pixel art: it is light).
            const int n = 24;
            glow = Draw(n, n, (x, y) =>
            {
                var dx = (x + 0.5f - n / 2f) / (n / 2f);
                var dy = (y + 0.5f - n / 2f) / (n / 2f);
                var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                return new Color32(255, 255, 255, (byte)(255 * a * a));
            }, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        }

        static Sprite DrawRows(string[] rows, Color32 body, Color32 dark, Color32 pink)
        {
            var h = rows.Length;
            var w = rows[0].Length;
            return Draw(w, h, (x, y) =>
            {
                var c = rows[h - 1 - y][x];
                return c == 'f' || c == 'w' ? body : c == 'l' || c == 'b' ? dark : c == 'e' ? pink : c == 't' ? pink : new Color32(0, 0, 0, 0);
            }, new Vector2(0.5f, 0f));
        }

        static Sprite Draw(int w, int h, System.Func<int, int, Color32> paint, Vector2 pivot, FilterMode filter = FilterMode.Point)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp, name = "Ambience" };
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    pixels[x + y * w] = paint(x, y);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable (CLAUDE.md, pixel art).
            return Sprite.Create(texture, new Rect(0, 0, w, h), pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
        }
    }

    /// <summary>The ambience's rules (pure, tested).</summary>
    public static class AmbienceRules
    {
        /// <summary>A rat runs when she comes this close (ground units), this fast, for this long, and comes back (out of
        /// sight) after this long.</summary>
        public const float FleeDistance = 3.2f, RatSpeed = 4.5f, FleeSeconds = 1.1f, RatReturnSeconds = 25f;

        /// <summary>A bat crosses the view in this long; the next comes after a gap between these.</summary>
        public const float BatSeconds = 2.6f, MinBatGap = 7f, MaxBatGap = 16f;

        /// <summary>A drop falls from this high (world units) in this long.</summary>
        public const float DropStart = 1.6f, FallSeconds = 0.45f;

        public static bool ShouldFlee(Vector2 rat, Vector2 her) => (rat - her).sqrMagnitude < FleeDistance * FleeDistance;

        /// <summary>Away from her; straight down the screen if she stands right on it.</summary>
        public static Vector2 FleeDirection(Vector2 rat, Vector2 her)
        {
            var away = rat - her;
            return away.sqrMagnitude > 1e-6f ? away.normalized : Vector2.down;
        }

        /// <summary>A drop's height over the floor after this long falling (accelerating); 0 or less once it lands.</summary>
        public static float DropHeight(float t) => DropStart * (1f - (t / FallSeconds) * (t / FallSeconds));

        /// <summary>A point kept inside the view: past one edge it comes back at the other.</summary>
        public static Vector2 Wrap(Vector2 p, Vector2 centre, Vector2 half)
        {
            var d = p - centre;
            if (d.x > half.x)
                d.x -= 2f * half.x;
            else if (d.x < -half.x)
                d.x += 2f * half.x;
            if (d.y > half.y)
                d.y -= 2f * half.y;
            else if (d.y < -half.y)
                d.y += 2f * half.y;
            return centre + d;
        }

        public static bool InView(Vector2 p, Vector2 centre, Vector2 half) =>
            Mathf.Abs(p.x - centre.x) <= half.x + 1f && Mathf.Abs(p.y - centre.y) <= half.y + 1f;

        /// <summary>A flame's brightness, 0.55 to 1, wavering at a few rates at once.</summary>
        public static float Flicker(float time, float phase)
        {
            var w = Mathf.Sin(time * 7.3f + phase) * 0.5f + Mathf.Sin(time * 13.1f + phase * 1.7f) * 0.3f + Mathf.Sin(time * 2.1f + phase * 0.3f) * 0.2f;
            return 0.775f + 0.225f * w;
        }

        /// <summary>A fixed spread in -1..1 for the n-th thing on axis k (where the dust starts).</summary>
        public static float Spread(int n, int k)
        {
            var h = Mathf.Sin(n * 12.9898f + k * 78.233f) * 43758.5453f;
            return (h - Mathf.Floor(h)) * 2f - 1f;
        }
    }
}
