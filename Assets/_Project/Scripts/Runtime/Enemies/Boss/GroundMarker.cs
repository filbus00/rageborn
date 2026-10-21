using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Placeholder sprites for telegraphs and boss effects, made in code once and shared: a circle with a hard outline
    /// and a soft fill, a solid disc for the fill that grows, a ring for the burning arena edge, a line and an ember.
    /// Each is one world unit across, so scaling a sprite by its size in world units sizes it.
    /// </summary>
    public static class TelegraphArt
    {
        const int Size = 128;

        static Sprite circle, disc, ring, line, ember;

        /// <summary>Docs/01-core-gameplay.md: a hard outline and a soft fill.</summary>
        public static Sprite Circle => circle != null ? circle : circle = Make("Telegraph Circle", r => r > 1f ? 0f : r > 0.93f ? 1f : 0.16f);

        public static Sprite Disc => disc != null ? disc : disc = Make("Telegraph Disc", r => r > 1f ? 0f : 0.5f);

        /// <summary>A band from 72 to 100 percent of the radius: the Cinder Warden's burning arena edge.</summary>
        public static Sprite Ring => ring != null ? ring : ring = Make("Fire Ring", r => r > 1f || r < 0.72f ? 0f : r > 0.96f || r < 0.75f ? 0.9f : 0.42f);

        public static Sprite Line => line != null ? line : line = Make("Telegraph Line", r => 1f);

        public static Sprite Ember => ember != null ? ember : ember = Make("Ember", r => r > 1f ? 0f : 1f - r * 0.6f);

        // The attack indicators, pools and projectile glows remade (the owner, 2026-10-18: "work on the graphics of the
        // attack indicators, the pools and the projectiles, make them look better").
        static Sprite rim, fillDisc, shock, pool, bubbles, lane, laneFill, glow, streak;

        /// <summary>A telegraph's outline: a bright double rim, eight notches pointing in, and a faint floor that brightens
        /// toward the edge.</summary>
        public static Sprite Rim => rim != null ? rim : rim = MakeXY("Telegraph Rim", (u, v) =>
        {
            var r = Mathf.Sqrt(u * u + v * v);
            if (r > 1f)
                return 0f;
            if (r > 0.94f)
                return 1f;
            if (r > 0.86f && r < 0.89f)
                return 0.75f;
            var a = Mathf.Atan2(v, u) / (Mathf.PI * 2f) * 8f;
            var notch = Mathf.Abs(a - Mathf.Round(a));
            if (r > 0.76f && notch < 0.035f)
                return 0.9f;
            return 0.06f + 0.16f * r * r;
        });

        /// <summary>The fill that grows inside a telegraph: soft inside, a hard bright edge that closes in on the rim.</summary>
        public static Sprite FillDisc => fillDisc != null ? fillDisc : fillDisc = Make("Telegraph Fill", r => r > 1f ? 0f : r > 0.9f ? 0.95f : 0.22f + 0.2f * r);

        /// <summary>The shock ring where an attack lands, thin and bright.</summary>
        public static Sprite Shock => shock != null ? shock : shock = Make("Shock Ring", r => r > 1f || r < 0.8f ? 0f : 1f - Mathf.Abs(r - 0.92f) * 8f);

        /// <summary>A pool: an uneven blob with a darker heart and a bright lip.</summary>
        public static Sprite Pool => pool != null ? pool : pool = MakeXY("Pool", (u, v) =>
        {
            var r = Mathf.Sqrt(u * u + v * v);
            var a = Mathf.Atan2(v, u);
            var edge = 0.86f + 0.07f * Mathf.Sin(a * 3f + 0.6f) + 0.05f * Mathf.Sin(a * 5f + 2.1f) + 0.03f * Mathf.Sin(a * 9f);
            if (r > edge)
                return 0f;
            if (r > edge - 0.07f)
                return 0.95f;
            return 0.45f + 0.25f * (r / edge);
        });

        // Made once, not once a pixel.
        static readonly (float x, float y, float size)[] BubbleSpots =
            { (0.3f, 0.2f, 0.11f), (-0.35f, 0.35f, 0.08f), (-0.15f, -0.4f, 0.13f), (0.45f, -0.25f, 0.07f), (0.05f, 0.55f, 0.06f), (-0.55f, -0.05f, 0.09f) };

        /// <summary>Bubbles or embers in a pool: scattered bright rings, turned over the pool to look alive.</summary>
        public static Sprite Bubbles => bubbles != null ? bubbles : bubbles = MakeXY("Bubbles", (u, v) =>
        {
            foreach (var (x, y, size) in BubbleSpots)
            {
                var d = Mathf.Sqrt((u - x) * (u - x) + (v - y) * (v - y));
                if (d < size && d > size * 0.55f)
                    return 0.9f;
                if (d < size * 0.55f)
                    return 0.3f;
            }
            return 0f;
        });

        /// <summary>A lane's outline (x along it, y across): hard bright edges, a faint floor, an arrowhead at the far end.</summary>
        public static Sprite Lane => lane != null ? lane : lane = MakeXY("Lane", (u, v) =>
        {
            var x = (u + 1f) * 0.5f;
            var y = Mathf.Abs(v);
            if (y > 0.82f)
                return 1f;
            var chevron = Mathf.Abs((x * 6f - y * 0.35f) % 1f - 0.5f);
            return chevron < 0.06f ? 0.35f : 0.12f;
        });

        /// <summary>A lane's fill, grown from the thrower along the lane.</summary>
        public static Sprite LaneFill => laneFill != null ? laneFill : laneFill = MakeXY("Lane Fill", (u, v) =>
        {
            var x = (u + 1f) * 0.5f;
            return Mathf.Abs(v) > 0.7f ? 0f : x > 0.94f ? 0.95f : 0.3f;
        }, new Vector2(0f, 0.5f));

        /// <summary>A soft round glow, bright at the heart (a projectile's halo).</summary>
        public static Sprite Glow => glow != null ? glow : glow = Make("Glow", r => r > 1f ? 0f : Mathf.Pow(1f - r, 2f));

        /// <summary>A tapered streak, its head on the right at the pivot, fading out toward the tail on the left.</summary>
        public static Sprite Streak => streak != null ? streak : streak = MakeXY("Streak", (u, v) =>
        {
            var x = (u + 1f) * 0.5f;
            var width = x;
            return Mathf.Abs(v) > width ? 0f : x * x * (1f - Mathf.Abs(v) / Mathf.Max(0.01f, width) * 0.5f);
        }, new Vector2(1f, 0.5f));

        /// <summary>A white sprite whose alpha at each pixel is <paramref name="alpha"/> of its place, both from -1 to 1
        /// across the square.</summary>
        static Sprite MakeXY(string name, Func<float, float, float> alpha, Vector2? pivot = null)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[Size * Size];
            var half = Size / 2f;
            for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                    pixels[x + y * Size] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha((x + 0.5f - half) / half, (y + 0.5f - half) / half)) * 255f));
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), pivot ?? new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect, Vector4.zero, false);
        }

        /// <summary>A white sprite whose alpha at each pixel is <paramref name="alpha"/> of its distance from the
        /// center, 0 there and 1 at the edge.</summary>
        static Sprite Make(string name, Func<float, float> alpha)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[Size * Size];
            var half = Size / 2f;
            for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                {
                    var r = Mathf.Sqrt((x + 0.5f - half) * (x + 0.5f - half) + (y + 0.5f - half) * (y + 0.5f - half)) / half;
                    pixels[x + y * Size] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(r)) * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable, and tracing its outline fails on iOS.
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect, Vector4.zero, false);
        }
    }

    /// <summary>
    /// A telegraph drawn on the ground (Docs/01-core-gameplay.md, kiting and telegraphs): a circle or a line in ground
    /// space, projected to the isometric view, with an inner fill that grows until the attack lands. The fight that made
    /// it advances it with the same clock as its attack, so a pause or a stagger stops it too. An enemy keeps one marker
    /// and reuses it for every attack (<see cref="RestartCircle"/>, <see cref="RestartLine"/>, <see cref="Hide"/>), so a
    /// fight creates no objects.
    /// </summary>
    public sealed class GroundMarker
    {
        readonly GameObject root;
        readonly Transform fill;
        // 2026-10-18: the rim's colour (it pulses in the last quarter), and a pool's bubbles, made when first needed.
        readonly SpriteRenderer rootRenderer;
        Color color;
        bool pooled;
        SpriteRenderer bubbles;
        float poolTime;
        Vector3 poolScale;
        static readonly List<GroundMarker> live = new List<GroundMarker>();

        GroundMarker(GameObject root, Transform fill, float duration, bool isCircle, Vector2 center, float radius)
        {
            this.root = root;
            this.fill = fill;
            rootRenderer = root.GetComponent<SpriteRenderer>();
            color = rootRenderer.color;
            Duration = duration;
            IsCircle = isCircle;
            Center = center;
            Radius = radius;
            live.Add(this);
        }

        /// <summary>Every marker not yet destroyed, shown or hidden. For development tools (the autopilot steps out of
        /// circles), not gameplay.</summary>
        public static IReadOnlyList<GroundMarker> Live
        {
            get
            {
                // Markers of an unloaded scene are gone without Destroy being called.
                live.RemoveAll(m => m.root == null);
                return live;
            }
        }

        // Domain reload is off in the editor, so statics survive between plays.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlay() => live.Clear();

        /// <summary>A filling ground circle (a slam, a rain drop), as opposed to a line or the boss's ring.</summary>
        public bool IsCircle { get; }
        public Vector2 Center { get; private set; }
        public float Radius { get; private set; }
        public bool IsVisible => root != null && root.activeInHierarchy;

        public float Duration { get; private set; }
        public float Elapsed { get; private set; }
        public bool Done => Elapsed >= Duration;

        /// <summary>A ground circle of the given radius, filling over <paramref name="duration"/> seconds.</summary>
        public static GroundMarker Circle(Vector2 ground, float radius, float duration, Color color, Transform parent)
        {
            var root = NewSprite("Telegraph", TelegraphArt.Rim, color, parent, 40);
            Place(root.transform, ground, radius);
            var fillObject = NewSprite("Fill", TelegraphArt.FillDisc, color, root.transform, 41);
            fillObject.transform.localScale = Vector3.zero;
            return new GroundMarker(root, fillObject.transform, duration, true, ground, radius);
        }

        /// <summary>A lasting ring (the burning arena edge), fading in over <paramref name="duration"/> seconds.</summary>
        public static GroundMarker Ring(Vector2 ground, float radius, float duration, Color color, Transform parent)
        {
            var root = NewSprite("Fire Ring", TelegraphArt.Ring, color, parent, 39);
            Place(root.transform, ground, radius);
            return new GroundMarker(root, null, duration, false, ground, radius);
        }

        /// <summary>A thin line on the ground from one point to another (projectile and charge telegraphs).</summary>
        public static GroundMarker Line(Vector2 from, Vector2 to, float width, float duration, Color color, Transform parent)
        {
            var root = NewSprite("Line", TelegraphArt.Lane, color, parent, 42);
            PlaceLine(root.transform, from, to, width);
            // The fill runs from the thrower to the far end (2026-10-18).
            var fillObject = NewSprite("Fill", TelegraphArt.LaneFill, color, root.transform, 43);
            fillObject.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
            fillObject.transform.localScale = new Vector3(0f, 1f, 1f);
            return new GroundMarker(root, fillObject.transform, duration, false, from, 0f);
        }

        /// <summary>Shows a circle marker again somewhere else, its fill starting over.</summary>
        public void RestartCircle(Vector2 ground, float radius, float duration)
        {
            Center = ground;
            Radius = radius;
            Place(root.transform, ground, radius);
            Restart(duration);
        }

        /// <summary>Shows a line marker again along another segment.</summary>
        public void RestartLine(Vector2 from, Vector2 to, float width, float duration)
        {
            PlaceLine(root.transform, from, to, width);
            Restart(duration);
        }

        /// <summary>Recolours the marker (a pooled marker used for another kind of hazard).</summary>
        public void SetColor(Color color)
        {
            if (root == null)
                return;
            this.color = color;
            rootRenderer.color = color;
            if (fill != null)
                fill.GetComponent<SpriteRenderer>().color = color;
            if (bubbles != null)
                bubbles.color = Color.Lerp(color, Color.white, 0.35f);
        }

        /// <summary>
        /// Turns a landed circle into a lasting pool (2026-10-18): an uneven blob in its colour with bubbles turning over
        /// it, which <see cref="Animate"/> keeps moving. <see cref="RestartCircle"/> makes it a telegraph again.
        /// </summary>
        public void SetPool()
        {
            if (root == null || !IsCircle || pooled)
                return;
            pooled = true;
            poolTime = UnityEngine.Random.value * 10f;
            poolScale = root.transform.localScale;
            rootRenderer.sprite = TelegraphArt.Pool;
            rootRenderer.color = new Color(color.r, color.g, color.b, Mathf.Min(0.85f, color.a));
            if (fill != null)
                fill.gameObject.SetActive(false);
            if (bubbles == null)
                bubbles = NewSprite("Bubbles", TelegraphArt.Bubbles, color, root.transform, 41).GetComponent<SpriteRenderer>();
            bubbles.color = Color.Lerp(color, Color.white, 0.35f);
            bubbles.gameObject.SetActive(true);
        }

        /// <summary>A pool's life: it breathes a little, its bubbles turn and flicker.</summary>
        public void Animate(float deltaSeconds)
        {
            if (!pooled || root == null)
                return;
            poolTime += deltaSeconds;
            var breathe = 1f + 0.035f * Mathf.Sin(poolTime * 3.1f);
            root.transform.localScale = new Vector3(poolScale.x * breathe, poolScale.y * (2f - breathe), 1f);
            bubbles.transform.localRotation = Quaternion.Euler(0f, 0f, poolTime * 40f);
            var flicker = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(poolTime * 4.3f));
            var c = Color.Lerp(color, Color.white, 0.35f);
            bubbles.color = new Color(c.r, c.g, c.b, c.a * flicker);
        }

        /// <summary>Moves a shown marker without restarting it (an aura that follows its enemy).</summary>
        public void MoveTo(Vector2 ground)
        {
            Center = ground;
            Place(root.transform, ground, Radius);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        void Restart(float duration)
        {
            Duration = duration;
            Elapsed = 0f;
            if (pooled)
            {
                pooled = false;
                rootRenderer.sprite = TelegraphArt.Rim;
                if (fill != null)
                    fill.gameObject.SetActive(true);
                if (bubbles != null)
                    bubbles.gameObject.SetActive(false);
            }
            rootRenderer.color = color;
            if (fill != null)
                fill.localScale = IsCircle ? Vector3.zero : new Vector3(0f, 1f, 1f);
            root.SetActive(true);
        }

        /// <summary>Advances the fill. A ring fades in instead.</summary>
        public void Advance(float deltaSeconds)
        {
            Elapsed += deltaSeconds;
            var t = Duration > 0f ? Mathf.Clamp01(Elapsed / Duration) : 1f;
            if (fill != null)
                fill.localScale = IsCircle ? new Vector3(t, t, 1f) : new Vector3(t, 1f, 1f);
            // In the last quarter the rim pulses quicker and quicker: it is about to land (2026-10-18). Once it has
            // landed it holds its own colour again: a lasting zone (BossFight.LastingZone) or a marker kept until its
            // attack ends must not keep pulsing as if about to land.
            if (rootRenderer != null && fill != null && !pooled && Done)
                rootRenderer.color = color;
            else if (rootRenderer != null && fill != null && !pooled)
            {
                var pulse = t < 0.75f ? 1f : 0.7f + 0.3f * Mathf.Cos(Elapsed * (18f + 30f * (t - 0.75f)));
                rootRenderer.color = new Color(Mathf.Min(1f, color.r * (2f - pulse)), Mathf.Min(1f, color.g * (2f - pulse)), Mathf.Min(1f, color.b * (2f - pulse)), color.a * pulse);
            }
        }

        public void Destroy()
        {
            live.Remove(this);
            if (root != null)
                UnityEngine.Object.Destroy(root);
        }

        static void PlaceLine(Transform transform, Vector2 from, Vector2 to, float width)
        {
            var a = Elevation.World2(from);
            var b = Elevation.World2(to);
            var delta = b - a;
            transform.position = (a + b) / 2f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            transform.localScale = new Vector3(delta.magnitude, width, 1f);
        }

        // A ground circle of radius r is an ellipse r wide and r/2 tall on screen.
        static void Place(Transform transform, Vector2 ground, float radius)
        {
            transform.position = Elevation.World2(ground);
            transform.localScale = new Vector3(radius * 2f, radius * 2f * IsoMath.GroundSquash, 1f);
        }

        internal static GameObject NewSprite(string name, Sprite sprite, Color color, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            // On the ground, under every character (Decals draws between the floor and the Entities layer).
            spriteRenderer.sortingLayerName = GameSortingLayers.Decals;
            spriteRenderer.sortingOrder = order;
            // Telegraphs and effects must read in a dark dungeon, so no 2D light dims them.
            SpriteMaterials.MakeUnlit(spriteRenderer);
            return go;
        }
    }
}
