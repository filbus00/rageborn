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
        // Ground-space distance covered by the home flow field. Larger than any level.
        const float HomeFieldRange = 200f;

        const float GoldenAngle = 2.3999632f;

        [SerializeField] EnemyDefinition definition;

        [Tooltip("Docs: pack sizes range from 3 to 12 for normal packs.")]
        [SerializeField, Range(3, 12)] int count = 8;

        [Tooltip("Members spread over a disc of this radius, in ground units. About 2.2 keeps 12 members clear of each other.")]
        [SerializeField, Min(0f)] float radius = 2.2f;

        [Tooltip("Left empty, the first EnemyManager in the scene is used.")]
        [SerializeField] EnemyManager manager;

        readonly List<EnemyController> members = new List<EnemyController>();

        Vector2 anchor;
        FlowField homeField;

        public IReadOnlyList<EnemyController> Members => members;

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
            for (var i = 0; i < count; i++)
            {
                var position = anchor + SlotOffset(i, count, radius);
                if (!manager.Nav.IsWalkable(IsoMath.GroundToCell(position)))
                    continue;

                members.Add(manager.Spawn(definition, position, false, this));
            }
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
