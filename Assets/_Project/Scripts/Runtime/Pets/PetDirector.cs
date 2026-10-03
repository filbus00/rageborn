using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Pets across scenes (Docs/02, Pets): places the Pet Vendor (and the merchant) in town, and keeps the active pet at the character's
    /// side in every scene that has one, swapping it the moment a different pet is made active. Created from code before
    /// the first scene and kept across scenes, like the travel director, so no scene needs rebuilding.
    /// </summary>
    public class PetDirector : MonoBehaviour
    {
        const string TownScene = "Town";

        // Beside the Waystone (3, -3), away from the Forge (-4, 4), the portal back (-2, -4) and the DEV stairs (9, 1), (9, 5).
        static readonly Vector2Int VendorCell = new Vector2Int(5, 1);

        // The merchant (2026-10-02) mirrors the vendor on the other side of the start.
        static readonly Vector2Int MerchantCell = new Vector2Int(-4, 1);

        GameSession session;
        PetController pet;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Pet Director");
            DontDestroyOnLoad(go);
            go.AddComponent<PetDirector>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (session != null)
                session.Changed -= Refresh;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (session != GameSession.Current)
            {
                if (session != null)
                    session.Changed -= Refresh;
                session = GameSession.Current;
                session.Changed += Refresh;
            }

            // The old scene's pet went with it.
            pet = null;
            if (scene.name == TownScene)
            {
                PetVendor.Create(IsoMath.CellToGround(VendorCell), null);
                Merchant.Create(IsoMath.CellToGround(MerchantCell), null);
            }
            Refresh();
        }

        // The active pet follows; a different one replaces it at once.
        void Refresh()
        {
            if (session != GameSession.Current)
                return;
            var active = session.Pets.Active;
            if (pet != null && (active == null || pet.Kind != active.Value))
            {
                Destroy(pet.gameObject);
                pet = null;
            }
            if (pet != null || active == null)
                return;
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                pet = PetController.Spawn(active.Value, player);
        }
    }

    /// <summary>
    /// The Pet Vendor in town (Docs/05, Towns; decided 2026-09-30): walked up to like every NPC, it opens the
    /// <see cref="PetVendorScreen"/>. Placeholder figure until NPC art exists.
    /// </summary>
    public class PetVendor : WalkOnTrigger
    {
        public static PetVendor Create(Vector2 ground, Transform parent)
        {
            var go = new GameObject("Pet Vendor", typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(ground);
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.9f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;

            NpcFigure.Create(go.transform, "pet_vendor", new Color(0.45f, 0.55f, 0.35f));
            TravelArt.Label(go.transform, "Pet Vendor", new Color(0.8f, 0.95f, 0.7f), 1.5f);
            return go.AddComponent<PetVendor>();
        }

        protected override void OnWalkedOn()
        {
            if (InventoryScreen.Current != null && InventoryScreen.Current.IsOpen)
                return;
            PetVendorScreen.Open();
        }
    }
}
