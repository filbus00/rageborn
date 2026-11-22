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
        public const int RootCount = 8;
        const float RiseSeconds = 0.22f;
        const float SinkSeconds = 0.3f;
        const int MaxHeight = 14;
        // Lighter than the dirt and the forest floor, with a dark outline, so they read on any ground.
        static readonly Color Bark = new Color(0.62f, 0.44f, 0.24f, 1f);
        static readonly Color Light = new Color(0.78f, 0.6f, 0.34f, 1f);
        static readonly Color Moss = new Color(0.42f, 0.66f, 0.26f, 1f);
        static readonly Color Outline = new Color(0.1f, 0.06f, 0.03f, 1f);

        sealed class Root
        {
            public SpriteRenderer[] Pixels;
            public SpriteRenderer[] Edges;
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
                Spawn(ground + offset, seconds, Random.Range(8, MaxHeight + 1), Random.Range(-0.4f, 0.4f), -Random.value * 0.1f);
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
                root = new Root { Pixels = new SpriteRenderer[MaxHeight], Edges = new SpriteRenderer[MaxHeight] };
                for (var i = 0; i < MaxHeight; i++)
                {
                    root.Edges[i] = NewPixel(Outline);
                    root.Pixels[i] = NewPixel(Bark);
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

        // Standing up from the ground, so it draws with the characters, in front of what is behind it.
        SpriteRenderer NewPixel(Color colour)
        {
            var go = GroundMarker.NewSprite("Root", FireFx.Pixel, colour, parent, 0);
            var r = go.GetComponent<SpriteRenderer>();
            r.sortingLayerName = GameSortingLayers.Entities;
            go.SetActive(false);
            return r;
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
                    for (var i = 0; i < MaxHeight; i++)
                    {
                        root.Pixels[i].gameObject.SetActive(false);
                        root.Edges[i].gameObject.SetActive(false);
                    }
                    continue;
                }
                var grown = root.Age < 0f ? 0f
                    : root.Age < RiseSeconds ? root.Age / RiseSeconds
                    : root.Age < sinkStart ? 1f
                    : 1f - (root.Age - sinkStart) / SinkSeconds;
                var shown = Mathf.RoundToInt(root.Height * grown);
                var baseAt = Elevation.World2(root.At);
                for (var i = 0; i < MaxHeight; i++)
                {
                    var p = root.Pixels[i];
                    var edge = root.Edges[i];
                    if (i >= shown)
                    {
                        p.gameObject.SetActive(false);
                        edge.gameObject.SetActive(false);
                        continue;
                    }
                    p.gameObject.SetActive(true);
                    edge.gameObject.SetActive(true);
                    // Thick at the foot, thinning to a tip, leaning as it goes up and swaying a little when held.
                    var t = (float)i / MaxHeight;
                    var sway = Mathf.Sin(Time.time * 7f + root.At.x * 5f) * 0.012f * i;
                    var x = root.Lean * t * 0.7f + sway;
                    var at = FireFxRules.Snap(baseAt + new Vector2(x, i * FireFxRules.Block));
                    var width = i < 3 ? 3f : i < 8 ? 2f : 1f;
                    p.transform.position = at;
                    p.transform.localScale = new Vector3(width, 1f, 1f);
                    // Order 0 with the characters: the layer sorts along Y, lower in front. The outline sits a hair behind.
                    // One block tall (a taller outline of a lower piece would cover the piece above); only the tip is capped.
                    var tip = i == shown - 1;
                    edge.transform.position = at + new Vector2(0f, tip ? FireFxRules.Block * 0.5f : 0.001f);
                    edge.transform.localScale = new Vector3(width + 2f, tip ? 2f : 1f, 1f);
                    p.color = i % 5 == 4 ? Moss : (i % 2 == 0 ? Bark : Light);
                }
            }
        }
    }
}
