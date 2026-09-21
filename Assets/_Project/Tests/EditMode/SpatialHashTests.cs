using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class SpatialHashTests
    {
        [Test]
        public void Query_MatchesBruteForce()
        {
            var random = new System.Random(7);
            var hash = new SpatialHash(new Vector2(-20f, -20f), new Vector2(20f, 20f), 2f, 16);
            var points = new List<Vector2>();
            for (var i = 0; i < 300; i++)
            {
                var point = new Vector2((float)random.NextDouble() * 40f - 20f, (float)random.NextDouble() * 40f - 20f);
                points.Add(point);
                Assert.AreEqual(i, hash.Insert(point));
            }

            var results = new List<int>();
            for (var q = 0; q < 50; q++)
            {
                var center = new Vector2((float)random.NextDouble() * 40f - 20f, (float)random.NextDouble() * 40f - 20f);
                var radius = 0.5f + (float)random.NextDouble() * 6f;

                hash.Query(center, radius, results);

                var expected = new List<int>();
                for (var i = 0; i < points.Count; i++)
                    if ((points[i] - center).sqrMagnitude <= radius * radius)
                        expected.Add(i);
                results.Sort();
                CollectionAssert.AreEqual(expected, results, $"query {q}");
            }
        }

        [Test]
        public void Clear_RemovesEveryPoint()
        {
            var hash = new SpatialHash(Vector2.zero, new Vector2(10f, 10f), 2f, 4);
            hash.Insert(new Vector2(5f, 5f));
            var results = new List<int>();

            hash.Clear();
            hash.Query(new Vector2(5f, 5f), 3f, results);

            Assert.AreEqual(0, hash.Count);
            Assert.IsEmpty(results);
        }

        [Test]
        public void PointsOutsideTheBounds_AreStillFoundByNearbyQueries()
        {
            var hash = new SpatialHash(Vector2.zero, new Vector2(10f, 10f), 2f, 4);
            var outside = hash.Insert(new Vector2(-30f, 50f));
            var results = new List<int>();

            hash.Query(new Vector2(-29f, 49f), 3f, results);

            CollectionAssert.AreEqual(new[] { outside }, results);
        }

        [Test]
        public void Query_ExcludesPointsBeyondTheRadius_EvenInTheSameCell()
        {
            var hash = new SpatialHash(Vector2.zero, new Vector2(10f, 10f), 4f, 4);
            hash.Insert(new Vector2(1f, 1f));
            var far = hash.Insert(new Vector2(3.9f, 3.9f));
            var results = new List<int>();

            hash.Query(new Vector2(1f, 1f), 1f, results);

            CollectionAssert.DoesNotContain(results, far);
            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void Insert_GrowsPastTheInitialCapacity()
        {
            var hash = new SpatialHash(Vector2.zero, new Vector2(10f, 10f), 2f, 2);

            for (var i = 0; i < 50; i++)
                hash.Insert(new Vector2(i % 10, i / 10f));

            var results = new List<int>();
            hash.Query(new Vector2(5f, 2.5f), 100f, results);
            Assert.AreEqual(50, results.Count);
            Assert.AreEqual(new Vector2(3f, 0.3f), hash.PointAt(3));
        }
    }
}
