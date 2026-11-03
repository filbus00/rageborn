using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Wild Arrow Company's menu, in place of the town (the owner, 2026-11-03: "remove the town. Everything done in
    /// the town will now be done in a starting menu ... You select missions on a map ... The menu should be stylish, you
    /// should be seeing pictures of the vendors and the trainers. When a character dies. It is honored by being displayed
    /// on a grave list"). Covers the hub scene whenever it is loaded: the archer under contract at the top, then three
    /// pages: the contracts on a map (<see cref="ContractRules"/>), the company's quarters (the town's shops, each a card
    /// with its keeper's picture, opening the same sheets as before) and the grave stone of the fallen. The Bag and the
    /// settings are buttons at the bottom. Built in code once and kept.
    /// </summary>
    public class CompanyMenu : MonoBehaviour
    {
        enum Page { Contracts, Quarters, Fallen }

        static CompanyMenu current;

        Canvas canvas;
        RectTransform body;
        RectTransform header;
        RectTransform content;
        ScrollRect scroll;
        CharacterPortrait portrait;
        readonly List<(Page page, Image fill, Text label)> tabs = new List<(Page, Image, Text)>();
        Page page = Page.Contracts;
        string selected = ContractRules.All[0].Id;
        GameSession session;
        bool dirty;

        public static bool IsShown => current != null && current.canvas != null && current.canvas.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            current = null;
            if (!Features.CompanyMenu)
                return;
            var go = new GameObject("Company Menu");
            DontDestroyOnLoad(go);
            current = go.AddComponent<CompanyMenu>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneTravel.TownScene)
            {
                if (canvas != null)
                    canvas.enabled = false;
                return;
            }
            if (canvas == null)
                Build();
            if (session != GameSession.Current)
            {
                if (session != null)
                    session.Changed -= OnChanged;
                session = GameSession.Current;
                session.Changed += OnChanged;
            }
            // The contract on the map: the one she last took, or the newest open.
            selected = session.Company.ContractId;
            foreach (var contract in ContractRules.All)
                if (ContractRules.IsOpen(contract, session.Company.Cleared) && !session.Company.Cleared.Contains(contract.Id))
                {
                    selected = contract.Id;
                    break;
                }
            canvas.enabled = true;
            HideTheTown();
            Redraw();
        }

        // The hub scene still holds the town's player and HUD (the Bag lives on its canvas): the player is hidden and still.
        static void HideTheTown()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return;
            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            var physics = player.GetComponent<Rigidbody2D>();
            if (physics != null)
                physics.simulated = false;
        }

        void OnChanged() => dirty = true;

        void Update()
        {
            if (!dirty || canvas == null || !canvas.enabled)
                return;
            dirty = false;
            Redraw();
        }

        void Build()
        {
            var canvasObject = new GameObject("Company Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Over the hub's HUD (10) and its hints (45 sits above), under the sheets it opens (50) and the fade (100).
            canvas.sortingOrder = 40;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = UiStyle.Rect(canvasObject.transform, "Root");
            UiStyle.Stretch(root);
            var backdrop = UiStyle.Image(root, "Backdrop", UiStyle.Backdrop, true);
            UiStyle.Stretch(backdrop.rectTransform);
            if (UiTextures.Backdrop != null)
            {
                backdrop.sprite = UiTextures.Backdrop;
                backdrop.type = Image.Type.Tiled;
                backdrop.color = new Color(0.55f, 0.5f, 0.48f, 1f);
            }
            // A warm glow behind the title and a dark fall-off to the bottom, so the page has depth.
            if (UiTextures.Glow != null)
            {
                var glow = UiStyle.Image(root, "Glow", new Color(0.85f, 0.35f, 0.15f, 0.22f));
                glow.sprite = UiTextures.Glow;
                UiStyle.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(1500f, 900f));
            }
            // The page's contents inside the safe area (the backdrop above reaches the screen's edges).
            var safe = UiStyle.Rect(root, "Frame");
            UiStyle.Stretch(safe);
            body = UiStyle.Rect(safe, "Body");
            UiStyle.Stretch(body);
            SafeArea.WrapChildren(safe);

            // The title.
            var title = UiStyle.Text(body, "WILD ARROW", 132, UiStyle.Gold, TextAnchor.MiddleCenter, true, FontStyle.Bold);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiStyle.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1100f, 160f));
            var subtitle = UiStyle.Text(body, "M E R C E N A R Y   C O M P A N Y", 34, UiStyle.TextDim, TextAnchor.MiddleCenter, true);
            UiStyle.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(1100f, 50f));
            var rule = UiStyle.Image(body, "Rule", new Color(UiStyle.Gold.r, UiStyle.Gold.g, UiStyle.Gold.b, 0.6f));
            UiStyle.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(760f, 3f));
            var diamond = MenuChrome.Diamond(body, 22f, UiStyle.Gold);
            UiStyle.Place(diamond.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(22f, 22f));

            // The archer under contract.
            header = UiStyle.Rect(body, "Archer");
            UiStyle.Place(header, new Vector2(0.5f, 1f), new Vector2(0f, -305f), new Vector2(1080f, 330f));

            // The tabs.
            var tabRow = UiStyle.Rect(body, "Tabs");
            UiStyle.Place(tabRow, new Vector2(0.5f, 1f), new Vector2(0f, -660f), new Vector2(1080f, 110f));
            var names = new[] { (Page.Contracts, "Contracts"), (Page.Quarters, "Quarters"), (Page.Fallen, "The Fallen") };
            for (var i = 0; i < names.Length; i++)
            {
                var (tab, label) = names[i];
                var frame = UiStyle.Framed(tabRow, label, UiStyle.Panel, 3f);
                frame.raycastTarget = true;
                var rect = frame.rectTransform;
                rect.anchorMin = new Vector2(i / 3f, 0f);
                rect.anchorMax = new Vector2((i + 1) / 3f, 1f);
                rect.offsetMin = new Vector2(i == 0 ? 0f : 8f, 0f);
                rect.offsetMax = new Vector2(i == 2 ? 0f : -8f, 0f);
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = UiStyle.FillOf(frame);
                button.onClick.AddListener(() => Show(tab));
                var text = UiStyle.Text(frame.transform, label, 40, UiStyle.TextMain, TextAnchor.MiddleCenter, true);
                UiStyle.Stretch(text.rectTransform, 6f);
                tabs.Add((tab, UiStyle.FillOf(frame), text));
            }

            // The page, scrolling between the tabs and the buttons at the bottom.
            var area = new GameObject("Page", typeof(RectTransform), typeof(VerticalLayoutGroup));
            area.transform.SetParent(body, false);
            var areaRect = (RectTransform)area.transform;
            areaRect.anchorMin = new Vector2(0f, 0f);
            areaRect.anchorMax = new Vector2(1f, 1f);
            areaRect.offsetMin = new Vector2(45f, 230f);
            areaRect.offsetMax = new Vector2(-45f, -790f);
            var layout = area.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            content = SheetKit.ScrollList(area.transform, out scroll);

            // The Bag and the settings.
            var bag = UiStyle.Button(body, "The Bag", () => InventoryScreen.Current?.Open(), 44);
            UiStyle.Place((RectTransform)bag.transform, new Vector2(0.5f, 0f), new Vector2(-275f, 60f), new Vector2(520f, 140f));
            var settings = UiStyle.Button(body, "Settings", SettingsScreen.Open, 44);
            UiStyle.Place((RectTransform)settings.transform, new Vector2(0.5f, 0f), new Vector2(275f, 60f), new Vector2(520f, 140f));
        }

        void Show(Page next)
        {
            page = next;
            Redraw();
            scroll.verticalNormalizedPosition = 1f;
        }

        void Redraw()
        {
            if (session == null)
                return;
            DrawHeader();
            foreach (var (tab, fill, label) in tabs)
            {
                fill.color = tab == page ? UiStyle.Selected : UiStyle.Panel;
                label.color = tab == page ? UiStyle.Gold : UiStyle.TextMain;
            }
            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            switch (page)
            {
                case Page.Contracts:
                    DrawContracts();
                    break;
                case Page.Quarters:
                    DrawQuarters();
                    break;
                default:
                    DrawFallen();
                    break;
            }
        }

        // --- The archer ----------------------------------------------------------------------------------------------

        Text nameText, linesText, coinsText;

        void DrawHeader()
        {
            if (nameText == null)
            {
                var panel = UiStyle.Framed(header, "Archer Panel", SheetKit.CardFill, 4f);
                UiStyle.Stretch(panel.rectTransform);
                var picture = UiStyle.Framed(panel.transform, "Portrait", UiStyle.Sheet, 3f);
                UiStyle.Place(picture.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(290f, 290f));
                // The portrait sizes itself from its parent's size when made, so it gets a fixed square of its own.
                var square = UiStyle.Rect(UiStyle.FillOf(picture).transform, "Square");
                UiStyle.Place(square, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 270f));
                portrait = new CharacterPortrait(square, true) { Animated = true };
                nameText = UiStyle.Text(panel.transform, "", 50, UiStyle.Gold, TextAnchor.UpperLeft, true);
                UiStyle.Place(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(345f, -40f), new Vector2(715f, 80f));
                linesText = UiStyle.Text(panel.transform, "", 32, UiStyle.TextDim, TextAnchor.UpperLeft, true);
                UiStyle.Place(linesText.rectTransform, new Vector2(0f, 1f), new Vector2(345f, -125f), new Vector2(715f, 120f));
                coinsText = UiStyle.Text(panel.transform, "", 40, UiStyle.TextMain, TextAnchor.UpperLeft, true);
                UiStyle.Place(coinsText.rectTransform, new Vector2(0f, 1f), new Vector2(345f, -250f), new Vector2(715f, 60f));
            }
            var company = session.Company;
            nameText.text = company.Mercenary;
            linesText.text = $"Amazon archer of the company\nContracts cleared  {company.Cleared.Count} of {ContractRules.All.Length}" +
                             $"   ·   fallen  {company.Fallen.Count}";
            coinsText.text = $"<color=#DBB87A>{session.Gold:N0}</color>  coins in the company chest";
            portrait.Refresh(session.Equipment);
        }

        // --- The contracts -------------------------------------------------------------------------------------------

        // Where each contract stands on the map, as a fraction of its width and height from its bottom left: the
        // Thornwood above, the deeper places winding down below it.
        static readonly Vector2[] MapPlaces = { new Vector2(0.26f, 0.74f), new Vector2(0.72f, 0.6f), new Vector2(0.3f, 0.36f), new Vector2(0.72f, 0.15f) };
        static readonly string[] Numerals = { "I", "II", "III", "IV" };

        void DrawContracts()
        {
            var company = session.Company;
            SheetKit.Note(content, ContractRules.CompanyStory, 30, UiStyle.TextDim, TextAnchor.MiddleCenter);

            // The map.
            var mapObject = new GameObject("Map", typeof(RectTransform), typeof(LayoutElement));
            mapObject.transform.SetParent(content, false);
            mapObject.GetComponent<LayoutElement>().minHeight = 860f;
            var map = UiStyle.Framed(mapObject.transform, "Map Frame", new Color(0.1f, 0.12f, 0.08f, 1f), 4f);
            UiStyle.Stretch(map.rectTransform);
            var land = UiStyle.FillOf(map).rectTransform;
            var heading = UiStyle.Text(land, "THE DUCHY OF VELL  ·  AND WHAT LIES BENEATH", 28, new Color(0.75f, 0.68f, 0.5f, 0.8f), TextAnchor.MiddleCenter, true);
            UiStyle.Place(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 50f));
            // The road between them, a line of dots.
            for (var i = 0; i + 1 < MapPlaces.Length; i++)
            {
                var open = ContractRules.IsOpen(ContractRules.All[i + 1], company.Cleared);
                for (var k = 1; k < 12; k++)
                {
                    var p = Vector2.Lerp(MapPlaces[i], MapPlaces[i + 1], k / 12f);
                    var dot = UiStyle.Image(land, "Road", open ? new Color(0.86f, 0.72f, 0.48f, 0.7f) : new Color(0.4f, 0.38f, 0.34f, 0.5f));
                    dot.sprite = UiStyle.Circle;
                    var rect = dot.rectTransform;
                    rect.anchorMin = rect.anchorMax = p;
                    rect.sizeDelta = new Vector2(12f, 12f);
                    rect.anchoredPosition = Vector2.zero;
                }
            }
            for (var i = 0; i < ContractRules.All.Length; i++)
                DrawPlace(land, ContractRules.All[i], i);

            // The chosen contract.
            var chosen = ContractRules.Get(selected);
            var openNow = ContractRules.IsOpen(chosen, company.Cleared);
            var cleared = company.Cleared.Contains(chosen.Id);
            SheetKit.Heading(content, chosen.Name.ToUpperInvariant());
            SheetKit.Note(content, chosen.Region + $"   ·   depths {chosen.FirstDepth} to {chosen.LastDepth}", 30, UiStyle.Gold);
            SheetKit.Note(content, openNow ? chosen.Story : "Sealed. The company takes this contract once the one before it is cleared.", 32,
                UiStyle.TextMain);
            if (openNow)
                SheetKit.Note(content, "Bosses   " + string.Join("   ·   ", chosen.Bosses), 30, UiStyle.TextDim);
            var label = !openNow ? "Sealed" : cleared ? "Take It Again" : "Take the Contract";
            var button = SheetKit.ActionButton(content, label, openNow, !cleared && openNow, () => RunDirector.Embark(chosen));
            button.interactable = openNow;
            button.gameObject.AddComponent<LayoutElement>().minHeight = 150f;
        }

        void DrawPlace(RectTransform land, Contract contract, int index)
        {
            var company = session.Company;
            var open = ContractRules.IsOpen(contract, company.Cleared);
            var cleared = company.Cleared.Contains(contract.Id);
            var place = UiStyle.Rect(land, contract.Name);
            place.anchorMin = place.anchorMax = MapPlaces[index];
            place.sizeDelta = new Vector2(380f, 230f);
            place.anchoredPosition = Vector2.zero;
            if (contract.Id == selected && UiTextures.Glow != null)
            {
                var glow = UiStyle.Image(place, "Chosen", new Color(1f, 0.6f, 0.25f, 0.55f));
                glow.sprite = UiTextures.Glow;
                UiStyle.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 50f), new Vector2(260f, 260f));
            }
            var ring = UiStyle.Disc(place, "Ring", open ? UiStyle.Gold : new Color(0.35f, 0.33f, 0.3f, 1f));
            UiStyle.Place(ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(140f, 140f));
            ring.raycastTarget = true;
            var fill = UiStyle.Disc(ring.transform, "Fill", cleared ? new Color(0.22f, 0.32f, 0.16f, 1f) : open ? UiStyle.Selected : UiStyle.Locked);
            UiStyle.Stretch(fill.rectTransform, 9f);
            var numeral = UiStyle.Text(ring.transform, open ? Numerals[index] : "?", 58, open ? UiStyle.TextMain : UiStyle.TextDim, TextAnchor.MiddleCenter, true);
            UiStyle.Stretch(numeral.rectTransform);
            var button = ring.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var id = contract.Id;
            button.onClick.AddListener(() =>
            {
                selected = id;
                Redraw();
            });
            var name = UiStyle.Text(place, open ? contract.Name : "Unknown", 34, open ? UiStyle.TextMain : UiStyle.TextDim, TextAnchor.MiddleCenter, true);
            UiStyle.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(380f, 46f));
            if (cleared)
            {
                var stamp = UiStyle.Text(place, "CLEARED", 26, new Color(0.6f, 0.85f, 0.45f, 1f), TextAnchor.MiddleCenter, true, FontStyle.Bold);
                UiStyle.Place(stamp.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(380f, 34f));
            }
        }

        // --- The quarters --------------------------------------------------------------------------------------------

        static readonly (string name, string role, string line, string figure, Action open)[] Keepers =
        {
            ("Brunhilde", "Captain of the company", "Drills the archers: harder, tougher, quicker.", "trainer",
                () => UpgradeShopScreen.Open(MetaShop.Training, "Brunhilde")),
            ("Miranda", "The company's healer", "Potions for the road, more of them each time.", "healer",
                () => UpgradeShopScreen.Open(MetaShop.Potions, "Miranda")),
            ("Sylthara", "Informant", "Secrets of the trade: deeper boons on contract.", "gambler",
                () => UpgradeShopScreen.Open(MetaShop.Secrets, "Sylthara")),
            ("Garrett", "Quartermaster", "Outfits each new archer, and buys what you bring home.", "merchant",
                () => UpgradeShopScreen.Open(MetaShop.Outfitting, "Garrett")),
            ("Thorin", "Blacksmith", "Forges gear from the blueprints you recover.", "pellam", SmithScreen.Open),
            ("Elara", "Keeper of the vault", "Keeps what you store between contracts.", "stash_keeper", StashScreen.Open),
            ("Lyriel", "Beastmaster", "Wolves, ravens and boars to fight at your side.", "pet_vendor", PetVendorScreen.Open),
        };

        static readonly Dictionary<string, Sprite> Pictures = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Pictures.Clear();

        // A keeper's picture: the first frame of her or his baked idle, facing front.
        static Sprite Picture(string figure)
        {
            if (Pictures.TryGetValue(figure, out var sprite))
                return sprite;
            foreach (var candidate in Resources.LoadAll<Sprite>($"Characters/{figure}/{figure}_idle"))
                if (candidate.name.EndsWith("_s_00"))
                    sprite = candidate;
            Pictures[figure] = sprite;
            return sprite;
        }

        void DrawQuarters()
        {
            SheetKit.Note(content, "The company's people. Coins brought back from contracts buy what they offer, for good.", 30,
                UiStyle.TextDim, TextAnchor.MiddleCenter);
            foreach (var keeper in Keepers)
            {
                var card = new GameObject(keeper.name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                card.transform.SetParent(content, false);
                card.GetComponent<LayoutElement>().minHeight = 280f;
                var fill = card.GetComponent<Image>();
                fill.color = SheetKit.CardFill;
                UiStyle.Rim(card, 3f);
                var open = keeper.open;
                card.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Sfx.Play(SoundId.Hint);
                    open();
                });
                var frame = UiStyle.Framed(card.transform, "Picture", new Color(0.13f, 0.15f, 0.1f, 1f), 3f);
                UiStyle.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(240f, 240f));
                var sprite = Picture(keeper.figure);
                if (sprite != null)
                {
                    var picture = UiStyle.Image(UiStyle.FillOf(frame).transform, "Figure", Color.white);
                    picture.sprite = sprite;
                    picture.preserveAspect = true;
                    UiStyle.Stretch(picture.rectTransform, 6f);
                }
                var name = UiStyle.Text(card.transform, keeper.name, 52, UiStyle.Gold, TextAnchor.UpperLeft, true);
                UiStyle.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(295f, -28f), new Vector2(740f, 70f));
                var role = UiStyle.Text(card.transform, keeper.role.ToUpperInvariant(), 26, UiStyle.TextDim, TextAnchor.UpperLeft, true);
                UiStyle.Place(role.rectTransform, new Vector2(0f, 1f), new Vector2(295f, -100f), new Vector2(740f, 40f));
                var line = UiStyle.Text(card.transform, keeper.line, 32, UiStyle.TextMain, TextAnchor.UpperLeft, true);
                UiStyle.Place(line.rectTransform, new Vector2(0f, 1f), new Vector2(295f, -150f), new Vector2(740f, 110f));
            }
        }

        // --- The fallen ----------------------------------------------------------------------------------------------

        void DrawFallen()
        {
            var fallen = session.Company.Fallen;
            SheetKit.Heading(content, "THE GRAVE STONE");
            SheetKit.Note(content, "Cut into the stone in the company's yard: every archer who fell on a contract, the deepest first.", 30,
                UiStyle.TextDim);
            if (fallen.Count == 0)
            {
                SheetKit.Note(content, "No archer of the company has fallen yet. The stone is waiting.", 34, UiStyle.TextMain,
                    TextAnchor.MiddleCenter);
                return;
            }
            for (var i = 0; i < fallen.Count; i++)
            {
                var dead = fallen[i];
                var date = DateTimeOffset.FromUnixTimeSeconds(dead.Day * 86400L).UtcDateTime;
                var label = $"<size=44><color=#DBB87A>{i + 1}.  {dead.Name}</color></size>\n" +
                            $"<size=28>{dead.Contract}, depth {dead.Depth}   ·   {dead.Kills:N0} slain" +
                            (dead.Bosses > 0 ? $"   ·   {dead.Bosses} boss{(dead.Bosses > 1 ? "es" : "")}" : "") +
                            $"   ·   {RunSummary.Duration(dead.Seconds)}   ·   {date:d MMM yyyy}</size>";
                SheetKit.Card(content, label, null, false, null, 150f, accent: i == 0 ? UiStyle.Gold : SheetKit.Accent);
            }
        }
    }
}
