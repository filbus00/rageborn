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

        /// <summary>
        /// Heavier pixels (the owner, 2026-10-18: "Implement the 1.5 pixelation"): the world is drawn at a resolution this
        /// many times lower on each side with the same view, so each rendered pixel is half again as big (3 x 3 screen
        /// pixels on an iPhone 11, was 2 x 2). The art is drawn at fewer pixels a unit to keep the view's size.
        /// </summary>
        public const float Pixelation = 1.5f;

        /// <summary>The screen pixels a rendered pixel covers with the heavier pixels: <see cref="Pixelation"/> times
        /// <see cref="Factor"/>, rounded up to a whole number (3 on a 2x phone, 5 on a 3x one).</summary>
        public static int PixelatedFactor(int screenLongSide) => Mathf.CeilToInt(Factor(screenLongSide) * Pixelation - 0.01f);

        /// <summary>The art's pixels a unit at the heavier pixels, so the view stays as wide as at <see cref="Factor"/>
        /// with <see cref="PixelArt.PixelsPerUnit"/> (27 on an iPhone 11: 40 times 2 over 3).</summary>
        public static int PixelatedUnit(int screenLongSide) =>
            Mathf.Max(1, Mathf.RoundToInt(PixelArt.PixelsPerUnit * Factor(screenLongSide) / (float)PixelatedFactor(screenLongSide)));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>The camera stands this much further back (2026-10-25, the owner: "move the game camera back 20%"):
        /// the view shows 20 percent more each way.</summary>
        public const float ViewScale = 1.2f;

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var camera = Camera.main;
            if (camera == null || !camera.orthographic || camera.GetComponent<PixelPerfectCamera>() != null)
                return;
            var longSide = Mathf.Max(Screen.width, Screen.height);
            if (!Features.PixelFilter)
            {
                // The look reset (Features.PixelFilter, 2026-10-20): the screen's own resolution, the view the filter
                // gave (an art pixel Factor screen pixels).
                // The long side: the game is portrait only, and the editor sometimes reports a landscape screen at load.
                camera.orthographicSize = longSide / (2f * PixelArt.PixelsPerUnit * Factor(longSide)) * ViewScale;
                return;
            }
            var factor = PixelatedFactor(longSide);
            // The component renders into a texture of the screen over the factor (rounded down to even) and sets the
            // camera's orthographic size from its height and the pixels per unit.
            var height = Screen.height / factor / 2 * 2;
            var pixelPerfect = camera.gameObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.refResolutionX = Screen.width / factor;
            pixelPerfect.refResolutionY = Screen.height / factor;
            // Every world sprite is drawn at PixelArt.PixelsPerUnit, so the world renders at that too and one art pixel is
            // one rendered pixel; the view's height follows the screen (14.9 units on an iPhone 11, the scenes' 15 give
            // or take, 16.4 on an iPhone 17). It used to follow the camera's size instead, which the art could not match.
            // Since 2026-10-18 at the heavier pixels: fewer art pixels a unit (PixelatedUnit), the same view.
            pixelPerfect.assetsPPU = PixelatedUnit(longSide);
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            Debug.Log($"RenderResolution: {scene.name} world at {Screen.width / factor} x {height}, " +
                      $"{pixelPerfect.assetsPPU} px a unit, each shown as {factor} x {factor}");
        }
    }
}
