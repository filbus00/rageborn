using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Runs the onboarding from Docs/06-ui-ux.md for the M1 slice (<see cref="Onboarding"/> holds the rules and what has
    /// been taught): the ghost thumb on a new character's first arrival in town, the Forge hint on the first Rare picked
    /// up, and on the first Legendary a hint to compare and equip it, with the Bag button pulsing until the bag is
    /// opened. The guaranteed Legendary itself drops in <see cref="LootDirector"/>. It also counts play time. Created
    /// from code and kept across scenes, like the XP and potion directors, so no scene needs rebuilding.
    /// Not built from the docs' onboarding: the scripted town scene and first level, and the loot filter introduction.
    /// </summary>
    public class OnboardingDirector : MonoBehaviour
    {
        const string TownScene = "Town";
        const string BagButtonName = "Bag Button";
        const float PulseSpeed = 6f;
        const float PulseScale = 0.12f;

        const string ForgeHint = "A Rare! The smith in town runs the <color=#FF8030>Forge</color>: salvage spare gear there, and reforge Rares.";
        const string LegendaryHint = "A <color=#E07A20>Legendary</color>! Tap Bag, tap it to compare with what you wear, then Equip.";

        HintBanner banner;
        GameSession session;
        Transform bagButton;
        bool pulseBag;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Onboarding Director");
            DontDestroyOnLoad(go);
            go.AddComponent<OnboardingDirector>();
        }

        void Awake() => banner = HintBanner.Create();

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (session != null)
                session.PickedUp -= OnPickedUp;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // A loaded save replaces the session at startup, so it is read here, not kept from before.
            if (session != GameSession.Current)
            {
                if (session != null)
                    session.PickedUp -= OnPickedUp;
                session = GameSession.Current;
                session.PickedUp += OnPickedUp;
            }

            StopPulse();
            var bag = GameObject.Find(BagButtonName);
            bagButton = bag != null ? bag.transform : null;

            if (scene.name == TownScene && !session.Onboarding.StickTaught)
            {
                var stick = FindAnyObjectByType<FloatingStickInput>();
                GhostThumb.Create(stick).Finished += session.Onboarding.MarkStickTaught;
            }
        }

        void OnPickedUp(Item item)
        {
            switch (session.Onboarding.OnItemFound(item))
            {
                case OnboardingHint.Forge:
                    if (Features.Forge)
                        banner.Show(ForgeHint);
                    break;
                case OnboardingHint.Legendary:
                    banner.Show(LegendaryHint);
                    pulseBag = bagButton != null;
                    break;
            }
        }

        void Update()
        {
            // Scaled time: the inventory pause does not count as play.
            if (session != null)
                session.Onboarding.Tick(Time.deltaTime);

            if (!pulseBag)
                return;
            if (bagButton == null || (InventoryScreen.Current != null && InventoryScreen.Current.IsOpen))
            {
                StopPulse();
                return;
            }
            var scale = 1f + PulseScale * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed));
            bagButton.localScale = new Vector3(scale, scale, 1f);
        }

        void StopPulse()
        {
            pulseBag = false;
            if (bagButton != null)
                bagButton.localScale = Vector3.one;
        }
    }
}
