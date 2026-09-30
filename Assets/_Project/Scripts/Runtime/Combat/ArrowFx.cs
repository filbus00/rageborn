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
    /// </summary>
    public sealed class ArrowFx
    {
        // More than this many particles at once and new ones are skipped: effects must never cost the frame.
        const int MaxParticles = 450;

        // A droplet's fall: world units a second squared, and how long its splash lies on the floor.
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
        }

        readonly Transform root;
        readonly List<Particle> live = new List<Particle>(MaxParticles);
        readonly Stack<SpriteRenderer> spare = new Stack<SpriteRenderer>(64);

        public ArrowFx(Transform parent)
        {
            var go = new GameObject("Arrow Effects");
            go.transform.SetParent(parent, false);
            root = go.transform;
        }

        public int Count => live.Count;

        // --- Emitters ---------------------------------------------------------------------------------------------

        /// <summary>A puff of the arrow's glow left behind it; a line of them reads as a trail.</summary>
        public void Trail(Vector2 ground, Vector2 direction, float height, Color color, float size)
        {
            Emit(ground, -direction * 0.3f, height, 0f, 0.22f, size, size * 0.3f, 1.8f, color, Faded(color), false, false);
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
            var count = Mathf.RoundToInt(5f * amount);
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
                        // Landed: the drop becomes a splash on the floor, which dries away.
                        p.Height = 0f;
                        p.Falls = false;
                        p.Flat = true;
                        p.Velocity = Vector2.zero;
                        p.Age = 0f;
                        p.Life = SplatSeconds * Random.Range(0.7f, 1.2f);
                        p.Size0 = p.Size1 = p.Size0 * Random.Range(1.4f, 2.4f);
                        p.Stretch = 1f;
                        p.Color0 = new Color(BloodDark.r, BloodDark.g, BloodDark.b, 0.9f);
                        p.Color1 = new Color(BloodDark.r, BloodDark.g, BloodDark.b, 0f);
                        p.Renderer.sprite = TelegraphArt.Disc;
                        Layer(p.Renderer, true);
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
