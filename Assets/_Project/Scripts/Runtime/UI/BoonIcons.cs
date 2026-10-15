using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The boons' pixel icons (2026-10-15, the owner: "add small icons at the top of the viewport that shows which boons
    /// are active"): a 10 x 10 glyph per kind of boon, drawn in its colours inside a frame in its tree's colour. An
    /// "Improved" node shows its skill's glyph with a gold plus. Pure but for the sprite cache.
    /// </summary>
    public static class BoonIconArt
    {
        public const int Size = 12;

        static readonly Dictionary<string, string[]> Glyphs = new Dictionary<string, string[]>
        {
            ["arrow"] = new[] { "......####", ".......###", "......#.##", ".....#...#", "....#.....", "...#......", "..#.......", "+#........", ".++.......", "+.+......." },
            ["arrows"] = new[] { "....###...", ".....##...", "....#.#.##", "...#....##", "..#....#.#", ".#....#...", "#....#....", "....#.....", "...#......", "..#......." },
            ["pierce"] = new[] { "......+...", "......+...", "......+.#.", "#########.", "......+.#.", "......+...", "......+...", "..........", "..........", ".........." },
            ["split"] = new[] { "#.......#.", ".#.....#..", "..#...#...", "...#.#....", "....#.....", "....#.....", "....#.....", "...+#+....", "...+.+....", ".........." },
            ["seek"] = new[] { "......###.", ".....#+++#", ".....#+#+#", ".....#+++#", "......###.", "....#.....", "...#......", "..#.......", ".#........", "#........." },
            ["bounce"] = new[] { "#.......##", ".#......##", "..#....#..", "...#..#...", "....##....", "..........", "..........", "..........", "..........", ".........." },
            ["bow"] = new[] { "...##.....", "..#..#....", ".#....#...", ".#....+...", "#.....+...", "#.....+...", ".#....+...", ".#....#...", "..#..#....", "...##....." },
            ["drop"] = new[] { "....#.....", "....#.....", "...###....", "...###....", "..#####...", "..##+##...", ".###+###..", ".#######..", "..#####...", "...###...." },
            ["ring"] = new[] { "....#.....", ".#.....#..", "..........", "..........", "#...+...#.", "..........", "..........", ".#.....#..", "....#.....", ".........." },
            ["burst"] = new[] { "....#.....", ".#..#..#..", "..#.#.#...", "...+++....", "####+####.", "...+++....", "..#.#.#...", ".#..#..#..", "....#.....", ".........." },
            ["flame"] = new[] { "....#.....", "....##....", "...###....", "...####.#.", "..#####.#.", "..##++###.", ".##+++###.", ".##+++##..", "..#+++##..", "...####..." },
            ["frost"] = new[] { "....#.....", ".#..#..#..", "..#.#.#...", "...###....", "#########.", "...###....", "..#.#.#...", ".#..#..#..", "....#.....", ".........." },
            ["bolt"] = new[] { ".....###..", "....###...", "...###....", "..######..", ".....##...", "....##....", "...##.....", "..##......", ".##.......", ".#........" },
            ["skull"] = new[] { "..######..", ".########.", ".#..##..#.", ".#..##..#.", ".########.", "..##..##..", "..######..", "..#.##.#..", "..........", ".........." },
            ["barrage"] = new[] { ".+..+..+..", ".#..#..#..", ".#..#..#..", ".#..#..#..", ".#..#..#..", "#########.", ".#..#..#..", "..........", "..........", ".........." },
            ["breath"] = new[] { "..........", "######....", "......#...", "..###..#..", ".......#..", "#######...", "..........", "#####.....", ".....#....", "....#....." },
            ["claw"] = new[] { "#..#..#...", "#..#..#...", ".#..#..#..", ".#..#..#..", "..#..#..#.", "..#..#..#.", "...#..#..#", "...#..#..#", "..........", ".........." },
            ["knock"] = new[] { "..........", "....#..+..", ".....#..+.", "######..+.", ".....#..+.", "....#..+..", "..........", "..........", "..........", ".........." },
        };

        static readonly Color32 Bone = new Color32(232, 224, 198, 255);
        static readonly Color32 Wood = new Color32(168, 128, 86, 255);
        static readonly Color32 Gold = new Color32(240, 200, 80, 255);

        /// <summary>A node's glyph and its two colours.</summary>
        public static (string glyph, Color32 main, Color32 accent) Look(string nodeId) => nodeId switch
        {
            "pierce_arrow" or "piercing_arrows" => ("pierce", Bone, Wood),
            "extra_arrow" => ("arrows", Bone, Wood),
            "extra_shot" => ("bow", Wood, Bone),
            "splitting_arrows" or "split_arrow" => ("split", Bone, Wood),
            "seeking_arrows" or "homing_arrow" => ("seek", Bone, new Color32(200, 60, 50, 255)),
            "kill_shot" => ("skull", Bone, Bone),
            "barrage" => ("barrage", Bone, Wood),
            "bleeding_arrows" => ("drop", new Color32(170, 24, 20, 255), new Color32(230, 90, 80, 255)),
            "ricochet" => ("bounce", Bone, Wood),
            "arrow_nova" => ("ring", Bone, Gold),
            "hunters_breath" => ("breath", new Color32(120, 190, 230, 255), Bone),
            "corpse_burst" => ("burst", new Color32(180, 30, 24, 255), new Color32(240, 140, 60, 255)),
            "wild_frenzy" => ("claw", new Color32(220, 80, 40, 255), Bone),
            "knockback_shot" => ("knock", Bone, Wood),
            "fire_arrow" => ("flame", new Color32(232, 90, 24, 255), new Color32(255, 210, 90, 255)),
            "chain_lightning" => ("bolt", new Color32(150, 200, 255, 255), Bone),
            "freezing_arrow" => ("frost", new Color32(150, 220, 255, 255), Bone),
            "explosive_arrow" => ("burst", new Color32(240, 120, 30, 255), new Color32(255, 220, 110, 255)),
            _ => ("arrow", Bone, Wood),
        };

        public static Color32 TreeColor(TalentTree tree) => tree switch
        {
            TalentTree.Marksmanship => new Color32(120, 150, 70, 255),
            TalentTree.BeastMastery => new Color32(160, 100, 50, 255),
            _ => new Color32(70, 110, 160, 255),
        };

        /// <summary>The icon's pixels, <see cref="Size"/> square, bottom row first (as a texture wants them).</summary>
        public static Color32[] Draw(TalentNode node)
        {
            var pixels = new Color32[Size * Size];
            var frame = TreeColor(node.Tree);
            var fill = new Color32(22, 16, 14, 255);
            for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                    pixels[y * Size + x] = x == 0 || y == 0 || x == Size - 1 || y == Size - 1 ? frame : fill;
            var improved = node.ImprovesSkill != null;
            var (glyph, main, accent) = Look(improved ? node.Requires : node.Id);
            var rows = Glyphs[glyph];
            for (var row = 0; row < rows.Length; row++)
                for (var x = 0; x < rows[row].Length && x < Size - 2; x++)
                {
                    var c = rows[row][x];
                    if (c == '.')
                        continue;
                    pixels[(Size - 2 - row) * Size + 1 + x] = c == '#' ? main : accent;
                }
            if (improved)
            {
                // A gold plus in the top right corner.
                void Set(int x, int y) => pixels[y * Size + x] = Gold;
                Set(9, 10);
                Set(8, 9);
                Set(9, 9);
                Set(10, 9);
                Set(9, 8);
            }
            return pixels;
        }
    }

    /// <summary>
    /// The row of active boons along the top of the screen, under the depth: one icon a boon taken, its rank in a corner
    /// when it has more than one. Rebuilt when the boons change (checked twice a second, allocation free otherwise).
    /// </summary>
    public class BoonBar : MonoBehaviour
    {
        const float IconSize = 52f, Gap = 6f;
        const int PerRow = 8;

        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Sprites.Clear();

        RectTransform rect;
        TalentState shownFor;
        int shownSignature = -1;
        float checkTimer;

        public static BoonBar Create(Transform parent, Vector2 topLeft)
        {
            var rect = UiStyle.Place(UiStyle.Rect(parent, "Boons"), new Vector2(0f, 1f), topLeft, new Vector2(PerRow * (IconSize + Gap), IconSize * 3f));
            var bar = rect.gameObject.AddComponent<BoonBar>();
            bar.rect = rect;
            return bar;
        }

        void Update()
        {
            checkTimer -= Time.unscaledDeltaTime;
            if (checkTimer > 0f)
                return;
            checkTimer = 0.5f;
            var talents = GameSession.Current.Talents;
            var signature = talents.Spent;
            if (talents == shownFor && signature == shownSignature)
                return;
            shownFor = talents;
            shownSignature = signature;
            Rebuild(talents);
        }

        void Rebuild(TalentState talents)
        {
            for (var i = rect.childCount - 1; i >= 0; i--)
                Destroy(rect.GetChild(i).gameObject);
            var index = 0;
            foreach (var node in TalentRules.Nodes)
            {
                var rank = talents.RankOf(node.Id);
                if (rank <= 0)
                    continue;
                var x = index % PerRow * (IconSize + Gap);
                var y = -(index / PerRow) * (IconSize + Gap);
                var icon = UiStyle.Image(rect, node.Name, Color.white);
                icon.sprite = SpriteFor(node);
                icon.raycastTarget = false;
                UiStyle.Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(IconSize, IconSize));
                if (node.MaxRanks > 1)
                {
                    var label = UiStyle.Text(icon.transform, rank.ToString(), 22, UiStyle.Gold, TextAnchor.LowerRight, true);
                    UiStyle.Stretch(label.rectTransform, 3f);
                    label.raycastTarget = false;
                    var outline = label.gameObject.AddComponent<Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(2f, -2f);
                }
                index++;
            }
        }

        static Sprite SpriteFor(TalentNode node)
        {
            if (Sprites.TryGetValue(node.Id, out var sprite) && sprite != null)
                return sprite;
            var texture = new Texture2D(BoonIconArt.Size, BoonIconArt.Size, TextureFormat.RGBA32, false)
            {
                name = "Boon " + node.Id,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(BoonIconArt.Draw(node));
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, BoonIconArt.Size, BoonIconArt.Size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
            return Sprites[node.Id] = sprite;
        }
    }
}
