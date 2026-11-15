using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Her aim, shown as a small arc of pixels round her feet (2026-11-15, the owner: the crosshair on the enemies "is bad.
    /// It needs to be placed on the character", then "1, and remove the crosshair on enemies"): a short fan of dashes on the
    /// ground in front of her, toward her target, in the menus' pixels. It is as wide as her aim is loose and as warm:
    /// green and narrow while she aims true, yellow and wider walking, red and widest running (<see cref="AimRules"/>); it
    /// opens and closes smoothly, shows through a fight and fades a moment after it. The dashes are sprites on the
    /// ground, not touched by the lights; they follow her, so walls and bodies can hide them as they would a shadow. Added
    /// by <see cref="PlayerCombat"/>. (Until 2026-11-15 a crosshair on her target; 2026-11-08 to 2026-10-18 before it, an
    /// arc round her feet, and before that a wedge.)
    /// </summary>
    public class AimCone : MonoBehaviour
    {
        /// <summary>The half width of the arc, in degrees round the way she aims, at each aim: a picture of the spread, a
        /// little larger than the arrows' own (<see cref="AimRules.MaxStrayDegrees"/>) so that it reads on a small screen.</summary>
        public const float SteadyHalf = 7f, WalkingHalf = 22f, RunningHalf = 38f;

        /// <summary>How far from her feet the arc stands, in ground units.</summary>
        public const float Radius = 1.5f;

        /// <summary>The distance between two dashes along the arc, in degrees, and how many there can be.</summary>
        public const float StepDegrees = 6f;
        const int MaxDashes = 15;

        /// <summary>How long it stays after the last target, so it does not flicker between enemies.</summary>
        const float LingerSeconds = 1.0f;
        const float FadeSeconds = 0.3f;
        const float Ease = 14f;

        static readonly Color Steady = new Color32(96, 214, 96, 235);
        static readonly Color Walking = new Color32(236, 196, 64, 235);
        static readonly Color Running = new Color32(228, 64, 48, 235);
        static readonly Color Shadow = new Color32(8, 8, 12, 160);

        /// <summary>The half width for an aim (pure, for tests and the look).</summary>
        public static float HalfFor(float aim) => aim >= 1f ? SteadyHalf : aim >= AimRules.WalkingAim ? WalkingHalf : RunningHalf;

        /// <summary>The colour for an aim.</summary>
        public static Color ColourFor(float aim) => aim >= 1f ? Steady : aim >= AimRules.WalkingAim ? Walking : Running;

        /// <summary>The angles of the dashes, in degrees from the middle, for a half width: the middle one and pairs either
        /// side at <see cref="StepDegrees"/>, the last at the edge. Always at least three.</summary>
        public static int DashCount(float half) => Mathf.Clamp(2 * Mathf.CeilToInt(half / StepDegrees) + 1, 3, MaxDashes);

        /// <summary>The angle of the k-th dash (0 to count - 1) for a half width: spread evenly from one edge to the other.</summary>
        public static float AngleOf(int k, int count, float half) => count <= 1 ? 0f : Mathf.Lerp(-half, half, k / (float)(count - 1));

        PlayerCombat combat;
        PlayerController player;
        readonly SpriteRenderer[] dashes = new SpriteRenderer[MaxDashes];
        readonly SpriteRenderer[] shadows = new SpriteRenderer[MaxDashes];
        Transform root;
        EnemyController shownTarget;
        Vector2 lastDirection = Vector2.down;
        float half = SteadyHalf;
        float lastFight = -100f;
        static Sprite dot;

        void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            root = new GameObject("Aim Arc").transform;
            root.SetParent(transform, false);
            for (var i = 0; i < MaxDashes; i++)
            {
                shadows[i] = NewDash("Shadow " + i, 20);
                dashes[i] = NewDash("Dash " + i, 21);
            }
            Show(0);
        }

        // One drawn pixel (2 x 2 of the screen's at 27 a unit), on the ground under the characters, over the floor marks.
        SpriteRenderer NewDash(string name, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(root, false);
            var r = go.GetComponent<SpriteRenderer>();
            r.sprite = Dot;
            r.sortingLayerName = GameSortingLayers.Decals;
            r.sortingOrder = order;
            var unlit = SpriteMaterials.Unlit;
            if (unlit != null)
                r.sharedMaterial = unlit;
            r.enabled = false;
            return r;
        }

        static Sprite Dot
        {
            get
            {
                if (dot != null)
                    return dot;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                texture.SetPixels32(new[] { (Color32)Color.white, (Color32)Color.white, (Color32)Color.white, (Color32)Color.white });
                texture.Apply();
                dot = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), FireFxRules.PixelsPerUnit, 0, SpriteMeshType.FullRect);
                return dot;
            }
        }

        void OnDestroy()
        {
            if (root != null)
                Destroy(root.gameObject);
        }

        void LateUpdate()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (player == null || combat == null)
            {
                Show(0);
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
                Show(0);
                return;
            }
            var feet = IsoMath.WorldToGround(player.transform.position);
            if (shownTarget != null && shownTarget.IsAlive)
            {
                var toward = shownTarget.GroundPosition - feet;
                if (toward.sqrMagnitude > 1e-4f)
                    lastDirection = toward.normalized;
            }

            var aim = AimRules.Aim(player.StickPush);
            half = Mathf.Lerp(half, HalfFor(aim), 1f - Mathf.Exp(-Ease * Time.deltaTime));
            var count = DashCount(half);
            var colour = ColourFor(aim);
            // Fading out after the fight.
            var fade = Mathf.Clamp01((LingerSeconds - (Time.time - lastFight)) / FadeSeconds);
            colour.a *= fade;
            var shadow = Shadow;
            shadow.a *= fade;

            for (var k = 0; k < count; k++)
            {
                var angle = AngleOf(k, count, half);
                var ground = feet + Rotate(lastDirection, angle) * Radius;
                var world = FireFxRules.Snap(Elevation.World2(ground));
                var middle = Mathf.Abs(angle) < 0.01f || k == count / 2 && count % 2 == 1;
                // The dash that points at her target is a little longer.
                var dash = dashes[k];
                dash.enabled = true;
                dash.transform.position = world;
                dash.transform.localScale = middle ? new Vector3(1f, 2f, 1f) : Vector3.one;
                dash.color = colour;
                var back = shadows[k];
                back.enabled = true;
                back.transform.position = world + new Vector2(0f, -FireFxRules.Block);
                back.transform.localScale = dash.transform.localScale;
                back.color = shadow;
            }
            Show(count);
        }

        void Show(int count)
        {
            for (var i = 0; i < MaxDashes; i++)
            {
                var on = i < count;
                dashes[i].enabled = on;
                shadows[i].enabled = on;
            }
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            var c = Mathf.Cos(r);
            var s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
