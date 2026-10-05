using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The first boss, at depth 6 (Docs/05-world-and-content.md, bosses): the Cinder Warden in its circular arena, three
    /// phases, telegraphed attacks only. Phase 1 slams and fires ember volleys; phase 2 adds burning husks and sets the
    /// arena edge alight; phase 3 charges in quick chains and calls down ember rain. Everything shared with the other
    /// bosses (engaging, phases, stagger, the bar, the shapes) is <see cref="BossFight"/>. Every number here is tuning;
    /// the docs fix only the shapes of the fight.
    /// </summary>
    public class CinderWardenFight : BossFight
    {
        public const string Name = "Cinder Warden";

        // Attack damage as a multiple of the boss level's base enemy hit (CombatFormulas.EnemyHitDamage). Tuning.
        const float SlamDamage = 1.6f;
        const float EmberDamage = 0.6f;
        const float ChargeDamage = 1.3f;
        const float RainDamage = 0.9f;
        const float FireDamagePerSecond = 0.7f;

        const float SlamRadius = 3.2f;
        const float SlamSeconds = 1.1f;
        const int VolleyCount = 5;
        const float VolleySpreadDegrees = 12f;
        const float EmberSpeed = 7f;
        const float EmberRadius = 0.45f;
        const int ChargesPerChain = 3;
        const float ChargeSpeed = 15f;
        const int RainDrops = 5;
        const float RainRadius = 1.5f;
        const float RainSeconds = 1.2f;
        const float FireBandWidth = 3.5f;
        const float AddsEverySeconds = 12f;
        const int AddsPerWave = 4;
        const int MaxAdds = 10;

        static readonly Color FireColor = new Color(1f, 0.45f, 0.1f, 0.8f);
        static readonly Color EmberColor = new Color(1f, 0.6f, 0.2f, 1f);

        GroundMarker fireRing;
        float fireTickTimer;
        float addsTimer;

        protected override string BossName => Name;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase >= 2 && fireRing == null)
            {
                // Docs: phase 2, the arena edge ignites. It is telegraphed like anything else: it fades in before it burns.
                fireRing = GroundMarker.Ring(center, arenaRadius, 1.5f, FireColor, transform);
                addsTimer = 2f;
            }
        }

        protected override IEnumerator NextAttack()
        {
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

        protected override void TickFight(Vector2 player)
        {
            fireRing?.Advance(frameDelta);
            TickFire(player);
            // Docs: phase 2 spawns burning husks. They come in from the arena edge, already after the player.
            if (phase < 2)
                return;
            addsTimer -= frameDelta;
            if (addsTimer > 0f)
                return;
            addsTimer = AddsEverySeconds;
            SpawnAdds(addDefinition, AddsPerWave, MaxAdds);
        }

        protected override void ClearFight()
        {
            fireRing?.Destroy();
            fireRing = null;
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
            yield return CircleStrike(boss.GroundPosition, SlamRadius, SlamSeconds, SlamDamage);
            cooldown = 0.9f;
        }

        // Phase 1 and 2: a fan of embers at the player, each shown as a line first.
        IEnumerator Volley()
        {
            yield return Fan(VolleyCount, VolleySpreadDegrees, EmberSpeed, EmberRadius, EmberDamage, EmberColor);
            cooldown = 0.8f;
        }

        // Phase 3: three quick charges, each a line to a point just past the player and half a second's windup.
        IEnumerator Charges()
        {
            for (var n = 0; n < ChargesPerChain; n++)
            {
                var toPlayer = manager.PlayerGround - boss.GroundPosition;
                var target = manager.PlayerGround + (toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized * 2f : Vector2.zero);
                yield return Charge(target, 0.5f, ChargeSpeed, ChargeDamage);
                yield return Wait(0.35f);
            }
            cooldown = 1.2f;
        }

        // Phase 3: circles fall around the player, one after another, each filling before it lands.
        IEnumerator Rain()
        {
            var drops = new List<(Vector2 at, GroundMarker marker)>();
            for (var i = 0; i < RainDrops; i++)
            {
                var offset = Random.insideUnitCircle * 3.5f;
                var at = ClampToArena(manager.PlayerGround + offset, RainRadius);
                drops.Add((at, Track(GroundMarker.Circle(at, RainRadius, RainSeconds, TelegraphColor, transform))));
                yield return Wait(0.18f);
            }
            while (!drops.TrueForAll(d => d.marker.Done))
                yield return null;
            // Each drop hits on landing; one hit per rain, so overlapping circles cannot stack into an unfair burst.
            foreach (var (at, _) in drops)
                if (PlayerWithin(at, RainRadius))
                {
                    HitPlayer(RainDamage, dodgeable: false);
                    break;
                }
            cooldown = 1f;
        }

        void TickFire(Vector2 player)
        {
            if (fireRing == null || !fireRing.Done)
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
    }
}
