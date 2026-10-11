using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Dresses the town when it loads (<see cref="TownLayout"/>): each piece's sprite from <see cref="WorldArt"/>,
    /// standing on its cell, blocking the character with its footprint (on the Obstacle layer, as walls do), the fires
    /// and lamps lit and flickering. Moves the town stairway into the mausoleum's doorway, and puts the player before it
    /// when she comes up from the first level. Pieces not imported yet are skipped. Created from code before the first scene and kept across scenes, so the town scene needs no rebuild.
    /// </summary>
    public class TownDressing : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Town Dressing");
            DontDestroyOnLoad(go);
            go.AddComponent<TownDressing>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneTravel.TownScene)
                return;
            var root = new GameObject("Town Pieces").transform;
            SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            var obstacle = LayerMask.NameToLayer(GameLayers.Obstacle);
            for (var i = 0; i < TownLayout.Pieces.Length; i++)
            {
                var piece = TownLayout.Pieces[i];
                var world = IsoMath.GroundToWorld(IsoMath.CellToGround(piece.Cell));
                var renderer = WorldArt.Place(piece.Name, root, new Vector3(world.x, world.y, 0f));
                if (renderer == null)
                    continue;
                var go = renderer.gameObject;
                if (piece.Half.x > 0f && piece.Half.y > 0f)
                {
                    // On a child with no sprite: a polygon collider added beside a sprite traces the sprite's outline
                    // first, which logs "Sprite outline generation failed" on iOS (the textures are not readable).
                    var footprint = new GameObject("Footprint", typeof(Rigidbody2D));
                    footprint.transform.SetParent(go.transform, false);
                    if (obstacle >= 0)
                        footprint.layer = obstacle;
                    footprint.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                    footprint.AddComponent<PolygonCollider2D>().points = TownLayout.Footprint(piece.Half);
                }
                switch (piece.Light)
                {
                    case TownLayout.Glow.Fire:
                        Flicker(WorldLights.Add(go.transform, new Color(1f, 0.55f, 0.22f), 1.4f, 0.5f, 5f, 0.3f), i);
                        break;
                    case TownLayout.Glow.Lamp:
                        Flicker(WorldLights.Add(go.transform, new Color(1f, 0.75f, 0.4f), 0.9f, 0.3f, 3.5f, 1.8f), i);
                        break;
                }
            }

            // The stairs down are the mausoleum's doorway (2026-10-11): the scene's stairway moves there and shows nothing
            // of its own; the mausoleum piece draws the door and the steps going down inside.
            var stairs = GameObject.Find("Stairs Down");
            if (stairs != null && WorldArt.Get("mausoleum") != null)
            {
                var door = IsoMath.GroundToWorld(IsoMath.CellToGround(TownLayout.StairsCell));
                stairs.transform.position = new Vector3(door.x, door.y, stairs.transform.position.z);
                if (stairs.TryGetComponent<SpriteRenderer>(out var stairsRenderer))
                    stairsRenderer.enabled = false;
                if (stairs.TryGetComponent<CircleCollider2D>(out var trigger))
                    trigger.radius = 0.9f;
            }

            // Up the stairs from the first level: she comes out of the mausoleum, on the road before its door.
            var session = GameSession.Current;
            if (session.Travel.Arrival == Arrival.FromBelow)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    var outside = IsoMath.GroundToWorld(IsoMath.CellToGround(TownLayout.StairsCell + new Vector2Int(0, -3)));
                    player.transform.position = new Vector3(outside.x, outside.y, player.transform.position.z);
                }
            }
            session.Travel = default;
        }

        static void Flicker(UnityEngine.Rendering.Universal.Light2D light, int index)
        {
            if (light != null)
                light.gameObject.AddComponent<FlickerLight>().Init(light, index * 1.9f);
        }
    }
}
