using System.Linq;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The 2026-10-17 batch: deep entrances, the lightning lances, the Retcher, enemy life.</summary>
    public class DeepEntranceTests
    {
        [Test]
        public void AnEntranceOpens_ForEveryTenDepthsEverReached()
        {
            CollectionAssert.IsEmpty(EntranceRules.Open(9));
            CollectionAssert.AreEqual(new[] { 10 }, EntranceRules.Open(10));
            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, EntranceRules.Open(39));
            CollectionAssert.AreEqual(new[] { 10, 20, 30, 40 }, EntranceRules.Open(48));
        }

        [Test]
        public void TheEntrances_StandClearOfTheTownsPieces_AndOfEachOther()
        {
            var cells = new[] { 10, 20, 30, 40 }.Select(EntranceRules.Cell).ToList();
            Assert.AreEqual(4, cells.Distinct().Count());
            foreach (var cell in cells)
                foreach (var piece in TownLayout.Pieces)
                    Assert.IsFalse(TownLayout.Covers(piece, cell, 0.5f), $"{cell} stands on {piece.Name}");
        }

        [Test]
        public void ARunBegunDeeper_IsOwedABoonForEverySkippedDepth()
        {
            var deep = new GameSession(1);
            Assert.IsTrue(deep.ReachDepth(20));
            Assert.AreEqual(20, deep.PendingBoons);
            var shallow = new GameSession(1);
            shallow.ReachDepth(1);
            Assert.AreEqual(1, shallow.PendingBoons);
        }

        [Test]
        public void LightningLances_StrikeASecondLater_AndStandTwiceAsFarApart()
        {
            Assert.AreEqual(1f, EliteAffixRules.LanceExtraSeconds);
            Assert.AreEqual(4.8f, EliteAffixRules.LanceGap);
        }

        [Test]
        public void TheRetcher_SpewsFromTheBack_FromDepthFour()
        {
            var entry = PackComposition.Table.Single(e => e.Member == PackMember.Retcher);
            Assert.AreEqual(PackRole.Back, entry.Role);
            Assert.AreEqual(4, entry.First);
            var special = EnemySpecialRules.For("Retcher");
            Assert.AreEqual(SpecialShape.Spew, special.Shape);
            Assert.AreEqual(SpecialShape.Spew, EnemySpecialRules.For("RetcherChampion").Shape, "its ranks spew too");
            Assert.IsTrue(special.Poison);
        }

        [Test]
        public void EveryEnemy_HasAFifthMoreLife()
        {
            Assert.AreEqual(1.2f, CombatFormulas.EnemyLifeScale);
            Assert.AreEqual(9.6f, CombatFormulas.EnemyLife(1), 1e-4f);
        }
    }
}
