using UnityEngine;
using UnityEngine.Rendering;

namespace ARPG
{
    /// <summary>
    /// An item or a pile of gold lying on the ground: a marker on the floor and, for items, a colored beam of light
    /// whose height and color show the rarity. Pooled by <see cref="LootDirector"/>. It has no logic of its own;
    /// <see cref="PlayerLoot"/> decides when it is picked up.
    /// </summary>
    public class LootDrop : MonoBehaviour
    {
        // A beam sprite is 24 x 256 pixels at 128 pixels per unit, so 2 units tall and 0.1875 wide.
        const float BeamSpriteHeight = 2f;
        const float BeamSpriteWidth = 0.1875f;

        // How wide the beam is drawn, in world units. Tuning value.
        const float BeamWidth = 0.22f;

        // The diamond marker is 0.5 units across; gold is drawn smaller as a coin.
        const float GoldScale = 0.5f;

        SpriteRenderer marker;
        SpriteRenderer beam;
        Sprite diamondSprite;
        Sprite coinSprite;

        /// <summary>The item on the ground, or null for gold.</summary>
        public Item Item { get; private set; }

        public int GoldAmount { get; private set; }

        public bool IsGold => Item == null;

        public Vector2 GroundPosition { get; private set; }

        /// <summary>Creates the two renderers. Called once when the drop is made.</summary>
        internal void Build(Sprite diamond, Sprite coin, Sprite beamSprite)
        {
            diamondSprite = diamond;
            coinSprite = coin;

            var material = DefaultMaterial();
            marker = NewRenderer("Marker", null, GameSortingLayers.Decals, material);
            beam = NewRenderer("Beam", beamSprite, GameSortingLayers.Effects, material);
            gameObject.SetActive(false);
        }

        internal void ShowItem(Item item, Vector2 ground)
        {
            Item = item;
            GoldAmount = 0;
            Place(ground);

            var color = LootColors.Of(item.Rarity);
            marker.sprite = diamondSprite;
            marker.color = color;
            marker.transform.localScale = Vector3.one;

            // The beam fades toward its top, so it is tinted with a fully opaque color.
            beam.enabled = true;
            beam.color = color;
            beam.transform.localScale = new Vector3(BeamWidth / BeamSpriteWidth, LootColors.BeamHeight(item.Rarity) / BeamSpriteHeight, 1f);
            gameObject.SetActive(true);
        }

        internal void ShowGold(int amount, Vector2 ground)
        {
            Item = null;
            GoldAmount = amount;
            Place(ground);

            marker.sprite = coinSprite;
            marker.color = LootColors.Gold;
            marker.transform.localScale = new Vector3(GoldScale, GoldScale, 1f);
            beam.enabled = false;
            gameObject.SetActive(true);
        }

        internal void Hide()
        {
            Item = null;
            GoldAmount = 0;
            gameObject.SetActive(false);
        }

        void Place(Vector2 ground)
        {
            GroundPosition = ground;
            var world = IsoMath.GroundToWorld(ground);
            transform.position = new Vector3(world.x, world.y, 0f);
        }

        SpriteRenderer NewRenderer(string objectName, Sprite sprite, string sortingLayer, Material material)
        {
            var go = new GameObject(objectName, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);

            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingLayerName = sortingLayer;
            if (material != null)
                spriteRenderer.sharedMaterial = material;
            return spriteRenderer;
        }

        static Material DefaultMaterial()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            return pipeline != null ? pipeline.default2DMaterial : null;
        }
    }
}
