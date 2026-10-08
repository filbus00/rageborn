using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>
    /// Sets each scene's light (Docs/05-world-and-content.md: dark scenes lit by the player's ember and enemy effects;
    /// dynamic 2D lights on the player and a few props only): the dungeon near black, the town at dusk, a warm flickering
    /// ember on the character and a light at the Forge. The stairs, waypoints and portals add their own through
    /// <see cref="WorldLights"/>. A scene that is neither (the Sandbox) is left fully lit. Created from code and kept
    /// across scenes. The numbers are in <see cref="LightingRules"/>.
    /// </summary>
    public class LightingDirector : MonoBehaviour
    {
        static float characterShade = 1f;

        readonly List<Light2D> flickering = new List<Light2D>();
        readonly List<float> baseIntensity = new List<float>();

        /// <summary>See <see cref="LightingRules.Mood.CharacterShade"/>; 1 in a fully lit scene.</summary>
        public static float CharacterShade => characterShade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => characterShade = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Lighting Director");
            DontDestroyOnLoad(go);
            go.AddComponent<LightingDirector>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            flickering.Clear();
            baseIntensity.Clear();

            var mood = WorldLights.MoodOf(scene);
            characterShade = mood.HasValue ? mood.Value.CharacterShade : 1f;
            if (!mood.HasValue)
                return;

            foreach (var light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (light.lightType == Light2D.LightType.Global)
                {
                    light.color = mood.Value.Ambient;
                    light.intensity = mood.Value.AmbientIntensity;
                }

            var player = FindAnyObjectByType<PlayerController>();
            // Pellam's lantern and the great lamp (2026-10-08, quests) carry her light further below.
            var reach = scene.name == SceneTravel.DungeonScene ? GameSession.Current.Quests.Boons.Light : 1f;
            if (player != null)
                Flicker(WorldLights.Add(player.transform, LightingRules.EmberColor, LightingRules.EmberIntensity,
                    LightingRules.EmberInnerRadius * reach, LightingRules.EmberOuterRadius * reach, 0.4f));

            // The dungeon shades its own floor as it paints it (DungeonLevel); the town's is painted in the scene.
            if (scene.name == SceneTravel.TownScene)
                foreach (var tilemap in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                    if (tilemap.name == "Ground")
                    {
                        if (!GroundPainter.PaintTown(tilemap))
                            DungeonArt.PaveTown(tilemap);
                        WorldLights.ShadeGround(tilemap);
                    }

            // The smith's fire: the one warm prop in town besides the character.
            var forge = FindAnyObjectByType<ForgeNpc>();
            if (forge != null)
                Flicker(WorldLights.Add(forge.transform, new Color(1f, 0.5f, 0.2f), 1.2f, 0.5f, 4f));
        }

        void Flicker(Light2D light)
        {
            if (light == null)
                return;
            flickering.Add(light);
            baseIntensity.Add(light.intensity);
        }

        void Update()
        {
            var time = Time.time;
            for (var i = 0; i < flickering.Count; i++)
                if (flickering[i] != null)
                    flickering[i].intensity = baseIntensity[i] * LightingRules.Flicker(time, i * 2.3f);
        }
    }

    /// <summary>
    /// Adds the few prop lights (a stairway, a waypoint, a portal) in a dark scene only, since in a fully lit one they
    /// would only wash the floor out. A point light is a circle on screen whatever its transform's scale (URP builds its
    /// matrix from the radius alone), so a pool reaches twice as far in ground units up and down as sideways.
    /// </summary>
    public static class WorldLights
    {
        const string LightName = "Light";

        /// <summary>The scene's mood, or none for a scene that stays fully lit.</summary>
        public static LightingRules.Mood? MoodOf(Scene scene)
        {
            var level = Object.FindAnyObjectByType<DungeonLevel>();
            if (level != null)
                return LightingRules.DungeonAt(level.Depth);
            if (scene.name == SceneTravel.TownScene)
                return LightingRules.Town;
            return null;
        }

        /// <summary>A point light on an object, once (a second call returns the first). Null in a fully lit scene.</summary>
        public static Light2D Add(Transform owner, Color color, float intensity, float innerRadius, float outerRadius, float height = 0f)
        {
            if (owner == null || !MoodOf(owner.gameObject.scene).HasValue)
                return null;
            var existing = owner.Find(LightName);
            if (existing != null)
                return existing.GetComponent<Light2D>();

            var go = new GameObject(LightName);
            go.transform.SetParent(owner, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);

            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = innerRadius;
            light.pointLightOuterRadius = outerRadius;
            light.falloffIntensity = 0.6f;
            light.shadowsEnabled = false;
            return light;
        }

        /// <summary>
        /// Gives every ground tile its own shade (<see cref="LightingRules.FloorShade"/>), so the placeholder
        /// checkerboard stops reading as a grid. A tile asset locks its color, hence the flags.
        /// </summary>
        public static void ShadeGround(Tilemap ground)
        {
            if (ground == null)
                return;
            var bounds = ground.cellBounds;
            foreach (var cell in bounds.allPositionsWithin)
            {
                if (!ground.HasTile(cell))
                    continue;
                ground.SetTileFlags(cell, TileFlags.None);
                ground.SetColor(cell, LightingRules.GroundShade(new Vector2Int(cell.x, cell.y)));
            }
        }

        public static Light2D AddStairs(Transform stairs) => Add(stairs, new Color(1f, 0.8f, 0.55f), 0.9f, 0.3f, 3f);

        public static Light2D AddTravel(Transform point) => Add(point, new Color(0.45f, 0.7f, 1f), 1.5f, 0.5f, 3.5f);
    }

    /// <summary>The sprite materials code-built renderers need, from Resources so a build includes them.</summary>
    public static class SpriteMaterials
    {
        static Material unlit;
        static Material effect;

        /// <summary>Not touched by 2D lights: for telegraphs, loot beams and other effects that must read in the dark.</summary>
        public static Material Unlit => unlit != null ? unlit : unlit = Load("Materials/SpriteUnlit");

        /// <summary>The hit flash and dissolve shader (see <see cref="SpriteEffects"/>).</summary>
        public static Material Effect => effect != null ? effect : effect = Resources.Load<Material>("Materials/SpriteEffect");

        public static void MakeUnlit(SpriteRenderer renderer)
        {
            var material = Unlit;
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        static Material Load(string path)
        {
            var material = Resources.Load<Material>(path);
            if (material != null)
                return material;
            var pipeline = GraphicsSettings.currentRenderPipeline;
            return pipeline != null ? pipeline.default2DMaterial : null;
        }
    }

    /// <summary>
    /// A hit flash and a death dissolve on a character's sprites. While either shows, the sprites draw with the effect
    /// material and a property block; otherwise they are back on their own lit material. Ticked by its owner; allocates
    /// nothing per frame.
    /// </summary>
    public sealed class SpriteEffects
    {
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly int ShadeId = Shader.PropertyToID("_Shade");

        readonly SpriteRenderer[] renderers;
        readonly Material[] originals;
        readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        float flashRemaining;
        float flashDuration;
        Color flashColor = Color.white;
        float dissolve;
        bool showing;

        public SpriteEffects(SpriteRenderer[] renderers)
        {
            this.renderers = renderers;
            originals = new Material[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
                originals[i] = renderers[i].sharedMaterial;
        }

        public void Flash(Color color, float seconds)
        {
            flashColor = color;
            flashDuration = seconds;
            flashRemaining = seconds;
            Apply();
        }

        /// <summary>0 whole, 1 gone.</summary>
        public void SetDissolve(float amount)
        {
            dissolve = Mathf.Clamp01(amount);
            Apply();
        }

        public void Tick(float deltaTime)
        {
            if (flashRemaining <= 0f)
                return;
            flashRemaining = Mathf.Max(0f, flashRemaining - deltaTime);
            Apply();
        }

        public void Clear()
        {
            flashRemaining = 0f;
            dissolve = 0f;
            Apply();
        }

        void Apply()
        {
            var flash = LightingRules.FlashAmount(flashRemaining, flashDuration);
            var material = SpriteMaterials.Effect;
            var show = material != null && (flash > 0f || dissolve > 0f);
            if (!show && !showing)
                return;
            showing = show;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!show)
                {
                    renderer.sharedMaterial = originals[i];
                    renderer.SetPropertyBlock(null);
                    continue;
                }
                renderer.sharedMaterial = material;
                block.Clear();
                block.SetFloat(FlashId, flash);
                block.SetColor(FlashColorId, flashColor);
                block.SetFloat(DissolveId, dissolve);
                block.SetFloat(ShadeId, LightingDirector.CharacterShade);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
