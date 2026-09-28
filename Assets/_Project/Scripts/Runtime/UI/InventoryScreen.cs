using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The inventory and equip screen (Docs/03-itemization.md, Docs/08-production.md decision: no separate pause
    /// menu, just this). Opened from a HUD button, it shows equipped gear and character stats and lists the
    /// backpack, each row with an upgrade arrow and the power score change (<see cref="PowerScore"/>). Tapping a row or
    /// an equipped slot opens the item sheet (<see cref="ItemSheet"/>) with the full comparison. A green arrow on the
    /// Bag button says the backpack holds an upgrade (Docs/06-ui-ux.md, upgrade badge). Opening it sets
    /// <see cref="Time.timeScale"/> to 0, which is the game's only pause.
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
        ItemSheet sheet;
        Text upgradeBadge;
        GameSession session;

        /// <summary>The screen in the current scene, or null when it has none. Lets <see cref="HitStop"/> tell
        /// this pause apart from its own, much shorter one when they happen to overlap.</summary>
        public static InventoryScreen Current { get; private set; }

        Button skillsButton;
        Button treeButton;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        void Awake()
        {
            Current = this;
            if (health == null)
                health = FindAnyObjectByType<PlayerHealth>();

            slots = new[]
            {
                new SlotUi { Slot = ItemSlot.Weapon, Swatch = weaponSwatch, Label = weaponLabel, Button = weaponButton },
                new SlotUi { Slot = ItemSlot.Chest, Swatch = chestSwatch, Label = chestLabel, Button = chestButton },
                new SlotUi { Slot = ItemSlot.Helm, Swatch = helmSwatch, Label = helmLabel, Button = helmButton },
                SmallSlot(ItemSlot.Gloves, 0), SmallSlot(ItemSlot.Boots, 1), SmallSlot(ItemSlot.Belt, 2),
                SmallSlot(ItemSlot.Amulet, 3), SmallSlot(ItemSlot.Ring, 4), SmallSlot(ItemSlot.Ring2, 5),
            };

            if (openButton != null)
            {
                PlaceBagButton();
                SettingsDirector.Changed += PlaceBagButton;
                openButton.onClick.AddListener(Toggle);
                upgradeBadge = CreateBadge(openButton.transform);
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
                skillsButton = CreateSkillsButton(closeButton, 1, "Skills", LoadoutScreen.Open);
                treeButton = CreateSkillsButton(closeButton, 2, "Tree", PassiveTreeScreen.Open);
                CreateSettingsButton(closeButton);
            }

            if (panelRoot != null)
            {
                SafeArea.WrapChildren((RectTransform)panelRoot.transform);
                sheet = ItemSheet.Create(panelRoot.transform);
                sheet.Changed += Refresh;
            }

            session = GameSession.Current;
            session.Changed += RefreshBadge;
            RefreshBadge();

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
            if (Current == this)
                Current = null;
            if (session != null)
                session.Changed -= RefreshBadge;
            SettingsDirector.Changed -= PlaceBagButton;
        }

        void PlaceBagButton()
        {
            if (openButton != null)
                Handedness.PlaceInCorner((RectTransform)openButton.transform);
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

            if (sheet != null)
                sheet.Hide();
            LoadoutScreen.CloseIfOpen();
            PassiveTreeScreen.CloseIfOpen();
            SettingsScreen.CloseIfOpen();
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
            RefreshBadge();
            // Docs/02 and Q4: the loadout opens at level 9, when there is a fifth skill to choose; before that the page
            // only shows while there are skill points to spend (Claude's choice, 2026-09-28).
            if (skillsButton != null)
            {
                var points = session.SkillLevels.Available(session.Level);
                skillsButton.gameObject.SetActive(session.Level >= LoadoutScreen.OpensAtLevel || points > 0);
                var label = skillsButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = points > 0 ? $"Skills +{points}" : "Skills";
            }
            // The passive tree: a point every level from 2 (Docs/02).
            if (treeButton != null)
            {
                var treePoints = session.PassiveTree.Available(session.Level);
                treeButton.gameObject.SetActive(session.Level >= 2);
                var label = treeButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = treePoints > 0 ? $"Tree +{treePoints}" : "Tree";
            }
        }

        // A copy of the Close button placed the given number of buttons to its left, opening a page of the Bag.
        static Button CreateSkillsButton(Button close, int place, string name, UnityEngine.Events.UnityAction open)
        {
            var copy = Instantiate(close.gameObject, close.transform.parent);
            copy.name = name + " Button";
            var button = copy.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(open);
            var label = copy.GetComponentInChildren<Text>();
            if (label != null)
                label.text = name;
            var rect = (RectTransform)copy.transform;
            var source = (RectTransform)close.transform;
            rect.anchoredPosition = source.anchoredPosition - new Vector2((source.rect.width + 24f) * place, 0f);
            return button;
        }

        // A copy of the Close button mirrored into the top-left corner (the right side holds Tree, Skills and Close).
        static void CreateSettingsButton(Button close)
        {
            var button = CreateSkillsButton(close, 0, "Settings", SettingsScreen.Open);
            var rect = (RectTransform)button.transform;
            var source = (RectTransform)close.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, source.anchorMax.y);
            rect.pivot = new Vector2(0f, source.pivot.y);
            rect.anchoredPosition = new Vector2(-source.anchoredPosition.x, source.anchoredPosition.y);
            rect.sizeDelta = new Vector2(source.sizeDelta.x + 40f, source.sizeDelta.y);
        }

        /// <summary>Shows the badge while any backpack item would raise the power score.</summary>
        void RefreshBadge()
        {
            if (upgradeBadge == null)
                return;

            var current = GameSession.Current;
            var items = current.Inventory.Items;
            var any = false;
            for (var i = 0; i < items.Count && !any; i++)
                any = PowerScore.IsUpgrade(current.Equipment, items[i], current.Level, current.PassiveTree.Bonuses);
            upgradeBadge.gameObject.SetActive(any);
        }

        static Text CreateBadge(Transform button)
        {
            var text = NewText(button, "\u25B2", TextAnchor.MiddleCenter);
            text.fontSize = 40;
            text.color = ItemSheet.GainColor;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-8f, -8f);
            rect.sizeDelta = new Vector2(56f, 56f);
            text.gameObject.name = "Upgrade Badge";
            return text;
        }

        // The six slots that do not show on the character, in a second, smaller row under the weapon, chest and helm:
        // copies of the chest tile (the scene holds only the first three), so the layout needs no new scene objects.
        const float SmallSlotWidth = 170f, SmallSlotHeight = 110f, SmallSlotGap = 12f, SmallRowTop = -386f;

        SlotUi SmallSlot(ItemSlot slot, int column)
        {
            if (chestButton == null)
                return new SlotUi { Slot = slot };
            var copy = Instantiate(chestButton.gameObject, chestButton.transform.parent);
            copy.name = slot + " Slot";
            var rect = (RectTransform)copy.transform;
            rect.sizeDelta = new Vector2(SmallSlotWidth, SmallSlotHeight);
            rect.anchoredPosition = new Vector2((column - 2.5f) * (SmallSlotWidth + SmallSlotGap), SmallRowTop);
            var label = copy.GetComponentInChildren<Text>();
            if (label != null)
                label.fontSize = 22;
            return new SlotUi { Slot = slot, Swatch = copy.GetComponent<Image>(), Label = label, Button = copy.GetComponent<Button>() };
        }

        static string SlotName(ItemSlot slot) => slot == ItemSlot.Ring2 ? "Ring" : slot.ToString();

        void RefreshSlot(GameSession session, EquipmentState equipment, SlotUi ui)
        {
            var item = equipment.Get(ui.Slot);

            if (ui.Swatch != null)
                ui.Swatch.color = item != null ? LootColors.Of(item.Rarity) : EmptySlotColor;
            if (ui.Label != null)
                ui.Label.text = item != null
                    ? $"{SlotName(ui.Slot)}\n{item.Rarity}, iLvl {item.ItemLevel}\n{item.Affixes.Count} affixes"
                    : $"{SlotName(ui.Slot)}\n(empty)";

            if (ui.Button == null)
                return;

            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = item != null;
            if (item != null && sheet != null)
            {
                var slot = ui.Slot;
                ui.Button.onClick.AddListener(() => sheet.ShowEquipped(slot));
            }
        }

        string BuildStats(GameSession session, EquipmentState equipment)
        {
            var maxLife = health != null ? health.MaxLife : CombatFormulas.CharacterLife(session.Level) + equipment.TotalLifeBonus;
            var armor = health != null ? health.Armor : equipment.TotalArmor;

            statsBuilder.Clear();
            var progress = session.Progress;
            statsBuilder.Append("Level ").Append(progress.Level).Append("   XP ");
            if (progress.IsMaxLevel)
                statsBuilder.Append("max");
            else
                statsBuilder.Append(progress.Xp).Append(" / ").Append(progress.XpToNextLevel);
            statsBuilder.AppendLine();
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
            statsBuilder.Append("Move speed +").Append(equipment.MovementSpeedPercent.ToString("F0")).Append("%   ");
            statsBuilder.Append("Dodge +").Append(equipment.DodgePercent.ToString("F0")).Append("%   ");
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
                CreateRow(session, items, i);
        }

        void CreateRow(GameSession session, System.Collections.Generic.IReadOnlyList<Item> items, int index)
        {
            var item = items[index];
            var row = new GameObject("Item Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(Button));
            row.transform.SetParent(listContent, false);

            row.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            // The whole row opens the item sheet; its own buttons still take their taps first.
            if (sheet != null)
                row.GetComponent<Button>().onClick.AddListener(() => sheet.ShowBackpack(items, index));
            // Docs/06-ui-ux.md: tap targets at least 48 points; the canvas has 3 units per point.
            var layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 150f;
            layout.preferredHeight = 150f;

            // The layout group must control widths, or the label's flexible width is ignored and long names wrap.
            var group = row.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(16, 16, 12, 12);
            group.spacing = 16f;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = false;

            var swatch = NewImage(row.transform, LootColors.Of(item.Rarity));
            AddLayoutSize(swatch.gameObject, 24f, -1f);

            var label = NewText(row.transform, ItemSummary(item), TextAnchor.MiddleLeft);
            AddLayoutSize(label.gameObject, -1f, 1f);

            // Comparison before reading (Docs/06-ui-ux.md): the arrow and the power change come first to the eye.
            var change = PowerScore.Change(session.Equipment, item, session.Level, session.PassiveTree.Bonuses);
            var arrow = NewText(row.transform, $"{ItemSheet.Arrow(change)} {change * 100f:+0;-0;0}%", TextAnchor.MiddleRight);
            arrow.color = ItemSheet.DirectionColor(change);
            arrow.fontStyle = FontStyle.Bold;
            AddLayoutSize(arrow.gameObject, 150f, -1f);

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
            $"{ItemComparison.Name(item)} - item level {item.ItemLevel}, {item.Affixes.Count} affixes";

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
