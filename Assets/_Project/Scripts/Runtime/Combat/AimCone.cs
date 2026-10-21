using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Her aim on the ground (the owner, 2026-10-17: "change the aming to partial circles around the character, the
    /// partial circle shows the firing radius for the current movement speed. The partial circle should be present during
    /// all of combat. It should have a background and the firing radius should be another foreground color"): a dark arc
    /// round her feet facing her target (or where she last aimed), and on it, in white, amber or red, the arc her arrows
    /// may stray over at her current pace (<see cref="AimRules"/>): a thin tick standing, wider walking, widest running.
    /// Shown while she is in a fight. Drawn like the Focus arc (<see cref="RageArc"/>): an overlay that follows her feet,
    /// so the dark floor and the enemies' bodies never hide it, with the arcs turned inside a parent squashed like the
    /// floor (<see cref="IsoMath"/>). Added by <see cref="PlayerCombat"/>. (Until 2026-10-17 a wedge toward the target.)
    /// </summary>
    public class AimCone : MonoBehaviour
    {
        /// <summary>The arc's radius and band, in ground units, and the background's half span in degrees.</summary>
        public const float Radius = 1.35f;
        public const float Band = 0.24f;
        public const float BackgroundHalfDegrees = 45f;

        /// <summary>The foreground never thinner than this either side, so perfect aim still shows a tick.</summary>
        public const float MinimumHalfDegrees = 3f;

        /// <summary>How long the arc stays after the last target, so it does not flicker between enemies.</summary>
        const float LingerSeconds = 1.5f;
        const float TurnSpeed = 14f;

        static readonly Color Background = new Color(0.85f, 0.8f, 0.72f, 0.28f);
        static readonly Color Steady = new Color(0.95f, 0.95f, 0.9f, 0.9f);
        static readonly Color Walking = new Color(1f, 0.74f, 0.25f, 0.9f);
        static readonly Color Running = new Color(1f, 0.3f, 0.2f, 0.9f);

        PlayerCombat combat;
        PlayerController player;
        RectTransform root;
        RectTransform ground;
        Image back;
        Image front;
        float angle;
        bool hasAngle;
        float shownAim = -1f;
        float lastFight = -100f;

        static readonly Dictionary<int, Sprite> Arcs = new Dictionary<int, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Arcs.Clear();

        /// <summary>The foreground's half span for an aim, in degrees.</summary>
        public static float FiringHalfDegrees(float aim) => Mathf.Max(MinimumHalfDegrees, AimRules.MaxStrayDegrees(aim));

        Canvas canvas;

        void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            var canvasObject = new GameObject("Aim Arc Canvas", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8; // Under the Focus arc (9) and the HUD.
            var rootObject = new GameObject("Aim Arc", typeof(RectTransform));
            rootObject.transform.SetParent(canvasObject.transform, false);
            root = (RectTransform)rootObject.transform;
            var groundObject = new GameObject("Ground", typeof(RectTransform));
            groundObject.transform.SetParent(root, false);
            ground = (RectTransform)groundObject.transform;
            ground.localScale = new Vector3(1f, IsoMath.GroundSquash, 1f);
            back = NewArc("Background");
            back.sprite = ArcSprite(BackgroundHalfDegrees);
            back.color = Background;
            front = NewArc("Firing");
            Show(false);
        }

        Image NewArc(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(ground, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
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
            if (player == null || combat == null)
            {
                Show(false);
                return;
            }

            var origin = IsoMath.WorldToGround(player.transform.position);
            var target = combat.Target;
            if (target != null && target.IsAlive)
            {
                lastFight = Time.time;
                var toTarget = target.GroundPosition - origin;
                if (toTarget.sqrMagnitude > 1e-4f)
                {
                    var wanted = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                    angle = hasAngle ? Mathf.LerpAngle(angle, wanted, 1f - Mathf.Exp(-TurnSpeed * Time.deltaTime)) : wanted;
                    hasAngle = true;
                }
            }
            // All through the fight: from the first target until a moment after the last.
            if (!hasAngle || Time.time - lastFight > LingerSeconds)
            {
                Show(false);
                return;
            }

            var aim = AimRules.Aim(player.StickPush);
            if (aim != shownAim)
            {
                shownAim = aim;
                front.sprite = ArcSprite(FiringHalfDegrees(aim));
                front.color = aim >= 1f ? Steady : aim >= AimRules.WalkingAim ? Walking : Running;
            }
            var cam = Camera.main;
            if (cam == null)
            {
                Show(false);
                return;
            }
            // Sized from the camera each frame, so the arc keeps its size on the ground whatever the zoom.
            var feet = Elevation.Lifted(player.transform.position);
            var center = cam.WorldToScreenPoint(feet);
            var unit = Mathf.Abs(cam.WorldToScreenPoint(feet + Vector3.right).x - center.x);
            root.position = center;
            var size = Vector2.one * (back.sprite.rect.width / PixelArt.PixelsPerUnit * unit);
            back.rectTransform.sizeDelta = size;
            front.rectTransform.sizeDelta = size;
            var turn = Quaternion.Euler(0f, 0f, angle);
            back.rectTransform.localRotation = turn;
            front.rectTransform.localRotation = turn;
            Show(true);
        }

        void Show(bool on)
        {
            back.enabled = on;
            front.enabled = on;
        }

        /// <summary>A white ring arc, <see cref="Radius"/> across its middle and <see cref="Band"/> thick, spanning half
        /// degrees either side of +x, at the pixel art's scale, its pivot at the ring's centre; a brighter rim on its
        /// edges. Cached by span.</summary>
        static Sprite ArcSprite(float halfDegrees)
        {
            var key = Mathf.RoundToInt(halfDegrees * 10f);
            if (Arcs.TryGetValue(key, out var sprite) && sprite != null)
                return sprite;
            var ppu = PixelArt.PixelsPerUnit;
            var outer = (Radius + Band * 0.5f) * ppu;
            var inner = (Radius - Band * 0.5f) * ppu;
            var size = Mathf.CeilToInt(outer * 2f) + 2;
            var half = size / 2f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Aim Arc " + key,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx = x + 0.5f - half;
                    var dy = y + 0.5f - half;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                    byte alpha = 0;
                    if (r >= inner && r <= outer && a <= halfDegrees)
                    {
                        var edge = r - inner < 1.5f || outer - r < 1.5f || (halfDegrees - a) * Mathf.Deg2Rad * r < 1.5f;
                        alpha = (byte)(edge ? 255 : 170);
                    }
                    pixels[x + y * size] = new Color32(255, 255, 255, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable (CLAUDE.md, pixel art).
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            return Arcs[key] = sprite;
        }
    }
}
