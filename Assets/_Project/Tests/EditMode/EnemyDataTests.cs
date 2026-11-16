using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>
    /// The enemies' data files (2026-11-16, the code review). A level that cannot find a pack member's definition falls
    /// back to the plain enemy without a word (it once showed the Cinder Warden for two forest bosses), and a definition
    /// whose drawn character has no sheet shows a placeholder. These read the project's files, so they run in Unity's
    /// EditMode and in Tools/CompileCheck from the project folder.
    /// </summary>
    public class EnemyDataTests
    {
        const string Definitions = "Assets/_Project/Data/Enemies/";
        const string Characters = "Assets/_Project/Resources/Characters/";

        static bool InProject => Directory.Exists(Definitions);

        static string Field(string assetText, string name)
        {
            var m = Regex.Match(assetText, @"^\s*" + name + @":\s*(.*)$", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        [Test]
        public void EveryPackMember_HasADefinitionAsset()
        {
            if (!InProject)
                Assert.Ignore("not run from the project folder");
            var missing = System.Enum.GetNames(typeof(PackMember)).Where(n => n != "None" && !File.Exists(Definitions + n + ".asset")).ToList();
            Assert.IsEmpty(missing, "no definition for: " + string.Join(", ", missing));
        }

        // The kinds of the four contracts' tables and their bosses. (The older crypt kinds' definitions still name sheets
        // that were removed when the forest's, the gaol's and the rest were redrawn; no contract plays them.)
        static readonly string[] Bosses =
            { "BanditKing", "BroodMother", "Gaoler", "HangedMagistrate", "SaintMarrow", "AshArchivist", "RiftMother", "FirstWatchman" };

        static System.Collections.Generic.IEnumerable<string> PlayedKinds() =>
            new[] { PackComposition.ForestTable, PackComposition.GaolTable, PackComposition.LibraryTable, PackComposition.RiftTable }
                .SelectMany(t => t).Select(e => e.Member.ToString()).Concat(Bosses).Distinct();

        [Test]
        public void EveryPlayedKinds_DrawnCharacter_HasItsSheets()
        {
            if (!InProject)
                Assert.Ignore("not run from the project folder");
            var broken = new System.Collections.Generic.List<string>();
            foreach (var kind in PlayedKinds())
            {
                var path = Definitions + kind + ".asset";
                if (!File.Exists(path))
                {
                    broken.Add(kind + " has no definition");
                    continue;
                }
                var character = Field(File.ReadAllText(path), "spriteCharacter");
                if (!File.Exists(Characters + character + "/" + character + "_idle.png"))
                    broken.Add(kind + " -> '" + character + "'");
            }
            Assert.IsEmpty(broken, "played kinds whose character has no sheet: " + string.Join("; ", broken));
        }
    }
}
