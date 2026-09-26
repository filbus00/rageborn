using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Forge's bottom sheet (Docs/03-itemization.md, Forge; Docs/06-ui-ux.md: town panels are bottom sheets, one
    /// Forge action per tab), opened by walking up to the smith (<see cref="ForgeNpc"/>). Tabs: Salvage (backpack items
    /// into materials, one by one or every Common and Magic at once), Reforge, Reroll and Temper (<see cref="ForgeRules"/>).
    /// A crafting tab lists the items it can work on, worn ones first; picking one shows its affixes, the cost and the
    /// action button, and the result stays selected so the player can see what changed and go again. The wallet line
    /// shows gold and every material. Built in code, like the item sheet. It does not pause: the town has no enemies.
    /// </summary>
    public class ForgeScreen : MonoBehaviour
    {
        enum Tab
        {
            Salvage,
            Reforge,
            Reroll,
            Temper,
        }

        static readonly Color SheetColor = new Color(0.09f, 0.08f, 0.08f, 0.98f);
        static readonly Color EmberColor = new Color(1f, 0.45f, 0.15f);
        static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.16f);
        static readonly Color SelectedColor = new Color(1f, 0.45f, 0.15f, 0.45f);
        static readonly Color DisabledColor = new Color(1f, 1f, 1f, 0.05f);
        static readonly Color RowColor = new Color(0f, 0f, 0f, 0.35f);

        // Docs/06-ui-ux.md: tap targets at least 48 points, 56 preferred; 3 canvas units per point.
        const float ButtonHeight = 168f;
        const float RowHeight = 150f;
        const float AffixHeight = 144f; // 48 points, the docs' minimum: a weapon's five affixes must still fit.
        const float SheetHeight = 2150f;

        Text wallet;
        Text hint;
        RectTransform list;
        RectTransform detail;
        readonly Dictionary<Tab, Image> tabImages = new Dictionary<Tab, Image>();

        Tab tab = Tab.Salvage;
        Item selected;
        int selectedAffix = -1;
        string message = "";

        public static ForgeScreen Current { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Builds the screen on its own overlay canvas, hidden.</summary>
        public static ForgeScreen Create()
        {
            var canvasObject = new GameObject("Forge Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // With the inventory screen: above the HUD (10), below the fade (100).
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            // A dimmed backdrop: tapping above the sheet closes it, and it keeps touches off the stick underneath.
            var root = new GameObject("Forge", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var screen = root.AddComponent<ForgeScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);
            screen.Build();
            root.SetActive(false);
            return screen;
        }

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
            GameSession.Current.Changed -= OnSessionChanged;
        }

        public void Open()
        {
            if (IsOpen)
                return;
            gameObject.SetActive(true);
            GameSession.Current.Changed += OnSessionChanged;
            selected = null;
            selectedAffix = -1;
            message = "";
            Render();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            GameSession.Current.Changed -= OnSessionChanged;
            gameObject.SetActive(false);
        }

        void OnSessionChanged() => Render();

        void Build()
        {
            var panel = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, SheetHeight);
            panel.GetComponent<Image>().color = SheetColor;

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 48);
            layout.spacing = 20f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var stroke = new GameObject("Stroke", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            stroke.transform.SetParent(panel.transform, false);
            stroke.GetComponent<Image>().color = EmberColor;
            stroke.GetComponent<LayoutElement>().ignoreLayout = true;
            var strokeRect = (RectTransform)stroke.transform;
            strokeRect.anchorMin = new Vector2(0f, 1f);
            strokeRect.anchorMax = new Vector2(1f, 1f);
            strokeRect.pivot = new Vector2(0.5f, 1f);
            strokeRect.sizeDelta = new Vector2(0f, 4f);

            var header = NewRow(panel.transform, "Header", 120f);
            var title = NewText(header, 56, FontStyle.Bold);
            title.text = "Forge";
            title.color = EmberColor;
            title.alignment = TextAnchor.MiddleLeft;
            Flexible(title.gameObject);
            Fixed(NewButton(header, "Close", Close).gameObject, 260f);

            wallet = NewText(panel.transform, 34, FontStyle.Normal);

            var tabs = NewRow(panel.transform, "Tabs", ButtonHeight);
            foreach (Tab each in Enum.GetValues(typeof(Tab)))
            {
                var value = each;
                var button = NewButton(tabs, value.ToString(), () => SelectTab(value));
                Flexible(button.gameObject);
                tabImages[value] = button.GetComponent<Image>();
            }

            hint = NewText(panel.transform, 30, FontStyle.Normal);
            hint.color = ItemSheet.NeutralColor;

            list = NewScrollList(panel.transform);

            var detailObject = new GameObject("Detail", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            detailObject.transform.SetParent(panel.transform, false);
            detail = (RectTransform)detailObject.transform;
            var detailLayout = detailObject.GetComponent<VerticalLayoutGroup>();
            detailLayout.spacing = 14f;
            detailLayout.childControlHeight = true;
            detailLayout.childControlWidth = true;
            detailLayout.childForceExpandHeight = false;
            detailLayout.childForceExpandWidth = true;
            detailObject.GetComponent<LayoutElement>().flexibleHeight = 0f;
        }

        void SelectTab(Tab value)
        {
            tab = value;
            selected = null;
            selectedAffix = -1;
            message = "";
            Render();
        }

        void Render()
        {
            if (!IsOpen)
                return;

            var session = GameSession.Current;
            wallet.text = WalletText(session);
            foreach (var pair in tabImages)
                pair.Value.color = pair.Key == tab ? SelectedColor : ButtonColor;
            hint.text = HintText(tab);

            // The selected item may have been replaced by a Forge result, or salvaged.
            if (selected != null && !Owns(session, selected))
                selected = null;

            Clear(list);
            Clear(detail);

            if (tab == Tab.Salvage)
                RenderSalvage(session);
            else
                RenderCrafting(session);
        }

        static string WalletText(GameSession session)
        {
            var text = new StringBuilder();
            text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(LootColors.Gold)).Append(">Gold ").Append(session.Gold).Append("</color>");
            foreach (CraftingMaterial material in Enum.GetValues(typeof(CraftingMaterial)))
                text.Append("   <color=#").Append(ColorUtility.ToHtmlStringRGB(MaterialColor(material))).Append('>')
                    .Append(material).Append(' ').Append(session.Materials(material)).Append("</color>");
            return text.ToString();
        }

        static string HintText(Tab tab)
        {
            switch (tab)
            {
                case Tab.Salvage:
                    return "Break backpack items into materials: Common gives Ash, Magic Cinders, Rare Bloodstone, Legendary Soulglass.";
                case Tab.Reforge:
                    return "Turn one affix of a Rare into a new random affix at the same tier. Each reforge of an item costs 25% more.";
                case Tab.Reroll:
                    return "Roll the values of every affix on a Legendary again, within their tiers.";
                default:
                    return $"Raise one affix a tier, keeping its place in the range. At most {ForgeRules.MaxTempers} times per item.";
            }
        }

        // --- Salvage ---------------------------------------------------------------------------------------------

        void RenderSalvage(GameSession session)
        {
            var items = session.Inventory.Items;
            if (items.Count == 0)
                AddNote(list, "The backpack is empty. Worn items are not salvaged.");

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var row = NewListRow(item, false, null);
                var (material, amount) = ForgeRules.SalvageYield(item);
                var yieldText = NewText(row, 30, FontStyle.Normal);
                yieldText.text = $"+{amount} {material}";
                yieldText.color = MaterialColor(material);
                yieldText.alignment = TextAnchor.MiddleRight;
                Fixed(yieldText.gameObject, 230f);
                Fixed(NewButton(row, "Salvage", () => session.Salvage(item)).gameObject, 220f);
            }

            var bulk = CountBelowRare(session);
            var bulkButton = NewButton(detail, bulk > 0 ? $"Salvage all Common and Magic ({bulk})" : "No Common or Magic items", () =>
            {
                // Collected first: salvaging changes the list being walked.
                var junk = new List<Item>();
                foreach (var item in session.Inventory.Items)
                    if (item.Rarity < ItemRarity.Rare)
                        junk.Add(item);
                foreach (var item in junk)
                    session.Salvage(item);
            });
            FixedHeight(bulkButton.gameObject, ButtonHeight);
            SetEnabled(bulkButton, bulk > 0);
        }

        static int CountBelowRare(GameSession session)
        {
            var count = 0;
            foreach (var item in session.Inventory.Items)
                if (item.Rarity < ItemRarity.Rare)
                    count++;
            return count;
        }

        // --- Reforge, Reroll, Temper -----------------------------------------------------------------------------

        void RenderCrafting(GameSession session)
        {
            var candidates = new List<(Item item, bool worn)>();
            foreach (ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))
            {
                var worn = session.Equipment.Get(slot);
                if (Eligible(worn))
                    candidates.Add((worn, true));
            }
            foreach (var item in session.Inventory.Items)
                if (Eligible(item))
                    candidates.Add((item, false));

            if (candidates.Count == 0)
                AddNote(list, tab == Tab.Reforge ? "No Rare items to reforge." : tab == Tab.Reroll ? "No Legendary items to reroll." : "No items with a temper left.");

            foreach (var (item, worn) in candidates)
            {
                var chosen = item;
                NewListRow(item, worn, () =>
                {
                    selected = chosen;
                    selectedAffix = -1;
                    message = "";
                    Render();
                });
            }

            if (selected != null)
                RenderDetail(session, selected);
        }

        bool Eligible(Item item)
        {
            switch (tab)
            {
                case Tab.Reforge: return ForgeRules.CanReforge(item);
                case Tab.Reroll: return ForgeRules.CanReroll(item);
                default: return ForgeRules.CanTemper(item);
            }
        }

        void RenderDetail(GameSession session, Item item)
        {
            var name = NewText(detail, 40, FontStyle.Bold);
            name.text = $"{ItemComparison.Name(item)}   <size=28><color=#999999>item level {item.ItemLevel}{ForgeHistory(item)}</color></size>";
            name.color = LootColors.Of(item.Rarity);

            var pickAffix = tab != Tab.Reroll;
            for (var i = 0; i < item.Affixes.Count; i++)
            {
                var index = i;
                var affix = item.Affixes[i];
                var usable = tab != Tab.Temper || ForgeRules.CanTemper(item, i);
                var button = NewButton(detail, AffixLine(affix, tab == Tab.Temper && usable), () =>
                {
                    selectedAffix = index;
                    message = "";
                    Render();
                });
                var label = button.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.fontStyle = FontStyle.Normal;
                label.fontSize = 32;
                label.rectTransform.offsetMin = new Vector2(28f, 0f);
                FixedHeight(button.gameObject, AffixHeight);
                button.GetComponent<Image>().color = pickAffix && index == selectedAffix ? SelectedColor : RowColor;
                button.interactable = pickAffix && usable;
            }

            var cost = tab == Tab.Reforge ? ForgeRules.ReforgeCost(item) : tab == Tab.Reroll ? ForgeRules.RerollCost(item) : ForgeRules.TemperCost(item);
            var affordable = session.CanAfford(cost);
            var ready = !pickAffix || selectedAffix >= 0;

            var costText = NewText(detail, 32, FontStyle.Normal);
            costText.text = CostText(session, cost) + (ready ? "" : "   <color=#999999>· pick an affix</color>");

            var action = NewButton(detail, tab.ToString(), () => Craft(session, item, cost));
            FixedHeight(action.gameObject, ButtonHeight);
            SetEnabled(action, affordable && ready);

            if (message.Length > 0)
            {
                var result = NewText(detail, 32, FontStyle.Bold);
                result.text = message;
                result.color = ItemSheet.GainColor;
            }
        }

        void Craft(GameSession session, Item item, ForgeCost cost)
        {
            Item result;
            string change;
            switch (tab)
            {
                case Tab.Reforge:
                    result = ForgeRules.Reforge(item, selectedAffix, session.ForgeRandom);
                    change = $"{Describe(item.Affixes[selectedAffix])}  →  {Describe(result.Affixes[selectedAffix])}";
                    break;
                case Tab.Reroll:
                    result = ForgeRules.Reroll(item, session.ForgeRandom);
                    change = "New values rolled.";
                    break;
                default:
                    result = ForgeRules.Temper(item, selectedAffix);
                    change = $"{Describe(item.Affixes[selectedAffix])}  →  {Describe(result.Affixes[selectedAffix])}";
                    break;
            }

            // Set before applying: the session's change event renders straight away and should show the result.
            var previous = (selected, message);
            selected = result;
            message = change;
            if (!session.ApplyForge(item, result, cost))
            {
                (selected, message) = previous;
                return;
            }
            if (tab == Tab.Temper && !ForgeRules.CanTemper(result, selectedAffix))
                selectedAffix = -1;
            Render();
        }

        static string Describe(AffixRoll affix) =>
            $"{ItemComparison.TierDots(affix.Tier)} +{ItemComparison.FormatValue(affix.Value, ItemComparison.IsPercent(affix.Id))} {ItemComparison.AffixLabel(affix.Id)}";

        static string AffixLine(AffixRoll affix, bool showNextTier)
        {
            var line = $"<color=#E0A040>{ItemComparison.TierDots(affix.Tier)}</color>  +{ItemComparison.FormatValue(affix.Value, ItemComparison.IsPercent(affix.Id))} {ItemComparison.AffixLabel(affix.Id)}";
            if (showNextTier)
            {
                var (min, max) = AffixRoller.ValueRange(affix.Id, affix.Tier - 1);
                var percent = ItemComparison.IsPercent(affix.Id);
                line += $"   <color=#999999>T{affix.Tier - 1}: {ItemComparison.FormatValue(min, percent)} to {ItemComparison.FormatValue(max, percent)}</color>";
            }
            return line;
        }

        static string ForgeHistory(Item item)
        {
            var parts = new List<string>();
            if (item.Reforges > 0)
                parts.Add($"reforged {item.Reforges}×");
            if (item.Tempers > 0)
                parts.Add($"tempered {item.Tempers} of {ForgeRules.MaxTempers}");
            return parts.Count > 0 ? "   ·   " + string.Join(", ", parts) : "";
        }

        static string CostText(GameSession session, ForgeCost cost)
        {
            var goldColor = session.Gold >= cost.Gold ? LootColors.Gold : ItemSheet.LossColor;
            var materialColor = session.Materials(cost.Material) >= cost.Amount ? MaterialColor(cost.Material) : ItemSheet.LossColor;
            return $"Cost  <color=#{ColorUtility.ToHtmlStringRGB(goldColor)}>{cost.Gold} gold</color>   " +
                   $"<color=#{ColorUtility.ToHtmlStringRGB(materialColor)}>{cost.Amount} {cost.Material}</color>";
        }

        static bool Owns(GameSession session, Item item) =>
            session.Inventory.Contains(item) || session.Equipment.Get(item.Slot) == item;

        /// <summary>Each material in its rarity's color, so Bloodstone reads as the Rare material and so on.</summary>
        static Color MaterialColor(CraftingMaterial material)
        {
            switch (material)
            {
                case CraftingMaterial.Ash: return LootColors.Of(ItemRarity.Common);
                case CraftingMaterial.Cinders: return LootColors.Of(ItemRarity.Magic);
                case CraftingMaterial.Bloodstone: return LootColors.Of(ItemRarity.Rare);
                default: return LootColors.Of(ItemRarity.Legendary);
            }
        }

        // --- Building blocks -------------------------------------------------------------------------------------

        RectTransform NewListRow(Item item, bool worn, Action onClick)
        {
            var row = new GameObject("Item Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(Button));
            row.transform.SetParent(list, false);
            row.GetComponent<Image>().color = item == selected ? SelectedColor : RowColor;
            var button = row.GetComponent<Button>();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            else
                button.interactable = false;
            FixedHeight(row, RowHeight);

            // The layout group must control widths, or the label's flexible width is ignored and names wrap.
            var group = row.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(16, 16, 12, 12);
            group.spacing = 16f;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = false;

            var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
            swatch.transform.SetParent(row.transform, false);
            swatch.GetComponent<Image>().color = LootColors.Of(item.Rarity);
            swatch.GetComponent<Image>().raycastTarget = false;
            Fixed(swatch, 24f);

            var label = NewText(row.transform, 32, FontStyle.Normal);
            label.alignment = TextAnchor.MiddleLeft;
            label.text = $"{ItemComparison.Name(item)}{(worn ? "  <color=#999999>(worn)</color>" : "")}\n" +
                         $"<size=26><color=#999999>item level {item.ItemLevel} · {item.Affixes.Count} {(item.Affixes.Count == 1 ? "affix" : "affixes")}{ForgeHistory(item)}</color></size>";
            Flexible(label.gameObject);
            return (RectTransform)row.transform;
        }

        static RectTransform NewScrollList(Transform parent)
        {
            var scrollGo = new GameObject("List", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(parent, false);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            var element = scrollGo.GetComponent<LayoutElement>();
            element.minHeight = 2 * RowHeight;
            element.flexibleHeight = 1f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = (RectTransform)viewportGo.transform;
            Stretch(viewportRect);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return contentRect;
        }

        static RectTransform NewRow(Transform parent, string name, float height)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            FixedHeight(row, height);
            var group = row.GetComponent<HorizontalLayoutGroup>();
            group.spacing = 20f;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = false;
            return (RectTransform)row.transform;
        }

        static void AddNote(Transform parent, string text)
        {
            var note = NewText(parent, 30, FontStyle.Italic);
            note.text = text;
            note.color = ItemSheet.NeutralColor;
            note.alignment = TextAnchor.MiddleCenter;
            FixedHeight(note.gameObject, RowHeight);
        }

        static Button NewButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = ButtonColor;
            go.GetComponent<Button>().onClick.AddListener(() => onClick());

            var text = NewText(go.transform, 36, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            Stretch(text.rectTransform);
            return go.GetComponent<Button>();
        }

        static void SetEnabled(Button button, bool enabled)
        {
            button.interactable = enabled;
            button.GetComponent<Image>().color = enabled ? ButtonColor : DisabledColor;
            button.GetComponentInChildren<Text>().color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        static Text NewText(Transform parent, int size, FontStyle style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static void Fixed(GameObject go, float width)
        {
            var element = Element(go);
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        static void Flexible(GameObject go)
        {
            var element = Element(go);
            element.flexibleWidth = 1f;
        }

        // A force-expanding layout group claims spare height itself unless its element pins flexibleHeight to 0.
        static void FixedHeight(GameObject go, float height)
        {
            var element = Element(go);
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }

        // Not `??`: a missing component is a Unity fake null in the editor, which `??` does not see as null.
        static LayoutElement Element(GameObject go) =>
            go.TryGetComponent<LayoutElement>(out var element) ? element : go.AddComponent<LayoutElement>();

        static void Clear(Transform parent)
        {
            // Detached first: Destroy waits for the end of the frame, and two renders in one frame would stack rows.
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
