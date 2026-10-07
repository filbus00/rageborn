using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>
    /// A frame-time benchmark on the device (development builds only), for what the editor cannot show: the phone's GPU
    /// (2026-10-06: the road ran at 30 fps on the iPhone 11). Started by launching the app with <c>-benchmark</c> (or a
    /// file <c>benchmark.flag</c> in the app's data folder). It never touches the save (<see cref="SaveDirector"/> stays
    /// off): it loads stretch 1 of the road with a fresh character, kept alive, and measures standing in the hall with
    /// the stream coming, then the same with one thing switched off at a time (the 2D lights, the walls, the ground,
    /// the enemies, the UI), then walking up the hall. Frame times come from <see cref="FrameTimingManager"/> (CPU main
    /// and render threads, GPU). The report is written to <c>benchmark.txt</c> in the data folder and the app quits.
    /// </summary>
    public class DeviceBenchmark : MonoBehaviour
    {
        public static bool Active { get; private set; }

        const float PhaseSeconds = 8f;
        const float SnapshotSeconds = 15f;
        const int PlaySnapshots = 16;

        readonly FrameTiming[] timing = new FrameTiming[1];
        readonly StringBuilder report = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Detect()
        {
            Active = false;
            if (!Debug.isDebugBuild)
                return;
            foreach (var arg in System.Environment.GetCommandLineArgs())
                if (arg == "-benchmark")
                    Active = true;
            var flag = Path.Combine(Application.persistentDataPath, "benchmark.flag");
            if (File.Exists(flag))
            {
                Active = true;
                File.Delete(flag);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            if (!Active)
                return;
            var go = new GameObject("Device Benchmark");
            DontDestroyOnLoad(go);
            go.AddComponent<DeviceBenchmark>();
        }

        IEnumerator Start()
        {
            report.AppendLine($"Benchmark {System.DateTime.Now:yyyy-MM-dd HH:mm}, {SystemInfo.deviceModel}, {Screen.width}x{Screen.height}, target {Application.targetFrameRate} fps");
            yield return new WaitForSeconds(1f);
            // The player's own character, read from the save and never written back (saving stays off; only the read,
            // none of the loader's set-asides), on the open rift's stretch, as the owner plays it.
            var store = new SaveStore(SaveDirector.SaveDirectory);
            var loaded = store.Load();
            if (loaded.Loaded && !SaveCodec.IsRetired(loaded.Data))
                GameSession.Install(SaveCodec.Restore(loaded.Data, 1));
            var session = GameSession.Current;
            var stretch = RoadRules.StretchFromTown(session.DeepestDepth);
            report.AppendLine($"save {(loaded.Loaded ? "loaded" : "none, fresh character")}: level {session.Level}, open rift {stretch}, pet {(session.Pets.Active.HasValue ? session.Pets.Active.Value.ToString() : "none")}");
            session.Travel = new LevelTravel(stretch, Arrival.FromAbove);
            SceneManager.LoadScene(SceneTravel.DungeonScene);
            yield return null;
            yield return new WaitForSeconds(1f);

            var level = FindAnyObjectByType<DungeonLevel>();
            var player = FindAnyObjectByType<PlayerController>();
            var health = FindAnyObjectByType<PlayerHealth>();
            var stick = FindAnyObjectByType<FloatingStickInput>();
            var road = RoadGenerator.Generate(DungeonRules.LevelSeed(GameSession.Current.DungeonSeed, level.Depth), level.Depth);
            report.AppendLine($"stretch {level.Depth}, hall {road.Path.Length:0} units, lights {FindObjectsByType<Light2D>(FindObjectsSortMode.None).Length}");

            // Stand a quarter of the way up the hall, out of the bottom beacon's light, so the stream comes.
            var spot = IsoMath.GroundToWorld(road.Path.PointAt(30f));
            Place(player, spot);
            StartCoroutine(KeepAlive(health));
            yield return Measure("warm up (not counted)", 3f, false);
            yield return Measure("standing, all on", PhaseSeconds, true);

            // Then play for four minutes (the owner, 2026-10-07: "the fps drops to 30 after around 2-3 minutes"): fight
            // standing and push up the hall in turn, and every 15 s note the frame times beside what might pile up.
            var enemies = FindAnyObjectByType<EnemyManager>();
            var kills = 0;
            enemies.Killed += _ => kills++;
            StartCoroutine(Walk(player, stick, road));
            for (var snapshot = 1; snapshot <= PlaySnapshots; snapshot++)
            {
                walking = snapshot % 2 == 0;
                yield return Measure($"t {snapshot * SnapshotSeconds:0} s, {(walking ? "walking" : "standing")}", SnapshotSeconds, true);
                var loot = FindAnyObjectByType<LootDirector>();
                report.AppendLine($"  kills {kills}, level {GameSession.Current.Level}, lights {FindObjectsByType<Light2D>(FindObjectsSortMode.None).Length}, " +
                                  $"objects {FindObjectsByType<Transform>(FindObjectsSortMode.None).Length}, drops {(loot != null ? loot.Active.Count : 0)}, " +
                                  $"managed {System.GC.GetTotalMemory(false) / 1048576f:0} MB, backpack {GameSession.Current.Inventory.Count}");
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "benchmark.txt"), report.ToString());
            }
            walking = false;
            stick.TestOverride = null;

            var path = Path.Combine(Application.persistentDataPath, "benchmark.txt");
            File.WriteAllText(path, report.ToString());
            Debug.Log("[ARPG] Benchmark written to " + path + "\n" + report);
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        bool walking;

        IEnumerator Walk(PlayerController player, FloatingStickInput stick, RoadLayout road)
        {
            while (true)
            {
                if (!walking)
                {
                    stick.TestOverride = Vector2.zero;
                    yield return null;
                    continue;
                }
                var here = IsoMath.WorldToGround(player.transform.position);
                var progress = road.Path.Progress(here);
                var target = road.Path.PointAt(progress + 2f) + (road.Path.PointAt(progress) - here) * 0.8f;
                var step = target - here;
                stick.TestOverride = new Vector2(step.x, step.y * 0.5f).normalized;
                yield return null;
            }
        }

        static IEnumerator KeepAlive(PlayerHealth health)
        {
            while (health != null)
            {
                health.Heal(1e6f);
                yield return null;
            }
        }

        IEnumerator Measure(string label, float seconds, bool record)
        {
            var cpu = new List<float>();
            var main = new List<float>();
            var render = new List<float>();
            var gpu = new List<float>();
            var delta = new List<float>();
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, timing) > 0)
                {
                    cpu.Add((float)timing[0].cpuFrameTime);
                    main.Add((float)timing[0].cpuMainThreadFrameTime);
                    render.Add((float)timing[0].cpuRenderThreadFrameTime);
                    gpu.Add((float)timing[0].gpuFrameTime);
                }
                delta.Add(Time.unscaledDeltaTime * 1000f);
            }
            if (!record)
                yield break;
            var enemies = FindAnyObjectByType<EnemyManager>();
            var loot = FindAnyObjectByType<LootDirector>();
            report.AppendLine($"\n{label} ({delta.Count} frames, {delta.Count / seconds:0.0} fps, enemies {(enemies != null ? enemies.ActiveCount : 0)}, drops on the ground {(loot != null ? loot.Active.Count : 0)})");
            report.AppendLine("  frame  " + Stats(delta));
            report.AppendLine("  main   " + Stats(main));
            report.AppendLine("  render " + Stats(render));
            report.AppendLine("  gpu    " + Stats(gpu));
            report.AppendLine("  cpu    " + Stats(cpu));
        }

        static string Stats(List<float> values)
        {
            if (values.Count == 0)
                return "no data";
            values.Sort();
            var sum = 0f;
            foreach (var v in values)
                sum += v;
            return $"avg {sum / values.Count:0.0} ms, median {values[values.Count / 2]:0.0}, p95 {values[(int)(values.Count * 0.95f)]:0.0}, max {values[values.Count - 1]:0.0}";
        }

        static void Place(PlayerController player, Vector2 world)
        {
            var position = new Vector3(world.x, world.y, 0f);
            player.transform.position = position;
            if (player.TryGetComponent<Rigidbody2D>(out var body))
                body.position = position;
        }

        static void SetRenderers(List<TilemapRenderer> renderers, bool on)
        {
            foreach (var renderer in renderers)
                if (renderer != null)
                    renderer.enabled = on;
        }

        static void SetAll<T>(List<T> things, bool on) where T : Behaviour
        {
            foreach (var thing in things)
                if (thing != null)
                    thing.enabled = on;
        }
    }
}
