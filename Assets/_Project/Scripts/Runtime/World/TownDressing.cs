using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Dresses the town when it loads (<see cref="TownLayout"/>): each piece's sprite from <see cref="WorldArt"/>,
    /// standing on its cell, blocking the character with its footprint (on the Obstacle layer, as walls do), the fires
    /// and lamps lit and flickering. Swaps the town stairway's placeholder for the modelled one. Pieces not imported yet
    /// are skipped. Created from code before the first scene and kept across scenes, so the town scene needs no rebuild.
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
                    if (obstacle >= 0)
                        go.layer = obstacle;
                    var body = go.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Static;
                    go.AddComponent<PolygonCollider2D>().points = TownLayout.Footprint(piece.Half);
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

            // The modelled stairway down, lying flat where the placeholder was.
            var stairs = GameObject.Find("Stairs Down");
            var sprite = WorldArt.Get("stairs_down");
            if (stairs != null && sprite != null && stairs.TryGetComponent<SpriteRenderer>(out var stairsRenderer))
            {
                stairsRenderer.sprite = sprite;
                stairsRenderer.sortingLayerName = GameSortingLayers.Decals;
                WorldArt.Lit(stairsRenderer);
            }
        }

        static void Flicker(UnityEngine.Rendering.Universal.Light2D light, int index)
        {
            if (light != null)
                light.gameObject.AddComponent<FlickerLight>().Init(light, index * 1.9f);
        }
    }
}
