using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class EnemyTypesTests
    {
        // --- Archer steering ---

        [Test]
        public void AnArcher_ClosesIn_WhenItCannotSeeThePlayer_OrIsOutOfRange()
        {
            Assert.AreEqual(1, ArcherSteering.Intent(4f, lineOfSight: false, preferredRange: 5f, attackRange: 7.5f));
            Assert.AreEqual(1, ArcherSteering.Intent(9f, lineOfSight: true, preferredRange: 5f, attackRange: 7.5f));
        }

        [Test]
        public void AnArcher_BacksOff_WhenThePlayerComesTooClose_AndHoldsInItsBand()
        {
            Assert.AreEqual(-1, ArcherSteering.Intent(3f, true, 5f, 7.5f));
            Assert.AreEqual(0, ArcherSteering.Intent(5f - ArcherSteering.Band + 0.01f, true, 5f, 7.5f));
            Assert.AreEqual(0, ArcherSteering.Intent(7f, true, 5f, 7.5f));
        }

        // --- Projectiles ---

        // An open floor of cells 0..29 on both axes, with a wall line at x = 15 when asked for.
        static NavGrid Floor(bool wall)
        {
            var nav = new NavGrid(Vector2Int.zero, 30, 30);
            for (var x = 0; x < 30; x++)
                for (var y = 0; y < 30; y++)
                    nav.SetWalkable(new Vector2Int(x, y), !(wall && x == 15));
            return nav;
        }

        static Vector2 Ground(int x, int y) => IsoMath.CellToGround(new Vector2Int(x, y));

        static EnemyProjectileFlight.Outcome Fly(NavGrid nav, Vector2 from, Vector2 to, Vector2 player, float maxDistance, float deltaTime, out Vector2 end)
        {
            var position = from;
            var travelled = 0f;
            var velocity = (to - from).normalized * 9f;
            for (var frame = 0; frame < 1000; frame++)
            {
                var outcome = EnemyProjectileFlight.Step(ref position, ref travelled, velocity, deltaTime, maxDistance, nav, player, 0.4f);
                if (outcome != EnemyProjectileFlight.Outcome.Flying)
                {
                    end = position;
                    return outcome;
                }
            }
            end = position;
            return EnemyProjectileFlight.Outcome.Flying;
        }

        [Test]
        public void AnArrow_HitsThePlayerInItsPath()
        {
            var nav = Floor(false);
            var outcome = Fly(nav, Ground(5, 10), Ground(20, 10), Ground(12, 10), 20f, 1f / 60f, out _);
            Assert.AreEqual(EnemyProjectileFlight.Outcome.HitPlayer, outcome);
        }

        [Test]
        public void AWall_StopsAnArrow_EvenOnALongFrame()
        {
            var nav = Floor(true);
            // A 0.3 s frame at 9 units per second is 2.7 units in one step, far more than a wall is thick.
            var outcome = Fly(nav, Ground(5, 10), Ground(25, 10), Ground(22, 10), 30f, 0.3f, out var end);
            Assert.AreEqual(EnemyProjectileFlight.Outcome.Blocked, outcome);
            Assert.Less(IsoMath.GroundToCell(end).x, 15);
        }

        [Test]
        public void AnArrow_IsSpent_AfterItsRange()
        {
            var nav = Floor(false);
            var from = Ground(2, 2);
            var outcome = Fly(nav, from, Ground(28, 28), Ground(0, 29), 6f, 1f / 60f, out var end);
            Assert.AreEqual(EnemyProjectileFlight.Outcome.Spent, outcome);
            Assert.AreEqual(6f, Vector2.Distance(from, end), EnemyProjectileFlight.MaxStep);
        }

        // --- Pack composition (the concept roster, 2026-10-13) ---

        static float Share(int depth, PackMember member, PackKind kind = PackKind.Normal)
        {
            var with = 0;
            for (var pack = 0; pack < 2000; pack++)
                if (PackComposition.Roll(depth, kind, 8, 2468, pack).Contains(member))
                    with++;
            return with / 2000f;
        }

        [Test]
        public void TheFirstLevel_IsZombiesOnly()
        {
            for (var pack = 0; pack < 200; pack++)
                Assert.IsTrue(PackComposition.Roll(1, PackKind.Normal, 10, 12345, pack).All(m => m == PackMember.Zombie));
        }

        [Test]
        public void ElitePacks_AreOneKind()
        {
            for (var pack = 0; pack < 200; pack++)
            {
                var members = PackComposition.Roll(20, PackKind.Elite, 4, 777, pack);
                Assert.IsTrue(members.All(m => m == members[0] && m != PackMember.None));
            }
        }

        [Test]
        public void APack_IsTheSameEveryTimeTheLevelLoads()
        {
            for (var pack = 0; pack < 50; pack++)
                CollectionAssert.AreEqual(PackComposition.Roll(14, PackKind.Normal, 9, 31337, pack), PackComposition.Roll(14, PackKind.Normal, 9, 31337, pack));
        }

        [Test]
        public void EveryKind_IsMetInItsSpan_AndNeverOutsideIt()
        {
            foreach (var entry in PackComposition.Table)
            {
                Assert.Greater(Share(entry.First + 2, entry.Member), 0f, entry.Member + " in its span");
                if (entry.First > 1)
                    Assert.AreEqual(0f, Share(entry.First - 1, entry.Member), entry.Member + " before its first depth");
                if (entry.Last < DungeonRules.Depths)
                    Assert.AreEqual(0f, Share(entry.Last + 1, entry.Member), entry.Member + " after its last depth");
            }
        }

        [Test]
        public void EveryDepth_HasFrontFighters()
        {
            for (var depth = 1; depth <= DungeonRules.Depths; depth++)
                Assert.IsTrue(PackComposition.Has(PackRole.Front, depth), depth.ToString());
        }

        [Test]
        public void SwarmPacks_AreOneKindOfFourToSix()
        {
            var swarms = 0;
            for (var pack = 0; pack < 500; pack++)
            {
                var members = PackComposition.Roll(30, PackKind.Normal, 8, 99, pack);
                var kinds = members.Where(m => m != PackMember.None).Distinct().ToArray();
                if (kinds.Length == 1 && PackComposition.RoleOf(kinds[0]) == PackRole.Swarm)
                {
                    swarms++;
                    Assert.That(members.Count(m => m != PackMember.None), Is.InRange(PackComposition.MinSwarm, PackComposition.MaxSwarm));
                }
                else
                    Assert.IsFalse(members.Any(m => m != PackMember.None && PackComposition.RoleOf(m) == PackRole.Swarm), "swarms run alone");
            }
            Assert.That(swarms / 500f, Is.InRange(0.1f, 0.3f));
        }

        [Test]
        public void ASupport_StandsAtTheBack()
        {
            for (var pack = 0; pack < 500; pack++)
            {
                var members = PackComposition.Roll(20, PackKind.Normal, 6, 4321, pack);
                for (var i = 0; i < members.Length - 1; i++)
                    Assert.AreNotEqual(PackRole.Support, members[i] == PackMember.None ? PackRole.Front : PackComposition.RoleOf(members[i]), "only the last slot");
            }
        }

        [Test]
        public void AGuardPack_KeepsEverySlotFilled()
        {
            for (var pack = 0; pack < 200; pack++)
                Assert.IsTrue(PackComposition.Roll(10, PackKind.WithChampion, 3, 55, pack).All(m => m != PackMember.None));
        }

        [Test]
        public void ABossWaitsEverySixLevels()
        {
            foreach (var depth in new[] { 6, 12, 18, 24, 30, 36 })
                Assert.IsTrue(DungeonRules.IsBossDepth(depth), depth.ToString());
            foreach (var depth in new[] { 1, 5, 7, 11, 13, 23, 25, 35 })
                Assert.IsFalse(DungeonRules.IsBossDepth(depth), depth.ToString());
        }
    }
}
