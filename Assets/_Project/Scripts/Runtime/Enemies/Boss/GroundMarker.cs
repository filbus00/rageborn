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
        static readonly List<GroundMarker> live = new List<GroundMarker>();

        GroundMarker(GameObject root, Transform fill, float duration, bool isCircle, Vector2 center, float radius)
        {
            this.root = root;
            this.fill = fill;
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
            var root = NewSprite("Telegraph", TelegraphArt.Circle, color, parent, 40);
            Place(root.transform, ground, radius);
            var fillObject = NewSprite("Fill", TelegraphArt.Disc, color, root.transform, 41);
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
            var root = NewSprite("Line", TelegraphArt.Line, color, parent, 42);
            PlaceLine(root.transform, from, to, width);
            return new GroundMarker(root, null, duration, false, from, 0f);
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

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        void Restart(float duration)
        {
            Duration = duration;
            Elapsed = 0f;
            if (fill != null)
                fill.localScale = Vector3.zero;
            root.SetActive(true);
        }

        /// <summary>Advances the fill. A ring fades in instead.</summary>
        public void Advance(float deltaSeconds)
        {
            Elapsed += deltaSeconds;
            var t = Duration > 0f ? Mathf.Clamp01(Elapsed / Duration) : 1f;
            if (fill != null)
                fill.localScale = new Vector3(t, t, 1f);
        }

        public void Destroy()
        {
            live.Remove(this);
            if (root != null)
                UnityEngine.Object.Destroy(root);
        }

        static void PlaceLine(Transform transform, Vector2 from, Vector2 to, float width)
        {
            var a = IsoMath.GroundToWorld(from);
            var b = IsoMath.GroundToWorld(to);
            var delta = b - a;
            transform.position = (a + b) / 2f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            transform.localScale = new Vector3(delta.magnitude, width, 1f);
        }

        // A ground circle of radius r is an ellipse r wide and r/2 tall on screen.
        static void Place(Transform transform, Vector2 ground, float radius)
        {
            transform.position = IsoMath.GroundToWorld(ground);
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
