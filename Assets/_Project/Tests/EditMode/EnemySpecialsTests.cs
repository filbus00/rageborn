using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class EnemySpecialsTests
    {
        // Every enemy the packs are made of (Data/Enemies), bosses and break targets aside.
        static readonly string[] Roster = System.Enum.GetNames(typeof(PackMember)).Where(n => n != "None")
            .Concat(new[] { "Swarmer", "SwarmerChampion", "SwarmerElite" }).ToArray();

        [Test]
        public void EveryEnemyType_HasASpecial_ThatCanBeDodged()
        {
            foreach (var name in Roster)
            {
                var special = EnemySpecialRules.For(name);
                Assert.AreNotEqual(SpecialShape.None, special.Shape, name);
                // A ground shape fills for time to get out; a throw warns with its aim line, then the projectile flies.
                Assert.GreaterOrEqual(special.Fill, special.Shape == SpecialShape.Throw ? 0.45f : 0.7f, name + ": time to get out");
                if (special.Shape == SpecialShape.Throw)
                    Assert.Greater(special.Speed, 0f, name);
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
            Assert.AreEqual("Javelin", EnemySpecialRules.For("SkeletonWarrior").Name);
            Assert.AreEqual("Fire Spit", EnemySpecialRules.For("Fallen").Name);
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
        public void Enemies_AttackQuicker_ButWalkAsBefore_AndTelegraphFaster()
        {
            Assert.AreEqual(1f, EnemyRules.MoveSpeedFactor(EnemyRank.Normal), "they walk at their own speed (the owner, 2026-10-11)");
            Assert.AreEqual(2f, EnemyRules.AttackSpeedFactor(EnemyRank.Normal, EnemyArchetype.Archer), "ranged attacks twice as often (2026-10-17)");
            Assert.AreEqual(1f, EnemyRules.AttackSpeedFactor(EnemyRank.Boss, EnemyArchetype.Archer));
            Assert.AreEqual(1.5f, EnemyRules.ProjectileSpeedFactor(EnemyRank.Normal));
            Assert.AreEqual(1f, EnemyRules.ProjectileDamageFactor(EnemyRank.Normal), "softer since 2026-10-17");
            Assert.AreEqual(1.2f, EnemyRules.WindupSpeedFactor(EnemyRank.Normal, EnemyArchetype.Swarmer));
            Assert.AreEqual(2f, EnemyRules.WindupSpeedFactor(EnemyRank.Elite, EnemyArchetype.Brute), "a slam circle fills twice as fast (2026-10-11)");
            Assert.AreEqual(2f, EnemyRules.WindupSpeedFactor(EnemyRank.Normal, EnemyArchetype.Archer), "an aim line too");
            Assert.AreEqual(1f, EnemyRules.WindupSpeedFactor(EnemyRank.Boss, EnemyArchetype.Brute), "bosses keep their own timing");
            Assert.AreEqual(0.5f, EnemyRules.Telegraph(1f), 1e-5f);
        }
    }
}
