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
        /// <summary>Final pixels per metre across the screen: one world unit is 128 px, and a tile's 1 m diagonal is 128 px.</summary>
        public const float PixelsPerMeter = 128f;

        /// <summary>The camera looks down 30 degrees, so a ground length along the view shows at half (the 2:1 tile).</summary>
        public const float ElevationDegrees = 30f;

        /// <summary>The engine's texture limit; a sheet above it is split into one file per direction.</summary>
        public const int MaxSheetSize = 4096;

        /// <summary>Row order of every sheet (the brief, 0.3).</summary>
        public static readonly string[] DirectionCodes = { "s", "sw", "w", "nw", "n", "ne", "e", "se" };

        public const int DirectionCount = 8;

        /// <summary>
        /// The direction's angle on the ground, counter-clockwise from screen right (E 0, N 90, S 270). The game's ground
        /// space has its x along the screen and its y up the screen, so the 8 directions are 45 degrees apart on the ground,
        /// and the diagonals show on screen at the 2:1 slope.
        /// </summary>
        public static float GroundAngle(int row) => Mathf.Repeat(270f - 45f * row, 360f);

        /// <summary>
        /// Which way the model faces for a row, in the bake scene, whose camera looks along +z with +x to the right: so
        /// ground east is +x and ground north (up the screen, away from the camera) is +z. S faces the camera (-z).
        /// </summary>
        public static Vector3 Facing(int row)
        {
            var radians = GroundAngle(row) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        }

        /// <summary>The orthographic size (half the view's height in metres) that makes a square cell of this many final
        /// pixels.</summary>
        public static float OrthographicSize(int cellSize) => cellSize * 0.5f / PixelsPerMeter;

        /// <summary>How far the view's centre sits from the pivot, in screen metres (right and up), so the model's feet at
        /// the origin land on the pivot pixel, measured from the cell's bottom left.</summary>
        public static Vector2 CenterFromPivot(int cellSize, Vector2 pivot) =>
            new Vector2((cellSize * 0.5f - pivot.x) / PixelsPerMeter, (cellSize * 0.5f - pivot.y) / PixelsPerMeter);

        /// <summary>
        /// How tall a box of this height and depth (metres, depth along the view) shows on screen: the camera's tilt
        /// shortens the height by cos 30 degrees and shows the depth at sin 30, so a foot or a pauldron that reaches toward
        /// the camera adds to the height too.
        /// </summary>
        public static float ScreenHeight(float height, float depth) =>
            height * Mathf.Cos(ElevationDegrees * Mathf.Deg2Rad) + depth * Mathf.Sin(ElevationDegrees * Mathf.Deg2Rad);

        /// <summary>The uniform scale that makes a model showing this many metres high on screen show this many pixels
        /// high. Zero or less keeps the model's own size.</summary>
        public static float ModelScale(float screenHeightMeters, float targetHeightPixels)
        {
            if (targetHeightPixels <= 0f || screenHeightMeters <= 0f)
                return 1f;
            return targetHeightPixels / (screenHeightMeters * PixelsPerMeter);
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

        /// <summary>Whether a sheet of 8 rows fits the texture limit, else one file per direction.</summary>
        public static bool NeedsSplit(int frames, int cellSize) =>
            frames * cellSize > MaxSheetSize || DirectionCount * cellSize > MaxSheetSize;

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
    }
}
