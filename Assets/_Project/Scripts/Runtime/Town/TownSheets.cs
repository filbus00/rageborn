using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The sheets the town's newcomers open (2026-10-05): the stash, the Gambler and the Trainer, in the Pet Vendor's
    /// look (<see cref="PetVendorScreen"/>): a dark backdrop that closes on a tap, a leather sheet with a title, a line
    /// under it, a scrolling list and a Close button. They do not pause the game (the town has no enemies). Each kind of
    /// sheet is made the first time it opens and kept.
    /// </summary>
    public abstract class TownSheets : MonoBehaviour
    {
        static TownSheets openSheet;

        protected RectTransform content;
        protected Text subtitle;

        public static bool AnyOpen => openSheet != null && openSheet.gameObject.activeSelf;

        public static void CloseAny()
        {
            if (AnyOpen)
                openSheet.Close();
        }

        public void Close() => gameObject.SetActive(false);

        /// <summary>A sheet that cannot be put away without a choice (no Close, the backdrop does nothing).</summary>
        protected virtual bool MustChoose => false;

        void CloseByTap()
        {
            if (!MustChoose)
                Close();
        }

        protected void Show()
        {
            gameObject.SetActive(true);
            openSheet = this;
            Rerender();
        }

        /// <summary>Fills <see cref="content"/> and <see cref="subtitle"/> from the session.</summary>
        protected abstract void Render();

        protected void Rerender()
        {
            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            Render();
        }

        protected static T Make<T>(string title) where T : TownSheets
        {
            var canvasObject = new GameObject(title + " Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<T>();
            root.GetComponent<Button>().onClick.AddListener(screen.CloseByTap);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Button));
            sheet.transform.SetParent(root.transform, false);
            sheet.GetComponent<Button>().transition = Selectable.Transition.None;
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 2060f);
            sheet.GetComponent<Image>().color = UiStyle.Sheet;
            UiStyle.Leather(sheet.GetComponent<Image>());
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 48);
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var heading = Label(sheet.transform, title, 56, FontStyle.Bold, TextAnchor.MiddleCenter);
            heading.color = UiStyle.Gold;
            Height(heading.gameObject, 90f);
            screen.subtitle = Label(sheet.transform, "", 32, FontStyle.Normal, TextAnchor.MiddleCenter);
            Height(screen.subtitle.gameObject, 96f);

            // The list scrolls inside the sheet, masked, so a long one never pushes Close off the screen.
            var view = new GameObject("View", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
            view.transform.SetParent(sheet.transform, false);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            view.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var list = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(view.transform, false);
            var listRect = (RectTransform)list.transform;
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.offsetMin = listRect.offsetMax = Vector2.zero;
            var listLayout = list.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.GetComponent<ScrollRect>();
            scroll.content = listRect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            screen.content = listRect;

            var close = Button(sheet.transform, "Close", screen.Close, 40);
            Height(close.gameObject, 150f);
            close.gameObject.SetActive(!screen.MustChoose);
            return screen;
        }

        /// <summary>A row: a label on the left and one button on the right.</summary>
        protected void Row(string label, string action, bool enabled, Action onClick, float height = 150f, float buttonWidth = 320f)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(content, false);
            row.GetComponent<Image>().color = UiStyle.RowFill;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 12, 8, 8);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            Height(row, height);

            var text = Label(row.transform, label, 34, FontStyle.Normal, TextAnchor.MiddleLeft);
            Width(text.gameObject, 0f, 1f);
            var button = Button(row.transform, action, () =>
            {
                onClick?.Invoke();
                Rerender();
            }, 30);
            Width(button.gameObject, buttonWidth, 0f);
            button.interactable = enabled;
        }

        /// <summary>A heading line inside the list.</summary>
        protected void Heading(string label)
        {
            var heading = Label(content, label, 38, FontStyle.Bold, TextAnchor.MiddleLeft);
            heading.color = UiStyle.Gold;
            Height(heading.gameObject, 70f);
        }

        /// <summary>A line of text, no button.</summary>
        protected void Line(string text, int size = 32, float height = 56f)
        {
            var line = Label(content, text, size, FontStyle.Normal, TextAnchor.MiddleLeft);
            Height(line.gameObject, height);
        }

        /// <summary>A wrapping grid of item tiles; a tap on one calls <paramref name="onTap"/> with it.</summary>
        protected void Tiles(System.Collections.Generic.IReadOnlyList<Item> items, Action<Item> onTap, string empty)
        {
            if (items.Count == 0)
            {
                var none = Label(content, empty, 30, FontStyle.Italic, TextAnchor.MiddleCenter);
                none.color = UiStyle.TextDim;
                Height(none.gameObject, 90f);
                return;
            }
            const float tile = 168f;
            var grid = new GameObject("Tiles", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(content, false);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(tile, tile);
            layout.spacing = new Vector2(10f, 10f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 6;
            var rows = (items.Count + 5) / 6;
            Height(grid, rows * tile + (rows - 1) * 10f);
            foreach (var item in items)
            {
                var go = new GameObject("Tile", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(grid.transform, false);
                var frame = go.GetComponent<Image>();
                frame.color = Color.Lerp(UiStyle.RowFill, LootColors.Of(item.Rarity), 0.35f);
                var captured = item;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    onTap(captured);
                    Rerender();
                });
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(go.transform, false);
                var image = icon.GetComponent<Image>();
                image.sprite = ItemIcons.For(item);
                image.preserveAspect = true;
                image.raycastTarget = false;
                var rect = (RectTransform)icon.transform;
                rect.anchorMin = new Vector2(0.1f, 0.22f);
                rect.anchorMax = new Vector2(0.9f, 0.95f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var level = Label(go.transform, $"{item.ItemLevel}", 26, FontStyle.Bold, TextAnchor.LowerRight);
                level.color = LootColors.Of(item.Rarity);
                var levelRect = level.rectTransform;
                levelRect.anchorMin = Vector2.zero;
                levelRect.anchorMax = Vector2.one;
                levelRect.offsetMin = new Vector2(6f, 4f);
                levelRect.offsetMax = new Vector2(-8f, -4f);
            }
        }

        protected static Button Button(Transform parent, string label, Action onClick, int size)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = UiStyle.ButtonFill;
            UiStyle.Rim(go);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var text = Label(go.transform, label, size, FontStyle.Bold, TextAnchor.MiddleCenter);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);
            return go.GetComponent<Button>();
        }

        protected static Text Label(Transform parent, string contents, int size, FontStyle style, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiStyle.Title;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = UiStyle.TextMain;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = contents;
            return text;
        }

        protected static void Height(GameObject go, float height)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }

        protected static void Width(GameObject go, float width, float flexible)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = flexible;
        }

        protected static string Gold(int amount) => $"<color=#E8C66A>{amount:N0} gold</color>";
    }

    /// <summary>The Stash Keeper's chest (2026-10-05): the stash and the backpack, a tap moving an item from one to the
    /// other.</summary>
    public class StashScreen : TownSheets
    {
        static StashScreen current;

        public static void Open()
        {
            if (current == null)
                current = Make<StashScreen>("Stash");
            current.Show();
        }

        protected override void Render()
        {
            var session = GameSession.Current;
            // The stash chest (the owner, 2026-10-15): slots bought with coins; items go in and out freely in town, and
            // what is in it is safe from death. What she wears or carries into the dungeon is lost if she dies there.
            var slots = session.Meta.StashSlots;
            subtitle.text = $"{session.Stash.Count} of {slots} slots used.  {Gold(session.Gold)}.  Tap an item to move it.";
            var slot = MetaRules.Get(MetaUpgradeId.StashSlot);
            var maxed = session.Meta.IsMaxed(MetaUpgradeId.StashSlot);
            var cost = session.Meta.NextCost(MetaUpgradeId.StashSlot);
            Row($"<b>{slot.Name}</b>\n<size=28><color=#CCCCCC>{MetaProgress.StashSlotsPerRank} more slots. The chest keeps what is in it through every death.</color></size>",
                maxed ? "All bought" : $"Buy {cost:N0}", !maxed && session.Gold >= cost, () =>
                {
                    if (session.BuyUpgrade(MetaUpgradeId.StashSlot))
                        Sfx.Play(SoundId.LevelUp, 0.6f);
                    Rerender();
                });
            Heading("In the chest");
            Tiles(session.Stash, item =>
            {
                if (session.TakeFromStash(item))
                    Sfx.Play(SoundId.Pickup);
            }, "Nothing kept yet.");
            Heading("In the backpack");
            Tiles(session.Inventory.Items, item =>
            {
                if (session.StoreInStash(item))
                    Sfx.Play(SoundId.Pickup);
            }, "The backpack is empty.");
        }
    }

    /// <summary>
    /// The run summary (2026-10-15): when a run ends, the town opens on what it came to (<see cref="RunSummary"/>): how it
    /// ended, the depth, time, kills, coins, items and blueprints, and the boons she had.
    /// </summary>
    public class RunSummaryScreen : TownSheets
    {
        static RunSummaryScreen current;
        RunSummary summary;

        public static void Open(RunSummary summary)
        {
            if (current == null)
                current = Make<RunSummaryScreen>("Run Summary");
            current.summary = summary;
            current.Show();
            Sfx.Play(summary.End == RunEnd.Died ? SoundId.Death : SoundId.LevelUp, 0.6f);
        }

        protected override void Render()
        {
            if (summary == null)
                return;
            var color = summary.End == RunEnd.Died ? "#D06050" : "#E8C66A";
            subtitle.text = $"<color={color}><b>{summary.Title}</b></color>\n<size=28>{summary.Headline}</size>";
            foreach (var line in summary.Lines())
                Line(line);
            Heading(summary.Boons.Count > 0 ? $"Boons ({summary.Boons.Count})" : "No boons taken");
            foreach (var (name, rank) in summary.Boons)
                Line(rank > 1 ? $"{name}  <color=#E8C66A>{rank}</color>" : name, 30, 46f);
            Line("<size=28><color=#999999>Coins buy upgrades in town; the chest keeps what you put in it.</color></size>", 28, 80f);
        }
    }

    /// <summary>
    /// Thorin's forge (2026-10-15): every blueprint he has learned, forged for coins into a Rare piece that goes into the
    /// backpack and is the kit she starts a run in after a death (<see cref="GameSession.Forge"/>).
    /// </summary>
    public class SmithScreen : TownSheets
    {
        static SmithScreen current;
        readonly System.Collections.Generic.List<string> keys = new System.Collections.Generic.List<string>();

        public static void Open()
        {
            if (current == null)
                current = Make<SmithScreen>(SmithNpc.Name);
            current.Show();
        }

        protected override void Render()
        {
            var session = GameSession.Current;
            subtitle.text = $"{Gold(session.Gold)}.  Bring blueprints home through the portal and I learn them for good.";
            keys.Clear();
            keys.AddRange(session.Meta.Blueprints);
            keys.Sort(System.StringComparer.Ordinal);
            if (keys.Count == 0)
            {
                Row("<b>No blueprints yet</b>\n<size=28><color=#CCCCCC>They drop now and then, most often from bosses and elites.</color></size>",
                    "Farewell", true, Close);
                return;
            }
            foreach (var key in keys)
            {
                if (!BlueprintRules.TryParse(key, out var kind, out var look))
                    continue;
                var cost = BlueprintRules.ForgeCost(look);
                var kit = session.Meta.ForgedFor(kind);
                var now = kit != null ? $"Starting kit: {ItemComparison.Name(kit)}, item level {kit.ItemLevel}." : "Not yet forged.";
                var captured = key;
                Row($"<b>{ItemNames.LookName(kind, look)}</b>  <size=26><color=#999999>{ItemComparison.KindName(kind)}</color></size>\n" +
                    $"<size=28><color=#CCCCCC>{now}</color></size>",
                    $"Forge {cost:N0}", session.Gold >= cost && session.Inventory.CanFit(new Item(kind, ItemRarity.Rare, 1)), () =>
                    {
                        var item = session.Forge(captured);
                        if (item != null)
                        {
                            Sfx.Play(SoundId.LevelUp, 0.7f);
                            (HintBanner.Current != null ? HintBanner.Current : HintBanner.Create()).Show($"Thorin forged a {ItemComparison.Name(item)}. It is in your bag.");
                        }
                        Rerender();
                    });
            }
        }
    }

    /// <summary>
    /// A townsperson's upgrades (the roguelite, 2026-10-13): Brunhilde's training, Miranda's brews, Sylthara's secrets of the
    /// boons, Garrett's outfitting. Each row the upgrade, what it gives now and next, and its price in coins.
    /// </summary>
    public class UpgradeShopScreen : TownSheets
    {
        static readonly System.Collections.Generic.Dictionary<string, UpgradeShopScreen> screens =
            new System.Collections.Generic.Dictionary<string, UpgradeShopScreen>();

        MetaShop shop;

        public static void Open(MetaShop shop, string title)
        {
            if (!screens.TryGetValue(title, out var screen) || screen == null)
                screens[title] = screen = Make<UpgradeShopScreen>(title);
            screen.shop = shop;
            screen.Show();
        }

        protected override void Render()
        {
            var session = GameSession.Current;
            subtitle.text = $"{Gold(session.Gold)}.  Upgrades last for every run.";
            foreach (var upgrade in MetaRules.All)
            {
                if (upgrade.Shop != shop)
                    continue;
                var rank = session.Meta.Rank(upgrade.Id);
                var maxed = rank >= upgrade.MaxRanks;
                var cost = session.Meta.NextCost(upgrade.Id);
                var now = rank > 0 ? MetaRules.Describe(upgrade, rank) : "Not yet bought.";
                var next = maxed ? "" : $"\n<size=26><color=#E8C66A>Next: {MetaRules.Describe(upgrade, rank + 1)}</color></size>";
                var captured = upgrade.Id;
                Row($"<b>{upgrade.Name}</b>  <size=28>({rank}/{upgrade.MaxRanks})</size>\n<size=28><color=#CCCCCC>{now}</color></size>{next}",
                    maxed ? "All bought" : $"Buy {cost:N0}", !maxed && session.Gold >= cost, () =>
                    {
                        if (session.BuyUpgrade(captured))
                            Sfx.Play(SoundId.LevelUp, 0.6f);
                        Rerender();
                    }, 190f);
            }
        }
    }

    /// <summary>
    /// The boon choice (the roguelite, 2026-10-13): on each new depth three boons (four with A Fourth Path), drawn from the
    /// talent nodes she can take another rank of; she takes one. Rerolls bought from Sylthara draw again. The game waits
    /// while it is open.
    /// </summary>
    public class BoonScreen : TownSheets
    {
        static BoonScreen current;
        static readonly System.Random random = new System.Random();
        readonly System.Collections.Generic.List<string> offer = new System.Collections.Generic.List<string>();
        float savedTimeScale = 1f;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        // A boon owed is taken before the run goes on.
        protected override bool MustChoose => true;

        public static void Open()
        {
            if (current == null)
                current = Make<BoonScreen>("Choose a Boon");
            current.Draw();
            current.savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            current.Show();
        }

        void OnDisable()
        {
            if (Time.timeScale == 0f)
                Time.timeScale = savedTimeScale;
        }

        void Draw()
        {
            var session = GameSession.Current;
            offer.Clear();
            offer.AddRange(BoonRules.Offer(session.Talents, session.Meta.MaxBoonTier, session.Meta.BoonChoices, random));
        }

        protected override void Render()
        {
            var session = GameSession.Current;
            subtitle.text = $"Depth {session.DeepestDepth}.  {(session.PendingBoons > 1 ? session.PendingBoons + " boons owed." : "One boon.")}";
            if (offer.Count == 0)
            {
                Row("<b>Nothing left to learn</b>", "Go on", true, () =>
                {
                    session.PendingBoons = 0;
                    Close();
                });
                return;
            }
            foreach (var id in offer)
            {
                var node = TalentRules.Get(id);
                var rank = session.Talents.RankOf(id);
                var ranks = node.MaxRanks > 1 ? $"  <size=28>({rank + 1}/{node.MaxRanks})</size>" : "";
                if (node.IsRare)
                    ranks += "  <size=28><color=#E0B040>Rare</color></size>";
                var captured = id;
                Row($"<b>{node.Name}</b>{ranks}\n<size=28><color=#CCCCCC>{TalentRules.Describe(node, rank + 1)}</color></size>", "Take", true, () =>
                {
                    if (!session.TakeBoon(captured))
                        return;
                    Sfx.Play(SoundId.LevelUp, 0.8f);
                    if (session.PendingBoons > 0)
                    {
                        Draw();
                        Rerender();
                    }
                    else
                        Close();
                }, 190f);
            }
            if (session.RerollsLeft > 0)
                Row($"<b>Reroll</b>\n<size=28><color=#CCCCCC>{session.RerollsLeft} left this run.</color></size>", "Reroll", true, () =>
                {
                    if (session.UseReroll())
                    {
                        Draw();
                        Rerender();
                    }
                });
        }
    }

    /// <summary>The Gambler (2026-10-05): an item of a chosen kind for gold, its rarity a gamble (<see cref="GambleRules"/>).</summary>
    public class GamblerScreen : TownSheets
    {
        static GamblerScreen current;
        readonly System.Random random = new System.Random();
        string lastBought;

        public static void Open()
        {
            if (current == null)
                current = Make<GamblerScreen>("Sylthara");
            current.lastBought = null;
            current.Show();
        }

        protected override void Render()
        {
            var session = GameSession.Current;
            var itemLevel = GambleRules.ItemLevel(session.DeepestDepth);
            subtitle.text = lastBought ?? $"{Gold(session.Gold)}.  Item level {itemLevel}: mostly Magic, sometimes Rare, rarely Legendary.";
            foreach (var kind in GambleRules.Offered)
            {
                var price = GambleRules.Price(kind, itemLevel);
                var captured = kind;
                Row($"<b>{ItemComparison.KindName(kind)}</b>", $"Buy {price:N0}", session.Gold >= price, () =>
                {
                    if (session.Gamble(captured, random, out var item))
                    {
                        Sfx.Play(item.Rarity == ItemRarity.Legendary ? SoundId.DropLegendary : item.Rarity == ItemRarity.Rare ? SoundId.DropRare : SoundId.DropMagic);
                        var color = ColorUtility.ToHtmlStringRGB(LootColors.Of(item.Rarity));
                        lastBought = $"You got <color=#{color}>{ItemComparison.Name(item)}</color>.  {Gold(session.Gold)} left.";
                    }
                    else
                        lastBought = "No room in the backpack for it.";
                }, 130f);
            }
        }
    }

    /// <summary>The Trainer (2026-10-05): every skill point, or every stat point, back for a fee (<see cref="RespecRules"/>).</summary>
    public class TrainerScreen : TownSheets
    {
        static TrainerScreen current;

        public static void Open()
        {
            if (current == null)
                current = Make<TrainerScreen>("Brunhilde");
            current.Show();
        }

        protected override void Render()
        {
            var session = GameSession.Current;
            subtitle.text = $"{Gold(session.Gold)}.  Spend the points again in the Bag.";
            var skills = RespecRules.SkillsPrice(session.Level);
            Row($"<b>Reset talent points</b>\n<size=28><color=#CCCCCC>{session.Talents.Spent} spent</color></size>", $"Reset {skills:N0}",
                session.Gold >= skills && session.Talents.Spent > 0, () =>
                {
                    if (session.RespecSkills())
                        Sfx.Play(SoundId.LevelUp, 0.6f);
                });
            var stats = RespecRules.StatsPrice(session.Level);
            Row($"<b>Reset stat points</b>\n<size=28><color=#CCCCCC>{session.Attributes.TotalSpent} spent</color></size>", $"Reset {stats:N0}",
                session.Gold >= stats && session.Attributes.TotalSpent > 0, () =>
                {
                    if (session.RespecStats())
                        Sfx.Play(SoundId.LevelUp, 0.6f);
                });
        }
    }
}
