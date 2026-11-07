using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Lyriel's sheet (Docs/02, Pets): the wolf, its training for coins (since 2026-11-07, when the raven and the boar
    /// went and its skills became boons), and its rules, picked from chips with one thumb and no
    /// typing: what it attacks (a chip that cycles through the choices) and what it does in which situation (on/off
    /// rows). Built in code the first time it opens, in the other sheets' look (<see cref="UiStyle"/>). It does not
    /// pause the game (the town has no enemies); the full-screen backdrop keeps touches off the stick.
    /// </summary>
    public class PetVendorScreen : MonoBehaviour
    {
        static PetVendorScreen current;

        RectTransform content;
        ScrollRect scroll;
        Text gold;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        public static void Open()
        {
            if (current == null)
                current = Create();
            current.gameObject.SetActive(true);
            current.Render();
            current.scroll.verticalNormalizedPosition = 1f;
        }

        public static void CloseIfOpen()
        {
            if (IsOpen)
                current.Close();
        }

        public void Close() => gameObject.SetActive(false);

        static PetVendorScreen Create()
        {
            var canvasObject = new GameObject("Pet Vendor Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;
            // The company menu's pixel look (2026-11-05, the owner: "remake all").
            PixelMenu.Attach(canvasObject);

            var root = new GameObject("Lyriel", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<PetVendorScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);

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

            var title = NewText(sheet.transform, "Lyriel", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = UiStyle.Gold;
            Height(title.gameObject, 90f);
            SheetKit.Rule(sheet.transform, 4f);
            screen.gold = NewText(sheet.transform, "", 32, FontStyle.Normal, TextAnchor.MiddleCenter);
            Height(screen.gold.gameObject, 64f);

            // The list scrolls (2026-10-17: with a pet's four skills it ran off the sheet).
            screen.content = SheetKit.ScrollList(sheet.transform, out screen.scroll);

            var close = NewButton(sheet.transform, "Close", screen.Close, 38);
            Height(close.gameObject, 118f);
            return screen;
        }

        void Render()
        {
            var session = GameSession.Current;
            var pets = session.Pets;
            gold.text = $"<color=#E8C66A>{session.Gold:N0} gold</color>.  The wolf follows you into the dungeon and fights at your side.";

            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            // The wolf runs with every archer (2026-11-07, the owner: "Remove all other pets and rework the beastmaster"):
            // nothing to buy; Lyriel trains it for coins, and its skills are boons in a run.
            var wolf = PetRules.Get(PetKind.Wolf);
            SheetKit.Heading(content, "The wolf");
            Row($"<b>{wolf.Name}</b>\n<size=28><color=#CCCCCC>{wolf.BonusText}; runs with every archer and levels with her. Its skills are boons.</color></size>",
                "Following", false, null, 150f);

            SheetKit.Heading(content, "Training");
            foreach (var upgrade in MetaRules.All)
            {
                if (upgrade.Shop != MetaShop.Kennel)
                    continue;
                var rank = session.Meta.Rank(upgrade.Id);
                var maxed = session.Meta.IsMaxed(upgrade.Id);
                var cost = session.Meta.NextCost(upgrade.Id);
                var id = upgrade.Id;
                Row($"<b>{upgrade.Name}</b>{SheetKit.Pips(rank, upgrade.MaxRanks)}\n" +
                    $"<size=26><color=#CCCCCC>{MetaRules.Describe(upgrade, maxed ? rank : rank + 1)}</color></size>",
                    maxed ? "Mastered" : $"Train {cost:N0}", !maxed && session.Gold >= cost, () =>
                    {
                        if (session.BuyUpgrade(id))
                            Sfx.Play(SoundId.LevelUp, 0.6f);
                    }, 150f);
            }

            SheetKit.Heading(content, "How it fights");
            Row($"<b>Target</b>\n<size=28><color=#CCCCCC>Tap to change</color></size>", PetRules.TargetingText(pets.Targeting), true,
                pets.CycleTargeting, 150f, 520f);
            foreach (PetBehaviour behaviour in Enum.GetValues(typeof(PetBehaviour)))
            {
                if (behaviour == PetBehaviour.None)
                    continue;
                var on = pets.Has(behaviour);
                Row(PetRules.BehaviourText(behaviour), on ? "On" : "Off", true, () => pets.Toggle(behaviour), 110f, 180f, on);
            }
        }

        void Row(string label, string action, bool enabled, Action onClick, float height, float buttonWidth = 300f, bool highlighted = false) =>
            SheetKit.Card(content, label, action, enabled, () =>
            {
                onClick?.Invoke();
                Render();
            }, height, buttonWidth, highlighted);

        static Button NewButton(Transform parent, string label, Action onClick, int size)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = UiStyle.ButtonFill;
            UiStyle.Rim(go);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var text = NewText(go.transform, label, size, FontStyle.Bold, TextAnchor.MiddleCenter);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);
            return go.GetComponent<Button>();
        }

        static Text NewText(Transform parent, string contents, int size, FontStyle style, TextAnchor alignment)
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

        static void Height(GameObject go, float height)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }

        static void Width(GameObject go, float width, float flexible)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = flexible;
        }
    }
}
