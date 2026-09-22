using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The inventory and equip screen (Docs/03-itemization.md, Docs/08-production.md decision: no separate pause
    /// menu, just this). Opened from a HUD button, it shows equipped gear and character stats, lists the backpack,
    /// and lets the player equip, unequip and discard items. Opening it sets <see cref="Time.timeScale"/> to 0,
    /// which is the game's only pause. Replaces the placeholder <c>LootHud</c> text readout.
    /// </summary>
    public class InventoryScreen : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] GameObject panelRoot;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] Text statsText;
        [SerializeField] RectTransform listContent;

        [Header("Equipped slots")]
        [SerializeField] Image weaponSwatch;
        [SerializeField] Text weaponLabel;
        [SerializeField] Button weaponButton;
        [SerializeField] Image chestSwatch;
        [SerializeField] Text chestLabel;
        [SerializeField] Button chestButton;
        [SerializeField] Image helmSwatch;
        [SerializeField] Text helmLabel;
        [SerializeField] Button helmButton;

        [Tooltip("Left empty, the first PlayerHealth in the scene is used.")]
        [SerializeField] PlayerHealth health;

        static readonly Color EmptySlotColor = new Color(1f, 1f, 1f, 0.12f);

        struct SlotUi
        {
            public ItemSlot Slot;
            public Image Swatch;
            public Text Label;
            public Button Button;
        }

        SlotUi[] slots;
        readonly StringBuilder statsBuilder = new StringBuilder(256);

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        void Awake()
        {
            if (health == null)
                health = FindAnyObjectByType<PlayerHealth>();

            slots = new[]
            {
                new SlotUi { Slot = ItemSlot.Weapon, Swatch = weaponSwatch, Label = weaponLabel, Button = weaponButton },
                new SlotUi { Slot = ItemSlot.Chest, Swatch = chestSwatch, Label = chestLabel, Button = chestButton },
                new SlotUi { Slot = ItemSlot.Helm, Swatch = helmSwatch, Label = helmLabel, Button = helmButton },
            };

            if (openButton != null)
                openButton.onClick.AddListener(Toggle);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            // Never start paused, whatever state the scene was saved in.
            if (panelRoot != null)
                panelRoot.SetActive(false);
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            // Defensive: a scene change while the screen happened to be open must not leave the next scene paused.
            if (IsOpen)
                Time.timeScale = 1f;
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (panelRoot == null)
                return;

            panelRoot.SetActive(true);
            Time.timeScale = 0f;
            Refresh();
        }

        public void Close()
        {
            if (panelRoot == null)
                return;

            panelRoot.SetActive(false);
            Time.timeScale = 1f;
        }

        void Refresh()
        {
            var session = GameSession.Current;
            var equipment = session.Equipment;

            foreach (var slot in slots)
                RefreshSlot(session, equipment, slot);

            if (statsText != null)
                statsText.text = BuildStats(session, equipment);

            RebuildBackpack(session);
        }

        void RefreshSlot(GameSession session, EquipmentState equipment, SlotUi ui)
        {
            var item = equipment.Get(ui.Slot);

            if (ui.Swatch != null)
                ui.Swatch.color = item != null ? LootColors.Of(item.Rarity) : EmptySlotColor;
            if (ui.Label != null)
                ui.Label.text = item != null
                    ? $"{ui.Slot}\n{item.Rarity}, iLvl {item.ItemLevel}\n{item.Affixes.Count} affixes"
                    : $"{ui.Slot}\n(empty)";

            if (ui.Button == null)
                return;

            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = item != null;
            if (item != null)
            {
                var slot = ui.Slot;
                ui.Button.onClick.AddListener(() =>
                {
                    session.Unequip(slot);
                    Refresh();
                });
            }
        }

        string BuildStats(GameSession session, EquipmentState equipment)
        {
            var maxLife = health != null ? health.MaxLife : CombatFormulas.CharacterBaseLife(1) + equipment.TotalLifeBonus;
            var armor = health != null ? health.Armor : equipment.TotalArmor;

            statsBuilder.Clear();
            statsBuilder.Append("Life ").Append(maxLife.ToString("F0")).Append("   ");
            statsBuilder.Append("Armor ").Append(armor.ToString("F0")).Append("   ");
            statsBuilder.Append("Gold ").Append(session.Gold).AppendLine();
            statsBuilder.Append("Weapon damage ").Append(equipment.WeaponDamage.ToString("F1"));
            statsBuilder.Append(" (+").Append(equipment.FlatWeaponDamageBonus.ToString("F0")).Append(" flat, +");
            statsBuilder.Append(equipment.IncreasedDamagePercent.ToString("F0")).Append("% increased)").AppendLine();
            statsBuilder.Append("Attack speed +").Append(equipment.AttackSpeedPercent.ToString("F0")).Append("%   ");
            statsBuilder.Append("Crit ").Append(equipment.CriticalChancePercent.ToString("F0")).Append("% / +");
            statsBuilder.Append(equipment.CriticalDamagePercent.ToString("F0")).Append("%").AppendLine();
            statsBuilder.Append("Cooldown reduction ").Append(equipment.CooldownReductionPercent.ToString("F0")).Append("%   ");
            statsBuilder.Append("Life on hit ").Append(equipment.LifeOnHit.ToString("F0")).AppendLine();
            statsBuilder.Append("Backpack ").Append(session.Inventory.Count).Append("/").Append(session.Inventory.Capacity);
            return statsBuilder.ToString();
        }

        void RebuildBackpack(GameSession session)
        {
            if (listContent == null)
                return;

            // Destroy() is deferred to the end of the frame, so a detach first keeps childCount correct even if
            // Refresh runs again before the frame ends (equip and discard both call it immediately).
            while (listContent.childCount > 0)
            {
                var child = listContent.GetChild(0);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            var items = session.Inventory.Items;
            for (var i = 0; i < items.Count; i++)
                CreateRow(session, items[i]);
        }

        void CreateRow(GameSession session, Item item)
        {
            var row = new GameObject("Item Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(listContent, false);

            row.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            var layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 90f;
            layout.preferredHeight = 90f;

            var group = row.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(16, 16, 8, 8);
            group.spacing = 12f;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlHeight = true;
            group.childControlWidth = false;
            group.childForceExpandHeight = true;

            var swatch = NewImage(row.transform, LootColors.Of(item.Rarity));
            AddLayoutSize(swatch.gameObject, 24f, -1f);

            var label = NewText(row.transform, ItemSummary(item), TextAnchor.MiddleLeft);
            AddLayoutSize(label.gameObject, -1f, 1f);

            var equip = NewButton(row.transform, "Equip", () =>
            {
                session.EquipFromInventory(item);
                Refresh();
            });
            AddLayoutSize(equip.gameObject, 140f, -1f);

            var discard = NewButton(row.transform, "Discard", () =>
            {
                session.Discard(item);
                Refresh();
            });
            AddLayoutSize(discard.gameObject, 140f, -1f);
        }

        static string ItemSummary(Item item) =>
            $"{item.Rarity} {item.Slot} - item level {item.ItemLevel}, {item.Affixes.Count} affixes";

        static void AddLayoutSize(GameObject go, float width, float flexibleWidth)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null)
                element = go.AddComponent<LayoutElement>();
            if (width >= 0f)
                element.preferredWidth = width;
            element.flexibleWidth = flexibleWidth;
        }

        static Image NewImage(Transform parent, Color color)
        {
            var go = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text NewText(Transform parent, string text, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 26;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        static Button NewButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

            var text = NewText(go.transform, label, TextAnchor.MiddleCenter);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());
            return button;
        }
    }
}
