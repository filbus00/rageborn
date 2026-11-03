using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Bandit King, the forest's boss at depth 6 (the owner, 2026-11-02, "Duelist with a gang"; Docs/08): phase 1 sword
    /// combos and a leaping slam; phase 2 (2/3 life) he whistles in waves of cutthroats and bowmen while he throws knives;
    /// phase 3 (1/3) enraged, a spinning sword whirl that chases her and bigger leaping slams. Warned by circles on the
    /// ground and symbols on her ring, never lines. Claude's numbers, to review.
    /// </summary>
    public class BanditKingFight : BossFight
    {
        public const string Name = "The Bandit King";

        /// <summary>The bowmen he whistles in beside his cutthroats (the adds).</summary>
        public EnemyDefinition Bowmen;

        static readonly Color KnifeColor = new Color(0.85f, 0.87f, 0.9f, 1f);
        float addsTimer;

        protected override string BossName => Name;
        protected override bool RingWarnings => true;
        protected override float MoveSpeed => phase >= 3 ? 3.4f : 2.8f;
        protected override float KeepDistance => 1.6f;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 2)
                addsTimer = 1f;
        }

        protected override void TickFight(Vector2 player)
        {
            if (phase < 2)
                return;
            addsTimer -= frameDelta;
            if (addsTimer > 0f)
                return;
            // A whistle: two cutthroats and a bowman from the camp's edge.
            addsTimer = phase >= 3 ? 16f : 13f;
            Sfx.Play(SoundId.EnemySlam, 0.3f);
            SpawnAdds(addDefinition, 2, 6);
            SpawnAdds(Bowmen ?? addDefinition, 1, 6);
        }

        protected override IEnumerator NextAttack()
        {
            switch (phase)
            {
                case 1:
                    return attackIndex % 3 == 2 ? Leap(2.2f) : Combo();
                case 2:
                    return attackIndex % 3 == 0 ? Knives() : attackIndex % 3 == 1 ? Combo() : Leap(2.4f);
                default:
                    return attackIndex % 3 == 0 ? Whirl() : attackIndex % 3 == 1 ? Leap(3.2f) : Knives();
            }
        }

        // Walks in, then two quick sword cuts in front of him, each a circle that fills fast.
        IEnumerator Combo()
        {
            var approach = 2f;
            while (approach > 0f && Vector2.Distance(boss.GroundPosition, manager.PlayerGround) > KeepDistance)
            {
                approach -= frameDelta;
                WalkTowards(manager.PlayerGround, KeepDistance);
                yield return null;
            }
            for (var cut = 0; cut < 2; cut++)
            {
                var toward = manager.PlayerGround - boss.GroundPosition;
                var at = boss.GroundPosition + (toward.sqrMagnitude > 1e-4f ? toward.normalized : Vector2.down) * 1.1f;
                yield return CircleStrike(at, 1.4f, cut == 0 ? 0.6f : 0.4f, 1.3f);
            }
            cooldown = 0.8f;
        }

        // A leap onto where she stands: the landing circle fills, he lands there and the slam hits.
        IEnumerator Leap(float radius)
        {
            var at = ClampToArena(manager.PlayerGround, radius);
            var marker = Track(GroundMarker.Circle(at, radius, 1.0f, TelegraphColor, transform));
            while (!marker.Done)
                yield return null;
            Teleport(at);
            Sfx.Play(SoundId.EnemySlam, 0.9f);
            if (PlayerWithin(at, radius))
                HitPlayer(1.7f, dodgeable: false);
            cooldown = 1.1f;
        }

        // A fan of thrown knives.
        IEnumerator Knives()
        {
            yield return Fan(phase >= 3 ? 7 : 5, 11f, 11f, 0.35f, 0.7f, KnifeColor);
            cooldown = 0.9f;
        }

        // Enraged: he spins, sword out, after her for three seconds; close to him she is cut twice a second.
        IEnumerator Whirl()
        {
            manager.Warnings?.Warn(boss, boss.GroundPosition, 0.6f, IncomingWarnings.LungeColor, IncomingWarnings.Shape.Diamond);
            yield return Wait(0.6f);
            var left = 3f;
            var tick = 0f;
            while (left > 0f)
            {
                left -= frameDelta;
                tick -= frameDelta;
                WalkTowards(manager.PlayerGround, 0.2f);
                if (tick <= 0f && PlayerWithin(boss.GroundPosition, 1.7f))
                {
                    tick = 0.5f;
                    HitPlayer(0.6f);
                }
                yield return null;
            }
            cooldown = 1.4f;
        }
    }

    /// <summary>
    /// The Brood Mother, the forest's boss at depth 12 (the owner, 2026-11-02, "Brood and web"; Docs/08): phase 1 lunging
    /// bites and web globs that leave sticky patches; phase 2 (2/3 life) she climbs into the canopy out of reach while her
    /// egg sacs hatch waves of brood spiders, then drops down onto her; phase 3 (1/3) a poison spray before her and the
    /// arena slowly covered in web. Claude's numbers, to review.
    /// </summary>
    public class BroodMotherFight : BossFight
    {
        public const string Name = "The Brood Mother";

        static readonly Color WebColor = new Color(0.85f, 0.85f, 0.8f, 0.45f);
        static readonly Color PoisonColor = new Color(0.55f, 0.85f, 0.25f, 0.55f);
        float webTimer;
        bool climbed;

        protected override string BossName => Name;
        protected override bool RingWarnings => true;
        protected override float MoveSpeed => 2.6f;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 3)
                webTimer = 1f;
        }

        protected override void TickFight(Vector2 player)
        {
            // Phase 3: the arena slowly covered in web, a sticky patch every few seconds somewhere near her.
            if (phase < 3)
                return;
            webTimer -= frameDelta;
            if (webTimer > 0f)
                return;
            webTimer = 4f;
            var at = ClampToArena(center + Random.insideUnitCircle * (arenaRadius - 2f), 1.6f);
            LastingZone(at, 1.6f, 0.8f, 20f, 0f, 0.55f, WebColor);
        }

        protected override void ClearFight()
        {
            if (boss != null)
                boss.Aloft = 0f;
        }

        // Dropped while up in the canopy, she comes down at once, never staying out of reach.
        protected override void OnAttackCancelled()
        {
            if (boss != null)
                boss.Aloft = 0f;
        }

        protected override IEnumerator NextAttack()
        {
            // Into the canopy once as phase 2 begins, and again every fourth attack after.
            if (phase >= 2 && (!climbed || attackIndex % 4 == 0))
            {
                climbed = true;
                return Canopy();
            }
            switch (phase)
            {
                case 1:
                    return attackIndex % 2 == 0 ? Bite() : Webs(3);
                case 2:
                    return attackIndex % 2 == 0 ? Webs(4) : Bite();
                default:
                    return attackIndex % 3 == 0 ? Spray() : attackIndex % 3 == 1 ? Bite() : Webs(5);
            }
        }

        // A lunge at her, warned on her ring.
        IEnumerator Bite()
        {
            var toward = manager.PlayerGround - boss.GroundPosition;
            var target = manager.PlayerGround + (toward.sqrMagnitude > 0.01f ? toward.normalized * 1.5f : Vector2.zero);
            yield return Charge(target, 0.7f, 12f, 1.5f);
            cooldown = 1f;
        }

        // Web globs lobbed round her: each circle fills, hurts a little, and stays as a sticky patch that slows her.
        IEnumerator Webs(int count)
        {
            const float radius = 1.3f;
            var spots = new List<Vector2>();
            for (var i = 0; i < count; i++)
            {
                var at = ClampToArena(manager.PlayerGround + (i == 0 ? Vector2.zero : Random.insideUnitCircle * 3f), radius);
                spots.Add(at);
                Track(GroundMarker.Circle(at, radius, 0.9f, TelegraphColor, transform));
            }
            yield return Wait(0.9f);
            foreach (var at in spots)
                if (PlayerWithin(at, radius))
                {
                    HitPlayer(0.6f, dodgeable: false);
                    break;
                }
            foreach (var at in spots)
                LastingZone(at, radius, 0f, 6f, 0f, 0.5f, WebColor);
            cooldown = 1f;
        }

        // Up into the canopy out of reach: her egg sacs hatch two waves of her brood, then she drops onto her.
        IEnumerator Canopy()
        {
            var rise = 0f;
            while (rise < 1f)
            {
                rise += frameDelta / 0.6f;
                boss.Aloft = Mathf.Lerp(0.05f, 6f, rise);
                yield return null;
            }
            for (var wave = 0; wave < 2; wave++)
            {
                SpawnAdds(addDefinition, 4, 10);
                yield return Wait(4f);
            }
            var at = ClampToArena(manager.PlayerGround, 2.4f);
            var marker = Track(GroundMarker.Circle(at, 2.4f, 1.3f, TelegraphColor, transform));
            Teleport(at);
            while (!marker.Done)
            {
                boss.Aloft = Mathf.Lerp(6f, 0.05f, marker.Progress);
                yield return null;
            }
            boss.Aloft = 0f;
            Sfx.Play(SoundId.EnemySlam, 1f);
            if (PlayerWithin(at, 2.4f))
                HitPlayer(1.9f, dodgeable: false);
            cooldown = 1.2f;
        }

        // A spray of poison before her: three pools in a widening fan toward her, each poisoning for a while.
        IEnumerator Spray()
        {
            var origin = boss.GroundPosition;
            var aim = (manager.PlayerGround - origin).normalized;
            if (aim == Vector2.zero)
                aim = Vector2.down;
            var side = new Vector2(-aim.y, aim.x);
            manager.Warnings?.Warn(boss, origin, 0.8f, IncomingWarnings.ShotColor, IncomingWarnings.Shape.Round);
            var spots = new[] { origin + aim * 2f, origin + aim * 3.6f + side * 1.2f, origin + aim * 3.6f - side * 1.2f };
            foreach (var at in spots)
                LastingZone(ClampToArena(at, 1.3f), 1.3f, 0.8f, 4f, 0.5f, 0f, PoisonColor);
            yield return Wait(1f);
            cooldown = 1f;
        }
    }
}
