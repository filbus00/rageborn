using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Gaoler, the boss at depth 18 in the middle of the Iron Gaol (2026-10-12, reworked 2026-11-16 as the first of
    /// the six unfinished fights; the story: when the fires died he kept the Iron Hold's doors locked with everyone
    /// inside). A huge, slow jailer in an iron helm swinging a chain, a ring of keys at his belt. Warned by circles on the
    /// ground and symbols on her ring, never lines. Phase 1, the Round: the Iron Ring (a circle of radius 3 round him),
    /// the Key Toss (a fan of three bouncing keys) and the Hammer (a short leap onto her). Phase 2 (2/3 life): he swings
    /// his chain, the Chain Whirl (a spin she must stay out of), and every 16 s opens three cells on the rim, each
    /// marked a moment before the prisoners come out (up to 8). Phase 3 (1/3): the Lockdown, a ring of cages falling
    /// round her with one gap to run through, the Rush (a charge), bigger hammers, and two cells at a time. Claude's
    /// design, to review. Tuning.
    /// </summary>
    public class GaolerFight : BossFight
    {
        public const string Name = "The Gaoler";
        static readonly Color BrassColor = new Color(0.95f, 0.78f, 0.3f, 0.9f);
        static readonly Color CellColor = new Color(0.8f, 0.85f, 0.95f, 0.55f);

        float cellsTimer;

        protected override string BossName => Name;
        protected override float MoveSpeed => phase >= 3 ? 2.6f : 2.1f;
        protected override float KeepDistance => 2.6f;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 2)
                cellsTimer = 2f;
        }

        protected override void TickFight(Vector2 player)
        {
            if (phase < 2)
                return;
            cellsTimer -= frameDelta;
            if (cellsTimer > 0f)
                return;
            cellsTimer = phase >= 3 ? 20f : 16f;
            OpenCells(phase >= 3 ? 2 : 3);
        }

        protected override IEnumerator NextAttack()
        {
            switch (phase)
            {
                case 1:
                    switch (Pick(3))
                    {
                        case 0: return Ring();
                        case 1: return KeyToss();
                        default: return Hammer(2.2f);
                    }
                case 2:
                    switch (Pick(4))
                    {
                        case 0: return Ring();
                        case 1: return KeyToss();
                        case 2: return Hammer(2.4f);
                        default: return ChainWhirl();
                    }
                default:
                    switch (Pick(5))
                    {
                        case 0: return Lockdown();
                        case 1: return Rush();
                        case 2: return Hammer(3f);
                        case 3: return ChainWhirl();
                        default: return Ring();
                    }
            }
        }

        // The cells on the rim open one after another: a circle shows where, then the prisoners come out of it.
        void OpenCells(int count)
        {
            var turn = Random.value;
            for (var i = 0; i < count; i++)
            {
                var at = RimPoint(turn + i / (float)count);
                LastingZone(at, 1.5f, 1.4f, 0.05f, 0f, 0f, CellColor);
                StartCoroutine(Release(at, 1.4f));
            }
            Sfx.Play(SoundId.EnemySlam, 0.4f);
        }

        // On its own clock, beside the boss's attacks.
        IEnumerator Release(Vector2 at, float after)
        {
            while (after > 0f)
            {
                after -= Time.deltaTime;
                yield return null;
            }
            if (boss != null && boss.IsAlive)
                SpawnAddAt(addDefinition, at, 8);
        }

        IEnumerator Ring()
        {
            yield return CircleStrike(boss.GroundPosition, 3f, 0.9f, 1.3f);
            cooldown = 1f;
        }

        // Three keys on his ring flung at her, bouncing off the walls.
        IEnumerator KeyToss()
        {
            yield return Wait(0.3f);
            yield return Fan(3, 16f, 7f, 0.45f, 0.9f, BrassColor, kind: ProjectileKind.Bouncing);
            cooldown = 1f;
        }

        // A short leap onto where she stands and the slam.
        IEnumerator Hammer(float radius)
        {
            yield return Approach(1.5f, 6f);
            yield return Leap(manager.PlayerGround, radius, 1.6f, 1.2f, 1.1f);
            cooldown = 1.2f;
        }

        // The chain swung round him: she keeps out of its reach while he lumbers after her.
        IEnumerator ChainWhirl()
        {
            yield return Whirl(3.6f, 2.5f, 0.7f, true, CellColor);
            cooldown = 1.3f;
        }

        IEnumerator Rush()
        {
            yield return Charge(manager.PlayerGround, 0.8f, 10f, 1.5f);
            cooldown = 1.2f;
        }

        // Cages fall in a ring round her and on her spot, all but one place: she runs through the gap before they land.
        IEnumerator Lockdown()
        {
            const float radius = 1.3f;
            var around = manager.PlayerGround;
            var gap = Random.Range(0, 8);
            var spots = new List<Vector2> { ClampToArena(around, 1.4f) };
            for (var i = 0; i < 8; i++)
                if (i != gap)
                    spots.Add(ClampToArena(Round(around, 2.6f, i / 8f), radius));
            for (var i = 0; i < spots.Count; i++)
                Track(GroundMarker.Circle(spots[i], i == 0 ? 1.4f : radius, 1.4f, TelegraphColor, transform));
            yield return Wait(1.4f);
            Sfx.Play(SoundId.EnemySlam, 1f);
            for (var i = 0; i < spots.Count; i++)
                if (PlayerWithin(spots[i], i == 0 ? 1.4f : radius))
                {
                    HitPlayer(1.4f, dodgeable: false);
                    SlowPlayer(0.4f, 1.5f);
                    break;
                }
            cooldown = 1.3f;
        }
    }

    /// <summary>
    /// The Hanged Magistrate, the boss at depth 24 at the bottom of the Iron Gaol (2026-10-12, reworked 2026-11-16; the
    /// story: the Hold's fire burned in his court, he was hanged by it and still passes sentence). A gaunt judge in a black
    /// robe and a powdered wig, a noose round his neck, drifting rather than walking, keeping his distance. Phase 1, the
    /// Trial: the Sentence (a circle under her), the Gavel (a fan of five bolts) and the Noose (two rings of circles, the
    /// outer then the inner, so she must step from one gap to the next). Phase 2 (2/3 life): the Verdict (a circle on her
    /// and six round her, each leaving rope that holds her), and his bailiffs come every 15 s. Phase 3 (1/3): the
    /// Execution, a big circle that follows her, then holds, then falls; the Sentence twice from a new place; the Gavel
    /// seven wide. Claude's design, to review. Tuning.
    /// </summary>
    public class HangedMagistrateFight : BossFight
    {
        public const string Name = "The Hanged Magistrate";
        static readonly Color BoltColor = new Color(0.85f, 0.75f, 1f, 0.9f);
        static readonly Color RopeColor = new Color(0.55f, 0.32f, 0.2f, 0.55f);
        static readonly Color CourtColor = new Color(0.82f, 0.78f, 0.95f, 0.9f);

        float bailiffTimer;

        protected override string BossName => Name;

        protected override float KeepDistance => 5f;
        protected override float MoveSpeed => phase >= 3 ? 3f : 2.4f;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 2)
                bailiffTimer = 2f;
        }

        protected override void TickFight(Vector2 player)
        {
            if (phase < 2)
                return;
            bailiffTimer -= frameDelta;
            if (bailiffTimer > 0f)
                return;
            bailiffTimer = 15f;
            SpawnAdds(addDefinition, 2, 4);
        }

        protected override IEnumerator NextAttack()
        {
            switch (phase)
            {
                case 1:
                    switch (Pick(3))
                    {
                        case 0: return Sentence(1, false);
                        case 1: return Gavel(5);
                        default: return Noose();
                    }
                case 2:
                    switch (Pick(4))
                    {
                        case 0: return Verdict();
                        case 1: return Sentence(1, false);
                        case 2: return Gavel(5);
                        default: return Noose();
                    }
                default:
                    switch (Pick(5))
                    {
                        case 0: return Execution();
                        case 1: return Sentence(2, true);
                        case 2: return Gavel(7);
                        case 3: return Verdict();
                        default: return Noose();
                    }
            }
        }

        IEnumerator Sentence(int times, bool drift)
        {
            if (drift)
                Teleport(ClampToArena(center + Random.insideUnitCircle * arenaRadius * 0.7f, 2f));
            for (var n = 0; n < times; n++)
            {
                yield return CircleStrike(ClampToArena(manager.PlayerGround, 2.2f), 2.2f, 1.2f, 1.4f);
                yield return Wait(0.2f);
            }
            cooldown = 1f;
        }

        IEnumerator Gavel(int bolts)
        {
            yield return Wait(0.4f);
            yield return Fan(bolts, 14f, 6f, 0.4f, 0.9f, BoltColor);
            cooldown = 1.1f;
        }

        // Two rings of circles round her, the outer one first and the inner a beat after: she steps between them.
        IEnumerator Noose()
        {
            const float radius = 1.3f;
            var around = manager.PlayerGround;
            var turn = Random.value;
            var outer = new List<Vector2>();
            for (var i = 0; i < 6; i++)
                outer.Add(ClampToArena(Round(around, 4.2f, turn + i / 6f), radius));
            foreach (var at in outer)
                Track(GroundMarker.Circle(at, radius, 1f, TelegraphColor, transform));
            yield return Wait(1f);
            Sfx.Play(SoundId.EnemySlam, 0.6f);
            foreach (var at in outer)
                if (PlayerWithin(at, radius))
                {
                    HitPlayer(1.1f, dodgeable: false);
                    break;
                }
            ClearAttackMarkers();
            var inner = new List<Vector2>();
            for (var i = 0; i < 4; i++)
                inner.Add(ClampToArena(Round(around, 1.9f, turn + 0.08f + i / 4f), radius));
            foreach (var at in inner)
                Track(GroundMarker.Circle(at, radius, 0.9f, TelegraphColor, transform));
            yield return Wait(0.9f);
            Sfx.Play(SoundId.EnemySlam, 0.6f);
            foreach (var at in inner)
                if (PlayerWithin(at, radius))
                {
                    HitPlayer(1.1f, dodgeable: false);
                    break;
                }
            cooldown = 1.2f;
        }

        // A circle on her and six round her; each leaves rope on the floor that holds and hurts a while.
        IEnumerator Verdict()
        {
            const float radius = 1.6f;
            var spots = new List<Vector2> { ClampToArena(manager.PlayerGround, radius) };
            var turn = Random.value;
            for (var i = 0; i < 6; i++)
                spots.Add(ClampToArena(Round(manager.PlayerGround, 3.4f, turn + i / 6f), radius));
            foreach (var at in spots)
                Track(GroundMarker.Circle(at, radius, 1f, WarningOf(RopeColor), transform));
            yield return Wait(1f);
            Sfx.Play(SoundId.EnemySlam, 0.7f);
            foreach (var at in spots)
                if (PlayerWithin(at, radius))
                {
                    HitPlayer(1.2f, dodgeable: false);
                    break;
                }
            foreach (var at in spots)
                LastingZone(at, radius, 0f, 2.5f, 0.4f, 0.25f, RopeColor);
            cooldown = 1.3f;
        }

        // The last sentence: a big circle follows her, holds still a moment, then falls.
        IEnumerator Execution()
        {
            yield return Tracking(2.6f, 2.2f, 0.8f, 1.8f, CourtColor);
            cooldown = 1.2f;
        }
    }
}
