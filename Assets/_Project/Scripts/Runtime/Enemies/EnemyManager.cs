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
    [DefaultExecutionOrder(-100)]
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
        readonly List<int> queryBuffer = new List<int>(64);

        // The enemies inserted into the spatial hash this frame, indexed by hash id.
        readonly List<EnemyController> hashed = new List<EnemyController>(64);

        FlowField flow;
        Vector2Int flowCell;
        float flowCooldown;
        bool flowValid;

        /// <summary>Raised when an enemy is killed, with the enemy. Loot listens to it.</summary>
        public event System.Action<EnemyController> Killed;

        public bool IsReady { get; private set; }

        /// <summary>
        /// How many living enemies are engaged with the player (approaching, winding up or recovering). Zero means the
        /// room is clear, which lets loot be picked up while nobody is attacking. Counted each frame.
        /// </summary>
        public int EngagedCount { get; private set; }

        public int ActiveCount => active.Count;

        public IReadOnlyList<EnemyController> Active => active;

        public NavGrid Nav { get; private set; }

        /// <summary>The player's life, or null when the scene has none. Enemies attack it.</summary>
        public PlayerHealth Player { get; private set; }

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
            Player = FindAnyObjectByType<PlayerHealth>();
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

            // Dead enemies are left out of the hash so they are neither targeted nor pushed against.
            Hash.Clear();
            hashed.Clear();
            for (var i = 0; i < active.Count; i++)
            {
                var enemy = active[i];
                if (!enemy.IsAlive)
                    continue;

                enemy.HashId = Hash.Insert(enemy.GroundPosition);
                hashed.Add(enemy);
            }

            // Backwards so a finished enemy can be swap-removed while looping.
            var engaged = 0;
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var enemy = active[i];
                if (enemy.IsAlive)
                {
                    enemy.Tick(deltaTime, this);
                    var state = enemy.State;
                    if (state == EnemyState.Approach || state == EnemyState.Attack || state == EnemyState.Recover)
                        engaged++;
                }
                else if (enemy.TickDeath(deltaTime))
                {
                    Despawn(i);
                }
            }
            EngagedCount = engaged;
        }

        internal void NotifyKilled(EnemyController enemy) => Killed?.Invoke(enemy);

        /// <summary>
        /// Fills <paramref name="results"/> with the living enemies whose centers are within radius of a ground
        /// position, as they stood at the start of this frame. Does not allocate.
        /// </summary>
        public void QueryEnemies(Vector2 center, float radius, List<EnemyController> results)
        {
            results.Clear();
            if (!IsReady)
                return;

            Hash.Query(center, radius, queryBuffer);
            for (var i = 0; i < queryBuffer.Count; i++)
            {
                var enemy = hashed[queryBuffer[i]];
                if (enemy.IsAlive)
                    results.Add(enemy);
            }
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

            enemy.Activate(definition, groundPosition, aggroed, pack, this);
            active.Add(enemy);
            return enemy;
        }

        void Despawn(int index)
        {
            var enemy = active[index];
            var last = active.Count - 1;
            active[index] = active[last];
            active.RemoveAt(last);

            enemy.Deactivate();
            pool.Push(enemy);
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
