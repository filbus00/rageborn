using UnityEngine;

namespace ARPG
{
    /// <summary>What auto-loot picks up besides gold, Legendaries and upgrades (always taken). Saved by number in the
    /// account's settings: add new ones at the end.</summary>
    public enum PickupRule
    {
        Everything,
        MagicAndBetter,
        RareAndBetter,
        UpgradesOnly,
    }

    /// <summary>
    /// The auto-loot rules from Docs/01-core-gameplay.md, as pure functions so they can be tested without a fight:
    /// gold is always picked up within 2.5 units; items are picked up within 2.5 units when the character has not
    /// been hit for 1.5 seconds, or when nothing is engaged with it any more.
    /// </summary>
    public static class AutoLootRules
    {
        public const float PickupRadius = 2.5f;
        public const float SafeSeconds = 1.5f;

        /// <summary>An item lies on the ground this long before auto-loot takes it, so its name can be read (the owner,
        /// 2026-10-02: "a slight delay, maybe a second or two"). Gold is taken at once.</summary>
        public const float ItemDelaySeconds = 1.5f;

        public static bool ItemReady(float secondsOnGround) => secondsOnGround >= ItemDelaySeconds;

        /// <summary>
        /// Whether auto-loot takes an item under the player's pick-up rule (the owner, 2026-10-02: a simple loot filter,
        /// now that gear takes space in the backpack grid). A Legendary and an upgrade (by the power score) are always
        /// taken; otherwise the rule's rarity floor decides. An item left on the ground can still be walked over later
        /// with another rule.
        /// </summary>
        public static bool Wants(PickupRule rule, ItemRarity rarity, bool isUpgrade)
        {
            if (rarity == ItemRarity.Legendary || isUpgrade)
                return true;
            switch (rule)
            {
                case PickupRule.Everything: return true;
                case PickupRule.MagicAndBetter: return rarity >= ItemRarity.Magic;
                case PickupRule.RareAndBetter: return rarity >= ItemRarity.Rare;
                default: return false; // UpgradesOnly
            }
        }

        /// <summary>The rule's name in the Settings sheet.</summary>
        public static string Describe(PickupRule rule)
        {
            switch (rule)
            {
                case PickupRule.Everything: return "Everything";
                case PickupRule.MagicAndBetter: return "Magic and better";
                case PickupRule.RareAndBetter: return "Rare and better";
                default: return "Upgrades only";
            }
        }

        public static bool InRange(Vector2 character, Vector2 drop) => Vector2.Distance(character, drop) <= PickupRadius;

        /// <summary>Whether items (not gold) may be picked up right now.</summary>
        /// <param name="secondsSinceLastHit">Seconds since the character last took a hit.</param>
        /// <param name="engagedEnemies">Living enemies that are approaching, attacking or recovering.</param>
        public static bool CanPickUpItems(float secondsSinceLastHit, int engagedEnemies) =>
            secondsSinceLastHit >= SafeSeconds || engagedEnemies == 0;
    }
}
