using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// The game world is drawn at a low resolution and enlarged in hard, whole pixels, as Diablo 2 looked at 800 by 600
    /// (the owner, 2026-09-28: "Do it, diablo 2 style on all"). The world renders at a whole fraction of the screen, chosen
    /// so the long side is about <see cref="TargetLongSide"/> pixels: on a 3x iPhone that is its point resolution (402 by
    /// 874 on an iPhone 17), on a 2x one half its pixels. Screen space overlay canvases (the HUD, the bag, the stick,
    /// damage numbers) are drawn after the enlargement at full resolution, so text and buttons stay sharp.
    ///
    /// It is done with URP's Pixel Perfect Camera in its upscale render texture mode, added to each scene's main camera
    /// on load. The pipeline's own render scale did render the world at a third, but the 2D renderer enlarges it with
    /// bilinear filtering unless a Pixel Perfect Camera is present (Renderer2DRendergraph picks the target's filter mode),
    /// so the first try came out soft rather than pixelated: found by reading the simulator's pixels, where every edge
    /// ramped over 3 pixels.
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

        /// <summary>
        /// The pixels per unit that give a rendered height of <paramref name="renderedHeight"/> pixels the scene camera's
        /// view (twice its orthographic size), so the framing is the scene's own, give or take the rounding (7.5 comes out
        /// 7.53 at 874 pixels).
        /// </summary>
        public static int PixelsPerUnit(int renderedHeight, float orthographicSize) =>
            Mathf.Max(1, Mathf.RoundToInt(renderedHeight / (2f * orthographicSize)));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var camera = Camera.main;
            if (camera == null || !camera.orthographic || camera.GetComponent<PixelPerfectCamera>() != null)
                return;
            var factor = Factor(Mathf.Max(Screen.width, Screen.height));
            // The component renders into a texture of the screen over the factor (rounded down to even) and sets the
            // camera's orthographic size from its height and the pixels per unit.
            var height = Screen.height / factor / 2 * 2;
            var pixelPerfect = camera.gameObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.refResolutionX = Screen.width / factor;
            pixelPerfect.refResolutionY = Screen.height / factor;
            pixelPerfect.assetsPPU = PixelsPerUnit(height, camera.orthographicSize);
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            Debug.Log($"RenderResolution: {scene.name} world at {Screen.width / factor} x {height}, " +
                      $"{pixelPerfect.assetsPPU} px a unit, each shown as {factor} x {factor}");
        }
    }
}
