using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Where the next shot or lunge comes from, shown around her rather than along the ground (the owner, 2026-10-19:
    /// "instead of making the attack telegraphs be in the form of lines emenating from the enemies. Make the show up as
    /// warning symbols around the character. So if an attack is about to come from the to left, a warning symbol (color
    /// coded according to attack) show up top left above the character"). Each wind-up aimed at her (an archer's shot, a
    /// charger's lunge, a thrown special, and as well as their lanes the specials that strike along a lane) puts a
    /// round warning on a ring round her chest, on the side of its enemy as seen on screen, in its attack's colour,
    /// throbbing faster as the attack nears. It goes when the attack is let loose, is cancelled or its enemy dies.
    /// Drawn on an overlay like <see cref="AimCone"/>, so walls and bodies never hide it. Owned by
    /// <see cref="EnemyManager"/>; pooled, so a fight makes no objects after its first warnings.
    /// </summary>
    public class IncomingWarnings : MonoBehaviour
    {
        /// <summary>The symbol's outline by kind of attack (the owner, 2026-10-20: "Make pools show as circular warnings,
        /// make projectiles show as triangular warnings"): pools round, projectiles (arrows, bolts, throws) a triangle,
        /// lunges and lanes a diamond.</summary>
        public enum Shape
        {
            Round,
            Triangle,
            Diamond,
        }

        /// <summary>The ring's radius and the symbol's size, in world units; the ring's centre this far above her feet.
        /// Every warning sits on this one ring, close round her (the owner, 2026-10-20: "make the warnings cluster in a
        /// circle around the player character"; was 2.1, crowded ones stacking outward).</summary>
        public const float Radius = 1.5f, Size = 0.84f, ChestHeight = 0.6f;

        /// <summary>Colours by attack: arrows and bolts red, lunges amber; a special shows its own colour.</summary>
        public static readonly Color ShotColor = new Color(1f, 0.25f, 0.18f, 1f);
        public static readonly Color LungeColor = new Color(1f, 0.68f, 0.18f, 1f);

        sealed class Warning
        {
            public Image Image;
            public EnemyController Source;
            public Vector2 From;
            public float Seconds;
            public float Elapsed;
            public bool Active;
            // A pool's warning shows only while she is within this of its edge (0: always).
            public float Near;
            public float PoolRadius;
        }

        /// <summary>A pool (a ground hazard) is near, shown while she is within this many units of its edge.</summary>
        public const float PoolNear = 3f;

        /// <summary>Shows a warning for a pool about to hurt (the owner, 2026-10-19: "Add warnings for pools also"),
        /// on its side of her, while she is near it.</summary>
        public void WarnPool(Vector2 center, float radius, float seconds, Color color)
        {
            Warn(null, center, seconds, color, Shape.Round);
            var w = last;
            w.Near = PoolNear;
            w.PoolRadius = radius;
        }

        Warning last;

        readonly List<Warning> warnings = new List<Warning>();
        readonly List<float> placedAngles = new List<float>(16);
        RectTransform root;
        Canvas canvas;
        PlayerController player;
        static readonly Sprite[] symbols = new Sprite[3];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => System.Array.Clear(symbols, 0, symbols.Length);

        void Awake()
        {
            var canvasObject = new GameObject("Warning Canvas", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9; // Over the aim arc (8), under the HUD (10).
            root = (RectTransform)new GameObject("Warnings", typeof(RectTransform)).transform;
            root.SetParent(canvasObject.transform, false);
        }

        void OnDestroy()
        {
            if (canvas != null)
                Destroy(canvas.gameObject);
        }

        /// <summary>Shows a warning for <paramref name="seconds"/>: from its enemy while it lives (it follows the enemy),
        /// or from a fixed place when <paramref name="source"/> is null.</summary>
        public void Warn(EnemyController source, Vector2 from, float seconds, Color color, Shape shape)
        {
            Warning warning = null;
            foreach (var w in warnings)
                if (!w.Active)
                {
                    warning = w;
                    break;
                }
            if (warning == null)
            {
                var image = UiStyle.Image(root, "Warning", Color.white);
                image.preserveAspect = true;
                warning = new Warning { Image = image };
                warnings.Add(warning);
            }
            warning.Source = source;
            warning.From = from;
            warning.Seconds = Mathf.Max(seconds, 0.05f);
            warning.Elapsed = 0f;
            warning.Active = true;
            warning.Image.sprite = SymbolOf(shape);
            warning.Image.color = color;
            warning.Image.enabled = false;
            warning.Near = 0f;
            warning.PoolRadius = 0f;
            last = warning;
        }

        /// <summary>Takes down every warning from this enemy (its attack was let loose or broken off).</summary>
        public void Cancel(EnemyController source)
        {
            foreach (var w in warnings)
                if (w.Active && w.Source == source)
                    Finish(w);
        }

        /// <summary>Takes down every warning (a new level).</summary>
        public void Clear()
        {
            foreach (var w in warnings)
                if (w.Active)
                    Finish(w);
        }

        static void Finish(Warning w)
        {
            w.Active = false;
            w.Source = null;
            w.Image.enabled = false;
        }

        void LateUpdate()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            var cam = Camera.main;
            placedAngles.Clear();
            for (var i = 0; i < warnings.Count; i++)
            {
                var w = warnings[i];
                if (!w.Active)
                    continue;
                w.Elapsed += Time.deltaTime;
                if (w.Elapsed >= w.Seconds || (w.Source != null && !w.Source.IsAlive))
                {
                    Finish(w);
                    continue;
                }
                if (player == null || cam == null)
                {
                    w.Image.enabled = false;
                    continue;
                }
                var chestWorld = Elevation.Lifted(player.transform.position) + Vector3.up * ChestHeight;
                var chest = (Vector2)cam.WorldToScreenPoint(chestWorld);
                var unit = Mathf.Abs(cam.WorldToScreenPoint(chestWorld + Vector3.right).x - chest.x);
                var from = w.Source != null ? w.Source.GroundPosition : w.From;
                if (w.Near > 0f &&
                    Vector2.Distance(IsoMath.WorldToGround(player.transform.position), from) - w.PoolRadius > w.Near)
                {
                    w.Image.enabled = false;
                    continue;
                }
                var toward = (Vector2)cam.WorldToScreenPoint(Elevation.World2(from)) - chest;
                var angle = toward.sqrMagnitude > 1f ? Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg : 90f;
                // Crowded warnings move along the ring, not out from it.
                angle = WarningRules.Place(angle, placedAngles);
                placedAngles.Add(angle);
                var radians = angle * Mathf.Deg2Rad;
                var rect = w.Image.rectTransform;
                rect.position = chest + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (unit * Radius);
                rect.sizeDelta = Vector2.one * (unit * Size);
                rect.localScale = Vector3.one * WarningRules.Scale(w.Elapsed, w.Seconds);
                w.Image.enabled = true;
            }
        }

        // The symbols: white so the colour tints them, the outline and the "!" dark. 15 pixels square.
        static Sprite SymbolOf(Shape shape)
        {
            var index = (int)shape;
            if (symbols[index] != null)
                return symbols[index];
            const int n = 15;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "Warning Symbol " + shape,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[n * n];
            var dark = new Color32(20, 12, 10, 255);
            var white = new Color32(255, 255, 255, 255);
            for (var row = 0; row < n; row++)
                for (var x = 0; x < n; x++)
                {
                    if (!WarningRules.Inside(shape, x, row, n))
                        continue;
                    // The outline: a cell inside with a neighbour outside.
                    var edge = !WarningRules.Inside(shape, x - 1, row, n) || !WarningRules.Inside(shape, x + 1, row, n) ||
                        !WarningRules.Inside(shape, x, row - 1, n) || !WarningRules.Inside(shape, x, row + 1, n);
                    // The "!": a bar down the middle and a dot below it (lower in the triangle, where it is wide).
                    var top = shape == Shape.Triangle ? 5 : 3;
                    var bar = x >= 6 && x <= 8 && ((row >= top && row <= top + 5) || (row >= top + 7 && row <= top + 8));
                    pixels[(n - 1 - row) * n + x] = edge || bar ? dark : white;
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable (CLAUDE.md, pixel art).
            symbols[index] = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                Vector4.zero, false);
            return symbols[index];
        }
    }

    /// <summary>The warning symbols' layout and throb (pure, tested).</summary>
    public static class WarningRules
    {
        /// <summary>Warnings closer together than this (degrees round her) stack outward rather than overlap.</summary>
        public const float CrowdDegrees = 26f;

        /// <summary>Where a warning goes on the ring: at its own angle, or when that is within <see cref="CrowdDegrees"/>
        /// of one already placed, the nearest free angle either side (its own side first), so they cluster round her.
        /// </summary>
        public static float Place(float angle, IReadOnlyList<float> placed)
        {
            for (var step = 0; step < 14; step++)
            {
                var offset = (step + 1) / 2 * CrowdDegrees * (step % 2 == 1 ? 1f : -1f);
                var candidate = Mathf.Repeat(angle + offset + 180f, 360f) - 180f;
                var free = true;
                for (var i = 0; i < placed.Count && free; i++)
                    free = Mathf.Abs(Mathf.DeltaAngle(candidate, placed[i])) >= CrowdDegrees;
                if (free)
                    return candidate;
            }
            return angle;
        }

        /// <summary>Whether pixel (x, row from the top) of an n-pixel symbol is inside its shape.</summary>
        public static bool Inside(IncomingWarnings.Shape shape, int x, int row, int n)
        {
            if (x < 0 || row < 0 || x >= n || row >= n)
                return false;
            var dx = x + 0.5f - n / 2f;
            var dy = row + 0.5f - n / 2f;
            switch (shape)
            {
                case IncomingWarnings.Shape.Triangle:
                    // Point up, base along the bottom row.
                    return row >= 1 && Mathf.Abs(dx) <= (row + 0.5f) * (n / 2f) / (n - 1);
                case IncomingWarnings.Shape.Diamond:
                    return Mathf.Abs(dx) + Mathf.Abs(dy) <= n / 2f;
                default:
                    return dx * dx + dy * dy <= n * n / 4f;
            }
        }

        /// <summary>The symbol's scale: it pops in over its first tenth of a second, then throbs, faster as the attack
        /// nears (two beats a second at the start, eight at the end).</summary>
        public static float Scale(float elapsed, float seconds)
        {
            var pop = Mathf.Clamp01(elapsed / 0.1f);
            var progress = Mathf.Clamp01(elapsed / Mathf.Max(seconds, 1e-4f));
            var beatsPerSecond = Mathf.Lerp(2f, 8f, progress);
            // The phase is the beats so far: the integral of a rate rising linearly with progress.
            var phase = elapsed * (2f + (beatsPerSecond - 2f) * 0.5f);
            var throb = 1f + 0.14f * Mathf.Sin(phase * Mathf.PI * 2f);
            return Mathf.Lerp(0.4f, 1f, pop) * throb;
        }
    }
}
