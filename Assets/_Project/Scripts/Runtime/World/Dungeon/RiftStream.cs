using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The open rift at the top of a stretch of the Vigil's road (Docs/05, 2026-10-06): demon packs come out of it in
    /// surges and down the hall at her, without end, stronger and denser the closer she is to it (<see cref="RoadRules"/>;
    /// no timers). To keep pace in a long hall a pack joins the stream a screen ahead of her, up the hall, never behind
    /// her and never in sight. When she comes near, its guardian comes through; killing it closes the rift for good
    /// (<see cref="Closed"/>). On a boss's stretch the boss is the guardian and the stream stops once the fight starts.
    /// Set up by <see cref="DungeonLevel"/>.
    /// </summary>
    public class RiftStream : MonoBehaviour
    {
        /// <summary>Rolls a pack's members: (kind, count, pack index) to one definition per slot.</summary>
        public delegate EnemyDefinition[] PackRoller(PackKind kind, int count, int index);

        int stretch;
        RoadPath path;
        Vector2 rift;
        PackRoller roll;
        EnemyDefinition guardianDefinition;
        BossFight bossFight;
        Action closed;

        EnemyManager manager;
        PlayerController player;
        readonly List<EnemyController> stream = new List<EnemyController>(32);
        readonly System.Random random = new System.Random();
        float surgeTimer = 1.5f;
        int packIndex;
        EnemyController guardian;
        bool closing;

        /// <summary>Whether the guardian (or the boss) has died: the stream has stopped for good.</summary>
        public bool IsClosed => closing;

        /// <summary>How far up the stretch she is, 0 at the bottom beacon and 1 at the rift.</summary>
        public float Closeness { get; private set; }

        public void Configure(int stretchNumber, RoadPath hall, Vector2 riftGround, PackRoller packRoller,
            EnemyDefinition guardianDef, BossFight fight, Action onClosed)
        {
            stretch = stretchNumber;
            path = hall;
            rift = riftGround;
            roll = packRoller;
            guardianDefinition = guardianDef;
            bossFight = fight;
            closed = onClosed;
        }

        void Start()
        {
            manager = FindAnyObjectByType<EnemyManager>();
            player = FindAnyObjectByType<PlayerController>();
            if (manager == null || !manager.IsReady || player == null || path == null)
            {
                Debug.LogError("[ARPG] The rift needs a ready EnemyManager, the player and its hall.", this);
                enabled = false;
                return;
            }
            // The guardian slain closes the rift (heard from the manager: instances are pooled, so its own life could
            // belong to another demon by the next look).
            manager.Killed += OnKilled;
        }

        void OnDestroy()
        {
            if (manager != null)
                manager.Killed -= OnKilled;
        }

        void OnKilled(EnemyController enemy)
        {
            if (enemy == guardian && guardian != null)
                Close();
        }

        void Update()
        {
            if (closing)
                return;
            var here = IsoMath.WorldToGround(player.transform.position);
            var progress = path.Progress(here);
            Closeness = RoadRules.Closeness(progress, path.Length);

            for (var i = stream.Count - 1; i >= 0; i--)
                if (!stream[i].IsAlive)
                    stream.RemoveAt(i);

            if (bossFight != null)
            {
                // The boss is the guardian; its own fight calls its adds, so the stream stops once it starts.
                if (bossFight.Engaged)
                    return;
            }
            else if (guardian == null && !RoadRules.IsEndless(stretch) && Vector2.Distance(here, rift) <= RoadRules.GuardianReach)
            {
                guardian = manager.Spawn(guardianDefinition, rift, true, null, RoadRules.GuardianLevel(stretch));
                guardian.Relentless = true;
                HintBanner.Current?.Show("The rift's guardian comes through.");
            }

            surgeTimer -= Time.deltaTime;
            if (surgeTimer > 0f || stream.Count >= RoadRules.MaxAwake(Closeness))
                return;
            surgeTimer = RoadRules.SurgeSeconds(Closeness);
            Surge(here, progress);
        }

        // A pack joins the stream a screen ahead of her up the hall (or at the rift when that is nearer), awake.
        void Surge(Vector2 here, float progress)
        {
            var at = Mathf.Min(progress + RoadRules.SpawnAhead, path.Length);
            var center = path.PointAt(at);
            if (Vector2.Distance(center, here) < RoadRules.MinSpawnDistance && at < path.Length)
                return;
            // Across the hall a little, so packs do not all walk the middle line.
            var direction = path.DirectionAt(at);
            var side = new Vector2(-direction.y, direction.x) * ((float)random.NextDouble() * 4f - 2f);
            if (manager.Nav.IsWalkable(IsoMath.GroundToCell(center + side)))
                center += side;
            if (manager.InLight(center))
                return;

            var kind = RoadRules.RollKind(stretch, Closeness, (float)random.NextDouble());
            var count = kind == PackKind.Elite ? 2 : RoadRules.PackSize(Closeness);
            var members = roll(kind, count, packIndex++);
            var level = RoadRules.PackLevel(stretch, Closeness, progress);
            for (var i = 0; i < members.Length; i++)
            {
                if (members[i] == null)
                    continue;
                var position = center + EnemyPack.SlotOffset(i, members.Length, 1.6f);
                if (!manager.Nav.IsWalkable(IsoMath.GroundToCell(position)))
                    position = center;
                var demon = manager.Spawn(members[i], position, true, null, level);
                demon.Relentless = true;
                stream.Add(demon);
            }
        }

        /// <summary>Called when the guardian (or the boss) dies: the stream stops and the rift closes for good.</summary>
        public void Close()
        {
            if (closing)
                return;
            closing = true;
            closed?.Invoke();
        }
    }
}
