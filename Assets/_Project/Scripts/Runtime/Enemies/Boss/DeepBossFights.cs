using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Tidewife, the boss at depth 12 (2026-10-05, from Docs/05's act 2 boss; the owner chose one dungeon with a boss
    /// every 6 levels). Phase 1: Tide Pools (3 circles of radius 2 near the player, a 1.0 s fill, 1.0 hit, leaving water
    /// that slows 40 percent for 4 s) and the Tentacle Lash (a band 7 long and 1.2 wide, 0.8 s, 1.4). Phase 2: the
    /// Flood, one side of the arena under water that fills over 2 s and stays 8 s (slows 40 percent, 0.5 hit a second),
    /// changing sides each time; pools of radius 3; drowned rise from the rim every 15 s, 3 at a time, up to 9.
    /// Phase 3: the Sweeping Wave every other attack (a band 3 wide across the whole arena with a 3-unit gap to stand in,
    /// 1.2 s, 1.6) and the lash twice in a row. Simplified from Docs/05: the flood is a large circle on one side rather
    /// than a half disc. Tuning.
    /// </summary>
    public class TidewifeFight : BossFight
    {
        public const string Name = "The Tidewife";
        static readonly Color WaterColor = new Color(0.25f, 0.55f, 0.85f, 0.55f);
        protected override Color TelegraphColor => WarningOf(WaterColor);


        float addsTimer;
        int floodSide = 1;

        protected override string BossName => Name;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 2)
                addsTimer = 3f;
        }

        protected override void TickFight(Vector2 player)
        {
            if (phase < 2)
                return;
            addsTimer -= frameDelta;
            if (addsTimer > 0f)
                return;
            addsTimer = 15f;
            SpawnAdds(addDefinition, 3, 9);
        }

        protected override IEnumerator NextAttack()
        {
            switch (phase)
            {
                case 1:
                    return attackIndex % 2 == 1 ? Pools(2f) : Lash(1);
                case 2:
                    return attackIndex % 3 == 0 ? Flood() : attackIndex % 3 == 1 ? Pools(3f) : Lash(1);
                default:
                    return attackIndex % 2 == 0 ? Wave() : Lash(2);
            }
        }

        IEnumerator Pools(float radius)
        {
            var spots = new List<Vector2>();
            for (var i = 0; i < 3; i++)
            {
                var at = ClampToArena(manager.PlayerGround + Random.insideUnitCircle * 3f, radius);
                spots.Add(at);
                Track(GroundMarker.Circle(at, radius, 1f, TelegraphColor, transform));
            }
            yield return Wait(1f);
            Sfx.Play(SoundId.EnemySlam, 0.6f);
            foreach (var at in spots)
            {
                if (PlayerWithin(at, radius))
                {
                    HitPlayer(1f, dodgeable: false);
                    break;
                }
            }
            foreach (var at in spots)
                LastingZone(at, radius, 0f, 4f, 0f, 0.4f, WaterColor);
            cooldown = 1.1f;
        }

        IEnumerator Lash(int times)
        {
            for (var n = 0; n < times; n++)
            {
                var from = boss.GroundPosition;
                var aim = (manager.PlayerGround - from).normalized;
                if (aim == Vector2.zero)
                    aim = Vector2.up;
                yield return LineStrike(from, from + aim * 7f, 1.2f, 0.8f, 1.4f);
                yield return Wait(0.25f);
            }
            cooldown = 1f;
        }

        IEnumerator Flood()
        {
            floodSide = -floodSide;
            var side = new Vector2(floodSide, 0f);
            var at = center + side * arenaRadius * 0.45f;
            LastingZone(at, arenaRadius * 0.6f, 2f, 8f, 0.5f, 0.4f, WaterColor);
            yield return Wait(1f);
            cooldown = 1.5f;
        }

        // A band across the whole arena, square to the line from the boss to the player, with a gap to stand in.
        IEnumerator Wave()
        {
            var toPlayer = (manager.PlayerGround - center).normalized;
            if (toPlayer == Vector2.zero)
                toPlayer = Vector2.up;
            var across = new Vector2(-toPlayer.y, toPlayer.x);
            var through = center + toPlayer * Vector2.Dot(manager.PlayerGround - center, toPlayer);
            var gapAt = Random.Range(-arenaRadius * 0.6f, arenaRadius * 0.6f);
            const float gap = 3f;
            var a0 = through - across * arenaRadius;
            var a1 = through + across * (gapAt - gap * 0.5f);
            var b0 = through + across * (gapAt + gap * 0.5f);
            var b1 = through + across * arenaRadius;
            Track(GroundMarker.Line(a0, a1, 3f, 1.2f, TelegraphColor, transform));
            Track(GroundMarker.Line(b0, b1, 3f, 1.2f, TelegraphColor, transform));
            yield return Wait(1.2f);
            Sfx.Play(SoundId.EnemySlam, 0.9f);
            if (PlayerOnLine(a0, a1, 3f) || PlayerOnLine(b0, b1, 3f))
                HitPlayer(1.6f, dodgeable: false);
            cooldown = 1.2f;
        }
    }
}
