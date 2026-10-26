using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A pack of enemies placed by hand or by the level generator (Docs/01-core-gameplay.md: 3 to 12 per pack).
    /// Its members appear around the pack's position when the level loads, idle until the player comes within
    /// aggro range, and walk back home when the leash breaks. Nothing spawns around the player.
    /// </summary>
    public class EnemyPack : MonoBehaviour
    {
        /// <summary>The affixes the pack's elites share (2026-10-08), rolled for its first elite.</summary>
        public EliteModifiers? EliteModifiers { get; set; }

        // Ground-space distance covered by the home flow field. Larger than any level.
        const float HomeFieldRange = 200f;

        const float GoldenAngle = 2.3999632f;

        [SerializeField] EnemyDefinition definition;

        [Tooltip("Left empty, every member uses definition. Set to give the pack a single Champion leader (Docs/03-itemization.md, its own loot table) in slot 0; the rest of the pack still uses definition. For a pack that is entirely Elite, set definition itself to an Elite EnemyDefinition instead.")]
        [SerializeField] EnemyDefinition championDefinition;

        [Tooltip("Docs: pack sizes range from 3 to 12 for normal packs.")]
        [SerializeField, Range(3, 12)] int count = 8;

        [Tooltip("Members spread over a disc of this radius, in ground units. About 2.2 keeps 12 members clear of each other.")]
        [SerializeField, Min(0f)] float radius = 2.2f;

        [Tooltip("Left empty, the first EnemyManager in the scene is used.")]
        [SerializeField] EnemyManager manager;

        [Tooltip("The members' level. 0 uses each definition's own level.")]
        [SerializeField, Min(0)] int level;

        readonly List<EnemyController> members = new List<EnemyController>();
        // A balcony's spots (2026-10-20): the pack's archers and casters stand there, in this order.
        List<Vector2> perches;

        /// <summary>Gives the pack a balcony: its ranged members stand on these ground spots instead of in the disc, and
        /// keep them (<see cref="EnemyController.Perched"/>).</summary>
        public void Perch(List<Vector2> spots) => perches = spots;

        // Set from code for a mixed pack: one definition per slot, null for a slot left empty. Overrides definition,
        // championDefinition and count.
        EnemyDefinition[] slotDefinitions;

        Vector2 anchor;
        // Wandering (2026-10-26, WanderRules): where the pack was placed, where its middle drifts to, how long it stands,
        // and each member's place relative to the middle.
        Vector2 origin;
        Vector2 wanderTarget;
        float wanderPause;
        readonly List<Vector2> places = new List<Vector2>();

        /// <summary>How far the pack's middle drifts from where it was placed, in ground units; 0 stands still.</summary>
        public float WanderRadius { get; set; }

        FlowField homeField;
        string packKey;

        public IReadOnlyList<EnemyController> Members => members;

        /// <summary>How many members of this pack have been killed. Killed enemies stay dead while the level is loaded.</summary>
        public int KilledCount { get; private set; }

        /// <summary>The pack's center on the ground plane.</summary>
        public Vector2 Anchor => anchor;

        /// <summary>
        /// Where member number <paramref name="index"/> of <paramref name="count"/> stands relative to the pack center.
        /// A sunflower spiral spreads members evenly over the disc without any randomness.
        /// </summary>
        public static Vector2 SlotOffset(int index, int count, float radius)
        {
            var distance = radius * Mathf.Sqrt((index + 0.5f) / count);
            var angle = index * GoldenAngle;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        /// <summary>Sets a pack up from code, as the dungeon generator does, before its Start runs.</summary>
        public void Configure(EnemyDefinition memberDefinition, EnemyDefinition leaderDefinition, int memberCount, float spreadRadius, int memberLevel)
        {
            definition = memberDefinition;
            championDefinition = leaderDefinition;
            count = Mathf.Clamp(memberCount, 1, 12);
            radius = spreadRadius;
            level = memberLevel;
            slotDefinitions = null;
        }

        /// <summary>Sets up a mixed pack: one definition per slot (null leaves the slot empty). Kills are still
        /// remembered by slot, so the same slots must get the same enemies each time the level loads.</summary>
        public void Configure(EnemyDefinition[] perSlot, float spreadRadius, int memberLevel)
        {
            slotDefinitions = perSlot;
            count = perSlot.Length;
            radius = spreadRadius;
            level = memberLevel;
            definition = null;
            championDefinition = null;
            foreach (var slot in perSlot)
                if (slot != null && definition == null)
                    definition = slot;
        }

        void Start()
        {
            if (manager == null)
                manager = FindAnyObjectByType<EnemyManager>();
            if (manager == null || !manager.IsReady || definition == null)
            {
                Debug.LogWarning("[ARPG] EnemyPack needs a ready EnemyManager and an EnemyDefinition.", this);
                return;
            }

            anchor = IsoMath.WorldToGround(transform.position);
            origin = anchor;
            wanderTarget = anchor;
            wanderPause = Random.Range(0f, WanderRules.MaxPause);

            // Killed enemies stay dead while the session lasts, so a pack that was cleared earlier comes back cleared.
            // Keyed by level, not scene: every dungeon level shares one scene.
            packKey = LevelContext.CurrentId + "/" + name;
            var perch = 0;
            for (var i = 0; i < count; i++)
            {
                if (GameSession.Current.IsKilled(packKey, i))
                {
                    KilledCount++;
                    continue;
                }

                var slotDefinition = slotDefinitions != null ? slotDefinitions[i]
                    : i == 0 && championDefinition != null ? championDefinition : definition;
                if (slotDefinition == null)
                    continue;
                var position = anchor + SlotOffset(i, count, radius);
                var perched = false;
                // Past spots a prop stands on.
                while (perches != null && perch < perches.Count && !manager.Nav.IsWalkable(IsoMath.GroundToCell(perches[perch])))
                    perch++;
                if (perches != null && perch < perches.Count &&
                    (slotDefinition.Archetype == EnemyArchetype.Archer || slotDefinition.Archetype == EnemyArchetype.Caster))
                {
                    position = perches[perch];
                    perch++;
                    perched = true;
                }
                if (!manager.Nav.IsWalkable(IsoMath.GroundToCell(position)))
                    continue;

                var member = manager.Spawn(slotDefinition, position, false, this, level);
                member.Perched = perched;
                member.PackSlot = i;
                members.Add(member);
                places.Add(position - anchor);
            }
        }

        void Update()
        {
            if (WanderRadius <= 0f || manager == null || members.Count == 0)
                return;
            // Only a calm pack wanders: once any member is awake, the pack stays where the fight is.
            for (var i = 0; i < members.Count; i++)
                if (members[i].IsAlive && members[i].State != EnemyState.Idle)
                    return;
            if (wanderPause > 0f)
            {
                wanderPause -= Time.deltaTime;
                if (wanderPause <= 0f)
                    PickWanderTarget();
                return;
            }
            if (WanderRules.Step(ref anchor, wanderTarget, Time.deltaTime))
                wanderPause = Random.Range(WanderRules.MinPause, WanderRules.MaxPause);
            for (var i = 0; i < members.Count; i++)
                if (members[i].IsAlive && !members[i].Perched)
                    members[i].SetHome(anchor + places[i]);
        }

        // A point within the radius that can be walked to in a straight line from where the middle is; stays put when
        // a few tries find none.
        void PickWanderTarget()
        {
            for (var tries = 0; tries < 6; tries++)
            {
                var target = WanderRules.Target(origin, WanderRadius, Random.value, Random.value);
                if (manager.Nav.IsWalkable(IsoMath.GroundToCell(target)) && manager.Nav.HasClearWalk(anchor, target))
                {
                    wanderTarget = target;
                    return;
                }
            }
            wanderTarget = anchor;
            wanderPause = Random.Range(WanderRules.MinPause, WanderRules.MaxPause);
        }

        /// <summary>
        /// A member engaged the player: every idle member joins in (Docs/01: a pack idles until the player comes within
        /// aggro range). Without it, an archer at the back of a pack stood idle just past its own aggro range while the
        /// rest of the pack fought. Members walking home are left alone; they come back when the player is in range.
        /// </summary>
        internal void Alert()
        {
            for (var i = 0; i < members.Count; i++)
                members[i].Wake();
        }

        /// <summary>Called by a member when it dies. The pack never respawns it.</summary>
        internal void NotifyDeath(EnemyController member)
        {
            if (!members.Remove(member))
                return;

            KilledCount++;
            GameSession.Current.RecordKill(packKey, member.PackSlot);
        }

        /// <summary>
        /// The unit ground direction from a position toward the pack's center, routed around walls.
        /// The field is built on the first request, so a pack that never loses the player costs nothing.
        /// </summary>
        internal bool TryGetHomeDirection(Vector2 from, out Vector2 direction)
        {
            direction = Vector2.zero;
            if (manager == null || !manager.IsReady)
                return false;

            if (homeField == null)
            {
                homeField = new FlowField(manager.Nav);
                homeField.Compute(IsoMath.GroundToCell(anchor), HomeFieldRange);
            }

            return homeField.TryGetDirection(from, out direction);
        }
    }
}
