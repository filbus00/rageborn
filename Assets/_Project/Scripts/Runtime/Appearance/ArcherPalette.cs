using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Her hair and skin in the drawn sheets, and how to swap them (the owner, 2026-11-18: "now make the character model
    /// match the hair and skin color of each generated character portrait"). The sheets are hard-edged pixel art in a
    /// fixed palette: her hair is three exact colours and her skin three (build_archer_sheets.py HAIR, SKIN), in her body
    /// sheets only (a helm layer repaints her head in the helm's own colours, and a bow draws none). Each key colour is
    /// replaced by the portrait's colour at the same shade: the key's share of the average of its ramp, taken over the
    /// pixels the sheets hold of each, scales the portrait's colour, so the shading survives. Pure.
    /// </summary>
    public static class ArcherPalette
    {
        public static readonly Color32[] HairKeys =
        {
            new Color32(240, 200, 104, 255), new Color32(230, 194, 92, 255), new Color32(190, 134, 52, 255),
        };

        public static readonly Color32[] SkinKeys =
        {
            new Color32(236, 190, 154, 255), new Color32(226, 150, 132, 255), new Color32(192, 134, 106, 255),
        };

        // How many pixels of each key a body sheet holds (an idle sheet counts), so the ramp's average is the one the eye
        // sees: her hair is mostly its darkest shade, her skin mostly its first.
        static readonly int[] HairWeights = { 888, 196, 2432 };
        static readonly int[] SkinWeights = { 920, 48, 136 };

        /// <summary>The three colours a ramp's keys become for a portrait colour: the key's channels over the ramp's average,
        /// times the colour, with a faint lift on the lighter shades so dark hair keeps a highlight.</summary>
        public static Color32[] Ramp(Color target, Color32[] keys, int[] weights)
        {
            float ar = 0f, ag = 0f, ab = 0f, total = 0f;
            for (var i = 0; i < keys.Length; i++)
            {
                ar += keys[i].r / 255f * weights[i];
                ag += keys[i].g / 255f * weights[i];
                ab += keys[i].b / 255f * weights[i];
                total += weights[i];
            }
            ar /= total;
            ag /= total;
            ab /= total;
            var averageLight = Light(ar, ag, ab);
            var result = new Color32[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                float r = keys[i].r / 255f, g = keys[i].g / 255f, b = keys[i].b / 255f;
                var lift = 0.06f * (Light(r, g, b) / averageLight - 1f);
                result[i] = new Color32(Byte(target.r * r / ar + lift), Byte(target.g * g / ag + lift), Byte(target.b * b / ab + lift), 255);
            }
            return result;
        }

        public static Color32[] HairRamp(Color hair) => Ramp(hair, HairKeys, HairWeights);

        public static Color32[] SkinRamp(Color skin) => Ramp(skin, SkinKeys, SkinWeights);

        static float Light(float r, float g, float b) => 0.299f * r + 0.587f * g + 0.114f * b;

        static byte Byte(float value) => (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);

        /// <summary>Swaps the key colours of a sheet's pixels for the ramps; every other pixel is left. Returns how many changed.</summary>
        public static int Recolour(Color32[] pixels, Color32[] hair, Color32[] skin)
        {
            var changed = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a == 0)
                    continue;
                var swapped = false;
                for (var k = 0; k < 3 && !swapped; k++)
                {
                    if (p.r == HairKeys[k].r && p.g == HairKeys[k].g && p.b == HairKeys[k].b)
                    {
                        pixels[i] = new Color32(hair[k].r, hair[k].g, hair[k].b, p.a);
                        swapped = true;
                    }
                    else if (p.r == SkinKeys[k].r && p.g == SkinKeys[k].g && p.b == SkinKeys[k].b)
                    {
                        pixels[i] = new Color32(skin[k].r, skin[k].g, skin[k].b, p.a);
                        swapped = true;
                    }
                }
                if (swapped)
                    changed++;
            }
            return changed;
        }
    }
}
