using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// The portal home (the owner, 2026-10-15: "Add a portal function that ends the run and lets you keep all you have
    /// found", anytime from the Bag). Since 2026-11-08 (the owner: "make an animation of a portal opening for 5 sec before
    /// you can step trough. You keep control of the character while the portal is opening") it is a portal standing where
    /// she opened it: it takes <see cref="Seconds"/> to open (a ring of runes drawn on the ground, a slit of light, a swirl
    /// widening; Resources/Art/World/portal_home, ArtSource/pixel/sixteen/portal_16.py), she moves and fights meanwhile,
    /// and once it is open, stepping into it ends the run with everything she carries kept
    /// (<see cref="RunDirector.TakePortal"/>). A hit no longer breaks it. It stays open until she steps in or leaves.
    /// </summary>
    public class PortalChannel : MonoBehaviour
    {
        public const float Seconds = 5f;

        /// <summary>How near her feet must come to its middle to step through, in ground units.</summary>
        public const float StepRadius = 0.6f;

        // The sheet: OpenFrames over the opening, then LoopFrames over and over (portal_16.py).
        const int OpenFrames = 25, LoopFrames = 8, CellWidth = 36, CellHeight = 48;
        const float LoopSeconds = 0.8f;
        const float PixelsPerUnit = 13.5f;
        const float PivotY = 6f / CellHeight;

        static PortalChannel current;
        static readonly List<Sprite> frames = new List<Sprite>();
        PlayerHealth health;
        Transform player;
        SpriteRenderer picture;
        Vector2 ground;
        float age;
        bool open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            frames.Clear();
        }

        /// <summary>A portal can be opened: in the dungeon, in a run, alive, none opened yet.</summary>
        public static bool CanOpen
        {
            get
            {
                if (!Features.Roguelite || SceneManager.GetActiveScene().name != SceneTravel.DungeonScene || SceneTravel.IsTravelling)
                    return false;
                var health = FindAnyObjectByType<PlayerHealth>();
                return health != null && health.IsAlive;
            }
        }

        /// <summary>A portal is opening or open on this level.</summary>
        public static bool IsOpening => current != null;

        /// <summary>The portal is open: she can step through.</summary>
        public static bool IsOpen => current != null && current.open;

        public static void Begin()
        {
            if (current != null || !CanOpen)
                return;
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller == null)
                return;
            var go = new GameObject("Portal Home");
            current = go.AddComponent<PortalChannel>();
            current.health = FindAnyObjectByType<PlayerHealth>();
            current.player = controller.transform;
            current.ground = IsoMath.WorldToGround(controller.transform.position);
            go.transform.position = controller.transform.position;
            current.picture = go.AddComponent<SpriteRenderer>();
            current.picture.sortingLayerName = GameSortingLayers.Entities;
            current.picture.spriteSortPoint = SpriteSortPoint.Pivot;
            current.picture.sprite = Frame(0);
            Sfx.Play(SoundId.Hint, 0.8f);
            Banner("A portal home is opening...");
        }

        // The sheet cut into frames once.
        static Sprite Frame(int index)
        {
            if (frames.Count == 0)
            {
                var sheet = Resources.Load<Texture2D>(WorldArt.Folder + "/portal_home");
                if (sheet == null)
                    return null;
                sheet.filterMode = FilterMode.Point;
                for (var i = 0; i < OpenFrames + LoopFrames; i++)
                {
                    var sprite = Sprite.Create(sheet, new Rect(i * CellWidth, 0f, CellWidth, CellHeight), new Vector2(0.5f, PivotY),
                        PixelsPerUnit, 0, SpriteMeshType.FullRect);
                    sprite.name = $"portal_home_{i:00}";
                    frames.Add(sprite);
                }
            }
            return frames.Count > 0 ? frames[Mathf.Clamp(index, 0, frames.Count - 1)] : null;
        }

        void Update()
        {
            if (health == null || !health.IsAlive || player == null)
            {
                Destroy(gameObject);
                return;
            }
            age += Time.deltaTime;
            if (!open)
            {
                picture.sprite = Frame(Mathf.Min(OpenFrames - 1, Mathf.FloorToInt(age / Seconds * OpenFrames)));
                if (age < Seconds)
                    return;
                open = true;
                age = 0f;
                Sfx.Play(SoundId.LevelUp, 0.6f);
                Banner("The portal is open. Step into it to go home.");
            }
            picture.sprite = Frame(OpenFrames + Mathf.FloorToInt(age / LoopSeconds * LoopFrames) % LoopFrames);
            if ((IsoMath.WorldToGround(player.position) - ground).sqrMagnitude <= StepRadius * StepRadius && !SceneTravel.IsTravelling)
            {
                enabled = false;
                RunDirector.TakePortal();
            }
        }

        static void Banner(string text) => (HintBanner.Current != null ? HintBanner.Current : HintBanner.Create()).Show(text);

        void OnDestroy()
        {
            if (current == this)
                current = null;
        }
    }
}
