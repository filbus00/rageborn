using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The loadout (Docs/06, Loadout; Docs/02; the owner's decisions of 2026-09-27: it opens from level 9, and each slot's
    /// trigger is its skill's own or one of two alternatives). A bottom sheet over the Bag: the four slots in cast order,
    /// each with buttons to move it up or down, the skill (a tap opens the list of the class's skills, locked ones shown
    /// with their level) and its trigger (a tap cycles through the three choices). Changes go straight into
    /// <see cref="GameSession.Loadout"/>, which is saved. Built in code the first time it opens; the Bag has paused the
    /// game. Placeholder look until the UI art exists (Docs/09, section 11).
    /// </summary>
    public class LoadoutScreen : MonoBehaviour
    {
        /// <summary>The level the loadout opens at (Q4).</summary>
        public const int OpensAtLevel = 9;

        static readonly Color SheetColor = new Color(0.09f, 0.08f, 0.08f, 0.98f);
        static readonly Color StrokeColor = new Color(0.9f, 0.45f, 0.2f);
        static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.16f);
        static readonly Color ChipColor = new Color(0.9f, 0.45f, 0.2f, 0.35f);
        static readonly Color LockedColor = new Color(1f, 1f, 1f, 0.05f);

        const float RowHeight = 168f; // 56 points, the docs' preferred tap target.

        static LoadoutScreen current;

        RectTransform list;
        Text hint;
        PlayerCombat combat;
        int choosingSlot = -1;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        public static void Open()
        {
            var combat = FindAnyObjectByType<PlayerCombat>();
            if (combat == null)
                return;
            if (current == null)
                current = Create();
            current.combat = combat;
            current.choosingSlot = -1;
            current.gameObject.SetActive(true);
            current.Render();
        }

        public void Close() => gameObject.SetActive(false);

        /// <summary>Closes the loadout if it is open (the Bag closing takes it along).</summary>
        public static void CloseIfOpen()
        {
            if (IsOpen)
                current.Close();
        }

        static LoadoutScreen Create()
        {
            var canvasObject = new GameObject("Loadout Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = new GameObject("Loadout", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<LoadoutScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Button));
            sheet.transform.SetParent(root.transform, false);
            // The sheet swallows taps so only the dimmed area above it closes.
            sheet.GetComponent<Button>().transition = Selectable.Transition.None;
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 1900f);
            sheet.GetComponent<Image>().color = SheetColor;
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 48);
            layout.spacing = 16f;
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

            var title = NewText(sheet.transform, "Skills", 56, FontStyle.Bold);
            title.color = StrokeColor;
            Height(title.gameObject, 90f);
            screen.hint = NewText(sheet.transform, "", 32, FontStyle.Normal);
            screen.hint.color = new Color(1f, 1f, 1f, 0.7f);
            Height(screen.hint.gameObject, 90f);

            var listObject = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listObject.transform.SetParent(sheet.transform, false);
            var listLayout = listObject.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 12f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            screen.list = (RectTransform)listObject.transform;

            var close = NewButton(sheet.transform, "Close", screen.Close);
            Height(close.gameObject, RowHeight);
            return screen;
        }

        void Render()
        {
            for (var i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            if (choosingSlot >= 0)
                RenderChoices();
            else
                RenderSlots();
        }

        void RenderSlots()
        {
            var session = GameSession.Current;
            var points = session.SkillLevels.Available(session.Level);
            var editable = session.Level >= OpensAtLevel;
            hint.text = (points > 0 ? $"<color=#FFB45A>{points} skill point{(points == 1 ? "" : "s")} to spend (+).</color> " : "") +
                        (editable ? "Slot 1 fires first. Tap a skill to change it, its trigger to choose when it fires."
                            : $"Choosing skills and triggers opens at level {OpensAtLevel}.");
            var loadout = session.Loadout;
            for (var slot = 0; slot < SkillLoadout.SlotCount; slot++)
            {
                var index = slot;
                var skill = combat.FindSkill(loadout.SkillAt(slot));
                var row = NewRow(list);
                var up = NewButton(row.transform, "Up", () => Move(index, -1));
                Width(up.gameObject, 110f, 0f);
                up.GetComponentInChildren<Text>().fontSize = 30;
                up.interactable = editable && slot > 0;
                var down = NewButton(row.transform, "Down", () => Move(index, 1));
                Width(down.gameObject, 110f, 0f);
                down.GetComponentInChildren<Text>().fontSize = 30;
                down.interactable = editable && slot < SkillLoadout.SlotCount - 1;
                var level = skill != null ? session.SkillLevels.LevelOf(skill.name) : 0;
                var name = NewButton(row.transform, $"{slot + 1}. " + (skill != null ? $"{skill.DisplayName} <size=30><color=#BBBBBB>{level}</color></size>" : "<color=#999999>empty</color>"), () =>
                {
                    choosingSlot = index;
                    Render();
                });
                Width(name.gameObject, 0f, 1f);
                name.interactable = editable;
                var raise = NewButton(row.transform, "+", () => Raise(skill));
                Width(raise.gameObject, 100f, 0f);
                raise.interactable = skill != null && session.SkillLevels.CanRaise(skill.name, skill.UnlockLevel, session.Level);
                var trigger = NewButton(row.transform, skill != null ? TriggerLabel(skill, loadout.TriggerAt(slot)) : "", () => CycleTrigger(index));
                Width(trigger.gameObject, 340f, 0f);
                trigger.GetComponent<Image>().color = skill != null ? ChipColor : LockedColor;
                trigger.interactable = editable && skill != null;
                trigger.GetComponentInChildren<Text>().fontSize = 28;
            }
        }

        void Raise(SkillDefinition skill)
        {
            var session = GameSession.Current;
            if (skill != null)
                session.SkillLevels.Raise(skill.name, skill.UnlockLevel, session.Level);
            Render();
        }

        void RenderChoices()
        {
            hint.text = $"Choose the skill for slot {choosingSlot + 1}. One already carried swaps places.";
            var loadout = GameSession.Current.Loadout;
            var level = GameSession.Current.Level;
            foreach (var skill in combat.Skills)
            {
                if (skill == null)
                    continue;
                var chosen = skill;
                var unlocked = level >= skill.UnlockLevel;
                var inSlot = loadout.SlotOf(skill.name);
                var skillLevel = GameSession.Current.SkillLevels.LevelOf(skill.name);
                var detail = !unlocked ? $"   <size=30><color=#999999>opens at level {skill.UnlockLevel}</color></size>"
                    : $"   <size=30><color=#999999>level {skillLevel}" + (inSlot >= 0 ? $", slot {inSlot + 1}" : "") + "</color></size>";
                var button = NewButton(list, skill.DisplayName + detail, () =>
                {
                    loadout.Equip(choosingSlot, chosen.name);
                    choosingSlot = -1;
                    Render();
                });
                Height(button.gameObject, 132f);
                button.interactable = unlocked;
                if (!unlocked)
                    button.GetComponent<Image>().color = LockedColor;
            }
            var empty = NewButton(list, "<color=#999999>Leave the slot empty</color>", () =>
            {
                loadout.Clear(choosingSlot);
                choosingSlot = -1;
                Render();
            });
            Height(empty.gameObject, 132f);
            var back = NewButton(list, "Back", () =>
            {
                choosingSlot = -1;
                Render();
            });
            Height(back.gameObject, 132f);
        }

        void Move(int slot, int delta)
        {
            GameSession.Current.Loadout.Swap(slot, slot + delta);
            Render();
        }

        // The skill's own trigger, then its two alternatives, then back to its own.
        void CycleTrigger(int slot)
        {
            var loadout = GameSession.Current.Loadout;
            var skill = combat.FindSkill(loadout.SkillAt(slot));
            if (skill == null)
                return;
            var choices = new System.Collections.Generic.List<SkillTrigger> { SkillTrigger.Default };
            choices.AddRange(skill.AlternativeTriggers);
            var next = (choices.IndexOf(loadout.TriggerAt(slot)) + 1) % choices.Count;
            loadout.SetTrigger(slot, choices[next]);
            Render();
        }

        static string TriggerLabel(SkillDefinition skill, SkillTrigger trigger) =>
            SkillTriggerRules.Label(trigger, string.IsNullOrEmpty(skill.TriggerText) ? "Its own" : skill.TriggerText);

        static GameObject NewRow(Transform parent)
        {
            var row = new GameObject("Slot", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            // Children's widths are controlled, or a label's flexible width is ignored (CLAUDE.md, Loot).
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            Height(row, RowHeight);
            return row;
        }

        static Button NewButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = ButtonColor;
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var text = NewText(go.transform, label, 40, FontStyle.Bold);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
            return go.GetComponent<Button>();
        }

        static Text NewText(Transform parent, string content, int size, FontStyle style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
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
