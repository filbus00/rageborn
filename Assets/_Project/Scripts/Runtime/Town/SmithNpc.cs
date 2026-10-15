using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Thorin the smith before his smithy (the roguelite, 2026-10-15): walking up opens his forge
    /// (<see cref="SmithScreen"/>), where the blueprints brought home are forged into kit pieces.
    /// </summary>
    public class SmithNpc : WalkOnTrigger
    {
        public const string Name = "Thorin";

        /// <summary>Where he stands: by the smithy (TownLayout's smithy at -15, 8), clear of it (checked by a test).</summary>
        public static readonly Vector2Int Cell = new Vector2Int(-11, 6);

        public static SmithNpc Create(Transform parent)
        {
            var go = new GameObject(Name, typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(IsoMath.CellToGround(Cell));
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.9f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;
            NpcFigure.Create(go.transform, QuestNpc.Character(QuestPerson.Pellam), new Color(0.6f, 0.45f, 0.35f));
            TravelArt.Label(go.transform, Name, LootColors.Gold, 1.5f);
            return go.AddComponent<SmithNpc>();
        }

        protected override void OnWalkedOn()
        {
            var bag = InventoryScreen.Current;
            if ((bag != null && bag.IsOpen) || PetVendorScreen.IsOpen || TownSheets.AnyOpen || DialoguePanel.IsOpen)
                return;
            SmithScreen.Open();
        }
    }
}
