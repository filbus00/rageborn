using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    public enum DungeonCell : byte
    {
        Void,
        Floor,
        Wall,

        /// <summary>Floor with a prop standing on it (a barrel, a brazier): drawn on the floor, blocks like a wall.</summary>
        Prop,
    }

    /// <summary>Things standing in a dungeon room (<see cref="DungeonDressing"/>). All block the way.</summary>
    public enum PropKind
    {
        Barrel,
        Crate,
        Urn,
        BonePile,
        Rubble,
        BrokenColumn,
        Sarcophagus,
        Brazier,
        Candles,
        Bucket,
        Torch,

        // Lairs (2026-10-08, the owner: "make it so that it looks like the monsters and demons are truly living in them").
        Campfire,
        Sack,
        WeaponRack,
        Carcass,
        Altar,
        Coffin,
        FleshPod,
        Spikes,
        Hellfire,
        Cage,
        Stake,
    }

    /// <summary>Marks on a dungeon floor, walked over.</summary>
    public enum DecalKind
    {
        Cracks,
        Bones,
        Blood,
        Rubble,
        Moss,
        Skull,
        Puddle,
        Ritual,

        // Lairs (2026-10-08).
        Bedroll,
        Straw,
        Gore,
        Sigil,
        Ash,
        Net,
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

        /// <summary>The act boss's circular arena, in place of the exit room on the act's last level.</summary>
        Boss,
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
        public RoomPlacement(RoomKind kind, RoomShape shape, Vector2Int macro, RectInt interior, int style = 0)
        {
            Kind = kind;
            Shape = shape;
            Macro = macro;
            Interior = interior;
            Style = style;
        }

        public RoomKind Kind { get; }

        /// <summary>The hand-authored pillar layout the room was dressed with, the arena's disc, or null.</summary>
        public RoomShape Shape { get; }

        /// <summary>The room's floor style, 0 to <see cref="DungeonGenerator.FloorStyles"/> - 1.</summary>
        public int Style { get; }

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

    /// <summary>How a level is built and drawn (the owner, 2026-10-10, from his concept art): a gothic crypt and
    /// cathedral under the town for the first half, natural caves below it.</summary>
    public enum DungeonTheme : byte
    {
        Crypt,
        Cave,
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

        /// <summary>Crypt or cave (<see cref="DungeonRules.ThemeAt"/>).</summary>
        public DungeonTheme Theme { get; internal set; }

        /// <summary>Caves (2026-10-10, the owner's terraces): the rim of a raised terrace, wall cells drawn as a rock
        /// ledge rather than a wall.</summary>
        public HashSet<Vector2Int> Ledges { get; } = new HashSet<Vector2Int>();

        /// <summary>Floor cells on a raised terrace, drawn lighter.</summary>
        public HashSet<Vector2Int> Raised { get; } = new HashSet<Vector2Int>();

        /// <summary>The steps cut through a ledge: floor cells, each with the way down (an edge step toward the lower
        /// floor), so the steps are drawn facing it.</summary>
        public Dictionary<Vector2Int, Vector2Int> TerraceStairs { get; } = new Dictionary<Vector2Int, Vector2Int>();

        public List<RoomPlacement> Rooms { get; } = new List<RoomPlacement>();
        public List<PackPlacement> Packs { get; } = new List<PackPlacement>();
        public List<Vector2Int> Chests { get; } = new List<Vector2Int>();
        public Dictionary<Vector2Int, PropKind> Props { get; } = new Dictionary<Vector2Int, PropKind>();
        public Dictionary<Vector2Int, DecalKind> Decals { get; } = new Dictionary<Vector2Int, DecalKind>();

        /// <summary>Wall standing free inside a room (pillars, heaps of fallen stone), not joined to the rock around the
        /// rooms: it keeps its full height on the camera side.</summary>
        public HashSet<Vector2Int> FreeStanding { get; } = new HashSet<Vector2Int>();

        /// <summary>Each pack's lair (2026-10-08): its middle and what kind of home it is.</summary>
        public List<(Vector2Int cell, LairTheme theme)> Lairs { get; } = new List<(Vector2Int, LairTheme)>();

        /// <summary>The middles of the summoning circles painted on some rooms' floors (3 x 3 cells, walked over).</summary>
        public List<Vector2Int> Rituals { get; } = new List<Vector2Int>();

        /// <summary>The floor style of the room a cell lies in (an opening takes one of its rooms'), 0 outside rooms.</summary>
        public int StyleAt(Vector2Int cell)
        {
            var best = 0;
            foreach (var room in Rooms)
            {
                var r = room.Interior;
                if (cell.x >= r.xMin - 1 && cell.x <= r.xMax && cell.y >= r.yMin - 1 && cell.y <= r.yMax)
                {
                    best = room.Style;
                    if (r.Contains(cell))
                        return best;
                }
            }
            return best;
        }

        public Vector2Int StairsUp { get; internal set; }

        /// <summary>Where the player stands on coming down from the level above (or the town).</summary>
        public Vector2Int ArrivalFromAbove { get; internal set; }

        /// <summary>The level's waypoint, in the start room (Docs/05).</summary>
        public Vector2Int Waypoint { get; internal set; }

        /// <summary>Where the Wanderer stands on the Portal Tome's depth: beside the first treasure chest.</summary>
        public Vector2Int WandererSpot { get; internal set; }
        public bool HasWandererSpot { get; internal set; }

        /// <summary>False on the last level of the act, which ends in the boss arena instead.</summary>
        public bool HasStairsDown { get; internal set; }

        public Vector2Int StairsDown { get; internal set; }

        /// <summary>True on the act's last level, where the exit room is the boss arena.</summary>
        public bool HasBossArena { get; internal set; }

        /// <summary>The arena's middle, where the boss waits.</summary>
        public Vector2Int BossArenaCenter { get; internal set; }

        /// <summary>The arena's floor radius in cells (0.707 ground units each).</summary>
        public int BossArenaRadius { get; internal set; }

        /// <summary>Three cells inside the arena's rim: where the development shortcut to the boss lands, close enough
        /// that the fight starts.</summary>
        public Vector2Int BossArenaEntry => BossArenaCenter - new Vector2Int(0, BossArenaRadius - 3);

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
