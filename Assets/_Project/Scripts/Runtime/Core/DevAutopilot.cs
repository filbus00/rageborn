// Development builds only. Unity 6.6 deprecated DEVELOPMENT_BUILD; a development build still defines
// UNITY_INCLUDE_INSTRUMENTATION, a Release managed code variant without Development Build does not.
#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
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

        // The balance probe (2026-10-17, the owner: "run a ballance test, the game feels too easy"): in place of one long
        // run, a fresh run at each of these depths, owed the boons of the depths skipped (picked at random) and in Magic
        // gear of the depth's item level, fought for ProbeSeconds of game time. Writes Logs/BalanceProbe.md.
        public const string ProbeKey = "ARPG.AutopilotProbe";
        static readonly int[] ProbeDepths = { 3, 9, 15, 21, 27, 33 };
        const float ProbeSeconds = 150f;
        const string ProbeReportPath = "Logs/BalanceProbe.md";
        bool probing;
        static bool startProbing;
        int probeIndex = -1;
        float probeClock = -1f;
        int probeKills, probeHits;
        float probeDamage, probeMinLife, probeMaxLife;
        bool probeDied;
        readonly List<string> probeRows = new List<string>();
        static readonly ItemSlot[] ProbeKit = { ItemSlot.Weapon, ItemSlot.Shield, ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Gloves,
            ItemSlot.Boots, ItemSlot.Belt, ItemSlot.Amulet, ItemSlot.Ring };
        const string ReportPath = "Logs/AutopilotReport.md";
        const float StepSeconds = 1f / 30f;
        const float TimeLimitSeconds = 90f * 60f;
        // The roguelite (2026-10-17): three runs, each from depth 1 until she dies, or one that reaches past this depth.
        const int DeathLimit = 3;
        const int TargetDepth = 24;

        const string TownScene = "Town";
        
        // Bows only (2026-09-30): it shoots from about this far, and backs away from anything closer than the kite distance.
        const float ShootDistance = 5.5f;
        const float KiteDistance = 2.5f;
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
            public int Deaths, Kills, Potions, LevelIn, LevelOut, Visits, Runs;
        }

        // Each run's end: how deep, after how long, with how many boons.
        readonly List<string> runs = new List<string>();
        float runStartedAt;
        System.Random boonPicks = new System.Random(17);

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
            var probe = UnityEditor.EditorPrefs.GetBool(ProbeKey, false);
            if (!UnityEditor.EditorPrefs.GetBool(RequestKey, false) && !probe)
                return;
            UnityEditor.EditorPrefs.SetBool(RequestKey, false);
            UnityEditor.EditorPrefs.SetBool(ProbeKey, false);
            var go = new GameObject("Dev Autopilot");
            DontDestroyOnLoad(go);
            // Read in Awake, which runs inside AddComponent.
            startProbing = probe;
            go.AddComponent<DevAutopilot>();
#endif
        }

        void Awake()
        {
            probing = startProbing;
            startProbing = false;
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
                if (probing)
                {
                    // The town after a probe's death, or at the start: on to the next probe.
                    if (probeClock >= 0f)
                        EndProbe();
                    StartNextProbe();
                    return;
                }
                if (bossKilled)
                    Finish("the Cinder Warden is dead and the character is back in town");
                return;
            }

            var level = LevelContext.CurrentId;
            depth = level != null && level.StartsWith("Dungeon ") ? int.Parse(level.Substring(8)) : 0;
            if (depth > TargetDepth && !probing)
            {
                runs.Add($"reached depth {depth} in {(clock - runStartedAt) / 60f:0.0} min with {session.Talents.Spent} boons");
                Finish($"a run reached depth {depth}");
                return;
            }
            var stats = Stats(depth);
            stats.Visits++;
            stats.Runs++;
            if (stats.FirstEntry < 0f)
            {
                stats.FirstEntry = clock;
                stats.LevelIn = session.Level;
                if (depth > 1)
                    Stats(depth - 1).LastExit = clock;
            }
            if (enemies != null)
                enemies.Killed += OnKilled;
            if (probing && probeClock < 0f && depth > 0)
            {
                probeClock = 0f;
                probeMaxLife = health != null ? health.MaxLife : 0f;
            }
        }

        // A fresh run at the next probe depth, in Magic gear of its item level; the boons come owed on arrival.
        void StartNextProbe()
        {
            probeIndex++;
            if (probeIndex >= ProbeDepths.Length)
            {
                FinishProbe();
                return;
            }
            var target = ProbeDepths[probeIndex];
            session.PickedUp -= OnPickedUp;
            session = new GameSession(1000 + probeIndex);
            GameSession.Install(session);
            session.PickedUp += OnPickedUp;
            var level = DungeonRules.EnemyLevel(target);
            var kit = EquipmentState.Empty;
            foreach (var slot in ProbeKit)
                kit = kit.Equip(EquipmentState.PlacesFor(slot)[0], session.Loot.RollItemOf(slot, ItemRarity.Magic, level));
            session.Equip(kit);
            probeClock = -1f;
            probeKills = probeHits = 0;
            probeDamage = 0f;
            probeMinLife = 1f;
            probeDied = false;
            SceneTravel.ToDepth(target, Arrival.FromAbove);
        }

        void EndProbe()
        {
            var target = ProbeDepths[probeIndex];
            var minutes = Mathf.Max(0.01f, probeClock / 60f);
            var maxLife = probeMaxLife;
            var power = PowerScore.Evaluate(session);
            probeRows.Add($"| {target} | {DungeonRules.EnemyLevel(target)} | {session.Talents.Spent} | {power.DamagePerSecond:0} | {power.EffectiveLife:0} | " +
                          $"{(probeDied ? $"died at {probeClock:0} s" : $"alive, {probeClock:0} s")} | {probeKills} ({probeKills / minutes:0.0}/min) | " +
                          $"{probeHits} hits, {probeDamage:0} ({(maxLife > 0f ? probeDamage / maxLife : 0f):0.0} lives) | {probeMinLife * 100f:0}% |");
            probeClock = -1f;
        }

        void FinishProbe()
        {
            finished = true;
            if (stick != null)
                stick.TestOverride = null;
            Time.captureDeltaTime = 0f;
            var text = new StringBuilder();
            text.AppendLine("# Balance probe");
            text.AppendLine();
            text.AppendLine($"A fresh run started at each depth, owed the boons of the depths skipped (picked at random), in Magic gear of the depth's item level, " +
                            $"no potions, played by the autopilot (it stands to shoot, steps out of circles, fights every pack) for {ProbeSeconds:0} s of game time or until it died.");
            text.AppendLine();
            text.AppendLine("| Depth | Enemy level | Boons | Power: DPS | Power: effective life | Result | Kills | Damage taken | Lowest life |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var row in probeRows)
                text.AppendLine(row);
            Directory.CreateDirectory(Path.GetDirectoryName(ProbeReportPath));
            File.WriteAllText(ProbeReportPath, text.ToString());
            Debug.Log($"[ARPG] Balance probe finished. Report at {ProbeReportPath}.\n{text}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Destroy(gameObject);
        }

        DepthStats Stats(int d)
        {
            if (!depths.TryGetValue(d, out var stats))
                depths[d] = stats = new DepthStats();
            return stats;
        }

        void OnKilled(EnemyController enemy)
        {
            if (probing && probeClock >= 0f)
                probeKills++;
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
            if (probing)
            {
                probeDied = true;
                return;
            }
            runs.Add($"died at depth {depth} after {(clock - runStartedAt) / 60f:0.0} min with {session.Talents.Spent} boons, " +
                     $"{session.Run.Kills} kills");
            runStartedAt = clock;
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
            if (probing && probeClock >= 0f)
            {
                probeHits++;
                probeDamage += damage;
                probeMinLife = Mathf.Min(probeMinLife, health.Fraction);
            }
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

        // Stat points go round the five attributes, one in five on each: the typical build the balance report models.
        static readonly Attribute[] AllAttributes = (Attribute[])System.Enum.GetValues(typeof(Attribute));

        void SpendStatPoints()
        {
            while (session.Attributes.Available(session.Level) > 0)
            {
                var next = AllAttributes[session.Attributes.TotalSpent % AllAttributes.Length];
                if (!session.Attributes.Spend(next, session.Level))
                    break;
            }
        }

        // Equip what the power score calls an upgrade; throw the rest away so the bag never fills.
        void OnPickedUp(Item item)
        {
            rarityFound[(int)item.Rarity]++;
            if (PowerScore.IsUpgrade(session.Equipment, item, session.Level, session.Talents.Bonuses))
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
            // A new run replaces the session (the roguelite): follow it.
            if (session != GameSession.Current)
            {
                session.PickedUp -= OnPickedUp;
                session = GameSession.Current;
                session.PickedUp += OnPickedUp;
            }
            // A boon owed: one of those offered, at random, as a player who does not plan would.
            if (BoonScreen.IsOpen)
            {
                var offer = BoonRules.Offer(session.Talents, session.Meta.MaxBoonTier, session.Meta.BoonChoices, boonPicks);
                if (offer.Count == 0 || !session.TakeBoon(offer[boonPicks.Next(offer.Count)]))
                    session.PendingBoons = 0;
                if (session.PendingBoons <= 0)
                    TownSheets.CloseAny();
                return;
            }
            if (depth == 0 && TownSheets.AnyOpen)
                TownSheets.CloseAny();
            if (probing && probeClock >= 0f && depth > 0 && health != null && health.IsAlive)
            {
                probeClock += Time.deltaTime;
                if (probeClock >= ProbeSeconds)
                {
                    EndProbe();
                    StartNextProbe();
                    return;
                }
            }
            SpendStatPoints();
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

            // 3. The nearest enemy close by: with a bow, back off from one that is too close, stand and shoot at one in
            // reach and in sight, else walk up until it is.
            var near = NearestEnemy(me, EngageRange);
            if (near != null)
                return Engage(me, near);

            // 4. Loot lying around once the fight is over.
            LootDrop bestDrop = null;
            var bestDropDistance = LootRange;
            foreach (var drop in FindObjectsByType<LootDrop>())
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
                return Go(me, far, far.GroundPosition, ShootDistance - 1f);

            // 6. The level is clear: down (past a dead boss too, since 2026-10-05), or home from the bottom.
            var exit = GameObject.Find("Stairs Down") ?? (bossKilled ? GameObject.Find("Stairs To Town") : null);
            if (exit != null)
                return Go(me, exit, IsoMath.WorldToGround(exit.transform.position), 0f);
            return Vector2.zero;
        }

        // A mediocre archer: it keeps its distance by walking straight away (sliding sideways along a wall), and otherwise
        // stands still to shoot, so its arrows get Stillness while it has room.
        Vector2 Engage(Vector2 me, EnemyController enemy)
        {
            var gap = Vector2.Distance(me, enemy.GroundPosition);
            if (gap < KiteDistance + enemy.Definition.BodyRadius)
            {
                var away = me - enemy.GroundPosition;
                away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector2.right;
                foreach (var turn in new[] { 0f, 45f, -45f, 90f, -90f })
                {
                    var r = turn * Mathf.Deg2Rad;
                    var step = new Vector2(away.x * Mathf.Cos(r) - away.y * Mathf.Sin(r), away.x * Mathf.Sin(r) + away.y * Mathf.Cos(r));
                    if (enemies.Nav.IsWalkable(IsoMath.GroundToCell(me + step)))
                        return step;
                }
            }
            if (gap <= ShootDistance + enemy.Definition.BodyRadius && enemies.Nav.HasLineOfSight(me, enemy.GroundPosition))
                return Vector2.zero;
            return Go(me, enemy, enemy.GroundPosition, ShootDistance - 1f);
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
            text.AppendLine("# Autopilot report (the roguelite)");
            text.AppendLine();
            text.AppendLine($"Result: {how}. Game time {clock / 60f:0.0} min, {deaths} deaths, character level {session.Level}, " +
                            $"power {PowerScore.Evaluate(session).Score:0}.");
            if (bossEngagedAt >= 0f)
                text.AppendLine($"Boss: engaged at {bossEngagedAt / 60f:0.0} min, " + (bossKilledAt >= 0f ? $"killed {bossKilledAt - bossEngagedAt:0} s later." : "not killed."));
            text.AppendLine($"Items picked up: {rarityFound[0]} Common, {rarityFound[1]} Magic, {rarityFound[2]} Rare, {rarityFound[3]} Legendary.");
            text.AppendLine();
            text.AppendLine("Runs:");
            foreach (var run in runs)
                text.AppendLine("- " + run);
            text.AppendLine();
            text.AppendLine("| Depth | Runs there | Deaths | Kills | Lowest life | Damage taken (all runs) | Potions |");
            text.AppendLine("|---|---|---|---|---|---|---|");
            var visited = new List<int>(depths.Keys);
            visited.Sort();
            foreach (var d in visited)
            {
                if (d <= 0)
                    continue;
                var s = depths[d];
                text.AppendLine($"| {d} | {s.Runs} | {s.Deaths} | {s.Kills} | {s.MinLife * 100f:0}% | {s.DamageTaken:0} | {s.Potions} |");
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
