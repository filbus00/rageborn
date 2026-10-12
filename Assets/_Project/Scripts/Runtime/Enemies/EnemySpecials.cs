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

        /// <summary>Real projectiles thrown at her along the aim lines it showed during the wind-up (2026-10-11: "more
        /// throwing attacks with projectiles").</summary>
        Throw,
    }

    /// <summary>One enemy type's special attack (numbers in ground units, seconds and enemy hits).</summary>
    public readonly struct EnemySpecial
    {
        public EnemySpecial(string name, SpecialShape shape, float every, float minRange, float maxRange, float size, float fill, float hit,
            Color color, int count = 1, float spread = 0f, float length = 0f, float lingerSeconds = 0f, float lingerHit = 0f, bool poison = false,
            float slow = 0f, float slowSeconds = 0f, float stagger = 0f, float speed = 0f)
        {
            Speed = speed;
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
        /// <summary>A thrown projectile's speed (ground units a second).</summary>
        public float Speed { get; }
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
        static readonly Color Rock = new Color(0.55f, 0.5f, 0.45f, 1f);
        static readonly Color Steel = new Color(0.8f, 0.82f, 0.85f, 1f);

        // By the definition's name; a champion or elite of a kind ("SwarmerChampion") takes the kind's. The concept
        // roster's (2026-10-13), each the ranged move its card names. Claude's numbers, to review.
        static readonly (string kind, EnemySpecial special)[] Table =
        {
            ("Swarmer", new EnemySpecial("Bile Spit", SpecialShape.Lob, 6.5f, 2.5f, 6.5f, 0.9f, 0.9f, 0.8f, Bile, lingerSeconds: 2.5f, lingerHit: 0.3f, poison: true)),
            ("Zombie", new EnemySpecial("Vomit", SpecialShape.Lob, 7f, 2f, 6f, 1.0f, 1.0f, 0.8f, Bile, lingerSeconds: 2.5f, lingerHit: 0.3f, poison: true)),
            ("SkeletonWarrior", new EnemySpecial("Javelin", SpecialShape.Throw, 6f, 2.5f, 9f, 0.25f, 0.5f, 1.2f, Bone, length: 11f, speed: 15f)),
            ("Fallen", new EnemySpecial("Fire Spit", SpecialShape.Throw, 6.5f, 2f, 8f, 0.25f, 0.45f, 0.8f, Fire, length: 9f, speed: 13f)),
            ("QuillRat", new EnemySpecial("Quill Shot", SpecialShape.Throw, 5.5f, 2f, 8f, 0.2f, 0.45f, 0.7f, Bone, count: 3, spread: 14f, length: 9f, speed: 16f)),
            ("Ghoul", new EnemySpecial("Flesh Throw", SpecialShape.Throw, 6f, 2.5f, 8f, 0.3f, 0.5f, 1.2f, Blood, length: 10f, speed: 12f)),
            ("DarkCultist", new EnemySpecial("Fire Rain", SpecialShape.Lob, 8f, 2f, 9f, 1.0f, 0.9f, 0.9f, Fire, count: 3, spread: 1.6f, stagger: 0.35f)),
            ("BileFiend", new EnemySpecial("Poison Spit", SpecialShape.Lob, 7f, 2.5f, 7.5f, 1.4f, 1.0f, 0.6f, Bile, lingerSeconds: 4f, lingerHit: 0.4f, poison: true)),
            ("Wight", new EnemySpecial("Curse", SpecialShape.Lob, 7.5f, 2f, 8f, 1.2f, 1.0f, 0.9f, Grave, slow: 0.6f, slowSeconds: 2f)),
            ("Mummy", new EnemySpecial("Rot Curse", SpecialShape.Ring, 7f, 0f, 3.5f, 3f, 1.1f, 0.9f, Bile, lingerSeconds: 2f, lingerHit: 0.3f, poison: true)),
            ("BoneMage", new EnemySpecial("Bone Spears", SpecialShape.Throw, 6f, 2f, 10f, 0.25f, 0.5f, 1.0f, Bone, count: 3, spread: 12f, length: 12f, speed: 15f)),
            ("FleshGolem", new EnemySpecial("Vomit", SpecialShape.Lines, 7f, 1.5f, 6f, 1.2f, 1.0f, 1.1f, Bile, length: 6f, slow: 0.6f, slowSeconds: 1.5f)),
            ("Wraith", new EnemySpecial("Phase Bolts", SpecialShape.Throw, 6f, 2f, 9f, 0.25f, 0.5f, 0.9f, Water, count: 2, spread: 16f, length: 11f, speed: 14f)),
            ("Ogre", new EnemySpecial("Club Toss", SpecialShape.Lob, 7f, 2.5f, 8f, 1.5f, 1.1f, 1.5f, Rock)),
            ("Gargoyle", new EnemySpecial("Rock Throw", SpecialShape.Throw, 7f, 2.5f, 9f, 0.3f, 0.5f, 1.4f, Rock, length: 11f, speed: 12f)),
            ("Lich", new EnemySpecial("Soul Bolts", SpecialShape.Throw, 6f, 2f, 10f, 0.3f, 0.55f, 1.2f, Void, count: 3, spread: 15f, length: 12f, speed: 13f)),
            ("Imp", new EnemySpecial("Fire Spit", SpecialShape.Throw, 5.5f, 2f, 8f, 0.25f, 0.45f, 0.8f, Fire, length: 9f, speed: 14f)),
            ("Succubus", new EnemySpecial("Drain", SpecialShape.Throw, 6f, 2f, 9f, 0.3f, 0.55f, 1.0f, Void, length: 11f, speed: 12f, slow: 0.6f, slowSeconds: 1.5f)),
            ("Harpy", new EnemySpecial("Feather Darts", SpecialShape.Throw, 5.5f, 2f, 9f, 0.2f, 0.45f, 0.8f, Steel, count: 3, spread: 12f, length: 10f, speed: 17f)),
            ("StygianDemon", new EnemySpecial("Fireball", SpecialShape.Lob, 7f, 2.5f, 9f, 1.3f, 1.0f, 1.3f, Fire, lingerSeconds: 2f, lingerHit: 0.4f)),
            ("HellHound", new EnemySpecial("Fire Spit", SpecialShape.Lines, 6f, 2.5f, 6f, 0.9f, 0.7f, 1.0f, Fire, length: 6f)),
            ("Arachnid", new EnemySpecial("Poison Web", SpecialShape.Lob, 7f, 2.5f, 9f, 1.3f, 0.9f, 0.6f, Bile, lingerSeconds: 3f, lingerHit: 0.3f, poison: true, slow: 0.5f, slowSeconds: 2f)),
            ("SandWorm", new EnemySpecial("Burrow Burst", SpecialShape.Ring, 7.5f, 0f, 4f, 3.4f, 1.1f, 1.4f, Rock)),
            ("Giant", new EnemySpecial("Rock Throw", SpecialShape.Throw, 7f, 3f, 10f, 0.4f, 0.6f, 1.8f, Rock, length: 12f, speed: 11f)),
            ("Nightmare", new EnemySpecial("Fire Breath", SpecialShape.Lines, 6.5f, 1.5f, 6f, 1.2f, 0.8f, 1.2f, Fire, length: 6f, lingerSeconds: 2f, lingerHit: 0.4f)),
            ("BalrogLite", new EnemySpecial("Fireball", SpecialShape.Lob, 7f, 2.5f, 10f, 1.6f, 1.0f, 1.6f, Fire, count: 2, spread: 2f, stagger: 0.4f, lingerSeconds: 2f, lingerHit: 0.5f)),
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
                        Begin(enemy, special, target, world);
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

        const float AimWidth = 0.18f;

        void Begin(EnemyController enemy, EnemySpecial special, Vector2 target, EnemyManager world)
        {
            var aim = target - enemy.GroundPosition;
            aim = aim.sqrMagnitude > 1e-4f ? aim.normalized : Vector2.down;
            if (special.Shape == SpecialShape.Throw)
            {
                // A thin aim line along each throw for the wind-up, then the projectiles fly down them.
                var warning = EnemyRules.Telegraph(EnemySpecialRules.CastSeconds + special.Fill);
                enemy.BeginCast(warning, aim);
                for (var k = 0; k < special.Count; k++)
                {
                    var direction = EnemySpecialRules.LineDirection(aim, k, special.Count, special.Spread);
                    var from = enemy.GroundPosition + direction * enemy.Definition.BodyRadius;
                    world.Hazards.Line(from, from + direction * special.Length, AimWidth, warning, 0f, enemy.Level, special.Color);
                }
                Queue(enemy, special, target, aim, warning, 0);
                return;
            }
            enemy.BeginCast(EnemyRules.Telegraph(EnemySpecialRules.CastSeconds + special.Fill * 0.5f), aim);
            var roll = Random.value;
            var count = special.Shape == SpecialShape.Lob ? special.Count : 1;
            for (var k = 0; k < count; k++)
            {
                var point = special.Shape == SpecialShape.Lob ? EnemySpecialRules.LobPoint(target, k, special.Count, special.Spread, roll) : target;
                Queue(enemy, special, point, aim, EnemyRules.Telegraph(EnemySpecialRules.CastSeconds + special.Stagger * k), k);
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
                        world.Hazards.Poison(p.Target, s.Size, EnemyRules.Telegraph(s.Fill), s.LingerSeconds, hit * s.LingerHit, level, s.Color, hit * s.Hit);
                    else
                        world.Hazards.Strike(p.Target, s.Size, EnemyRules.Telegraph(s.Fill), hit * s.Hit, s.LingerSeconds, hit * s.LingerHit, level, s.Color, s.Slow, s.SlowSeconds);
                    break;
                case SpecialShape.Ring:
                    world.Hazards.Strike(enemy.GroundPosition, s.Size, EnemyRules.Telegraph(s.Fill), hit * s.Hit, s.LingerSeconds, hit * s.LingerHit, level, s.Color, s.Slow, s.SlowSeconds);
                    break;
                case SpecialShape.Throw:
                    Sfx.Play(SoundId.ArrowShot, 0.6f);
                    for (var k = 0; k < s.Count; k++)
                    {
                        var direction = EnemySpecialRules.LineDirection(p.Aim, k, s.Count, s.Spread);
                        world.Projectiles.Fire(enemy, enemy.GroundPosition + direction * enemy.Definition.BodyRadius, direction * s.Speed, s.Length,
                            hit * s.Hit * EnemyRules.ProjectileDamageFactor(enemy.Definition.Rank), level, 0f, s.Color);
                    }
                    break;
                case SpecialShape.Lines:
                    for (var k = 0; k < s.Count; k++)
                    {
                        var direction = EnemySpecialRules.LineDirection(p.Aim, k, s.Count, s.Spread);
                        var from = enemy.GroundPosition + direction * enemy.Definition.BodyRadius;
                        world.Hazards.Line(from, from + direction * s.Length, s.Size, EnemyRules.Telegraph(s.Fill), hit * s.Hit, level, s.Color, s.Slow, s.SlowSeconds);
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
