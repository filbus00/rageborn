using System.Text;

namespace ARPG
{
    /// <summary>What an imported dungeon art file is, read from its name.</summary>
    public enum ArtKind
    {
        Unknown,
        Floor,
        Decal,
        Wall,
        LowWall,
        Prop,
    }

    /// <summary>
    /// The file names of hand-made or generated dungeon art (Docs/09, 0.9; `Tools > ARPG > Import Pixel Art`):
    /// <c>floor_&lt;style&gt;_&lt;n&gt;</c> (styles flagstone, brick, earth, moss), <c>decal_&lt;kind&gt;_&lt;n&gt;</c>
    /// (cracks, bones, blood, rubble, moss, skull, puddle), <c>wall</c>, <c>wall_low</c> and <c>prop_&lt;kind&gt;</c>
    /// (barrel, crate, urn, bone_pile, rubble, broken_column, sarcophagus, brazier, candles). Lower case; the number is
    /// the variant, from 1. The importer and <see cref="DungeonArt"/> both read names through here. Pure.
    /// </summary>
    public static class ArtNames
    {
        /// <summary>The floor styles in <see cref="RoomPlacement.Style"/> order.</summary>
        public static readonly string[] FloorStyles = { "flagstone", "brick", "earth", "moss" };

        /// <summary>The most variants of one floor style or decal kind that are used.</summary>
        public const int MaxVariants = 16;

        /// <summary>Every frame's width in art pixels: one cell, <see cref="PixelArt.PixelsPerUnit"/>.</summary>
        public const int Width = PixelArt.PixelsPerUnit;

        /// <summary>A floor tile's height: the 2:1 diamond.</summary>
        public const int FloorHeight = PixelArt.PixelsPerUnit / 2;

        /// <summary>
        /// Where every frame stands, in art pixels up from its bottom edge: the middle of the cell's diamond, a quarter
        /// unit up (a floor tile's middle; a wall's or prop's footprint sits in its bottom 20 pixels).
        /// </summary>
        public const int PivotY = PixelArt.PixelsPerUnit / 4;

        /// <summary>
        /// Reads a file name (no folder or extension). <paramref name="index"/> is the floor style, decal kind or prop
        /// kind; <paramref name="variant"/> counts from 0 (the name's number minus 1), 0 for walls and props.
        /// </summary>
        public static ArtKind Parse(string name, out int index, out int variant)
        {
            index = 0;
            variant = 0;
            if (string.IsNullOrEmpty(name))
                return ArtKind.Unknown;
            name = name.ToLowerInvariant();
            if (name == "wall")
                return ArtKind.Wall;
            if (name == "wall_low")
                return ArtKind.LowWall;
            if (name.StartsWith("prop_"))
            {
                var kind = name.Substring(5);
                for (var i = 0; i < PropCount; i++)
                    if (Snake(((PropKind)i).ToString()) == kind)
                    {
                        index = i;
                        return ArtKind.Prop;
                    }
                return ArtKind.Unknown;
            }

            var isFloor = name.StartsWith("floor_");
            if (!isFloor && !name.StartsWith("decal_"))
                return ArtKind.Unknown;
            var rest = name.Substring(6);
            var underscore = rest.LastIndexOf('_');
            if (underscore <= 0 || !int.TryParse(rest.Substring(underscore + 1), out var number) || number < 1 || number > MaxVariants)
                return ArtKind.Unknown;
            var what = rest.Substring(0, underscore);
            variant = number - 1;
            if (isFloor)
            {
                for (var i = 0; i < FloorStyles.Length; i++)
                    if (FloorStyles[i] == what)
                    {
                        index = i;
                        return ArtKind.Floor;
                    }
                return ArtKind.Unknown;
            }
            for (var i = 0; i < DecalCount; i++)
                if (Snake(((DecalKind)i).ToString()) == what)
                {
                    index = i;
                    return ArtKind.Decal;
                }
            return ArtKind.Unknown;
        }

        /// <summary>The file name for a prop.</summary>
        public static string PropName(PropKind kind) => "prop_" + Snake(kind.ToString());

        /// <summary>The names the importer accepts, for its report.</summary>
        public static string Accepted()
        {
            var props = new StringBuilder();
            for (var i = 0; i < PropCount; i++)
                props.Append(i == 0 ? "" : ", ").Append(Snake(((PropKind)i).ToString()));
            var decals = new StringBuilder();
            for (var i = 0; i < DecalCount; i++)
                decals.Append(i == 0 ? "" : ", ").Append(Snake(((DecalKind)i).ToString()));
            return $"floor_<{string.Join("|", FloorStyles)}>_<1..{MaxVariants}>, decal_<{decals}>_<1..{MaxVariants}>, wall, wall_low, prop_<{props}>";
        }

        static readonly int PropCount = System.Enum.GetValues(typeof(PropKind)).Length;
        static readonly int DecalCount = System.Enum.GetValues(typeof(DecalKind)).Length;

        /// <summary>BonePile to bone_pile.</summary>
        public static string Snake(string pascal)
        {
            var text = new StringBuilder(pascal.Length + 4);
            for (var i = 0; i < pascal.Length; i++)
            {
                var c = pascal[i];
                if (char.IsUpper(c) && i > 0)
                    text.Append('_');
                text.Append(char.ToLowerInvariant(c));
            }
            return text.ToString();
        }
    }
}
