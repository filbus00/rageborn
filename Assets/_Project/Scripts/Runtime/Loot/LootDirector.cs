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
            var level = enemy.Level;
            var at = enemy.GroundPosition;
            var source = ToLootSource(enemy.Definition.Rank);

            DropGold(loot.RollGold(source, level), at);

            var items = loot.RollDrops(source, level, magicFind);
            for (var i = 0; i < items.Count; i++)
                DropItem(items[i], at + Scatter());

            GrantMaterials(source, at);

            // Docs/06: the first Legendary is guaranteed at minute 20 of play, from an elite.
            var onboarding = GameSession.Current.Onboarding;
            if (source == LootSource.Elite && onboarding.LegendaryDue)
            {
                onboarding.MarkGuaranteeDropped();
                DropItem(loot.RollItem(ItemRarity.Legendary, level), at + Scatter());
            }
        }

        /// <summary>Docs/04: Bloodstone also comes from elites and Soulglass from bosses. Materials are always picked up
        /// (Docs/01), so they go straight to the character, with a callout where the enemy fell.</summary>
        static void GrantMaterials(LootSource source, Vector2 at)
        {
            CraftingMaterial material;
            int amount;
            if (source == LootSource.Elite)
                (material, amount) = (CraftingMaterial.Bloodstone, ForgeRules.EliteBloodstone);
            else if (source == LootSource.Boss)
                (material, amount) = (CraftingMaterial.Soulglass, ForgeRules.BossSoulglass);
            else
                return;

            GameSession.Current.AddMaterial(material, amount);
            var world = IsoMath.GroundToWorld(at);
            DamageNumbers.Current?.ShowText(new Vector3(world.x, world.y + 1.2f, 0f), $"+{amount} {material}", MaterialCalloutColor, 40);
        }

        static readonly Color MaterialCalloutColor = new Color(0.85f, 0.55f, 1f);

        static LootSource ToLootSource(EnemyRank rank)
        {
            switch (rank)
            {
                case EnemyRank.Champion: return LootSource.Champion;
                case EnemyRank.Elite: return LootSource.Elite;
                case EnemyRank.Boss: return LootSource.Boss;
                default: return LootSource.NormalEnemy;
            }
        }

        /// <summary>Rolls a zone chest's loot (Docs/03-itemization.md: 1 to 3 items, Magic or better, and gold) at an
        /// item level and scatters it around the chest.</summary>
        public void DropChest(int itemLevel, Vector2 ground)
        {
            var loot = GameSession.Current.Loot;
            DropGold(loot.RollGold(LootSource.ZoneChest, itemLevel), ground + Scatter());

            var items = loot.RollDrops(LootSource.ZoneChest, itemLevel, magicFind);
            for (var i = 0; i < items.Count; i++)
                DropItem(items[i], ground + Scatter());
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
