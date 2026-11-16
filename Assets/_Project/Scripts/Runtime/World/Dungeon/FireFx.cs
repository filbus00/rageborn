using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>What burns: how many sparks it throws and how high they start.</summary>
    public enum FireKind
    {
        Campfire,
        Brazier,
        Torch,
        Candles,
        Hellfire,
        Lantern,
    }

    /// <summary>What drifts in the air of a level.</summary>
    public enum MoteKind
    {
        None,
        Fireflies,
        Dust,

        /// <summary>The Burnt Library's air (2026-11-16): embers rising and ash flakes drifting down.</summary>
        Embers,

        /// <summary>The Rift Caves' air (2026-11-16): violet sparks drifting up out of the rock, glowing and going out.</summary>
        RiftSparks,
    }

    /// <summary>The fire particles' numbers (2026-11-15, Claude's, to review). Pure, tested.</summary>
    public static class FireFxRules
    {
        /// <summary>The screen's pixels a unit, and a drawn pixel's size in them: particles sit on that grid.</summary>
        public const float PixelsPerUnit = 27f;
        public const float Block = 2f / PixelsPerUnit;

        /// <summary>Sparks a second.</summary>
        public static float Rate(FireKind kind) => kind switch
        {
            FireKind.Campfire => 11f,
            FireKind.Hellfire => 11f,
            FireKind.Brazier => 8f,
            FireKind.Torch => 5f,
            FireKind.Lantern => 1.5f,
            _ => 2f,
        };

        /// <summary>How far to either side of its middle a fire throws its sparks (world units).</summary>
        public static float Width(FireKind kind) => kind switch
        {
            FireKind.Campfire => 0.22f,
            FireKind.Hellfire => 0.25f,
            FireKind.Brazier => 0.16f,
            FireKind.Candles => 0.14f,
            _ => 0.05f,
        };

        /// <summary>One spark in this many is a puff of smoke instead (0 for none).</summary>
        public static int SmokeEvery(FireKind kind) => kind == FireKind.Campfire ? 5 : kind == FireKind.Brazier || kind == FireKind.Torch ? 8 : 0;

        static readonly Color32[] Fire =
        {
            new Color32(255, 246, 192, 255), new Color32(255, 216, 74, 255), new Color32(255, 138, 42, 255), new Color32(200, 50, 26, 255),
            new Color32(110, 30, 22, 255),
        };

        static readonly Color32[] Hell =
        {
            new Color32(255, 200, 200, 255), new Color32(255, 96, 74, 255), new Color32(200, 50, 42, 255), new Color32(122, 26, 20, 255),
            new Color32(60, 14, 20, 255),
        };

        // The Rift Caves' fire (2026-11-16): white to violet to a dark purple.
        static readonly Color32[] RiftFire =
        {
            new Color32(240, 220, 255, 255), new Color32(206, 150, 255, 255), new Color32(150, 90, 230, 255), new Color32(90, 44, 160, 255),
            new Color32(44, 22, 80, 255),
        };

        static readonly Color32[] Smoke =
        {
            new Color32(70, 66, 72, 200), new Color32(58, 56, 62, 170), new Color32(46, 44, 50, 130), new Color32(36, 34, 40, 90),
        };

        /// <summary>A spark's colour by its age (0 to 1): in flat steps from white-yellow to dark red, never a fade.</summary>
        public static Color32 Colour(float age, bool hell, bool smoke, bool rift = false)
        {
            var ramp = smoke ? Smoke : hell ? (rift ? RiftFire : Hell) : Fire;
            return ramp[Mathf.Clamp((int)(Mathf.Clamp01(age) * ramp.Length), 0, ramp.Length - 1)];
        }

        /// <summary>A spark is two drawn pixels across while young, one after; smoke the other way round.</summary>
        public static int Size(float age, bool smoke) => smoke ? (age < 0.35f ? 1 : 2) : age < 0.4f ? 2 : 1;

        /// <summary>A position on the grid of drawn pixels.</summary>
        public static Vector2 Snap(Vector2 world) => new Vector2(Mathf.Round(world.x / Block) * Block, Mathf.Round(world.y / Block) * Block);

        /// <summary>Whether a firefly shows at this moment: lit for a little under half of each slow blink.</summary>
        public static bool FireflyLit(float time, float phase) => Mathf.Repeat(time * 0.45f + phase, 1f) < 0.42f;
    }

    /// <summary>
    /// Sparks, smoke and drifting motes (the owner, 2026-11-15: "add some particles to the campfires, torches and anything
    /// that could need it"): every fire in a level throws stepped pixel sparks while it is in view, campfires, braziers and
    /// torches a puff of smoke now and then; fireflies drift in the forest, dust in the gaol. Drawn only, pooled, nothing
    /// made after the level loads; sparks are not touched by the lights, so they glow in the dark.
    /// </summary>
    public class FireFx : MonoBehaviour
    {
        const int MaxParticles = 180, Motes = 14;

        struct Emitter
        {
            public Vector2 At;
            public FireKind Kind;
            public float Owed;
            public int Count;
        }

        struct Particle
        {
            public Vector2 At;
            public Vector2 Velocity;
            public float Age, Life, Sway, Phase;
            public bool Smoke, Hell;
        }

        readonly List<Emitter> emitters = new List<Emitter>(64);
        readonly Particle[] particles = new Particle[MaxParticles];
        readonly SpriteRenderer[] renderers = new SpriteRenderer[MaxParticles];
        readonly SpriteRenderer[] motes = new SpriteRenderer[Motes];
        readonly Vector2[] moteDrift = new Vector2[Motes];
        readonly float[] motePhase = new float[Motes];
        int alive;
        MoteKind moteKind;
        Camera view;
        static Sprite dot;

        public static FireFx Begin(Transform parent, MoteKind motes)
        {
            var go = new GameObject("Fire Fx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<FireFx>();
            fx.Build(motes);
            return fx;
        }

        /// <summary>A fire at a world position (where its flame is).</summary>
        public void Add(Vector2 world, FireKind kind) => emitters.Add(new Emitter { At = world, Kind = kind, Owed = Random.value });

        public int Count => emitters.Count;

        void Build(MoteKind kind)
        {
            moteKind = kind;
            for (var i = 0; i < MaxParticles; i++)
            {
                renderers[i] = NewRenderer("Spark");
                renderers[i].enabled = false;
            }
            if (kind == MoteKind.None)
                return;
            for (var i = 0; i < Motes; i++)
            {
                motes[i] = NewRenderer("Mote");
                motes[i].enabled = false;
                moteDrift[i] = new Vector2(AmbienceRules.Spread(i, 5) * 0.12f, kind == MoteKind.Dust ? 0.03f + 0.02f * (i % 3) :
                    kind == MoteKind.Embers ? (i % 2 == 0 ? 0.16f + 0.04f * (i % 3) : -0.07f) :
                    kind == MoteKind.RiftSparks ? 0.1f + 0.05f * (i % 3) : AmbienceRules.Spread(i, 6) * 0.08f);
                motePhase[i] = i * 0.37f;
            }
        }

        SpriteRenderer NewRenderer(string name)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<SpriteRenderer>();
            r.sprite = Dot;
            r.sortingLayerName = GameSortingLayers.Effects;
            var unlit = SpriteMaterials.Unlit;
            if (unlit != null)
                r.sharedMaterial = unlit;
            return r;
        }

        // One drawn pixel: 2 x 2 of the screen's.
        static Sprite Dot
        {
            get
            {
                if (dot != null)
                    return dot;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                texture.SetPixels32(new[] { (Color32)Color.white, (Color32)Color.white, (Color32)Color.white, (Color32)Color.white });
                texture.Apply();
                dot = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), FireFxRules.PixelsPerUnit, 0, SpriteMeshType.FullRect);
                return dot;
            }
        }

        void Update()
        {
            if (view == null)
                view = Camera.main;
            if (view == null)
                return;
            var dt = Time.deltaTime;
            var time = Time.time;
            var centre = (Vector2)view.transform.position;
            var half = new Vector2(view.orthographicSize * view.aspect, view.orthographicSize);

            // Only the fires in view (a little past its edges) throw sparks.
            for (var i = 0; i < emitters.Count; i++)
            {
                var e = emitters[i];
                if (Mathf.Abs(e.At.x - centre.x) > half.x + 1f || Mathf.Abs(e.At.y - centre.y) > half.y + 2f)
                    continue;
                e.Owed += FireFxRules.Rate(e.Kind) * dt;
                while (e.Owed >= 1f)
                {
                    e.Owed -= 1f;
                    e.Count++;
                    Spawn(e);
                }
                emitters[i] = e;
            }

            for (var i = alive - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    // The last live one takes its place; its renderer goes dark.
                    alive--;
                    particles[i] = particles[alive];
                    renderers[alive].enabled = false;
                    continue;
                }
                p.At += p.Velocity * dt;
                var age = p.Age / p.Life;
                var at = p.At + new Vector2(Mathf.Sin(time * 5f + p.Phase) * p.Sway, 0f);
                var r = renderers[i];
                r.enabled = true;
                r.transform.position = FireFxRules.Snap(at);
                var size = FireFxRules.Size(age, p.Smoke);
                r.transform.localScale = new Vector3(size, size, 1f);
                r.color = FireFxRules.Colour(age, p.Hell, p.Smoke, moteKind == MoteKind.RiftSparks);
                r.sortingOrder = p.Smoke ? 0 : 1;
                particles[i] = p;
            }

            if (moteKind != MoteKind.None)
                UpdateMotes(centre, half, time, dt);
        }

        void Spawn(Emitter e)
        {
            if (alive >= MaxParticles)
                return;
            var smokeEvery = FireFxRules.SmokeEvery(e.Kind);
            var smoke = smokeEvery > 0 && e.Count % smokeEvery == 0;
            var width = FireFxRules.Width(e.Kind);
            var small = e.Kind == FireKind.Candles || e.Kind == FireKind.Lantern;
            particles[alive++] = new Particle
            {
                At = e.At + new Vector2(Random.Range(-width, width), Random.Range(-0.03f, 0.05f) + (smoke ? 0.2f : 0f)),
                Velocity = new Vector2(Random.Range(-0.12f, 0.12f), smoke ? Random.Range(0.35f, 0.55f) : small ? Random.Range(0.25f, 0.5f) : Random.Range(0.6f, 1.2f)),
                Life = smoke ? Random.Range(1.1f, 1.7f) : small ? Random.Range(0.35f, 0.6f) : Random.Range(0.45f, 0.95f),
                Sway = smoke ? 0.08f : 0.04f,
                Phase = Random.value * 6.28f,
                Smoke = smoke,
                Hell = e.Kind == FireKind.Hellfire,
            };
        }

        // Fireflies blink and wander; dust drifts up. Both wrap round the view, so they are always about her.
        void UpdateMotes(Vector2 centre, Vector2 half, float time, float dt)
        {
            for (var i = 0; i < Motes; i++)
            {
                var m = motes[i];
                var p = (Vector2)m.transform.position;
                if (!m.enabled)
                {
                    p = centre + new Vector2(AmbienceRules.Spread(i, 1) * half.x, AmbienceRules.Spread(i, 2) * half.y);
                    m.enabled = true;
                }
                p += (moteDrift[i] + new Vector2(Mathf.Sin(time * 0.6f + motePhase[i] * 9f) * 0.1f, Mathf.Cos(time * 0.45f + motePhase[i] * 7f) * 0.05f)) * dt;
                p = AmbienceRules.Wrap(p, centre, half);
                m.transform.position = p;
                if (moteKind == MoteKind.Fireflies)
                    m.color = FireFxRules.FireflyLit(time, motePhase[i]) ? new Color32(214, 246, 110, 255) : new Color32(214, 246, 110, 0);
                else if (moteKind == MoteKind.RiftSparks)
                    m.color = FireFxRules.FireflyLit(time * 1.3f, motePhase[i]) ? new Color32(206, 150, 255, 255) : new Color32(110, 60, 190, 150);
                else if (moteKind == MoteKind.Embers)
                    // Even ones are embers (orange, rising, glowing and going out), odd ones grey ash sinking.
                    m.color = i % 2 == 0 ? (FireFxRules.FireflyLit(time * 1.6f, motePhase[i]) ? new Color32(255, 150, 50, 255) : new Color32(160, 60, 30, 200))
                        : new Color32(150, 146, 152, 140);
                else
                    m.color = new Color32(200, 196, 210, (byte)(i % 2 == 0 ? 70 : 40));
            }
        }
    }
}
