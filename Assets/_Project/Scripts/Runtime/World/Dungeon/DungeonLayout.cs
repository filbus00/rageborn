using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    public enum DungeonCell : byte
    {
        Void,
        Floor,
        Wall,
    }

    /// <summary>Room types from Docs/05-world-and-content.md. Shrine and ambush rooms are not built; until they are,
    /// their share of the roll goes to combat rooms.</summary>
    public enum RoomKind
    {
        Start,
        Exit,
        Combat,
        Elite,
        Treasure,
    }

    public enum PackKind
    {
        Normal,

        /// <summary>A normal pack whose slot 0 is a Champion leader.</summary>
        WithChampion,

        /// <summary>Every member is an Elite.</summary>
        Elite,
    }

    public readonly struct RoomPlacement
    {
        public RoomPlacement(RoomKind kind, RoomShape shape, Vector2Int macro, RectInt interior)
        {
            Kind = kind;
            Shape = shape;
            Macro = macro;
            Interior = interior;
        }

        public RoomKind Kind { get; }
        public RoomShape Shape { get; }

        /// <summary>The room's slot on the coarse layout grid.</summary>
        public Vector2Int Macro { get; }

        /// <summary>The room's inside, in level cells.</summary>
        public RectInt Interior { get; }
    }

    public readonly struct PackPlacement
    {
        public PackPlacement(Vector2Int cell, int count, float radius, PackKind kind, int room)
        {
            Cell = cell;
            Count = count;
            Radius = radius;
            Kind = kind;
            Room = room;
        }

        public Vector2Int Cell { get; }
        public int Count { get; }

        /// <summary>In ground units, as <see cref="EnemyPack"/> uses it.</summary>
        public float Radius { get; }

        public PackKind Kind { get; }

        /// <summary>Index into <see cref="DungeonLayout.Rooms"/>.</summary>
        public int Room { get; }
    }

    /// <summary>
    /// One generated dungeon level: which cells are floor, wall or nothing, the rooms, where the packs, chests and
    /// stairs go, and where the player arrives. Built by <see cref="DungeonGenerator"/>; pure data.
    /// </summary>
    public sealed class DungeonLayout
    {
        readonly DungeonCell[,] cells;

        internal DungeonLayout(RectInt bounds)
        {
            Bounds = bounds;
            cells = new DungeonCell[bounds.width, bounds.height];
        }

        /// <summary>The cells the level spans. Everything outside is <see cref="DungeonCell.Void"/>.</summary>
        public RectInt Bounds { get; }

        public int Depth { get; internal set; }
        public int EnemyLevel { get; internal set; }

        public List<RoomPlacement> Rooms { get; } = new List<RoomPlacement>();
        public List<PackPlacement> Packs { get; } = new List<PackPlacement>();
        public List<Vector2Int> Chests { get; } = new List<Vector2Int>();

        public Vector2Int StairsUp { get; internal set; }

        /// <summary>Where the player stands on coming down from the level above (or the town).</summary>
        public Vector2Int ArrivalFromAbove { get; internal set; }

        /// <summary>False on the last level of the act, which has no stairs down (its boss is not built yet).</summary>
        public bool HasStairsDown { get; internal set; }

        public Vector2Int StairsDown { get; internal set; }

        /// <summary>Where the player stands on coming up from the level below.</summary>
        public Vector2Int ArrivalFromBelow { get; internal set; }

        public DungeonCell Get(Vector2Int cell) => Get(cell.x, cell.y);

        public DungeonCell Get(int x, int y)
        {
            x -= Bounds.xMin;
            y -= Bounds.yMin;
            if (x < 0 || y < 0 || x >= Bounds.width || y >= Bounds.height)
                return DungeonCell.Void;
            return cells[x, y];
        }

        internal void Set(int x, int y, DungeonCell value) => cells[x - Bounds.xMin, y - Bounds.yMin] = value;

        public bool IsFloor(Vector2Int cell) => Get(cell) == DungeonCell.Floor;
    }
}
