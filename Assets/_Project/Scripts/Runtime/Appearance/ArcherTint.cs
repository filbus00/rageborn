using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Gives each archer's drawn body her portrait's hair and skin (the owner, 2026-11-18): the portrait's colours come
    /// from her name (<see cref="PortraitLooks"/>), and a copy of each body sheet she plays is made with them swapped in
    /// (<see cref="ArcherPalette"/>), once per archer and sheet. Only the player's drawn Wild Arrow body sheets (and the
    /// camp's archers') are readable; a sheet that is not is shown as drawn. The last few archers' copies are kept, older
    /// ones destroyed.
    /// </summary>
    public static class ArcherTint
    {
        public const int Kept = 8;

        sealed class Set
        {
            public readonly Dictionary<string, CharacterSheet> Sheets = new Dictionary<string, CharacterSheet>();
            public readonly List<Object> Made = new List<Object>();
            public Color32[] Hair, Skin;
        }

        static readonly Dictionary<string, Set> sets = new Dictionary<string, Set>();
        static readonly List<string> order = new List<string>();
        static bool warned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            sets.Clear();
            order.Clear();
            warned = false;
        }

        /// <summary>Whether a sheet of a character is one the tint changes: a drawn Wild Arrow body.</summary>
        public static bool Applies(string character, string sheetName) =>
            character == PlayerSpriteAnimator.WildArrowDrawnCharacter && sheetName.Contains("_body_");

        /// <summary>The sheet in the hair and skin of the archer with this name (the sheet itself when it cannot be).</summary>
        public static CharacterSheet For(string archer, string sheetName, CharacterSheet source)
        {
            if (source == null || string.IsNullOrEmpty(archer))
                return source;
            if (!sets.TryGetValue(archer, out var set))
            {
                var look = PortraitFaces.LookFor(archer);
                set = new Set { Hair = ArcherPalette.HairRamp(look.Hair), Skin = ArcherPalette.SkinRamp(look.Skin) };
                sets[archer] = set;
                order.Add(archer);
                while (order.Count > Kept)
                    Forget(order[0]);
            }
            if (set.Sheets.TryGetValue(sheetName, out var tinted))
                return tinted;
            tinted = Make(source, set);
            set.Sheets[sheetName] = tinted;
            return tinted;
        }

        static void Forget(string archer)
        {
            if (sets.TryGetValue(archer, out var set))
                foreach (var made in set.Made)
                    if (made != null)
                        Object.Destroy(made);
            sets.Remove(archer);
            order.Remove(archer);
        }

        static CharacterSheet Make(CharacterSheet source, Set set)
        {
            var textures = new Dictionary<Texture2D, Texture2D>();
            var rows = new Sprite[source.Rows.Length][];
            var made = new Dictionary<Sprite, Sprite>();
            for (var r = 0; r < rows.Length; r++)
            {
                rows[r] = new Sprite[source.Rows[r].Length];
                for (var f = 0; f < rows[r].Length; f++)
                {
                    var sprite = source.Rows[r][f];
                    if (sprite == null)
                        continue;
                    if (!made.TryGetValue(sprite, out var copy))
                    {
                        var texture = Recoloured(sprite.texture, textures, set);
                        if (texture == null)
                            return source;
                        var rect = sprite.rect;
                        copy = Sprite.Create(texture, rect, new Vector2(sprite.pivot.x / rect.width, sprite.pivot.y / rect.height),
                            sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
                        copy.name = sprite.name;
                        set.Made.Add(copy);
                        made[sprite] = copy;
                    }
                    rows[r][f] = copy;
                }
            }
            return new CharacterSheet { Frames = source.Frames, Rows = rows };
        }

        static Texture2D Recoloured(Texture2D source, Dictionary<Texture2D, Texture2D> textures, Set set)
        {
            if (textures.TryGetValue(source, out var done))
                return done;
            if (!source.isReadable)
            {
                if (!warned)
                {
                    Debug.LogWarning($"[ARPG] {source.name} is not readable: her hair and skin keep their drawn colours (run Tools > ARPG > Make Archer Bodies Readable).");
                    warned = true;
                }
                return null;
            }
            var pixels = source.GetPixels32();
            ArcherPalette.Recolour(pixels, set.Hair, set.Skin);
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = source.name + " tinted",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            copy.SetPixels32(pixels);
            copy.Apply(false, true);
            set.Made.Add(copy);
            textures[source] = copy;
            return copy;
        }
    }
}
