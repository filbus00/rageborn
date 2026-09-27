using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ARPG
{
    /// <summary>
    /// The game world is drawn at a low resolution and enlarged with hard pixels, as Diablo 2 looked at 800 by 600 (the
    /// owner, 2026-09-28: "Do it, diablo 2 style on all"). The world renders at a whole fraction of the screen, chosen so
    /// the long side is about <see cref="TargetLongSide"/> pixels: on a 3x iPhone that is its point resolution (402 by 874
    /// on an iPhone 17), on a 2x one half its pixels. Screen space overlay canvases (the HUD, the bag, the stick, damage
    /// numbers) are drawn after the upscale at full resolution, so text and buttons stay sharp.
    /// </summary>
    public static class RenderResolution
    {
        /// <summary>About Diablo 2's 800 pixels on the long side, turned to portrait.</summary>
        public const int TargetLongSide = 870;

        /// <summary>
        /// How many screen pixels each rendered pixel covers along a side: the whole number that brings the long side
        /// closest to <see cref="TargetLongSide"/>, at least 1. Whole numbers keep every enlarged pixel the same size.
        /// </summary>
        public static int Factor(int screenLongSide) => Mathf.Max(1, Mathf.RoundToInt(screenLongSide / (float)TargetLongSide));

        static float originalScale = 1f;
        static UpscalingFilterSelection originalFilter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline))
                return;
            originalScale = pipeline.renderScale;
            originalFilter = pipeline.upscalingFilter;
            var factor = Factor(Mathf.Max(Screen.width, Screen.height));
            pipeline.renderScale = 1f / factor;
            pipeline.upscalingFilter = UpscalingFilterSelection.Point;
#if UNITY_EDITOR
            // The pipeline is an asset: put it back when play mode ends, so the editor's views and the saved asset keep
            // their own settings.
            Application.quitting += () =>
            {
                pipeline.renderScale = originalScale;
                pipeline.upscalingFilter = originalFilter;
            };
#endif
            Debug.Log($"RenderResolution: world at 1/{factor} of {Screen.width} x {Screen.height}");
        }
    }
}
