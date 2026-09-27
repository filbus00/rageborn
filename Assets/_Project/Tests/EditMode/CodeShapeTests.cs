using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class CodeShapeTests
    {
        static readonly Regex UnbracedControl = new Regex(@"^\s*(if|else if|for|foreach|while)\b.*\)\s*$|^\s*else\s*$");

        static int Indent(string line) => line.Length - line.TrimStart(' ').Length;

        /// <summary>
        /// No statement is indented as if an unbraced if (or loop) guarded it when only the line before it is guarded.
        /// Twice this hid a sound that played far too often: the potion sound every frame (heard as jumbled music) and
        /// the level-up fanfare on every kill (2026-09-27).
        /// </summary>
        [Test]
        public void NoStatement_LooksGuardedByAnUnbracedIf_WhenItIsNot()
        {
            var found = new List<string>();
            foreach (var file in Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file);
                for (var i = 2; i < lines.Length; i++)
                {
                    var control = lines[i - 2];
                    var body = lines[i - 1];
                    var next = lines[i];
                    if (!UnbracedControl.IsMatch(control) || string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(next))
                        continue;
                    var trimmedBody = body.TrimStart();
                    var trimmedNext = next.TrimStart();
                    // A nested control line or a comment is not a statement; braces and chained calls are not either.
                    if (trimmedBody.StartsWith("//") || trimmedBody.StartsWith("{") || UnbracedControl.IsMatch(body) ||
                        trimmedNext.StartsWith("//") || trimmedNext.StartsWith("{") || trimmedNext.StartsWith("}") ||
                        trimmedNext.StartsWith(".") || trimmedNext.StartsWith("?") || trimmedNext.StartsWith(":") ||
                        trimmedNext.StartsWith("&&") || trimmedNext.StartsWith("||") || trimmedNext.StartsWith("+") ||
                        !body.TrimEnd().EndsWith(";"))
                        continue;
                    if (Indent(body) > Indent(control) && Indent(next) == Indent(body))
                        found.Add($"{file}:{i + 1}: {trimmedNext}");
                }
            }
            CollectionAssert.IsEmpty(found, "Indented like the if's body but not guarded by it:\n" + string.Join("\n", found));
        }
    }
}
