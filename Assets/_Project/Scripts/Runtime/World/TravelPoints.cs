using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A level's waypoint, or the Waystone in town (depth 0) (Docs/05-world-and-content.md): stepping on a waypoint
    /// activates it, and stepping on any of them opens the travel list (<see cref="WaypointScreen"/>) of every activated
    /// waypoint and the town.
    /// </summary>
    public class Waypoint : WalkOnTrigger
    {
        static readonly Color ActivatedColor = new Color(0.45f, 0.8f, 1f, 0.9f);
        static readonly Color DormantColor = new Color(0.45f, 0.55f, 0.65f, 0.6f);

        int depth;
        SpriteRenderer glow;

        /// <summary>Builds a waypoint (or the Waystone, depth 0) at a ground position.</summary>
        public static Waypoint Create(int depth, Vector2 ground, Transform parent)
        {
            var go = new GameObject(depth > 0 ? "Waypoint" : "Waystone", typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(ground);
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;

            var waypoint = go.AddComponent<Waypoint>();
            waypoint.depth = depth;
            waypoint.glow = TravelArt.GroundRing(go.transform, 1.4f, DormantColor);
            TravelArt.Label(go.transform, depth > 0 ? "Waypoint" : "Waystone", new Color(0.6f, 0.85f, 1f));
            waypoint.Refresh();
            return waypoint;
        }

        void Refresh() => glow.color = depth == 0 || GameSession.Current.HasWaypoint(depth) ? ActivatedColor : DormantColor;

        protected override void OnWalkedOn()
        {
            if (depth > 0 && GameSession.Current.ActivateWaypoint(depth))
            {
                Refresh();
                DamageNumbers.Current?.ShowText(transform.position + Vector3.up * 1.2f, "WAYPOINT", ActivatedColor, 44);
                Sfx.Play(SoundId.Hint);
            }
            WaypointScreen.Open(depth);
        }
    }

    /// <summary>
    /// A town portal from the Portal Tome (Docs/05): on its dungeon level it leads to town; in town, the matching portal
    /// leads back to where it was opened. It stays open until the player comes back through it (as in Diablo 1), or a new
    /// one is opened.
    /// </summary>
    public class TownPortal : WalkOnTrigger
    {
        static readonly Color PortalColor = new Color(0.4f, 0.65f, 1f, 0.85f);

        bool inTown;
        int depth;

        public static TownPortal Create(bool inTown, int depth, Vector2 ground, Transform parent)
        {
            var go = new GameObject(inTown ? "Portal Back" : "Town Portal", typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(ground);
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;

            var portal = go.AddComponent<TownPortal>();
            portal.inTown = inTown;
            portal.depth = depth;
            TravelArt.GroundRing(go.transform, 1.2f, PortalColor);
            TravelArt.Standing(go.transform, 0.9f, 1.8f, PortalColor);
            TravelArt.Label(go.transform, inTown ? $"Portal to level {depth}" : "Portal to town", new Color(0.6f, 0.8f, 1f), 2.1f);
            return portal;
        }

        protected override void OnWalkedOn()
        {
            Sfx.Play(SoundId.Hint);
            if (inTown)
                SceneTravel.ToDepth(depth, Arrival.AtPortal);
            else
                SceneTravel.ToTown();
        }
    }

    /// <summary>
    /// The Wanderer on the Portal Tome's depth (the user's choice, 2026-09-26: an NPC on dungeon depth 3 gives the Tome).
    /// Walking up to them the first time hands it over, with a hint on how to use it.
    /// </summary>
    public class Wanderer : WalkOnTrigger
    {
        const string GiftHint = "The Wanderer presses a worn book into your hands: the <color=#80B0FF>Portal Tome</color>. Tap Portal to open a way to town, and walk back through it to return.";

        public static Wanderer Create(Vector2 ground, Transform parent)
        {
            var go = new GameObject("Wanderer", typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(ground);
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.9f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;

            TravelArt.Figure(go.transform, new Color(0.62f, 0.58f, 0.5f));
            TravelArt.Label(go.transform, "Wanderer", new Color(0.9f, 0.85f, 0.7f), 1.5f);
            return go.AddComponent<Wanderer>();
        }

        protected override void OnWalkedOn()
        {
            var session = GameSession.Current;
            if (session.HasPortalTome)
                return;
            session.GivePortalTome();
            HintBanner.Current?.Show(GiftHint);
        }
    }

    /// <summary>Placeholder art for the travel points, made from the telegraph sprites: a ring on the ground, an upright
    /// glow for a portal, a plain figure for an NPC, and a floating label.</summary>
    public static class TravelArt
    {
        public static SpriteRenderer GroundRing(Transform parent, float diameter, Color color)
        {
            var go = GroundMarker.NewSprite("Ring", TelegraphArt.Ring, color, parent, 30);
            go.transform.localScale = new Vector3(diameter, diameter * IsoMath.GroundSquash, 1f);
            return go.GetComponent<SpriteRenderer>();
        }

        public static SpriteRenderer Standing(Transform parent, float width, float height, Color color)
        {
            var go = GroundMarker.NewSprite("Glow", TelegraphArt.Ember, color, parent, 0);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingLayerName = GameSortingLayers.Entities;
            go.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            go.transform.localScale = new Vector3(width, height, 1f);
            return renderer;
        }

        public static void Figure(Transform parent, Color color)
        {
            var body = GroundMarker.NewSprite("Body", TelegraphArt.Disc, color, parent, 0);
            var renderer = body.GetComponent<SpriteRenderer>();
            renderer.sortingLayerName = GameSortingLayers.Entities;
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(0.7f, 1.1f, 1f);
        }

        public static void Label(Transform parent, string text, Color color, float height = 1.1f)
        {
            var label = new GameObject("Label", typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3(0f, height, 0f);
            var mesh = label.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.characterSize = 0.07f;
            mesh.fontSize = 48;
            mesh.color = color;
            label.GetComponent<MeshRenderer>().sortingLayerName = GameSortingLayers.WorldUI;
        }
    }
}
