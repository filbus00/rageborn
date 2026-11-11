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
    /// emblems, and the grave stone with its epitaph. Reworked on 2026-11-11 (the owner: "rework all the menus ... too
    /// many vendors in quarters. Does the wild arrow logo really need to be displayed at the top? Reimagine it all"): no
    /// plaque; the archer is one strip at the top (her face, name, record, coins, the settings); the pages and the Bag
    /// are a bar at the bottom, under the thumb; the quarters are three places drawn in the page itself
    /// (CompanyMenu.Quarters.cs), with no sheets.
    /// </summary>
    public partial class CompanyMenu : MonoBehaviour
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

        // Menu pixels (PixelUi).
        const float Edge = 6f;
        const float StripHeight = 74f;
        const float NavHeight = 38f;
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

            // The archer under contract: one strip.
            header = PixelUi.Frame(body, "Archer", "frame_panel_b4").rectTransform;
            PixelUi.Across(header, Edge, StripHeight, Edge, Edge);

            // The page, scrolling between the strip and the bar at the bottom.
            var viewport = UiStyle.Rect(body, "Page");
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(Edge, Edge + NavHeight + 5f);
            viewport.offsetMax = new Vector2(-Edge, -(Edge + StripHeight + 5f));
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

            // The bar at the bottom: the three pages and the Bag.
            NavButton("CONTRACTS", "icon_scroll", 0, () => Show(Page.Contracts), Page.Contracts);
            NavButton("QUARTERS", "icon_hammer", 1, () => Show(Page.Quarters), Page.Quarters);
            NavButton("THE FALLEN", "icon_grave", 2, () => Show(Page.Fallen), Page.Fallen);
            NavButton("THE BAG", "icon_bag", 3, () => InventoryScreen.Current?.Open(), null);
        }

        // A button of the bar: its picture over its name. A page's button is lit while its page shows.
        void NavButton(string label, string icon, int index, Action onClick, Page? shows)
        {
            const int count = 4;
            var frame = PixelUi.Frame(body, label, shows.HasValue ? "frame_tab_b3" : "frame_button_b5", true);
            var rect = frame.rectTransform;
            rect.anchorMin = new Vector2(index / (float)count, 0f);
            rect.anchorMax = new Vector2((index + 1) / (float)count, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(index == 0 ? Edge : 2f, Edge);
            rect.offsetMax = new Vector2(index == count - 1 ? -Edge : -2f, Edge + NavHeight);
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() =>
            {
                Sfx.Play(SoundId.Hint);
                onClick();
            });
            var picture = PixelUi.Picture(frame.transform, "Icon", icon).rectTransform;
            UiStyle.Place(picture, new Vector2(0.5f, 1f), new Vector2(0f, -13f), picture.sizeDelta);
            picture.pivot = new Vector2(0.5f, 0.5f);
            var text = PixelUi.Label(frame.transform, label, PixelUi.BodyFont, PixelUi.TextDim, TextAnchor.UpperCenter);
            PixelUi.Across(text.rectTransform, 23f, 10f, 0f, 0f);
            if (shows.HasValue)
                tabs.Add((shows.Value, frame, text));
            else
                text.color = PixelUi.Gold;
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
        Image portrait;

        void DrawHeader()
        {
            if (nameText == null)
            {
                // Her face in a gold frame: a window onto her portrait at its own size (her own face, made from her name:
                // PortraitFaces, 2026-11-07; the concept picture's when there are no base faces).
                var frame = PixelUi.Frame(header, "Portrait", "frame_portrait_b3").rectTransform;
                PixelUi.At(frame, 4f, 4f, 66f, 66f);
                var window = UiStyle.Rect(frame, "Window");
                UiStyle.Stretch(window, 2f);
                window.gameObject.AddComponent<RectMask2D>();
                portrait = PixelUi.Picture(window, "Archer", "portrait_archer");
                PixelUi.At(portrait.rectTransform, -31f, -28f, portrait.rectTransform.sizeDelta.x, portrait.rectTransform.sizeDelta.y);

                nameText = PixelUi.Label(header, "", PixelUi.TitleFont, PixelUi.Gold);
                PixelUi.Across(nameText.rectTransform, 11f, 12f, 78f, 40f);
                var role = PixelUi.Label(header, "Amazon Archer of the Company", PixelUi.BodyFont, PixelUi.TextDim);
                PixelUi.Across(role.rectTransform, 27f, 10f, 78f, 40f);
                PixelUi.Across(PixelUi.Tiled(header, "Rule", "rule_dim").rectTransform, 43f, 1f, 78f, 8f);
                UiStyle.Place(PixelUi.Picture(header, "Skull", "icon_skull").rectTransform, new Vector2(0f, 1f), new Vector2(78f, -51f), new Vector2(9f, 9f));
                contractsText = PixelUi.Label(header, "", PixelUi.BodyFont, PixelUi.Text);
                PixelUi.Across(contractsText.rectTransform, 51f, 10f, 91f, 8f);
                UiStyle.Place(PixelUi.Picture(header, "Coin", "icon_coin").rectTransform, new Vector2(1f, 1f), new Vector2(-8f, -51f), new Vector2(9f, 9f));
                coinsText = PixelUi.Label(header, "", PixelUi.BodyFont, PixelUi.Gold, TextAnchor.UpperRight);
                PixelUi.Across(coinsText.rectTransform, 51f, 10f, 91f, 21f);

                // The settings, a small button in the strip's corner.
                var gear = PixelUi.Frame(header, "SETTINGS", "frame_button_b5", true);
                UiStyle.Place(gear.rectTransform, new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(29f, 28f));
                var button = gear.gameObject.AddComponent<Button>();
                button.targetGraphic = gear;
                button.onClick.AddListener(() =>
                {
                    Sfx.Play(SoundId.Hint);
                    SettingsScreen.Open();
                });
                var icon = PixelUi.Picture(gear.transform, "Icon", "icon_gear").rectTransform;
                UiStyle.Place(icon, new Vector2(0.5f, 0.5f), Vector2.zero, icon.sizeDelta);
            }
            var company = session.Company;
            nameText.text = company.Mercenary.ToUpperInvariant();
            if (PortraitFaces.For(company.Mercenary) is Sprite face)
            {
                // Her own face at its own size (66 x 81), her head in the window.
                portrait.sprite = face;
                PixelUi.At(portrait.rectTransform, -2f, -6f, face.rect.width, face.rect.height);
            }
            contractsText.text = $"Contracts {company.Cleared.Count} of {ContractRules.All.Length} <color=#CC342C>·</color> Fallen {company.Fallen.Count}";
            coinsText.text = $"{session.Gold:N0}";
        }

        // --- The contracts -------------------------------------------------------------------------------------------

        // Where each contract stands on the map, as a fraction of its width and height from its bottom left: the
        // Thornwood above, the deeper places winding down below it.
        static readonly Vector2[] MapPlaces = { new Vector2(0.26f, 0.74f), new Vector2(0.72f, 0.6f), new Vector2(0.3f, 0.36f), new Vector2(0.72f, 0.15f) };
        static readonly string[] Numerals = { "I", "II", "III", "IV" };

        void DrawContracts()
        {
            var company = session.Company;
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
            PixelUi.Label(card, chosen.Region.ToUpperInvariant() + $" · LEVELS {ContractRules.LevelCode(chosen.FirstDepth)} TO {ContractRules.LevelCode(chosen.LastDepth)}", PixelUi.BodyFont,
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

            // The company's story, under what she came for.
            var story = Stack(content, "Story", null, 6);
            PixelUi.Label(story, ContractRules.CompanyStory, PixelUi.BodyFont, PixelUi.TextDim, TextAnchor.UpperCenter);
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
                var details = $"{ContractRules.LevelName(dead.Depth)} <color=#CC342C>·</color> {dead.Kills:N0} Slain" +
                              (dead.Bosses > 0 ? $" <color=#CC342C>·</color> {dead.Bosses} Boss{(dead.Bosses > 1 ? "es" : "")}" : "") +
                              $" <color=#CC342C>·</color> {RunSummary.Duration(dead.Seconds)} <color=#CC342C>·</color> {date:d MMM yyyy}";
                var line = PixelUi.Label(entry, details, PixelUi.BodyFont, PixelUi.Text);
                PixelUi.Across(line.rectTransform, 21f, 10f, 14f, 6f);
            }
        }
    }
}
