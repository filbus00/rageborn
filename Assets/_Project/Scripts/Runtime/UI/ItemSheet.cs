using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The item tooltip as a bottom sheet (Docs/03-itemization.md, item tooltips; Docs/06-ui-ux.md: menus open as
    /// bottom sheets, comparison before reading). Top to bottom: name in its rarity color, item level, the power score
    /// change with an arrow, the affixes with tier dots, the comparison strip against the equipped item with gains in
    /// green and losses in red, and one row of large buttons. A backpack item offers Equip and Discard, an equipped one
    /// Unequip. Swiping sideways moves through the backpack. Built in code by <see cref="InventoryScreen"/>; sockets,
    /// unique powers and Lock come with the systems behind them. Salvage is at the Forge in town, not here (the user's
    /// decision, 2026-09-26).
    /// </summary>
    public class ItemSheet : MonoBehaviour
    {
        public static readonly Color GainColor = new Color(0.55f, 0.9f, 0.5f);
        public static readonly Color LossColor = new Color(0.95f, 0.42f, 0.36f);
        public static readonly Color NeutralColor = new Color(0.7f, 0.7f, 0.7f);

        static readonly Color SheetColor = new Color(0.09f, 0.08f, 0.08f, 0.98f);
        static readonly Color EmberColor = new Color(1f, 0.45f, 0.15f);
        static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.16f);

        // Docs/06-ui-ux.md: tap targets at least 48 points, 56 preferred. The canvas is 1170 wide for a 390 point
        // screen, so a point is 3 canvas units.
        const float ButtonHeight = 168f;
        const float SheetHeight = 1350f;

        Text title;
        Text subtitle;
        Text power;
        Text affixes;
        Text comparison;
        RectTransform actions;

        IReadOnlyList<Item> list;
        int index;
        ItemSlot equippedSlot;
        bool showingEquipped;

        /// <summary>Raised after an action changed the gear or the backpack, so the screen behind can refresh.</summary>
        public event Action Changed;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Builds the sheet under <paramref name="parent"/>, hidden.</summary>
        public static ItemSheet Create(Transform parent)
        {
            // A dimmed full-screen backdrop: tapping above the sheet closes it.
            var root = new GameObject("Item Sheet", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var sheet = root.AddComponent<ItemSheet>();
            root.GetComponent<Button>().onClick.AddListener(sheet.Hide);
            sheet.Build();
            root.SetActive(false);
            return sheet;
        }

        void Build()
        {
            var panel = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(SwipeArea));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, SheetHeight);
            panel.GetComponent<Image>().color = SheetColor;
            panel.GetComponent<SwipeArea>().Swiped += OnSwiped;

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 40);
            layout.spacing = 18f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // Thin ember stroke along the top edge (Docs/06-ui-ux.md: dark stone panels with thin ember strokes).
            var stroke = new GameObject("Stroke", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            stroke.transform.SetParent(panel.transform, false);
            stroke.GetComponent<Image>().color = EmberColor;
            stroke.GetComponent<LayoutElement>().ignoreLayout = true;
            var strokeRect = (RectTransform)stroke.transform;
            strokeRect.anchorMin = new Vector2(0f, 1f);
            strokeRect.anchorMax = new Vector2(1f, 1f);
            strokeRect.pivot = new Vector2(0.5f, 1f);
            strokeRect.sizeDelta = new Vector2(0f, 4f);

            title = NewText(panel.transform, 50, FontStyle.Bold);
            subtitle = NewText(panel.transform, 30, FontStyle.Normal);
            subtitle.color = NeutralColor;
            power = NewText(panel.transform, 36, FontStyle.Bold);
            affixes = NewText(panel.transform, 32, FontStyle.Normal);
            comparison = NewText(panel.transform, 32, FontStyle.Normal);

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(panel.transform, false);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

            var row = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(panel.transform, false);
            actions = (RectTransform)row.transform;
            // Fixed height: a layout group that force-expands its children also claims spare height itself, and the
            // buttons grew to twice their size before this.
            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.minHeight = ButtonHeight;
            rowElement.preferredHeight = ButtonHeight;
            rowElement.flexibleHeight = 0f;
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 24f;
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
        }

        /// <summary>Shows one backpack item, compared with what is equipped in its slot. Swiping moves along the list.</summary>
        public void ShowBackpack(IReadOnlyList<Item> items, int itemIndex)
        {
            if (items == null || items.Count == 0)
            {
                Hide();
                return;
            }

            list = items;
            index = Mathf.Clamp(itemIndex, 0, items.Count - 1);
            showingEquipped = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Render();
        }

        /// <summary>Shows the item worn in a slot, with no comparison and an Unequip button.</summary>
        public void ShowEquipped(ItemSlot slot)
        {
            if (GameSession.Current.Equipment.Get(slot) == null)
                return;

            equippedSlot = slot;
            showingEquipped = true;
            list = null;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Render();
        }

        public void Hide() => gameObject.SetActive(false);

        void OnSwiped(int direction)
        {
            if (showingEquipped || list == null || list.Count < 2)
                return;

            // A swipe to the left brings in the next item, as a page turn does.
            index = (index - direction + list.Count) % list.Count;
            Render();
        }

        void Render()
        {
            var session = GameSession.Current;
            var equipment = session.Equipment;
            var item = showingEquipped ? equipment.Get(equippedSlot) : list[index];
            if (item == null)
            {
                Hide();
                return;
            }

            title.text = ItemComparison.Name(item);
            title.color = LootColors.Of(item.Rarity);

            var position = showingEquipped ? "equipped" : $"{index + 1} of {list.Count}, swipe for more";
            subtitle.text = $"Item level {item.ItemLevel}   ·   {position}";

            var worn = showingEquipped ? item : equipment.Get(PowerScore.PlaceFor(equipment, item, session.Level, session.PassiveTree.Bonuses));
            if (showingEquipped)
            {
                var now = PowerScore.Evaluate(equipment, session.Level, session.PassiveTree.Bonuses);
                power.text = $"Damage per second {now.DamagePerSecond:0.#}   ·   Effective life {now.EffectiveLife:0}";
                power.color = NeutralColor;
            }
            else
            {
                var before = PowerScore.Evaluate(equipment, session.Level, session.PassiveTree.Bonuses);
                var after = PowerScore.WithItem(equipment, item, session.Level, session.PassiveTree.Bonuses);
                var change = before.Score > 0f ? after.Score / before.Score - 1f : 0f;
                power.text = $"{Arrow(change)} Power {change * 100f:+0;-0;0}%\n" +
                             $"<size=28>Damage per second {before.DamagePerSecond:0.#} → {after.DamagePerSecond:0.#}   ·   " +
                             $"Effective life {before.EffectiveLife:0} → {after.EffectiveLife:0}</size>";
                power.color = DirectionColor(change);
            }

            affixes.text = AffixText(item);
            comparison.text = showingEquipped ? "" : ComparisonText(item, worn);

            BuildActions(session, item);
        }

        static string AffixText(Item item)
        {
            if (item.Affixes.Count == 0)
                return "<color=#999999>No affixes</color>";

            var text = new StringBuilder();
            foreach (var affix in item.Affixes)
            {
                if (text.Length > 0)
                    text.Append('\n');
                text.Append("<color=#E0A040>").Append(ItemComparison.TierDots(affix.Tier)).Append("</color>  +")
                    .Append(ItemComparison.FormatValue(affix.Value, ItemComparison.IsPercent(affix.Id))).Append(' ')
                    .Append(ItemComparison.AffixLabel(affix.Id));
            }
            return text.ToString();
        }

        static string ComparisonText(Item item, Item worn)
        {
            var text = new StringBuilder();
            text.Append("<size=28><color=#999999>")
                .Append(worn != null ? $"Against equipped {ItemComparison.Name(worn)}, item level {worn.ItemLevel}" : "Nothing equipped in this slot")
                .Append("</color></size>");

            foreach (var line in ItemComparison.Lines(item, worn))
            {
                var color = ColorUtility.ToHtmlStringRGB(line.Direction > 0 ? GainColor : line.Direction < 0 ? LossColor : NeutralColor);
                text.Append("\n<color=#").Append(color).Append('>').Append(Arrow(line.Direction)).Append(' ')
                    .Append(line.Label).Append("  ").Append(ItemComparison.FormatValue(line.Candidate, line.Percent))
                    .Append("</color><color=#999999>  (now ").Append(ItemComparison.FormatValue(line.Equipped, line.Percent))
                    .Append(")</color>");
            }
            return text.ToString();
        }

        void BuildActions(GameSession session, Item item)
        {
            for (var i = actions.childCount - 1; i >= 0; i--)
            {
                var child = actions.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            if (showingEquipped)
            {
                var slot = equippedSlot;
                NewButton("Unequip", () =>
                {
                    if (session.Unequip(slot))
                    {
                        Hide();
                        Changed?.Invoke();
                    }
                });
            }
            else
            {
                NewButton("Equip", () =>
                {
                    if (session.EquipFromInventory(item))
                    {
                        Hide();
                        Changed?.Invoke();
                    }
                });
                NewButton("Discard", () =>
                {
                    if (!session.Discard(item))
                        return;
                    Changed?.Invoke();
                    // Stay open on the next item, so a run of junk can be cleared quickly.
                    if (list.Count == 0)
                        Hide();
                    else
                        ShowBackpack(list, Mathf.Min(index, list.Count - 1));
                });
            }
            NewButton("Close", Hide);
        }

        public static string Arrow(float change) =>
            change > PowerScore.SameThreshold ? "▲" : change < -PowerScore.SameThreshold ? "▼" : "=";

        static string Arrow(int direction) => direction > 0 ? "▲" : direction < 0 ? "▼" : "=";

        public static Color DirectionColor(float change) =>
            change > PowerScore.SameThreshold ? GainColor : change < -PowerScore.SameThreshold ? LossColor : NeutralColor;

        void NewButton(string label, Action onClick)
        {
            var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(actions, false);
            go.GetComponent<Image>().color = ButtonColor;
            go.GetComponent<Button>().onClick.AddListener(() => onClick());

            var text = NewText(go.transform, 36, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            Stretch(text.rectTransform);
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

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
