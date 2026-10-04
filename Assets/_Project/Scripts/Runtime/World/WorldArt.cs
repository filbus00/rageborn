using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The pieces larger than one cell, modelled and rendered in Blender (`ArtSource/tools/props`) and brought in by
    /// `Tools > ARPG > Import World Art`: stairs, chests, waypoints, the ritual circle and the town's buildings and
    /// dressing, in Resources/Art/World, each with its pivot at the middle of its footprint. Null when a piece has not
    /// been imported, so callers keep their placeholder.
    /// </summary>
    public static class WorldArt
    {
        public const string Folder = "Art/World";

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var sprite) && sprite != null)
                return sprite;
            sprite = Resources.Load<Sprite>(Folder + "/" + name);
            cache[name] = sprite;
            return sprite;
        }

        /// <summary>
        /// A sprite of a piece standing in the world, lit by the 2D lights and sorted by its pivot against the
        /// characters; or lying flat under them on the Decals layer. Null when the piece is missing.
        /// </summary>
        public static SpriteRenderer Place(string name, Transform parent, Vector3 position, bool flat = false)
        {
            var sprite = Get(name);
            if (sprite == null)
                return null;
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = flat ? GameSortingLayers.Decals : GameSortingLayers.Entities;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            Lit(renderer);
            return renderer;
        }

        /// <summary>The 2D lit sprite material, so a piece darkens with the scene and catches the fires' light.</summary>
        public static void Lit(SpriteRenderer renderer)
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.default2DMaterial != null)
                renderer.sharedMaterial = pipeline.default2DMaterial;
        }
    }
}
