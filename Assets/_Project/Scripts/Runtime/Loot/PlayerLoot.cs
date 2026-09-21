using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Auto-loot from Docs/01-core-gameplay.md: there is nothing to tap. Gold is always picked up within 2.5 units.
    /// Items are picked up within 2.5 units when the character has not been hit for 1.5 seconds, or when nobody is
    /// engaged with it any more (the room is clear). An item stays on the ground when the backpack is full.
    /// </summary>
    public class PlayerLoot : MonoBehaviour
    {
        [Tooltip("Left empty, the first LootDirector in the scene is used.")]
        [SerializeField] LootDirector loot;

        [Tooltip("Left empty, the first EnemyManager in the scene is used.")]
        [SerializeField] EnemyManager enemies;

        PlayerController player;
        PlayerHealth health;

        /// <summary>Totals picked up since the scene started. For tests and tuning.</summary>
        public int ItemsPickedUp { get; private set; }

        public int GoldPickedUp { get; private set; }

        void Awake()
        {
            if (loot == null)
                loot = FindAnyObjectByType<LootDirector>();
            if (enemies == null)
                enemies = FindAnyObjectByType<EnemyManager>();
            player = FindAnyObjectByType<PlayerController>();
            health = FindAnyObjectByType<PlayerHealth>();
        }

        void Update()
        {
            if (loot == null || player == null || (health != null && !health.IsAlive))
                return;

            var drops = loot.Active;
            if (drops.Count == 0)
                return;

            var origin = IsoMath.WorldToGround(player.transform.position);
            var quiet = AutoLootRules.CanPickUpItems(
                health != null ? health.SecondsSinceLastHit : float.MaxValue,
                enemies != null ? enemies.EngagedCount : 0);
            var session = GameSession.Current;

            // Backwards, because picking a drop up removes it from the list.
            for (var i = drops.Count - 1; i >= 0; i--)
            {
                var drop = drops[i];
                if (!AutoLootRules.InRange(origin, drop.GroundPosition))
                    continue;

                if (drop.IsGold)
                {
                    session.AddGold(drop.GoldAmount);
                    GoldPickedUp += drop.GoldAmount;
                    loot.Release(drop);
                }
                else if (quiet && session.PickUp(drop.Item))
                {
                    ItemsPickedUp++;
                    loot.Release(drop);
                }
            }
        }
    }
}
