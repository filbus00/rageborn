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
        }

        sealed class LineHazard
        {
            public GroundMarker Marker;
            public Vector2 From;
            public Vector2 To;
            public float Width;
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
        public void Fire(Vector2 center, float radius, float seconds, float damagePerSecond, int level) =>
            Add(center, radius, 0f, seconds, 0f, damagePerSecond, level, FireColor);

        /// <summary>
        /// A circle that fills, strikes once (slowing her when <paramref name="slowMultiplier"/> is set), then burns on
        /// for <paramref name="burnSeconds"/> (elite affixes, 2026-10-08: Desecrator, Frost Nova).
        /// </summary>
        public void Strike(Vector2 center, float radius, float fillSeconds, float damage, float burnSeconds, float burnPerSecond, int level,
            Color color, float slowMultiplier = 0f, float slowSeconds = 0f)
        {
            var hazard = Add(center, radius, fillSeconds, burnSeconds, damage, burnPerSecond, level, color);
            hazard.SlowMultiplier = slowMultiplier;
            hazard.SlowSeconds = slowSeconds;
        }

        /// <summary>A poison pool (Plagued): it spreads over <paramref name="fillSeconds"/> without hitting, then hurts
        /// while she stands in it.</summary>
        public void Poison(Vector2 center, float radius, float fillSeconds, float seconds, float damagePerSecond, int level) =>
            Add(center, radius, fillSeconds, seconds, 0f, damagePerSecond, level, PoisonColor);

        /// <summary>A poison pool in a colour that also strikes when it lands (the enemies' specials, 2026-10-11).</summary>
        public void Poison(Vector2 center, float radius, float fillSeconds, float seconds, float damagePerSecond, int level, Color color, float landDamage) =>
            Add(center, radius, fillSeconds, seconds, landDamage, damagePerSecond, level, color);

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

        Hazard Add(Vector2 center, float radius, float fill, float burn, float damage, float burnPerSecond, int level, Color color)
        {
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
            return hazard;
        }

        public void Tick(float deltaTime, EnemyManager world)
        {
            TickLines(deltaTime, world);
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

        /// <summary>Clears every hazard (a new level).</summary>
        public void Clear()
        {
            foreach (var h in hazards)
                Finish(h);
            foreach (var l in lines)
            {
                l.Active = false;
                l.Marker.Hide();
            }
        }
    }
}
