namespace ARPG
{
    /// <summary>Which stairs the player comes in by, which decides where a dungeon level puts them.</summary>
    public enum Arrival
    {
        /// <summary>Down the stairs from the town or the level above: at the level's stairs up.</summary>
        FromAbove,

        /// <summary>Up the stairs from the level below: at the level's stairs down.</summary>
        FromBelow,
    }

    /// <summary>A trip between scenes: the dungeon depth to build and where to arrive. Set by the stairway that starts
    /// the trip and read by the scene it loads.</summary>
    public readonly struct LevelTravel
    {
        public LevelTravel(int depth, Arrival arrival)
        {
            Depth = depth;
            Arrival = arrival;
        }

        public int Depth { get; }
        public Arrival Arrival { get; }
    }
}
