using System;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The numbers of the sprite bake (Docs/09-art-brief.md, sections 0.2 to 0.4 and 0.7), pure so they are tested:
    /// the camera, the 8 directions, the sample times, the sheet layout, and turning two renders into a transparent,
    /// downscaled cell.
    /// </summary>
    public static class SpriteBakeMath
    {
        /// <summary>Final pixels per metre across the screen at full resolution: one world unit is 128 px, and a tile's 1 m
        /// diagonal is 128 px. A job can bake at a lower resolution (<see cref="SpriteBakeJob.pixelsPerUnit"/>).</summary>
        public const float PixelsPerMeter = 128f;

        /// <summary>The camera looks down 30 degrees, so a ground length along the view shows at half (the 2:1 tile).</summary>
        public const float ElevationDegrees = 30f;

        /// <summary>The engine's texture limit; a sheet above it is split into one file per direction.</summary>
        public const int MaxSheetSize = 4096;

        /// <summary>Row order of an 8-direction sheet (the brief, 0.3); see <see cref="AppearanceRules.DirectionCodes"/>.</summary>
        public static readonly string[] DirectionCodes = AppearanceRules.DirectionCodes(8);

        /// <summary>The usual count; a job can ask for 16.</summary>
        public const int DirectionCount = 8;

        /// <summary>
        /// The direction's angle on the ground, counter-clockwise from screen right (E 0, N 90, S 270). The game's ground
        /// space has its x along the screen and its y up the screen, so the 8 directions are 45 degrees apart on the ground,
        /// and the diagonals show on screen at the 2:1 slope.
        /// </summary>
        public static float GroundAngle(int row, int count = DirectionCount) => Mathf.Repeat(270f - 360f / count * row, 360f);

        /// <summary>
        /// Which way the model faces for a row, in the bake scene, whose camera looks along +z with +x to the right: so
        /// ground east is +x and ground north (up the screen, away from the camera) is +z. S faces the camera (-z).
        /// </summary>
        public static Vector3 Facing(int row, int count = DirectionCount)
        {
            var radians = GroundAngle(row, count) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        }

        /// <summary>The orthographic size (half the view's height in metres) that makes a square cell of this many final
        /// pixels.</summary>
        public static float OrthographicSize(int cellSize, float pixelsPerMeter = PixelsPerMeter) => cellSize * 0.5f / pixelsPerMeter;

        /// <summary>How far the view's centre sits from the pivot, in screen metres (right and up), so the model's feet at
        /// the origin land on the pivot pixel, measured from the cell's bottom left.</summary>
        public static Vector2 CenterFromPivot(int cellSize, Vector2 pivot, float pixelsPerMeter = PixelsPerMeter) =>
            new Vector2((cellSize * 0.5f - pivot.x) / pixelsPerMeter, (cellSize * 0.5f - pivot.y) / pixelsPerMeter);

        /// <summary>
        /// How tall a box of this height and depth (metres, depth along the view) shows on screen: the camera's tilt
        /// shortens the height by cos 30 degrees and shows the depth at sin 30, so a foot or a pauldron that reaches toward
        /// the camera adds to the height too.
        /// </summary>
        public static float ScreenHeight(float height, float depth) =>
            height * Mathf.Cos(ElevationDegrees * Mathf.Deg2Rad) + depth * Mathf.Sin(ElevationDegrees * Mathf.Deg2Rad);

        /// <summary>The uniform scale that makes a model showing this many metres high on screen show this many pixels
        /// high. Zero or less keeps the model's own size.</summary>
        public static float ModelScale(float screenHeightMeters, float targetHeightPixels, float pixelsPerMeter = PixelsPerMeter)
        {
            if (targetHeightPixels <= 0f || screenHeightMeters <= 0f)
                return 1f;
            return targetHeightPixels / (screenHeightMeters * pixelsPerMeter);
        }

        /// <summary>
        /// When to sample each frame of a clip. A loop spreads the frames over the whole length without repeating the
        /// first at the end (so it joins seamlessly); a one-shot includes both ends. Frames of 0 or less take the clip's
        /// length at <paramref name="framesPerSecond"/>.
        /// </summary>
        public static float[] SampleTimes(float length, int frames, bool loop, float framesPerSecond)
        {
            if (frames <= 0)
                frames = Mathf.Max(1, Mathf.RoundToInt(length * framesPerSecond) + (loop ? 0 : 1));
            var times = new float[frames];
            for (var i = 0; i < frames; i++)
                times[i] = frames == 1 ? 0f : loop ? length * i / frames : length * i / (frames - 1);
            return times;
        }

        /// <summary>
        /// Where a one-shot's action is. <paramref name="motion"/> is how far the body's key points moved between samples.
        /// Summed into 0.2 s bins, the action is the bins above 40 percent of the busiest one, pauses up to 0.8 s bridged,
        /// the run holding the busiest bin, widened by <paramref name="pad"/> seconds each side. Mixamo's Hew was 5.2 s of
        /// idle sway with the swing in about 2 s of it. With <paramref name="keepEnd"/> it runs to the clip's end (a death,
        /// whose last pose is held). A clip with no motion keeps its whole length.
        /// </summary>
        public static Vector2 ActiveWindow(float[] motion, float sampleRate, float length, float pad, bool keepEnd)
        {
            const float binSeconds = 0.2f, threshold = 0.4f, bridgeSeconds = 0.8f;
            var perBin = Mathf.Max(1, Mathf.RoundToInt(binSeconds * sampleRate));
            var bins = new float[(motion.Length + perBin - 1) / perBin];
            for (var i = 0; i < motion.Length; i++)
                bins[i / perBin] += motion[i];
            var peak = 0;
            for (var i = 1; i < bins.Length; i++)
                if (bins[i] > bins[peak])
                    peak = i;
            if (bins.Length == 0 || bins[peak] <= 1e-6f)
                return new Vector2(0f, length);
            var busy = bins[peak] * threshold;
            var maxGap = Mathf.RoundToInt(bridgeSeconds / binSeconds);
            int first = peak, last = peak;
            for (var i = peak - 1; i >= 0 && first - i <= maxGap + 1; i--)
                if (bins[i] >= busy)
                    first = i;
            for (var i = peak + 1; i < bins.Length && i - last <= maxGap + 1; i++)
                if (bins[i] >= busy)
                    last = i;
            var seconds = perBin / sampleRate;
            var start = Mathf.Max(0f, first * seconds - pad);
            var end = keepEnd ? length : Mathf.Min(length, (last + 1) * seconds + pad);
            return new Vector2(start, end);
        }

        /// <summary>
        /// Where the legs of a moving action are in their loop at each frame: the action's frames are played in the game
        /// over <paramref name="playbackSeconds"/>, and the legs run through the loop at <paramref name="legRate"/> times
        /// its recorded pace (the character's speed over the loop's recorded ground speed), so the feet keep pace with the
        /// ground while the arms swing. Loop times, in 0 to <paramref name="loopLength"/>.
        /// </summary>
        public static float[] LegTimes(int frames, bool includeEnd, float playbackSeconds, float legRate, float loopLength)
        {
            var times = new float[frames];
            if (loopLength <= 0f)
                return times;
            for (var i = 0; i < frames; i++)
            {
                var progress = frames == 1 ? 0f : includeEnd ? i / (float)(frames - 1) : i / (float)frames;
                times[i] = Mathf.Repeat(progress * playbackSeconds * legRate, loopLength);
            }
            return times;
        }

        /// <summary>Whether a sheet of a row per direction fits the texture limit, else one file per direction.</summary>
        public static bool NeedsSplit(int frames, int cellSize, int count = DirectionCount) =>
            frames * cellSize > MaxSheetSize || count * cellSize > MaxSheetSize;

        /// <summary>Columns and rows of one direction's own file when a sheet is split: rows as wide as the limit allows.</summary>
        public static Vector2Int SplitGrid(int frames, int cellSize)
        {
            var columns = Mathf.Clamp(MaxSheetSize / cellSize, 1, frames);
            return new Vector2Int(columns, (frames + columns - 1) / columns);
        }

        /// <summary>
        /// The ground speed a locomotion clip looks right at: how fast a planted foot slides along the ground in an
        /// in-place clip (the body stays put, so the planted foot travels backward at the speed the body would go). A foot
        /// counts as planted while it is within <paramref name="contactTolerance"/> of the lowest point either foot reaches
        /// (the floor), so a foot that never comes down never counts. Feet positions are
        /// per foot, per sample, <paramref name="sampleRate"/> samples a second, in the units the game uses. 0 when no foot
        /// is ever planted between two samples (an idle, an attack in place).
        /// </summary>
        public static float PlantedFootSpeed(Vector3[][] feet, float sampleRate, float contactTolerance)
        {
            float sum = 0f;
            var count = 0;
            var lowest = float.MaxValue;
            foreach (var samples in feet)
                if (samples != null)
                    foreach (var p in samples)
                        lowest = Mathf.Min(lowest, p.y);
            var limit = lowest + contactTolerance;
            foreach (var samples in feet)
            {
                if (samples == null || samples.Length < 2)
                    continue;
                for (var i = 1; i < samples.Length; i++)
                {
                    if (samples[i].y > limit || samples[i - 1].y > limit)
                        continue;
                    var step = samples[i] - samples[i - 1];
                    step.y = 0f;
                    sum += step.magnitude * sampleRate;
                    count++;
                }
            }
            return count > 0 ? sum / count : 0f;
        }

        /// <summary>
        /// Recovers colour and transparency from the same frame rendered over black and over white: where the two differ
        /// the background showed through. Returns straight (not premultiplied) colour, linear-blend exact for any renderer,
        /// which is why the bake renders twice rather than trusting the camera's alpha (HDR drops it).
        /// </summary>
        public static Color[] Matte(Color[] overBlack, Color[] overWhite)
        {
            if (overBlack.Length != overWhite.Length)
                throw new ArgumentException("Both renders must be the same size.");
            var result = new Color[overBlack.Length];
            for (var i = 0; i < result.Length; i++)
            {
                var b = overBlack[i];
                var w = overWhite[i];
                var seen = ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f;
                var alpha = Mathf.Clamp01(1f - seen);
                result[i] = alpha <= 1e-4f
                    ? new Color(0f, 0f, 0f, 0f)
                    : new Color(Mathf.Clamp01(b.r / alpha), Mathf.Clamp01(b.g / alpha), Mathf.Clamp01(b.b / alpha), alpha);
            }
            return result;
        }

        /// <summary>
        /// Shrinks an image by a whole factor, averaging each block with its colour weighted by alpha, so a soft edge does
        /// not pick up a dark fringe from the transparent pixels around it.
        /// </summary>
        public static Color[] Downsample(Color[] source, int width, int height, int factor)
        {
            if (factor <= 1)
                return (Color[])source.Clone();
            var outWidth = width / factor;
            var outHeight = height / factor;
            var result = new Color[outWidth * outHeight];
            var count = factor * factor;
            for (var y = 0; y < outHeight; y++)
                for (var x = 0; x < outWidth; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (var sy = 0; sy < factor; sy++)
                        for (var sx = 0; sx < factor; sx++)
                        {
                            var c = source[(y * factor + sy) * width + x * factor + sx];
                            r += c.r * c.a;
                            g += c.g * c.a;
                            b += c.b * c.a;
                            a += c.a;
                        }
                    result[y * outWidth + x] = a <= 1e-6f ? new Color(0f, 0f, 0f, 0f) : new Color(r / a, g / a, b / a, a / count);
                }
            return result;
        }
    

        /// <summary>
        /// The part of a sheet's cells that any frame draws in, in cell pixels, grown by a margin and kept inside the cell:
        /// every cell of the sheet is cropped to it (<see cref="SheetTrimmer"/>). A helm or a belt draws in a small part of
        /// its 80 px cells, and on iOS a transparent pixel costs as much as a drawn one. The margin keeps a transparent edge
        /// round each frame, so filtering does not pick up the neighbouring frame. An empty sheet keeps one pixel.
        /// </summary>
        public static RectInt UsedArea(Color32[] pixels, int width, int height, int cellWidth, int cellHeight, int margin)
        {
            int minX = cellWidth, minY = cellHeight, maxX = -1, maxY = -1;
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a == 0)
                        continue;
                    var cx = x % cellWidth;
                    var cy = y % cellHeight;
                    if (cx < minX) minX = cx;
                    if (cx > maxX) maxX = cx;
                    if (cy < minY) minY = cy;
                    if (cy > maxY) maxY = cy;
                }
            if (maxX < 0)
                return new RectInt(0, 0, 1, 1);
            minX = Math.Max(0, minX - margin);
            minY = Math.Max(0, minY - margin);
            maxX = Math.Min(cellWidth - 1, maxX + margin);
            maxY = Math.Min(cellHeight - 1, maxY + margin);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>A sprite's pivot (normalized, as the importer keeps it) after its cell is cropped to an area, so the
        /// feet stay where they were. It can fall outside 0 to 1 (a helm's area is above the feet).</summary>
        public static Vector2 TrimmedPivot(Vector2 pivot, int cellWidth, int cellHeight, RectInt area) =>
            new Vector2((pivot.x * cellWidth - area.x) / area.width, (pivot.y * cellHeight - area.y) / area.height);
    }
}
