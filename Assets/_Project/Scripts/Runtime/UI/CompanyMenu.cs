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
    /// settings are buttons at the bottom. Built in code once and kept. Remade as pixel art after the owner's two concept
    /// pictures (2026-11-05, ArtSource/ui/concepts; <see cref="PixelUi"/>): the WILD ARROW plaque, her painted portrait
    /// beside her name, contracts, fallen and coins, red and green tabs, keeper cards with painted portraits, signs and
    /// emblems, and the grave stone with its epitaph.
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
        readonly List<(Page page, Image frame, Text label)> tabs = new List<(Page, Image, Text)>();
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

        // Menu pixels (PixelUi): the drawn frames' layout after the owner's concept pictures (2026-11-05).
        const float Edge = 6f;
        const float TopOfTabs = 238f;
        const float TabHeight = 22f;
        const float ButtonHeight = 40f;

        void Build()
        {
            canvas = PixelUi.MakeCanvas(transform, "Company Menu Canvas", 40);
            // Over the hub's HUD (10) and its hints (45 sits above), under the sheets it opens (50) and the fade (100).

            var root = UiStyle.Rect(canvas.transform, "Root");
            UiStyle.Stretch(root);
            var backdrop = PixelUi.Tiled(root, "Backdrop", "backdrop");
            backdrop.raycastTarget = true;
            UiStyle.Stretch(backdrop.rectTransform);
            // The page's contents inside the safe area (the backdrop and the screen's frame reach the screen's edges).
            var safe = UiStyle.Rect(root, "Frame");
            UiStyle.Stretch(safe);
            body = UiStyle.Rect(safe, "Body");
            UiStyle.Stretch(body);
            SafeArea.WrapChildren(safe);
            UiStyle.Stretch(PixelUi.Frame(root, "Screen Edge", "frame_screen_b4").rectTransform, 1f);

            // The title plaque: WILD ARROW between two arrows, MERCENARY COMPANY under it between two feathered arrows.
            var plaque = PixelUi.Frame(body, "Title", "frame_plaque_b5").rectTransform;
            PixelUi.Across(plaque, Edge, 52f, Edge, Edge);
            var title = PixelUi.Label(plaque, "WILD ARROW", PixelUi.TitleBigFont, PixelUi.Gold, TextAnchor.UpperCenter);
            PixelUi.Across(title.rectTransform, 8f, 24f, 0f, 0f);
            var subtitle = PixelUi.Label(plaque, "MERCENARY COMPANY", PixelUi.BodyFont, PixelUi.Gold, TextAnchor.UpperCenter);
            PixelUi.Across(subtitle.rectTransform, 34f, 10f, 0f, 0f);
            var subtitleHalf = subtitle.preferredWidth / 2f;
            UiStyle.Place(PixelUi.Picture(plaque, "Feather Left", "small_arrow").rectTransform, new Vector2(0.5f, 1f),
                new Vector2(-subtitleHalf - 12f, -37f), new Vector2(11f, 3f));
            var right = PixelUi.Picture(plaque, "Feather Right", "small_arrow").rectTransform;
            UiStyle.Place(right, new Vector2(0.5f, 1f), new Vector2(subtitleHalf + 12f, -37f), new Vector2(11f, 3f));
            right.localScale = new Vector3(-1f, 1f, 1f);
            var leftArrow = PixelUi.Picture(plaque, "Arrow Left", "title_arrow").rectTransform;
            UiStyle.Place(leftArrow, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(16f, 10f));
            var rightArrow = PixelUi.Picture(plaque, "Arrow Right", "title_arrow").rectTransform;
            UiStyle.Place(rightArrow, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(16f, 10f));
            rightArrow.localScale = new Vector3(-1f, 1f, 1f);
            leftArrow.pivot = rightArrow.pivot = new Vector2(0.5f, 0.5f);
            leftArrow.anchoredPosition = new Vector2(18f, 0f);
            rightArrow.anchoredPosition = new Vector2(-18f, 0f);

            // The archer under contract.
            header = UiStyle.Rect(body, "Archer");
            PixelUi.Across(header, 64f, 169f, Edge, Edge);

            // The tabs.
            var tabRow = UiStyle.Rect(body, "Tabs");
            PixelUi.Across(tabRow, TopOfTabs, TabHeight, Edge, Edge);
            var names = new[] { (Page.Contracts, "CONTRACTS"), (Page.Quarters, "QUARTERS"), (Page.Fallen, "THE FALLEN") };
            for (var i = 0; i < names.Length; i++)
            {
                var (tab, label) = names[i];
                var frame = PixelUi.Frame(tabRow, label, "frame_tab_b3", true);
                var rect = frame.rectTransform;
                rect.anchorMin = new Vector2(i / 3f, 0f);
                rect.anchorMax = new Vector2((i + 1) / 3f, 1f);
                rect.offsetMin = new Vector2(i == 0 ? 0f : 2f, 0f);
                rect.offsetMax = new Vector2(i == 2 ? 0f : -2f, 0f);
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                button.onClick.AddListener(() =>
                {
                    Sfx.Play(SoundId.Hint);
                    Show(tab);
                });
                var text = PixelUi.Label(frame.transform, label, PixelUi.TitleFont, PixelUi.TextDim, TextAnchor.MiddleCenter);
                UiStyle.Stretch(text.rectTransform);
                text.rectTransform.anchoredPosition = new Vector2(0f, 1f);
                tabs.Add((tab, frame, text));
            }

            // The page, scrolling between the tabs and the buttons at the bottom.
            var viewport = UiStyle.Rect(body, "Page");
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(Edge, Edge + ButtonHeight + 6f);
            viewport.offsetMax = new Vector2(-Edge, -(TopOfTabs + TabHeight + 6f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            content = UiStyle.Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 12f;

            // The Bag and the settings.
            BottomButton("THE BAG", "icon_bag", 0, () => InventoryScreen.Current?.Open());
            BottomButton("SETTINGS", "icon_gear", 1, SettingsScreen.Open);
        }

        void BottomButton(string label, string icon, int side, Action onClick)
        {
            var frame = PixelUi.Frame(body, label, "frame_button_b5", true);
            var rect = frame.rectTransform;
            rect.anchorMin = new Vector2(side * 0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f + side * 0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(side == 0 ? Edge : 3f, Edge);
            rect.offsetMax = new Vector2(side == 0 ? -3f : -Edge, Edge + ButtonHeight);
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => onClick());
            var picture = PixelUi.Picture(frame.transform, "Icon", icon).rectTransform;
            UiStyle.Place(picture, new Vector2(0f, 0.5f), new Vector2(10f, 0f), picture.sizeDelta);
            var text = PixelUi.Label(frame.transform, label, PixelUi.TitleFont, PixelUi.Gold, TextAnchor.MiddleCenter);
            UiStyle.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(picture.sizeDelta.x + 14f, 0f);
            text.rectTransform.anchoredPosition += new Vector2(0f, 1f);
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
            foreach (var (tab, frame, label) in tabs)
            {
                frame.sprite = PixelUi.Sprite(tab == page ? "frame_tab_on_b3" : "frame_tab_b3");
                label.color = tab == page ? PixelUi.Gold : PixelUi.TextDim;
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

        // A block in the page's list, this tall (menu pixels), on a drawn frame (or none).
        RectTransform Block(string name, float height, string frame = "frame_card_b3")
        {
            RectTransform rect;
            if (frame != null)
                rect = PixelUi.Frame(content, name, frame).rectTransform;
            else
                rect = UiStyle.Rect(content, name);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            return rect;
        }

        // A framed card whose rows (texts, rules) stack inside it and size it, so wrapped text sets its height.
        RectTransform Stack(Transform parent, string name, string frame = "frame_card_b3", int pad = 8, float spacing = 3f)
        {
            var rect = frame != null ? PixelUi.Frame(parent, name, frame).rectTransform : UiStyle.Rect(parent, name);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        static void StackRule(Transform stack)
        {
            var rule = PixelUi.Tiled(stack, "Rule", "rule_dim");
            var element = rule.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 1f;
        }

        // --- The archer ----------------------------------------------------------------------------------------------

        Text nameText, contractsText, coinsText;

        void DrawHeader()
        {
            if (nameText == null)
            {
                // Her portrait (the owner's concept picture), framed in gold.
                var frame = PixelUi.Frame(header, "Portrait", "frame_portrait_b3").rectTransform;
                PixelUi.At(frame, 0f, 0f, 139f, 169f);
                var picture = PixelUi.Picture(frame, "Archer", "portrait_archer").rectTransform;
                UiStyle.Place(picture, new Vector2(0.5f, 0.5f), Vector2.zero, picture.sizeDelta);
                var panel = PixelUi.Frame(header, "Panel", "frame_panel_b4").rectTransform;
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.one;
                panel.offsetMin = new Vector2(145f, 0f);
                panel.offsetMax = Vector2.zero;
                nameText = PixelUi.Label(panel, "", PixelUi.TitleFont, PixelUi.Gold);
                PixelUi.Across(nameText.rectTransform, 12f, 24f, 9f, 6f);
                var role = PixelUi.Label(panel, "Amazon Archer of the Company", PixelUi.BodyFont, PixelUi.TextDim);
                PixelUi.Across(role.rectTransform, 38f, 12f, 9f, 6f);
                PixelUi.Across(PixelUi.Rule(panel, "Rule"), 60f, 5f, 9f, 9f);
                UiStyle.Place(PixelUi.Picture(panel, "Skull", "icon_skull").rectTransform, new Vector2(0f, 1f), new Vector2(9f, -73f), new Vector2(9f, 9f));
                contractsText = PixelUi.Label(panel, "", PixelUi.BodyFont, PixelUi.Text);
                PixelUi.Across(contractsText.rectTransform, 74f, 24f, 22f, 6f);
                PixelUi.Across(PixelUi.Rule(panel, "Rule"), 102f, 5f, 9f, 9f);
                UiStyle.Place(PixelUi.Picture(panel, "Coin", "icon_coin").rectTransform, new Vector2(0f, 1f), new Vector2(9f, -115f), new Vector2(9f, 9f));
                coinsText = PixelUi.Label(panel, "", PixelUi.BodyFont, PixelUi.Gold);
                PixelUi.Across(coinsText.rectTransform, 116f, 24f, 22f, 6f);
                PixelUi.Across(PixelUi.Rule(panel, "Rule"), 146f, 5f, 9f, 9f);
            }
            var company = session.Company;
            nameText.text = company.Mercenary.ToUpperInvariant();
            contractsText.text = $"Contracts Cleared {company.Cleared.Count} of {ContractRules.All.Length} <color=#CC342C>·</color> Fallen {company.Fallen.Count}";
            coinsText.text = $"{session.Gold:N0} Coins in the Company Chest";
        }

        // --- The contracts -------------------------------------------------------------------------------------------

        // Where each contract stands on the map, as a fraction of its width and height from its bottom left: the
        // Thornwood above, the deeper places winding down below it.
        static readonly Vector2[] MapPlaces = { new Vector2(0.26f, 0.74f), new Vector2(0.72f, 0.6f), new Vector2(0.3f, 0.36f), new Vector2(0.72f, 0.15f) };
        static readonly string[] Numerals = { "I", "II", "III", "IV" };

        void DrawContracts()
        {
            var company = session.Company;
            var intro = Stack(content, "Story");
            PixelUi.Label(intro, ContractRules.CompanyStory, PixelUi.BodyFont, PixelUi.Text, TextAnchor.UpperCenter);

            // The map.
            var map = Block("Map", 230f, "frame_panel_b4");
            var heading = PixelUi.Label(map, "THE DUCHY OF VELL · AND WHAT LIES BENEATH", PixelUi.BodyFont, PixelUi.GoldDim, TextAnchor.UpperCenter);
            PixelUi.Across(heading.rectTransform, 9f, 10f, 6f, 6f);
            var land = UiStyle.Rect(map, "Land");
            land.anchorMin = Vector2.zero;
            land.anchorMax = Vector2.one;
            land.offsetMin = new Vector2(8f, 8f);
            land.offsetMax = new Vector2(-8f, -22f);
            // The road between them, a line of dots.
            for (var i = 0; i + 1 < MapPlaces.Length; i++)
            {
                var open = ContractRules.IsOpen(ContractRules.All[i + 1], company.Cleared);
                for (var k = 2; k < 11; k++)
                {
                    var p = Vector2.Lerp(MapPlaces[i], MapPlaces[i + 1], k / 12f);
                    var dot = PixelUi.Picture(land, "Road", open ? "dot" : "rule_dim").rectTransform;
                    dot.anchorMin = dot.anchorMax = p;
                    dot.sizeDelta = new Vector2(2f, 2f);
                    dot.anchoredPosition = Vector2.zero;
                }
            }
            for (var i = 0; i < ContractRules.All.Length; i++)
                DrawPlace(land, ContractRules.All[i], i);

            // The chosen contract.
            var chosen = ContractRules.Get(selected);
            var openNow = ContractRules.IsOpen(chosen, company.Cleared);
            var cleared = company.Cleared.Contains(chosen.Id);
            var card = Stack(content, "Chosen");
            PixelUi.Label(card, chosen.Name.ToUpperInvariant(), PixelUi.TitleFont, PixelUi.Gold);
            PixelUi.Label(card, chosen.Region.ToUpperInvariant() + $" · DEPTHS {chosen.FirstDepth} TO {chosen.LastDepth}", PixelUi.BodyFont,
                PixelUi.GoldDim);
            StackRule(card);
            PixelUi.Label(card, openNow ? chosen.Story : "Sealed. The company takes this contract once the one before it is cleared.", PixelUi.BodyFont,
                PixelUi.Text);
            if (openNow)
                PixelUi.Label(card, "Bosses: " + string.Join(" · ", chosen.Bosses), PixelUi.BodyFont, PixelUi.TextDim);

            var take = Block("Take", ButtonHeight, openNow ? "frame_button_b5" : "frame_tab_b3");
            var label = !openNow ? "SEALED" : cleared ? "TAKE IT AGAIN" : "TAKE THE CONTRACT";
            var takeText = PixelUi.Label(take, label, PixelUi.TitleFont, openNow ? PixelUi.Gold : PixelUi.TextDim, TextAnchor.MiddleCenter);
            UiStyle.Stretch(takeText.rectTransform);
            takeText.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            if (openNow)
            {
                var button = take.gameObject.AddComponent<Button>();
                button.targetGraphic = take.GetComponent<Image>();
                take.GetComponent<Image>().raycastTarget = true;
                button.onClick.AddListener(() => RunDirector.Embark(chosen));
            }
        }

        void DrawPlace(RectTransform land, Contract contract, int index)
        {
            var company = session.Company;
            var open = ContractRules.IsOpen(contract, company.Cleared);
            var cleared = company.Cleared.Contains(contract.Id);
            var place = UiStyle.Rect(land, contract.Name);
            place.anchorMin = place.anchorMax = MapPlaces[index];
            place.sizeDelta = new Vector2(140f, 50f);
            place.anchoredPosition = Vector2.zero;
            var chosen = contract.Id == selected;
            var mark = PixelUi.Frame(place, "Mark", chosen ? "frame_tab_on_b3" : open ? "frame_tab_b3" : "frame_inset_b3", true);
            UiStyle.Place(mark.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(30f, 26f));
            var numeral = PixelUi.Label(mark.transform, open ? Numerals[index] : "?", PixelUi.TitleFont, open ? PixelUi.Gold : PixelUi.TextDim,
                TextAnchor.MiddleCenter);
            UiStyle.Stretch(numeral.rectTransform);
            numeral.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            var button = mark.gameObject.AddComponent<Button>();
            button.targetGraphic = mark;
            var id = contract.Id;
            button.onClick.AddListener(() =>
            {
                Sfx.Play(SoundId.Hint);
                selected = id;
                Redraw();
            });
            var name = PixelUi.Label(place, (open ? contract.Name : "Unknown") + (cleared ? "\n<color=#7DB45A>CLEARED</color>" : ""), PixelUi.BodyFont,
                open ? PixelUi.Text : PixelUi.TextDim, TextAnchor.UpperCenter);
            UiStyle.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -29f), new Vector2(140f, 24f));
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
            ("Lyriel", "Beastmaster", "Trains the wolf that runs with every archer.", "pet_vendor", PetVendorScreen.Open),
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

        // The small sign after a keeper's name, and the emblem on the right of the card.
        static readonly Dictionary<string, (string icon, string emblem, string portrait)> Signs = new Dictionary<string, (string, string, string)>
        {
            ["Brunhilde"] = ("icon_helm", "emblem_sword", "portrait_brunhilde"),
            ["Miranda"] = ("icon_hand", "emblem_goblet", "portrait_miranda"),
            ["Sylthara"] = ("icon_eye", "emblem_feather", "portrait_sylthara"),
            ["Garrett"] = ("icon_shield", "emblem_compass", "portrait_garrett"),
            ["Thorin"] = ("icon_hammer", "emblem_anvil", "portrait_thorin"),
            ["Elara"] = ("icon_key", "emblem_chest", "portrait_elara"),
            ["Lyriel"] = ("icon_paw", "emblem_paw", "portrait_lyriel"),
        };

        void DrawQuarters()
        {
            var intro = Stack(content, "About");
            intro.GetComponent<VerticalLayoutGroup>().padding.right = 28;
            PixelUi.Label(intro, "The company's people. Coins brought back from contracts buy what they offer, for good.", PixelUi.BodyFont,
                PixelUi.Text);
            var feather = PixelUi.Picture(intro, "Arrow", "small_arrow");
            feather.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiStyle.Place(feather.rectTransform, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(11f, 3f));
            for (var i = 0; i < Keepers.Length; i++)
            {
                var keeper = Keepers[i];
                Signs.TryGetValue(keeper.name, out var sign);
                var card = Block(keeper.name, 78f);
                var image = card.GetComponent<Image>();
                image.raycastTarget = true;
                var open = keeper.open;
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() =>
                {
                    Sfx.Play(SoundId.Hint);
                    open();
                });
                // The keeper's picture: the owner's painted portrait, or the first frame of the baked idle.
                var frame = PixelUi.Frame(card, "Picture", "frame_portrait_b3").rectTransform;
                PixelUi.At(frame, 4f, 4f, 77f, 70f);
                if (sign.portrait != null)
                {
                    var portrait = PixelUi.Picture(frame, "Portrait", sign.portrait).rectTransform;
                    UiStyle.Place(portrait, new Vector2(0.5f, 0.5f), Vector2.zero, portrait.sizeDelta);
                }
                else if (Picture(keeper.figure) is Sprite baked)
                {
                    var figure = UiStyle.Image(frame, "Figure", Color.white);
                    figure.sprite = baked;
                    figure.preserveAspect = true;
                    UiStyle.Stretch(figure.rectTransform, 4f);
                }
                var name = PixelUi.Label(card, $"{i + 1}. {keeper.name.ToUpperInvariant()}", PixelUi.TitleFont, PixelUi.Gold);
                PixelUi.At(name.rectTransform, 88f, 8f, 200f, 12f);
                if (sign.icon != null)
                {
                    var icon = PixelUi.Picture(card, "Sign", sign.icon).rectTransform;
                    icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0f, 1f);
                    icon.anchoredPosition = new Vector2(88f + name.preferredWidth + 5f, -7f);
                }
                var role = PixelUi.Label(card, keeper.role.ToUpperInvariant(), PixelUi.BodyFont, PixelUi.Gold);
                PixelUi.Across(role.rectTransform, 23f, 10f, 88f, 40f);
                PixelUi.Across(PixelUi.Tiled(card, "Rule", "rule_dim").rectTransform, 35f, 1f, 88f, 40f);
                var line = PixelUi.Label(card, keeper.line, PixelUi.BodyFont, PixelUi.Text);
                PixelUi.Across(line.rectTransform, 40f, 34f, 88f, 40f);
                if (sign.emblem != null)
                {
                    var emblem = PixelUi.Picture(card, "Emblem", sign.emblem).rectTransform;
                    UiStyle.Place(emblem, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), emblem.sizeDelta);
                    emblem.pivot = new Vector2(1f, 0.5f);
                }
            }
        }

        // --- The fallen ----------------------------------------------------------------------------------------------

        // The grave stone's words (the owner's concept picture, 2026-11-05).
        const string Epitaph = "They went forth with steel and courage, and the wilds claimed them. Their names are carved in stone, " +
                               "but their deeds remain with the Company.\n\nHonor the fallen. Their watch is over, but the Company marches on.";

        void DrawFallen()
        {
            var fallen = session.Company.Fallen;
            var stone = PixelUi.Frame(content, "Grave Stone", "frame_panel_b4").rectTransform;
            var layout = stone.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(9, 9, 9, 10);
            layout.spacing = 7f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var head = UiStyle.Rect(stone, "Heading");
            var headElement = head.gameObject.AddComponent<LayoutElement>();
            headElement.minHeight = headElement.preferredHeight = 20f;
            var grave = PixelUi.Picture(head, "Grave", "icon_grave").rectTransform;
            UiStyle.Place(grave, new Vector2(0f, 1f), Vector2.zero, grave.sizeDelta);
            var title = PixelUi.Label(head, "THE GRAVE STONE", PixelUi.TitleFont, PixelUi.TextDim);
            PixelUi.At(title.rectTransform, 18f, 3f, 260f, 12f);
            var rule = PixelUi.Tiled(head, "Rule", "rule_dim").rectTransform;
            PixelUi.Across(rule, 18f, 1f, 0f, 30f);

            PixelUi.Label(stone, Epitaph, PixelUi.BodyFont, PixelUi.Text);
            if (fallen.Count == 0)
            {
                PixelUi.Label(stone, "No archer of the company has fallen yet. The stone is waiting.", PixelUi.BodyFont, PixelUi.TextDim,
                    TextAnchor.UpperCenter);
                return;
            }
            for (var i = 0; i < fallen.Count; i++)
            {
                var dead = fallen[i];
                var date = DateTimeOffset.FromUnixTimeSeconds(dead.Day * 86400L).UtcDateTime;
                var entry = PixelUi.Frame(stone, dead.Name, "frame_inset_b3").rectTransform;
                var element = entry.gameObject.AddComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = 34f;
                var name = PixelUi.Label(entry, $"{i + 1}. <color=#CC342C>{dead.Name.ToUpperInvariant()}</color>", PixelUi.TitleFont, PixelUi.Text);
                PixelUi.At(name.rectTransform, 7f, 6f, 300f, 12f);
                var place = PixelUi.Label(entry, $"- {dead.Contract},", PixelUi.BodyFont, PixelUi.Text);
                PixelUi.At(place.rectTransform, 7f + name.preferredWidth + 5f, 8f, 200f, 10f);
                var details = $"Depth {dead.Depth} <color=#CC342C>·</color> {dead.Kills:N0} Slain" +
                              (dead.Bosses > 0 ? $" <color=#CC342C>·</color> {dead.Bosses} Boss{(dead.Bosses > 1 ? "es" : "")}" : "") +
                              $" <color=#CC342C>·</color> {RunSummary.Duration(dead.Seconds)} <color=#CC342C>·</color> {date:d MMM yyyy}";
                var line = PixelUi.Label(entry, details, PixelUi.BodyFont, PixelUi.Text);
                PixelUi.Across(line.rectTransform, 21f, 10f, 14f, 6f);
            }
        }
    }
}
