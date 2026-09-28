using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>One baked animation: its frames by direction row (0 south, then clockwise seen from above).</summary>
    public sealed class CharacterSheet
    {
        public Sprite[][] Rows;
        public int Frames;
    }

    /// <summary>
    /// Loads the sprite bake's sheets (Resources/Characters/&lt;character&gt;/&lt;sheet&gt;, a whole sheet or one file per
    /// direction) and a character's timing file (<c>&lt;clip&gt; &lt;seconds&gt; [&lt;ground speed&gt;]</c>, keys with or
    /// without a grip prefix). Shared by the player's layered sprite and the enemies.
    /// </summary>
    public static class CharacterSheets
    {
        /// <summary>A whole sheet, or its per-direction files. Frames are found by their names' last two parts: the
        /// direction code and the frame number. Null when the sheet does not exist.</summary>
        public static CharacterSheet Load(string path)
        {
            var sprites = new List<Sprite>(Resources.LoadAll<Sprite>(path));
            if (sprites.Count == 0)
                foreach (var code in AppearanceRules.DirectionCodes(16))
                    sprites.AddRange(Resources.LoadAll<Sprite>($"{path}_{code}"));
            if (sprites.Count == 0)
                return null;

            // 16 directions when any frame carries a code the 8 lack (ssw, wsw...).
            var sixteen = AppearanceRules.DirectionCodes(16);
            var eight = AppearanceRules.DirectionCodes(8);
            var count = 8;
            foreach (var sprite in sprites)
            {
                var parts = sprite.name.Split('_');
                if (parts.Length >= 2 && System.Array.IndexOf(eight, parts[parts.Length - 2]) < 0 &&
                    System.Array.IndexOf(sixteen, parts[parts.Length - 2]) >= 0)
                {
                    count = 16;
                    break;
                }
            }
            var codes = AppearanceRules.DirectionCodes(count);

            var frames = 0;
            var parsed = new List<(int row, int frame, Sprite sprite)>(sprites.Count);
            foreach (var sprite in sprites)
            {
                var parts = sprite.name.Split('_');
                if (parts.Length < 2 || !int.TryParse(parts[parts.Length - 1], out var frame))
                    continue;
                var direction = System.Array.IndexOf(codes, parts[parts.Length - 2]);
                if (direction < 0)
                    continue;
                parsed.Add((direction, frame, sprite));
                frames = Mathf.Max(frames, frame + 1);
            }
            if (frames == 0)
                return null;

            var sheet = new CharacterSheet { Frames = frames, Rows = new Sprite[count][] };
            for (var i = 0; i < sheet.Rows.Length; i++)
                sheet.Rows[i] = new Sprite[frames];
            foreach (var (direction, frame, sprite) in parsed)
                sheet.Rows[direction][frame] = sprite;
            // A missing frame shows its neighbour rather than nothing.
            foreach (var rowFrames in sheet.Rows)
                for (var f = 0; f < frames; f++)
                    if (rowFrames[f] == null)
                        rowFrames[f] = f > 0 ? rowFrames[f - 1] : System.Array.Find(rowFrames, s => s != null);
            return sheet;
        }

        /// <summary>Reads a character's timing file into seconds and recorded ground speeds by key.</summary>
        public static void LoadTiming(string character, Dictionary<string, float> seconds, Dictionary<string, float> speeds)
        {
            var file = Resources.Load<TextAsset>($"Characters/{character}/{character}_timing");
            if (file == null)
                return;
            foreach (var line in file.text.Split('\n'))
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length >= 2 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var value) && value > 0f)
                    seconds[parts[0]] = value;
                if (parts.Length >= 3 && float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var speed))
                    speeds[parts[0]] = speed;
            }
        }
    }
}
