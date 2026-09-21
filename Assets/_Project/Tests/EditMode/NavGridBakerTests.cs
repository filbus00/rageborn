using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG.Tests
{
    public class NavGridBakerTests
    {
        GameObject root;
        Tile tile;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Test Grid", typeof(Grid));
            tile = ScriptableObject.CreateInstance<Tile>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        Tilemap NewTilemap(string name)
        {
            var go = new GameObject(name, typeof(Tilemap));
            go.transform.SetParent(root.transform, false);
            return go.GetComponent<Tilemap>();
        }

        void Fill(Tilemap map, int minX, int minY, int maxX, int maxY)
        {
            for (var y = minY; y <= maxY; y++)
                for (var x = minX; x <= maxX; x++)
                    map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        [Test]
        public void GroundCells_AreWalkable_AndCellsWithoutGround_AreNot()
        {
            var ground = NewTilemap("Ground");
            Fill(ground, 0, 0, 3, 3);

            var grid = NavGridBaker.Bake(ground, System.Array.Empty<Tilemap>());

            Assert.IsTrue(grid.IsWalkable(new Vector2Int(0, 0)));
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(3, 3)));
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(4, 0)), "beyond the ground");
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(-1, 0)), "beyond the ground");
        }

        [Test]
        public void GridBounds_CoverEveryGroundTile()
        {
            // Tilemap.cellBounds always includes the origin, so the grid may be larger than the tiles, never smaller.
            var ground = NewTilemap("Ground");
            Fill(ground, -2, 1, 4, 5);

            var grid = NavGridBaker.Bake(ground, System.Array.Empty<Tilemap>());

            for (var y = 1; y <= 5; y++)
                for (var x = -2; x <= 4; x++)
                    Assert.IsTrue(grid.IsWalkable(new Vector2Int(x, y)), $"cell ({x}, {y})");
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(5, 3)), "beside the ground");
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(0, 0)), "the origin has no tile here, though the bounds include it");
        }

        [Test]
        public void ObstacleTiles_BlockOnlyTheirOwnCells()
        {
            var ground = NewTilemap("Ground");
            var walls = NewTilemap("Walls");
            Fill(ground, 0, 0, 4, 4);
            walls.SetTile(new Vector3Int(2, 2, 0), tile);
            walls.SetTile(new Vector3Int(3, 2, 0), tile);

            var grid = NavGridBaker.Bake(ground, new[] { walls });

            Assert.IsFalse(grid.IsWalkable(new Vector2Int(2, 2)));
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(3, 2)));
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(1, 2)));
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(2, 3)));
        }

        [Test]
        public void SeveralObstacleTilemaps_AllBlock()
        {
            var ground = NewTilemap("Ground");
            var wallsA = NewTilemap("Walls A");
            var wallsB = NewTilemap("Walls B");
            Fill(ground, 0, 0, 3, 3);
            wallsA.SetTile(new Vector3Int(1, 1, 0), tile);
            wallsB.SetTile(new Vector3Int(2, 2, 0), tile);

            var grid = NavGridBaker.Bake(ground, new[] { wallsA, wallsB });

            Assert.IsFalse(grid.IsWalkable(new Vector2Int(1, 1)));
            Assert.IsFalse(grid.IsWalkable(new Vector2Int(2, 2)));
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(0, 0)));
        }

        [Test]
        public void ObstacleTilesOffTheGround_AreIgnored()
        {
            var ground = NewTilemap("Ground");
            var walls = NewTilemap("Walls");
            Fill(ground, 0, 0, 2, 2);
            walls.SetTile(new Vector3Int(20, 20, 0), tile);

            var grid = NavGridBaker.Bake(ground, new[] { walls });

            Assert.AreEqual(3, grid.Width);
            Assert.IsTrue(grid.IsWalkable(new Vector2Int(1, 1)));
        }
    }
}
