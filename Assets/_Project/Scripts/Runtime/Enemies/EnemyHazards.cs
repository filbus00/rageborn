using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Ground hazards that enemies leave behind (2026-10-05, the rest of act 1's roster, Docs/05): a blast that fills
    /// and then hits whoever is inside once (the Carrion Bloat's burst), and burning ground that hurts the player a few
    /// times a second while she stands in it (the Ember Acolyte's fire circle, after its fill). Owned and ticked by
    /// <see cref="EnemyManager"/>; markers are pooled, so a fight makes no objects after its first hazards.
    /// </summary>
    public sealed class EnemyHazards
    {
        // The burning ground's look: ember orange, fainter than a telegraph.
        static readonly Color FireColor = new Color(1f, 0.45f, 0.1f, 0.6f);
        static readonly Color BlastColor = new Color(1f, 0.22f, 0.12f, 0.95f);
        static readonly Color PoisonColor = new Color(0.45f, 0.85f, 0.25f, 0.6f);
        public static readonly Color VoidColor = new Color(0.6f, 0.3f, 0.85f, 0.9f);
        const float BurnPulseSeconds = 0.5f;

        sealed class Hazard
        {
            public GroundMarker Marker;
            public Vector2 Center;
            public float Radius;
            public float FillLeft;
            public float BurnLeft;
            public float Damage;
            public float BurnDamagePerSecond;
            public float PulseTimer;
            public int Level;
            public bool Active;
            public float SlowMultiplier;
            public float SlowSeconds;
            /// <summary>A sound when it goes off, heard whether it hits or not (the Frost Nova, 2026-10-12).</summary>
            public SoundId? Sound;
            public Color Color;
            public bool Pooled;
            /// <summary>How long the pool lies before it starts to hurt (regular enemies' pools, 2026-10-18).</summary>
            public float GraceLeft;

            public Hazard Delay(float seconds)
            {
                GraceLeft = Mathf.Max(0f, seconds);
                return this;
            }
        }

        // A thin bright ring that flies out where an attack lands and fades (2026-10-18).
        sealed class Shock
        {
            public SpriteRenderer Renderer;
            public Vector2 Center;
            public float Radius;
            public float Time;
            public Color Color;
        }

        const float ShockSeconds = 0.3f;
        readonly List<Shock> shocks = new List<Shock>();

        void ShockAt(Vector2 center, float radius, Color color)
        {
            Shock shock = null;
            foreach (var s in shocks)
                if (s.Time >= 1f)
                {
                    shock = s;
                    break;
                }
            if (shock == null)
            {
                shock = new Shock { Renderer = GroundMarker.NewSprite("Shock", TelegraphArt.Shock, color, parent, 44).GetComponent<SpriteRenderer>() };
                shocks.Add(shock);
            }
            shock.Center = center;
            shock.Radius = radius;
            shock.Time = 0f;
            shock.Color = Color.Lerp(color, Color.white, 0.5f);
            shock.Renderer.gameObject.SetActive(true);
            shock.Renderer.transform.position = Elevation.World2(center);
            // Sized and coloured now: a shock made during a tick is first ticked next frame, and until then a new one
            // drew one unit across at full colour, a reused one at its last size.
            var r = radius * 0.85f;
            shock.Renderer.transform.localScale = new Vector3(r * 2f, r * 2f * IsoMath.GroundSquash, 1f);
            shock.Renderer.color = shock.Color;
        }

        void TickShocks(float deltaTime)
        {
            foreach (var s in shocks)
            {
                if (s.Time >= 1f)
                    continue;
                s.Time += deltaTime / ShockSeconds;
                if (s.Time >= 1f)
                {
                    s.Renderer.gameObject.SetActive(false);
                    continue;
                }
                var r = s.Radius * (0.85f + 0.45f * s.Time);
                s.Renderer.transform.localScale = new Vector3(r * 2f, r * 2f * IsoMath.GroundSquash, 1f);
                s.Renderer.color = new Color(s.Color.r, s.Color.g, s.Color.b, s.Color.a * (1f - s.Time));
            }
        }

        sealed class LineHazard
        {
            public GroundMarker Marker;
            public Vector2 From;
            public Vector2 To;
            public float Width;
            public Color Color;
            public float Damage;
            public int Level;
            public float SlowMultiplier;
            public float SlowSeconds;
            public bool Active;
        }

        readonly Transform parent;
        readonly List<Hazard> hazards = new List<Hazard>();
        readonly List<LineHazard> lines = new List<LineHazard>();

        public EnemyHazards(Transform parent) => this.parent = parent;

        /// <summary>A circle that fills over <paramref name="fillSeconds"/>, then deals <paramref name="damage"/> (already
        /// times the enemy's multipliers) to the player if she is inside. Not dodgeable: it is a ground shape.</summary>
        public void Blast(Vector2 center, float radius, float fillSeconds, float damage, int level) =>
            Add(center, radius, fillSeconds, 0f, damage, 0f, level, BlastColor);

        /// <summary>A blast in another colour (the Rift Caller's void pulses).</summary>
        public void Blast(Vector2 center, float radius, float fillSeconds, float damage, int level, Color color) =>
            Add(center, radius, fillSeconds, 0f, damage, 0f, level, color);

        /// <summary>Burning ground for <paramref name="seconds"/>, dealing this much a second in pulses while the player
        /// stands in it.</summary>
        public void Fire(Vector2 center, float radius, float seconds, float damagePerSecond, int level, float poolDelay = 0f) =>
            Add(center, radius, 0f, seconds, 0f, damagePerSecond, level, FireColor).Delay(poolDelay);

        /// <summary>
        /// A circle that fills, strikes once (slowing her when <paramref name="slowMultiplier"/> is set), then burns on
        /// for <paramref name="burnSeconds"/> (elite affixes, 2026-10-08: Desecrator, Frost Nova).
        /// </summary>
        public void Strike(Vector2 center, float radius, float fillSeconds, float damage, float burnSeconds, float burnPerSecond, int level,
            Color color, float slowMultiplier = 0f, float slowSeconds = 0f, SoundId? sound = null, float poolDelay = 0f)
        {
            var hazard = Add(center, radius, fillSeconds, burnSeconds, damage, burnPerSecond, level, color);
            if (burnSeconds > 0f)
                hazard.Delay(poolDelay);
            hazard.SlowMultiplier = slowMultiplier;
            hazard.SlowSeconds = slowSeconds;
            hazard.Sound = sound;
        }

        /// <summary>A poison pool (Plagued): it spreads over <paramref name="fillSeconds"/> without hitting, then hurts
        /// while she stands in it.</summary>
        public void Poison(Vector2 center, float radius, float fillSeconds, float seconds, float damagePerSecond, int level) =>
            Add(center, radius, fillSeconds, seconds, 0f, damagePerSecond, level, PoisonColor);

        /// <summary>A poison pool in a colour that also strikes when it lands (the enemies' specials, 2026-10-11).</summary>
        public void Poison(Vector2 center, float radius, float fillSeconds, float seconds, float damagePerSecond, int level, Color color, float landDamage,
            float poolDelay = 0f) =>
            Add(center, radius, fillSeconds, seconds, landDamage, damagePerSecond, level, color).Delay(poolDelay);

        /// <summary>
        /// A lane from <paramref name="from"/> to <paramref name="to"/>, <paramref name="width"/> wide, that fills and
        /// then strikes her if she stands in it (the enemies' thrown and breathed specials, 2026-10-11).
        /// </summary>
        public void Line(Vector2 from, Vector2 to, float width, float fillSeconds, float damage, int level, Color color, float slowMultiplier = 0f, float slowSeconds = 0f)
        {
            LineHazard line = null;
            foreach (var l in lines)
                if (!l.Active)
                {
                    line = l;
                    break;
                }
            if (line == null)
            {
                line = new LineHazard { Marker = GroundMarker.Line(from, to, width, Mathf.Max(fillSeconds, 0.01f), color, parent) };
                lines.Add(line);
            }
            line.Marker.SetColor(color);
            line.Marker.RestartLine(from, to, width, Mathf.Max(fillSeconds, 0.01f));
            line.From = from;
            line.To = to;
            line.Width = width;
            line.Color = color;
            line.Damage = damage;
            line.Level = level;
            line.SlowMultiplier = slowMultiplier;
            line.SlowSeconds = slowSeconds;
            line.Active = true;
        }

        void TickLines(float deltaTime, EnemyManager world)
        {
            foreach (var line in lines)
            {
                if (!line.Active)
                    continue;
                line.Marker.Advance(deltaTime);
                if (!line.Marker.Done)
                    continue;
                line.Active = false;
                line.Marker.Hide();
                if (line.Damage > 0f)
                    LaneShock(line);
                // A line that only warns (a throw's aim line) does nothing when it fills.
                if (line.Damage <= 0f)
                    continue;
                Sfx.Play(SoundId.EnemySlam, 0.5f);
                var player = world.Player;
                if (player == null || !player.IsAlive ||
                    EliteAffixRules.DistanceToSegment(world.PlayerGround, line.From, line.To) > line.Width / 2f)
                    continue;
                player.TakeHit(line.Damage, line.Level, 0f, dodgeable: false);
                if (line.SlowMultiplier > 0f)
                    world.PlayerController?.ApplySlow(line.SlowMultiplier, line.SlowSeconds);
            }
        }

        /// <summary>The warning symbols round her (set by the manager): every circle shows one while she is near it
        /// (2026-10-19).</summary>
        public IncomingWarnings Warnings { get; set; }

        /// <summary>How long a pool that hurts at once still warns (burning ground, which has no fill).</summary>
        const float LeastWarningSeconds = 0.6f;

        Hazard Add(Vector2 center, float radius, float fill, float burn, float damage, float burnPerSecond, int level, Color color)
        {
            Warnings?.WarnPool(center, radius, Mathf.Max(fill, LeastWarningSeconds), color);
            Hazard hazard = null;
            foreach (var h in hazards)
                if (!h.Active && h.Marker != null)
                {
                    hazard = h;
                    break;
                }
            if (hazard == null)
            {
                hazard = new Hazard { Marker = GroundMarker.Circle(center, radius, Mathf.Max(fill, 0.01f), color, parent) };
                hazards.Add(hazard);
            }
            hazard.Marker.SetColor(color);
            hazard.Marker.RestartCircle(center, radius, Mathf.Max(fill, 0.01f));
            // A reused hazard forgets the last one's sound (Strike sets its own).
            hazard.Sound = null;
            if (fill <= 0f)
                hazard.Marker.Advance(1f);
            hazard.Center = center;
            hazard.Radius = radius;
            hazard.FillLeft = fill;
            hazard.BurnLeft = burn;
            hazard.Damage = damage;
            hazard.BurnDamagePerSecond = burnPerSecond;
            hazard.PulseTimer = 0f;
            hazard.Level = level;
            hazard.Active = true;
            hazard.SlowMultiplier = 0f;
            hazard.SlowSeconds = 0f;
            hazard.Color = color;
            hazard.Pooled = false;
            hazard.GraceLeft = 0f;
            return hazard;
        }

        public void Tick(float deltaTime, EnemyManager world)
        {
            TickLines(deltaTime, world);
            TickShocks(deltaTime);
            for (var i = 0; i < hazards.Count; i++)
            {
                var h = hazards[i];
                if (!h.Active)
                    continue;
                var player = world.Player;
                var inside = player != null && player.IsAlive && Vector2.Distance(world.PlayerGround, h.Center) <= h.Radius;
                if (h.FillLeft > 0f)
                {
                    h.FillLeft -= deltaTime;
                    h.Marker.Advance(deltaTime);
                    if (h.FillLeft > 0f)
                        continue;
                    if (h.Sound.HasValue)
                        Sfx.Play(h.Sound.Value, 0.8f);
                    if (h.Damage > 0f)
                        ShockAt(h.Center, h.Radius, h.Color);
                    if (inside && h.Damage > 0f)
                    {
                        Sfx.Play(SoundId.EnemySlam, 0.6f);
                        player.TakeHit(h.Damage, h.Level, 0f, dodgeable: false);
                        if (h.SlowMultiplier > 0f)
                            world.PlayerController?.ApplySlow(h.SlowMultiplier, h.SlowSeconds);
                    }
                    if (h.BurnLeft <= 0f)
                    {
                        Finish(h);
                        continue;
                    }
                }
                // What lasts after landing is drawn as a pool (2026-10-18).
                if (!h.Pooled)
                {
                    h.Pooled = true;
                    h.Marker.SetPool();
                }
                h.Marker.Animate(deltaTime);
                // The pool lies harmless for its grace, then hurts for its full time.
                if (h.GraceLeft > 0f)
                {
                    h.GraceLeft -= deltaTime;
                    continue;
                }
                h.BurnLeft -= deltaTime;
                h.PulseTimer -= deltaTime;
                if (inside && h.PulseTimer <= 0f)
                {
                    h.PulseTimer = BurnPulseSeconds;
                    player.TakeHit(h.BurnDamagePerSecond * BurnPulseSeconds, h.Level, 0f, dodgeable: false);
                }
                if (h.BurnLeft <= 0f)
                    Finish(h);
            }
        }

        static void Finish(Hazard h)
        {
            h.Active = false;
            h.Marker.Hide();
        }

        // A lane that strikes flashes along its length as it goes off.
        void LaneShock(LineHazard line)
        {
            var length = Vector2.Distance(line.From, line.To);
            var steps = Mathf.Clamp(Mathf.CeilToInt(length / 1.5f), 1, 6);
            for (var k = 0; k <= steps; k++)
                ShockAt(Vector2.Lerp(line.From, line.To, (float)k / steps), line.Width * 0.7f, line.Color);
        }

        /// <summary>Clears every hazard (a new level).</summary>
        public void Clear()
        {
            foreach (var h in hazards)
                Finish(h);
            foreach (var s in shocks)
            {
                s.Time = 1f;
                s.Renderer.gameObject.SetActive(false);
            }
            foreach (var l in lines)
            {
                l.Active = false;
                l.Marker.Hide();
            }
        }
    }
}
