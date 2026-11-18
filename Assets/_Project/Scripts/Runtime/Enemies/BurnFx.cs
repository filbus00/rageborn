using UnityEngine;

namespace ARPG
{
    /// <summary>The flames on a burning enemy (2026-11-18, the owner: "add particle effects to show enemies burning when
    /// using fire arrows"): how many a second, how wide, how high. Pure.</summary>
    public static class BurnFxRules
    {
        /// <summary>Flame pixels a second for an enemy with this many burn stacks (none when it is not burning): the more
        /// stacks, the fiercer.</summary>
        public static float Rate(int stacks) => stacks <= 0 ? 0f : 9f + 5f * Mathf.Min(stacks, BurnStacks.MaxStacks);

        /// <summary>How far to either side of its middle the flames start, world units: about a third of its body.</summary>
        public static float Width(float visualScale) => 0.12f * Mathf.Max(0.5f, visualScale);

        /// <summary>One flame pixel in this many is a puff of smoke instead.</summary>
        public const int SmokeEvery = 7;

        /// <summary>The flare when a fire catches: this many pixels thrown at once.</summary>
        public const int IgniteBurst = 10;
    }

    /// <summary>
    /// Pixel flames and smoke rising off every burning enemy in view, in the forest fires' own colours and steps
    /// (<see cref="FireFxRules"/>), pooled, nothing made after the first frame. Fire arrows, the ignite chance and the
    /// boons that burn all set <see cref="EnemyController.IsBurning"/>, so they all show; a flare leaps off the enemy
    /// when the fire first catches (<see cref="Ignite"/>). Drawn only: it changes nothing in the fight. Created from code
    /// before the first scene and kept across scenes.
    /// </summary>
    public class BurnFx : MonoBehaviour
    {
        const int MaxParticles = 260;

        struct Particle
        {
            public Vector2 At;
            public Vector2 Velocity;
            public float Age, Life, Sway, Phase;
            public bool Smoke;
        }

        static BurnFx instance;

        readonly Particle[] particles = new Particle[MaxParticles];
        readonly SpriteRenderer[] renderers = new SpriteRenderer[MaxParticles];
        int alive;
        EnemyManager manager;
        Camera view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Burn Fx");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BurnFx>();
        }

        void Awake()
        {
            var unlit = SpriteMaterials.Unlit;
            for (var i = 0; i < MaxParticles; i++)
            {
                var go = new GameObject("Flame", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<SpriteRenderer>();
                r.sprite = FireFx.Pixel;
                r.sortingLayerName = GameSortingLayers.Effects;
                if (unlit != null)
                    r.sharedMaterial = unlit;
                r.enabled = false;
                renderers[i] = r;
            }
        }

        /// <summary>A fire catching on an enemy: a flare of flame pixels off its body.</summary>
        public static void Ignite(EnemyController enemy)
        {
            if (instance == null || enemy == null)
                return;
            for (var i = 0; i < BurnFxRules.IgniteBurst; i++)
                instance.Spawn(enemy, i % BurnFxRules.SmokeEvery == BurnFxRules.SmokeEvery - 1, 1.6f);
        }

        void Update()
        {
            if (view == null)
                view = Camera.main;
            if (manager == null)
                manager = FindAnyObjectByType<EnemyManager>();
            var dt = Time.deltaTime;
            var time = Time.time;
            if (view != null && manager != null)
            {
                var centre = (Vector2)view.transform.position;
                var half = new Vector2(view.orthographicSize * view.aspect + 1f, view.orthographicSize + 2f);
                var active = manager.Active;
                for (var i = 0; i < active.Count; i++)
                {
                    var enemy = active[i];
                    if (enemy == null || !enemy.IsAlive || !enemy.IsBurning)
                        continue;
                    var at = Elevation.World2(enemy.GroundPosition);
                    if (Mathf.Abs(at.x - centre.x) > half.x || Mathf.Abs(at.y - centre.y) > half.y)
                        continue;
                    // The expected number this frame, whole ones for sure and the rest by chance (no state to keep).
                    var expected = BurnFxRules.Rate(enemy.BurnStackCount) * dt;
                    var count = Mathf.FloorToInt(expected);
                    if (Random.value < expected - count)
                        count++;
                    for (var k = 0; k < count; k++)
                        Spawn(enemy, Random.Range(0, BurnFxRules.SmokeEvery) == 0, 1f);
                }
            }

            for (var i = alive - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    alive--;
                    particles[i] = particles[alive];
                    renderers[alive].enabled = false;
                    continue;
                }
                p.At += p.Velocity * dt;
                var age = p.Age / p.Life;
                var drawn = p.At + new Vector2(Mathf.Sin(time * 6f + p.Phase) * p.Sway, 0f);
                var r = renderers[i];
                r.enabled = true;
                r.transform.position = FireFxRules.Snap(drawn);
                var size = FireFxRules.Size(age, p.Smoke);
                r.transform.localScale = new Vector3(size, size, 1f);
                r.color = FireFxRules.Colour(age, false, p.Smoke);
                r.sortingOrder = p.Smoke ? 0 : 1;
                particles[i] = p;
            }
        }

        // One pixel off an enemy's body: from anywhere across it, rising, smoke from higher up.
        void Spawn(EnemyController enemy, bool smoke, float spread)
        {
            if (alive >= MaxParticles)
                return;
            var body = Elevation.World2(enemy.GroundPosition);
            var width = BurnFxRules.Width(enemy.Definition.VisualScale) * spread;
            var height = enemy.FlameHeight;
            particles[alive++] = new Particle
            {
                At = body + new Vector2(Random.Range(-width, width), smoke ? height * Random.Range(0.8f, 1.1f) : Random.Range(0.05f, height * 0.85f)),
                Velocity = new Vector2(Random.Range(-0.1f, 0.1f) * spread, smoke ? Random.Range(0.3f, 0.5f) : Random.Range(0.5f, 1.0f) * spread),
                Life = smoke ? Random.Range(0.9f, 1.4f) : Random.Range(0.3f, 0.65f),
                Sway = smoke ? 0.07f : 0.03f,
                Phase = Random.value * 6.28f,
                Smoke = smoke,
            };
        }
    }
}
