using UnityEngine;

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
        TextMesh label;
        float shownAt;
        float wantedCheckedAt = float.NegativeInfinity;
        bool wanted;
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

            // The item's name over it in its rarity's color (the owner, 2026-10-02), so a drop is read before it is
            // picked up. A world label: it is pixelated with the world, like the NPC names.
            var labelObject = new GameObject("Name", typeof(TextMesh));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, LabelHeight, 0f);
            label = labelObject.GetComponent<TextMesh>();
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.055f;
            label.fontSize = 48;
            labelObject.GetComponent<MeshRenderer>().sortingLayerName = GameSortingLayers.WorldUI;
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
            label.gameObject.SetActive(true);
            label.text = ItemComparison.Name(item);
            label.color = color;
            shownAt = Time.time;
            wantedCheckedAt = float.NegativeInfinity;
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
            label.gameObject.SetActive(false);
            shownAt = Time.time;
            gameObject.SetActive(true);
        }

        internal void Hide()
        {
            Item = null;
            GoldAmount = 0;
            gameObject.SetActive(false);
        }

        /// <summary>Seconds this drop has lain on the ground (scaled time, so the Bag's pause does not count).</summary>
        public float SecondsOnGround => Time.time - shownAt;

        /// <summary>
        /// Whether auto-loot and the pet take this item under the player's pick-up rule (<see cref="AutoLootRules.Wants"/>);
        /// always true for gold. Checked at most once a second, since the upgrade test runs the power score.
        /// </summary>
        public bool IsWanted()
        {
            if (IsGold)
                return true;
            if (Time.unscaledTime - wantedCheckedAt < WantedRecheckSeconds)
                return wanted;
            wantedCheckedAt = Time.unscaledTime;
            var session = GameSession.Current;
            var upgrade = PowerScore.IsUpgrade(session.Equipment, Item, session.Level, session.PassiveTree.Bonuses);
            wanted = AutoLootRules.Wants(SettingsDirector.Current.PickupRule, Item.Rarity, upgrade);
            // A drop the rule leaves behind shows its name dimmed.
            var color = LootColors.Of(Item.Rarity);
            label.color = wanted ? color : new Color(color.r, color.g, color.b, 0.45f);
            return wanted;
        }

        const float WantedRecheckSeconds = 1f;
        const float LabelHeight = 0.35f;

        /// <summary>Moves the drop on the ground: a pet carrying it to the character (Docs/02, Pets).</summary>
        internal void MoveTo(Vector2 ground) => Place(ground);

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

        // Unlit: a drop's marker and beam must be seen in the dark, which is how loot is found in a dark dungeon.
        static Material DefaultMaterial() => SpriteMaterials.Unlit;
    }
}
