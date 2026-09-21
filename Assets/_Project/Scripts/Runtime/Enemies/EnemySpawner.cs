using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Keeps a fixed number of enemies alive around the player by spawning on a ring outside the screen.
    /// Enemies that lose the player return to the pool, and this refills the count, so the M0 stress test
    /// always has the target number chasing. Uses a seeded generator so a run can be replayed (Docs/07-technical.md).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        const int PlacementAttempts = 8;

        [Tooltip("Left empty, the first EnemyManager in the scene is used.")]
        [SerializeField] EnemyManager manager;

        [SerializeField] EnemyDefinition definition;

        [Tooltip("Enemies to keep alive. The M0 performance target is 40 on screen.")]
        [SerializeField, Min(0)] int targetCount = 40;

        [Tooltip("Spawn ring around the player, in ground units. The screen is 9 units wide, so 8 and up is off screen sideways.")]
        [SerializeField, Min(0f)] float minRadius = 8f;

        [SerializeField, Min(0f)] float maxRadius = 12f;

        [Tooltip("Caps spawns per frame so refilling the pack never causes a spike.")]
        [SerializeField, Min(1)] int spawnsPerFrame = 2;

        [Tooltip("Spawn already chasing. Off, enemies idle until the player comes within their aggro range.")]
        [SerializeField] bool spawnAggroed = true;

        [SerializeField] int seed = 20260921;

        System.Random random;

        void Awake()
        {
            random = new System.Random(seed);
            if (manager == null)
                manager = FindAnyObjectByType<EnemyManager>();
        }

        void Update()
        {
            if (manager == null || !manager.IsReady || definition == null)
                return;

            for (var spawned = 0; spawned < spawnsPerFrame && manager.ActiveCount < targetCount; spawned++)
                TrySpawn();
        }

        void TrySpawn()
        {
            var center = manager.PlayerGround;
            for (var attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
                var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (!manager.Nav.IsWalkable(IsoMath.GroundToCell(position)))
                    continue;

                manager.Spawn(definition, position, spawnAggroed);
                return;
            }
        }
    }
}
