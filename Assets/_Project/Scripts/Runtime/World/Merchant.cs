using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The merchant in town (the owner, 2026-10-02: a merchant to sell unwanted gear for gold, which replaces Q26's "no
    /// vendor"). Walked up to like every NPC, it opens the Bag at the merchant (<see cref="InventoryScreen.OpenAtMerchant"/>):
    /// a backpack item's sheet offers Sell for its price (<see cref="SellRules"/>), and a row sells all of a rarity at
    /// once. It buys only; it sells nothing. Placed by <see cref="PetDirector"/> with the other town NPCs made in code.
    /// Its body is the baked merchant (NpcFigure), or a placeholder figure until it is baked.
    /// </summary>
    public class Merchant : WalkOnTrigger
    {
        public static Merchant Create(Vector2 ground, Transform parent)
        {
            var go = new GameObject("Merchant", typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(ground);
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.9f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;

            NpcFigure.Create(go.transform, "merchant", new Color(0.62f, 0.5f, 0.25f));
            TravelArt.Label(go.transform, "Merchant", LootColors.Gold, 1.5f);
            QuestMarker.Attach(go.transform, QuestGiver.Merchant);
            return go.AddComponent<Merchant>();
        }

        protected override void OnWalkedOn()
        {
            var bag = InventoryScreen.Current;
            if (bag == null || bag.IsOpen || PetVendorScreen.IsOpen || DialoguePanel.IsOpen)
                return;
            if (QuestTalk.TryTalk(QuestGiver.Merchant))
                return;
            bag.OpenAtMerchant();
        }
    }
}
