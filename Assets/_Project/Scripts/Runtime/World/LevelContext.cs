using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Which level the player is in, for everything a level remembers (killed pack members, opened chests, corpses).
    /// A hand-built scene is its own level, named after the scene; the dungeon scene is every dungeon level, so
    /// <see cref="DungeonLevel"/> names the one it built ("Dungeon 3").
    /// </summary>
    public static class LevelContext
    {
        static string overrideId;

        public static string CurrentId => overrideId ?? SceneManager.GetActiveScene().name;

        /// <summary>Set by a scene that builds its level at load. Cleared by <see cref="Clear"/> when it unloads.</summary>
        public static void Set(string id) => overrideId = id;

        public static void Clear() => overrideId = null;
    }
}
