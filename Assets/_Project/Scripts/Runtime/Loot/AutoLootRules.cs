using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The auto-loot rules from Docs/01-core-gameplay.md, as pure functions so they can be tested without a fight:
    /// gold is always picked up within 2.5 units; items are picked up within 2.5 units when the character has not
    /// been hit for 1.5 seconds, or when nothing is engaged with it any more.
    /// </summary>
    public static class AutoLootRules
    {
        public const float PickupRadius = 2.5f;
        public const float SafeSeconds = 1.5f;

        public static bool InRange(Vector2 character, Vector2 drop) => Vector2.Distance(character, drop) <= PickupRadius;

        /// <summary>Whether items (not gold) may be picked up right now.</summary>
        /// <param name="secondsSinceLastHit">Seconds since the character last took a hit.</param>
        /// <param name="engagedEnemies">Living enemies that are approaching, attacking or recovering.</param>
        public static bool CanPickUpItems(float secondsSinceLastHit, int engagedEnemies) =>
            secondsSinceLastHit >= SafeSeconds || engagedEnemies == 0;
    }
}
