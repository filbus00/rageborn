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
            Assert.AreEqual("Hand Axe", EnemySpecialRules.For("Fallen").Name);
            Assert.AreEqual("Bone Shards", EnemySpecialRules.For("SwarmerElite").Name);
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
            Assert.AreEqual(0.9f, EnemyRules.MoveSpeedFactor(EnemyRank.Normal), 1e-5f, "10 percent slower (the owner, 2026-10-17)");
            Assert.AreEqual(1f, EnemyRules.MoveSpeedFactor(EnemyRank.Boss), "bosses keep their pace");
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

        [Test]
        public void FewKindsLeavePools_AndTheThrowsUseTheNewProjectiles()
        {
            // The owner, 2026-10-17: "pools are okay but now there are too many enemies using that attack".
            var pools = 0;
            var kinds = new System.Collections.Generic.HashSet<ProjectileKind>();
            foreach (var kind in EnemySpecialRules.Kinds)
            {
                var special = EnemySpecialRules.For(kind);
                if (special.LingerSeconds > 0f)
                    pools++;
                if (special.Shape == SpecialShape.Throw)
                    kinds.Add(special.Kind);
            }
            Assert.LessOrEqual(pools, 3);
            foreach (var wanted in new[] { ProjectileKind.Knife, ProjectileKind.Axe, ProjectileKind.Fireball, ProjectileKind.Bomb, ProjectileKind.Bouncing })
                Assert.IsTrue(kinds.Contains(wanted), wanted.ToString());
        }

        [Test]
        public void ABomb_LandsWhereSheStood_OthersFlyTheirLength()
        {
            var bomb = EnemySpecialRules.For("Imp");
            Assert.AreEqual(ProjectileKind.Bomb, bomb.Kind);
            Assert.AreEqual(5f, EnemySpecialRules.ThrowReach(bomb, 5f), 1e-4f);
            Assert.AreEqual(bomb.Length, EnemySpecialRules.ThrowReach(bomb, 50f), 1e-4f);
            var knife = EnemySpecialRules.For("Harpy");
            Assert.AreEqual(knife.Length, EnemySpecialRules.ThrowReach(knife, 3f), 1e-4f);
        }

        [Test]
        public void ABlade_BouncesOffTheWallItHits()
        {
            // A wall along x >= 2 (cells): flying right, it turns back in x and keeps its y.
            bool Walkable(Vector2Int c) => IsoMath.CellToGround(c).x < IsoMath.CellToGround(new Vector2Int(2, 0)).x;
            var at = IsoMath.CellToGround(new Vector2Int(1, 0));
            var velocity = new Vector2(10f, 0f);
            var bounced = ProjectileKinds.Bounce(at, velocity, Walkable);
            Assert.Less(bounced.x, 0f);
            Assert.AreEqual(velocity.magnitude, bounced.magnitude, 1e-4f);
        }
    }
}
