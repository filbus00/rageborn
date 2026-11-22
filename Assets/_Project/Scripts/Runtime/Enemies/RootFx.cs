using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Roots that burst up from the ground round her feet when a rooting attack (the Briar Thrall's Root Grasp) hits her
    /// (2026-11-22, the owner: "make it so that roots spring out from the ground if the rooting attack hits you"). Each
    /// root is a short column of pixels that shoots up, holds while she is held, then sinks. Pooled; owned and ticked by
    /// <see cref="EnemyHazards"/>.
    /// </summary>
    public sealed class RootFx
    {
        public const int RootCount = 7;
        const float RiseSeconds = 0.22f;
        const float SinkSeconds = 0.3f;
        const int MaxHeight = 9;
        static readonly Color Bark = new Color(0.32f, 0.2f, 0.1f, 1f);
        static readonly Color Moss = new Color(0.3f, 0.5f, 0.2f, 1f);

        sealed class Root
        {
            public SpriteRenderer[] Pixels;
            public Vector2 At;
            public int Height;
            public float Age;
            public float Hold;
            public float Lean;
            public bool Active;
        }

        readonly List<Root> roots = new List<Root>();
        readonly Transform parent;

        public RootFx(Transform parent) => this.parent = parent;

        /// <summary>Roots burst up in a ring round a point on the ground and hold for <paramref name="seconds"/>.</summary>
        public void Burst(Vector2 ground, float seconds)
        {
            for (var i = 0; i < RootCount; i++)
            {
                var angle = (i + Random.value * 0.6f) / RootCount * Mathf.PI * 2f;
                var distance = Random.Range(0.35f, 0.75f);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                Spawn(ground + offset, seconds, Random.Range(5, MaxHeight + 1), Random.Range(-0.4f, 0.4f), -Random.value * 0.1f);
            }
        }

        void Spawn(Vector2 ground, float hold, int height, float lean, float delay)
        {
            Root root = null;
            foreach (var r in roots)
                if (!r.Active)
                {
                    root = r;
                    break;
                }
            if (root == null)
            {
                root = new Root { Pixels = new SpriteRenderer[MaxHeight] };
                for (var i = 0; i < MaxHeight; i++)
                {
                    var go = GroundMarker.NewSprite("Root", FireFx.Pixel, Bark, parent, 0);
                    var r = go.GetComponent<SpriteRenderer>();
                    // Standing up from the ground, so it draws with the characters, in front of what is behind it.
                    r.sortingLayerName = GameSortingLayers.Entities;
                    go.SetActive(false);
                    root.Pixels[i] = r;
                }
                roots.Add(root);
            }
            root.At = ground;
            root.Height = height;
            root.Lean = lean;
            root.Age = delay;
            root.Hold = hold;
            root.Active = true;
        }

        public void Tick(float deltaTime)
        {
            foreach (var root in roots)
            {
                if (!root.Active)
                    continue;
                root.Age += deltaTime;
                var sinkStart = RiseSeconds + root.Hold;
                if (root.Age >= sinkStart + SinkSeconds)
                {
                    root.Active = false;
                    foreach (var p in root.Pixels)
                        p.gameObject.SetActive(false);
                    continue;
                }
                var grown = root.Age < 0f ? 0f
                    : root.Age < RiseSeconds ? root.Age / RiseSeconds
                    : root.Age < sinkStart ? 1f
                    : 1f - (root.Age - sinkStart) / SinkSeconds;
                var shown = Mathf.RoundToInt(root.Height * grown);
                var baseAt = Elevation.World2(root.At);
                // Sorted by its foot, so she stands in front of roots behind her and behind roots in front.
                var order = Mathf.RoundToInt(-baseAt.y * 100f);
                for (var i = 0; i < root.Pixels.Length; i++)
                {
                    var p = root.Pixels[i];
                    if (i >= shown)
                    {
                        p.gameObject.SetActive(false);
                        continue;
                    }
                    p.gameObject.SetActive(true);
                    // Wider at the foot, thinning to a tip, leaning a little as it goes up and swaying when held.
                    var t = (float)i / MaxHeight;
                    var sway = Mathf.Sin(Time.time * 7f + root.At.x * 5f) * 0.01f * i;
                    var x = root.Lean * t * 0.5f + sway;
                    var y = i * FireFxRules.Block;
                    p.transform.position = FireFxRules.Snap(baseAt + new Vector2(x, y));
                    var size = i < 3 ? 2f : 1f;
                    p.transform.localScale = new Vector3(size, 1f, 1f);
                    p.color = i % 4 == 3 ? Moss : Bark;
                    p.sortingOrder = order;
                }
            }
        }
    }
}
