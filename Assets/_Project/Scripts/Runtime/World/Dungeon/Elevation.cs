using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// How high the floor stands under a point of a level (the owner, 2026-10-20: "The height differences do not read as
    /// hight differences. The steps are just props that the player walks through instead of stepping down on"): a dais,
    /// a balcony or a cave terrace one level up, a burial pit one level down, and on a flight of steps a height that
    /// runs smoothly from the top step to the bottom one. Gameplay stays flat on the ground plane; only what is drawn is
    /// lifted: the raised and sunken floors (their own tilemaps, <see cref="DungeonLevel"/>), the characters standing on
    /// them and the loot lying there. Pure.
    /// </summary>
    public sealed class ElevationMap
    {
        /// <summary>One level, in world units up the screen, rounded to whole pixels at 40 a unit: the crypt's daises and
        /// pits are 0.7 high in the models (2026-10-20, the owner: 0.45 did "not look elevated"), seen from 30 degrees
        /// (0.7 x 0.866); the caves' terraces 0.45.</summary>
        public const float CryptLevelHeight = 0.6f, CaveLevelHeight = 0.4f;

        /// <summary>This level's height, in world units up the screen.</summary>
        public float LevelHeight { get; }

        readonly HashSet<Vector2Int> raised;
        readonly HashSet<Vector2Int> sunken;
        readonly Dictionary<Vector2Int, Vector2Int> stairs;
        readonly System.Func<Vector2Int, int> tier;

        public ElevationMap(HashSet<Vector2Int> raised, HashSet<Vector2Int> sunken, Dictionary<Vector2Int, Vector2Int> stairs,
            float levelHeight = CryptLevelHeight, System.Func<Vector2Int, int> tier = null)
        {
            LevelHeight = levelHeight;
            this.tier = tier;
            this.raised = raised;
            this.sunken = sunken;
            this.stairs = stairs;
        }

        public static ElevationMap From(DungeonLayout layout) => new ElevationMap(layout.Raised, layout.Sunken, layout.TerraceStairs,
            layout.Theme == DungeonTheme.Cave ? CaveLevelHeight : CryptLevelHeight, layout.Tiered ? layout.TierLevel : null);

        /// <summary>The level the cell's floor stands at: its hall's in a climbing level (2026-10-21), one more on a
        /// dais, one less in a pit.</summary>
        public int LevelOf(Vector2Int cell) =>
            (tier != null ? tier(cell) : 0) + (raised.Contains(cell) ? 1 : sunken.Contains(cell) ? -1 : 0);

        /// <summary>The level at a ground point, in levels: on steps, from the top step's level at their upper edge to
        /// the bottom one's at their lower edge.</summary>
        public float LevelAt(Vector2 ground)
        {
            var cell = IsoMath.GroundToCell(ground);
            if (stairs.TryGetValue(cell, out var down))
            {
                StepLevels(cell, down, out var high, out var low);
                var center = IsoMath.CellToGround(cell);
                var along = IsoMath.CellToGround(cell + down) - center;
                var t = Mathf.Clamp01(Vector2.Dot(ground - center, along) / along.sqrMagnitude + 0.5f);
                return Mathf.Lerp(high, low, t);
            }
            return LevelOf(cell);
        }

        /// <summary>The levels at the top and the foot of a flight: a dais's from it down to the floor, a pit's from
        /// the floor down into it.</summary>
        public void StepLevels(Vector2Int cell, Vector2Int down, out int high, out int low)
        {
            high = LevelOf(cell - down);
            low = LevelOf(cell + down);
            if (high == low)
                high = low + 1;
        }

        /// <summary>The lift up the screen at a ground point, in world units.</summary>
        public float HeightAt(Vector2 ground) => LevelAt(ground) * LevelHeight;
    }

    /// <summary>The current level's <see cref="ElevationMap"/> (none in town: everything stands at 0).</summary>
    public static class Elevation
    {
        public static ElevationMap Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        /// <summary>The lift up the screen at a ground point, 0 off any level.</summary>
        public static float HeightAt(Vector2 ground) => Current != null ? Current.HeightAt(ground) : 0f;

        /// <summary>The same as a Vector2 (where effects and shots are drawn).</summary>
        public static Vector2 World2(Vector2 ground)
        {
            var world = IsoMath.GroundToWorld(ground);
            return new Vector2(world.x, world.y + HeightAt(ground));
        }

        /// <summary>Where her picture stands: her body's place lifted by the floor under it.</summary>
        public static Vector3 Lifted(Vector3 world) => new Vector3(world.x, world.y + HeightAt(IsoMath.WorldToGround(world)), world.z);

        /// <summary>Where a thing standing at a ground point is drawn: on the ground, lifted by the floor's height.</summary>
        public static Vector3 World(Vector2 ground)
        {
            var world = IsoMath.GroundToWorld(ground);
            return new Vector3(world.x, world.y + HeightAt(ground), 0f);
        }
    }
}
