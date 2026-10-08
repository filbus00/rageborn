using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Elite affixes (2026-10-08, the owner: "make them have interesting attacks that need to be dodged"; answers: rolled
    /// affixes, Diablo style, and all eight proposed): which open at which depth, how many a pack rolls, their names and
    /// colours, and their numbers. A whole elite pack shares one roll. Every attack is marked on the ground before it
    /// hits. Numbers are Claude's, to review (Docs/05 Elite modifiers). Pure.
    /// </summary>
    public static class EliteAffixRules
    {
        /// <summary>Every modifier and the depth it opens at: the three old ones from the start, the attacks one by one.</summary>
        public static readonly (EliteModifiers modifier, int fromDepth)[] Pool =
        {
            (EliteModifiers.Hasted, 1), (EliteModifiers.Vampiric, 1), (EliteModifiers.Frozen, 1),
            (EliteModifiers.Molten, 1), (EliteModifiers.Desecrator, 2), (EliteModifiers.Mortar, 3),
            (EliteModifiers.Plagued, 4), (EliteModifiers.FrostNova, 5), (EliteModifiers.LightningLances, 7),
            (EliteModifiers.FireChains, 9), (EliteModifiers.ArcaneBeam, 11),
        };

        public const EliteModifiers Attacks = EliteModifiers.Molten | EliteModifiers.Desecrator | EliteModifiers.Mortar |
                                              EliteModifiers.Plagued | EliteModifiers.FrostNova | EliteModifiers.LightningLances |
                                              EliteModifiers.FireChains | EliteModifiers.ArcaneBeam;

        public static bool IsAttack(EliteModifiers modifier) => (modifier & Attacks) != 0;

        /// <summary>How many a pack rolls: one at the top, two from depth 7, three from 19.</summary>
        public static int Count(int depth) => depth >= 19 ? 3 : depth >= 7 ? 2 : 1;

        /// <summary>A pack's affixes: the first always an attack to dodge, the rest from everything open, none twice.</summary>
        public static EliteModifiers Roll(System.Random random, int depth)
        {
            var open = new List<EliteModifiers>();
            var attacks = new List<EliteModifiers>();
            foreach (var (modifier, from) in Pool)
                if (depth >= from)
                {
                    open.Add(modifier);
                    if (IsAttack(modifier))
                        attacks.Add(modifier);
                }
            var result = EliteModifiers.None;
            if (attacks.Count > 0)
            {
                var first = attacks[random.Next(attacks.Count)];
                result |= first;
                open.Remove(first);
            }
            var count = Mathf.Min(Count(depth), open.Count + (result != EliteModifiers.None ? 1 : 0));
            while (CountOf(result) < count && open.Count > 0)
            {
                var pick = random.Next(open.Count);
                result |= open[pick];
                open.RemoveAt(pick);
            }
            return result;
        }

        public static int CountOf(EliteModifiers modifiers)
        {
            var count = 0;
            for (var bits = (int)modifiers; bits != 0; bits &= bits - 1)
                count++;
            return count;
        }

        public static string Name(EliteModifiers modifier) => modifier switch
        {
            EliteModifiers.Hasted => "Hasted",
            EliteModifiers.Vampiric => "Vampiric",
            EliteModifiers.Frozen => "Frozen",
            EliteModifiers.Molten => "Molten",
            EliteModifiers.Desecrator => "Desecrator",
            EliteModifiers.Mortar => "Mortar",
            EliteModifiers.Plagued => "Plagued",
            EliteModifiers.FrostNova => "Frost Nova",
            EliteModifiers.LightningLances => "Lightning",
            EliteModifiers.FireChains => "Fire Chains",
            EliteModifiers.ArcaneBeam => "Arcane",
            _ => "",
        };

        /// <summary>The names of a set of affixes, in the pool's order, joined for a label.</summary>
        public static string Names(EliteModifiers modifiers)
        {
            var names = new List<string>();
            foreach (var (modifier, _) in Pool)
                if ((modifiers & modifier) != 0)
                    names.Add(Name(modifier));
            return string.Join("  ", names);
        }

        // The numbers: seconds between uses, radii in ground units, damage as a share of the elite's own hit.
        public const float MoltenEvery = 1.2f, MoltenRadius = 0.9f, MoltenSeconds = 4f, MoltenBurn = 0.5f;
        public const float MoltenDeathRadius = 2.2f, MoltenDeathFill = 1.1f, MoltenDeathHit = 1.6f;
        public const float DesecratorEvery = 5f, DesecratorRadius = 1.6f, DesecratorFill = 1.1f, DesecratorHit = 1.2f, DesecratorBurn = 0.5f, DesecratorSeconds = 3f;
        public const float MortarEvery = 4.5f, MortarRadius = 1.2f, MortarFill = 1.3f, MortarHit = 1.0f, MortarMinRange = 3.5f, MortarSpread = 1.7f;
        public const float PlagueEvery = 6f, PlagueRadius = 1.8f, PlagueFill = 0.8f, PlagueSeconds = 6f, PlagueBurn = 0.4f;
        public const float NovaEvery = 7f, NovaRange = 6f, NovaRadius = 4f, NovaFill = 1.3f, NovaHit = 1.0f, NovaSlow = 0.6f, NovaSlowSeconds = 2.5f;
        public const float LanceEvery = 6.5f, LanceLength = 12f, LanceWidth = 0.9f, LanceFill = 1.0f, LanceHit = 1.1f, LanceGap = 2.4f;
        public const float ChainWidth = 0.35f, ChainPulse = 0.4f, ChainHit = 0.5f;
        public const float BeamEvery = 11f, BeamLength = 7f, BeamWidth = 0.5f, BeamWindup = 1.2f, BeamSeconds = 7f, BeamDegreesPerSecond = 50f, BeamPulse = 0.35f, BeamHit = 0.6f;

        /// <summary>Attacks are used only while the elite is awake and she is this near.</summary>
        public const float Reach = 11f;

        /// <summary>The ground distance from a point to a segment.</summary>
        public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(point, a + ab * t);
        }
    }

    /// <summary>
    /// Runs the elites' attack affixes (2026-10-08): per elite timers for Molten, Desecrator, Mortar, Plagued, Frost Nova,
    /// Lightning Lances and Arcane Beam, the burning chains between a Fire Chains pack's elites, and a label of the
    /// pack's affixes over its first living elite. Owned and ticked by <see cref="EnemyManager"/>; circles go through
    /// <see cref="EnemyHazards"/>; lines and beams are pooled markers here.
    /// </summary>
    public sealed class EliteAffixes
    {
        static readonly Color LightningColor = new Color(0.55f, 0.75f, 1f, 0.95f);
        static readonly Color ChainColor = new Color(1f, 0.45f, 0.1f, 0.95f);
        static readonly Color ArcaneColor = new Color(0.8f, 0.4f, 1f, 0.95f);
        static readonly Color FrostColor = new Color(0.55f, 0.85f, 1f, 0.9f);
        static readonly Color DesecrateColor = new Color(0.35f, 0.1f, 0.45f, 0.95f);

        sealed class Timers
        {
            public float Molten, Desecrator, Mortar, Plague, Nova, Lance, Beam;
            public Vector2 LastTrail;
        }

        sealed class Lance
        {
            public GroundMarker Marker;
            public Vector2 From, To;
            public float Damage;
            public int Level;
            public bool Active;
        }

        sealed class Beam
        {
            public GroundMarker Marker;
            public GroundMarker Base;
            public Vector2 Center;
            public float Angle;
            public float Windup;
            public float Left;
            public float Pulse;
            public float Damage;
            public int Level;
            public bool Active;
        }

        sealed class Chain
        {
            public GroundMarker Marker;
            public bool Used;
        }

        readonly Transform parent;
        readonly Dictionary<EnemyController, Timers> timers = new Dictionary<EnemyController, Timers>();
        readonly List<Lance> lances = new List<Lance>();
        readonly List<Beam> beams = new List<Beam>();
        readonly List<Chain> chains = new List<Chain>();
        readonly Dictionary<EnemyPack, TextMesh> labels = new Dictionary<EnemyPack, TextMesh>();
        readonly Dictionary<EnemyPack, List<EnemyController>> packElites = new Dictionary<EnemyPack, List<EnemyController>>();
        readonly List<EnemyPack> packBuffer = new List<EnemyPack>();
        float chainPulse;

        public EliteAffixes(Transform parent) => this.parent = parent;

        public void Tick(float deltaTime, EnemyManager world)
        {
            var player = world.Player;
            var alive = player != null && player.IsAlive;
            var target = world.PlayerGround;

            foreach (var list in packElites.Values)
                list.Clear();
            foreach (var enemy in world.Active)
            {
                if (!enemy.IsAlive || enemy.Definition == null || enemy.Definition.Rank != EnemyRank.Elite)
                    continue;
                var modifiers = enemy.Modifiers;
                if (enemy.Pack != null)
                {
                    if (!packElites.TryGetValue(enemy.Pack, out var list))
                        packElites[enemy.Pack] = list = new List<EnemyController>();
                    list.Add(enemy);
                }
                if (!alive || (modifiers & EliteAffixRules.Attacks) == 0)
                    continue;
                var awake = enemy.State == EnemyState.Approach || enemy.State == EnemyState.Attack || enemy.State == EnemyState.Recover;
                if (!awake || Vector2.Distance(enemy.GroundPosition, target) > EliteAffixRules.Reach)
                    continue;
                if (!timers.TryGetValue(enemy, out var t))
                    timers[enemy] = t = NewTimers(enemy);
                Use(enemy, t, deltaTime, world, target);
            }

            TickLances(deltaTime, world);
            TickBeams(deltaTime, world);
            TickChains(deltaTime, world, alive);
            TickLabels(world);
        }

        int started;

        // Timers start part-way, so a pack's elites do not all fire at once.
        Timers NewTimers(EnemyController enemy)
        {
            var offset = (started++ & 7) * 0.37f;
            return new Timers
            {
                Molten = EliteAffixRules.MoltenEvery,
                Desecrator = 1.5f + offset,
                Mortar = 1f + offset,
                Plague = 2f + offset,
                Nova = 2.5f + offset,
                Lance = 2f + offset,
                Beam = 3f + offset,
                LastTrail = enemy.GroundPosition,
            };
        }

        static float Hit(EnemyController enemy) => CombatFormulas.EnemyHitDamage(enemy.Level) * enemy.Definition.DamageMultiplier;

        void Use(EnemyController enemy, Timers t, float dt, EnemyManager world, Vector2 target)
        {
            var m = enemy.Modifiers;
            var hit = Hit(enemy);
            var level = enemy.Level;
            var here = enemy.GroundPosition;

            if ((m & EliteModifiers.Molten) != 0)
            {
                t.Molten -= dt;
                if (t.Molten <= 0f && Vector2.Distance(here, t.LastTrail) > 0.6f)
                {
                    t.Molten = EliteAffixRules.MoltenEvery;
                    t.LastTrail = here;
                    world.Hazards.Fire(here, EliteAffixRules.MoltenRadius, EliteAffixRules.MoltenSeconds, hit * EliteAffixRules.MoltenBurn, level);
                }
            }
            if ((m & EliteModifiers.Desecrator) != 0 && (t.Desecrator -= dt) <= 0f)
            {
                t.Desecrator = EliteAffixRules.DesecratorEvery;
                world.Hazards.Strike(target, EliteAffixRules.DesecratorRadius, EliteAffixRules.DesecratorFill, hit * EliteAffixRules.DesecratorHit,
                    EliteAffixRules.DesecratorSeconds, hit * EliteAffixRules.DesecratorBurn, level, DesecrateColor);
            }
            if ((m & EliteModifiers.Mortar) != 0 && (t.Mortar -= dt) <= 0f && Vector2.Distance(here, target) > EliteAffixRules.MortarMinRange)
            {
                t.Mortar = EliteAffixRules.MortarEvery;
                world.Hazards.Blast(target, EliteAffixRules.MortarRadius, EliteAffixRules.MortarFill, hit * EliteAffixRules.MortarHit, level);
                for (var k = 0; k < 2; k++)
                {
                    var spot = target + Random.insideUnitCircle.normalized * EliteAffixRules.MortarSpread;
                    world.Hazards.Blast(spot, EliteAffixRules.MortarRadius, EliteAffixRules.MortarFill + 0.25f * (k + 1), hit * EliteAffixRules.MortarHit, level);
                }
            }
            if ((m & EliteModifiers.Plagued) != 0 && (t.Plague -= dt) <= 0f)
            {
                t.Plague = EliteAffixRules.PlagueEvery;
                world.Hazards.Poison(target, EliteAffixRules.PlagueRadius, EliteAffixRules.PlagueFill, EliteAffixRules.PlagueSeconds, hit * EliteAffixRules.PlagueBurn, level);
            }
            if ((m & EliteModifiers.FrostNova) != 0 && (t.Nova -= dt) <= 0f && Vector2.Distance(here, target) <= EliteAffixRules.NovaRange)
            {
                t.Nova = EliteAffixRules.NovaEvery;
                world.Hazards.Strike(here, EliteAffixRules.NovaRadius, EliteAffixRules.NovaFill, hit * EliteAffixRules.NovaHit, 0f, 0f, level, FrostColor,
                    EliteAffixRules.NovaSlow, EliteAffixRules.NovaSlowSeconds);
            }
            if ((m & EliteModifiers.LightningLances) != 0 && (t.Lance -= dt) <= 0f)
            {
                t.Lance = EliteAffixRules.LanceEvery;
                var angle = Random.Range(0f, Mathf.PI);
                var along = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var across = new Vector2(-along.y, along.x);
                for (var k = -1; k <= 1; k++)
                {
                    var middle = target + across * (k * EliteAffixRules.LanceGap);
                    AddLance(middle - along * (EliteAffixRules.LanceLength / 2f), middle + along * (EliteAffixRules.LanceLength / 2f),
                        hit * EliteAffixRules.LanceHit, level);
                }
            }
            if ((m & EliteModifiers.ArcaneBeam) != 0 && (t.Beam -= dt) <= 0f)
            {
                t.Beam = EliteAffixRules.BeamEvery;
                // A sentry between the elite and her, a little to the side, where it can stand.
                var toward = (target - here).normalized;
                var spot = here + toward * 2.5f + new Vector2(-toward.y, toward.x) * Random.Range(-1.5f, 1.5f);
                if (world.Nav.IsWalkable(IsoMath.GroundToCell(spot)))
                    AddBeam(spot, Mathf.Atan2(target.y - spot.y, target.x - spot.x) * Mathf.Rad2Deg + 90f, hit * EliteAffixRules.BeamHit, level);
            }
        }

        /// <summary>Molten's last act: the elite bursts where it fell. Called by the manager on a kill.</summary>
        public void OnKilled(EnemyController enemy, EnemyManager world)
        {
            timers.Remove(enemy);
            if (enemy.Definition == null || (enemy.Modifiers & EliteModifiers.Molten) == 0)
                return;
            world.Hazards.Blast(enemy.GroundPosition, EliteAffixRules.MoltenDeathRadius, EliteAffixRules.MoltenDeathFill,
                Hit(enemy) * EliteAffixRules.MoltenDeathHit, enemy.Level);
        }

        void AddLance(Vector2 from, Vector2 to, float damage, int level)
        {
            Lance lance = null;
            foreach (var l in lances)
                if (!l.Active)
                {
                    lance = l;
                    break;
                }
            if (lance == null)
            {
                lance = new Lance { Marker = GroundMarker.Line(from, to, EliteAffixRules.LanceWidth, EliteAffixRules.LanceFill, LightningColor, parent) };
                lances.Add(lance);
            }
            lance.Marker.SetColor(LightningColor);
            lance.Marker.RestartLine(from, to, EliteAffixRules.LanceWidth, EliteAffixRules.LanceFill);
            lance.From = from;
            lance.To = to;
            lance.Damage = damage;
            lance.Level = level;
            lance.Active = true;
        }

        void TickLances(float dt, EnemyManager world)
        {
            foreach (var lance in lances)
            {
                if (!lance.Active)
                    continue;
                lance.Marker.Advance(dt);
                if (!lance.Marker.Done)
                    continue;
                lance.Active = false;
                lance.Marker.Hide();
                Sfx.Play(SoundId.EnemySlam, 0.5f);
                var player = world.Player;
                if (player != null && player.IsAlive &&
                    EliteAffixRules.DistanceToSegment(world.PlayerGround, lance.From, lance.To) <= EliteAffixRules.LanceWidth / 2f)
                    player.TakeHit(lance.Damage, lance.Level, 0f, dodgeable: false);
            }
        }

        void AddBeam(Vector2 center, float angle, float damage, int level)
        {
            Beam beam = null;
            foreach (var b in beams)
                if (!b.Active)
                {
                    beam = b;
                    break;
                }
            var end = center + Direction(angle) * EliteAffixRules.BeamLength;
            if (beam == null)
            {
                beam = new Beam
                {
                    Marker = GroundMarker.Line(center, end, EliteAffixRules.BeamWidth, EliteAffixRules.BeamWindup, ArcaneColor, parent),
                    Base = GroundMarker.Circle(center, 0.45f, 0.01f, ArcaneColor, parent),
                };
                beams.Add(beam);
            }
            beam.Marker.SetColor(ArcaneColor);
            beam.Marker.RestartLine(center, end, EliteAffixRules.BeamWidth, EliteAffixRules.BeamWindup);
            beam.Base.RestartCircle(center, 0.45f, 0.01f);
            beam.Base.Advance(1f);
            beam.Center = center;
            beam.Angle = angle;
            beam.Windup = EliteAffixRules.BeamWindup;
            beam.Left = EliteAffixRules.BeamSeconds;
            beam.Pulse = 0f;
            beam.Damage = damage;
            beam.Level = level;
            beam.Active = true;
        }

        static Vector2 Direction(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        void TickBeams(float dt, EnemyManager world)
        {
            foreach (var beam in beams)
            {
                if (!beam.Active)
                    continue;
                if (beam.Windup > 0f)
                {
                    beam.Windup -= dt;
                    beam.Marker.Advance(dt);
                    continue;
                }
                beam.Left -= dt;
                beam.Angle += EliteAffixRules.BeamDegreesPerSecond * dt;
                var end = beam.Center + Direction(beam.Angle) * EliteAffixRules.BeamLength;
                beam.Marker.RestartLine(beam.Center, end, EliteAffixRules.BeamWidth, 0.01f);
                beam.Marker.Advance(1f);
                beam.Pulse -= dt;
                var player = world.Player;
                if (beam.Pulse <= 0f && player != null && player.IsAlive &&
                    EliteAffixRules.DistanceToSegment(world.PlayerGround, beam.Center, end) <= EliteAffixRules.BeamWidth / 2f + 0.2f)
                {
                    beam.Pulse = EliteAffixRules.BeamPulse;
                    player.TakeHit(beam.Damage, beam.Level, 0f, dodgeable: false);
                }
                if (beam.Left > 0f)
                    continue;
                beam.Active = false;
                beam.Marker.Hide();
                beam.Base.Hide();
            }
        }

        // The chains: between each pair of a Fire Chains pack's living, awake elites, in the order they stand in the pack.
        void TickChains(float dt, EnemyManager world, bool playerAlive)
        {
            foreach (var chain in chains)
                chain.Used = false;
            chainPulse -= dt;
            var pulse = chainPulse <= 0f;
            if (pulse)
                chainPulse = EliteAffixRules.ChainPulse;
            var used = 0;
            foreach (var pair in packElites)
            {
                var elites = pair.Value;
                if (elites.Count < 2 || (elites[0].Modifiers & EliteModifiers.FireChains) == 0)
                    continue;
                var awake = false;
                foreach (var e in elites)
                    awake |= e.State != EnemyState.Idle;
                if (!awake)
                    continue;
                for (var i = 0; i + 1 < elites.Count; i++)
                {
                    var a = elites[i].GroundPosition;
                    var b = elites[(i + 1) % elites.Count].GroundPosition;
                    if (used == chains.Count)
                        chains.Add(new Chain { Marker = GroundMarker.Line(a, b, EliteAffixRules.ChainWidth, 0.01f, ChainColor, parent) });
                    var chain = chains[used++];
                    chain.Used = true;
                    chain.Marker.SetColor(ChainColor);
                    chain.Marker.RestartLine(a, b, EliteAffixRules.ChainWidth, 0.01f);
                    chain.Marker.Advance(1f);
                    if (pulse && playerAlive && EliteAffixRules.DistanceToSegment(world.PlayerGround, a, b) <= EliteAffixRules.ChainWidth / 2f + 0.25f)
                        world.Player.TakeHit(Hit(elites[i]) * EliteAffixRules.ChainHit, elites[i].Level, 0f, dodgeable: false);
                }
            }
            foreach (var chain in chains)
                if (!chain.Used)
                    chain.Marker.Hide();
        }

        // Each elite pack's affixes in its colours over its first living elite.
        void TickLabels(EnemyManager world)
        {
            packBuffer.Clear();
            packBuffer.AddRange(labels.Keys);
            foreach (var pack in packBuffer)
                if (!packElites.TryGetValue(pack, out var alive) || alive.Count == 0)
                {
                    if (labels[pack] != null)
                        Object.Destroy(labels[pack].gameObject);
                    labels.Remove(pack);
                }
            foreach (var pair in packElites)
            {
                if (pair.Value.Count == 0 || pair.Key == null)
                    continue;
                var leader = pair.Value[0];
                if (!labels.TryGetValue(pair.Key, out var label) || label == null)
                {
                    var go = new GameObject("Elite Affixes", typeof(TextMesh));
                    go.transform.SetParent(parent, false);
                    label = go.GetComponent<TextMesh>();
                    label.anchor = TextAnchor.LowerCenter;
                    label.characterSize = 0.06f;
                    label.fontSize = 44;
                    label.color = new Color(1f, 0.75f, 0.45f);
                    label.text = EliteAffixRules.Names(leader.Modifiers);
                    go.GetComponent<MeshRenderer>().sortingLayerName = GameSortingLayers.WorldUI;
                    labels[pair.Key] = label;
                }
                var height = 1.5f * (leader.Definition != null ? leader.Definition.VisualScale : 1f);
                label.transform.position = leader.transform.position + new Vector3(0f, height, 0f);
            }
        }

        /// <summary>Clears every attack and label (a new level).</summary>
        public void Clear()
        {
            timers.Clear();
            foreach (var lance in lances)
            {
                lance.Active = false;
                lance.Marker.Hide();
            }
            foreach (var beam in beams)
            {
                beam.Active = false;
                beam.Marker.Hide();
                beam.Base.Hide();
            }
            foreach (var chain in chains)
                chain.Marker.Hide();
            foreach (var label in labels.Values)
                if (label != null)
                    Object.Destroy(label.gameObject);
            labels.Clear();
            packElites.Clear();
        }
    }
}
