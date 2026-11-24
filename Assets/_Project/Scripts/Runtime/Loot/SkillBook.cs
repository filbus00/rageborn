using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A skill book a boss drops (2026-11-24, the owner: "make the boss drop a skill book that is picked up and small animation
    /// when the book is picked up and the skill learned"): it hops out of the boss, then bobs on the ground glinting. Walked
    /// over, it rises, spins and swells, bursts in gold, and the company learns its skill (<see cref="TapSkillRules"/>) for
    /// good; a banner says so and the game saves. A book left lying is lost; the next boss drops it again.
    /// </summary>
    public class SkillBook : MonoBehaviour
    {
        const float PickupRadius = 0.9f;
        const float HopSeconds = 0.55f;
        const float HopHeight = 1.1f;
        const float ReadSeconds = 1.1f;
        const float RiseHeight = 1.4f;
        static readonly Color Gold = new Color(1f, 0.85f, 0.35f, 1f);

        // The tome, a drawn pixel each 2 game pixels: a red cover, gold corners and clasp, the pages' edge.
        static readonly string[] Picture =
        {
            ".AAAAAAAAAA.",
            "AJMMMMMMMMJA",
            "AMMMMJJMMMMA",
            "AMMMJMMJMMMA",
            "AMMMMJJMMMMA",
            "AJMMMMMMMMJA",
            "ASSSSSSSSSSA",
            "APPPPPPPPPPA",
            ".AAAAAAAAAA.",
        };

        static Sprite sprite;

        string skillId;
        Vector2 ground;
        SpriteRenderer body;
        PlayerController player;
        float clock;
        float sparkle;
        float readClock = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => sprite = null;

        public string SkillId => skillId;

        /// <summary>A book drops where a boss fell, unless one for the same skill already lies in the level.</summary>
        public static SkillBook Drop(string skillId, Vector2 groundPosition)
        {
            if (string.IsNullOrEmpty(skillId))
                return null;
            foreach (var lying in FindObjectsByType<SkillBook>(FindObjectsSortMode.None))
                if (lying.skillId == skillId)
                    return null;
            var go = new GameObject("Skill Book");
            var book = go.AddComponent<SkillBook>();
            book.skillId = skillId;
            book.ground = groundPosition;
            var art = new GameObject("Tome", typeof(SpriteRenderer));
            art.transform.SetParent(go.transform, false);
            book.body = art.GetComponent<SpriteRenderer>();
            book.body.sprite = BookSprite();
            book.body.sortingLayerName = GameSortingLayers.Entities;
            book.body.sortingOrder = 5;
            SpriteMaterials.MakeUnlit(book.body);
            go.transform.position = Elevation.World2(groundPosition);
            var skill = TapSkillRules.Get(skillId);
            TravelArt.Label(go.transform, skill != null ? "Book of " + skill.Name : "Skill book", Gold, 1.4f);
            Sfx.Play(SoundId.LevelUp, 0.5f);
            return book;
        }

        static Sprite BookSprite()
        {
            if (sprite != null)
                return sprite;
            var colours = new System.Collections.Generic.Dictionary<char, Color32>
            {
                { 'A', new Color32(0x14, 0x0f, 0x1c, 255) }, { 'M', new Color32(0xc0, 0x2a, 0x2e, 255) },
                { 'S', new Color32(0x7a, 0x16, 0x26, 255) }, { 'J', new Color32(0xe6, 0xc2, 0x5c, 255) },
                { 'P', new Color32(0xf2, 0xee, 0xde, 255) },
            };
            int h = Picture.Length, w = Picture[0].Length;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var c = Picture[h - 1 - y][x];
                    pixels[y * w + x] = colours.TryGetValue(c, out var colour) ? colour : new Color32(0, 0, 0, 0);
                }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Two game pixels a drawn pixel, at the screen's 27 a unit (the camp's and the forest's art).
            sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 27f / 2f, 0, SpriteMeshType.FullRect);
            return sprite;
        }

        void Update()
        {
            clock += Time.deltaTime;
            var art = body.transform;
            if (readClock >= 0f)
            {
                Read(art);
                return;
            }
            // The hop out of the boss, then a slow bob and a glint now and then.
            var lift = clock < HopSeconds
                ? Mathf.Sin(clock / HopSeconds * Mathf.PI) * HopHeight
                : 0.12f + Mathf.Sin((clock - HopSeconds) * 3f) * 0.08f;
            art.localPosition = new Vector3(0f, lift, 0f);
            sparkle -= Time.deltaTime;
            if (sparkle <= 0f && clock > HopSeconds)
            {
                sparkle = 0.45f;
                ArrowFx.Current?.Sparks((Vector2)art.position + new Vector2(Random.Range(-0.2f, 0.2f), 0.2f), Vector2.up, 0.1f, Gold, 2);
            }
            if (clock < HopSeconds)
                return;
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return;
            var playerGround = IsoMath.WorldToGround(player.transform.position);
            if (Vector2.Distance(playerGround, ground) <= PickupRadius)
            {
                readClock = 0f;
                // The name over it goes; only the book itself rises.
                foreach (Transform child in transform)
                    if (child != art)
                        child.gameObject.SetActive(false);
                Sfx.Play(SoundId.LevelUp, 1f);
                ArrowFx.Current?.Burst((Vector2)transform.position, 0.6f, Gold);
            }
        }

        // Picked up: it rises over her, spins and swells, then bursts and the skill is hers.
        void Read(Transform art)
        {
            readClock += Time.deltaTime;
            var t = Mathf.Clamp01(readClock / ReadSeconds);
            var eased = 1f - (1f - t) * (1f - t);
            if (player != null)
                transform.position = Vector3.Lerp(transform.position, player.transform.position, Time.deltaTime * 6f);
            art.localPosition = new Vector3(0f, 0.3f + eased * RiseHeight, 0f);
            var size = 1f + eased * 0.8f;
            art.localScale = new Vector3(Mathf.Cos(readClock * 10f) * size, size, 1f);
            var colour = body.color;
            colour.a = t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f;
            body.color = colour;
            sparkle -= Time.deltaTime;
            if (sparkle <= 0f)
            {
                sparkle = 0.08f;
                ArrowFx.Current?.Sparks((Vector2)art.position, Random.insideUnitCircle.normalized, 0.1f, Gold, 2);
            }
            if (t < 1f)
                return;
            ArrowFx.Current?.Burst((Vector2)art.position, 1.2f, Gold);
            Learn();
            Destroy(gameObject);
        }

        void Learn()
        {
            var session = GameSession.Current;
            var skill = TapSkillRules.Get(skillId);
            if (session == null || skill == null || !session.Company.LearnTapSkill(skillId))
                return;
            Sfx.Play(SoundId.LevelUp, 1f);
            (HintBanner.Current != null ? HintBanner.Current : HintBanner.Create()).Show(
                $"Learned {skill.Name}! Double tap to use it. Choose your double tap skill in the Bag's Skills page.");
            SaveDirector.SaveNow();
        }
    }
}
