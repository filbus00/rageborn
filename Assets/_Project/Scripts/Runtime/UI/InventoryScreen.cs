using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// An item dragged from the backpack onto a slot it fits is worn there, and worn gear dragged onto the backpack comes
    /// off (the owner, 2026-10-03); while dragging, the places it fits light up.
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

        // Dragging (ItemDrag): the item, where it was worn (null from the backpack), the picture following the finger,
        // and every doll slot's rim to light up the places the item fits.
        Item dragItem;
        ItemSlot? dragFrom;
        RectTransform dragGhost;
        readonly Dictionary<ItemSlot, (Image rim, Color color)> slotRims = new Dictionary<ItemSlot, (Image rim, Color color)>();
        Image backpackRim;
        Color backpackRimColor;
        ItemDropTarget lastOver;
        readonly List<RaycastResult> dropHits = new List<RaycastResult>();
        static readonly Color DropColor = new Color(0.45f, 0.85f, 0.4f, 1f);
        static readonly Color NoDropColor = new Color(0.25f, 0.22f, 0.2f, 1f);

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
                {
                    background.color = UiStyle.Backdrop;
                    UiStyle.Leather(background);
                    // Opaque, so the town's labels and the HUD do not show through the gaps (2026-10-19).
                    var opaque = background.color;
                    opaque.a = 1f;
                    background.color = opaque;
                }
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
            TalentScreen.CloseIfOpen();
            BoonListScreen.CloseIfOpen();
            PassiveTreeScreen.CloseIfOpen();
            StatsScreen.CloseIfOpen();
            CodexScreen.CloseIfOpen();
            QuestLogScreen.CloseIfOpen();
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

            EndDrag();
            slotRims.Clear();
            var current = GameSession.Current;
            LayOut();
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

        const float HeaderHeight = MenuChrome.HeaderHeight;

        void BuildHeader(GameSession current)
        {
            // The header and the page's title as the owner's concept art has them (2026-10-19, MenuChrome).
            var where = Features.Roguelite ? MenuChrome.Where() : $"Level {current.Level}";
            var subtitle = atMerchant ? $"{where}   ·   <color=#E0C040>At the merchant</color>" : where;
            var header = MenuChrome.Header(content, current, health != null ? health.Fraction : 1f, subtitle, Close);
            MenuChrome.Title(content, "Inventory", HeaderHeight);

            // The portal home (2026-10-15): in the dungeon, ends the run keeping what she carries, after a short channel.
            if (PortalChannel.CanOpen)
            {
                var portal = UiStyle.Button(header, PortalChannel.IsOpening ? "Opening..." : "Portal home", () =>
                {
                    Close();
                    PortalChannel.Begin();
                }, 30, UiStyle.Blood);
                UiStyle.Place((RectTransform)portal.transform, new Vector2(1f, 1f), new Vector2(-24f, -150f), new Vector2(300f, 64f));
            }
        }

        // --- Paper doll ---------------------------------------------------------------------------------------------

        // Only the gear slots since 2026-10-18 (the owner: "Remove the character display in the inventory screen, just put
        // the gear slots there"): two rows of five, so the backpack has more room.
        const float DollHeight = 520f;

        // Where each block starts (negative, from the top), set by LayOut for the screen's height: the spare height
        // is shared out between the blocks, so the page fills the screen as the concept's does.
        float dollTop, statsTop, materialsTop, gridTop, gap;

        void LayOut()
        {
            var used = HeaderHeight + MenuChrome.TitleHeight + DollHeight + StatsHeight + MaterialsHeight + GridFrameHeight + TabsHeight + 16f;
            var available = content != null ? content.rect.height : 0f;
            gap = Mathf.Clamp((available - used) / 5f, 12f, 120f);
            dollTop = -HeaderHeight - MenuChrome.TitleHeight;
            statsTop = dollTop - DollHeight - gap;
            materialsTop = statsTop - StatsHeight - gap;
            gridTop = materialsTop - MaterialsHeight - gap;
        }
        const float SlotRowHeight = 230f, SlotGap = 16f;

        void BuildDoll(GameSession current)
        {
            var doll = UiStyle.Framed(content, "Paper Doll", new Color(0.07f, 0.05f, 0.05f, 1f), 4f);
            var rect = doll.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, dollTop - DollHeight);
            rect.offsetMax = new Vector2(-16f, dollTop);
            var inside = UiStyle.FillOf(doll).transform;

            // Weapons and the head on the top row, the body below: bow, helm, amulet, ring, quiver; gloves, chest,
            // belt, ring, boots.
            var equipment = current.Equipment;
            var rows = new[]
            {
                new[] { ItemSlot.Weapon, ItemSlot.Helm, ItemSlot.Amulet, ItemSlot.Ring, ItemSlot.OffHand },
                new[] { ItemSlot.Gloves, ItemSlot.Chest, ItemSlot.Belt, ItemSlot.Ring2, ItemSlot.Boots },
            };
            for (var row = 0; row < rows.Length; row++)
                for (var column = 0; column < rows[row].Length; column++)
                    SlotInRow(inside, equipment, rows[row][column], column, rows[row].Length, row);
        }

        // A slot in the grid of slots: a fifth of the width, a row of SlotRowHeight, a gap all round.
        void SlotInRow(Transform parent, EquipmentState equipment, ItemSlot place, int column, int columns, int row)
        {
            var item = equipment.Get(place);
            var tile = ItemTile(parent, item, UiStyle.SlotLabel(place), 28);
            if (item == null)
                Ghost(tile, place);
            var rect = tile.rectTransform;
            rect.anchorMin = new Vector2(column / (float)columns, 1f);
            rect.anchorMax = new Vector2((column + 1) / (float)columns, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(SlotGap / 2f, -SlotGap - (row + 1) * SlotRowHeight - row * SlotGap);
            rect.offsetMax = new Vector2(-SlotGap / 2f, -SlotGap - row * (SlotRowHeight + SlotGap));
            tile.gameObject.AddComponent<ItemDropTarget>().Place = place;
            var rim = tile.transform.GetChild(0).GetComponent<Image>();
            slotRims[place] = (rim, rim.color);
            if (item != null)
            {
                tile.gameObject.AddComponent<Button>().onClick.AddListener(() => sheet.ShowEquipped(place));
                Draggable(tile, item, place);
            }
        }

        // --- Dragging -----------------------------------------------------------------------------------------------

        void Draggable(Image tile, Item item, ItemSlot? wornIn)
        {
            var drag = tile.gameObject.AddComponent<ItemDrag>();
            drag.Began = (d, e) => BeginDrag(d, e, item, wornIn);
            drag.Moved = (d, e) =>
            {
                MoveGhost(e);
                lastOver = TargetAt(e.position);
            };
            drag.Ended = (d, e) => Drop(e);
        }

        void BeginDrag(ItemDrag tile, PointerEventData eventData, Item item, ItemSlot? wornIn)
        {
            EndDrag();
            dragItem = item;
            dragFrom = wornIn;
            // A copy of the tile's picture follows the finger, over everything and blind to raycasts so the drop sees
            // what is under it.
            var canvas = panelRoot.GetComponentInParent<Canvas>().rootCanvas;
            var source = (RectTransform)tile.transform;
            var ghost = UiStyle.Image(canvas.transform, "Dragged Item", new Color(1f, 1f, 1f, 0.85f));
            ghost.sprite = ItemIcons.For(item);
            ghost.preserveAspect = true;
            ghost.raycastTarget = false;
            dragGhost = ghost.rectTransform;
            dragGhost.SetAsLastSibling();
            dragGhost.sizeDelta = source.rect.size * 1.1f;
            MoveGhost(eventData);

            // Light the places it fits; dim the rest. Worn gear lights the backpack instead.
            var places = EquipmentState.PlacesFor(item.Slot);
            foreach (var pair in slotRims)
                pair.Value.rim.color = wornIn == null && System.Array.IndexOf(places, pair.Key) >= 0 ? DropColor : NoDropColor;
            // Worn gear can go back in the backpack, and a backpack item can be moved within it (2026-10-18).
            if (backpackRim != null)
                backpackRim.color = DropColor;
        }

        void MoveGhost(PointerEventData eventData)
        {
            if (dragGhost == null)
                return;
            var canvasRect = (RectTransform)dragGhost.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out var local))
                dragGhost.localPosition = local;
        }

        void Drop(PointerEventData eventData)
        {
            var item = dragItem;
            var from = dragFrom;
            // Raycast at the release point: on iOS the touch's own raycast result is already cleared when the finger lifts
            // (found on the simulator, 2026-10-03), so it is asked here, falling back to what was under the finger at its last move.
            var target = TargetAt(eventData.position) ?? lastOver;
            EndDrag();
            if (item == null || target == null)
                return;

            var current = GameSession.Current;
            var done = false;
            if (from == null && !target.Backpack)
                done = current.EquipFromInventory(item, target.Place);
            else if (from != null && target.Backpack)
                done = current.Unequip(from.Value);
            // A backpack item dropped on the backpack moves there, centred on the finger, if those cells are free (the
            // owner, 2026-10-18: "make it so that the inventory can be manually organized").
            else if (from == null && target.Backpack && CellUnder(eventData.position, out var cell))
            {
                var size = ItemSize.Of(item);
                done = current.MoveInBackpack(item, new Vector2Int(cell.x - (size.x - 1) / 2, cell.y - (size.y - 1) / 2));
            }
            // The Bag redraws itself only for the item sheet's buttons, so a drop does it here; a refused drop just
            // restores the colors (EndDrag).
            if (!done)
                return;
            Sfx.Play(SoundId.Pickup);
            Refresh();
        }

        ItemDropTarget TargetAt(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return null;
            var pointer = new PointerEventData(eventSystem) { position = screenPosition };
            dropHits.Clear();
            eventSystem.RaycastAll(pointer, dropHits);
            foreach (var hit in dropHits)
            {
                var target = hit.gameObject.GetComponentInParent<ItemDropTarget>();
                if (target != null)
                    return target;
            }
            return null;
        }

        void EndDrag()
        {
            lastOver = null;
            dragItem = null;
            dragFrom = null;
            if (dragGhost != null)
                Destroy(dragGhost.gameObject);
            dragGhost = null;
            foreach (var pair in slotRims)
                if (pair.Value.rim != null)
                    pair.Value.rim.color = pair.Value.color;
            if (backpackRim != null)
                backpackRim.color = backpackRimColor;
        }

        /// <summary>An item's tile: framed in its rarity's color on a blood-dark fill, its icon (<see cref="ItemIcons"/>)
        /// and item level; or an empty place with the kind dimmed.</summary>
        // An empty slot shows a faint picture of what goes there, its name small along the bottom (2026-10-04).
        static void Ghost(Image tile, ItemSlot place)
        {
            var kind = place == ItemSlot.Ring2 ? ItemSlot.Ring : place == ItemSlot.OffHand ? ItemSlot.Shield : place;
            var sprite = ItemIcons.For(kind, ItemRarity.Common);
            if (sprite == null)
                return;
            var ghost = UiStyle.Image(tile.transform, "Ghost", new Color(0.75f, 0.68f, 0.6f, 0.16f));
            ghost.sprite = sprite;
            ghost.preserveAspect = true;
            UiStyle.Stretch(ghost.rectTransform, 22f);
            ghost.transform.SetAsFirstSibling();
            ghost.transform.SetSiblingIndex(1);
            var label = tile.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.alignment = TextAnchor.LowerCenter;
                label.rectTransform.offsetMin = new Vector2(label.rectTransform.offsetMin.x, 26f);
                label.fontSize = Mathf.RoundToInt(label.fontSize * 0.8f);
                label.color = new Color(0.55f, 0.5f, 0.45f);
                label.transform.SetAsLastSibling();
            }
        }

        static readonly Color WornFill = new Color(0.1f, 0.075f, 0.065f, 1f);

        static Image ItemTile(Transform parent, Item item, string emptyLabel, int size)
        {
            // A dark fill, the rarity in the frame (the concept art, 2026-10-19; a blood-red fill before).
            var tile = UiStyle.Framed(parent, item != null ? item.Slot + " Tile" : "Empty Tile", item != null ? WornFill : UiStyle.EmptySlotFill, 4f);
            tile.raycastTarget = true;
            var rim = tile.transform.GetChild(0).GetComponent<Image>();
            if (item == null)
            {
                var empty = UiStyle.Text(tile.transform, emptyLabel, size, new Color(0.4f, 0.36f, 0.33f), TextAnchor.MiddleCenter, true);
                UiStyle.Stretch(empty.rectTransform, 8f);
                return tile;
            }

            rim.color = Color.Lerp(LootColors.Of(item.Rarity), UiStyle.Frame, item.Rarity == ItemRarity.Common ? 0.35f : 0f);
            var icon = UiStyle.Image(tile.transform, "Icon", Color.white);
            icon.sprite = ItemIcons.For(item);
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

        const float StatsHeight = 200f;

        void BuildStats(GameSession current)
        {
            var panel = UiStyle.Framed(content, "Stats", UiStyle.Panel, 3f);
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, statsTop - StatsHeight);
            rect.offsetMax = new Vector2(-16f, statsTop);
            var inside = UiStyle.FillOf(panel).transform;

            var equipment = current.Equipment;
            var power = PowerScore.Evaluate(current);
            var maxLife = health != null ? health.MaxLife : CombatFormulas.CharacterLife(current.Level) + current.AttributeBonuses.Life + equipment.TotalLifeBonus;
            var life = health != null ? health.Fraction * maxLife : maxLife;
            var armor = health != null ? health.Armor : equipment.TotalArmor;
            var tree = current.Talents.Bonuses;
            var attributes = current.AttributeBonuses;
            var crit = equipment.CriticalChancePercent + tree.CriticalChance + attributes.CriticalChance;
            var speed = (equipment.AttackSpeedPercent / 100f + tree.AttackSpeed + attributes.AttackSpeed) * 100f;

            StatLine(inside, 0, 0, MenuIcons.Kind.Heart, "Life", $"{life:0} / {maxLife:0}");
            StatLine(inside, 1, 0, MenuIcons.Kind.Shield, "Defense", $"{armor:0}");
            StatLine(inside, 0, 1, MenuIcons.Kind.Sword, "Damage / s", $"{power.DamagePerSecond:0}");
            StatLine(inside, 1, 1, MenuIcons.Kind.Boot, "Move speed", $"+{equipment.MovementSpeedPercent + (tree.MoveSpeed + attributes.MoveSpeed) * 100f:0}%");
            StatLine(inside, 0, 2, MenuIcons.Kind.Speed, "Attack speed", $"+{speed:0}%");
            StatLine(inside, 1, 2, MenuIcons.Kind.Star, "Critical", $"{crit:0.#}%");

            var divider = UiStyle.Image(inside, "Divider", UiStyle.Frame);
            divider.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            divider.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            divider.rectTransform.sizeDelta = new Vector2(2f, -24f);
        }

        static void StatLine(Transform parent, int column, int row, MenuIcons.Kind kind, string name, string value)
        {
            var line = UiStyle.Rect(parent, name);
            line.anchorMin = new Vector2(column * 0.5f, 1f);
            line.anchorMax = new Vector2(column * 0.5f + 0.5f, 1f);
            line.pivot = new Vector2(0.5f, 1f);
            line.offsetMin = new Vector2(28f, -14f - 58f * (row + 1));
            line.offsetMax = new Vector2(-28f, -14f - 58f * row);
            // Each stat's icon at its left, as the concept art has it.
            var icon = MenuChrome.Icon(line, kind, 40f);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            var label = UiStyle.Text(line, name, 30, UiStyle.TextDim, TextAnchor.MiddleLeft, true);
            UiStyle.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(62f, 0f);
            var number = UiStyle.Text(line, value, 32, UiStyle.TextMain, TextAnchor.MiddleRight, true);
            UiStyle.Stretch(number.rectTransform);
        }

        const float MaterialsHeight = 96f;

        void BuildMaterials(GameSession current)
        {
            var panel = UiStyle.Framed(content, "Materials", UiStyle.Panel, 3f);
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, materialsTop - MaterialsHeight);
            rect.offsetMax = new Vector2(-16f, materialsTop);
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
            // The auto sort (2026-10-18): the backpack packed afresh, largest and best first.
            var sort = UiStyle.Button(inside, "Sort", () =>
            {
                current.SortBackpack();
                Sfx.Play(SoundId.Pickup, 0.6f);
                Refresh();
            }, 30);
            UiStyle.Place((RectTransform)sort.transform, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(220f, 72f));
            ((RectTransform)sort.transform).pivot = new Vector2(1f, 0.5f);
            var sortIcon = MenuChrome.Icon(sort.transform, MenuIcons.Kind.Sort, 34f, UiStyle.TextMain);
            sortIcon.rectTransform.anchorMin = sortIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            sortIcon.rectTransform.anchoredPosition = new Vector2(44f, 0f);
            sort.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(58f, 8f);
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
                else
                {
                    var pack = MenuChrome.Icon(cell, MenuIcons.Kind.Backpack, 46f);
                    pack.rectTransform.anchorMin = pack.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    pack.rectTransform.anchoredPosition = new Vector2(38f, 0f);
                }
                var text = UiStyle.Text(cell, $"{count} <size=22><color=#9E948A>{name}</color></size>", 30, UiStyle.TextMain, TextAnchor.MiddleLeft, true);
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(name != "Backpack" ? 56f : 78f, 0f);
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
            row.offsetMin = new Vector2(16f, materialsTop - MaterialsHeight);
            row.offsetMax = new Vector2(-16f, materialsTop);

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

        const float TabsHeight = 150f;

        // The backpack's frame hugs its grid (6 rows of cells, the padding, the frame's rim).
        const float GridFrameHeight = 6 * Cell + 5 * CellGap + 2 * GridPadding + 30f;

        void BuildGrid(GameSession current)
        {
            var frame = UiStyle.Framed(content, "Backpack", new Color(0.06f, 0.045f, 0.045f, 1f), 4f);
            frame.gameObject.AddComponent<ItemDropTarget>().Backpack = true;
            backpackRim = frame.transform.GetChild(0).GetComponent<Image>();
            backpackRimColor = backpackRim.color;
            var rect = frame.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, gridTop - GridFrameHeight);
            rect.offsetMax = new Vector2(-16f, gridTop);
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
            gridRect = grid;

            for (var y = 0; y < inventory.Rows; y++)
                for (var x = 0; x < inventory.Columns; x++)
                {
                    var cell = UiStyle.Image(grid, "Cell", UiStyle.EmptySlotFill * 1.6f);
                    UiStyle.Recessed(cell);
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
                Draggable(tile, item, null);
                if (PowerScore.IsUpgrade(current.Equipment, item, current.Level, current.Talents.Bonuses))
                {
                    var arrow = UiStyle.Text(tile.transform, "▲", 30, ItemSheet.GainColor, TextAnchor.MiddleCenter);
                    UiStyle.Place(arrow.rectTransform, new Vector2(1f, 1f), new Vector2(-6f, -4f), new Vector2(40f, 40f));
                }
            }
        }

        RectTransform gridRect;

        /// <summary>The backpack cell under a point of the screen (x right, y down from the top left), if any.</summary>
        bool CellUnder(Vector2 screenPosition, out Vector2Int cell)
        {
            cell = default;
            if (gridRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRect, screenPosition, null, out var local))
                return false;
            // The grid's pivot is its top middle.
            var fromLeft = local.x + gridRect.rect.width * 0.5f;
            var fromTop = -local.y - GridPadding;
            if (fromLeft < 0f || fromTop < 0f)
                return false;
            cell = new Vector2Int(Mathf.FloorToInt(fromLeft / (Cell + CellGap)), Mathf.FloorToInt(fromTop / (Cell + CellGap)));
            var inventory = GameSession.Current.Inventory;
            return cell.x < inventory.Columns && cell.y < inventory.Rows;
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

            // The talent trees (2026-10-07, in place of the loadout and skill points): a point a level from 2. Stats
            // (Docs/02, 2026-09-30): 5 points every level from 2.
            var skillPoints = current.Talents.Available(current.Level);
            var statPoints = current.Attributes.Available(current.Level);
            var tabs = new List<(string label, System.Action open, bool enabled, bool active)>
            {
                ("Inventory", null, true, true),
                (Features.Roguelite ? "Boons" : skillPoints > 0 ? $"Talents +{skillPoints}" : "Talents",
                    Features.Roguelite ? BoonListScreen.Open : (System.Action)TalentScreen.Open, true, false),
            };
            // The roguelite (2026-10-13): no stat points and no quests.
            if (!Features.Roguelite)
                tabs.Add((statPoints > 0 ? $"Stats +{statPoints}" : "Stats", StatsScreen.Open, true, false));
            if (Features.Quests)
                tabs.Add(("Quests", QuestLogScreen.Open, true, false));
            tabs.Add(("Codex", CodexScreen.Open, true, false));
            tabs.Add(("Settings", SettingsScreen.Open, true, false));
            for (var i = 0; i < tabs.Count; i++)
            {
                var (label, open, enabled, active) = tabs[i];
                var button = UiStyle.Button(bar, label, () => open?.Invoke(), 30, active ? UiStyle.Blood : UiStyle.Panel);
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(i / (float)tabs.Count, 0f);
                rect.anchorMax = new Vector2((i + 1) / (float)tabs.Count, 1f);
                rect.offsetMin = new Vector2(4f, 0f);
                rect.offsetMax = new Vector2(-4f, 0f);
                button.interactable = enabled;
                var text = button.GetComponentInChildren<Text>();
                // An icon over each tab's name, and a diamond under the open one (the concept art, 2026-10-19).
                text.fontSize = 28;
                text.alignment = TextAnchor.LowerCenter;
                text.rectTransform.offsetMin = new Vector2(8f, 26f);
                var icon = MenuChrome.Icon(button.transform, TabIcon(label), 54f, active ? UiStyle.Gold : MenuIcons.Tint);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(0f, -46f);
                if (active)
                {
                    var mark = MenuChrome.Diamond(button.transform, 12f, UiStyle.Gold);
                    mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    mark.rectTransform.anchoredPosition = new Vector2(0f, 16f);
                }
                if (active)
                    text.color = UiStyle.Gold;
                else if (!enabled)
                    text.color = new Color(0.4f, 0.36f, 0.33f);
                else if (label.Contains("+"))
                    text.color = new Color(1f, 0.71f, 0.35f);
            }
        }

        static MenuIcons.Kind TabIcon(string label) =>
            label.StartsWith("Inventory") ? MenuIcons.Kind.Backpack :
            label.StartsWith("Boons") || label.StartsWith("Talents") ? MenuIcons.Kind.Star :
            label.StartsWith("Stats") ? MenuIcons.Kind.Heart :
            label.StartsWith("Settings") ? MenuIcons.Kind.Cog : MenuIcons.Kind.Book;

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
                any = PowerScore.IsUpgrade(current.Equipment, items[i], current.Level, current.Talents.Bonuses);
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
