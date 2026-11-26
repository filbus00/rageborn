using NUnit.Framework;

namespace ARPG.Tests
{
    public class ForestRulesTests
    {
        [Test]
        public void TheEdge_IsTreesAndBushes_NeverBare_AboutSixInTenTrees()
        {
            int trees = 0, total = 2000;
            for (var h = 0; h < total; h++)
            {
                var g = ForestRules.Raw(true, h * 7919);
                Assert.AreNotEqual(ForestGrowth.Bare, g, "an edge always reads as closed");
                if (g == ForestGrowth.Tree)
                    trees++;
            }
            Assert.AreEqual(ForestRules.EdgeTrees, trees / (float)total, 0.06f);
        }

        [Test]
        public void DeeperIn_IsSparser_WithTheDarkBetween()
        {
            int trees = 0, bare = 0, total = 2000;
            for (var h = 0; h < total; h++)
            {
                var g = ForestRules.Raw(false, h * 104729);
                if (g == ForestGrowth.Tree)
                    trees++;
                if (g == ForestGrowth.Bare)
                    bare++;
            }
            Assert.AreEqual(ForestRules.DeepTrees, trees / (float)total, 0.06f);
            Assert.Greater(bare, total / 4);
        }

        [Test]
        public void NoTree_StandsWithTreesOnBothSides()
        {
            for (var h = 0; h < 500; h++)
            {
                Assert.AreNotEqual(ForestGrowth.Tree, ForestRules.At(true, h, true, true));
                Assert.AreNotEqual(ForestGrowth.Tree, ForestRules.At(false, h, true, true));
                Assert.AreEqual(ForestRules.Raw(true, h), ForestRules.At(true, h, false, true), "one tree beside it is fine");
            }
        }

        [Test]
        public void ATreeStandsOffItsCellsMiddle_ButInsideTheCell()
        {
            var moved = 0;
            for (var h = 0; h < 500; h++)
            {
                var o = ForestRules.Offset(h * 31);
                Assert.LessOrEqual(UnityEngine.Mathf.Abs(o.x), 0.2f);
                Assert.LessOrEqual(UnityEngine.Mathf.Abs(o.y), 0.08f);
                if (o.sqrMagnitude > 0.0001f)
                    moved++;
            }
            Assert.Greater(moved, 400);
        }
    }
}
