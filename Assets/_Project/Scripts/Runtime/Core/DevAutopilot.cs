#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Development only: plays act 1 by itself, as a playtest stand-in, and writes Logs/AutopilotReport.md. It drives
    /// the real game through the stick (<see cref="FloatingStickInput.TestOverride"/>): from a new character in town down
    /// the stairs, through every level killing every enemy, picking up loot and equipping upgrades, to the Cinder Warden
    /// and back to town. It steps out of ground warnings (slam and rain circles) and otherwise walks up to the nearest
    /// enemy and stands to fight, which is what the auto-combat rewards. A death sends it back down from town to its
    /// corpse. Game time runs on a fixed 1/30 s step (<see cref="Time.captureDeltaTime"/>), so a run is repeatable in
    /// its pace and runs as fast as the editor can draw. Started by Tools > ARPG > Autopilot Act 1.
    ///
    /// It is a mediocre player on purpose: it does not kite, does not use the Forge, and fights every pack it sees.
    /// </summary>
    public class DevAutopilot : MonoBehaviour
    {
        public const string RequestKey = "ARPG.AutopilotRequested";
        const string ReportPath = "Logs/AutopilotReport.md";
        const float StepSeconds = 1f / 30f;
        const float TimeLimitSeconds = 90f * 60f;
        const int DeathLimit = 12;

        const string TownScene = "Town";
        const float StandDistance = 1.2f;
        const float EngageRange = 12f;
        const float LootRange = 10f;
        const float DangerMargin = 0.6f;
        const float StuckSeconds = 15f;
        const float FlowRefreshSeconds = 0.5f;

        // Pinned on a wall corner (the flow field steps diagonally past corners, and the player's physics body catches on
        // them): no progress for this long while pushing, then steer this far to one side for a moment, alternating.
        const float PinnedSeconds = 0.4f;
        const float PinnedDistance = 0.15f;
        const float SidestepSeconds = 0.5f;
        const float SidestepDegrees = 70f;

        sealed class DepthStats
        {
            public float FirstEntry = -1f, LastExit, MinLife = 1f, DamageTaken;
            public int Deaths, Kills, Potions, LevelIn, LevelOut, Visits;
        }

        readonly Dictionary<int, DepthStats> depths = new Dictionary<int, DepthStats>();
        readonly HashSet<object> unreachable = new HashSet<object>();
        readonly int[] rarityFound = new int[4];

        GameSession session;
        FloatingStickInput stick;
        PlayerController player;
        PlayerHealth health;
        EnemyManager enemies;
        FlowField flow;
        Vector2Int flowGoal = new Vector2Int(int.MinValue, 0);
        float flowAge;

        int depth;
        float clock;
        int deaths;
        int lastCharges;
        bool bossKilled;
        float bossEngagedAt = -1f, bossKilledAt = -1f;
        bool finished;

        object target;
        float targetSince;
        float targetBestDistance;

        Vector2 pinCheckFrom;
        float pinCheckTimer;
        float sidestepTimer;
        float sidestepSign = 1f;

        /// <summary>Starts a run in this play session, if the editor menu asked for one.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void MaybeStart()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorPrefs.GetBool(RequestKey, false))
                return;
            UnityEditor.EditorPrefs.SetBool(RequestKey, false);
            var go = new GameObject("Dev Autopilot");
            DontDestroyOnLoad(go);
            go.AddComponent<DevAutopilot>();
#endif
        }

        void Awake()
        {
            Application.runInBackground = true;
            Time.captureDeltaTime = StepSeconds;
            session = GameSession.Current;
            session.PickedUp += OnPickedUp;
            lastCharges = session.Potion.Charges;
            session.Potion.Changed += OnPotionChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Time.captureDeltaTime = 0f;
            if (stick != null)
                stick.TestOverride = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (finished)
                return;

            stick = FindAnyObjectByType<FloatingStickInput>();
            player = FindAnyObjectByType<PlayerController>();
            health = FindAnyObjectByType<PlayerHealth>();
            if (health != null)
            {
                health.Died += OnDied;
                health.HitTaken += OnHitTaken;
            }
            enemies = FindAnyObjectByType<EnemyManager>();
            flow = enemies != null && enemies.Nav != null ? new FlowField(enemies.Nav) : null;
            flowGoal = new Vector2Int(int.MinValue, 0);
            unreachable.Clear();
            target = null;

            if (scene.name == TownScene)
            {
                if (depth > 0)
                    Stats(depth).LastExit = clock;
                depth = 0;
                if (bossKilled)
                    Finish("the Cinder Warden is dead and the character is back in town");
                return;
            }

            var level = LevelContext.CurrentId;
            depth = level != null && level.StartsWith("Dungeon ") ? int.Parse(level.Substring(8)) : 0;
            var stats = Stats(depth);
            stats.Visits++;
            if (stats.FirstEntry < 0f)
            {
                stats.FirstEntry = clock;
                stats.LevelIn = session.Level;
                if (depth > 1)
                    Stats(depth - 1).LastExit = clock;
            }
            if (enemies != null)
                enemies.Killed += OnKilled;
        }

        DepthStats Stats(int d)
        {
            if (!depths.TryGetValue(d, out var stats))
                depths[d] = stats = new DepthStats();
            return stats;
        }

        void OnKilled(EnemyController enemy)
        {
            if (depth > 0)
                Stats(depth).Kills++;
            if (enemy.Definition.Rank == EnemyRank.Boss)
            {
                bossKilled = true;
                bossKilledAt = clock;
            }
        }

        void OnDied()
        {
            deaths++;
            if (depth > 0)
                Stats(depth).Deaths++;
            if (deaths >= DeathLimit)
                Finish($"stopped after {deaths} deaths");
        }

        void OnHitTaken(float damage)
        {
            if (depth <= 0 || health == null)
                return;
            var stats = Stats(depth);
            stats.DamageTaken += damage;
            stats.MinLife = Mathf.Min(stats.MinLife, health.Fraction);
        }

        void OnPotionChanged()
        {
            if (session.Potion.Charges < lastCharges && depth > 0)
                Stats(depth).Potions++;
            lastCharges = session.Potion.Charges;
        }

        // Equip what the power score calls an upgrade; throw the rest away so the bag never fills.
        void OnPickedUp(Item item)
        {
            rarityFound[(int)item.Rarity]++;
            if (PowerScore.IsUpgrade(session.Equipment, item, session.Level, session.PassiveTree.Bonuses))
                session.EquipFromInventory(item);
            else
                session.Discard(item);
        }

        void Update()
        {
            if (finished || stick == null || player == null)
                return;

            clock += Time.deltaTime;
            flowAge += Time.deltaTime;
            if (depth > 0)
                Stats(depth).LevelOut = session.Level;
            if (clock > TimeLimitSeconds)
            {
                Finish("stopped at the time limit");
                return;
            }
            if (health != null && !health.IsAlive)
            {
                stick.TestOverride = Vector2.zero;
                return;
            }

            var me = IsoMath.WorldToGround(player.transform.position);
            var move = depth == 0 ? TownMove(me) : DungeonMove(me);
            move = Unpin(me, move);
            stick.TestOverride = move.sqrMagnitude > 1e-4f ? new Vector2(move.x, move.y * IsoMath.GroundSquash).normalized : Vector2.zero;
        }

        Vector2 Unpin(Vector2 me, Vector2 move)
        {
            if (sidestepTimer > 0f)
            {
                sidestepTimer -= Time.deltaTime;
                return Rotate(move, SidestepDegrees * sidestepSign);
            }

            pinCheckTimer += Time.deltaTime;
            if (move.sqrMagnitude < 1e-4f)
            {
                pinCheckTimer = 0f;
                pinCheckFrom = me;
                return move;
            }
            if (pinCheckTimer >= PinnedSeconds)
            {
                if (Vector2.Distance(me, pinCheckFrom) < PinnedDistance)
                {
                    sidestepTimer = SidestepSeconds;
                    sidestepSign = -sidestepSign;
                }
                pinCheckTimer = 0f;
                pinCheckFrom = me;
            }
            return move;
        }

        static Vector2 Rotate(Vector2 vector, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        Vector2 TownMove(Vector2 me)
        {
            var stairs = GameObject.Find("Stairs Down");
            return stairs != null ? (IsoMath.WorldToGround(stairs.transform.position) - me).normalized : Vector2.zero;
        }

        Vector2 DungeonMove(Vector2 me)
        {
            // 1. Out of any ground warning, straight away from its center.
            foreach (var marker in GroundMarker.Live)
            {
                if (!marker.IsCircle || !marker.IsVisible)
                    continue;
                var away = me - marker.Center;
                if (away.magnitude < marker.Radius + DangerMargin)
                    return away.sqrMagnitude > 1e-4f ? away.normalized : Vector2.right;
            }

            // 2. Our own corpse, to get the gear back.
            var corpse = FindAnyObjectByType<CorpseMarker>();
            if (corpse != null)
                return Go(me, corpse, IsoMath.WorldToGround(corpse.transform.position), 0f);

            // 3. The nearest enemy close by: walk up to it and stand to fight.
            var near = NearestEnemy(me, EngageRange);
            if (near != null)
                return Go(me, near, near.GroundPosition, StandDistance + near.Definition.BodyRadius);

            // 4. Loot lying around once the fight is over.
            LootDrop bestDrop = null;
            var bestDropDistance = LootRange;
            foreach (var drop in FindObjectsByType<LootDrop>(FindObjectsSortMode.None))
            {
                if (!drop.gameObject.activeInHierarchy || unreachable.Contains(drop))
                    continue;
                var distance = Vector2.Distance(me, drop.GroundPosition);
                if (distance < bestDropDistance)
                {
                    bestDrop = drop;
                    bestDropDistance = distance;
                }
            }
            if (bestDrop != null)
                return Go(me, bestDrop, bestDrop.GroundPosition, 0f);

            // 5. The next enemy anywhere on the level.
            var far = NearestEnemy(me, float.MaxValue);
            if (far != null)
                return Go(me, far, far.GroundPosition, StandDistance + far.Definition.BodyRadius);

            // 6. The level is clear: down, or home after the boss.
            var exit = GameObject.Find(bossKilled ? "Stairs To Town" : "Stairs Down");
            if (exit != null)
                return Go(me, exit, IsoMath.WorldToGround(exit.transform.position), 0f);
            return Vector2.zero;
        }

        EnemyController NearestEnemy(Vector2 me, float range)
        {
            EnemyController best = null;
            var bestDistance = range;
            var list = enemies.Active;
            for (var i = 0; i < list.Count; i++)
            {
                var enemy = list[i];
                if (!enemy.IsAlive || unreachable.Contains(enemy))
                    continue;
                var distance = Vector2.Distance(me, enemy.GroundPosition);
                if (distance < bestDistance)
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }
            if (best != null && best.Definition.Rank == EnemyRank.Boss && bossEngagedAt < 0f && bestDistance < EngageRange)
                bossEngagedAt = clock;
            return best;
        }

        // Walks toward a goal along the walls (a flow field to it, refreshed now and then), stopping at a distance.
        // A goal that gets no closer for a while is given up on, so one unreachable enemy cannot stall the run.
        Vector2 Go(Vector2 me, object goal, Vector2 at, float stopAt)
        {
            var distance = Vector2.Distance(me, at);
            if (!ReferenceEquals(goal, target))
            {
                target = goal;
                targetSince = clock;
                targetBestDistance = distance;
            }
            if (distance < targetBestDistance - 0.5f)
            {
                targetBestDistance = distance;
                targetSince = clock;
            }
            else if (clock - targetSince > StuckSeconds && (goal is EnemyController || goal is LootDrop))
            {
                unreachable.Add(goal);
                target = null;
            }

            if (distance <= stopAt)
                return Vector2.zero;
            if (flow == null || enemies.Nav.HasLineOfSight(me, at))
                return (at - me).normalized;

            var cell = IsoMath.GroundToCell(at);
            if (cell != flowGoal || flowAge > FlowRefreshSeconds)
            {
                flow.Compute(cell, 400f);
                flowGoal = cell;
                flowAge = 0f;
            }
            return flow.TryGetDirection(me, out var direction) ? direction : (at - me).normalized;
        }

        void Finish(string how)
        {
            if (finished)
                return;
            finished = true;
            if (stick != null)
                stick.TestOverride = null;
            Time.captureDeltaTime = 0f;

            var text = new StringBuilder();
            text.AppendLine("# Autopilot report (act 1)");
            text.AppendLine();
            text.AppendLine($"Result: {how}. Game time {clock / 60f:0.0} min, {deaths} deaths, character level {session.Level}, " +
                            $"power {PowerScore.Evaluate(session).Score:0}.");
            if (bossEngagedAt >= 0f)
                text.AppendLine($"Boss: engaged at {bossEngagedAt / 60f:0.0} min, " + (bossKilledAt >= 0f ? $"killed {bossKilledAt - bossEngagedAt:0} s later." : "not killed."));
            text.AppendLine($"Items picked up: {rarityFound[0]} Common, {rarityFound[1]} Magic, {rarityFound[2]} Rare, {rarityFound[3]} Legendary.");
            text.AppendLine();
            text.AppendLine("| Depth | Minutes in the level | Deaths | Kills | Level in → out | Lowest life | Damage taken | Potions |");
            text.AppendLine("|---|---|---|---|---|---|---|---|");
            for (var d = 1; d <= DungeonRules.LevelsPerAct; d++)
            {
                if (!depths.TryGetValue(d, out var s))
                    continue;
                var end = s.LastExit > s.FirstEntry ? s.LastExit : clock;
                text.AppendLine($"| {d} | {(end - s.FirstEntry) / 60f:0.0} | {s.Deaths} | {s.Kills} | {s.LevelIn} → {s.LevelOut} | {s.MinLife * 100f:0}% | {s.DamageTaken:0} | {s.Potions} |");
            }
            text.AppendLine();
            text.AppendLine("Minutes in a level run from the first arrival to leaving it for the next, so they include trips back after a death. " +
                            "The autopilot fights every pack standing, steps out of ground circles, equips upgrades by power score and discards the rest, and never uses the Forge.");

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, text.ToString());
            Debug.Log($"[ARPG] Autopilot finished: {how}. Report at {ReportPath}.\n{text}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Destroy(gameObject);
        }
    }
}
#endif
