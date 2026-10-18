using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Where the next shot or lunge comes from, shown around her rather than along the ground (the owner, 2026-10-19:
    /// "instead of making the attack telegraphs be in the form of lines emenating from the enemies. Make the show up as
    /// warning symbols around the character. So if an attack is about to come from the to left, a warning symbol (color
    /// coded according to attack) show up top left above the character"). Each wind-up aimed at her (an archer's shot, a
    /// charger's lunge, a thrown special, and as well as their lanes the specials that strike along a lane) puts a
    /// warning triangle on a ring round her chest, on the side of its enemy as seen on screen, in its attack's colour,
    /// throbbing faster as the attack nears. It goes when the attack is let loose, is cancelled or its enemy dies.
    /// Drawn on an overlay like <see cref="AimCone"/>, so walls and bodies never hide it. Owned by
    /// <see cref="EnemyManager"/>; pooled, so a fight makes no objects after its first warnings.
    /// </summary>
    public class IncomingWarnings : MonoBehaviour
    {
        /// <summary>The ring's radius and the symbol's size, in world units; the ring's centre this far above her feet.
        /// </summary>
        public const float Radius = 2.1f, Size = 1.05f, ChestHeight = 0.6f;

        /// <summary>Colours by attack: arrows and bolts red, lunges amber; a special shows its own colour.</summary>
        public static readonly Color ShotColor = new Color(1f, 0.25f, 0.18f, 1f);
        public static readonly Color LungeColor = new Color(1f, 0.68f, 0.18f, 1f);

        sealed class Warning
        {
            public Image Image;
            public EnemyController Source;
            public Vector2 From;
            public float Seconds;
            public float Elapsed;
            public bool Active;
        }

        readonly List<Warning> warnings = new List<Warning>();
        readonly List<float> placedAngles = new List<float>(16);
        RectTransform root;
        Canvas canvas;
        PlayerController player;
        static Sprite symbol;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => symbol = null;

        void Awake()
        {
            var canvasObject = new GameObject("Warning Canvas", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9; // Over the aim arc (8), under the HUD (10).
            root = (RectTransform)new GameObject("Warnings", typeof(RectTransform)).transform;
            root.SetParent(canvasObject.transform, false);
        }

        void OnDestroy()
        {
            if (canvas != null)
                Destroy(canvas.gameObject);
        }

        /// <summary>Shows a warning for <paramref name="seconds"/>: from its enemy while it lives (it follows the enemy),
        /// or from a fixed place when <paramref name="source"/> is null.</summary>
        public void Warn(EnemyController source, Vector2 from, float seconds, Color color)
        {
            Warning warning = null;
            foreach (var w in warnings)
                if (!w.Active)
                {
                    warning = w;
                    break;
                }
            if (warning == null)
            {
                var image = UiStyle.Image(root, "Warning", Color.white);
                image.sprite = Symbol;
                image.preserveAspect = true;
                warning = new Warning { Image = image };
                warnings.Add(warning);
            }
            warning.Source = source;
            warning.From = from;
            warning.Seconds = Mathf.Max(seconds, 0.05f);
            warning.Elapsed = 0f;
            warning.Active = true;
            warning.Image.color = color;
            warning.Image.enabled = false;
        }

        /// <summary>Takes down every warning from this enemy (its attack was let loose or broken off).</summary>
        public void Cancel(EnemyController source)
        {
            foreach (var w in warnings)
                if (w.Active && w.Source == source)
                    Finish(w);
        }

        /// <summary>Takes down every warning (a new level).</summary>
        public void Clear()
        {
            foreach (var w in warnings)
                if (w.Active)
                    Finish(w);
        }

        static void Finish(Warning w)
        {
            w.Active = false;
            w.Source = null;
            w.Image.enabled = false;
        }

        void LateUpdate()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            var cam = Camera.main;
            placedAngles.Clear();
            for (var i = 0; i < warnings.Count; i++)
            {
                var w = warnings[i];
                if (!w.Active)
                    continue;
                w.Elapsed += Time.deltaTime;
                if (w.Elapsed >= w.Seconds || (w.Source != null && !w.Source.IsAlive))
                {
                    Finish(w);
                    continue;
                }
                if (player == null || cam == null)
                {
                    w.Image.enabled = false;
                    continue;
                }
                var chestWorld = player.transform.position + Vector3.up * ChestHeight;
                var chest = (Vector2)cam.WorldToScreenPoint(chestWorld);
                var unit = Mathf.Abs(cam.WorldToScreenPoint(chestWorld + Vector3.right).x - chest.x);
                var from = w.Source != null ? w.Source.GroundPosition : w.From;
                var toward = (Vector2)cam.WorldToScreenPoint(IsoMath.GroundToWorld(from)) - chest;
                var angle = toward.sqrMagnitude > 1f ? Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg : 90f;
                var ring = WarningRules.RingFor(angle, placedAngles);
                placedAngles.Add(angle);
                var radians = angle * Mathf.Deg2Rad;
                var rect = w.Image.rectTransform;
                rect.position = chest + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (unit * (Radius + ring * Size * 0.9f));
                rect.sizeDelta = Vector2.one * (unit * Size);
                rect.localScale = Vector3.one * WarningRules.Scale(w.Elapsed, w.Seconds);
                w.Image.enabled = true;
            }
        }

        // A warning triangle with an "!", outlined: white so the colour tints it, the mark and the outline dark.
        static Sprite Symbol
        {
            get
            {
                if (symbol != null)
                    return symbol;
                string[] rows =
                {
                    "......oo......",
                    ".....o##o.....",
                    ".....o##o.....",
                    "....o####o....",
                    "....o#++#o....",
                    "...o##++##o...",
                    "...o##++##o...",
                    "..o###++###o..",
                    "..o###++###o..",
                    ".o##########o.",
                    ".o####++####o.",
                    "o############o",
                    "oooooooooooooo",
                };
                int h = rows.Length, w = rows[0].Length;
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = "Warning Symbol",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                var pixels = new Color32[w * h];
                for (var row = 0; row < h; row++)
                    for (var x = 0; x < w; x++)
                        pixels[(h - 1 - row) * w + x] = rows[row][x] switch
                        {
                            '#' => new Color32(255, 255, 255, 255),
                            '+' => new Color32(24, 14, 10, 255),
                            'o' => new Color32(20, 12, 10, 255),
                            _ => new Color32(0, 0, 0, 0),
                        };
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                // Full rect, no physics shape: the texture is no longer readable (CLAUDE.md, pixel art).
                symbol = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    Vector4.zero, false);
                return symbol;
            }
        }
    }

    /// <summary>The warning symbols' layout and throb (pure, tested).</summary>
    public static class WarningRules
    {
        /// <summary>Warnings closer together than this (degrees round her) stack outward rather than overlap.</summary>
        public const float CrowdDegrees = 26f;

        /// <summary>How many rings out a warning goes: one more for each warning already placed near its angle.</summary>
        public static int RingFor(float angle, IReadOnlyList<float> placed)
        {
            var ring = 0;
            for (var i = 0; i < placed.Count; i++)
                if (Mathf.Abs(Mathf.DeltaAngle(angle, placed[i])) < CrowdDegrees)
                    ring++;
            return ring;
        }

        /// <summary>The symbol's scale: it pops in over its first tenth of a second, then throbs, faster as the attack
        /// nears (two beats a second at the start, eight at the end).</summary>
        public static float Scale(float elapsed, float seconds)
        {
            var pop = Mathf.Clamp01(elapsed / 0.1f);
            var progress = Mathf.Clamp01(elapsed / Mathf.Max(seconds, 1e-4f));
            var beatsPerSecond = Mathf.Lerp(2f, 8f, progress);
            // The phase is the beats so far: the integral of a rate rising linearly with progress.
            var phase = elapsed * (2f + (beatsPerSecond - 2f) * 0.5f);
            var throb = 1f + 0.14f * Mathf.Sin(phase * Mathf.PI * 2f);
            return Mathf.Lerp(0.4f, 1f, pop) * throb;
        }
    }
}
