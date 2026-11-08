using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A support's aura on the ground (2026-11-08, the owner: "the enemies that haste other enemies, the circle around does
    /// not fit the art style, remake it"): a ring of short amber dashes with a dark edge under them, turning slowly round
    /// the enemy, in the 16x16 style (each drawn pixel 2 screen pixels, as the pools and rims, <see cref="PoolArt"/>).
    /// Its frames are drawn once a size. (It was the smooth telegraph ring, <see cref="GroundMarker.Ring"/>.)
    /// </summary>
    public sealed class AuraRing
    {
        const int Frames = 8;
        const float FramesPerSecond = 6f;
        const int Dashes = 14;

        static readonly Color32 Dash = new Color32(255, 176, 64, 230);
        static readonly Color32 DashLight = new Color32(255, 226, 140, 240);
        static readonly Color32 Edge = new Color32(90, 40, 12, 170);

        static readonly Dictionary<int, Sprite[]> Cache = new Dictionary<int, Sprite[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Cache.Clear();

        readonly SpriteRenderer renderer;
        readonly Sprite[] frames;

        AuraRing(SpriteRenderer renderer, Sprite[] frames)
        {
            this.renderer = renderer;
            this.frames = frames;
        }

        public bool IsVisible => renderer != null && renderer.enabled;

        public static AuraRing Create(float radius, Transform parent)
        {
            var go = GroundMarker.NewSprite("Aura Ring", null, Color.white, parent, 39);
            var renderer = go.GetComponent<SpriteRenderer>();
            return new AuraRing(renderer, FramesFor(radius));
        }

        /// <summary>Shows it round a ground point, turning with time.</summary>
        public void Show(Vector2 ground)
        {
            if (renderer == null)
                return;
            renderer.enabled = true;
            renderer.transform.position = Elevation.World2(ground);
            renderer.sprite = frames[Mathf.FloorToInt(Time.time * FramesPerSecond) % Frames];
        }

        public void Hide()
        {
            if (renderer != null)
                renderer.enabled = false;
        }

        static Sprite[] FramesFor(float radius)
        {
            var w = PoolArt.WidthFor(radius / PoolArt.Block);
            if (Cache.TryGetValue(w, out var cached))
                return cached;
            var h = w / 2;
            var sprites = new Sprite[Frames];
            for (var f = 0; f < Frames; f++)
            {
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = $"Aura Ring {w} {f}", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                };
                texture.SetPixels32(Pixels(w, h, f / (float)Frames));
                texture.Apply(false, true);
                sprites[f] = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), (float)PoolArt.ScreenUnit / PoolArt.Block, 0,
                    SpriteMeshType.FullRect, Vector4.zero, false);
            }
            return Cache[w] = sprites;
        }

        /// <summary>One frame's pixels (pure), from the bottom row: the dashes turned by phase (0 to 1 of one dash's
        /// spacing), a dark pixel under each dash pixel, the dashes' leading pixel lighter.</summary>
        public static Color32[] Pixels(int w, int h, float phase)
        {
            var pixels = new Color32[w * h];
            float hw = w / 2f, hh = h / 2f;
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    float u = (x + 0.5f - hw) / hw, v = (y + 0.5f - hh) / hh;
                    var r = Mathf.Sqrt(u * u + v * v);
                    // One pixel thick on the screen: the band's width in this ellipse's units, by where on it the pixel is.
                    var thickness = 1.2f / Mathf.Max(1f, Mathf.Lerp(hw, hh, Mathf.Abs(v) / Mathf.Max(0.001f, r)));
                    if (Mathf.Abs(r - 0.94f) > thickness * 0.5f)
                        continue;
                    var turn = (Mathf.Atan2(v, u) / (Mathf.PI * 2f) + 1f) % 1f;
                    var along = (turn * Dashes + phase) % 1f;
                    if (along < 0.55f)
                        pixels[x + y * w] = along > 0.45f ? DashLight : Dash;
                }
            // A dark edge under each dash pixel, so the ring reads on light grass.
            for (var y = 1; y < h; y++)
                for (var x = 0; x < w; x++)
                    if (pixels[x + y * w].a > 0 && pixels[x + (y - 1) * w].a == 0)
                        pixels[x + (y - 1) * w] = Edge;
            return pixels;
        }
    }
}
