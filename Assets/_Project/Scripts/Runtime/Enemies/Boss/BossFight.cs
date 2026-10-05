using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// What every boss fight shares (2026-10-05, out of the Cinder Warden's fight so the bosses at 12, 18 and 24 are only
    /// their own attacks): the boss is an ordinary pooled <see cref="EnemyController"/> in <see cref="EnemyController.Scripted"/>
    /// mode, so targeting, damage, the kill, its loot and XP work as for any enemy; the fight engages when the player
    /// enters the circular arena and resets (not heals) when she leaves it or dies; three phases at two thirds and one
    /// third of its life (<see cref="BossPhases"/>), each flashing the bar and cancelling the current attack; the stagger
    /// meter, which stops the boss when full. Attacks are iterators stepped with the game clock, so the inventory pause
    /// and hit stop freeze them and a stagger drops one mid-telegraph. Helpers give the shapes every boss uses: filling
    /// circles and lines, projectiles, lasting zones, adds from the arena's rim.
    /// Docs/01 design rule: every damaging attack is telegraphed; ground shapes cannot be dodged, only left.
    /// </summary>
    public abstract class BossFight : MonoBehaviour
    {
        // Docs/03-itemization.md: elite and boss damage ignores 15 percent of player armor.
        protected const float ArmorIgnore = 0.15f;

        // The player may step this far outside the arena before the boss gives up and walks back to its middle.
        const float LeaveMargin = 3f;
        const float ZonePulseSeconds = 0.5f;

        protected static readonly Color TelegraphColor = new Color(1f, 0.22f, 0.12f, 0.95f);

        protected EnemyDefinition bossDefinition;
        protected EnemyDefinition addDefinition;
        protected int level;
        protected Vector2 center;
        protected float arenaRadius;
        string killKey;
        Action onDefeated;

        protected EnemyManager manager;
        protected EnemyController boss;
        BossBar bar;
        readonly StaggerMeter stagger = new StaggerMeter();

        bool engaged;
        bool ended;
        protected int phase = 1;
        protected int attackIndex;
        protected float cooldown = 1.2f;
        protected float frameDelta;

        // The running attack and the helper attacks it is waiting on: a yielded IEnumerator runs to its end before the
        // one that yielded it goes on (attacks are stepped by hand on the game clock, not run as Unity coroutines).
        readonly Stack<IEnumerator> attack = new Stack<IEnumerator>();
        readonly List<GroundMarker> attackMarkers = new List<GroundMarker>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Zone> zones = new List<Zone>();
        protected readonly List<EnemyController> adds = new List<EnemyController>();

        sealed class Shot
        {
            public Transform Transform;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Radius;
            public float Multiplier;
            public float SlowFraction;
        }

        sealed class Zone
        {
            public GroundMarker Marker;
            public Vector2 Center;
            public float Radius;
            public float Left;
            public float PerSecond;
            public float SlowFraction;
            public float Pulse;
        }

        /// <summary>The name on the bar.</summary>
        protected abstract string BossName { get; }

        /// <summary>The next attack for the current phase (<see cref="phase"/>, <see cref="attackIndex"/> counts them).</summary>
        protected abstract IEnumerator NextAttack();

        /// <summary>Called on entering phase 2 or 3, after the bar's flash and the attack's cancel.</summary>
        protected virtual void OnPhase(int newPhase) { }

        /// <summary>Called every frame of an engaged fight before the attacks (lasting effects, adds).</summary>
        protected virtual void TickFight(Vector2 player) { }

        /// <summary>Called when the boss dies or the fight resets, to clear the fight's own objects.</summary>
        protected virtual void ClearFight() { }

        protected virtual float MoveSpeed => 2.4f;
        protected virtual float KeepDistance => 2.2f;

        public EnemyController Boss => boss;
        public int Phase => phase;
        public bool Engaged => engaged;

        public void Configure(EnemyDefinition bossDef, EnemyDefinition addDef, int bossLevel, Vector2 arenaCenter,
            float radius, string key, Action defeated)
        {
            bossDefinition = bossDef;
            addDefinition = addDef;
            level = bossLevel;
            center = arenaCenter;
            arenaRadius = radius;
            killKey = key;
            onDefeated = defeated;
        }

        void Start()
        {
            manager = FindAnyObjectByType<EnemyManager>();
            if (manager == null || !manager.IsReady || bossDefinition == null)
            {
                Debug.LogError("[ARPG] The boss fight needs a ready EnemyManager and the boss definition.", this);
                enabled = false;
                return;
            }

            boss = manager.Spawn(bossDefinition, center, false, null, level);
            boss.Scripted = true;
            boss.Damaged += OnBossHit;
            bar = BossBar.Create(transform, BossName);
        }

        void OnBossHit(float damage)
        {
            if (!engaged)
                Engage();
            if (stagger.AddHit())
                CancelAttack();
        }

        void Update()
        {
            if (boss == null)
                return;

            frameDelta = Time.deltaTime;
            for (var i = 0; i < attackMarkers.Count; i++)
                attackMarkers[i].Advance(frameDelta);
            AdvanceShots();
            AdvanceZones();

            if (!boss.IsAlive)
            {
                if (!ended)
                    EndFight();
                return;
            }

            var player = manager.PlayerGround;
            var playerAlive = manager.Player != null && manager.Player.IsAlive;
            var playerDistance = Vector2.Distance(player, center);

            if (!engaged)
            {
                if (playerAlive && playerDistance <= arenaRadius)
                    Engage();
                else
                {
                    WalkTowards(center, 0.2f);
                    return;
                }
            }
            else if (!playerAlive || playerDistance > arenaRadius + LeaveMargin)
            {
                Disengage();
                return;
            }

            bar.SetLife(boss.Life / boss.MaxLife);
            stagger.Tick(frameDelta);
            bar.SetStagger(stagger.Fraction, stagger.IsStaggered);

            var newPhase = BossPhases.PhaseFor(boss.Life / boss.MaxLife);
            if (newPhase > phase)
                EnterPhase(newPhase);

            TickFight(player);

            // A staggered boss stands still and does nothing: the window the stagger meter exists for.
            if (stagger.IsStaggered)
                return;

            if (attack.Count > 0)
            {
                if (!StepAttack())
                    ClearAttackMarkers();
                return;
            }

            WalkTowards(player, KeepDistance);
            cooldown -= frameDelta;
            if (cooldown <= 0f)
            {
                attackIndex++;
                attack.Push(NextAttack());
            }
        }

        void Engage()
        {
            engaged = true;
            bar.Show();
            bar.SetTitle(BossName);
        }

        void Disengage()
        {
            engaged = false;
            CancelAttack();
            ClearZones();
            bar.Hide();
            cooldown = 1.2f;
        }

        void EnterPhase(int newPhase)
        {
            phase = newPhase;
            bar.Flash();
            Sfx.Play(SoundId.BossPhase);
            CancelAttack();
            cooldown = 1f;
            OnPhase(newPhase);
        }

        void EndFight()
        {
            ended = true;
            CancelAttack();
            ClearZones();
            foreach (var shot in shots)
                Destroy(shot.Transform.gameObject);
            shots.Clear();
            ClearFight();

            GameSession.Current.RecordKill(killKey, 0);
            SaveDirector.SaveNow();
            bar.SetLife(0f);
            bar.SetTitle(BossName + " defeated");
            StartCoroutine(HideBarLater());
            onDefeated?.Invoke();
        }

        IEnumerator HideBarLater()
        {
            yield return new WaitForSeconds(3f);
            bar.Hide();
        }

        // --- Helpers for the attacks ---------------------------------------------------------------------------------

        /// <summary>Waits on the game clock.</summary>
        protected IEnumerator Wait(float seconds)
        {
            while (seconds > 0f)
            {
                seconds -= frameDelta;
                yield return null;
            }
        }

        /// <summary>Docs/01: dodge works on projectiles and melee, not on ground shapes.</summary>
        protected void HitPlayer(float multiplier, bool dodgeable = true)
        {
            if (manager.Player == null || !manager.Player.IsAlive)
                return;
            var damage = CombatFormulas.EnemyHitDamage(level) * bossDefinition.DamageMultiplier * multiplier;
            manager.Player.TakeHit(damage, level, ArmorIgnore, dodgeable);
        }

        protected void SlowPlayer(float fraction, float seconds) =>
            manager.PlayerController?.ApplySlow(1f - fraction, seconds);

        protected bool PlayerWithin(Vector2 at, float radius) => Vector2.Distance(manager.PlayerGround, at) <= radius;

        /// <summary>Whether the player stands on a band from one point to another, this wide.</summary>
        protected bool PlayerOnLine(Vector2 from, Vector2 to, float width) =>
            GroundRules.SegmentDistance(manager.PlayerGround, from, to) <= width * 0.5f;

        /// <summary>A circle that fills, then hits whoever is inside (a ground shape, not dodgeable).</summary>
        protected IEnumerator CircleStrike(Vector2 at, float radius, float fillSeconds, float multiplier)
        {
            var marker = Track(GroundMarker.Circle(at, radius, fillSeconds, TelegraphColor, transform));
            while (!marker.Done)
                yield return null;
            Sfx.Play(SoundId.EnemySlam, 0.8f);
            if (PlayerWithin(at, radius))
                HitPlayer(multiplier, dodgeable: false);
        }

        /// <summary>A band that shows for its fill time, then hits whoever stands on it.</summary>
        protected IEnumerator LineStrike(Vector2 from, Vector2 to, float width, float fillSeconds, float multiplier)
        {
            var marker = Track(GroundMarker.Line(from, to, width, fillSeconds, TelegraphColor, transform));
            while (!marker.Done)
                yield return null;
            Sfx.Play(SoundId.EnemySlam, 0.7f);
            if (PlayerOnLine(from, to, width))
                HitPlayer(multiplier, dodgeable: false);
        }

        /// <summary>A charge: a line for <paramref name="windup"/>, then the boss dashes along it, hitting once.</summary>
        protected IEnumerator Charge(Vector2 target, float windup, float speed, float multiplier)
        {
            var from = boss.GroundPosition;
            target = ClampToArena(target, 1.5f);
            var line = Track(GroundMarker.Line(from, target, 0.9f, windup, TelegraphColor, transform));
            while (!line.Done)
                yield return null;
            ClearAttackMarkers();
            var hit = false;
            var travelled = 0f;
            var length = Vector2.Distance(from, target);
            var direction = length > 0f ? (target - from) / length : Vector2.zero;
            while (travelled < length)
            {
                var step = Mathf.Min(speed * frameDelta, length - travelled);
                travelled += step;
                boss.ScriptedMove(direction * step);
                if (!hit && PlayerWithin(boss.GroundPosition, 1.2f))
                {
                    hit = true;
                    HitPlayer(multiplier);
                }
                yield return null;
            }
        }

        /// <summary>A fan of shots at the player, each shown as a line for 0.4 s first.</summary>
        protected IEnumerator Fan(int count, float spreadDegrees, float speed, float radius, float multiplier, Color color, float slowFraction = 0f)
        {
            var origin = boss.GroundPosition;
            var aim = (manager.PlayerGround - origin).normalized;
            if (aim == Vector2.zero)
                aim = Vector2.up;
            var directions = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var angle = (i - (count - 1) / 2f) * spreadDegrees * Mathf.Deg2Rad;
                directions[i] = new Vector2(aim.x * Mathf.Cos(angle) - aim.y * Mathf.Sin(angle), aim.x * Mathf.Sin(angle) + aim.y * Mathf.Cos(angle));
                Track(GroundMarker.Line(origin, origin + directions[i] * arenaRadius * 1.6f, 0.08f, 0.4f, TelegraphColor, transform));
            }
            yield return Wait(0.4f);
            foreach (var direction in directions)
                LaunchShot(origin, direction * speed, radius, multiplier, color, slowFraction);
        }

        /// <summary>A projectile from the boss; walls and the arena's edge stop it.</summary>
        protected void LaunchShot(Vector2 origin, Vector2 velocity, float radius, float multiplier, Color color, float slowFraction = 0f)
        {
            var go = GroundMarker.NewSprite("Shot", TelegraphArt.Ember, color, transform, 60);
            go.GetComponent<SpriteRenderer>().sortingLayerName = GameSortingLayers.Effects;
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            go.transform.position = IsoMath.GroundToWorld(origin);
            shots.Add(new Shot { Transform = go.transform, Position = origin, Velocity = velocity, Radius = radius, Multiplier = multiplier, SlowFraction = slowFraction });
        }

        void AdvanceShots()
        {
            for (var i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i];
                shot.Position += shot.Velocity * frameDelta;
                shot.Transform.position = IsoMath.GroundToWorld(shot.Position);
                var hitPlayer = manager != null && Vector2.Distance(shot.Position, manager.PlayerGround) <= shot.Radius;
                var gone = manager == null || Vector2.Distance(shot.Position, center) > arenaRadius + 1f ||
                           !manager.Nav.IsWalkable(IsoMath.GroundToCell(shot.Position));
                if (hitPlayer)
                {
                    HitPlayer(shot.Multiplier);
                    if (shot.SlowFraction > 0f)
                        SlowPlayer(shot.SlowFraction, 1.5f);
                }
                if (hitPlayer || gone)
                {
                    Destroy(shot.Transform.gameObject);
                    shots.RemoveAt(i);
                }
            }
        }

        /// <summary>A lasting zone on the ground: it fades in over <paramref name="fillSeconds"/> (harmless until then),
        /// then for <paramref name="seconds"/> hurts the player standing in it (hits a second) and slows her.</summary>
        protected void LastingZone(Vector2 at, float radius, float fillSeconds, float seconds, float hitsPerSecond, float slowFraction, Color color)
        {
            var marker = GroundMarker.Circle(at, radius, Mathf.Max(0.01f, fillSeconds), color, transform);
            zones.Add(new Zone { Marker = marker, Center = at, Radius = radius, Left = seconds, PerSecond = hitsPerSecond, SlowFraction = slowFraction });
        }

        void AdvanceZones()
        {
            for (var i = zones.Count - 1; i >= 0; i--)
            {
                var zone = zones[i];
                zone.Marker.Advance(frameDelta);
                if (!zone.Marker.Done)
                    continue;
                zone.Left -= frameDelta;
                if (zone.Left <= 0f || manager == null)
                {
                    zone.Marker.Destroy();
                    zones.RemoveAt(i);
                    continue;
                }
                if (!PlayerWithin(zone.Center, zone.Radius))
                    continue;
                if (zone.SlowFraction > 0f)
                    SlowPlayer(zone.SlowFraction, 0.3f);
                zone.Pulse -= frameDelta;
                if (zone.PerSecond > 0f && zone.Pulse <= 0f)
                {
                    zone.Pulse = ZonePulseSeconds;
                    HitPlayer(zone.PerSecond * ZonePulseSeconds, dodgeable: false);
                }
            }
        }

        void ClearZones()
        {
            foreach (var zone in zones)
                zone.Marker.Destroy();
            zones.Clear();
        }

        /// <summary>Adds walking in from the arena's rim, already after the player, up to a cap.</summary>
        protected void SpawnAdds(EnemyDefinition definition, int count, int max)
        {
            if (definition == null)
                return;
            adds.RemoveAll(a => a == null || !a.IsAlive);
            for (var i = 0; i < count && adds.Count < max; i++)
            {
                var angle = UnityEngine.Random.value * Mathf.PI * 2f;
                var at = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (arenaRadius - 2f);
                if (!manager.Nav.IsWalkable(IsoMath.GroundToCell(at)))
                    continue;
                adds.Add(manager.Spawn(definition, at, true, null, level));
            }
        }

        protected void WalkTowards(Vector2 target, float stopAt)
        {
            var offset = target - boss.GroundPosition;
            var distance = offset.magnitude;
            if (distance <= stopAt)
                return;
            var step = Mathf.Min(MoveSpeed * frameDelta, distance - stopAt);
            var next = ClampToArena(boss.GroundPosition + offset / distance * step, 1f);
            boss.ScriptedMove(next - boss.GroundPosition);
        }

        /// <summary>Puts the boss somewhere at once (a teleport), kept inside the arena on walkable ground.</summary>
        protected void Teleport(Vector2 to)
        {
            to = ClampToArena(to, 1.5f);
            if (manager.Nav.IsWalkable(IsoMath.GroundToCell(to)))
                boss.ScriptedMove(to - boss.GroundPosition);
        }

        protected Vector2 ClampToArena(Vector2 point, float margin)
        {
            var offset = point - center;
            var max = arenaRadius - margin;
            return offset.magnitude > max ? center + offset.normalized * max : point;
        }

        protected GroundMarker Track(GroundMarker marker)
        {
            attackMarkers.Add(marker);
            return marker;
        }

        protected void ClearAttackMarkers()
        {
            foreach (var marker in attackMarkers)
                marker.Destroy();
            attackMarkers.Clear();
        }

        void CancelAttack()
        {
            attack.Clear();
            ClearAttackMarkers();
        }

        /// <summary>Steps the running attack by one frame. False when it has ended.</summary>
        bool StepAttack()
        {
            while (attack.Count > 0)
            {
                var top = attack.Peek();
                if (top.MoveNext())
                {
                    if (top.Current is IEnumerator nested)
                    {
                        attack.Push(nested);
                        continue;
                    }
                    return true;
                }
                attack.Pop();
            }
            return false;
        }
    }
}
