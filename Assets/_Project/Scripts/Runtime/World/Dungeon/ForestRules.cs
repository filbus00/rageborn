using UnityEngine;

namespace ARPG
{
    /// <summary>What grows on a forest level's tree cell.</summary>
    public enum ForestGrowth
    {
        Tree,
        Bush,
        Bare,
    }

    /// <summary>
    /// How the forest's trees stand (2026-11-24, the owner: "review the way the trees are placed in the forest level. They
    /// seem to be too tightly packed"): a tree was drawn on every tree cell, a solid wall of crowns. Now the edge of a
    /// clearing (a cell touching the floor) has a tree on about six cells in ten and a bush in the gaps, so it still reads
    /// as closed; deeper in, about four in ten, bushes on two, the dark between; and no tree stands where the cells beside
    /// it on both sides are trees. Each tree and bush stands a little off its cell's middle, so the rows do not line up.
    /// The blockers are unchanged: where she can walk is the same. Pure.
    /// </summary>
    public static class ForestRules
    {
        public const float EdgeTrees = 0.62f;
        public const float DeepTrees = 0.42f;
        public const float DeepBushes = 0.2f;

        /// <summary>The cell's own 0 to 1 from its hash.</summary>
        public static float Roll(int hash, int salt = 0) => ((Mathf.Abs(hash * 31 + salt * 977) >> 3) % 1000) / 1000f;

        /// <summary>Tree, bush or bare by density alone (no neighbours), for the spacing check.</summary>
        public static ForestGrowth Raw(bool edge, int hash)
        {
            var v = Roll(hash);
            if (edge)
                return v < EdgeTrees ? ForestGrowth.Tree : ForestGrowth.Bush;
            return v < DeepTrees ? ForestGrowth.Tree : v < DeepTrees + DeepBushes ? ForestGrowth.Bush : ForestGrowth.Bare;
        }

        /// <summary>What grows here, given whether the cells to its -x and -y sides grow trees by density.</summary>
        public static ForestGrowth At(bool edge, int hash, bool treeWest, bool treeSouth)
        {
            var raw = Raw(edge, hash);
            if (raw == ForestGrowth.Tree && treeWest && treeSouth)
                return edge ? ForestGrowth.Bush : ForestGrowth.Bare;
            return raw;
        }

        /// <summary>How far off its cell's middle a tree or bush stands, in world units (a cell is 1 by 0.5).</summary>
        public static Vector2 Offset(int hash) => new Vector2((Roll(hash, 1) - 0.5f) * 0.4f, (Roll(hash, 2) - 0.5f) * 0.16f);
    }
}
