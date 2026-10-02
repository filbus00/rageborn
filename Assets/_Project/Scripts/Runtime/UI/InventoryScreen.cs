using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Bag: the one screen the inventory button opens (Docs/03, Docs/06; no separate pause menu). Built in code after
    /// the owner's reference of 2026-09-28: a header with the portrait, name, level and gold; the paper doll, the
    /// character in its gear with the nine slots around it; the main stats; the Forge materials; the backpack as a grid,
    /// each item's tile in its rarity's color with an upgrade arrow; and tabs for Skills, Stats (the attributes), the Codex
    /// (the legendaries found) and Settings.
    /// A tap on a slot or an item opens the item sheet (<see cref="ItemSheet"/>) with the comparison, Equip and Discard.
    /// A green arrow on the Bag button says the backpack holds an upgrade (Docs/06, upgrade badge). Opening it sets
    /// <see cref="Time.timeScale"/> to 0, the game's only pause. Icons, frames and the painted portrait wait for the UI
    /// art (Docs/09); slot names and the baked character stand in.
    /// </summary>
    public class InventoryScreen : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] Button openButton;

        [Tooltip("Left empty, the first PlayerHealth in the scene is used.")]
        [SerializeField] PlayerHealth health;

        // The backpack grid (Inventory, 10 x 6): cells this size, so ten fit the Bag's width.
        const float Cell = 104f, CellGap = 4f, GridPadding = 10f;

        RectTransform content;
        ItemSheet sheet;
        // Opened by walking up to the merchant in town: the sheet sells instead of discarding, and the materials row
        // becomes the sale of all of a rarity. Ends when the Bag closes.
        bool atMerchant;
        // A sale of all of a rarity asks for a second tap; this is the rarity waiting for it.
        ItemRarity? pendingBulk;
        readonly List<Item> bulkScratch = new List<Item>();
        Text upgradeBadge;
        GameSession session;

        /// <summary>The screen in the current scene, or null when it has none. Lets <see cref="HitStop"/> tell
        /// this pause apart from its own, much shorter one when they happen to overlap.</summary>
        public static InventoryScreen Current { get; private set; }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        void Awake()
        {
            Current = this;
            if (health == null)
                health = FindAnyObjectByType<PlayerHealth>();

            if (openButton != null)
            {
                openButton.onClick.AddListener(Toggle);
                upgradeBadge = CreateBadge(openButton.transform);
                // The HUD around it: portrait, bars and this button in the top-left corner (the owner, 2026-09-28).
                HudLayout.Arrange(openButton.transform.parent);
            }

            if (panelRoot != null)
            {
                // The scene's old layout is replaced by the one built here.
                for (var i = panelRoot.transform.childCount - 1; i >= 0; i--)
                    Destroy(panelRoot.transform.GetChild(i).gameObject);
                var background = panelRoot.GetComponent<Image>();
                if (background != null)
                    background.color = UiStyle.Backdrop;
                var safe = UiStyle.Rect(panelRoot.transform, "Safe Area");
                SafeArea.Fit(safe);
                content = UiStyle.Rect(safe, "Content");
                UiStyle.Stretch(content);
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

        /// <summary>Opens the Bag at the merchant (<see cref="Merchant"/>): items are sold rather than discarded.</summary>
        public void OpenAtMerchant()
        {
            atMerchant = true;
            pendingBulk = null;
            if (sheet != null)
                sheet.Selling = true;
            Open();
        }

        public void Close()
        {
            if (panelRoot == null)
                return;

            atMerchant = false;
            pendingBulk = null;
            if (sheet != null)
                sheet.Selling = false;
            if (sheet != null)
                sheet.Hide();
            LoadoutScreen.CloseIfOpen();
            PassiveTreeScreen.CloseIfOpen();
            StatsScreen.CloseIfOpen();
            CodexScreen.CloseIfOpen();
            SettingsScreen.CloseIfOpen();
            panelRoot.SetActive(false);
            Time.timeScale = 1f;
        }

        // Rebuilt whole on every change: a few hundred UI objects, only while the game is paused.
        void Refresh()
        {
            if (content == null)
                return;
            // Destroy() is deferred to the end of the frame, so detach first: two refreshes in one frame (equip and the
            // sheet's own change event) must not stack two layouts.
            while (content.childCount > 0)
            {
                var child = content.GetChild(0);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            var current = GameSession.Current;
            BuildHeader(current);
            BuildDoll(current);
            BuildStats(current);
            if (atMerchant)
                BuildSaleRow(current);
            else
                BuildMaterials(current);
            BuildGrid(current);
            BuildTabs(current);
            RefreshBadge();
        }

        // --- Header -------------------------------------------------------------------------------------------------

        const float HeaderHeight = 200f;

        void BuildHeader(GameSession current)
        {
            var header = UiStyle.Place(UiStyle.Rect(content, "Header"), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, HeaderHeight));
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);

            HudLayout.Portrait(header, new Vector2(24f, -12f), 176f, current.Equipment);

            var name = UiStyle.Text(header, "Wild Arrow", 52, UiStyle.Gold, TextAnchor.MiddleLeft, true);
            UiStyle.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(222f, -24f), new Vector2(480f, 66f));
            var level = UiStyle.Text(header, atMerchant ? $"Level {current.Level}   ·   <color=#E0C040>At the merchant</color>" : $"Level {current.Level}", 32, UiStyle.TextDim, TextAnchor.MiddleLeft, true);
            UiStyle.Place(level.rectTransform, new Vector2(0f, 1f), new Vector2(224f, -92f), new Vector2(560f, 44f));

            // A thin life bar under the name, as the reference has it.
            var fraction = health != null ? Mathf.Clamp01(health.Fraction) : 1f;
            var bar = UiStyle.Framed(header, "Life", UiStyle.FrameDark, 2f);
            UiStyle.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(222f, -148f), new Vector2(430f, 20f));
            var fill = UiStyle.Image(UiStyle.FillOf(bar).transform, "Amount", UiStyle.BloodBright);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(fraction, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            var gold = UiStyle.Framed(header, "Gold", UiStyle.Panel, 3f);
            UiStyle.Place(gold.rectTransform, new Vector2(1f, 1f), new Vector2(-190f, -40f), new Vector2(250f, 96f));
            var goldText = UiStyle.Text(gold.transform, $"<color=#D9B77A>●</color> {current.Gold:N0}", 34, UiStyle.Gold, TextAnchor.MiddleCenter, true);
            UiStyle.Stretch(goldText.rectTransform, 8f);

            var close = UiStyle.Button(header, "Close", Close, 34);
            UiStyle.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-24f, -32f), new Vector2(144f, 112f));
        }

        // --- Paper doll ---------------------------------------------------------------------------------------------

        const float DollTop = -HeaderHeight - 8f, DollHeight = 800f;

        void BuildDoll(GameSession current)
        {
            var doll = UiStyle.Framed(content, "Paper Doll", new Color(0.07f, 0.05f, 0.05f, 1f), 4f);
            var rect = doll.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, DollTop - DollHeight);
            rect.offsetMax = new Vector2(-16f, DollTop);
            var inside = UiStyle.FillOf(doll).transform;

            // A dark red glow behind the character.
            var glow = UiStyle.Disc(inside, "Glow", new Color(0.35f, 0.05f, 0.04f, 0.35f));
            UiStyle.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(560f, 560f));
            var figure = UiStyle.Place(UiStyle.Rect(inside, "Character"), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(660f, 660f));
            new CharacterPortrait(figure).Refresh(current.Equipment);

            var equipment = current.Equipment;
            // Left column: helm, weapon, gloves. Right: amulet, chest, off-hand, boots. Bottom: ring, belt, ring.
            Slot(inside, equipment, ItemSlot.Helm, new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(200f, 200f));
            Slot(inside, equipment, ItemSlot.Weapon, new Vector2(0f, 1f), new Vector2(16f, -232f), new Vector2(200f, 330f));
            Slot(inside, equipment, ItemSlot.Gloves, new Vector2(0f, 1f), new Vector2(16f, -578f), new Vector2(200f, 190f));
            Slot(inside, equipment, ItemSlot.Amulet, new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(200f, 160f));
            Slot(inside, equipment, ItemSlot.Chest, new Vector2(1f, 1f), new Vector2(-16f, -192f), new Vector2(200f, 230f));
            Slot(inside, equipment, ItemSlot.OffHand, new Vector2(1f, 1f), new Vector2(-16f, -438f), new Vector2(200f, 160f));
            Slot(inside, equipment, ItemSlot.Boots, new Vector2(1f, 1f), new Vector2(-16f, -614f), new Vector2(200f, 170f));
            Slot(inside, equipment, ItemSlot.Ring, new Vector2(0.5f, 0f), new Vector2(-190f, 16f), new Vector2(130f, 130f));
            Slot(inside, equipment, ItemSlot.Belt, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(220f, 130f));
            Slot(inside, equipment, ItemSlot.Ring2, new Vector2(0.5f, 0f), new Vector2(190f, 16f), new Vector2(130f, 130f));
        }

        void Slot(Transform parent, EquipmentState equipment, ItemSlot place, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var item = equipment.Get(place);
            var tile = ItemTile(parent, item, UiStyle.SlotLabel(place), size.y >= 160f ? 30 : 26);
            UiStyle.Place(tile.rectTransform, anchor, position, size);
            if (item != null)
                tile.gameObject.AddComponent<Button>().onClick.AddListener(() => sheet.ShowEquipped(place));
        }

        /// <summary>An item's tile: framed in its rarity's color on a blood-dark fill, its icon (<see cref="ItemIcons"/>)
        /// and item level; or an empty place with the kind dimmed.</summary>
        static Image ItemTile(Transform parent, Item item, string emptyLabel, int size)
        {
            var tile = UiStyle.Framed(parent, item != null ? item.Slot + " Tile" : "Empty Tile", item != null ? UiStyle.SlotFill : UiStyle.EmptySlotFill, 4f);
            tile.raycastTarget = true;
            var rim = tile.transform.GetChild(0).GetComponent<Image>();
            if (item == null)
            {
                var empty = UiStyle.Text(tile.transform, emptyLabel, size, new Color(0.4f, 0.36f, 0.33f), TextAnchor.MiddleCenter, true);
                UiStyle.Stretch(empty.rectTransform, 8f);
                return tile;
            }

            rim.color = Color.Lerp(LootColors.Of(item.Rarity), UiStyle.Frame, item.Rarity == ItemRarity.Common ? 0.5f : 0.15f);
            var icon = UiStyle.Image(tile.transform, "Icon", Color.white);
            icon.sprite = ItemIcons.For(item.Slot, item.Rarity);
            icon.preserveAspect = true;
            UiStyle.Stretch(icon.rectTransform, 8f);
            // The item level in a corner; a one-cell tile has no room for it.
            if (size >= 20)
            {
                var level = UiStyle.Text(tile.transform, item.ItemLevel.ToString(), size - 4, new Color(0.62f, 0.58f, 0.54f), TextAnchor.LowerRight, true);
                UiStyle.Stretch(level.rectTransform, 8f);
            }
            return tile;
        }

        // --- Stats and materials ------------------------------------------------------------------------------------

        const float StatsTop = DollTop - DollHeight - 12f, StatsHeight = 200f;

        void BuildStats(GameSession current)
        {
            var panel = UiStyle.Framed(content, "Stats", UiStyle.Panel, 3f);
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, StatsTop - StatsHeight);
            rect.offsetMax = new Vector2(-16f, StatsTop);
            var inside = UiStyle.FillOf(panel).transform;

            var equipment = current.Equipment;
            var power = PowerScore.Evaluate(current);
            var maxLife = health != null ? health.MaxLife : CombatFormulas.CharacterLife(current.Level) + current.AttributeBonuses.Life + equipment.TotalLifeBonus;
            var life = health != null ? health.Fraction * maxLife : maxLife;
            var armor = health != null ? health.Armor : equipment.TotalArmor;
            var tree = current.PassiveTree.Bonuses;
            var attributes = current.AttributeBonuses;
            var crit = equipment.CriticalChancePercent + tree.CriticalChance + attributes.CriticalChance;
            var speed = (equipment.AttackSpeedPercent / 100f + tree.AttackSpeed + attributes.AttackSpeed) * 100f;

            StatLine(inside, 0, 0, "Life", $"{life:0} / {maxLife:0}");
            StatLine(inside, 1, 0, "Defense", $"{armor:0}");
            StatLine(inside, 0, 1, "Damage / s", $"{power.DamagePerSecond:0}");
            StatLine(inside, 1, 1, "Move speed", $"+{equipment.MovementSpeedPercent + (tree.MoveSpeed + attributes.MoveSpeed) * 100f:0}%");
            StatLine(inside, 0, 2, "Attack speed", $"+{speed:0}%");
            StatLine(inside, 1, 2, "Critical", $"{crit:0.#}%");

            var divider = UiStyle.Image(inside, "Divider", UiStyle.Frame);
            divider.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            divider.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            divider.rectTransform.sizeDelta = new Vector2(2f, -24f);
        }

        static void StatLine(Transform parent, int column, int row, string name, string value)
        {
            var line = UiStyle.Rect(parent, name);
            line.anchorMin = new Vector2(column * 0.5f, 1f);
            line.anchorMax = new Vector2(column * 0.5f + 0.5f, 1f);
            line.pivot = new Vector2(0.5f, 1f);
            line.offsetMin = new Vector2(28f, -14f - 58f * (row + 1));
            line.offsetMax = new Vector2(-28f, -14f - 58f * row);
            var label = UiStyle.Text(line, name, 30, UiStyle.TextDim, TextAnchor.MiddleLeft, true);
            UiStyle.Stretch(label.rectTransform);
            var number = UiStyle.Text(line, value, 32, UiStyle.TextMain, TextAnchor.MiddleRight, true);
            UiStyle.Stretch(number.rectTransform);
        }

        const float MaterialsTop = StatsTop - StatsHeight - 12f, MaterialsHeight = 96f;

        void BuildMaterials(GameSession current)
        {
            var panel = UiStyle.Framed(content, "Materials", UiStyle.Panel, 3f);
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, MaterialsTop - MaterialsHeight);
            rect.offsetMax = new Vector2(-16f, MaterialsTop);
            var inside = UiStyle.FillOf(panel).transform;

            // With the Forge switched off (Features.Forge) the row shows only the backpack count.
            var entries = new List<(string name, Color color, string count)>();
            if (Features.Forge)
            {
                entries.Add(("Ash", LootColors.Of(ItemRarity.Common), current.Materials(CraftingMaterial.Ash).ToString()));
                entries.Add(("Cinders", LootColors.Of(ItemRarity.Magic), current.Materials(CraftingMaterial.Cinders).ToString()));
                entries.Add(("Bloodstone", LootColors.Of(ItemRarity.Rare), current.Materials(CraftingMaterial.Bloodstone).ToString()));
                entries.Add(("Soulglass", LootColors.Of(ItemRarity.Legendary), current.Materials(CraftingMaterial.Soulglass).ToString()));
            }
            entries.Add(("Backpack", UiStyle.TextDim, $"{current.Inventory.CellsUsed}/{current.Inventory.CellCount} space"));
            for (var i = 0; i < entries.Count; i++)
            {
                var (name, color, count) = entries[i];
                var cell = UiStyle.Rect(inside, name);
                cell.anchorMin = new Vector2(i / (float)entries.Count, 0f);
                cell.anchorMax = new Vector2((i + 1) / (float)entries.Count, 1f);
                cell.offsetMin = cell.offsetMax = Vector2.zero;
                if (name != "Backpack")
                {
                    var dot = UiStyle.Disc(cell, "Dot", color);
                    UiStyle.Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(30f, 30f));
                }
                var text = UiStyle.Text(cell, $"{count} <size=22><color=#9E948A>{name}</color></size>", 30, UiStyle.TextMain, TextAnchor.MiddleLeft, true);
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(name != "Backpack" ? 56f : 16f, 0f);
                text.rectTransform.offsetMax = Vector2.zero;
            }
        }

        // At the merchant, the row under the stats sells all of a rarity at once (upgrades and named legendaries are
        // kept, SellRules.InBulkSale). The first tap names what would go and for how much; the second sells.
        void BuildSaleRow(GameSession current)
        {
            var row = UiStyle.Rect(content, "Sale");
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.offsetMin = new Vector2(16f, MaterialsTop - MaterialsHeight);
            row.offsetMax = new Vector2(-16f, MaterialsTop);

            var rarities = SellRules.BulkRarities;
            var columns = rarities.Length + 1;
            for (var i = 0; i < rarities.Length; i++)
            {
                var rarity = rarities[i];
                var gold = current.BulkSale(rarity, bulkScratch);
                var count = bulkScratch.Count;
                var confirming = pendingBulk == rarity && count > 0;
                var label = confirming ? $"Sell {count}? +{gold}" : count > 0 ? $"All {RarityName(rarity)} ({count})" : $"No {RarityName(rarity)}";
                var button = UiStyle.Button(row, label, () => OnBulkSale(rarity), 26, confirming ? UiStyle.Blood : UiStyle.Panel);
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(i / (float)columns, 0f);
                rect.anchorMax = new Vector2((i + 1) / (float)columns, 1f);
                rect.offsetMin = new Vector2(4f, 0f);
                rect.offsetMax = new Vector2(-4f, 0f);
                button.interactable = count > 0;
                var text = button.GetComponentInChildren<Text>();
                text.color = count > 0 ? (confirming ? UiStyle.Gold : LootColors.Of(rarity)) : new Color(0.4f, 0.36f, 0.33f);
            }

            var space = UiStyle.Text(row, $"{current.Inventory.CellsUsed}/{current.Inventory.CellCount}\n<size=22><color=#9E948A>space</color></size>",
                28, UiStyle.TextMain, TextAnchor.MiddleCenter, true);
            space.rectTransform.anchorMin = new Vector2(rarities.Length / (float)columns, 0f);
            space.rectTransform.anchorMax = Vector2.one;
            space.rectTransform.offsetMin = space.rectTransform.offsetMax = Vector2.zero;
        }

        void OnBulkSale(ItemRarity rarity)
        {
            if (pendingBulk != rarity)
            {
                pendingBulk = rarity;
                Refresh();
                return;
            }
            pendingBulk = null;
            if (GameSession.Current.SellAll(rarity) > 0)
                Sfx.Play(SoundId.Gold);
            Refresh();
        }

        static string RarityName(ItemRarity rarity) => rarity == ItemRarity.Magic ? "Magic" : rarity + "s";

        // --- Backpack grid ------------------------------------------------------------------------------------------

        const float GridTop = MaterialsTop - MaterialsHeight - 12f, TabsHeight = 150f;

        void BuildGrid(GameSession current)
        {
            var frame = UiStyle.Framed(content, "Backpack", new Color(0.06f, 0.045f, 0.045f, 1f), 4f);
            var rect = frame.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(16f, TabsHeight + 16f);
            rect.offsetMax = new Vector2(-16f, GridTop);
            var inside = (RectTransform)UiStyle.FillOf(frame).transform;

            var scroll = inside.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            inside.gameObject.AddComponent<RectMask2D>();
            UiStyle.FillOf(frame).raycastTarget = true;

            // The grid of cells (Inventory, 10 x 6), with each item laid over the cells it takes (ItemSize): a bow is a
            // tall 2 x 4 block, a ring one cell.
            var inventory = current.Inventory;
            var grid = UiStyle.Rect(inside, "Grid");
            grid.anchorMin = new Vector2(0.5f, 1f);
            grid.anchorMax = new Vector2(0.5f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.sizeDelta = new Vector2(inventory.Columns * Cell + (inventory.Columns - 1) * CellGap,
                inventory.Rows * Cell + (inventory.Rows - 1) * CellGap + 2f * GridPadding);
            scroll.content = grid;
            scroll.viewport = inside;

            for (var y = 0; y < inventory.Rows; y++)
                for (var x = 0; x < inventory.Columns; x++)
                {
                    var cell = UiStyle.Image(grid, "Cell", UiStyle.EmptySlotFill);
                    PlaceOnGrid(cell.rectTransform, new RectInt(x, y, 1, 1));
                }

            var items = inventory.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!inventory.PlacementOf(item, out var cells))
                    continue;
                var tile = ItemTile(grid, item, "", cells.width * cells.height == 1 ? 18 : 24);
                PlaceOnGrid(tile.rectTransform, cells);
                var index = i;
                tile.gameObject.AddComponent<Button>().onClick.AddListener(() => sheet.ShowBackpack(items, index));
                if (PowerScore.IsUpgrade(current.Equipment, item, current.Level, current.PassiveTree.Bonuses))
                {
                    var arrow = UiStyle.Text(tile.transform, "▲", 30, ItemSheet.GainColor, TextAnchor.MiddleCenter);
                    UiStyle.Place(arrow.rectTransform, new Vector2(1f, 1f), new Vector2(-6f, -4f), new Vector2(40f, 40f));
                }
            }
        }

        /// <summary>Puts a rect over a block of grid cells, counted from the grid's top left.</summary>
        static void PlaceOnGrid(RectTransform rect, RectInt cells)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(cells.x * (Cell + CellGap), -(GridPadding + cells.y * (Cell + CellGap)));
            rect.sizeDelta = new Vector2(cells.width * Cell + (cells.width - 1) * CellGap, cells.height * Cell + (cells.height - 1) * CellGap);
        }

        // --- Tabs ---------------------------------------------------------------------------------------------------

        void BuildTabs(GameSession current)
        {
            var bar = UiStyle.Rect(content, "Tabs");
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(16f, 8f);
            bar.offsetMax = new Vector2(-16f, 8f + TabsHeight - 8f);

            // Docs/02 and Q4: the loadout opens at level 9; before that the page shows while there are skill points to
            // spend. Stats (Docs/02, 2026-09-30, in the retired passive tree's place): 5 points every level from 2.
            var skillPoints = current.SkillLevels.Available(current.Level);
            var statPoints = current.Attributes.Available(current.Level);
            var tabs = new List<(string label, System.Action open, bool enabled, bool active)>
            {
                ("Inventory", null, true, true),
                (skillPoints > 0 ? $"Skills +{skillPoints}" : "Skills", LoadoutScreen.Open,
                    current.Level >= LoadoutScreen.OpensAtLevel || skillPoints > 0, false),
                (statPoints > 0 ? $"Stats +{statPoints}" : "Stats", StatsScreen.Open, true, false),
                ("Codex", CodexScreen.Open, true, false),
                ("Settings", SettingsScreen.Open, true, false),
            };
            for (var i = 0; i < tabs.Count; i++)
            {
                var (label, open, enabled, active) = tabs[i];
                var button = UiStyle.Button(bar, label, () => open?.Invoke(), 34, active ? UiStyle.Blood : UiStyle.Panel);
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(i / (float)tabs.Count, 0f);
                rect.anchorMax = new Vector2((i + 1) / (float)tabs.Count, 1f);
                rect.offsetMin = new Vector2(4f, 0f);
                rect.offsetMax = new Vector2(-4f, 0f);
                button.interactable = enabled;
                var text = button.GetComponentInChildren<Text>();
                if (active)
                    text.color = UiStyle.Gold;
                else if (!enabled)
                    text.color = new Color(0.4f, 0.36f, 0.33f);
                else if (label.Contains("+"))
                    text.color = new Color(1f, 0.71f, 0.35f);
            }
        }

        // --- Upgrade badge on the Bag button ------------------------------------------------------------------------

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
            var text = UiStyle.Text(button, "▲", 40, ItemSheet.GainColor, TextAnchor.MiddleCenter);
            UiStyle.Place(text.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(56f, 56f));
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.gameObject.name = "Upgrade Badge";
            return text;
        }
    }
}
