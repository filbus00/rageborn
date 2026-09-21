using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>
    /// Owns the enemy pool and runs every active enemy once per frame. It also owns what enemies share: the
    /// navigation grid, the flow field toward the player and the spatial hash of enemy positions.
    /// Logic runs in Update for now; combat will move it to a fixed timestep (Docs/07-technical.md).
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] EnemyController prefab;

        [Tooltip("Instances created up front. Spawning never allocates while there is one free in the pool.")]
        [SerializeField, Min(1)] int poolSize = 64;

        [Tooltip("Left empty, the first Tilemap that is not on the Obstacle layer is used.")]
        [SerializeField] Tilemap groundTilemap;

        [Tooltip("Tilemaps whose tiles block movement. Left empty, every Tilemap on the Obstacle layer is used.")]
        [SerializeField] Tilemap[] obstacleTilemaps;

        [Tooltip("Left empty, the first PlayerController in the scene is used.")]
        [SerializeField] PlayerController player;

        [Tooltip("Side of a spatial hash cell in ground units. Around twice the largest query radius.")]
        [SerializeField, Min(0.5f)] float hashCellSize = 2f;

        [Tooltip("The flow field is rebuilt when the player changes cell, but no more often than this.")]
        [SerializeField, Min(0.02f)] float flowRefreshSeconds = 0.15f;

        [Tooltip("Cells farther than this from the player along the lattice get no flow direction. Above the leash range of 20.")]
        [SerializeField, Min(1f)] float flowRange = 24f;

        readonly List<EnemyController> active = new List<EnemyController>();
        readonly Stack<EnemyController> pool = new Stack<EnemyController>();
        readonly List<int> neighbourBuffer = new List<int>(32);

        FlowField flow;
        Vector2Int flowCell;
        float flowCooldown;
        bool flowValid;

        public bool IsReady { get; private set; }

        public int ActiveCount => active.Count;

        public IReadOnlyList<EnemyController> Active => active;

        public NavGrid Nav { get; private set; }

        /// <summary>The player's position on the ground plane this frame.</summary>
        public Vector2 PlayerGround { get; private set; }

        internal SpatialHash Hash { get; private set; }

        /// <summary>Scratch list for spatial hash queries. Single threaded, so one shared list is enough.</summary>
        internal List<int> NeighbourBuffer => neighbourBuffer;

        void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError("[ARPG] EnemyManager has no enemy prefab.", this);
                return;
            }

            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            FindTilemaps();
            if (groundTilemap == null || player == null)
            {
                Debug.LogError("[ARPG] EnemyManager needs a ground Tilemap and a PlayerController in the scene.", this);
                return;
            }

            Nav = NavGridBaker.Bake(groundTilemap, obstacleTilemaps);
            flow = new FlowField(Nav);
            Hash = CreateHash(Nav);

            for (var i = 0; i < poolSize; i++)
                pool.Push(CreateInstance());

            IsReady = true;
        }

        void Update()
        {
            if (!IsReady)
                return;

            var deltaTime = Time.deltaTime;
            PlayerGround = IsoMath.WorldToGround(player.transform.position);
            RefreshFlow(deltaTime);

            Hash.Clear();
            for (var i = 0; i < active.Count; i++)
                active[i].HashId = Hash.Insert(active[i].GroundPosition);

            for (var i = 0; i < active.Count; i++)
                active[i].Tick(deltaTime, this);
        }

        /// <summary>Activates a pooled enemy at a ground position, which becomes its home spot.</summary>
        /// <param name="pack">The pack the enemy belongs to, or null.</param>
        public EnemyController Spawn(EnemyDefinition definition, Vector2 groundPosition, bool aggroed, EnemyPack pack = null)
        {
            EnemyController enemy;
            if (pool.Count > 0)
            {
                enemy = pool.Pop();
            }
            else
            {
                Debug.LogWarning("[ARPG] Enemy pool exhausted; instantiating one. Raise the pool size.", this);
                enemy = CreateInstance();
            }

            enemy.Activate(definition, groundPosition, aggroed, pack);
            active.Add(enemy);
            return enemy;
        }

        /// <summary>The unit ground direction an enemy at <paramref name="from"/> should walk to reach the target.</summary>
        internal Vector2 ChaseDirection(Vector2 from, Vector2 target)
        {
            // With a clear line the straight route is best; the lattice route would zig-zag across open ground.
            if (!Nav.HasLineOfSight(from, target) && flow.TryGetDirection(from, out var direction))
                return direction;

            return (target - from).normalized;
        }

        void FindTilemaps()
        {
            if (groundTilemap != null && obstacleTilemaps != null && obstacleTilemaps.Length > 0)
                return;

            var obstacleLayer = LayerMask.NameToLayer(GameLayers.Obstacle);
            var found = new List<Tilemap>();
            foreach (var tilemap in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
            {
                if (obstacleLayer >= 0 && tilemap.gameObject.layer == obstacleLayer)
                    found.Add(tilemap);
                else if (groundTilemap == null)
                    groundTilemap = tilemap;
            }

            if (obstacleTilemaps == null || obstacleTilemaps.Length == 0)
                obstacleTilemaps = found.ToArray();
        }

        void RefreshFlow(float deltaTime)
        {
            flowCooldown -= deltaTime;

            var cell = IsoMath.GroundToCell(PlayerGround);
            if (flowValid && (cell == flowCell || flowCooldown > 0f))
                return;

            flow.Compute(cell, flowRange);
            flowCell = cell;
            flowCooldown = flowRefreshSeconds;
            flowValid = true;
        }

        EnemyController CreateInstance()
        {
            var enemy = Instantiate(prefab, transform);
            enemy.gameObject.SetActive(false);
            return enemy;
        }

        SpatialHash CreateHash(NavGrid grid)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            // The ground-space bounds of the grid are set by its four corner cells.
            for (var i = 0; i < 4; i++)
            {
                var cell = new Vector2Int(
                    grid.Min.x + (i % 2 == 0 ? 0 : grid.Width - 1),
                    grid.Min.y + (i < 2 ? 0 : grid.Height - 1));
                var ground = IsoMath.CellToGround(cell);
                min = Vector2.Min(min, ground);
                max = Vector2.Max(max, ground);
            }

            return new SpatialHash(min - Vector2.one, max + Vector2.one, hashCellSize, poolSize);
        }
    }
}
