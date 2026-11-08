using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Her aim, shown as a small crosshair on her target (2026-11-08, the owner: "remake the aim indicator. Maybe just a
    /// subtle crosshair that is red and expands when she is mooving to match, yellow and less expanded when movig less and
    /// green and not expanded when aiming true"): four short ticks round a gap, in the menus' pixels on an overlay, the
    /// gap and the colour by <see cref="AimRules"/>: green and tight standing, yellow and wider walking, red and widest
    /// running; the gap eases between them. It follows the target, and fades a moment after the fight. Added by
    /// <see cref="PlayerCombat"/>. (Until 2026-11-08 an arc round her feet, and before 2026-10-17 a wedge.)
    /// </summary>
    public class AimCone : MonoBehaviour
    {
        /// <summary>The gap from the centre to each tick, in menu pixels, at each aim.</summary>
        public const float SteadyGap = 2f, WalkingGap = 4f, RunningGap = 7f;

        /// <summary>A tick's length and thickness, in menu pixels.</summary>
        const float TickLength = 3f, TickWidth = 1f;

        /// <summary>How long it stays after the last target, so it does not flicker between enemies.</summary>
        const float LingerSeconds = 1.0f;
        const float Ease = 14f;

        static readonly Color Steady = new Color32(96, 214, 96, 230);
        static readonly Color Walking = new Color32(236, 196, 64, 230);
        static readonly Color Running = new Color32(228, 64, 48, 230);
        static readonly Color Shadow = new Color32(8, 8, 12, 170);

        /// <summary>The gap for an aim (pure, for tests and the look).</summary>
        public static float GapFor(float aim) => aim >= 1f ? SteadyGap : aim >= AimRules.WalkingAim ? WalkingGap : RunningGap;

        /// <summary>The colour for an aim.</summary>
        public static Color ColourFor(float aim) => aim >= 1f ? Steady : aim >= AimRules.WalkingAim ? Walking : Running;

        PlayerCombat combat;
        PlayerController player;
        Canvas canvas;
        RectTransform root;
        readonly Image[] ticks = new Image[4];
        readonly Image[] shadows = new Image[4];
        EnemyController shownTarget;
        Vector3 lastPoint;
        float gap = SteadyGap;
        float lastFight = -100f;
        float scale = 1f;

        void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            // Under the HUD, over the world and the loot names' overlay (5).
            canvas = PixelUi.Available ? PixelUi.MakeCanvas(null, "Aim Canvas", 8) : MakePlainCanvas();
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;
            scale = PixelUi.Available ? PixelUi.Scale(Screen.width) : 1f;
            var rootObject = new GameObject("Crosshair", typeof(RectTransform));
            rootObject.transform.SetParent(canvas.transform, false);
            root = (RectTransform)rootObject.transform;
            root.anchorMin = root.anchorMax = Vector2.zero;
            for (var i = 0; i < 4; i++)
            {
                shadows[i] = Tick("Shadow " + i, Shadow);
                ticks[i] = Tick("Tick " + i, Steady);
            }
            Show(false);
        }

        static Canvas MakePlainCanvas()
        {
            var go = new GameObject("Aim Canvas", typeof(Canvas));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 8;
            return c;
        }

        Image Tick(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        void OnDestroy()
        {
            if (canvas != null)
                Destroy(canvas.gameObject);
        }

        void LateUpdate()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            var cam = Camera.main;
            if (player == null || combat == null || cam == null)
            {
                Show(false);
                return;
            }
            var target = combat.Target;
            if (target != null && target.IsAlive)
            {
                lastFight = Time.time;
                shownTarget = target;
            }
            if (Time.time - lastFight > LingerSeconds)
            {
                Show(false);
                return;
            }
            // On the target's body: a little above its feet, by its size.
            if (shownTarget != null && shownTarget.IsAlive)
            {
                var size = shownTarget.Definition != null ? shownTarget.Definition.VisualScale : 1f;
                lastPoint = shownTarget.transform.position + new Vector3(0f, 0.45f * size, 0f);
            }
            var screen = cam.WorldToScreenPoint(lastPoint);
            if (screen.z < 0f)
            {
                Show(false);
                return;
            }
            var aim = AimRules.Aim(player.StickPush);
            gap = Mathf.Lerp(gap, GapFor(aim), 1f - Mathf.Exp(-Ease * Time.deltaTime));
            var colour = ColourFor(aim);
            // Fading out after the fight.
            var fade = Mathf.Clamp01((LingerSeconds - (Time.time - lastFight)) / 0.3f);
            colour.a *= fade;
            var shadow = Shadow;
            shadow.a *= fade;

            // On whole menu pixels.
            root.position = new Vector3(Mathf.Round(screen.x / scale) * scale, Mathf.Round(screen.y / scale) * scale, 0f);
            var g = Mathf.Round(gap);
            for (var i = 0; i < 4; i++)
            {
                // Right, up, left, down.
                var horizontal = i % 2 == 0;
                var sign = i < 2 ? 1f : -1f;
                var along = sign * (g + TickLength / 2f);
                var position = horizontal ? new Vector2(along, 0f) : new Vector2(0f, along);
                var size = horizontal ? new Vector2(TickLength, TickWidth) : new Vector2(TickWidth, TickLength);
                Place(ticks[i], position, size, colour);
                Place(shadows[i], position + new Vector2(1f, -1f), size, shadow);
            }
            Show(true);
        }

        static void Place(Image image, Vector2 position, Vector2 size, Color colour)
        {
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.color = colour;
        }

        void Show(bool on)
        {
            for (var i = 0; i < 4; i++)
            {
                ticks[i].enabled = on;
                shadows[i].enabled = on;
            }
        }
    }
}
