using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Act 1 boss fight (Docs/05-world-and-content.md, bosses): the Cinder Warden in its circular arena, three
    /// phases, telegraphed attacks only, and a stagger meter. Phase 1 slams and fires ember volleys; phase 2 adds
    /// burning husks and sets the arena edge alight; phase 3 charges in quick chains and calls down ember rain.
    /// <para>
    /// The boss itself is an ordinary pooled <see cref="EnemyController"/> in <see cref="EnemyController.Scripted"/>
    /// mode, so targeting, damage, the kill, its loot (<see cref="LootSource.Boss"/>) and its XP work as for any enemy;
    /// this component moves it and attacks for it. Attacks are iterators stepped with the game clock
    /// (<see cref="Time.deltaTime"/>, so the inventory pause and hit stop freeze them), which lets a stagger drop the
    /// current one mid-telegraph. Every number here is tuning; the docs fix only the shapes of the fight.
    /// </para>
    /// Docs/01-core-gameplay.md design rule: a player at gear parity dies only through repeated telegraph mistakes.
    /// Every damaging attack is telegraphed: ground circles fill for 1 to 1.2 seconds, projectile lines show for 0.4,
    /// charge lines for 0.5.
    /// </summary>
    public class CinderWardenFight : MonoBehaviour
    {
        public const string BossName = "Cinder Warden";

        // Attack damage as a multiple of the boss level's base enemy hit (CombatFormulas.EnemyHitDamage). Tuning.
        const float SlamDamage = 1.6f;
        const float EmberDamage = 0.6f;
        const float ChargeDamage = 1.3f;
        const float RainDamage = 0.9f;
        const float FireDamagePerSecond = 0.7f;

        // Docs/03-itemization.md: elite and boss damage ignores 15 percent of player armor.
        const float ArmorIgnore = 0.15f;

        const float MoveSpeed = 2.4f;
        const float KeepDistance = 2.2f;
        const float SlamRadius = 3.2f;
        const float SlamSeconds = 1.1f;
        const int VolleyCount = 5;
        const float VolleySpreadDegrees = 12f;
        const float EmberSpeed = 7f;
        const float EmberRadius = 0.45f;
        const int ChargesPerChain = 3;
        const float ChargeSpeed = 15f;
        const float ChargeHitRadius = 1.2f;
        const int RainDrops = 5;
        const float RainRadius = 1.5f;
        const float RainSeconds = 1.2f;
        const float FireBandWidth = 3.5f;
        const float AddsEverySeconds = 12f;
        const int AddsPerWave = 4;
        const int MaxAdds = 10;

        // The player may step this far outside the arena before the boss gives up and walks back to its middle.
        const float LeaveMargin = 3f;

        static readonly Color TelegraphColor = new Color(1f, 0.22f, 0.12f, 0.95f);
        static readonly Color FireColor = new Color(1f, 0.45f, 0.1f, 0.8f);
        static readonly Color EmberColor = new Color(1f, 0.6f, 0.2f, 1f);

        EnemyDefinition bossDefinition;
        EnemyDefinition addDefinition;
        int level;
        Vector2 center;
        float arenaRadius;
        string killKey;
        Action onDefeated;

        EnemyManager manager;
        EnemyController boss;
        BossBar bar;
        readonly StaggerMeter stagger = new StaggerMeter();

        bool engaged;
        bool ended;
        int phase = 1;
        int attackIndex;
        float cooldown = 1.2f;
        float addsTimer;
        float fireTickTimer;
        float frameDelta;

        IEnumerator attack;
        readonly List<GroundMarker> attackMarkers = new List<GroundMarker>();
        GroundMarker fireRing;
        bool fireActive;
        readonly List<Ember> embers = new List<Ember>();
        readonly List<EnemyController> adds = new List<EnemyController>();

        sealed class Ember
        {
            public Transform Transform;
            public Vector2 Position;
            public Vector2 Velocity;
        }

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
            AdvanceMarkers();
            AdvanceEmbers();

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

            TickFire(player);
            TickAdds();

            // A staggered boss stands still and does nothing: the window the stagger meter exists for.
            if (stagger.IsStaggered)
                return;

            if (attack != null)
            {
                if (!attack.MoveNext())
                {
                    attack = null;
                    ClearAttackMarkers();
                }
                return;
            }

            WalkTowards(player, KeepDistance);
            cooldown -= frameDelta;
            if (cooldown <= 0f)
                attack = NextAttack();
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

            if (phase >= 2 && fireRing == null)
            {
                // Docs: phase 2, the arena edge ignites. It is telegraphed like anything else: it fades in before it burns.
                fireRing = GroundMarker.Ring(center, arenaRadius, 1.5f, FireColor, transform);
                addsTimer = 2f;
            }
        }

        IEnumerator NextAttack()
        {
            attackIndex++;
            switch (phase)
            {
                case 1:
                    return attackIndex % 2 == 1 ? Slam() : Volley();
                case 2:
                    return attackIndex % 3 == 0 ? Volley() : Slam();
                default:
                    return attackIndex % 3 == 0 ? Slam() : attackIndex % 3 == 1 ? Charges() : Rain();
            }
        }

        // Phase 1 and 2: walk in, then a ground pound around the boss that fills for a second.
        IEnumerator Slam()
        {
            var approach = 1.5f;
            while (approach > 0f && Vector2.Distance(boss.GroundPosition, manager.PlayerGround) > KeepDistance)
            {
                approach -= frameDelta;
                WalkTowards(manager.PlayerGround, KeepDistance);
                yield return null;
            }

            var at = boss.GroundPosition;
            var marker = Track(GroundMarker.Circle(at, SlamRadius, SlamSeconds, TelegraphColor, transform));
            while (!marker.Done)
                yield return null;

            Sfx.Play(SoundId.EnemySlam);
            if (Vector2.Distance(manager.PlayerGround, at) <= SlamRadius)
                HitPlayer(SlamDamage, dodgeable: false);
            cooldown = 0.9f;
        }

        // Phase 1 and 2: a fan of embers at the player, each shown as a line first.
        IEnumerator Volley()
        {
            var origin = boss.GroundPosition;
            var aim = (manager.PlayerGround - origin).normalized;
            if (aim == Vector2.zero)
                aim = Vector2.up;

            var directions = new Vector2[VolleyCount];
            for (var i = 0; i < VolleyCount; i++)
            {
                var angle = (i - (VolleyCount - 1) / 2f) * VolleySpreadDegrees * Mathf.Deg2Rad;
                directions[i] = new Vector2(aim.x * Mathf.Cos(angle) - aim.y * Mathf.Sin(angle), aim.x * Mathf.Sin(angle) + aim.y * Mathf.Cos(angle));
                Track(GroundMarker.Line(origin, origin + directions[i] * arenaRadius * 1.6f, 0.08f, 0.4f, TelegraphColor, transform));
            }

            var wait = 0.4f;
            while (wait > 0f)
            {
                wait -= frameDelta;
                yield return null;
            }

            foreach (var direction in directions)
                LaunchEmber(origin, direction * EmberSpeed);
            cooldown = 0.8f;
        }

        // Phase 3: three quick charges, each a line to a point just past the player and half a second's windup.
        IEnumerator Charges()
        {
            for (var n = 0; n < ChargesPerChain; n++)
            {
                var from = boss.GroundPosition;
                var toPlayer = manager.PlayerGround - from;
                var target = manager.PlayerGround + (toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized * 2f : Vector2.zero);
                target = ClampToArena(target, 1.5f);

                var line = Track(GroundMarker.Line(from, target, 0.9f, 0.5f, TelegraphColor, transform));
                while (!line.Done)
                    yield return null;
                ClearAttackMarkers();

                var hit = false;
                var travelled = 0f;
                var length = Vector2.Distance(from, target);
                var direction = length > 0f ? (target - from) / length : Vector2.zero;
                while (travelled < length)
                {
                    var step = Mathf.Min(ChargeSpeed * frameDelta, length - travelled);
                    travelled += step;
                    boss.ScriptedMove(direction * step);
                    if (!hit && Vector2.Distance(manager.PlayerGround, boss.GroundPosition) <= ChargeHitRadius)
                    {
                        hit = true;
                        HitPlayer(ChargeDamage);
                    }
                    yield return null;
                }

                var pause = 0.35f;
                while (pause > 0f)
                {
                    pause -= frameDelta;
                    yield return null;
                }
            }
            cooldown = 1.2f;
        }

        // Phase 3: circles fall around the player, one after another, each filling before it lands.
        IEnumerator Rain()
        {
            var drops = new List<(Vector2 at, GroundMarker marker)>();
            for (var i = 0; i < RainDrops; i++)
            {
                var offset = UnityEngine.Random.insideUnitCircle * 3.5f;
                var at = ClampToArena(manager.PlayerGround + offset, RainRadius);
                drops.Add((at, Track(GroundMarker.Circle(at, RainRadius, RainSeconds, TelegraphColor, transform))));

                var gap = 0.18f;
                while (gap > 0f)
                {
                    gap -= frameDelta;
                    yield return null;
                }
            }

            var landed = 0;
            while (landed < drops.Count)
            {
                landed = 0;
                foreach (var (at, marker) in drops)
                {
                    if (!marker.Done)
                        continue;
                    landed++;
                }
                yield return null;
            }

            // Each drop hits on landing; one hit per rain, so overlapping circles cannot stack into an unfair burst.
            foreach (var (at, _) in drops)
                if (Vector2.Distance(manager.PlayerGround, at) <= RainRadius)
                {
                    HitPlayer(RainDamage, dodgeable: false);
                    break;
                }
            cooldown = 1f;
        }

        void TickFire(Vector2 player)
        {
            if (fireRing == null)
                return;
            fireActive = fireRing.Done;
            if (!fireActive)
                return;

            var distance = Vector2.Distance(player, center);
            if (distance < arenaRadius - FireBandWidth || distance > arenaRadius + 0.5f)
            {
                fireTickTimer = 0f;
                return;
            }

            fireTickTimer -= frameDelta;
            if (fireTickTimer <= 0f)
            {
                fireTickTimer = 0.5f;
                HitPlayer(FireDamagePerSecond * 0.5f, dodgeable: false);
            }
        }

        // Docs: phase 2 spawns burning husks. They come in from the arena edge, already after the player.
        void TickAdds()
        {
            if (phase < 2)
                return;

            adds.RemoveAll(a => a == null || !a.IsAlive);
            addsTimer -= frameDelta;
            if (addsTimer > 0f)
                return;
            addsTimer = AddsEverySeconds;

            for (var i = 0; i < AddsPerWave && adds.Count < MaxAdds; i++)
            {
                var angle = UnityEngine.Random.value * Mathf.PI * 2f;
                var at = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (arenaRadius - 2f);
                if (!manager.Nav.IsWalkable(IsoMath.GroundToCell(at)))
                    continue;
                adds.Add(manager.Spawn(addDefinition, at, true, null, level));
            }
        }

        void EndFight()
        {
            ended = true;
            CancelAttack();
            fireRing?.Destroy();
            fireRing = null;
            foreach (var ember in embers)
                Destroy(ember.Transform.gameObject);
            embers.Clear();

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

        // Docs/01: dodge works on projectiles and melee (the embers, a charge), not on ground shapes (slam, rain, fire).
        void HitPlayer(float multiplier, bool dodgeable = true)
        {
            if (manager.Player == null || !manager.Player.IsAlive)
                return;
            var damage = CombatFormulas.EnemyHitDamage(level) * bossDefinition.DamageMultiplier * multiplier;
            manager.Player.TakeHit(damage, level, ArmorIgnore, dodgeable);
        }

        void WalkTowards(Vector2 target, float stopAt)
        {
            var offset = target - boss.GroundPosition;
            var distance = offset.magnitude;
            if (distance <= stopAt)
                return;
            var step = Mathf.Min(MoveSpeed * frameDelta, distance - stopAt);
            var next = ClampToArena(boss.GroundPosition + offset / distance * step, 1f);
            boss.ScriptedMove(next - boss.GroundPosition);
        }

        Vector2 ClampToArena(Vector2 point, float margin)
        {
            var offset = point - center;
            var max = arenaRadius - margin;
            return offset.magnitude > max ? center + offset.normalized * max : point;
        }

        GroundMarker Track(GroundMarker marker)
        {
            attackMarkers.Add(marker);
            return marker;
        }

        void AdvanceMarkers()
        {
            for (var i = 0; i < attackMarkers.Count; i++)
                attackMarkers[i].Advance(frameDelta);
            fireRing?.Advance(frameDelta);
        }

        void ClearAttackMarkers()
        {
            foreach (var marker in attackMarkers)
                marker.Destroy();
            attackMarkers.Clear();
        }

        void CancelAttack()
        {
            attack = null;
            ClearAttackMarkers();
        }

        void LaunchEmber(Vector2 origin, Vector2 velocity)
        {
            var go = GroundMarker.NewSprite("Ember", TelegraphArt.Ember, EmberColor, transform, 60);
            go.GetComponent<SpriteRenderer>().sortingLayerName = GameSortingLayers.Effects;
            go.transform.localScale = new Vector3(EmberRadius * 2f, EmberRadius * 2f, 1f);
            go.transform.position = IsoMath.GroundToWorld(origin);
            embers.Add(new Ember { Transform = go.transform, Position = origin, Velocity = velocity });
        }

        void AdvanceEmbers()
        {
            for (var i = embers.Count - 1; i >= 0; i--)
            {
                var ember = embers[i];
                ember.Position += ember.Velocity * frameDelta;
                ember.Transform.position = IsoMath.GroundToWorld(ember.Position);

                var hitPlayer = Vector2.Distance(ember.Position, manager.PlayerGround) <= EmberRadius;
                var gone = Vector2.Distance(ember.Position, center) > arenaRadius + 1f ||
                           !manager.Nav.IsWalkable(IsoMath.GroundToCell(ember.Position));
                if (hitPlayer)
                    HitPlayer(EmberDamage);
                if (hitPlayer || gone)
                {
                    Destroy(ember.Transform.gameObject);
                    embers.RemoveAt(i);
                }
            }
        }
    }
}
