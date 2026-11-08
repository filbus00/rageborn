using UnityEngine;
using UnityEngine.UI;

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
        // Where the name sits (it slides up when lifted), and the name itself: sharp text on an overlay over the
        // world, since a world label is pixelated with it and could not be read (the owner, 2026-10-19).
        Transform label;
        Text text;
        Color labelColor;
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
            // picked up: on the overlay (LabelCanvas), placed over this point every frame.
            label = new GameObject("Name").transform;
            label.SetParent(transform, false);
            label.localPosition = new Vector3(0f, LabelHeight, 0f);
            var textObject = new GameObject("Loot Name", typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(LabelCanvas, false);
            text = textObject.GetComponent<Text>();
            // In the menus' pixel font when it is in the build (2026-11-08, the owner: "the text on dropped items is not
            // pixelated"), each font pixel a menu pixel (LabelCanvas is scaled to them).
            pixelFont = PixelUi.Available;
            text.font = pixelFont ? PixelUi.BodyFont : UiStyle.Body;
            text.fontStyle = pixelFont ? FontStyle.Normal : FontStyle.Bold;
            if (pixelFont)
                text.fontSize = text.font.fontSize;
            text.alignment = TextAnchor.LowerCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = pixelFont ? new Vector2(1f, -1f) : new Vector2(2f, -2f);
            var rect = (RectTransform)textObject.transform;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(10f, 10f);
            text.enabled = false;
            gameObject.SetActive(false);
        }

        /// <summary>Shows an item at <paramref name="ground"/>, hopping there from <paramref name="from"/> (where the
        /// enemy fell or the chest stands).</summary>
        internal void ShowItem(Item item, Vector2 ground, Vector2 from)
        {
            Item = item;
            GoldAmount = 0;
            Place(ground);
            var start = Elevation.World2(from);
            popFrom = new Vector3(start.x, start.y, 0f);
            popTime = 0f;

            var color = LootColors.Of(item.Rarity);
            marker.sprite = diamondSprite;
            marker.color = color;
            marker.sortingLayerName = GameSortingLayers.Decals;
            flying = false;
            marker.transform.localScale = Vector3.one;

            // The beam fades toward its top, so it is tinted with a fully opaque color.
            beam.enabled = true;
            beam.color = color;
            beamHeight = LootColors.BeamHeight(item.Rarity);
            beam.transform.localScale = new Vector3(BeamWidth / BeamSpriteWidth, 0f, 1f);
            labelLift = labelLiftTarget = 0f;
            label.localPosition = new Vector3(0f, LabelHeight, 0f);
            text.text = ItemComparison.Name(item);
            labelColor = color;
            text.color = color;
            text.enabled = true;
            shownAt = Time.time;
            wantedCheckedAt = float.NegativeInfinity;
            wanted = AutoLootRules.Wants(SettingsDirector.Current.PickupRule, item.Rarity, false);
            gameObject.SetActive(true);
            enabled = true;
            Update();
        }

        internal void ShowGold(int amount, Vector2 ground, int pile = 0)
        {
            Item = null;
            GoldAmount = amount;
            Place(ground);
            flying = false;
            marker.sortingLayerName = GameSortingLayers.Decals;

            // A drawn pile of coins, bigger for richer drops (2026-10-31, the owner's Gemini art), else the gold disc.
            var drawn = WorldArt.Get(GoldPileName + (Mathf.Clamp(pile, 0, 2) + 1));
            marker.sprite = drawn != null ? drawn : coinSprite;
            marker.color = drawn != null ? Color.white : LootColors.Gold;
            var scale = drawn != null ? 1f : GoldScale;
            marker.transform.localScale = new Vector3(scale, scale, 1f);
            marker.transform.localPosition = Vector3.zero;
            beam.enabled = false;
            text.enabled = false;
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

        // The name goes with the drop (picked up, or pooled away).
        void OnDisable()
        {
            if (text != null)
                text.enabled = false;
        }

        void OnDestroy()
        {
            if (text != null)
                Destroy(text.gameObject);
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
            // A blueprint is always taken.
            wanted = Item.IsBlueprint || AutoLootRules.Wants(SettingsDirector.Current.PickupRule, Item.Rarity, upgrade);
            // A drop the rule leaves behind shows its name dimmed.
            var color = LootColors.Of(Item.Rarity);
            labelColor = wanted ? color : new Color(color.r, color.g, color.b, 0.45f);
            text.color = labelColor;
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
                var world = Elevation.World2(GroundPosition);
                return new Vector2(world.x, world.y + LabelHeight);
            }
        }

        /// <summary>The name's size in world units, as last drawn.</summary>
        public Vector2 LabelSize => IsGold || text == null || pixelsPerUnit <= 0f
            ? Vector2.zero
            : new Vector2(text.preferredWidth * labelScale / pixelsPerUnit, text.preferredHeight * labelScale / pixelsPerUnit);

        bool pixelFont;
        // Screen pixels a world unit spans, measured each frame from the camera; the name's size follows it, so it
        // keeps its size against the world when the view changes.
        float pixelsPerUnit;

        /// <summary>The name's height against the world, in world units (the owner, 2026-10-19: the first sharp names, at
        /// 20 pixels, were "too big"): 16 pixels on the iPhone 11.</summary>
        public const float NameHeight = 0.3f;

        void LateUpdate()
        {
            if (IsGold || text == null || !text.enabled)
                return;
            var cam = Camera.main;
            if (cam == null)
                return;
            var at = label.position;
            var screen = cam.WorldToScreenPoint(at);
            pixelsPerUnit = Mathf.Abs(cam.WorldToScreenPoint(at + Vector3.right).x - screen.x);
            if (pixelFont)
            {
                // On whole menu pixels, so every letter's pixels are the same size.
                var s = labelScale;
                text.rectTransform.position = new Vector3(Mathf.Round(screen.x / s) * s, Mathf.Round(screen.y / s) * s, 0f);
                return;
            }
            var size = Mathf.Max(15, Mathf.RoundToInt(pixelsPerUnit * NameHeight));
            if (text.fontSize != size)
                text.fontSize = size;
            text.rectTransform.position = new Vector3(Mathf.Round(screen.x), Mathf.Round(screen.y), 0f);
        }

        static Transform labelCanvas;

        /// <summary>Screen pixels a unit of the names' overlay: the menus' pixel size, so a pixel font's pixel is that
        /// many screen pixels (2 on the iPhone 11); 1 with the old font.</summary>
        static float labelScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            labelCanvas = null;
            labelScale = 1f;
        }

        // One overlay for every drop's name, under the HUD and the menus (sorting 5), kept across scenes as the drops
        // are.
        static Transform LabelCanvas
        {
            get
            {
                if (labelCanvas != null)
                    return labelCanvas;
                var go = new GameObject("Loot Names", typeof(Canvas), typeof(CanvasScaler));
                DontDestroyOnLoad(go);
                var canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5;
                labelScale = PixelUi.Available ? PixelUi.Scale(Screen.width) : 1f;
                canvas.pixelPerfect = PixelUi.Available;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = labelScale;
                return labelCanvas = go.transform;
            }
        }

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
                var end = Elevation.World(GroundPosition);
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
                label.localPosition = new Vector3(0f, LabelHeight + labelLift, 0f);
            }
        }

        /// <summary>Moves the drop on the ground: a pet carrying it to the character (Docs/02, Pets).</summary>
        internal void MoveTo(Vector2 ground) => Place(ground);

        /// <summary>
        /// Moves gold flying to the character (2026-10-31: a heap sliding over the grass looked like a dragged stone): the
        /// pile becomes a single spinning coin that hops on its way, over everything on the ground.
        /// </summary>
        internal void Fly(Vector2 ground)
        {
            if (!flying)
            {
                flying = true;
                flyingSince = Time.time;
                var coin = WorldArt.Get(FlyingCoinName);
                if (coin != null)
                {
                    marker.sprite = coin;
                    marker.color = Color.white;
                }
                marker.sortingLayerName = GameSortingLayers.Effects;
            }
            Place(ground);
            var (hop, face) = AutoLootRules.CoinFlight(Time.time - flyingSince);
            var size = marker.sprite == coinSprite ? GoldScale : 1f;
            marker.transform.localPosition = new Vector3(0f, hop, 0f);
            marker.transform.localScale = new Vector3(size * Mathf.Max(0.15f, Mathf.Abs(face)), size, 1f);
        }

        const string GoldPileName = "gold_pile_";
        const string FlyingCoinName = "gold_coin";
        bool flying;
        float flyingSince;

        void Place(Vector2 ground)
        {
            GroundPosition = ground;
            transform.position = Elevation.World(ground);
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
