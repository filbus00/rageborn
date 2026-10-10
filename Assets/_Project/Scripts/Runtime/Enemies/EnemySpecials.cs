using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>The shape of an enemy's special attack: a circle lobbed onto her, lines thrown at her, or a ring around
    /// the enemy itself.</summary>
    public enum SpecialShape
    {
        None,
        Lob,
        Lines,
        Ring,
    }

    /// <summary>One enemy type's special attack (numbers in ground units, seconds and enemy hits).</summary>
    public readonly struct EnemySpecial
    {
        public EnemySpecial(string name, SpecialShape shape, float every, float minRange, float maxRange, float size, float fill, float hit,
            Color color, int count = 1, float spread = 0f, float length = 0f, float lingerSeconds = 0f, float lingerHit = 0f, bool poison = false,
            float slow = 0f, float slowSeconds = 0f, float stagger = 0f)
        {
            Name = name;
            Shape = shape;
            Every = every;
            MinRange = minRange;
            MaxRange = maxRange;
            Size = size;
            Fill = fill;
            Hit = hit;
            Color = color;
            Count = count;
            Spread = spread;
            Length = length;
            LingerSeconds = lingerSeconds;
            LingerHit = lingerHit;
            Poison = poison;
            Slow = slow;
            SlowSeconds = slowSeconds;
            Stagger = stagger;
        }

        public string Name { get; }
        public SpecialShape Shape { get; }
        /// <summary>Seconds between uses.</summary>
        public float Every { get; }
        /// <summary>She must be this far away at least and at most (ground units).</summary>
        public float MinRange { get; }
        public float MaxRange { get; }
        /// <summary>A circle's radius, or a line's width.</summary>
        public float Size { get; }
        /// <summary>The telegraph's fill: how long she has to get out (Docs/01: 0.6 to 1.2 s for a ground shape).</summary>
        public float Fill { get; }
        /// <summary>Damage on landing, in the enemy's hits.</summary>
        public float Hit { get; }
        public Color Color { get; }
        /// <summary>How many circles or lines.</summary>
        public int Count { get; }
        /// <summary>Lines: degrees between them; circles: how far apart they fall around her.</summary>
        public float Spread { get; }
        /// <summary>A line's length.</summary>
        public float Length { get; }
        /// <summary>A circle that burns or poisons on after landing, and how hard (hits a second).</summary>
        public float LingerSeconds { get; }
        public float LingerHit { get; }
        public bool Poison { get; }
        /// <summary>A slow on a landed hit: her speed multiplier and for how long.</summary>
        public float Slow { get; }
        public float SlowSeconds { get; }
        /// <summary>Several circles fall one after another, this many seconds apart.</summary>
        public float Stagger { get; }
    }

    /// <summary>
    /// Every enemy type's special attack (the owner, 2026-10-11: "more ranged and telegraphed attacks", "every type
    /// gets one"): each keeps its normal attack and every few seconds throws one of these, always drawn on the ground
    /// before it lands, so walking or diving out of it is the answer. Pure. Claude's numbers, to review.
    /// </summary>
    public static class EnemySpecialRules
    {
        static readonly Color Bile = new Color(0.55f, 0.85f, 0.25f, 0.85f);
        static readonly Color Bone = new Color(0.92f, 0.88f, 0.75f, 0.85f);
        static readonly Color Fire = new Color(1f, 0.45f, 0.12f, 0.9f);
        static readonly Color Blood = new Color(0.9f, 0.18f, 0.12f, 0.9f);
        static readonly Color Water = new Color(0.35f, 0.65f, 0.95f, 0.85f);
        static readonly Color Void = new Color(0.6f, 0.3f, 0.85f, 0.9f);
        static readonly Color Grave = new Color(0.45f, 0.9f, 0.6f, 0.85f);

        // By the definition's name; a champion or elite of a kind ("SwarmerChampion") takes the kind's.
        static readonly (string kind, EnemySpecial special)[] Table =
        {
            ("Swarmer", new EnemySpecial("Bile Spit", SpecialShape.Lob, 6.5f, 2.5f, 6.5f, 0.9f, 0.9f, 0.8f, Bile, lingerSeconds: 2.5f, lingerHit: 0.3f, poison: true)),
            ("Ghoul", new EnemySpecial("Ground Pound", SpecialShape.Ring, 7f, 0f, 3.2f, 2.6f, 1.0f, 1.4f, Blood)),
            ("BanditArcher", new EnemySpecial("Volley", SpecialShape.Lines, 7f, 2.5f, 9f, 0.55f, 0.8f, 0.9f, Blood, count: 3, spread: 16f, length: 10f)),
            ("Skeleton", new EnemySpecial("Bone Throw", SpecialShape.Lines, 6f, 2f, 7.5f, 0.6f, 0.75f, 1.0f, Bone, length: 8f)),
            ("Cultist", new EnemySpecial("Fire Rain", SpecialShape.Lob, 8f, 2f, 9f, 1.0f, 0.9f, 0.9f, Fire, count: 3, spread: 1.6f, stagger: 0.35f)),
            ("AshWolf", new EnemySpecial("Pounce Bite", SpecialShape.Lines, 6f, 2.5f, 6f, 0.9f, 0.7f, 1.1f, Blood, length: 6f)),
            ("Cutthroat", new EnemySpecial("Thrown Knives", SpecialShape.Lines, 6f, 2f, 7f, 0.45f, 0.7f, 0.8f, Blood, count: 2, spread: 14f, length: 8f)),
            ("EmberAcolyte", new EnemySpecial("Fire Wave", SpecialShape.Lines, 8f, 2f, 8f, 1.1f, 1.0f, 1.0f, Fire, length: 9f)),
            ("PyreKeeper", new EnemySpecial("Ember Burst", SpecialShape.Ring, 7f, 0f, 3.5f, 3f, 1.1f, 1.0f, Fire, lingerSeconds: 2f, lingerHit: 0.4f)),
            ("CarrionBloat", new EnemySpecial("Gas Lob", SpecialShape.Lob, 7f, 2.5f, 7f, 1.6f, 1.0f, 0.5f, Bile, lingerSeconds: 4f, lingerHit: 0.4f, poison: true)),
            ("Drowned", new EnemySpecial("Brine Spit", SpecialShape.Lines, 6.5f, 2f, 7f, 0.7f, 0.8f, 0.8f, Water, length: 8f, slow: 0.6f, slowSeconds: 1.5f)),
            ("Harpooner", new EnemySpecial("Net", SpecialShape.Lob, 8f, 3f, 8f, 1.3f, 0.9f, 0.5f, Water, slow: 0.5f, slowSeconds: 2f)),
            ("DrownedWatchman", new EnemySpecial("Tide Wave", SpecialShape.Lines, 8f, 1.5f, 7f, 1.6f, 1.1f, 1.3f, Water, length: 8f, slow: 0.6f, slowSeconds: 1.5f)),
            ("SkeletonKnight", new EnemySpecial("Shield Bash", SpecialShape.Ring, 6.5f, 0f, 3f, 2.2f, 0.9f, 1.4f, Bone)),
            ("GravePriest", new EnemySpecial("Grave Hands", SpecialShape.Lob, 7.5f, 2f, 8f, 1.2f, 1.0f, 1.0f, Grave, count: 2, spread: 2f, stagger: 0.5f)),
            ("Hollowed", new EnemySpecial("Shriek", SpecialShape.Ring, 7f, 0f, 4f, 3.2f, 1.1f, 1.0f, Void)),
            ("RiftCaller", new EnemySpecial("Rift Bolt", SpecialShape.Lines, 7f, 2f, 9f, 0.9f, 0.9f, 1.1f, Void, length: 10f)),
            ("VoidWraith", new EnemySpecial("Void Mark", SpecialShape.Lob, 6f, 2f, 8f, 1.2f, 0.9f, 1.2f, Void)),
        };

        /// <summary>The special of a definition by its name, or one with <see cref="SpecialShape.None"/>.</summary>
        public static EnemySpecial For(string definitionName)
        {
            if (!string.IsNullOrEmpty(definitionName))
            {
                // The longest matching kind: "DrownedWatchman" before "Drowned", "SkeletonKnight" before "Skeleton".
                var best = -1;
                for (var i = 0; i < Table.Length; i++)
                    if (definitionName.StartsWith(Table[i].kind, System.StringComparison.Ordinal) &&
                        (best < 0 || Table[i].kind.Length > Table[best].kind.Length))
                        best = i;
                if (best >= 0)
                    return Table[best].special;
            }
            return default;
        }

        /// <summary>Every kind that has a special, for tests.</summary>
        public static IEnumerable<string> Kinds
        {
            get
            {
                foreach (var row in Table)
                    yield return row.kind;
            }
        }

        /// <summary>How long the enemy shows its cast before the telegraph starts filling.</summary>
        public const float CastSeconds = 0.35f;

        /// <summary>The ground points circles fall on: one on her, the rest around it, evenly turned (pure, from a roll).</summary>
        public static Vector2 LobPoint(Vector2 target, int index, int count, float spread, float roll)
        {
            if (index == 0 || count <= 1)
                return target;
            var angle = (roll + (index - 1f) / Mathf.Max(1, count - 1)) * Mathf.PI * 2f;
            return target + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread;
        }

        /// <summary>The direction of line k of a fan of count lines centred on aim, spread degrees apart.</summary>
        public static Vector2 LineDirection(Vector2 aim, int index, int count, float spread)
        {
            var degrees = (index - (count - 1) / 2f) * spread;
            var r = degrees * Mathf.Deg2Rad;
            var c = Mathf.Cos(r);
            var s = Mathf.Sin(r);
            return new Vector2(aim.x * c - aim.y * s, aim.x * s + aim.y * c);
        }
    }

    /// <summary>
    /// Runs the enemies' specials (<see cref="EnemySpecialRules"/>), ticked by the <see cref="EnemyManager"/>: per-enemy
    /// timers that start part-way, used only while the enemy is awake, chasing, in range and in sight of her. The enemy
    /// shows a short cast, then the telegraph fills (circles through <see cref="EnemyHazards"/>, lines through its line
    /// strikes) and lands.
    /// </summary>
    public sealed class EnemySpecials
    {
        sealed class Pending
        {
            public EnemyController Enemy;
            public EnemySpecial Special;
            public Vector2 Target;
            public Vector2 Aim;
            public float Delay;
            public int Index;
            public bool Active;
        }

        readonly Dictionary<EnemyController, float> timers = new Dictionary<EnemyController, float>();
        readonly List<Pending> pending = new List<Pending>();
        int started;

        public void Tick(float deltaTime, EnemyManager world)
        {
            var player = world.Player;
            var alive = player != null && player.IsAlive;
            var target = world.PlayerGround;
            foreach (var enemy in world.Active)
            {
                if (!alive || !enemy.IsAlive || enemy.Definition == null || enemy.Definition.Rank == EnemyRank.Boss)
                    continue;
                var special = EnemySpecialRules.For(enemy.Definition.name);
                if (special.Shape == SpecialShape.None)
                    continue;
                if (!timers.TryGetValue(enemy, out var timer))
                    timer = special.Every * (0.35f + 0.13f * (started++ % 5));
                timer -= deltaTime;
                if (timer <= 0f && enemy.CanCast)
                {
                    var distance = Vector2.Distance(enemy.GroundPosition, target);
                    if (distance >= special.MinRange && distance <= special.MaxRange && world.Nav.HasLineOfSight(enemy.GroundPosition, target))
                    {
                        timer = special.Every * Random.Range(0.85f, 1.15f);
                        Begin(enemy, special, target);
                    }
                    else
                        timer = 0.4f;
                }
                timers[enemy] = timer;
            }

            for (var i = 0; i < pending.Count; i++)
            {
                var p = pending[i];
                if (!p.Active)
                    continue;
                p.Delay -= deltaTime;
                if (p.Delay > 0f)
                    continue;
                p.Active = false;
                if (p.Enemy == null || !p.Enemy.IsAlive)
                    continue;
                Release(p, world);
            }
        }

        void Begin(EnemyController enemy, EnemySpecial special, Vector2 target)
        {
            var aim = target - enemy.GroundPosition;
            aim = aim.sqrMagnitude > 1e-4f ? aim.normalized : Vector2.down;
            enemy.BeginCast(EnemySpecialRules.CastSeconds + special.Fill * 0.5f, aim);
            var roll = Random.value;
            var count = special.Shape == SpecialShape.Lob ? special.Count : 1;
            for (var k = 0; k < count; k++)
            {
                var point = special.Shape == SpecialShape.Lob ? EnemySpecialRules.LobPoint(target, k, special.Count, special.Spread, roll) : target;
                Queue(enemy, special, point, aim, EnemySpecialRules.CastSeconds + special.Stagger * k, k);
            }
        }

        void Queue(EnemyController enemy, EnemySpecial special, Vector2 target, Vector2 aim, float delay, int index)
        {
            Pending p = null;
            foreach (var q in pending)
                if (!q.Active)
                {
                    p = q;
                    break;
                }
            if (p == null)
            {
                p = new Pending();
                pending.Add(p);
            }
            p.Enemy = enemy;
            p.Special = special;
            p.Target = target;
            p.Aim = aim;
            p.Delay = delay;
            p.Index = index;
            p.Active = true;
        }

        static void Release(Pending p, EnemyManager world)
        {
            var enemy = p.Enemy;
            var s = p.Special;
            var hit = CombatFormulas.EnemyHitDamage(enemy.Level) * enemy.Definition.DamageMultiplier;
            var level = enemy.Level;
            switch (s.Shape)
            {
                case SpecialShape.Lob:
                    if (s.Poison)
                        world.Hazards.Poison(p.Target, s.Size, s.Fill, s.LingerSeconds, hit * s.LingerHit, level, s.Color, hit * s.Hit);
                    else
                        world.Hazards.Strike(p.Target, s.Size, s.Fill, hit * s.Hit, s.LingerSeconds, hit * s.LingerHit, level, s.Color, s.Slow, s.SlowSeconds);
                    break;
                case SpecialShape.Ring:
                    world.Hazards.Strike(enemy.GroundPosition, s.Size, s.Fill, hit * s.Hit, s.LingerSeconds, hit * s.LingerHit, level, s.Color, s.Slow, s.SlowSeconds);
                    break;
                case SpecialShape.Lines:
                    for (var k = 0; k < s.Count; k++)
                    {
                        var direction = EnemySpecialRules.LineDirection(p.Aim, k, s.Count, s.Spread);
                        var from = enemy.GroundPosition + direction * enemy.Definition.BodyRadius;
                        world.Hazards.Line(from, from + direction * s.Length, s.Size, s.Fill, hit * s.Hit, level, s.Color, s.Slow, s.SlowSeconds);
                    }
                    break;
            }
        }

        /// <summary>Forgets an enemy that died or left (the manager calls it on a kill).</summary>
        public void Forget(EnemyController enemy) => timers.Remove(enemy);

        public void Clear()
        {
            timers.Clear();
            foreach (var p in pending)
                p.Active = false;
        }
    }
}
