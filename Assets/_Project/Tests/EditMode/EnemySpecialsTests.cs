using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class EnemySpecialsTests
    {
        // Every enemy the packs are made of (Data/Enemies), bosses and break targets aside.
        static readonly string[] Roster =
        {
            "Swarmer", "SwarmerChampion", "SwarmerElite", "Ghoul", "BanditArcher", "Skeleton", "Cultist", "AshWolf", "Cutthroat",
            "EmberAcolyte", "PyreKeeper", "CarrionBloat", "Drowned", "Harpooner", "DrownedWatchman", "SkeletonKnight", "GravePriest",
            "Hollowed", "RiftCaller", "VoidWraith",
        };

        [Test]
        public void EveryEnemyType_HasASpecial_ThatCanBeDodged()
        {
            foreach (var name in Roster)
            {
                var special = EnemySpecialRules.For(name);
                Assert.AreNotEqual(SpecialShape.None, special.Shape, name);
                Assert.GreaterOrEqual(special.Fill, 0.7f, name + ": time to get out");
                Assert.Greater(special.Every, special.Fill + EnemySpecialRules.CastSeconds + 1f, name);
                Assert.Less(special.MinRange, special.MaxRange, name);
                if (special.Shape == SpecialShape.Lines)
                    Assert.Greater(special.Length, 0f, name);
            }
            Assert.AreEqual(SpecialShape.None, EnemySpecialRules.For("CinderWarden").Shape, "bosses keep their own fights");
        }

        [Test]
        public void TheLongestKind_Wins()
        {
            Assert.AreEqual("Tide Wave", EnemySpecialRules.For("DrownedWatchman").Name);
            Assert.AreEqual("Brine Spit", EnemySpecialRules.For("Drowned").Name);
            Assert.AreEqual("Shield Bash", EnemySpecialRules.For("SkeletonKnight").Name);
            Assert.AreEqual("Bile Spit", EnemySpecialRules.For("SwarmerElite").Name);
        }

        [Test]
        public void AFan_IsCentredOnHer_AndCirclesFallAroundHer()
        {
            var aim = Vector2.right;
            Assert.AreEqual(aim, EnemySpecialRules.LineDirection(aim, 1, 3, 16f));
            var left = EnemySpecialRules.LineDirection(aim, 0, 3, 16f);
            var right = EnemySpecialRules.LineDirection(aim, 2, 3, 16f);
            Assert.AreEqual(16f, Vector2.Angle(aim, left), 1e-3f);
            Assert.AreEqual(-left.y, right.y, 1e-4f);
            var target = new Vector2(3f, 4f);
            Assert.AreEqual(target, EnemySpecialRules.LobPoint(target, 0, 3, 1.6f, 0.3f));
            Assert.AreEqual(1.6f, Vector2.Distance(target, EnemySpecialRules.LobPoint(target, 2, 3, 1.6f, 0.3f)), 1e-4f);
        }

        [Test]
        public void Enemies_AreQuicker_ButKeepTheirTelegraphs()
        {
            Assert.AreEqual(1.25f, EnemyRules.MoveSpeedFactor(EnemyRank.Normal));
            Assert.AreEqual(1f, EnemyRules.MoveSpeedFactor(EnemyRank.Boss));
            Assert.AreEqual(1.2f, EnemyRules.WindupSpeedFactor(EnemyRank.Normal, EnemyArchetype.Swarmer));
            Assert.AreEqual(1f, EnemyRules.WindupSpeedFactor(EnemyRank.Elite, EnemyArchetype.Brute), "a slam circle keeps its warning");
            Assert.AreEqual(1f, EnemyRules.WindupSpeedFactor(EnemyRank.Normal, EnemyArchetype.Archer), "an aim line keeps its warning");
        }
    }
}
