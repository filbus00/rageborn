using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A newcomer in town (2026-10-05: they arrive as the player goes deeper, <see cref="Newcomers"/>), walked up to like
    /// every NPC: the Stash Keeper opens the stash, the Healer restores life and potion charges at once, the Gambler and
    /// the Trainer open their sheets. Placed by <see cref="PetDirector"/> with the town's other NPCs made in code, only
    /// once arrived. Its body is its baked character (<see cref="NpcFigure"/>), or a placeholder until baked.
    /// </summary>
    public class NewcomerNpc : WalkOnTrigger
    {
        Newcomer who;

        /// <summary>The baked character each one uses (Resources/Characters/&lt;name&gt;).</summary>
        public static string Character(Newcomer who) => who switch
        {
            Newcomer.StashKeeper => "stash_keeper",
            Newcomer.Healer => "healer",
            Newcomer.Gambler => "gambler",
            _ => "trainer",
        };

        static Color Placeholder(Newcomer who) => who switch
        {
            Newcomer.StashKeeper => new Color(0.55f, 0.42f, 0.28f),
            Newcomer.Healer => new Color(0.75f, 0.75f, 0.65f),
            Newcomer.Gambler => new Color(0.45f, 0.3f, 0.55f),
            _ => new Color(0.4f, 0.45f, 0.6f),
        };

        public static NewcomerNpc Create(Newcomer who, Transform parent)
        {
            var go = new GameObject(Newcomers.Name(who), typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(IsoMath.CellToGround(Newcomers.Cell(who)));
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.9f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;

            NpcFigure.Create(go.transform, Character(who), Placeholder(who));
            TravelArt.Label(go.transform, Newcomers.Name(who), LootColors.Gold, 1.5f);
            var npc = go.AddComponent<NewcomerNpc>();
            npc.who = who;
            return npc;
        }

        protected override void OnWalkedOn()
        {
            var bag = InventoryScreen.Current;
            if ((bag != null && bag.IsOpen) || PetVendorScreen.IsOpen || TownSheets.AnyOpen)
                return;
            switch (who)
            {
                case Newcomer.StashKeeper:
                    StashScreen.Open();
                    break;
                case Newcomer.Healer:
                    Heal();
                    break;
                case Newcomer.Gambler:
                    GamblerScreen.Open();
                    break;
                default:
                    TrainerScreen.Open();
                    break;
            }
        }

        // Docs/05 (the owner's pick of 2026-10-05): walking up restores life and refills the potion's charges, free.
        static void Heal()
        {
            var health = FindAnyObjectByType<PlayerHealth>();
            var session = GameSession.Current;
            var wasHurt = health != null && health.Fraction < 0.999f;
            var potionShort = session.Potion.Charges < AutoPotion.MaxCharges;
            if (health != null)
                health.Heal(health.MaxLife);
            session.Potion.Refill();
            if (wasHurt || potionShort)
            {
                Sfx.Play(SoundId.Potion, 0.8f);
                HintBanner.Current?.Show("The healer tends your wounds and fills your potion.");
            }
        }
    }
}
