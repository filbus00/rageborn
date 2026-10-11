using System.Diagnostics;
using System.Text;

namespace ARPG
{
    /// <summary>
    /// Times a scene load phase by phase (2026-10-11, the owner: "loading times could be somewhat shorter"): started when
    /// the scene is asked for, marked as each part is done, and logged once the level runs. Development builds only.
    /// </summary>
    public static class LoadTimer
    {
        static readonly Stopwatch watch = new Stopwatch();
        static readonly StringBuilder phases = new StringBuilder();
        static long last;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            watch.Reset();
            phases.Clear();
            last = 0;
            ender = null;
            stage = -1;
        }

        /// <summary>The scene is asked for (after any fade). Its first frame ends the timing.</summary>
        public static void Begin(string what)
        {
            if (!UnityEngine.Debug.isDebugBuild)
                return;
            phases.Clear();
            spent.Clear();
            phases.Append(what);
            last = 0;
            watch.Restart();
            if (ender == null)
            {
                var go = new UnityEngine.GameObject("Load Timer");
                UnityEngine.Object.DontDestroyOnLoad(go);
                ender = go.AddComponent<Ender>();
                go.AddComponent<FrameStart>();
                // Subscribed after every director, so its mark covers their sceneLoaded work.
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) =>
                {
                    if (stage == 0)
                    {
                        Mark("scene loaded handlers");
                        stage = 1;
                    }
                };
            }
            stage = 0;
        }

        static Ender ender;
        static int stage = -1;

        // Splits the loaded scene's first frame: starts and updates (marked by the latest script), late updates, then
        // the render (marked by the earliest script of the frame after), which ends the timing.
        [UnityEngine.DefaultExecutionOrder(32000)]
        sealed class Ender : UnityEngine.MonoBehaviour
        {
            void Update()
            {
                if (stage != 1)
                    return;
                Mark("starts, updates");
                stage = 2;
            }

            void LateUpdate()
            {
                if (stage != 2)
                    return;
                Mark("late updates");
                stage = 3;
            }
        }

        [UnityEngine.DefaultExecutionOrder(-32000)]
        sealed class FrameStart : UnityEngine.MonoBehaviour
        {
            void Update()
            {
                if (stage != 3)
                    return;
                stage = -1;
                Mark("render");
                End();
            }
        }

        static readonly System.Collections.Generic.Dictionary<string, (long ticks, int count)> spent =
            new System.Collections.Generic.Dictionary<string, (long, int)>();

        /// <summary>A timestamp for <see cref="Spent"/>, 0 when not timing.</summary>
        public static long Now => watch.IsRunning ? Stopwatch.GetTimestamp() : 0;

        /// <summary>Adds the time since <paramref name="since"/> (from <see cref="Now"/>) to a running total, logged
        /// with the phases: for work spread across them (sheet loads).</summary>
        public static void Spent(string what, long since)
        {
            if (since == 0 || !watch.IsRunning)
                return;
            spent.TryGetValue(what, out var total);
            spent[what] = (total.ticks + Stopwatch.GetTimestamp() - since, total.count + 1);
        }

        public static void Mark(string phase)
        {
            if (!watch.IsRunning)
                return;
            var now = watch.ElapsedMilliseconds;
            phases.Append(", ").Append(phase).Append(' ').Append(now - last).Append(" ms");
            last = now;
        }

        /// <summary>The level runs: logs the total and stops.</summary>
        public static void End()
        {
            if (!watch.IsRunning)
                return;
            foreach (var pair in spent)
                phases.Append("; in all ").Append(pair.Key).Append(' ').Append(pair.Value.ticks * 1000 / Stopwatch.Frequency)
                    .Append(" ms (").Append(pair.Value.count).Append(')');
            spent.Clear();
            UnityEngine.Debug.Log($"[ARPG] Load {watch.ElapsedMilliseconds} ms: {phases}");
            watch.Stop();
        }
    }
}
