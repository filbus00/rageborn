using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The numbers of the lighting pass (Docs/05-world-and-content.md: dark scenes lit by the player's ember and enemy
    /// effects, dynamic 2D lights on the player and a few props only). Pure, so the curves are tested. Every value is
    /// tuning; the docs give none.
    /// </summary>
    public static class LightingRules
    {
        /// <summary>What light the scene itself gives, before the player's ember and the props.</summary>
        public readonly struct Mood
        {
            public readonly Color Ambient;
            public readonly float AmbientIntensity;

            /// <summary>How bright an unlit effect sprite (a hit flash, a dissolve) draws a character's own colors, so one
            /// does not jump to full brightness in a dark room. Roughly what the ember gives at the character's feet.</summary>
            public readonly float CharacterShade;

            public Mood(Color ambient, float intensity, float characterShade)
            {
                Ambient = ambient;
                AmbientIntensity = intensity;
                CharacterShade = characterShade;
            }
        }

        // The dungeon is near black and cool, so the ember's warmth reads; enough stays to see a room's walls.
        public static readonly Mood Dungeon = new Mood(new Color(0.55f, 0.62f, 0.9f), 0.2f, 0.8f);

        /// <summary>
        /// The dungeon at a depth (2026-10-05: it changes gradually, no hard sections): the first level as above, getting
        /// darker and turning from cold blue toward a bruised red-violet by the bottom, so the deep feels like a
        /// different place without a seam between levels.
        /// </summary>
        public static Mood DungeonAt(int depth)
        {
            var t = Mathf.Clamp01((depth - 1) / (float)(DungeonRules.Depths - 1));
            var color = Color.Lerp(Dungeon.Ambient, new Color(0.72f, 0.46f, 0.58f), t);
            return new Mood(color, Mathf.Lerp(Dungeon.AmbientIntensity, 0.14f, t), Mathf.Lerp(Dungeon.CharacterShade, 0.74f, t));
        }

        // Town at dusk: dimmer than day but everything visible, since nothing there attacks.
        public static readonly Mood Town = new Mood(new Color(0.8f, 0.74f, 0.92f), 0.62f, 0.9f);

        public static readonly Color EmberColor = new Color(1f, 0.64f, 0.34f);
        public const float EmberIntensity = 1.15f;
        public const float EmberInnerRadius = 2f;

        // World units, and a light is a circle on screen: 9 ground units sideways and more up and down, past the swarmer's
        // aggro range of 7, so a pack is seen before it wakes.
        public const float EmberOuterRadius = 9f;

        /// <summary>The ember's intensity multiplier over time: two slow waves and a faster one, never below 0.88 or
        /// above 1.06 so the light breathes rather than strobes. <paramref name="phase"/> keeps two lights out of step.</summary>
        public static float Flicker(float time, float phase = 0f)
        {
            var t = time + phase;
            var wave = Mathf.Sin(t * 2.1f) * 0.5f + Mathf.Sin(t * 3.7f + 1.3f) * 0.3f + Mathf.Sin(t * 11.3f + 0.7f) * 0.2f;
            return 0.97f + wave * 0.09f;
        }

        /// <summary>A hit flash's strength: full for the first third, then fading to nothing.</summary>
        public static float FlashAmount(float remaining, float duration)
        {
            if (remaining <= 0f || duration <= 0f)
                return 0f;
            return Mathf.Clamp01(remaining / duration * 1.5f);
        }

        /// <summary>
        /// A floor tile's tint, the same for a cell every time: a little darker or lighter, some a touch warmer or cooler,
        /// so the placeholder checkerboard stops reading as a grid. Brightness stays within 0.82 and 1.
        /// </summary>
        public static Color FloorShade(Vector2Int cell)
        {
            var a = Hash(cell.x, cell.y);
            var b = Hash(cell.y + 7919, cell.x - 104729);
            var brightness = 0.82f + a * 0.18f;
            var warmth = (b - 0.5f) * 0.08f;
            return new Color(
                Mathf.Clamp01(brightness + warmth),
                Mathf.Clamp01(brightness),
                Mathf.Clamp01(brightness - warmth),
                1f);
        }

        /// <summary>
        /// The ground's tint at a cell for the continuous floors (2026-10-04): a slow, smooth swell of light and warmth
        /// across about six cells, so neighbouring tiles differ by a hair and no grid shows (<see cref="FloorShade"/>'s
        /// tile-by-tile jumps drew one). Brightness stays within 0.86 and 1.
        /// </summary>
        public static Color GroundShade(Vector2Int cell)
        {
            var light = Smooth(cell.x / 6f, cell.y / 6f, 0);
            var warm = Smooth(cell.x / 9f + 31f, cell.y / 9f - 17f, 1);
            var brightness = 0.86f + light * 0.14f;
            var warmth = (warm - 0.5f) * 0.05f;
            return new Color(Mathf.Clamp01(brightness + warmth), Mathf.Clamp01(brightness), Mathf.Clamp01(brightness - warmth), 1f);
        }

        // Value noise: the lattice's hashed corners blended with a smoothstep.
        static float Smooth(float x, float y, int salt)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0 + salt * 1013, y0), b = Hash(x0 + 1 + salt * 1013, y0);
            float c = Hash(x0 + salt * 1013, y0 + 1), d = Hash(x0 + 1 + salt * 1013, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        // An integer hash to 0..1, stable across platforms (no floating point sine).
        static float Hash(int x, int y)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
