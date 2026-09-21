using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Rolls what an enemy drops when it dies and puts it on the ground. Gold drops from every kill and an item with
    /// the chance from Docs/03-itemization.md. Item level equals the level of the enemy, which is the zone level.
    /// Drop objects come from a pool made at load. Drops left on the ground are lost when the scene changes;
    /// keeping them is a later step.
    /// </summary>
    public class LootDirector : MonoBehaviour
    {
        [Tooltip("Left empty, the first EnemyManager in the scene is used.")]
        [SerializeField] EnemyManager enemies;

        [SerializeField] Sprite diamondSprite;
        [SerializeField] Sprite coinSprite;
        [SerializeField] Sprite beamSprite;

        [Tooltip("Drop objects made up front. A big pack dropping at once should not need more.")]
        [SerializeField, Min(1)] int poolSize = 24;

        [Tooltip("Magic Find as a fraction, 0.5 for 50 percent. There is no gear that gives it yet, so it is fixed here.")]
        [SerializeField, Min(0f)] float magicFind;

        // Drops from one kill are spread out a little so a coin and an item do not sit on the same spot.
        const float ScatterRadius = 0.35f;
        const float GoldenAngle = 2.3999632f;

        readonly Stack<LootDrop> pool = new Stack<LootDrop>();
        readonly List<LootDrop> active = new List<LootDrop>();

        int scatterIndex;

        /// <summary>Everything currently lying on the ground.</summary>
        public IReadOnlyList<LootDrop> Active => active;

        /// <summary>How many drops have appeared since the scene started. For tests and tuning.</summary>
        public int GoldDropCount { get; private set; }

        public int ItemDropCount { get; private set; }

        void Awake()
        {
            for (var i = 0; i < poolSize; i++)
                pool.Push(CreateDrop());
        }

        void Start()
        {
            if (enemies == null)
                enemies = FindAnyObjectByType<EnemyManager>();
            if (enemies != null)
                enemies.Killed += OnKilled;
        }

        void OnDestroy()
        {
            if (enemies != null)
                enemies.Killed -= OnKilled;
        }

        /// <summary>Takes a drop off the ground, after it was picked up, and returns it to the pool.</summary>
        public void Release(LootDrop drop)
        {
            if (!active.Remove(drop))
                return;

            drop.Hide();
            pool.Push(drop);
        }

        void OnKilled(EnemyController enemy)
        {
            var loot = GameSession.Current.Loot;
            var level = enemy.Definition.Level;
            var at = enemy.GroundPosition;

            DropGold(loot.RollGold(LootSource.NormalEnemy, level), at);

            var item = loot.RollDrop(LootSource.NormalEnemy, level, magicFind);
            if (item != null)
                DropItem(item, at + Scatter());
        }

        /// <summary>Puts gold on the ground at a ground position. Chests, elites and bosses will use this too.</summary>
        public void DropGold(int amount, Vector2 ground)
        {
            var drop = TakeDrop();
            drop.ShowGold(amount, ground);
            active.Add(drop);
            GoldDropCount++;
        }

        /// <summary>Puts an item on the ground at a ground position, with its rarity beam.</summary>
        public void DropItem(Item item, Vector2 ground)
        {
            var drop = TakeDrop();
            drop.ShowItem(item, ground);
            active.Add(drop);
            ItemDropCount++;
        }

        LootDrop TakeDrop()
        {
            if (pool.Count > 0)
                return pool.Pop();

            Debug.LogWarning("[ARPG] Loot pool exhausted; creating a drop. Raise the pool size.", this);
            return CreateDrop();
        }

        LootDrop CreateDrop()
        {
            var go = new GameObject("Loot Drop", typeof(LootDrop));
            go.transform.SetParent(transform, false);

            var drop = go.GetComponent<LootDrop>();
            drop.Build(diamondSprite, coinSprite, beamSprite);
            return drop;
        }

        Vector2 Scatter()
        {
            var angle = scatterIndex++ * GoldenAngle;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ScatterRadius;
        }
    }
}
