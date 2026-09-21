using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Data for one enemy type. Ranges are in ground units. Values marked as tuning are starting points that
    /// Docs/01-core-gameplay.md does not fix yet.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Enemy Definition", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Tooltip("Sets life, damage and how much armor is worth through the formulas in Docs/03-itemization.md.")]
        [SerializeField, Min(1)] int level = 1;

        [Tooltip("Archetype adjustment on the level's base life. The docs call the Husk swarmer low health but give no number, so this is 1 until tuned.")]
        [SerializeField, Min(0.1f)] float lifeMultiplier = 1f;

        [Tooltip("Reduces the damage taken through the armor formula. The docs give no enemy armor values, so this is 0 until tuned.")]
        [SerializeField, Min(0f)] float armor;

        [Tooltip("Radius of the body on the ground, in ground units. A sweep hits when it reaches this far past the enemy's center. Tuning value.")]
        [SerializeField, Min(0.05f)] float bodyRadius = 0.3f;

        [Tooltip("Seconds the death animation takes before the enemy leaves the level. Tuning value.")]
        [SerializeField, Min(0f)] float deathSeconds = 0.25f;

        [Tooltip("The enemy starts an attack when the player is this close, in ground units. The docs give no swarmer attack numbers, so the attack values are tuning.")]
        [SerializeField, Min(0.1f)] float attackRange = 1f;

        [Tooltip("Seconds between starting an attack and it landing. The enemy swells during this time as a tell, and the player can step out of range. Tuning value.")]
        [SerializeField, Min(0f)] float attackWindupSeconds = 0.35f;

        [Tooltip("Seconds the enemy stands after an attack before it can attack again. Tuning value.")]
        [SerializeField, Min(0f)] float attackRecoverSeconds = 0.65f;

        [Tooltip("Archetype adjustment on the level's base hit damage from Docs/03-itemization.md. 1 until tuned.")]
        [SerializeField, Min(0f)] float damageMultiplier = 1f;

        [Tooltip("Ground units per second. Tuning value: swarmers are fast, but this stays under the player's 4 so they can be kited.")]
        [SerializeField, Min(0f)] float moveSpeed = 3.6f;

        [Tooltip("An idle enemy starts chasing when the player comes this close. Docs: 7 for normal enemies, 10 for elites.")]
        [SerializeField, Min(0f)] float aggroRange = 7f;

        [Tooltip("Beyond this distance the enemy starts losing track of the player. Docs: 20.")]
        [SerializeField, Min(0f)] float leashRange = 20f;

        [Tooltip("Seconds beyond the leash range before the enemy gives up. Docs: 4.")]
        [SerializeField, Min(0f)] float leashSeconds = 4f;

        [Tooltip("The enemy stops closing in at this distance from the player. Tuning value, becomes the melee reach later.")]
        [SerializeField, Min(0f)] float stopDistance = 0.7f;

        [Tooltip("Enemies closer than this push each other apart, which gives a swarm its loose cluster. Tuning value.")]
        [SerializeField, Min(0.1f)] float separationRadius = 0.8f;

        public int Level => level;
        public float MaxLife => CombatFormulas.EnemyLife(level) * lifeMultiplier;
        public float Armor => armor;
        public float BodyRadius => bodyRadius;
        public float DeathSeconds => deathSeconds;
        public float AttackRange => attackRange;
        public float AttackWindupSeconds => attackWindupSeconds;
        public float AttackRecoverSeconds => attackRecoverSeconds;
        public float DamageMultiplier => damageMultiplier;
        public float MoveSpeed => moveSpeed;
        public float AggroRange => aggroRange;
        public float LeashRange => leashRange;
        public float LeashSeconds => leashSeconds;
        public float StopDistance => stopDistance;
        public float SeparationRadius => separationRadius;
    }
}
