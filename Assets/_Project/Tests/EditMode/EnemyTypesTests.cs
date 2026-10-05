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

        // --- Pack composition ---

        [Test]
        public void TheFirstLevel_IsHusksOnly()
        {
            for (var pack = 0; pack < 200; pack++)
                Assert.IsTrue(PackComposition.Roll(1, PackKind.Normal, 10, 12345, pack).All(m => m == PackMember.Husk));
        }

        [Test]
        public void ElitePacks_StayHusks()
        {
            for (var pack = 0; pack < 200; pack++)
                Assert.IsTrue(PackComposition.Roll(5, PackKind.Elite, 4, 777, pack).All(m => m == PackMember.Husk));
        }

        [Test]
        public void APack_IsTheSameEveryTimeTheLevelLoads()
        {
            for (var pack = 0; pack < 50; pack++)
                CollectionAssert.AreEqual(PackComposition.Roll(4, PackKind.Normal, 9, 31337, pack), PackComposition.Roll(4, PackKind.Normal, 9, 31337, pack));
        }

        [Test]
        public void DeeperLevels_MixInGhoulsAndArchers_AtRoughlyTheDesignedShares()
        {
            int husksOnly = 0, withGhouls = 0, withArchers = 0, bands = 0;
            const int packs = 4000;
            for (var pack = 0; pack < packs; pack++)
            {
                // Depth 2: the mix alone, before the rest of act 1's roster joins at depth 3.
                var members = PackComposition.Roll(2, PackKind.Normal, 10, 2024, pack);
                var ghouls = members.Count(m => m == PackMember.Ghoul);
                var archers = members.Count(m => m == PackMember.Archer);
                var empty = members.Count(m => m == PackMember.None);

                if (ghouls == 0 && archers == 0)
                    husksOnly++;
                else if (empty > 0)
                {
                    bands++;
                    Assert.AreEqual(3, ghouls);
                    Assert.AreEqual(2, archers);
                    Assert.AreEqual(0, members.Count(m => m == PackMember.Husk));
                }
                else if (ghouls > 0)
                {
                    withGhouls++;
                    Assert.That(ghouls, Is.InRange(2, 3));
                    Assert.AreEqual(0, archers);
                    // Ghouls take the last slots, on the pack's outside.
                    Assert.IsTrue(members.Skip(10 - ghouls).All(m => m == PackMember.Ghoul));
                }
                else
                {
                    withArchers++;
                    Assert.That(archers, Is.InRange(2, 3));
                    Assert.IsTrue(members.Skip(10 - archers).All(m => m == PackMember.Archer));
                }
            }

            Assert.AreEqual(0.40f, husksOnly / (float)packs, 0.04f);
            Assert.AreEqual(0.25f, withGhouls / (float)packs, 0.04f);
            Assert.AreEqual(0.20f, withArchers / (float)packs, 0.04f);
            Assert.AreEqual(0.15f, bands / (float)packs, 0.04f);
        }

        [Test]
        public void Depth4_TurnsSomePacksUndead_AtRoughlyTheDesignedShares()
        {
            int withSkeletons = 0, withArchery = 0, withCultists = 0, packs = 0;
            for (var pack = 0; pack < 4000; pack++)
            {
                var members = PackComposition.Roll(5, PackKind.Normal, 10, 2024, pack);
                // Wolf packs (the rest of act 1's roster) replace a pack whole; the undead shares are of the others.
                if (members.Contains(PackMember.AshWolf))
                    continue;
                packs++;
                // Skeletons replace all the husks of a pack or none; cultists all the archers or none.
                var skeletons = members.Count(m => m == PackMember.Skeleton);
                Assert.IsTrue(skeletons == 0 || members.All(m => m != PackMember.Husk));
                if (skeletons > 0)
                    withSkeletons++;
                // Ember acolytes take the archers a pack kept after the cultists' roll.
                var archers = members.Count(m => m == PackMember.Archer || m == PackMember.EmberAcolyte);
                var cultists = members.Count(m => m == PackMember.Cultist);
                Assert.IsTrue(archers == 0 || cultists == 0);
                if (archers + cultists > 0)
                    withArchery++;
                if (cultists > 0)
                    withCultists++;
            }
            // Skeletons need husks (85 percent of packs have some), so about 35 percent of those.
            Assert.AreEqual(0.35f * 0.85f, withSkeletons / (float)packs, 0.04f);
            Assert.AreEqual(0.5f, withCultists / (float)withArchery, 0.06f);
        }

        [Test]
        public void Depth3_HasNoUndead_AndElitePacksNeverDo()
        {
            for (var pack = 0; pack < 500; pack++)
            {
                Assert.IsFalse(PackComposition.Roll(3, PackKind.Normal, 10, 7, pack).Any(m => m == PackMember.Skeleton || m == PackMember.Cultist));
                Assert.IsTrue(PackComposition.Roll(6, PackKind.Elite, 6, 7, pack).All(m => m == PackMember.Husk));
            }
        }

        [Test]
        public void AChampionPack_KeepsAHuskInSlotZero_ForTheLeader()
        {
            for (var pack = 0; pack < 500; pack++)
                Assert.AreEqual(PackMember.Husk, PackComposition.Roll(6, PackKind.WithChampion, 8, 99, pack)[0]);
        }

        [Test]
        public void ASmallGuardPack_KeepsEverySlotFilled()
        {
            // A treasure guard can be 3 strong; a band of 3 ghouls and 2 archers must not leave it with empty slots.
            for (var pack = 0; pack < 500; pack++)
            {
                var members = PackComposition.Roll(2, PackKind.Normal, 3, 5, pack);
                Assert.AreEqual(3, members.Length);
                Assert.IsFalse(members.Contains(PackMember.None));
            }
        }

        [Test]
        public void TheRestOfAct1_JoinsFromItsDepths()
        {
            bool Has(int depth, PackMember member)
            {
                for (var pack = 0; pack < 2000; pack++)
                    if (PackComposition.Roll(depth, PackKind.Normal, 8, 4242, pack).Contains(member))
                        return true;
                return false;
            }
            Assert.IsFalse(Has(2, PackMember.AshWolf));
            Assert.IsFalse(Has(2, PackMember.Cutthroat));
            Assert.IsTrue(Has(3, PackMember.AshWolf));
            Assert.IsTrue(Has(3, PackMember.Cutthroat));
            Assert.IsFalse(Has(3, PackMember.EmberAcolyte));
            Assert.IsFalse(Has(3, PackMember.PyreKeeper));
            Assert.IsTrue(Has(4, PackMember.EmberAcolyte));
            Assert.IsTrue(Has(4, PackMember.PyreKeeper));
            Assert.IsFalse(Has(4, PackMember.CarrionBloat));
            Assert.IsTrue(Has(5, PackMember.CarrionBloat));
        }

        [Test]
        public void WolfPacks_AreFourToSixWolvesAndNobodyElse()
        {
            var wolfPacks = 0;
            for (var pack = 0; pack < 2000; pack++)
            {
                var members = PackComposition.Roll(4, PackKind.Normal, 9, 99, pack);
                if (!members.Contains(PackMember.AshWolf))
                    continue;
                wolfPacks++;
                Assert.That(members.Count(m => m == PackMember.AshWolf), Is.InRange(PackComposition.MinWolves, PackComposition.MaxWolves));
                Assert.IsTrue(members.All(m => m == PackMember.AshWolf || m == PackMember.None));
            }
            Assert.AreEqual(PackComposition.WolfPackPercent / 100f, wolfPacks / 2000f, 0.04f);
            // Champion and elite packs never become wolves.
            for (var pack = 0; pack < 500; pack++)
            {
                Assert.IsFalse(PackComposition.Roll(5, PackKind.WithChampion, 8, 99, pack).Contains(PackMember.AshWolf));
                Assert.IsFalse(PackComposition.Roll(5, PackKind.Elite, 6, 99, pack).Contains(PackMember.AshWolf));
            }
        }

        [Test]
        public void APyreKeeper_StandsAtTheBack()
        {
            for (var pack = 0; pack < 2000; pack++)
            {
                var members = PackComposition.Roll(5, PackKind.Normal, 8, 7, pack);
                var keeper = System.Array.IndexOf(members, PackMember.PyreKeeper);
                if (keeper < 0)
                    continue;
                Assert.AreEqual(1, members.Count(m => m == PackMember.PyreKeeper));
                for (var i = keeper + 1; i < members.Length; i++)
                    Assert.AreEqual(PackMember.None, members[i]);
            }
        }
    }
}
