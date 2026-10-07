using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// A frame-time benchmark on the device (development builds only), for what the editor cannot show: the phone's GPU
    /// (2026-10-06/07: the game dropped to 30 fps on the iPhone 11; it was iOS's thermal state, through the player
    /// setting "Adjust iOS FPS based on thermal state"). Started by a file <c>benchmark.flag</c> in the app's data folder
    /// (launch arguments do not reach it on iOS). It never writes the save (<see cref="SaveDirector"/> stays off): it
    /// reads the player's character, loads their deepest dungeon level, keeps her alive where she arrives, and notes every
    /// 15 s for four minutes the frame times from <see cref="FrameTimingManager"/> (CPU main and render threads, GPU)
    /// beside kills, lights, objects, drops and memory. The report is written to <c>benchmark.txt</c> in the data folder
    /// as it goes, and the app quits.
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

        // The report is written after every step, and any error or exception the game logs goes into it, so a run that
        // stops early still says where (2026-10-07: two runs on the player's save wrote nothing at all).
        void Write() => File.WriteAllText(Path.Combine(Application.persistentDataPath, "benchmark.txt"), report.ToString());

        void OnEnable() => Application.logMessageReceived += OnLog;

        void OnDisable() => Application.logMessageReceived -= OnLog;

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            report.AppendLine($"[{type}] {message}\n{stack}");
            Write();
        }

        void Step(string line)
        {
            report.AppendLine(line);
            Write();
        }

        IEnumerator Start()
        {
            Step($"Benchmark {System.DateTime.Now:yyyy-MM-dd HH:mm}, {SystemInfo.deviceModel}, {Screen.width}x{Screen.height}, target {Application.targetFrameRate} fps");
            yield return new WaitForSeconds(1f);
            // The player's own character, read from the save and never written back (saving stays off; only the read,
            // none of the loader's set-asides), on their deepest level, as the owner plays it.
            Step("reading the save");
            var store = new SaveStore(SaveDirector.SaveDirectory);
            var loaded = store.Load();
            if (loaded.Loaded && !SaveCodec.IsRetired(loaded.Data))
                GameSession.Install(SaveCodec.Restore(loaded.Data, 1));
            var session = GameSession.Current;
            var depth = Mathf.Clamp(session.DeepestDepth, 1, DungeonRules.Depths);
            Step($"save {(loaded.Loaded ? "loaded" : "none, fresh character")}: level {session.Level}, depth {depth}, pet {(session.Pets.Active.HasValue ? session.Pets.Active.Value.ToString() : "none")}");
            session.Travel = new LevelTravel(depth, Arrival.FromAbove);
            SceneManager.LoadScene(SceneTravel.DungeonScene);
            yield return null;
            yield return new WaitForSeconds(1f);
            Step($"in {SceneManager.GetActiveScene().name}");

            var level = FindAnyObjectByType<DungeonLevel>();
            var player = FindAnyObjectByType<PlayerController>();
            var health = FindAnyObjectByType<PlayerHealth>();
            Step($"depth {level.Depth}, lights {FindObjectsByType<Light2D>(FindObjectsSortMode.None).Length}");
            StartCoroutine(KeepAlive(health));
            yield return Measure("warm up (not counted)", 3f, false);
            yield return Measure("standing, all on", PhaseSeconds, true);

            // Then four minutes (the owner, 2026-10-07: "the fps drops to 30 after around 2-3 minutes"), every 15 s the
            // frame times beside what might pile up.
            var enemies = FindAnyObjectByType<EnemyManager>();
            var kills = 0;
            enemies.Killed += _ => kills++;
            for (var snapshot = 1; snapshot <= PlaySnapshots; snapshot++)
            {
                yield return Measure($"t {snapshot * SnapshotSeconds:0} s", SnapshotSeconds, true);
                var loot = FindAnyObjectByType<LootDirector>();
                report.AppendLine($"  kills {kills}, level {GameSession.Current.Level}, lights {FindObjectsByType<Light2D>(FindObjectsSortMode.None).Length}, " +
                                  $"objects {FindObjectsByType<Transform>(FindObjectsSortMode.None).Length}, drops {(loot != null ? loot.Active.Count : 0)}, " +
                                  $"managed {System.GC.GetTotalMemory(false) / 1048576f:0} MB, backpack {GameSession.Current.Inventory.Count}");
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "benchmark.txt"), report.ToString());
            }

            var path = Path.Combine(Application.persistentDataPath, "benchmark.txt");
            File.WriteAllText(path, report.ToString());
            Debug.Log("[ARPG] Benchmark written to " + path + "\n" + report);
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
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
            Write();
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
    }
}
