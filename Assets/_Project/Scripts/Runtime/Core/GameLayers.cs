using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Physics layer and sorting layer names. Must match what ProjectSetup adds to TagManager.
    /// </summary>
    public static class GameLayers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Obstacle = "Obstacle";
        public const string Projectile = "Projectile";
        public const string Interactable = "Interactable";
        public const string Loot = "Loot";

        public static readonly string[] PhysicsLayers =
        {
            Player, Enemy, Obstacle, Projectile, Interactable, Loot,
        };

        public static int PlayerMask => LayerMask.GetMask(Player);
        public static int EnemyMask => LayerMask.GetMask(Enemy);
        public static int ObstacleMask => LayerMask.GetMask(Obstacle);
    }

    /// <summary>
    /// Sorting layers, back to front. Sprites on "Entities" sort against each other by Y
    /// (custom transparency sort axis 0,1,0), so sprite pivots must sit at the feet.
    /// </summary>
    public static class GameSortingLayers
    {
        public const string Ground = "Ground";
        public const string Decals = "Decals";
        public const string Entities = "Entities";
        public const string Effects = "Effects";
        public const string WorldUI = "WorldUI";

        public static readonly string[] All = { Ground, Decals, Entities, Effects, WorldUI };
    }
}
