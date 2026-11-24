using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The way on marked plainly (2026-11-24, the owner: the level exits "are still not too clear"): three glowing arrows
    /// down the trail's middle pointing in, lit one after another from the clearing toward the dark, so the eye is led in.
    /// </summary>
    public class TrailArrows : MonoBehaviour
    {
        static readonly Color Glow = new Color(1f, 0.82f, 0.4f, 1f);
        static readonly string[] Picture =
        {
            "AA.....",
            "AAA....",
            ".AAA...",
            "..AAA..",
            ".AAA...",
            "AAA....",
            "AA.....",
        };

        static Sprite arrow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => arrow = null;

        readonly List<SpriteRenderer> arrows = new List<SpriteRenderer>();
        float clock;

        /// <param name="mouth">The trail's mouth, in world space.</param>
        /// <param name="inward">The world direction into the trail.</param>
        /// <param name="cell">The world length of one cell's step along it.</param>
        public static void Add(Transform parent, Vector3 mouth, Vector2 inward, float cell)
        {
            var go = new GameObject("Trail Arrows");
            go.transform.SetParent(parent, false);
            var marks = go.AddComponent<TrailArrows>();
            var angle = Mathf.Atan2(inward.y, inward.x) * Mathf.Rad2Deg;
            for (var i = 0; i < 3; i++)
            {
                var piece = GroundMarker.NewSprite("Arrow", ArrowSprite(), Glow, go.transform, 70);
                piece.transform.position = mouth + (Vector3)(inward * cell * (i * 1.2f - 1.6f));
                piece.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                piece.transform.localScale = Vector3.one * 1.8f;
                marks.arrows.Add(piece.GetComponent<SpriteRenderer>());
            }
        }

        static Sprite ArrowSprite()
        {
            if (arrow != null)
                return arrow;
            int h = Picture.Length, w = Picture[0].Length;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    pixels[y * w + x] = Picture[h - 1 - y][x] == 'A' ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            arrow = UnityEngine.Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 27f / 2f, 0, SpriteMeshType.FullRect);
            return arrow;
        }

        void Update()
        {
            clock += Time.deltaTime;
            for (var i = 0; i < arrows.Count; i++)
            {
                // A light running inward along them, once a little over a second.
                var lit = Mathf.Repeat(clock * 0.9f - i * 0.25f, 1f) < 0.3f;
                var c = Glow;
                c.a = lit ? 1f : 0.35f;
                arrows[i].color = c;
            }
        }
    }

    /// <summary>
    /// The way in shut behind her (2026-11-24, the owner: "It is also possible to walk back through an exit which should not
    /// be possible"): once she is out of the trail she arrived by, bushes grow across it with a rustle of leaves and it
    /// blocks her like the trees. Closed at once when she did not walk out of it.
    /// </summary>
    public class TrailGate : MonoBehaviour
    {
        const float OutBy = 0.4f;
        const float LongestWait = 6f;
        static readonly Color Leaves = new Color(0.35f, 0.6f, 0.3f, 1f);

        List<Vector3> cells;
        Vector3 mouth;
        Vector2 inward;
        string bush;
        PlayerController player;
        float clock;

        public bool Closed { get; private set; }

        public void Init(List<Vector3> trailCells, Vector3 mouthWorld, Vector2 into, string bushName)
        {
            cells = trailCells;
            mouth = mouthWorld;
            inward = into;
            bush = bushName;
        }

        void Update()
        {
            if (Closed)
                return;
            clock += Time.deltaTime;
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            var outside = player == null || Vector2.Dot((Vector2)(player.transform.position - mouth), inward) < -OutBy;
            if (outside || clock > LongestWait)
                Close();
        }

        void Close()
        {
            Closed = true;
            var obstacle = LayerMask.NameToLayer(GameLayers.Obstacle);
            for (var i = 0; i < cells.Count; i++)
            {
                var at = cells[i];
                var block = new GameObject("Blocked", typeof(CircleCollider2D));
                block.transform.SetParent(transform, false);
                block.transform.position = at;
                if (obstacle >= 0)
                    block.layer = obstacle;
                block.GetComponent<CircleCollider2D>().radius = 0.55f;
                // A bush on each cell nearer the clearing, so it reads as grown shut.
                if (Vector2.Dot((Vector2)(at - mouth), inward) < 0.9f)
                {
                    var name = "forest_bush_" + (1 + i % 3);
                    if (WorldArt.Place(WorldArt.Get(name) != null ? name : bush, transform, at) == null)
                        continue;
                    ArrowFx.Current?.Burst((Vector2)at, 0.5f, Leaves);
                }
            }
        }
    }
}
