using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// The grim colour grade (the owner, 2026-10-18: "Implement the 1.5 pixelation and grim grade", after the look tests
    /// in Logs/LookTests): less colour, cold shadows and warm light, a little more contrast and a dark vignette, as the
    /// mock-up had it. URP's post-processing on each scene's main camera, from one global volume made in code on load;
    /// the overlay canvases (the HUD, the arcs, the menus) are drawn after it and keep their colours. Numbers in
    /// <see cref="GradeRules"/>.
    /// </summary>
    public static class GrimGrade
    {
        static VolumeProfile profile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => profile = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var camera = Camera.main;
            if (camera == null)
                return;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            foreach (var existing in Object.FindObjectsByType<Volume>())
                if (existing.sharedProfile == Profile)
                    return;
            var go = new GameObject("Grim Grade", typeof(Volume));
            SceneManager.MoveGameObjectToScene(go, scene);
            var volume = go.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = Profile;
        }

        static VolumeProfile Profile
        {
            get
            {
                if (profile != null)
                    return profile;
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Grim Grade";

                var colour = profile.Add<ColorAdjustments>(true);
                colour.saturation.Override(GradeRules.Saturation);
                colour.contrast.Override(GradeRules.Contrast);
                colour.postExposure.Override(GradeRules.Exposure);

                var split = profile.Add<SplitToning>(true);
                split.shadows.Override(GradeRules.ShadowTint);
                split.highlights.Override(GradeRules.HighlightTint);
                split.balance.Override(GradeRules.Balance);

                var vignette = profile.Add<Vignette>(true);
                vignette.color.Override(Color.black);
                vignette.intensity.Override(GradeRules.VignetteIntensity);
                vignette.smoothness.Override(GradeRules.VignetteSmoothness);
                vignette.rounded.Override(false);
                return profile;
            }
        }
    }

    /// <summary>The grim grade's numbers (URP's units). Claude's, after the mock-up, to review on the phone.</summary>
    public static class GradeRules
    {
        /// <summary>-100 is grey, 0 untouched: about half the colour, as the mock-up's 55 percent.</summary>
        public const float Saturation = -40f;

        /// <summary>-100 to 100: the mock-up's 1.25 times contrast.</summary>
        public const float Contrast = 18f;

        /// <summary>In stops: a touch brighter, since the contrast and the vignette darken a dark game.</summary>
        public const float Exposure = 0.15f;

        public static readonly Color ShadowTint = new Color(0.42f, 0.5f, 0.62f);
        public static readonly Color HighlightTint = new Color(0.78f, 0.62f, 0.42f);

        /// <summary>-100 to 100: which of the two tints reaches further; even.</summary>
        public const float Balance = 0f;

        public const float VignetteIntensity = 0.32f;
        public const float VignetteSmoothness = 0.45f;
    }
}
