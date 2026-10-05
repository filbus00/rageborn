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

    /// <summary>
    /// Saint Marrow, the boss at depth 18 (2026-10-05, from Docs/05's act 3 boss). Phase 1: Bone Spikes (5 circles of
    /// radius 1.2 in a line toward the player, each filling 0.7 s and landing 0.15 s after the one before, 1.2) and Grave
    /// Bolts (3 aimed shots, 0.6). Phase 2: skeletons rise from the rim every 12 s, 4 at a time, up to 8; she teleports
    /// every 8 s (a 0.6 s marker where she will stand, never within 4 of the player). Phase 3: the Ring of Bones, a band
    /// from the rim that closes to radius 5 over 20 s and hurts 1 hit a second outside it, then starts again; the spikes
    /// go on. Simplified from Docs/05: the ring has no turning gaps. Tuning.
    /// </summary>
    public class SaintMarrowFight : BossFight
    {
        public const string Name = "Saint Marrow";
        static readonly Color BoneColor = new Color(0.85f, 0.82f, 0.7f, 1f);
        static readonly Color RingColor = new Color(0.75f, 0.9f, 0.55f, 0.75f);

        float addsTimer;
        float teleportTimer;
        GroundMarker ring;
        float ringRadius;
        float ringPulse;

        protected override string BossName => Name;
        protected override float KeepDistance => 5f;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 2)
            {
                addsTimer = 2f;
                teleportTimer = 4f;
            }
            if (newPhase == 3)
            {
                ringRadius = arenaRadius;
                ring = GroundMarker.Ring(center, ringRadius, 1f, RingColor, transform);
            }
        }

        protected override void TickFight(Vector2 player)
        {
            if (phase >= 2)
            {
                addsTimer -= frameDelta;
                if (addsTimer <= 0f)
                {
                    addsTimer = 12f;
                    SpawnAdds(addDefinition, 4, 8);
                }
            }
            if (ring == null)
                return;
            // The ring closes to 5 over 20 s, then opens again; outside it, 1 hit a second.
            ringRadius -= (arenaRadius - 5f) / 20f * frameDelta;
            if (ringRadius <= 5f)
                ringRadius = arenaRadius;
            ring.RestartCircle(center, ringRadius, 0.01f);
            ring.Advance(1f);
            if (Vector2.Distance(player, center) > ringRadius - 0.5f)
            {
                ringPulse -= frameDelta;
                if (ringPulse <= 0f)
                {
                    ringPulse = 0.5f;
                    HitPlayer(0.5f, dodgeable: false);
                }
            }
            else
                ringPulse = 0f;
        }

        protected override void ClearFight()
        {
            ring?.Destroy();
            ring = null;
        }

        protected override IEnumerator NextAttack()
        {
            if (phase >= 2)
            {
                teleportTimer -= 2f;
                if (teleportTimer <= 0f)
                {
                    teleportTimer = 8f;
                    return Blink();
                }
            }
            return attackIndex % 2 == 1 ? Spikes() : Bolts();
        }

        IEnumerator Spikes()
        {
            var from = boss.GroundPosition;
            var aim = (manager.PlayerGround - from).normalized;
            if (aim == Vector2.zero)
                aim = Vector2.up;
            var spots = new List<(Vector2 at, GroundMarker marker)>();
            for (var i = 0; i < 5; i++)
            {
                var at = ClampToArena(from + aim * (1.8f + i * 2f), 1.2f);
                spots.Add((at, Track(GroundMarker.Circle(at, 1.2f, 0.7f, TelegraphColor, transform))));
                yield return Wait(0.15f);
            }
            var landed = new bool[spots.Count];
            var hit = false;
            while (System.Array.IndexOf(landed, false) >= 0)
            {
                for (var i = 0; i < spots.Count; i++)
                {
                    if (landed[i] || !spots[i].marker.Done)
                        continue;
                    landed[i] = true;
                    if (!hit && PlayerWithin(spots[i].at, 1.2f))
                    {
                        hit = true;
                        HitPlayer(1.2f, dodgeable: false);
                    }
                }
                yield return null;
            }
            cooldown = 1f;
        }

        IEnumerator Bolts()
        {
            yield return Fan(3, 14f, 8f, 0.4f, 0.6f, BoneColor);
            cooldown = 0.9f;
        }

        IEnumerator Blink()
        {
            Vector2 to = center;
            for (var tries = 0; tries < 12; tries++)
            {
                var candidate = center + Random.insideUnitCircle * (arenaRadius - 2f);
                if (Vector2.Distance(candidate, manager.PlayerGround) >= 4f && manager.Nav.IsWalkable(IsoMath.GroundToCell(candidate)))
                {
                    to = candidate;
                    break;
                }
            }
            var marker = Track(GroundMarker.Circle(to, 1f, 0.6f, RingColor, transform));
            while (!marker.Done)
                yield return null;
            Teleport(to);
            cooldown = 0.6f;
        }
    }

    /// <summary>
    /// The First Watchman, the last boss, at depth 24 (2026-10-05, from Docs/05's act 5 boss). Phase 1, Echoes: the
    /// earlier bosses' patterns in turn, the Warden's slam (radius 3.2, 1.1 s), the Tidewife's pools, Marrow's spike line
    /// and a charge. Phase 2: Void Fields (3 circles of radius 3 that fill in 1.2 s and stay 12 s, 0.8 hit a second), and
    /// adds (the deep levels' own enemies) every 15 s, 2 at a time, up to 8. Phase 3: the edges close, a band at the rim
    /// shrinks the arena to radius 6 over 30 s and stays (1.2 hit a second outside it), and void rain, a circle of radius
    /// 1.5 on the player's spot every second (0.9 s, 0.9). Tuning.
    /// </summary>
    public class FirstWatchmanFight : BossFight
    {
        public const string Name = "The First Watchman";
        static readonly Color VoidColor = new Color(0.55f, 0.25f, 0.75f, 0.6f);

        float addsTimer;
        GroundMarker edge;
        float edgeRadius;
        float edgePulse;
        float rainTimer;

        protected override string BossName => Name;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 2)
                addsTimer = 2f;
            if (newPhase == 3)
            {
                edgeRadius = arenaRadius;
                edge = GroundMarker.Ring(center, edgeRadius, 1f, VoidColor, transform);
            }
        }

        protected override void TickFight(Vector2 player)
        {
            if (phase >= 2)
            {
                addsTimer -= frameDelta;
                if (addsTimer <= 0f)
                {
                    addsTimer = 15f;
                    SpawnAdds(addDefinition, 2, 8);
                }
            }
            if (edge == null)
                return;
            edgeRadius = Mathf.Max(6f, edgeRadius - (arenaRadius - 6f) / 30f * frameDelta);
            edge.RestartCircle(center, edgeRadius, 0.01f);
            edge.Advance(1f);
            if (Vector2.Distance(player, center) > edgeRadius - 0.5f)
            {
                edgePulse -= frameDelta;
                if (edgePulse <= 0f)
                {
                    edgePulse = 0.5f;
                    HitPlayer(0.6f, dodgeable: false);
                }
            }
            else
                edgePulse = 0f;
            // Void rain: one circle on her spot every second.
            rainTimer -= frameDelta;
            if (rainTimer <= 0f)
            {
                rainTimer = 1f;
                LastingZone(manager.PlayerGround, 1.5f, 0.9f, 0.05f, 0f, 0f, VoidColor);
                StartCoroutine(RainDrop(manager.PlayerGround));
            }
        }

        // A void raindrop lands where she stood when it was called (its own clock, beside the boss's attacks).
        IEnumerator RainDrop(Vector2 at)
        {
            var wait = 0.9f;
            while (wait > 0f)
            {
                wait -= Time.deltaTime;
                yield return null;
            }
            if (boss != null && boss.IsAlive && PlayerWithin(at, 1.5f))
                HitPlayer(0.9f, dodgeable: false);
        }

        protected override void ClearFight()
        {
            edge?.Destroy();
            edge = null;
        }

        protected override IEnumerator NextAttack()
        {
            if (phase == 2 && attackIndex % 3 == 0)
                return VoidFields();
            // Phase 1 and between the fields: the echoes in turn.
            switch (attackIndex % 4)
            {
                case 0: return EchoSlam();
                case 1: return EchoPools();
                case 2: return EchoSpikes();
                default: return EchoCharge();
            }
        }

        IEnumerator EchoSlam()
        {
            var approach = 1.5f;
            while (approach > 0f && Vector2.Distance(boss.GroundPosition, manager.PlayerGround) > KeepDistance)
            {
                approach -= frameDelta;
                WalkTowards(manager.PlayerGround, KeepDistance);
                yield return null;
            }
            yield return CircleStrike(boss.GroundPosition, 3.2f, 1.1f, 1.6f);
            cooldown = 0.9f;
        }

        IEnumerator EchoPools()
        {
            var spots = new List<Vector2>();
            for (var i = 0; i < 3; i++)
            {
                var at = ClampToArena(manager.PlayerGround + Random.insideUnitCircle * 3f, 2f);
                spots.Add(at);
                Track(GroundMarker.Circle(at, 2f, 1f, TelegraphColor, transform));
            }
            yield return Wait(1f);
            foreach (var at in spots)
                if (PlayerWithin(at, 2f))
                {
                    HitPlayer(1f, dodgeable: false);
                    break;
                }
            cooldown = 1f;
        }

        IEnumerator EchoSpikes()
        {
            var from = boss.GroundPosition;
            var aim = (manager.PlayerGround - from).normalized;
            if (aim == Vector2.zero)
                aim = Vector2.up;
            for (var i = 0; i < 5; i++)
            {
                var at = ClampToArena(from + aim * (1.8f + i * 2f), 1.2f);
                Track(GroundMarker.Circle(at, 1.2f, 0.7f, TelegraphColor, transform));
                StartCoroutine(SpikeLands(at));
                yield return Wait(0.15f);
            }
            yield return Wait(0.7f);
            cooldown = 0.9f;
        }

        IEnumerator SpikeLands(Vector2 at)
        {
            var wait = 0.7f;
            while (wait > 0f)
            {
                wait -= Time.deltaTime;
                yield return null;
            }
            if (boss != null && boss.IsAlive && PlayerWithin(at, 1.2f))
                HitPlayer(1.2f, dodgeable: false);
        }

        IEnumerator EchoCharge()
        {
            var toPlayer = manager.PlayerGround - boss.GroundPosition;
            var target = manager.PlayerGround + (toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized * 2f : Vector2.zero);
            yield return Charge(target, 0.5f, 15f, 1.4f);
            cooldown = 1.1f;
        }

        IEnumerator VoidFields()
        {
            for (var i = 0; i < 3; i++)
            {
                var at = ClampToArena(manager.PlayerGround + Random.insideUnitCircle * 4f, 3f);
                LastingZone(at, 3f, 1.2f, 12f, 0.8f, 0f, VoidColor);
            }
            yield return Wait(1.2f);
            cooldown = 1.2f;
        }
    }
}
