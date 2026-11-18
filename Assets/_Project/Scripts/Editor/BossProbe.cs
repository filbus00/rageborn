using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// A development probe for the boss fights (2026-11-16, the rework of the six unfinished ones): while it runs in play
    /// mode it keeps her topped up, drops the boss's life to each phase's start in turn and writes which attacks fired in
    /// each phase, with the time of the first of each, to Logs/BossProbe.txt. Started from code
    /// (<c>BossProbe.Start(secondsPerPhase)</c>) once she is in a boss arena; stopped by <c>BossProbe.Stop()</c>.
    /// </summary>
    public static class BossProbe
    {
        static bool running;
        static float startedAt;
        static float perPhase;
        static readonly Dictionary<string, int> seen = new Dictionary<string, int>();
        static readonly List<string> lines = new List<string>();
        static int lastPhase;
        static float phaseStarted;
        static string lastAttack = "";
        const string Path = "Logs/BossProbe.txt";

        public static void Start(float secondsPerPhase)
        {
            running = true;
            perPhase = secondsPerPhase;
            startedAt = Time.realtimeSinceStartup;
            phaseStarted = startedAt;
            lastPhase = 0;
            lastAttack = "";
            seen.Clear();
            lines.Clear();
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Stop(bool write = true)
        {
            if (!running)
                return;
            running = false;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            if (write)
                Flush("stopped");
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
                lines.Add($"  !! {type}: {message.Split('\n')[0]}");
        }

        static void Tick()
        {
            if (!running)
                return;
            var fight = Object.FindAnyObjectByType<BossFight>();
            var player = Object.FindAnyObjectByType<PlayerHealth>();
            if (fight == null || fight.Boss == null || player == null)
                return;
            if (player.IsAlive)
                player.Heal(player.MaxLife);
            var boss = fight.Boss;
            if (!boss.IsAlive)
            {
                Flush("boss died");
                Stop(false);
                return;
            }
            // Walk through the phases: after a while in one, take the boss's life to the start of the next.
            var now = Time.realtimeSinceStartup;
            if (fight.Engaged && fight.Phase != lastPhase)
            {
                lastPhase = fight.Phase;
                phaseStarted = now;
                lines.Add($"phase {lastPhase} at {now - startedAt:0.0}s");
            }
            if (fight.Engaged && now - phaseStarted > perPhase)
            {
                var target = fight.Phase == 1 ? 0.64f : fight.Phase == 2 ? 0.31f : 0f;
                if (target > 0f)
                    boss.TakeDamage(Mathf.Max(0f, boss.Life - boss.MaxLife * target));
                else
                {
                    Flush("done");
                    Stop(false);
                    return;
                }
                phaseStarted = now;
            }
            var attack = fight.AttackName;
            if (attack.Length > 0 && attack != lastAttack)
            {
                var key = fight.Phase + " " + attack;
                seen[key] = seen.TryGetValue(key, out var n) ? n + 1 : 1;
            }
            lastAttack = attack;
        }

        static void Flush(string why)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Boss probe ({why}), {Time.realtimeSinceStartup - startedAt:0}s");
            foreach (var line in lines)
                sb.AppendLine(line);
            var keys = new List<string>(seen.Keys);
            keys.Sort();
            foreach (var key in keys)
                sb.AppendLine($"  {key}: {seen[key]}");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText(Path, sb.ToString());
        }
    }
}
