using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Ash Archivist, the boss at depth 24 at the end of the burnt library (2026-10-11, the owner's choice: keeper of
    /// the Watch's records, who burned the library to hide how the fires died, and burned with it). Phase 1: Burning
    /// Pages (4 circles of radius 1.5 round the player, a 0.9 s fill, 0.9 hit, leaving burning ground 3 s that hurts 0.4
    /// a second) and the Ink Lash (a band 6 long and 1 wide toward her, 0.7 s, 1.2). Phase 2: the Shelves Alight, a
    /// burning lane across the arena with a gap to stand in (1.2 s to light, then burning 5 s, 0.6 a second); his
    /// scribes (ember acolytes) come from the rim every 14 s, 2 at a time, up to 6. Phase 3: six pages at a time and the
    /// lash three times; the shelves every third attack. Claude's design, to review. Tuning.
    /// </summary>
    public class AshArchivistFight : BossFight
    {
        public const string Name = "The Ash Archivist";
        static readonly Color FireColor = new Color(1f, 0.45f, 0.12f, 0.55f);

        float addsTimer;

        protected override string BossName => Name;

        protected override void OnPhase(int newPhase)
        {
            if (newPhase == 2)
                addsTimer = 2f;
        }

        protected override void TickFight(Vector2 player)
        {
            if (phase < 2)
                return;
            addsTimer -= frameDelta;
            if (addsTimer > 0f)
                return;
            addsTimer = 14f;
            SpawnAdds(addDefinition, 2, 6);
        }

        protected override IEnumerator NextAttack()
        {
            switch (phase)
            {
                case 1:
                    return attackIndex % 2 == 0 ? Pages(4) : Lash(1);
                case 2:
                    return attackIndex % 3 == 0 ? Shelves() : attackIndex % 3 == 1 ? Pages(5) : Lash(2);
                default:
                    return attackIndex % 3 == 0 ? Shelves() : attackIndex % 3 == 1 ? Pages(6) : Lash(3);
            }
        }

        IEnumerator Pages(int count)
        {
            const float radius = 1.5f;
            var spots = new List<Vector2>();
            for (var i = 0; i < count; i++)
            {
                var at = ClampToArena(manager.PlayerGround + Random.insideUnitCircle * 3.5f, radius);
                spots.Add(at);
                Track(GroundMarker.Circle(at, radius, 0.9f, TelegraphColor, transform));
            }
            yield return Wait(0.9f);
            Sfx.Play(SoundId.EnemySlam, 0.5f);
            foreach (var at in spots)
                if (PlayerWithin(at, radius))
                {
                    HitPlayer(0.9f, dodgeable: false);
                    break;
                }
            foreach (var at in spots)
                LastingZone(at, radius, 0f, 3f, 0.4f, 0f, FireColor);
            cooldown = 1f;
        }

        IEnumerator Lash(int times)
        {
            for (var n = 0; n < times; n++)
            {
                var from = boss.GroundPosition;
                var aim = (manager.PlayerGround - from).normalized;
                if (aim == Vector2.zero)
                    aim = Vector2.up;
                yield return LineStrike(from, from + aim * 6f, 1f, 0.7f, 1.2f);
                yield return Wait(0.2f);
            }
            cooldown = 0.9f;
        }

        // A lane of burning shelves across the arena, square to the line from the middle to her, with a gap to stand in.
        IEnumerator Shelves()
        {
            var toPlayer = (manager.PlayerGround - center).normalized;
            if (toPlayer == Vector2.zero)
                toPlayer = Vector2.up;
            var across = new Vector2(-toPlayer.y, toPlayer.x);
            var through = center + toPlayer * Vector2.Dot(manager.PlayerGround - center, toPlayer);
            var gapAt = Random.Range(-arenaRadius * 0.6f, arenaRadius * 0.6f);
            const float gap = 3f;
            const float width = 2.4f;
            var pieces = new[]
            {
                (through - across * arenaRadius, through + across * (gapAt - gap * 0.5f)),
                (through + across * (gapAt + gap * 0.5f), through + across * arenaRadius),
            };
            foreach (var (a, b) in pieces)
                Track(GroundMarker.Line(a, b, width, 1.2f, TelegraphColor, transform));
            yield return Wait(1.2f);
            Sfx.Play(SoundId.EnemySlam, 0.8f);
            foreach (var (a, b) in pieces)
            {
                if (PlayerOnLine(a, b, width))
                    HitPlayer(1.2f, dodgeable: false);
                // The lane burns on: a row of burning patches along it.
                var length = Vector2.Distance(a, b);
                for (var t = 0.5f; t < length; t += 1.6f)
                    LastingZone(Vector2.Lerp(a, b, t / length), width * 0.5f, 0f, 5f, 0.6f, 0f, FireColor);
            }
            cooldown = 1.3f;
        }
    }

    /// <summary>
    /// The Rift Mother, the boss at depth 30, halfway down the caves (2026-10-11, the owner's choice: the thing breeding
    /// the demons in the deep, a swollen rift-beast that births adds and spews void bile). Phase 1: Void Bile (a fan of 5
    /// slow globs, 0.9 each, that slow 30 percent) and the Brood (2 void wraiths from her, up to 4). Phase 2: the Rift
    /// Pulse (rings of void circles round her that grow outward, 1.0 s each, 1.1) and the brood every 12 s, 3 at a time,
    /// up to 8. Phase 3: she heaves toward her (a charge, 1.5) and the bile goes 7 wide. Claude's design, to review.
    /// Tuning.
    /// </summary>
    public class RiftMotherFight : BossFight
    {
        public const string Name = "The Rift Mother";
        static readonly Color VoidColor = new Color(0.7f, 0.35f, 0.95f, 0.9f);

        float broodTimer;

        protected override string BossName => Name;

        protected override void OnPhase(int newPhase)
        {
            broodTimer = 2f;
        }

        protected override void TickFight(Vector2 player)
        {
            broodTimer -= frameDelta;
            if (broodTimer > 0f)
                return;
            broodTimer = phase >= 2 ? 12f : 18f;
            SpawnAdds(addDefinition, phase >= 2 ? 3 : 2, phase >= 2 ? 8 : 4);
        }

        protected override IEnumerator NextAttack()
        {
            switch (phase)
            {
                case 1:
                    return Bile(5);
                case 2:
                    return attackIndex % 2 == 0 ? Pulse(2) : Bile(5);
                default:
                    return attackIndex % 3 == 0 ? Heave() : attackIndex % 3 == 1 ? Pulse(3) : Bile(7);
            }
        }

        IEnumerator Bile(int globs)
        {
            yield return Wait(0.5f);
            yield return Fan(globs, 70f, 5f, 0.45f, 0.9f, VoidColor, 0.3f);
            cooldown = 1.2f;
        }

        // Rings of void circles round her, each ring further out, a beat apart.
        IEnumerator Pulse(int rings)
        {
            const float radius = 1.4f;
            for (var r = 1; r <= rings; r++)
            {
                var spots = new List<Vector2>();
                var count = 4 + 3 * r;
                var turn = Random.value * Mathf.PI * 2f;
                for (var i = 0; i < count; i++)
                {
                    var a = turn + Mathf.PI * 2f * i / count;
                    var at = boss.GroundPosition + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (2.2f * r + 0.5f);
                    spots.Add(ClampToArena(at, radius));
                    Track(GroundMarker.Circle(spots[i], radius, 1f, TelegraphColor, transform));
                }
                yield return Wait(1f);
                Sfx.Play(SoundId.EnemySlam, 0.6f);
                foreach (var at in spots)
                    if (PlayerWithin(at, radius))
                    {
                        HitPlayer(1.1f, dodgeable: false);
                        break;
                    }
                yield return Wait(0.3f);
            }
            cooldown = 1.2f;
        }

        IEnumerator Heave()
        {
            yield return Charge(manager.PlayerGround, 0.9f, 9f, 1.5f);
            cooldown = 1.4f;
        }
    }
}
