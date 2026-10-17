using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The deep entrances (the owner, 2026-10-17: "add a new entrance for every 10 levels"; answer: start deeper once
    /// reached): one opens for every ten depths ever reached (10, 20, 30, 40), and taking it starts the run at that
    /// depth. A run started deeper is owed a boon for every depth it skipped (<see cref="GameSession.ReachDepth"/>).
    /// Pure.
    /// </summary>
    public static class EntranceRules
    {
        public const int Every = 10;

        /// <summary>The start depths open by the deepest depth ever reached.</summary>
        public static List<int> Open(int deepestEver)
        {
            var open = new List<int>();
            for (var depth = Every; depth <= deepestEver && depth < DungeonRules.Depths; depth += Every)
                open.Add(depth);
            return open;
        }

        /// <summary>Where each stands in the graveyard, round the mausoleum (clear of the town's pieces; a test checks).</summary>
        public static Vector2Int Cell(int depth) => (depth / Every) switch
        {
            1 => new Vector2Int(28, 4),
            2 => new Vector2Int(34, 4),
            3 => new Vector2Int(27, 8),
            _ => new Vector2Int(35, 9),
        };
    }

    /// <summary>A deep entrance in the graveyard: a lit stone with the depth on it; walking on starts the run there.</summary>
    public class DeepEntrance : WalkOnTrigger
    {
        static readonly Color Glow = new Color(0.95f, 0.55f, 0.25f, 0.9f);

        int depth;

        public static void CreateAll(Transform parent)
        {
            foreach (var depth in EntranceRules.Open(GameSession.Current.Meta.DeepestEver))
                Create(depth, parent);
        }

        static DeepEntrance Create(int depth, Transform parent)
        {
            var go = new GameObject("Entrance to depth " + depth, typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(IsoMath.CellToGround(EntranceRules.Cell(depth)));
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;
            var entrance = go.AddComponent<DeepEntrance>();
            entrance.depth = depth;
            WorldArt.Place("waypoint", go.transform, go.transform.position, true);
            TravelArt.GroundRing(go.transform, 1.4f, Glow);
            TravelArt.Label(go.transform, "Depth " + depth, new Color(1f, 0.75f, 0.45f), 1.1f);
            WorldLights.AddTravel(go.transform);
            return entrance;
        }

        protected override void OnWalkedOn()
        {
            var bag = InventoryScreen.Current;
            if ((bag != null && bag.IsOpen) || TownSheets.AnyOpen || SceneTravel.IsTravelling || GameSession.Current.RunStarted)
                return;
            Sfx.Play(SoundId.Hint);
            SceneTravel.ToDepth(depth, Arrival.FromAbove);
        }
    }
}
