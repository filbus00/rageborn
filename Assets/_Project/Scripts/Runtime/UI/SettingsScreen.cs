using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Settings page (Docs/06-ui-ux.md: Settings sit on the one screen the inventory button opens), laid out after
    /// the owner's concept art (2026-10-19, menuconcept.png): a full page under the menus' header (<see cref="MenuChrome"/>)
    /// with the groups of Docs/06's table (Controls, Combat, Loot, Audio, Accessibility) as framed cards. Each row has
    /// its icon, its name and a line saying what it does, and its control in a framed box: a number between ‹ and ›
    /// that step it, a choice with › that turns it over, and the volumes as sliders. Every change applies at once
    /// through <see cref="SettingsDirector.Apply"/>, which saves it. Close and the reset (asked twice) along the bottom.
    /// Built in code the first time it opens; opened from the Bag's tab or the HUD's cog (<see cref="OpenPaused"/>).
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        const float RowHeight = 136f, GroupHeight = 74f, ControlWidth = 400f, ControlHeight = 90f, BottomHeight = 150f;

        static SettingsScreen current;

        RectTransform page;
        RectTransform chrome;
        RectTransform list;
        ScrollRect scroll;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        public static void Open()
        {
            if (current == null)
                current = Create();
            current.gameObject.SetActive(true);
            current.Render();
            if (current.scroll != null)
                current.scroll.verticalNormalizedPosition = 1f;
        }

        // Opened from the HUD's button the page pauses the game itself, as the Bag does (2026-10-18).
        static bool pausedHere;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => pausedHere = false;

        /// <summary>Opens the page from the HUD: the game pauses until it closes, unless the Bag already paused it.</summary>
        public static void OpenPaused()
        {
            if (InventoryScreen.Current == null || !InventoryScreen.Current.IsOpen)
            {
                pausedHere = true;
                Time.timeScale = 0f;
            }
            Open();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            if (!pausedHere)
                return;
            pausedHere = false;
            if (InventoryScreen.Current == null || !InventoryScreen.Current.IsOpen)
                Time.timeScale = 1f;
        }

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

            // A whole page on the leather, as the Bag is (a sheet over the dimmed game until 2026-10-19).
            var root = new GameObject("Settings", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            var background = root.GetComponent<Image>();
            background.color = UiStyle.Backdrop;
            UiStyle.Leather(background);
            background.raycastTarget = true;
            var screen = root.AddComponent<SettingsScreen>();

            // Inside the safe area, so the bottom row clears the home indicator.
            screen.page = UiStyle.Rect(root.transform, "Safe Area");
            SafeArea.Fit(screen.page);
            screen.chrome = UiStyle.Rect(screen.page, "Chrome");
            UiStyle.Stretch(screen.chrome);

            // The cards scroll between the title and the bottom row.
            var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scrollObject.transform.SetParent(screen.page, false);
            scrollObject.GetComponent<Image>().color = Color.clear;
            var scrollRect = (RectTransform)scrollObject.transform;
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(16f, BottomHeight + 16f);
            scrollRect.offsetMax = new Vector2(-16f, -(MenuChrome.HeaderHeight + MenuChrome.TitleHeight));
            var listObject = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listObject.transform.SetParent(scrollObject.transform, false);
            var listRect = (RectTransform)listObject.transform;
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.offsetMin = listRect.offsetMax = Vector2.zero;
            listObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.content = listRect;
            scroll.viewport = scrollRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            var listLayout = listObject.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 22f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            screen.list = listRect;
            screen.scroll = scroll;

            // The bottom row: Close, and the reset beside it in red. The reset asks twice: it erases the character.
            var bottom = UiStyle.Rect(screen.page, "Bottom Row");
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.offsetMin = new Vector2(16f, 8f);
            bottom.offsetMax = new Vector2(-16f, 8f + BottomHeight);
            var close = UiStyle.Button(bottom, "Close", screen.Close, 36);
            Half((RectTransform)close.transform, 0);
            screen.reset = UiStyle.Button(bottom, ResetLabel, screen.ResetPressed, 36, UiStyle.Blood);
            Half((RectTransform)screen.reset.transform, 1);
            screen.reset.GetComponentInChildren<Text>().color = UiStyle.Gold;
            return screen;
        }

        static void Half(RectTransform rect, int side)
        {
            rect.anchorMin = new Vector2(side * 0.5f, 0f);
            rect.anchorMax = new Vector2(side * 0.5f + 0.5f, 1f);
            rect.offsetMin = new Vector2(side == 0 ? 0f : 8f, 0f);
            rect.offsetMax = new Vector2(side == 0 ? -8f : 0f, 0f);
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
            UiStyle.FillOf(reset.GetComponent<Image>()).color = armed ? UiStyle.BloodBright : Color.Lerp(UiStyle.Blood, Color.white, 0.12f);
        }

        // The card the rows are going into, and whether a row has gone in yet (a rule goes between rows).
        RectTransform card;
        bool cardHasRows;

        void Render()
        {
            Clear(list);
            Clear(chrome);
            var session = GameSession.Current;
            var health = FindAnyObjectByType<PlayerHealth>();
            MenuChrome.Header(chrome, session, health != null ? health.Fraction : 1f, MenuChrome.Where(), Close);
            MenuChrome.Title(chrome, "Settings", MenuChrome.HeaderHeight);

            var s = SettingsDirector.Current;

            Group("Controls");
            Stepper(MenuIcons.Kind.Stick, "Stick size", "Adjust the virtual joystick size", $"{s.stickSize:0} pt", s.stickSize,
                GameSettings.MinStickSize, GameSettings.MaxStickSize, GameSettings.StickSizeStep, (c, v) => c.stickSize = v);
            Stepper(MenuIcons.Kind.DeadZone, "Dead zone", "Ignore small stick movements", $"{s.deadZone:0}%", s.deadZone,
                GameSettings.MinDeadZone, GameSettings.MaxDeadZone, GameSettings.DeadZoneStep, (c, v) => c.deadZone = v);
            Choice(MenuIcons.Kind.Portal, "Portal button", "Choose which hand to use", s.leftHanded ? "Left hand" : "Right hand",
                c => c.leftHanded = !c.leftHanded);

            Group("Combat");
            Stepper(MenuIcons.Kind.Potion, "Auto-potion at", "Use potion when life is below", $"{s.potionThreshold:0}% life", s.potionThreshold,
                AutoPotion.MinTriggerFraction * 100f, AutoPotion.MaxTriggerFraction * 100f, GameSettings.PotionStep, (c, v) => c.potionThreshold = v);

            Group("Loot");
            Choice(MenuIcons.Kind.LootBag, "Pick up", "Item quality to pick up", AutoLootRules.Describe(s.PickupRule),
                c => c.pickupRule = (c.pickupRule + 1) % ((int)PickupRule.UpgradesOnly + 1));

            Group("Audio");
            Volume(MenuIcons.Kind.Music, "Music", "Background music volume", s.musicVolume, (c, v) => c.musicVolume = v);
            Volume(MenuIcons.Kind.Speaker, "Effects", "Sound effects volume", s.effectsVolume, (c, v) => c.effectsVolume = v);

            Group("Accessibility");
            Choice(MenuIcons.Kind.Eye, "Reduce flashing", "Minimize flashing effects", s.reduceFlashing ? "On" : "Off",
                c => c.reduceFlashing = !c.reduceFlashing);
            Choice(MenuIcons.Kind.Motion, "Reduce motion", "Less screen shake and motion", s.reduceMotion ? "On" : "Off",
                c => c.reduceMotion = !c.reduceMotion);

            // Development builds only (2026-10-13).
            if (DevTools.Enabled)
            {
                Group("Developer");
                DevAction("Legendaries", "Equip all", () => DevTools.EquipLegendaries(GameSession.Current));
                DevAction("Every boon", "Give", () => DevTools.MaxCharacter(GameSession.Current));
                DevAction("100,000 coins", "Give", () => DevTools.GiveCoins(GameSession.Current));
            }
        }

        static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        // A card: the frame behind (outside the layout), the group's name after a diamond, a rule, then its rows.
        void Group(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(list, false);
            card = (RectTransform)go.transform;
            cardHasRows = false;
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(34, 34, 14, 18);
            layout.spacing = 0f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var frame = UiStyle.Framed(card, "Frame", new Color(0.075f, 0.06f, 0.055f, 1f), 3f);
            frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiStyle.Stretch(frame.rectTransform);

            var heading = UiStyle.Rect(card, "Heading");
            Height(heading.gameObject, GroupHeight);
            var diamond = MenuChrome.Diamond(heading, 16f, UiStyle.Gold);
            diamond.rectTransform.anchorMin = diamond.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            diamond.rectTransform.anchoredPosition = new Vector2(10f, 0f);
            var title = UiStyle.Text(heading, name, 38, UiStyle.Gold, TextAnchor.MiddleLeft, true);
            UiStyle.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(40f, 0f);
            Rule(card);
        }

        static void Rule(Transform parent)
        {
            var rule = UiStyle.Image(parent, "Rule", new Color(UiStyle.Frame.r, UiStyle.Frame.g, UiStyle.Frame.b, 0.35f));
            Height(rule.gameObject, 2f);
        }

        // A row: its icon, its name and what it does, and room on the right for its control (returned).
        RectTransform Row(MenuIcons.Kind kind, string label, string description)
        {
            if (cardHasRows)
                Rule(card);
            cardHasRows = true;
            var row = UiStyle.Rect(card, label);
            Height(row.gameObject, RowHeight);
            var icon = MenuChrome.Icon(row, kind, 64f);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(36f, 0f);
            var name = UiStyle.Text(row, label, 34, UiStyle.TextMain, TextAnchor.LowerLeft, true);
            Place(name.rectTransform, new Vector2(102f, RowHeight * 0.5f - 2f), new Vector2(-ControlWidth - 24f, -10f));
            var what = UiStyle.Text(row, description, 25, UiStyle.TextDim, TextAnchor.UpperLeft, false);
            Place(what.rectTransform, new Vector2(102f, 10f), new Vector2(-ControlWidth - 24f, -RowHeight * 0.5f - 4f));
            var control = UiStyle.Rect(row, "Control");
            control.anchorMin = control.anchorMax = control.pivot = new Vector2(1f, 0.5f);
            control.sizeDelta = new Vector2(ControlWidth, ControlHeight);
            return control;
        }

        // Between left and bottom offsets and right and top offsets inside the row.
        static void Place(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // The control's framed box.
        static Image Box(RectTransform control)
        {
            var box = UiStyle.Framed(control, "Box", UiStyle.Panel, 3f);
            UiStyle.Stretch(box.rectTransform);
            return box;
        }

        static Text BoxText(Transform parent, string text, int size, TextAnchor anchor)
        {
            var label = UiStyle.Text(parent, text, size, UiStyle.TextMain, anchor, true);
            label.raycastTarget = false;
            return label;
        }

        // A number between ‹ and ›, each half of the box a button that steps it.
        void Stepper(MenuIcons.Kind kind, string label, string description, string value, float now, float min, float max, float step,
            Action<GameSettings, float> set)
        {
            var control = Row(kind, label, description);
            var box = Box(control);
            var shown = BoxText(box.transform, value, 36, TextAnchor.MiddleCenter);
            UiStyle.Stretch(shown.rectTransform);
            for (var side = -1; side <= 1; side += 2)
            {
                var direction = side;
                var can = side < 0 ? now > min : now < max;
                var half = UiStyle.Image(box.transform, side < 0 ? "Down" : "Up", new Color(0f, 0f, 0f, 0f), true);
                half.rectTransform.anchorMin = new Vector2(side < 0 ? 0f : 0.5f, 0f);
                half.rectTransform.anchorMax = new Vector2(side < 0 ? 0.5f : 1f, 1f);
                half.rectTransform.offsetMin = half.rectTransform.offsetMax = Vector2.zero;
                var button = half.gameObject.AddComponent<Button>();
                button.interactable = can;
                button.onClick.AddListener(() => Change(c => set(c, GameSettings.Step(now, min, max, step, direction))));
                var arrow = BoxText(half.transform, side < 0 ? "‹" : "›", 56, side < 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
                arrow.font = UiStyle.Body;
                arrow.color = can ? UiStyle.TextMain : new Color(0.4f, 0.36f, 0.33f);
                UiStyle.Stretch(arrow.rectTransform, 22f);
            }
        }

        // A choice and a ›: the whole box turns it over.
        void Choice(MenuIcons.Kind kind, string label, string description, string value, Action<GameSettings> flip)
        {
            var control = Row(kind, label, description);
            var box = Box(control);
            box.raycastTarget = true;
            box.gameObject.AddComponent<Button>().onClick.AddListener(() => Change(flip));
            var shown = BoxText(box.transform, value, 32, TextAnchor.MiddleCenter);
            UiStyle.Stretch(shown.rectTransform);
            shown.rectTransform.offsetMax = new Vector2(-46f, 0f);
            var arrow = BoxText(box.transform, "›", 56, TextAnchor.MiddleRight);
            arrow.font = UiStyle.Body;
            UiStyle.Stretch(arrow.rectTransform, 22f);
        }

        // A volume: its number, then a slider on a red bar with a gold diamond to drag, in steps of 5.
        void Volume(MenuIcons.Kind kind, string label, string description, float now, Action<GameSettings, float> set)
        {
            var control = Row(kind, label, description);
            var number = BoxText(control, $"{now:0}", 36, TextAnchor.MiddleRight);
            number.rectTransform.anchorMin = new Vector2(0f, 0f);
            number.rectTransform.anchorMax = new Vector2(0f, 1f);
            number.rectTransform.pivot = new Vector2(0f, 0.5f);
            number.rectTransform.sizeDelta = new Vector2(84f, 0f);
            number.rectTransform.anchoredPosition = Vector2.zero;

            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(control, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(112f, -32f);
            rect.offsetMax = new Vector2(-18f, 32f);
            var track = UiStyle.Image(rect, "Track", new Color(0.18f, 0.06f, 0.05f, 1f), true);
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0f, 14f);
            track.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            var fillArea = UiStyle.Rect(rect, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(0f, 14f);
            var fill = UiStyle.Image(fillArea, "Fill", UiStyle.BloodBright);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = UiStyle.Rect(rect, "Handle Area");
            UiStyle.Stretch(handleArea);
            var handle = UiStyle.Image(handleArea, "Handle", new Color(0f, 0f, 0f, 0f), true);
            handle.rectTransform.sizeDelta = new Vector2(44f, 0f);
            var knob = MenuChrome.Diamond(handle.transform, 30f, UiStyle.Gold);
            knob.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(now);
            // Applied as it moves, without rebuilding the page under the finger.
            slider.onValueChanged.AddListener(v =>
            {
                var stepped = Mathf.Round(v / GameSettings.VolumeStep) * GameSettings.VolumeStep;
                number.text = $"{stepped:0}";
                var changed = SettingsDirector.Current.Copy();
                set(changed, stepped);
                SettingsDirector.Apply(changed);
            });
        }

        void DevAction(string label, string button, Action act)
        {
            var control = Row(MenuIcons.Kind.Wrench, label, "Development builds only");
            Button chip = null;
            chip = UiStyle.Button(control, button, () =>
            {
                act();
                chip.GetComponentInChildren<Text>().text = "Done";
            }, 32);
            UiStyle.Stretch((RectTransform)chip.transform);
        }

        static void Change(Action<GameSettings> edit)
        {
            var changed = SettingsDirector.Current.Copy();
            edit(changed);
            SettingsDirector.Apply(changed);
            if (current != null)
                current.Render();
        }

        static void Height(GameObject go, float height)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }
    }
}
