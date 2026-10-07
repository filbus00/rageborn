using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// An item or a pile of gold lying on the ground: a marker on the floor and, for items, a colored beam of light
    /// whose height and color show the rarity, and the item's name. An item hops out from where the enemy fell and
    /// lands with its beam rising; a Rare or Legendary marker flares as it lands. Pooled by <see cref="LootDirector"/>,
    /// which also lifts names that would overlap (<see cref="LabelLayout"/>). <see cref="PlayerLoot"/> decides when it
    /// is picked up.
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
        MeshRenderer labelRenderer;
        float labelLift;
        float labelLiftTarget;
        float beamHeight;
        Vector3 popFrom;
        float popTime = PopSeconds;
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
            labelRenderer = labelObject.GetComponent<MeshRenderer>();
            labelRenderer.sortingLayerName = GameSortingLayers.WorldUI;
            gameObject.SetActive(false);
        }

        /// <summary>Shows an item at <paramref name="ground"/>, hopping there from <paramref name="from"/> (where the
        /// enemy fell or the chest stands).</summary>
        internal void ShowItem(Item item, Vector2 ground, Vector2 from)
        {
            Item = item;
            GoldAmount = 0;
            Place(ground);
            var start = IsoMath.GroundToWorld(from);
            popFrom = new Vector3(start.x, start.y, 0f);
            popTime = 0f;

            var color = LootColors.Of(item.Rarity);
            marker.sprite = diamondSprite;
            marker.color = color;
            marker.transform.localScale = Vector3.one;

            // The beam fades toward its top, so it is tinted with a fully opaque color.
            beam.enabled = true;
            beam.color = color;
            beamHeight = LootColors.BeamHeight(item.Rarity);
            beam.transform.localScale = new Vector3(BeamWidth / BeamSpriteWidth, 0f, 1f);
            label.gameObject.SetActive(true);
            labelLift = labelLiftTarget = 0f;
            label.transform.localPosition = new Vector3(0f, LabelHeight, 0f);
            label.text = ItemComparison.Name(item);
            label.color = color;
            shownAt = Time.time;
            wantedCheckedAt = float.NegativeInfinity;
            wanted = AutoLootRules.Wants(SettingsDirector.Current.PickupRule, item.Rarity, false);
            gameObject.SetActive(true);
            enabled = true;
            Update();
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
            popTime = PopSeconds;
            shownAt = Time.time;
            gameObject.SetActive(true);
            enabled = false;
        }

        internal void Hide()
        {
            Item = null;
            GoldAmount = 0;
            gameObject.SetActive(false);
        }

        /// <summary>Seconds this drop has lain on the ground (scaled time, so the Bag's pause does not count).</summary>
        public float SecondsOnGround => Time.time - shownAt;

        /// <summary>Seconds since the drop finished its hop and landed (0 while it is still in the air). Auto-loot waits on
        /// this (AutoLootRules.ItemDelaySeconds after landing, as Docs/FIRST-RUN says): counting from the spawn picked items
        /// up 1.15 s after they landed (found in Unity on 2026-10-03).</summary>
        public float SecondsSinceLanding => popTime >= PopSeconds ? SecondsOnGround - PopSeconds : 0f;

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
            var upgrade = PowerScore.IsUpgrade(session.Equipment, Item, session.Level, session.Talents.Bonuses);
            wanted = AutoLootRules.Wants(SettingsDirector.Current.PickupRule, Item.Rarity, upgrade);
            // A drop the rule leaves behind shows its name dimmed.
            var color = LootColors.Of(Item.Rarity);
            label.color = wanted ? color : new Color(color.r, color.g, color.b, 0.45f);
            return wanted;
        }

        const float WantedRecheckSeconds = 1f;
        const float LabelHeight = 0.35f;

        // The hop out of a kill: its length, its height at the top in world units, and how long a Rare or better
        // marker flares on landing, at what scale. Tuning values.
        const float PopSeconds = 0.35f;
        const float PopHeight = 0.5f;
        const float FlareSeconds = 0.45f;
        const float FlareScale = 2.2f;
        // How fast a name slides to a new lift, in world units a second.
        const float LabelSlideSpeed = 3f;

        /// <summary>The last answer of <see cref="IsWanted"/>, without asking again.</summary>
        public bool WantedCached => IsGold || wanted;

        /// <summary>Where the name sits when not lifted: over the drop's resting place, in world units.</summary>
        public Vector2 LabelAnchor
        {
            get
            {
                var world = IsoMath.GroundToWorld(GroundPosition);
                return new Vector2(world.x, world.y + LabelHeight);
            }
        }

        /// <summary>The name's size in world units, as last drawn.</summary>
        public Vector2 LabelSize => IsGold || labelRenderer == null ? Vector2.zero : (Vector2)labelRenderer.bounds.size;

        /// <summary>Lifts the name this far above its anchor, so it clears the names around it; it slides there.</summary>
        public void SetLabelLift(float lift) => labelLiftTarget = lift;

        void Update()
        {
            if (IsGold)
            {
                enabled = false;
                return;
            }

            var dt = Time.deltaTime;
            if (popTime < PopSeconds)
            {
                popTime = Mathf.Min(PopSeconds, popTime + dt);
                var t = popTime / PopSeconds;
                var world = IsoMath.GroundToWorld(GroundPosition);
                var end = new Vector3(world.x, world.y, 0f);
                var position = Vector3.Lerp(popFrom, end, t);
                position.y += PopHeight * Mathf.Sin(t * Mathf.PI);
                transform.position = position;
                beam.transform.localScale = new Vector3(BeamWidth / BeamSpriteWidth, beamHeight * t / BeamSpriteHeight, 1f);
            }

            // A Rare or better marker flares as it lands, then settles.
            var sinceLanding = SecondsSinceLanding;
            if (Item.Rarity >= ItemRarity.Rare && sinceLanding > 0f && sinceLanding < FlareSeconds)
            {
                var scale = Mathf.Lerp(FlareScale, 1f, sinceLanding / FlareSeconds);
                marker.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else if (popTime >= PopSeconds && sinceLanding >= FlareSeconds)
            {
                marker.transform.localScale = Vector3.one;
            }

            if (!Mathf.Approximately(labelLift, labelLiftTarget))
            {
                labelLift = Mathf.MoveTowards(labelLift, labelLiftTarget, LabelSlideSpeed * dt);
                label.transform.localPosition = new Vector3(0f, LabelHeight + labelLift, 0f);
            }
        }

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
