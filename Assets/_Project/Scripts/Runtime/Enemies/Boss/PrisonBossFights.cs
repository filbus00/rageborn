using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Gaoler, the boss at depth 18 in the middle of the prison (2026-10-12, the owner: two new prison bosses; the
    /// story: when the fires died he kept the Iron Hold's doors locked with everyone inside). A huge jailer in iron and
    /// leather swinging a chain. Phase 1: the Chain Lash (a band 7 long and 1.1 wide toward her, 0.75 s, 1.2) and the
    /// Iron Ring (a circle of radius 3 round him, 0.9 s, 1.3). Phase 2: the Chains, three lashes at once in a fan 25
    /// degrees apart; he lets prisoners (the hollowed) out of their cells every 13 s, 3 at a time, up to 8. Phase 3: he
    /// rushes her (a charge, 1.5) and drops cages: three circles of radius 1.2 at and round her (0.8 s, 1.1) that hold
    /// her, slowed by 40 percent for 1.5 s. Claude's design, to review. Tuning.
    /// </summary>
    public class GaolerFight : BossFight
    {
        public const string Name = "The Gaoler";

        float prisonersTimer;

        protected override string BossName => Name;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 2)
                prisonersTimer = 2f;
        }

        protected override void TickFight(Vector2 player)
        {
            if (phase < 2)
                return;
            prisonersTimer -= frameDelta;
            if (prisonersTimer > 0f)
                return;
            prisonersTimer = 13f;
            SpawnAdds(addDefinition, 3, 8);
        }

        protected override IEnumerator NextAttack()
        {
            switch (phase)
            {
                case 1:
                    return attackIndex % 2 == 0 ? Lash() : Ring();
                case 2:
                    return attackIndex % 3 == 0 ? Chains() : attackIndex % 3 == 1 ? Lash() : Ring();
                default:
                    return attackIndex % 3 == 0 ? Rush() : attackIndex % 3 == 1 ? Cages() : Chains();
            }
        }

        Vector2 Aim()
        {
            var aim = (manager.PlayerGround - boss.GroundPosition).normalized;
            return aim == Vector2.zero ? Vector2.up : aim;
        }

        IEnumerator Lash()
        {
            var from = boss.GroundPosition;
            yield return LineStrike(from, from + Aim() * 7f, 1.1f, 0.75f, 1.2f);
            cooldown = 0.9f;
        }

        IEnumerator Ring()
        {
            yield return CircleStrike(boss.GroundPosition, 3f, 0.9f, 1.3f);
            cooldown = 1f;
        }

        // Three lashes at once, fanned 25 degrees apart.
        IEnumerator Chains()
        {
            var from = boss.GroundPosition;
            var aim = Aim();
            var ends = new Vector2[3];
            for (var i = 0; i < 3; i++)
            {
                var a = (i - 1) * 25f * Mathf.Deg2Rad;
                var d = new Vector2(aim.x * Mathf.Cos(a) - aim.y * Mathf.Sin(a), aim.x * Mathf.Sin(a) + aim.y * Mathf.Cos(a));
                ends[i] = from + d * 7f;
                Track(GroundMarker.Line(from, ends[i], 1.1f, 0.85f, TelegraphColor, transform));
            }
            yield return Wait(0.85f);
            Sfx.Play(SoundId.EnemySlam, 0.8f);
            foreach (var end in ends)
                if (PlayerOnLine(from, end, 1.1f))
                {
                    HitPlayer(1.2f, dodgeable: false);
                    break;
                }
            cooldown = 1.1f;
        }

        IEnumerator Rush()
        {
            yield return Charge(manager.PlayerGround, 0.8f, 10f, 1.5f);
            cooldown = 1.2f;
        }

        // Cages dropped on and round her: caught in one, she is held, slowed.
        IEnumerator Cages()
        {
            const float radius = 1.2f;
            var spots = new List<Vector2> { ClampToArena(manager.PlayerGround, radius) };
            for (var i = 0; i < 2; i++)
                spots.Add(ClampToArena(manager.PlayerGround + Random.insideUnitCircle.normalized * 2.4f, radius));
            foreach (var at in spots)
                Track(GroundMarker.Circle(at, radius, 0.8f, TelegraphColor, transform));
            yield return Wait(0.8f);
            Sfx.Play(SoundId.EnemySlam, 0.9f);
            foreach (var at in spots)
                if (PlayerWithin(at, radius))
                {
                    HitPlayer(1.1f, dodgeable: false);
                    SlowPlayer(0.4f, 1.5f);
                    break;
                }
            cooldown = 1f;
        }
    }

    /// <summary>
    /// The Hanged Magistrate, the boss at depth 24 at the bottom of the prison (2026-10-12; the story: the Hold's fire
    /// burned in his court, he was hanged by it and still passes sentence). A gaunt judge in a black robe and a powdered
    /// wig, a noose round his neck and its rope trailing, drifting rather than walking. He keeps his distance. Phase 1:
    /// the Sentence (a circle of radius 2.2 under her, 1.2 s, 1.4) and the Gavel (a fan of 5 bolts, 0.9). Phase 2: the
    /// Verdict (a circle on her and six round her, radius 1.6, 1.0 s, 1.2, each leaving rope that holds for 2.5 s: 0.4 a
    /// second, slowed 25 percent); his bailiffs (skeleton knights) come every 15 s, 2 at a time, up to 4. Phase 3: he
    /// drifts across the court before each Sentence, which falls twice, and the Gavel goes 7 wide. Claude's design, to
    /// review. Tuning.
    /// </summary>
    public class HangedMagistrateFight : BossFight
    {
        public const string Name = "The Hanged Magistrate";
        static readonly Color BoltColor = new Color(0.85f, 0.75f, 1f, 0.9f);
        static readonly Color RopeColor = new Color(0.55f, 0.32f, 0.2f, 0.55f);

        float bailiffTimer;

        protected override string BossName => Name;

        protected override float KeepDistance => 5f;

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
                    return attackIndex % 2 == 0 ? Sentence(1, false) : Gavel(5);
                case 2:
                    return attackIndex % 3 == 0 ? Verdict() : attackIndex % 3 == 1 ? Sentence(1, false) : Gavel(5);
                default:
                    return attackIndex % 3 == 0 ? Verdict() : attackIndex % 3 == 1 ? Sentence(2, true) : Gavel(7);
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

        // A circle on her and six round her; each leaves rope on the floor that holds and hurts a while.
        IEnumerator Verdict()
        {
            const float radius = 1.6f;
            var spots = new List<Vector2> { ClampToArena(manager.PlayerGround, radius) };
            var turn = Random.value * Mathf.PI * 2f;
            for (var i = 0; i < 6; i++)
            {
                var a = turn + Mathf.PI * 2f * i / 6;
                spots.Add(ClampToArena(manager.PlayerGround + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3.4f, radius));
            }
            foreach (var at in spots)
                Track(GroundMarker.Circle(at, radius, 1f, TelegraphColor, transform));
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
    }
}
