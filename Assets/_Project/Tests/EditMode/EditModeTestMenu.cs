using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>
    /// Tools > ARPG > Run EditMode Tests: runs the whole EditMode suite and writes the counts and every failure to
    /// Logs/EditModeTests.txt. For running the tests from a script (the Unity MCP bridge refuses the test runner API
    /// itself but can run a menu item); the Test Runner window does the same by hand.
    /// </summary>
    public static class EditModeTestMenu
    {
        public const string ResultPath = "Logs/EditModeTests.txt";

        [MenuItem("Tools/ARPG/Run EditMode Tests")]
        public static void Run()
        {
            if (File.Exists(ResultPath))
                File.Delete(ResultPath);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var text = new StringBuilder();
                text.AppendLine($"passed {result.PassCount}, failed {result.FailCount}, skipped {result.SkipCount}");
                AppendFailures(result, text);
                Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
                File.WriteAllText(ResultPath, text.ToString());
                Debug.Log($"[ARPG] EditMode tests: {text.ToString().Split('\n')[0]} (details in {ResultPath})");
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }

            static void AppendFailures(ITestResultAdaptor result, StringBuilder text)
            {
                if (result.HasChildren)
                {
                    foreach (var child in result.Children)
                        AppendFailures(child, text);
                }
                else if (result.TestStatus == TestStatus.Failed)
                {
                    text.AppendLine($"{result.FullName}: {result.Message}");
                }
            }
        }
    }
}
