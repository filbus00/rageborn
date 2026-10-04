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
        /// <summary>
        /// About 850 pixels on the long side: one zoom step out from the 600 of 2026-09-30 (the owner, 2026-10-04: enemies
        /// beside her within her bow's reach were shot off screen; "zoom one step"), so the view is half again as wide:
        /// 2 x 2 screen pixels a rendered pixel on the owner's iPhone 11 (414 x 896, 10.4 units wide), 3 x 3 on a 3x one
        /// (402 x 874 on an iPhone 17). She also targets only enemies on screen (PlayerCombat).
        /// </summary>
        public const int TargetLongSide = 850;

        /// <summary>
        /// How many screen pixels each rendered pixel covers along a side: the whole number that brings the long side
        /// closest to <see cref="TargetLongSide"/>, at least 1. Whole numbers keep every enlarged pixel the same size.
        /// </summary>
        public static int Factor(int screenLongSide) => Mathf.Max(1, Mathf.RoundToInt(screenLongSide / (float)TargetLongSide));

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
            // Every world sprite is drawn at PixelArt.PixelsPerUnit, so the world renders at that too and one art pixel is
            // one rendered pixel; the view's height follows the screen (14.9 units on an iPhone 11, the scenes' 15 give
            // or take, 16.4 on an iPhone 17). It used to follow the camera's size instead, which the art could not match.
            pixelPerfect.assetsPPU = PixelArt.PixelsPerUnit;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            Debug.Log($"RenderResolution: {scene.name} world at {Screen.width / factor} x {height}, " +
                      $"{pixelPerfect.assetsPPU} px a unit, each shown as {factor} x {factor}");
        }
    }
}
