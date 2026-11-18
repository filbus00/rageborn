using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Saint Marrow, the boss at depth 30, the first of the Burnt Library (2026-10-05, reworked 2026-11-16 as one of the six
    /// unfinished fights; the story: the monks' saint, long dead and still leading their prayer). He keeps his distance
    /// and shows nothing but circles on the ground and symbols on her ring. Phase 1, the Litany: Bone Spikes (a line of
    /// five circles toward her), Grave Bolts (a fan of three) and the Prayer (a slam round him, then a ring of bone circles
    /// beyond it). Phase 2 (2/3 life): penitents rise from the rim every 12 s, he blinks across the arena every 8 s (leaving
    /// a burst where he stood), the bolts go five wide. Phase 3 (1/3): the Ring of Bones closes in from the rim, and the
    /// Rapture, a circle that follows her and holds before it falls, joins the rest. Claude's design, to review. Tuning.
    /// </summary>
    public class SaintMarrowFight : BossFight
    {
        public const string Name = "Saint Marrow";
        static readonly Color BoneColor = new Color(0.85f, 0.82f, 0.7f, 1f);
        static readonly Color RingColor = new Color(0.75f, 0.9f, 0.55f, 0.75f);
        protected override Color TelegraphColor => WarningOf(BoneColor);

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
            switch (phase)
            {
                case 1:
                    switch (Pick(3))
                    {
                        case 0: return Spikes();
                        case 1: return Bolts(3);
                        default: return Prayer();
                    }
                case 2:
                    switch (Pick(3))
                    {
                        case 0: return Spikes();
                        case 1: return Bolts(5);
                        default: return Prayer();
                    }
                default:
                    switch (Pick(4))
                    {
                        case 0: return Spikes();
                        case 1: return Bolts(5);
                        case 2: return Prayer();
                        default: return Rapture();
                    }
            }
        }

        // Five spikes of bone in a line toward her, each landing a beat after the last.
        IEnumerator Spikes()
        {
            var from = boss.GroundPosition;
            var aim = (manager.PlayerGround - from).normalized;
            if (aim == Vector2.zero)
                aim = Vector2.up;
            yield return Barrage(5, i => from + aim * (1.8f + i * 2f), 1.2f, 0.15f, 0.7f, 1.2f, TelegraphColor);
            cooldown = 1f;
        }

        IEnumerator Bolts(int count)
        {
            yield return Fan(count, 14f, 8f, 0.4f, 0.6f, BoneColor);
            cooldown = 0.9f;
        }

        // The slam round him, then a ring of bone circles beyond it: she steps in, then out.
        IEnumerator Prayer()
        {
            var at = boss.GroundPosition;
            yield return CircleStrike(at, 3.2f, 1f, 1.3f);
            var turn = Random.value;
            var spots = new List<Vector2>();
            for (var i = 0; i < 8; i++)
                spots.Add(ClampToArena(Round(at, 5.4f, turn + i / 8f), 1.4f));
            foreach (var spot in spots)
                Track(GroundMarker.Circle(spot, 1.4f, 0.8f, TelegraphColor, transform));
            yield return Wait(0.8f);
            Sfx.Play(SoundId.EnemySlam, 0.6f);
            foreach (var spot in spots)
                if (PlayerWithin(spot, 1.4f))
                {
                    HitPlayer(1f, dodgeable: false);
                    break;
                }
            cooldown = 1.1f;
        }

        // A circle that follows her, holds, and falls.
        IEnumerator Rapture()
        {
            yield return Tracking(2.4f, 1.8f, 0.7f, 1.5f, BoneColor);
            cooldown = 1f;
        }

        // Across the arena: a marker where he will stand, and a burst of bone where he stood.
        IEnumerator Blink()
        {
            var from = boss.GroundPosition;
            var to = center;
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
            Track(GroundMarker.Circle(from, 2.4f, 0.8f, TelegraphColor, transform));
            yield return Wait(0.8f);
            Sfx.Play(SoundId.EnemySlam, 0.5f);
            if (PlayerWithin(from, 2.4f))
                HitPlayer(1f, dodgeable: false);
            cooldown = 0.6f;
        }
    }

    /// <summary>
    /// The Ash Archivist, the boss at depth 36 at the end of the Burnt Library (2026-10-11, reworked 2026-11-16; the owner's
    /// choice: keeper of the Watch's records, who burned the library to hide how the fires died, and burned with it). He
    /// stays back and reads. Phase 1, the Index: Burning Pages (circles round her that leave burning ground), Ink Blots (a
    /// fan of five slow, clinging shots) and Bookmarks (four circles that come down on where she was, one after another,
    /// so she must keep moving). Phase 2 (2/3 life): the Shelves Alight, a lane of fire across the arena with a gap, his
    /// scribes come every 14 s. Phase 3 (1/3): the Last Chapter, a circle that follows her and falls, six pages at a time
    /// and the shelves again and again. Claude's design, to review. Tuning.
    /// </summary>
    public class AshArchivistFight : BossFight
    {
        public const string Name = "The Ash Archivist";
        static readonly Color FireColor = new Color(1f, 0.45f, 0.12f, 0.55f);
        static readonly Color InkColor = new Color(0.4f, 0.32f, 0.6f, 0.9f);
        protected override Color TelegraphColor => WarningOf(FireColor);

        float addsTimer;

        protected override string BossName => Name;
        protected override float KeepDistance => 4.5f;

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
                    switch (Pick(3))
                    {
                        case 0: return Pages(4);
                        case 1: return InkBlots(5);
                        default: return Bookmarks(4);
                    }
                case 2:
                    switch (Pick(4))
                    {
                        case 0: return Shelves();
                        case 1: return Pages(5);
                        case 2: return InkBlots(5);
                        default: return Bookmarks(4);
                    }
                default:
                    switch (Pick(5))
                    {
                        case 0: return Shelves();
                        case 1: return Pages(6);
                        case 2: return Bookmarks(5);
                        case 3: return LastChapter();
                        default: return InkBlots(7);
                    }
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

        // Ink flung in a fan: whoever it touches is slowed by it.
        IEnumerator InkBlots(int count)
        {
            yield return Wait(0.3f);
            yield return Fan(count, 18f, 7f, 0.45f, 0.9f, InkColor, 0.3f);
            cooldown = 1f;
        }

        // A circle on where she stands now, another a beat later where she stands then: keep moving.
        IEnumerator Bookmarks(int count)
        {
            yield return Barrage(count, i => manager.PlayerGround, 1.4f, 0.45f, 0.9f, 1f, TelegraphColor);
            cooldown = 1f;
        }

        // The Last Chapter: a circle of fire that follows her and then falls.
        IEnumerator LastChapter()
        {
            yield return Tracking(2.4f, 2f, 0.8f, 1.6f, FireColor);
            cooldown = 1.1f;
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
    /// The Rift Mother, the boss at depth 42, halfway down the Rift Caves (2026-10-11, reworked 2026-11-16; the owner's
    /// choice: the thing breeding the demons in the deep, a swollen rift-beast that births adds and spews void bile). Huge
    /// and slow. Phase 1, the Birth: Void Bile (a fan of five slow globs), the Brood (a circle at her feet, then riftspawn
    /// from it) and the Stomp (a leap onto her). Phase 2 (2/3 life): the Rift Pulse (rings of void circles growing outward
    /// from her), the Rifts (three tears in the ground that burn for eight seconds and each give up a riftspawn) and the
    /// Heave (a charge); the brood comes every 12 s. Phase 3 (1/3): the Maw, a circle that follows her, holds, and falls,
    /// three rings, bile seven wide and a bigger stomp. Claude's design, to review. Tuning.
    /// </summary>
    public class RiftMotherFight : BossFight
    {
        public const string Name = "The Rift Mother";
        static readonly Color VoidColor = new Color(0.7f, 0.35f, 0.95f, 0.9f);
        protected override Color TelegraphColor => WarningOf(VoidColor);

        float broodTimer;

        protected override string BossName => Name;
        protected override float MoveSpeed => phase >= 3 ? 2.6f : 2.1f;

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
                    switch (Pick(3))
                    {
                        case 0: return Bile(5);
                        case 1: return Brood();
                        default: return Stomp(2.6f);
                    }
                case 2:
                    switch (Pick(4))
                    {
                        case 0: return Pulse(2);
                        case 1: return Bile(5);
                        case 2: return Rifts();
                        default: return Heave();
                    }
                default:
                    switch (Pick(5))
                    {
                        case 0: return Maw();
                        case 1: return Pulse(3);
                        case 2: return Bile(7);
                        case 3: return Heave();
                        default: return Stomp(3.2f);
                    }
            }
        }

        IEnumerator Bile(int globs)
        {
            yield return Wait(0.5f);
            yield return Fan(globs, 70f, 5f, 0.45f, 0.9f, VoidColor, 0.3f);
            cooldown = 1.2f;
        }

        // She labours: a circle at her feet, and what it gives up comes out of it.
        IEnumerator Brood()
        {
            var at = boss.GroundPosition;
            Track(GroundMarker.Circle(at, 2f, 1.2f, TelegraphColor, transform));
            yield return Wait(1.2f);
            Sfx.Play(SoundId.EnemySlam, 0.5f);
            if (PlayerWithin(at, 2f))
                HitPlayer(0.9f, dodgeable: false);
            for (var i = 0; i < 2; i++)
                SpawnAddAt(addDefinition, Round(at, 2.4f, Random.value), 6);
            cooldown = 1.3f;
        }

        IEnumerator Stomp(float radius)
        {
            yield return Approach(1.5f, 6f);
            yield return Leap(manager.PlayerGround, radius, 1.6f, 1.2f, 1.1f);
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
                var turn = Random.value;
                for (var i = 0; i < count; i++)
                {
                    var at = ClampToArena(Round(boss.GroundPosition, 2.2f * r + 0.5f, turn + i / (float)count), radius);
                    spots.Add(at);
                    Track(GroundMarker.Circle(at, radius, 1f, TelegraphColor, transform));
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

        // Three tears in the ground near her: they burn for a while, and each lets a riftspawn through.
        IEnumerator Rifts()
        {
            for (var i = 0; i < 3; i++)
            {
                var at = ClampToArena(manager.PlayerGround + Random.insideUnitCircle * 4.5f, 2.4f);
                LastingZone(at, 2.4f, 1.2f, 8f, 0.7f, 0.2f, VoidColor);
                StartCoroutine(Hatch(at, 1.2f));
            }
            yield return Wait(1.2f);
            cooldown = 1.3f;
        }

        IEnumerator Hatch(Vector2 at, float after)
        {
            while (after > 0f)
            {
                after -= Time.deltaTime;
                yield return null;
            }
            if (boss != null && boss.IsAlive)
                SpawnAddAt(addDefinition, at, 8);
        }

        IEnumerator Heave()
        {
            yield return Charge(manager.PlayerGround, 0.9f, 9f, 1.5f);
            cooldown = 1.4f;
        }

        // The maw opens under her: a circle follows, holds, and swallows.
        IEnumerator Maw()
        {
            yield return Tracking(2.8f, 2f, 0.8f, 1.8f, VoidColor);
            cooldown = 1.2f;
        }
    }

    /// <summary>
    /// The First Watchman, the last boss, at depth 48 at the bottom of the Rift Caves (2026-10-05, reworked 2026-11-16; the
    /// story: he guarded the Rift before there were dukes, and turned). A knight of the old Watch with a greatsword and
    /// no oath left. Phase 1, the Duel: the Combo (two quick cuts before him), the Greatsword Leap, the Oath Break (a
    /// slam round him and a ring beyond it) and a charge. Phase 2 (2/3 life): Void Fields (circles that burn for twelve
    /// seconds), the Echoes (pools of void), and his oathless watchmen come every 15 s. Phase 3 (1/3): the edges close in
    /// from the rim, void rain falls on her spot, the Last Watch (a spin that chases her) and bigger leaps. Claude's
    /// design, to review. Tuning.
    /// </summary>
    public class FirstWatchmanFight : BossFight
    {
        public const string Name = "The First Watchman";
        static readonly Color VoidColor = new Color(0.55f, 0.25f, 0.75f, 0.6f);
        static readonly Color SteelColor = new Color(0.8f, 0.85f, 0.95f, 0.9f);
        protected override Color TelegraphColor => WarningOf(VoidColor);

        float addsTimer;
        GroundMarker edge;
        float edgeRadius;
        float edgePulse;
        float rainTimer;

        protected override string BossName => Name;
        protected override float KeepDistance => 1.8f;
        protected override float MoveSpeed => phase >= 3 ? 3.2f : 2.7f;

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
            // Void rain: one circle on her spot every 1.4 seconds.
            rainTimer -= frameDelta;
            if (rainTimer <= 0f)
            {
                rainTimer = 1.4f;
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
            switch (phase)
            {
                case 1:
                    switch (Pick(4))
                    {
                        case 0: return Combo();
                        case 1: return GreatswordLeap(2.8f);
                        case 2: return OathBreak();
                        default: return Rush();
                    }
                case 2:
                    switch (Pick(5))
                    {
                        case 0: return Combo();
                        case 1: return GreatswordLeap(2.8f);
                        case 2: return OathBreak();
                        case 3: return VoidFields();
                        default: return Echoes();
                    }
                default:
                    switch (Pick(4))
                    {
                        case 0: return LastWatch();
                        case 1: return GreatswordLeap(3.4f);
                        case 2: return Combo();
                        default: return Rush();
                    }
            }
        }

        // Closes in, then three cuts, each a circle before him that fills fast.
        IEnumerator Combo()
        {
            yield return Approach(2f, KeepDistance);
            for (var cut = 0; cut < 3; cut++)
            {
                var toward = manager.PlayerGround - boss.GroundPosition;
                var at = boss.GroundPosition + (toward.sqrMagnitude > 1e-4f ? toward.normalized : Vector2.down) * 1.3f;
                yield return CircleStrike(at, 1.6f, cut == 0 ? 0.7f : 0.45f, 1.3f, SteelColor);
            }
            cooldown = 0.9f;
        }

        IEnumerator GreatswordLeap(float radius)
        {
            yield return Approach(1.2f, 6f);
            yield return Leap(manager.PlayerGround, radius, 1.7f, 1.6f, 1f);
            cooldown = 1.1f;
        }

        // The oath broken: a slam round him, and a ring of void circles beyond it.
        IEnumerator OathBreak()
        {
            var at = boss.GroundPosition;
            yield return CircleStrike(at, 3.2f, 1.1f, 1.5f);
            var turn = Random.value;
            var spots = new List<Vector2>();
            for (var i = 0; i < 6; i++)
                spots.Add(ClampToArena(Round(at, 5.2f, turn + i / 6f), 1.6f));
            foreach (var spot in spots)
                Track(GroundMarker.Circle(spot, 1.6f, 0.8f, TelegraphColor, transform));
            yield return Wait(0.8f);
            Sfx.Play(SoundId.EnemySlam, 0.6f);
            foreach (var spot in spots)
                if (PlayerWithin(spot, 1.6f))
                {
                    HitPlayer(1.1f, dodgeable: false);
                    break;
                }
            cooldown = 1f;
        }

        IEnumerator Rush()
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

        IEnumerator Echoes()
        {
            var spots = new List<Vector2>();
            for (var i = 0; i < 3; i++)
            {
                var at = ClampToArena(manager.PlayerGround + Random.insideUnitCircle * 3f, 2f);
                spots.Add(at);
                Track(GroundMarker.Circle(at, 2f, 1f, TelegraphColor, transform));
            }
            yield return Wait(1f);
            Sfx.Play(SoundId.EnemySlam, 0.6f);
            foreach (var at in spots)
                if (PlayerWithin(at, 2f))
                {
                    HitPlayer(1f, dodgeable: false);
                    break;
                }
            cooldown = 1f;
        }

        // The last watch: he spins, greatsword out, after her.
        IEnumerator LastWatch()
        {
            yield return Whirl(3.4f, 3f, 0.7f, true, SteelColor);
            cooldown = 1.3f;
        }
    }
}
