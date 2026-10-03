using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A town NPC's body (the owner, 2026-10-03: the Merchant and the Pet Vendor get models): the baked idle of
    /// Resources/Characters/&lt;character&gt; (<c>NpcBakeSetup</c>, 8 directions), looping, turned toward the character
    /// when she comes near. Without a bake it is the old placeholder figure (<see cref="TravelArt.Figure"/>).
    /// </summary>
    public class NpcFigure : MonoBehaviour
    {
        const float FramesPerSecond = 12f;
        // She is noticed within this many ground units; farther away the NPC faces the camera.
        const float NoticeRange = 6f;

        CharacterSheet idle;
        SpriteRenderer body;
        int row;
        float time;
        PlayerController player;

        /// <summary>Gives <paramref name="parent"/> its body: the baked character when it exists, else the placeholder.</summary>
        public static void Create(Transform parent, string character, Color placeholder)
        {
            var sheet = CharacterSheets.Load($"Characters/{character}/{character}_idle");
            if (sheet == null)
            {
                TravelArt.Figure(parent, placeholder);
                return;
            }
            var go = new GameObject("Body", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingLayerName = GameSortingLayers.Entities;
            // Lit like the other characters.
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.default2DMaterial != null)
                renderer.sharedMaterial = pipeline.default2DMaterial;
            var figure = go.AddComponent<NpcFigure>();
            figure.idle = sheet;
            figure.body = renderer;
            renderer.sprite = sheet.Rows[0][0];
        }

        void Update()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var toPlayer = IsoMath.WorldToGround(player.transform.position) - IsoMath.WorldToGround(transform.position);
                row = toPlayer.magnitude <= NoticeRange && toPlayer.sqrMagnitude > 1e-4f
                    ? AppearanceRules.DirectionRow(toPlayer.normalized, idle.Rows.Length)
                    : 0;
            }
            time += Time.deltaTime;
            body.sprite = idle.Rows[row][Mathf.FloorToInt(time * FramesPerSecond) % idle.Frames];
        }
    }
}
