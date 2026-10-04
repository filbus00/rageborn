using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Settings page of the Bag (Docs/06-ui-ux.md: Settings sit on the one screen the inventory button opens). A
    /// bottom sheet over the paused Bag, grouped as in Docs/06's table: Controls, Combat, Audio, Accessibility. Numbers
    /// change in steps with − and + buttons (one thumb, 56 point targets, no sliders to drag), switches with a chip
    /// that flips. Every change applies at once through <see cref="SettingsDirector.Apply"/>, which saves it. Only
    /// the settings with a system behind them (see <see cref="GameSettings"/>); the search field, haptics, colour
    /// blind modes, text size and the Game group wait for what they control. Built in code the first time it opens.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        static readonly Color SheetColor = UiStyle.Sheet;
        static readonly Color StrokeColor = UiStyle.Blood;
        static readonly Color ButtonColor = UiStyle.ButtonFill;
        static readonly Color ChipOnColor = UiStyle.Selected;
        static readonly Color GroupColor = UiStyle.Gold;

        const float RowHeight = 168f; // 56 points, the docs' preferred tap target.

        static SettingsScreen current;

        RectTransform list;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        public static void Open()
        {
            if (current == null)
                current = Create();
            current.gameObject.SetActive(true);
            current.Render();
        }

        public void Close() => gameObject.SetActive(false);

        /// <summary>Closes the page if it is open (the Bag closing takes it along).</summary>
        public static void CloseIfOpen()
        {
            if (IsOpen)
                current.Close();
        }

        static SettingsScreen Create()
        {
            var canvasObject = new GameObject("Settings Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = new GameObject("Settings", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<SettingsScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);

            // Inside the safe area, so the bottom rows clear the home indicator.
            var safe = new GameObject("Safe Area", typeof(RectTransform));
            safe.transform.SetParent(root.transform, false);
            SafeArea.Fit((RectTransform)safe.transform);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Button));
            sheet.transform.SetParent(safe.transform, false);
            // The sheet swallows taps so only the dimmed area above it closes.
            sheet.GetComponent<Button>().transition = Selectable.Transition.None;
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 2080f);
            sheet.GetComponent<Image>().color = SheetColor;
            UiStyle.Leather(sheet.GetComponent<Image>());
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 32, 32);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.UpperCenter;

            var stroke = new GameObject("Stroke", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            stroke.transform.SetParent(sheet.transform, false);
            stroke.GetComponent<Image>().color = StrokeColor;
            stroke.GetComponent<LayoutElement>().ignoreLayout = true;
            var strokeRect = (RectTransform)stroke.transform;
            strokeRect.anchorMin = new Vector2(0f, 1f);
            strokeRect.anchorMax = new Vector2(1f, 1f);
            strokeRect.pivot = new Vector2(0.5f, 1f);
            strokeRect.sizeDelta = new Vector2(0f, 4f);

            var title = NewText(sheet.transform, "Settings", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = UiStyle.Gold;
            Height(title.gameObject, 84f);

            var listObject = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listObject.transform.SetParent(sheet.transform, false);
            var listLayout = listObject.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            screen.list = (RectTransform)listObject.transform;

            // The bottom row: Close, and the reset beside it (the sheet has no room for another row). The reset asks
            // twice: it erases the character.
            var bottom = new GameObject("Bottom Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bottom.transform.SetParent(sheet.transform, false);
            var bottomLayout = bottom.GetComponent<HorizontalLayoutGroup>();
            bottomLayout.spacing = 12f;
            bottomLayout.childControlWidth = true;
            bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = true;
            bottomLayout.childForceExpandHeight = true;
            Height(bottom, RowHeight);
            var close = NewButton(bottom.transform, "Close", screen.Close);
            Width(close.gameObject, 0f, 1f);
            screen.reset = NewButton(bottom.transform, ResetLabel, screen.ResetPressed);
            screen.reset.GetComponent<Image>().color = UiStyle.Chip;
            Width(screen.reset.gameObject, 0f, 1f);
            return screen;
        }

        const string ResetLabel = "Reset save";

        Button reset;
        bool resetArmed;

        void OnDisable() => ArmReset(false);

        void ResetPressed()
        {
            if (!resetArmed)
            {
                ArmReset(true);
                return;
            }
            ArmReset(false);
            Close();
            InventoryScreen.Current?.Close();
            SaveDirector.StartOver();
        }

        void ArmReset(bool armed)
        {
            resetArmed = armed;
            if (reset == null)
                return;
            reset.GetComponentInChildren<Text>().text = armed ? "Tap again to erase" : ResetLabel;
            reset.GetComponent<Image>().color = armed ? UiStyle.BloodBright : UiStyle.Chip;
        }

        void Render()
        {
            for (var i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            var s = SettingsDirector.Current;

            Group("Controls");
            Stepper("Stick size", $"{s.stickSize:0} pt", s.stickSize, GameSettings.MinStickSize, GameSettings.MaxStickSize, GameSettings.StickSizeStep,
                (c, v) => c.stickSize = v);
            Stepper("Dead zone", $"{s.deadZone:0}%", s.deadZone, GameSettings.MinDeadZone, GameSettings.MaxDeadZone, GameSettings.DeadZoneStep,
                (c, v) => c.deadZone = v);
            Choice("Portal button", s.leftHanded ? "Left hand" : "Right hand", false, c => c.leftHanded = !c.leftHanded);

            Group("Combat");
            Stepper("Auto-potion at", $"{s.potionThreshold:0}% life", s.potionThreshold,
                AutoPotion.MinTriggerFraction * 100f, AutoPotion.MaxTriggerFraction * 100f, GameSettings.PotionStep,
                (c, v) => c.potionThreshold = v);

            Group("Loot");
            Choice("Pick up", AutoLootRules.Describe(s.PickupRule), false,
                c => c.pickupRule = (c.pickupRule + 1) % ((int)PickupRule.UpgradesOnly + 1));

            Group("Audio");
            Stepper("Music", $"{s.musicVolume:0}", s.musicVolume, 0f, 100f, GameSettings.VolumeStep, (c, v) => c.musicVolume = v);
            Stepper("Effects", $"{s.effectsVolume:0}", s.effectsVolume, 0f, 100f, GameSettings.VolumeStep, (c, v) => c.effectsVolume = v);

            Group("Accessibility");
            Choice("Reduce flashing", s.reduceFlashing ? "On" : "Off", s.reduceFlashing, c => c.reduceFlashing = !c.reduceFlashing);
            Choice("Reduce motion", s.reduceMotion ? "On" : "Off", s.reduceMotion, c => c.reduceMotion = !c.reduceMotion);
        }

        void Group(string name)
        {
            var text = NewText(list, name, 34, FontStyle.Bold, TextAnchor.LowerLeft);
            text.color = GroupColor;
            Height(text.gameObject, 64f);
        }

        void Stepper(string label, string value, float current, float min, float max, float step, Action<GameSettings, float> set)
        {
            var row = NewRow(label);
            var down = NewButton(row.transform, "−", () => Change(c => set(c, GameSettings.Step(current, min, max, step, -1))));
            Width(down.gameObject, 150f, 0f);
            down.interactable = current > min;
            var shown = NewText(row.transform, value, 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            Width(shown.gameObject, 250f, 0f);
            var up = NewButton(row.transform, "+", () => Change(c => set(c, GameSettings.Step(current, min, max, step, 1))));
            Width(up.gameObject, 150f, 0f);
            up.interactable = current < max;
        }

        void Choice(string label, string value, bool highlighted, Action<GameSettings> flip)
        {
            var row = NewRow(label);
            var chip = NewButton(row.transform, value, () => Change(flip));
            Width(chip.gameObject, 574f, 0f);
            if (highlighted)
                chip.GetComponent<Image>().color = ChipOnColor;
        }

        static void Change(Action<GameSettings> edit)
        {
            var changed = SettingsDirector.Current.Copy();
            edit(changed);
            SettingsDirector.Apply(changed);
            if (current != null)
                current.Render();
        }

        GameObject NewRow(string label)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(list, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            // Children's widths are controlled, or a label's flexible width is ignored (Docs/11-engineering-log.md, Loot).
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            Height(row, RowHeight);
            var name = NewText(row.transform, label, 38, FontStyle.Normal, TextAnchor.MiddleLeft);
            Width(name.gameObject, 0f, 1f);
            return row;
        }

        static Button NewButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = ButtonColor;
            UiStyle.Rim(go);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var text = NewText(go.transform, label, 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
            return go.GetComponent<Button>();
        }

        static Text NewText(Transform parent, string content, int size, FontStyle style, TextAnchor alignment)
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
            text.text = content;
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
