using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The arrow effects (the owner, 2026-09-30: "add some cool arrow effects, add a blood trail for piercing arrows"):
    /// glowing trails behind arrows, sparks where they strike, arrows left quivering in walls, the explosive arrow's burst,
    /// and blood: a spray where an arrow hits flesh, and a piercing arrow that has gone through an enemy drips a trail of
    /// blood that splashes on the floor and dries away. A pool of plain sprites, made on first need and reused, ticked by
    /// <see cref="PlayerCombat"/>, so a fight allocates nothing once the pool has grown. All numbers are tuning.
    ///
    /// More gore (2026-10-11, the owner: "add more gore and blood. Make sure that the monster bleed and that the blood
    /// splashes when they are hit"): every hit on a monster splashes (<see cref="Wound"/>, from
    /// <see cref="EnemyController.TakeDamage"/>), wounded and bleeding monsters drip as they go (<see cref="Drip"/>), a
    /// kill bursts (<see cref="Gore"/>: a spray all round, chunks and a pool), and blood that lands stays on the floor as
    /// a pixel splat (<see cref="BloodArt"/>) for half a minute, the oldest giving way when <see cref="MaxStains"/> lie.
    /// </summary>
    public sealed class ArrowFx
    {
        /// <summary>The level's effects, for the monsters' own blood; null between levels.</summary>
        public static ArrowFx Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        // Blood on the floor: at most this many splats, each lying this long and fading over its last seconds.
        const int MaxStains = 260;
        const float StainSeconds = 30f;
        const float StainFadeSeconds = 6f;

        // More than this many particles at once and new ones are skipped: effects must never cost the frame.
        // 600 since 2026-10-18: a lightning bolt draws its strokes from the same pool.
        const int MaxParticles = 600;

        // A droplet's fall, world units a second squared; and how long a scorch lies on the floor.
        const float Gravity = 9f;
        const float SplatSeconds = 4f;

        public static readonly Color BloodColor = new Color(0.55f, 0.03f, 0.04f, 1f);
        static readonly Color BloodDark = new Color(0.3f, 0.01f, 0.02f, 0.85f);
        static readonly Color SparkColor = new Color(1f, 0.85f, 0.55f, 1f);

        struct Particle
        {
            public SpriteRenderer Renderer;
            public Vector2 Ground;      // the point on the floor under it, in world space
            public Vector2 Velocity;    // along the floor, world units a second
            public float Height;        // above the floor, world units
            public float HeightVelocity;
            public float Age;
            public float Life;
            public float Size0;
            public float Size1;
            public float Stretch;       // longer than wide along its velocity (a streak), 1 for round
            public Color Color0;
            public Color Color1;
            public bool Falls;          // a droplet: gravity, and a splash on the floor when it lands
            public bool Flat;           // lies on the floor (a splash, a scorch)
            public bool Chunk;          // a chunk of flesh: lands as itself, not a splat
        }

        struct Stain
        {
            public SpriteRenderer Renderer;
            public float Age;
            public float Alpha;
        }

        readonly Stain[] stains = new Stain[MaxStains];
        int nextStain;

        readonly Transform root;
        readonly List<Particle> live = new List<Particle>(MaxParticles);
        readonly Stack<SpriteRenderer> spare = new Stack<SpriteRenderer>(64);

        public ArrowFx(Transform parent)
        {
            var go = new GameObject("Arrow Effects");
            go.transform.SetParent(parent, false);
            root = go.transform;
            Current = this;
        }

        /// <summary>Lets go of <see cref="Current"/> when its level ends.</summary>
        public void Release()
        {
            if (Current == this)
                Current = null;
        }

        public int Count => live.Count;

        // --- Emitters ---------------------------------------------------------------------------------------------

        /// <summary>A puff of the arrow's glow left behind it; a line of them reads as a trail.</summary>
        public void Trail(Vector2 ground, Vector2 direction, float height, Color color, float size)
        {
            Emit(ground, -direction * 0.3f, height, 0f, 0.22f, size, size * 0.3f, 1.8f, color, Faded(color), false, false);
        }

        // --- The elemental arrows (2026-10-18) --------------------------------------------------------------------

        static readonly Color FlameHot = new Color(1f, 0.85f, 0.35f, 1f);
        static readonly Color FlameRed = new Color(0.9f, 0.25f, 0.05f, 0f);
        static readonly Color FrostWhite = new Color(0.9f, 0.97f, 1f, 1f);
        static readonly Color FrostBlue = new Color(0.45f, 0.75f, 1f, 0f);
        static readonly Color ArcWhite = new Color(0.95f, 0.97f, 1f, 1f);
        static readonly Color ArcBlue = new Color(0.45f, 0.6f, 1f, 0f);

        /// <summary>A fire arrow's trail: an ember shaken off that rises and goes out.</summary>
        public void FireMote(Vector2 ground, Vector2 direction, float height)
        {
            Emit(ground + Random.insideUnitCircle * 0.05f, -direction * 0.4f + Random.insideUnitCircle * 0.3f, height, Random.Range(0.6f, 1.4f),
                Random.Range(0.25f, 0.45f), Random.Range(0.06f, 0.1f), 0.02f, 1f, FlameHot, FlameRed, false, false);
        }

        /// <summary>An ice arrow's trail: a glint of frost hanging in the air a moment.</summary>
        public void FrostMote(Vector2 ground, Vector2 direction, float height)
        {
            Emit(ground + Random.insideUnitCircle * 0.08f, -direction * 0.15f, height + Random.Range(-0.05f, 0.05f), -0.2f,
                Random.Range(0.3f, 0.5f), 0.05f, Random.Range(0.08f, 0.12f), 1f, FrostWhite, FrostBlue, false, false);
        }

        /// <summary>A lightning arrow's trail: a crackling spark jumping off it.</summary>
        public void ArcMote(Vector2 ground, Vector2 direction, float height)
        {
            var jump = Rotate(direction, Random.Range(60f, 120f) * (Random.value < 0.5f ? 1f : -1f)) * Random.Range(1.5f, 3f);
            Emit(ground, jump, height, Random.Range(-0.5f, 0.5f), Random.Range(0.06f, 0.12f), 0.07f, 0.02f, 3f, ArcWhite, ArcBlue, false, false);
        }

        /// <summary>A fire arrow setting one burning: a flare of flame and embers rising round it.</summary>
        public void Flames(Vector2 ground, float height)
        {
            Emit(ground, Vector2.zero, height, 0.6f, 0.3f, 0.25f, 0.55f, 1f, new Color(1f, 0.6f, 0.15f, 0.85f), FlameRed, false, false);
            for (var i = 0; i < 9; i++)
            {
                var drift = Rotate(Vector2.right, Random.Range(0f, 360f)) * Random.Range(0.3f, 1.2f);
                drift.y *= IsoMath.GroundSquash;
                Emit(ground, drift, height * Random.Range(0.4f, 1f), Random.Range(1.2f, 2.6f), Random.Range(0.35f, 0.7f),
                    Random.Range(0.06f, 0.11f), 0.02f, 1f, Color.Lerp(FlameHot, new Color(1f, 0.45f, 0.1f, 1f), Random.value), FlameRed, false, false);
            }
        }

        /// <summary>A frost arrow chilling one: shards of ice thrown out and a pale breath of cold.</summary>
        public void FrostShards(Vector2 ground, Vector2 direction, float height, float amount)
        {
            Emit(ground, Vector2.zero, height, 0f, 0.3f, 0.2f * amount, 0.5f * amount, 1f, new Color(0.75f, 0.9f, 1f, 0.6f), FrostBlue, false, false);
            var count = Mathf.RoundToInt(7f * amount);
            for (var i = 0; i < count; i++)
            {
                var spray = Rotate(-direction, Random.Range(-80f, 80f)) * Random.Range(1.5f, 3.5f) * Mathf.Sqrt(amount);
                Emit(ground, spray, height, Random.Range(0.5f, 2f), Random.Range(0.25f, 0.45f), Random.Range(0.06f, 0.1f), 0.02f, 3f,
                    FrostWhite, FrostBlue, true, false);
            }
        }

        /// <summary>
        /// A bolt of lightning between two points of ground at two heights: a jagged white-blue line of sparks, bright
        /// where it starts and ends. <paramref name="fromHeight"/> high above for a thunderbolt from the sky.
        /// </summary>
        public void Lightning(Vector2 from, Vector2 to, float fromHeight, float toHeight)
        {
            // Sharp corners every 0.4 units or so, each knocked sideways at random (never the ends), drawn as solid
            // strokes of close sparks with a soft glow round them; now and then a short fork off a corner.
            var length = Vector2.Distance(from, to) + Mathf.Abs(fromHeight - toHeight);
            var corners = Mathf.Clamp(Mathf.CeilToInt(length / 0.4f), 2, 14);
            var across = (to - from).sqrMagnitude > 1e-6f ? new Vector2(-(to - from).y, (to - from).x).normalized : Vector2.right;
            var previous = from;
            var previousHeight = fromHeight;
            for (var c = 1; c <= corners; c++)
            {
                var t = (float)c / corners;
                var kick = c == corners ? 0f : Random.Range(-0.28f, 0.28f);
                var point = Vector2.Lerp(from, to, t) + across * kick;
                var height = Mathf.Lerp(fromHeight, toHeight, t) + (fromHeight != toHeight ? kick * 0.8f : 0f);
                Stroke(previous, previousHeight, point, height, 0.075f, ArcWhite);
                if (c < corners && Random.value < 0.25f)
                {
                    var fork = point + Rotate(to - from, Random.Range(-60f, 60f)).normalized * Random.Range(0.25f, 0.5f);
                    Stroke(point, height, fork, height - Random.Range(0f, 0.3f), 0.05f, new Color(0.8f, 0.88f, 1f, 0.8f));
                }
                previous = point;
                previousHeight = height;
            }
            Emit(to, Vector2.zero, toHeight, 0f, 0.15f, 0.3f, 0.6f, 1f, new Color(0.85f, 0.92f, 1f, 0.9f), ArcBlue, false, false);
        }

        // One straight stroke of a bolt: sparks close enough to read as a line, and a faint glow along it.
        void Stroke(Vector2 from, float fromHeight, Vector2 to, float toHeight, float size, Color color)
        {
            var length = Vector2.Distance(from, to) + Mathf.Abs(fromHeight - toHeight);
            var steps = Mathf.Max(2, Mathf.CeilToInt(length / 0.075f));
            for (var k = 0; k <= steps; k++)
            {
                var t = (float)k / steps;
                var point = Vector2.Lerp(from, to, t);
                var height = Mathf.Lerp(fromHeight, toHeight, t);
                Emit(point, Vector2.zero, height, 0f, Random.Range(0.09f, 0.15f), size, size * 0.5f, 1f, color, ArcBlue, false, false);
                if (k % 4 == 0)
                    Emit(point, Vector2.zero, height, 0f, 0.12f, size * 3.2f, size * 2f, 1f, new Color(0.5f, 0.65f, 1f, 0.22f), ArcBlue, false, false);
            }
        }

        /// <summary>Sparks and splinters where an arrow strikes a wall or armour.</summary>
        public void Sparks(Vector2 ground, Vector2 direction, float height, Color color, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var spread = Rotate(-direction, Random.Range(-70f, 70f)) * Random.Range(1.5f, 4f);
                Emit(ground, spread, height, Random.Range(0.5f, 2.5f), Random.Range(0.12f, 0.3f), 0.09f, 0.03f, 2.5f,
                    Color.Lerp(color, SparkColor, Random.value), Faded(SparkColor), false, false);
            }
        }

        /// <summary>A spray of blood from a body an arrow hit, thrown the way the arrow flew; the drops splash on the
        /// floor. <paramref name="amount"/> scales it: a kill or a piercing arrow sprays more.</summary>
        public void Blood(Vector2 ground, Vector2 direction, float height, float amount)
        {
            var count = Mathf.RoundToInt(8f * amount);
            for (var i = 0; i < count; i++)
            {
                var spray = Rotate(direction, Random.Range(-35f, 35f)) * Random.Range(0.8f, 3.2f) * Mathf.Sqrt(amount);
                Emit(ground, spray, height, Random.Range(0.5f, 2.2f), 1.2f, Random.Range(0.07f, 0.12f), 0.06f, 1.4f,
                    BloodColor, BloodDark, true, false);
            }
            // A dark mist hanging a moment where the arrow went in.
            Emit(ground, direction * 0.4f, height, 0f, 0.3f, 0.18f * amount, 0.34f * amount, 1f,
                new Color(BloodColor.r, BloodColor.g, BloodColor.b, 0.7f), Faded(BloodDark), false, false);
        }

        /// <summary>Any hit on a monster (<see cref="EnemyController.TakeDamage"/>): drops thrown every way, more the harder
        /// the hit (<paramref name="amount"/>, about 0.3 for a scratch to 1.5 for a heavy blow), and a splat under it.</summary>
        public void Wound(Vector2 ground, float height, float amount)
        {
            var count = Mathf.RoundToInt(3f + 5f * amount);
            for (var i = 0; i < count; i++)
            {
                var spray = Rotate(Vector2.right, Random.Range(0f, 360f)) * Random.Range(0.6f, 2.4f) * Mathf.Sqrt(amount);
                spray.y *= IsoMath.GroundSquash;
                Emit(ground, spray, height, Random.Range(0.8f, 2.6f), 1.5f, Random.Range(0.06f, 0.11f), 0.05f, 1.5f,
                    BloodColor, BloodDark, true, false);
            }
            AddStain(ground + Random.insideUnitCircle * 0.12f, 0.35f + 0.35f * Mathf.Min(amount, 1.5f), 0.85f);
        }

        /// <summary>A drop or two falling from a wounded or bleeding monster as it goes, leaving a trail.</summary>
        public void Drip(Vector2 ground, float height)
        {
            Emit(ground + Random.insideUnitCircle * 0.1f, Random.insideUnitCircle * 0.3f, height, Random.Range(-0.3f, 0.3f), 1f,
                Random.Range(0.05f, 0.08f), 0.05f, 1.2f, BloodColor, BloodDark, true, false);
        }

        /// <summary>A kill: blood thrown all round, chunks of flesh flung out, and a wide pool where it fell.
        /// <paramref name="size"/> is the monster's size (its visual scale): a boss bursts bigger.</summary>
        public void Gore(Vector2 ground, float height, float size)
        {
            var count = Mathf.RoundToInt(16f * size);
            for (var i = 0; i < count; i++)
            {
                var spray = Rotate(Vector2.right, Random.Range(0f, 360f)) * Random.Range(1f, 4f) * Mathf.Sqrt(size);
                spray.y *= IsoMath.GroundSquash;
                Emit(ground, spray, height, Random.Range(1.2f, 3.5f), 1.6f, Random.Range(0.07f, 0.14f), 0.05f, 1.6f,
                    BloodColor, BloodDark, true, false);
            }
            var chunkCount = Mathf.RoundToInt(Random.Range(4f, 7f) * Mathf.Sqrt(size));
            for (var i = 0; i < chunkCount; i++)
            {
                var fling = Rotate(Vector2.right, Random.Range(0f, 360f)) * Random.Range(1f, 3f) * Mathf.Sqrt(size);
                fling.y *= IsoMath.GroundSquash;
                EmitChunk(ground, fling, height, Random.Range(1.5f, 3.5f));
            }
            // The mist of the burst, and the pool spreading where it fell.
            Emit(ground, Vector2.zero, height, 0f, 0.35f, 0.3f * size, 0.7f * size, 1f,
                new Color(BloodColor.r, BloodColor.g, BloodColor.b, 0.75f), Faded(BloodDark), false, false);
            AddStain(ground, 1.1f * size, 0.95f);
            AddStain(ground + Random.insideUnitCircle * 0.3f * size, 0.7f * size, 0.9f);
        }

        /// <summary>The blood trail of a piercing arrow that has gone through a body: drops shaken off the shaft that
        /// fall and splash, and a thin red streak behind it.</summary>
        public void BloodTrail(Vector2 ground, Vector2 direction, float height)
        {
            Emit(ground, -direction * 0.2f, height, 0f, 0.28f, 0.11f, 0.04f, 2.6f,
                new Color(0.7f, 0.05f, 0.05f, 0.9f), Faded(BloodDark), false, false);
            if (Random.value < 0.7f)
                Emit(ground, direction * Random.Range(0.3f, 1.2f) + Random.insideUnitCircle * 0.4f, height,
                    Random.Range(-0.2f, 0.4f), 1f, Random.Range(0.05f, 0.09f), 0.05f, 1.2f, BloodColor, BloodDark, true, false);
        }

        /// <summary>The explosive arrow's burst: a flash, a ring of fire on the floor and embers flung out.</summary>
        public void Burst(Vector2 ground, float radiusWorld, Color color)
        {
            Emit(ground, Vector2.zero, 0.25f, 0f, 0.18f, radiusWorld * 0.6f, radiusWorld * 1.6f, 1f,
                new Color(1f, 0.95f, 0.8f, 0.9f), Faded(color), false, false);
            Emit(ground, Vector2.zero, 0f, 0f, 0.55f, radiusWorld * 1.2f, radiusWorld * 2.1f, 1f,
                new Color(color.r, color.g, color.b, 0.55f), Faded(color), false, true);
            for (var i = 0; i < 22; i++)
            {
                var angle = Random.Range(0f, 360f);
                var fling = Rotate(Vector2.right, angle) * Random.Range(2f, 6f) * radiusWorld / 2.5f;
                fling.y *= IsoMath.GroundSquash;
                Emit(ground, fling, 0.2f, Random.Range(1f, 4f), Random.Range(0.3f, 0.7f), Random.Range(0.08f, 0.16f), 0.03f,
                    2f, Color.Lerp(color, SparkColor, Random.value * 0.6f), Faded(new Color(0.3f, 0.05f, 0f, 1f)), false, false);
            }
            // Scorch on the floor, dark and slow to fade.
            Emit(ground, Vector2.zero, 0f, 0f, SplatSeconds, radiusWorld * 1.1f, radiusWorld * 1.15f, 1f,
                new Color(0.08f, 0.04f, 0.03f, 0.55f), new Color(0.08f, 0.04f, 0.03f, 0f), false, true);
        }

        /// <summary>Burning ground (Cinder-Stitched Jerkin): a glowing patch of embers on the floor that lasts a while.</summary>
        public void Fire(Vector2 ground, float size, float seconds)
        {
            Emit(ground, Vector2.zero, 0f, 0f, seconds, size * 0.8f, size, 1f,
                new Color(1f, 0.45f, 0.1f, 0.5f), new Color(0.5f, 0.1f, 0f, 0f), false, true);
            Embers(ground, size, 4);
        }

        /// <summary>A few embers rising from a patch of floor.</summary>
        public void Embers(Vector2 ground, float size, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var offset = Rotate(Vector2.right, Random.Range(0f, 360f)) * Random.Range(0f, size * 0.45f);
                offset.y *= IsoMath.GroundSquash;
                Emit(ground + offset, new Vector2(Random.Range(-0.2f, 0.2f), 0f), 0.05f, Random.Range(0.8f, 1.6f), Random.Range(0.4f, 0.8f),
                    Random.Range(0.05f, 0.09f), 0.02f, 1f, new Color(1f, 0.7f, 0.25f, 1f), new Color(0.6f, 0.1f, 0f, 0f), false, false);
            }
        }

        /// <summary>A dark pool of blood on the floor that lasts a while (Gallowsreach's trail: enemies who walk through
        /// it bleed).</summary>
        public void Pool(Vector2 ground, float size, float seconds)
        {
            Emit(ground, Vector2.zero, 0f, 0f, seconds, size * 0.6f, size, 1f,
                new Color(BloodDark.r, BloodDark.g, BloodDark.b, 0.75f), new Color(BloodDark.r, BloodDark.g, BloodDark.b, 0f), false, true);
        }

        /// <summary>An arrow left standing in a wall, fading after a moment.</summary>
        public void Stuck(Vector2 ground, Vector2 direction, float height, Sprite arrow)
        {
            var renderer = Take();
            if (renderer == null)
                return;
            renderer.sprite = arrow;
            Layer(renderer, false);
            live.Add(new Particle
            {
                Renderer = renderer,
                Ground = ground,
                Velocity = direction * 0.0001f,
                Height = height,
                Life = 1.6f,
                Size0 = 1f,
                Size1 = 1f,
                Stretch = 0f, // a real sprite keeps its own shape
                Color0 = Color.white,
                Color1 = new Color(1f, 1f, 1f, 0f),
            });
        }

        // --- The pool ---------------------------------------------------------------------------------------------

        void Emit(Vector2 ground, Vector2 velocity, float height, float heightVelocity, float life, float size0, float size1,
            float stretch, Color color0, Color color1, bool falls, bool flat)
        {
            var renderer = Take();
            if (renderer == null)
                return;
            renderer.sprite = flat ? TelegraphArt.Disc : TelegraphArt.Ember;
            Layer(renderer, flat);
            live.Add(new Particle
            {
                Renderer = renderer,
                Ground = ground,
                Velocity = velocity,
                Height = height,
                HeightVelocity = heightVelocity,
                Life = life,
                Size0 = size0,
                Size1 = size1,
                Stretch = stretch,
                Color0 = color0,
                Color1 = color1,
                Falls = falls,
                Flat = flat,
            });
        }

        void EmitChunk(Vector2 ground, Vector2 velocity, float height, float heightVelocity)
        {
            var renderer = Take();
            if (renderer == null)
                return;
            renderer.sprite = BloodArt.Chunk(Random.Range(0, 3));
            Layer(renderer, false);
            live.Add(new Particle
            {
                Renderer = renderer,
                Ground = ground,
                Velocity = velocity,
                Height = height,
                HeightVelocity = heightVelocity,
                Life = 2f,
                Size0 = 1f,
                Size1 = 1f,
                Stretch = 0f,
                Color0 = Color.white,
                Color1 = Color.white,
                Falls = true,
                Chunk = true,
            });
        }

        /// <summary>A splat of blood on the floor, <paramref name="size"/> world units across, lying a while. The oldest
        /// splat makes way when <see cref="MaxStains"/> lie.</summary>
        public void AddStain(Vector2 ground, float size, float alpha) => AddStain(ground, size, alpha, BloodArt.Splat(Random.Range(0, BloodArt.Variants)));

        void AddStain(Vector2 ground, float size, float alpha, Sprite sprite)
        {
            ref var stain = ref stains[nextStain];
            nextStain = (nextStain + 1) % MaxStains;
            if (stain.Renderer == null)
            {
                stain.Renderer = GroundMarker.NewSprite("Blood", sprite, Color.white, root, 0).GetComponent<SpriteRenderer>();
                stain.Renderer.sortingLayerName = GameSortingLayers.Decals;
                stain.Renderer.sortingOrder = 4;
            }
            var renderer = stain.Renderer;
            renderer.gameObject.SetActive(true);
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            stain.Age = 0f;
            stain.Alpha = alpha;
            var transform = renderer.transform;
            transform.position = new Vector3(ground.x, ground.y, 0f);
            transform.rotation = Quaternion.identity;
            // The splat sprite is 0.8 units across at scale 1; a random flip so neighbours differ.
            var scale = size / (BloodArt.SplatWidth / (float)PixelArt.PixelsPerUnit);
            if (sprite.rect.width < BloodArt.SplatWidth)
                scale = 1f;
            transform.localScale = new Vector3(Random.value < 0.5f ? -scale : scale, scale, 1f);
        }

        void TickStains(float deltaTime)
        {
            for (var i = 0; i < stains.Length; i++)
            {
                ref var stain = ref stains[i];
                if (stain.Renderer == null || !stain.Renderer.gameObject.activeSelf)
                    continue;
                stain.Age += deltaTime;
                if (stain.Age >= StainSeconds)
                {
                    stain.Renderer.gameObject.SetActive(false);
                    continue;
                }
                var left = StainSeconds - stain.Age;
                if (left < StainFadeSeconds)
                    stain.Renderer.color = new Color(1f, 1f, 1f, stain.Alpha * left / StainFadeSeconds);
            }
        }

        SpriteRenderer Take()
        {
            if (live.Count >= MaxParticles)
                return null;
            SpriteRenderer renderer;
            if (spare.Count > 0)
            {
                renderer = spare.Pop();
                renderer.gameObject.SetActive(true);
            }
            else
            {
                renderer = GroundMarker.NewSprite("Fx", TelegraphArt.Ember, Color.white, root, 0).GetComponent<SpriteRenderer>();
            }
            return renderer;
        }

        public void Tick(float deltaTime)
        {
            TickStains(deltaTime);
            for (var i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.Age += deltaTime;
                if (p.Age >= p.Life)
                {
                    Release(p.Renderer);
                    live[i] = live[live.Count - 1];
                    live.RemoveAt(live.Count - 1);
                    continue;
                }

                p.Ground += p.Velocity * deltaTime;
                if (p.Falls)
                {
                    p.HeightVelocity -= Gravity * deltaTime;
                    p.Height += p.HeightVelocity * deltaTime;
                    if (p.Height <= 0f)
                    {
                        // Landed: the drop becomes a splat on the floor, the chunk lies where it fell; both stay a while.
                        if (p.Chunk)
                            AddStain(p.Ground, 0f, 1f, p.Renderer.sprite);
                        else
                            AddStain(p.Ground, p.Size0 * Random.Range(2.2f, 3.4f), 0.9f);
                        Release(p.Renderer);
                        live[i] = live[live.Count - 1];
                        live.RemoveAt(live.Count - 1);
                        continue;
                    }
                }
                else
                {
                    p.Height += p.HeightVelocity * deltaTime;
                    p.HeightVelocity *= 1f - Mathf.Min(1f, 4f * deltaTime);
                }

                Draw(ref p);
                live[i] = p;
            }
        }

        void Draw(ref Particle p)
        {
            var t = p.Life > 0f ? p.Age / p.Life : 1f;
            var renderer = p.Renderer;
            var transform = renderer.transform;
            transform.position = new Vector3(p.Ground.x, p.Ground.y + p.Height, 0f);
            renderer.color = Color.Lerp(p.Color0, p.Color1, t);
            if (p.Stretch <= 0f)
            {
                // A sprite with its own shape (a stuck arrow): pointed the way it flew.
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.Velocity.y, p.Velocity.x) * Mathf.Rad2Deg);
                transform.localScale = Vector3.one;
                return;
            }
            var size = Mathf.Lerp(p.Size0, p.Size1, t);
            if (p.Flat)
            {
                transform.rotation = Quaternion.identity;
                transform.localScale = new Vector3(size, size * IsoMath.GroundSquash, 1f);
                return;
            }
            var speed = p.Velocity.magnitude;
            var stretch = speed > 0.05f ? p.Stretch : 1f;
            transform.rotation = speed > 0.05f ? Quaternion.Euler(0f, 0f, Mathf.Atan2(p.Velocity.y, p.Velocity.x) * Mathf.Rad2Deg) : Quaternion.identity;
            transform.localScale = new Vector3(size * stretch, size, 1f);
        }

        // A splash lies on the floor under everything standing; everything else flies over it.
        static void Layer(SpriteRenderer renderer, bool flat)
        {
            renderer.sortingLayerName = flat ? GameSortingLayers.Decals : GameSortingLayers.Effects;
            renderer.sortingOrder = flat ? 5 : 0;
        }

        void Release(SpriteRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            spare.Push(renderer);
        }

        static Color Faded(Color color) => new Color(color.r, color.g, color.b, 0f);

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            var c = Mathf.Cos(r);
            var s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }

    /// <summary>
    /// The arrow itself, drawn in code as pixel art at the game's 40 pixels a unit (Docs/09, 0.9) until it has painted
    /// art: a dark shaft, an iron head and red fletching, pointing right (+x) with its pivot at the tip's middle, so a
    /// rotation points it along the flight.
    /// </summary>
    public static class ArrowArt
    {
        const int Width = 22;
        const int Height = 5;

        static Sprite arrow;

        public static Sprite Arrow => arrow != null ? arrow : arrow = Make();

        static Sprite Make()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "Arrow",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var clear = new Color32(0, 0, 0, 0);
            var shaft = new Color32(92, 64, 40, 255);
            var shaftLight = new Color32(130, 96, 62, 255);
            var head = new Color32(196, 200, 206, 255);
            var headDark = new Color32(110, 114, 122, 255);
            var fletch = new Color32(170, 28, 30, 255);
            var fletchDark = new Color32(104, 14, 18, 255);
            var pixels = new Color32[Width * Height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = clear;
            void Set(int x, int y, Color32 c) => pixels[x + y * Width] = c;
            const int mid = Height / 2;
            // Shaft from the fletching to the head.
            for (var x = 2; x < Width - 4; x++)
                Set(x, mid, x % 3 == 0 ? shaftLight : shaft);
            // The head, a small barbed point at the right.
            Set(Width - 4, mid, headDark);
            Set(Width - 3, mid, head);
            Set(Width - 2, mid, head);
            Set(Width - 1, mid, head);
            Set(Width - 4, mid + 1, headDark);
            Set(Width - 4, mid - 1, headDark);
            Set(Width - 3, mid + 1, head);
            Set(Width - 3, mid - 1, head);
            // Fletching: two red vanes at the back.
            for (var x = 0; x < 5; x++)
            {
                Set(x, mid + 1, x % 2 == 0 ? fletch : fletchDark);
                Set(x, mid - 1, x % 2 == 0 ? fletch : fletchDark);
            }
            Set(0, mid + 2, fletchDark);
            Set(0, mid - 2, fletchDark);
            Set(1, mid + 2, fletch);
            Set(1, mid - 2, fletch);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable, and tracing its outline fails on iOS.
            return Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(1f, 0.5f), PixelArt.PixelsPerUnit, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
        }
    }
}
