using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The people and things of the mercenary camp (<see cref="CampLayout"/>; the owner, 2026-11-18): walked onto like
    /// every town trigger, each opens the company's menu at its page. Brunhilde at the drill yard (the Yard), Thorin at
    /// the forge (the Armoury), Lyriel at the kennel (the Kennel), the billboard (the contracts) and the grave stone (the
    /// Fallen). The keepers are the camp's 16x16 figures (`camp_art.py`).
    /// </summary>
    public class CampSpot : WalkOnTrigger
    {
        public enum Kind
        {
            Billboard,
            Yard,
            Armoury,
            Kennel,
            Fallen,
        }

        Kind kind;

        /// <summary>Creates every spot of the camp.</summary>
        public static void CreateAll()
        {
            Create(Kind.Billboard, CampLayout.Billboard, "Contracts", new Color(0.95f, 0.85f, 0.5f), null);
            Create(Kind.Yard, CampLayout.Yard, "Brunhilde", new Color(0.8f, 0.9f, 0.75f), "camp_brunhilde");
            Create(Kind.Armoury, CampLayout.Armoury, "Thorin", new Color(0.95f, 0.7f, 0.45f), "camp_thorin");
            Create(Kind.Kennel, CampLayout.Kennel, "Lyriel", new Color(0.8f, 0.95f, 0.7f), "camp_lyriel");
            Create(Kind.Fallen, CampLayout.GraveStone, "The Fallen", new Color(0.75f, 0.75f, 0.85f), null);

            // The company's archers at the range, each in her own kit.
            var looks = new[]
            {
                ("ranger", "hood", "hunting_bow"), ("brigand", "", "recurve_bow"), ("stag_hide", "falconer", "horn_bow"),
                ("scale", "mask", "war_bow"), ("ranger", "", "yew_longbow"),
            };
            for (var i = 0; i < CampLayout.Archers.Length; i++)
            {
                var look = looks[i % looks.Length];
                CampArcher.Create(CampLayout.Archers[i], look.Item1, look.Item2, look.Item3, 17 + i * 31);
            }
        }

        static void Create(Kind kind, Vector2Int cell, string label, Color colour, string body)
        {
            var go = new GameObject(label, typeof(CircleCollider2D));
            go.transform.position = IsoMath.GroundToWorld(IsoMath.CellToGround(cell));
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = kind == Kind.Billboard ? 1.3f : 0.9f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;
            // A person is a drawn figure (camp_art.py) standing on the spot; the billboard and the grave stone are pieces of
            // the camp's layout, with only a name over them. Without the art, the placeholder figure.
            if (body != null && WorldArt.Place(body, go.transform, go.transform.position) == null)
                TravelArt.Figure(go.transform, colour);
            TravelArt.Label(go.transform, label, colour, body != null ? 1.7f : 1.8f);
            go.AddComponent<CampSpot>().kind = kind;
        }

        protected override void OnWalkedOn()
        {
            if ((InventoryScreen.Current != null && InventoryScreen.Current.IsOpen) || DialoguePanel.IsOpen || CompanyMenu.IsShown)
                return;
            switch (kind)
            {
                case Kind.Billboard:
                    CompanyMenu.OpenContracts();
                    break;
                case Kind.Fallen:
                    CompanyMenu.OpenFallen();
                    break;
                default:
                    CompanyMenu.OpenQuarters((int)kind - (int)Kind.Yard);
                    break;
            }
        }
    }
}
