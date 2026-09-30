using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Pet Vendor's sheet (Docs/02, Pets; decided 2026-09-30): the kinds for sale, each with its bonus and price and
    /// a button (Buy, Make active, or Following), and the active pet's rules, picked from chips with one thumb and no
    /// typing: what it attacks (a chip that cycles through the choices) and what it does in which situation (on/off
    /// rows). Built in code the first time it opens, in the other sheets' look (<see cref="UiStyle"/>). It does not
    /// pause the game (the town has no enemies); the full-screen backdrop keeps touches off the stick.
    /// </summary>
    public class PetVendorScreen : MonoBehaviour
    {
        static PetVendorScreen current;

        RectTransform content;
        Text gold;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        public static void Open()
        {
            if (current == null)
                current = Create();
            current.gameObject.SetActive(true);
            current.Render();
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

            var root = new GameObject("Pet Vendor", typeof(RectTransform), typeof(Image), typeof(Button));
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
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 48);
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var title = NewText(sheet.transform, "Pet Vendor", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = UiStyle.Gold;
            Height(title.gameObject, 90f);
            screen.gold = NewText(sheet.transform, "", 34, FontStyle.Normal, TextAnchor.MiddleCenter);
            Height(screen.gold.gameObject, 56f);

            var list = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            list.transform.SetParent(sheet.transform, false);
            var listLayout = list.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            list.AddComponent<LayoutElement>().flexibleHeight = 1f;
            screen.content = (RectTransform)list.transform;

            var close = NewButton(sheet.transform, "Close", screen.Close, 40);
            Height(close.gameObject, 150f);
            return screen;
        }

        void Render()
        {
            var session = GameSession.Current;
            var pets = session.Pets;
            gold.text = $"<color=#E8C66A>{session.Gold} gold</color>";

            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            foreach (var kind in PetRules.All)
            {
                var definition = PetRules.Get(kind);
                string action;
                var enabled = true;
                Action onClick;
                if (pets.Active == kind)
                {
                    action = "Following";
                    enabled = false;
                    onClick = null;
                }
                else if (pets.Owns(kind))
                {
                    action = "Make active";
                    onClick = () => pets.SetActive(kind);
                }
                else
                {
                    action = $"Buy {definition.Price}";
                    enabled = session.Gold >= definition.Price;
                    onClick = () =>
                    {
                        if (session.BuyPet(kind))
                            Sfx.Play(SoundId.Pickup);
                    };
                }
                Row($"<b>{definition.Name}</b>\n<size=28><color=#CCCCCC>{definition.BonusText}; levels with you</color></size>", action, enabled, onClick, 150f);
            }

            var header = NewText(content, pets.Active != null ? "Its rules" : "Its rules (for the pet you buy)", 38, FontStyle.Bold, TextAnchor.MiddleLeft);
            header.color = UiStyle.Gold;
            Height(header.gameObject, 70f);
            Row($"<b>Target</b>\n<size=28><color=#CCCCCC>Tap to change</color></size>", PetRules.TargetingText(pets.Targeting), true,
                pets.CycleTargeting, 150f, 520f);
            foreach (PetBehaviour behaviour in Enum.GetValues(typeof(PetBehaviour)))
            {
                if (behaviour == PetBehaviour.None)
                    continue;
                var on = pets.Has(behaviour);
                Row(PetRules.BehaviourText(behaviour), on ? "On" : "Off", true, () => pets.Toggle(behaviour), 130f, 200f, on);
            }
        }

        void Row(string label, string action, bool enabled, Action onClick, float height, float buttonWidth = 320f, bool highlighted = false)
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

            var text = NewText(row.transform, label, 34, FontStyle.Normal, TextAnchor.MiddleLeft);
            Width(text.gameObject, 0f, 1f);
            var button = NewButton(row.transform, action, () =>
            {
                onClick?.Invoke();
                Render();
            }, 30);
            Width(button.gameObject, buttonWidth, 0f);
            button.interactable = enabled;
            if (highlighted)
                button.GetComponent<Image>().color = UiStyle.Selected;
        }

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
