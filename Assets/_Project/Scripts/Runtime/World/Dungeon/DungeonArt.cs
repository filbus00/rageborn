using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>
    /// Placeholder dungeon art drawn in code the first time it is asked for (the owner, 2026-09-30: "Add some placeholder
    /// assets also to make it more interesting. Like different floor textures, brushes, and whatever fits inside a
    /// dungeon"), until the tiles and props of Docs/09 exist. Four floor styles (flagstone, dark brick, packed earth,
    /// mossy stone), three variants each, with the decals drawn into a copy of the tile; and the props, standing on the
    /// cell's middle in the wall tile's frame (pivot at a quarter height), blocking as walls do; and the walls, full and
    /// cut low. Everything is drawn at 128 pixels a unit and brought down to the game's pixel art (the owner, 2026-09-30:
    /// "I want it pixelated"): averaged to <see cref="PixelArt.PixelsPerUnit"/>, snapped to the palette, props outlined.
    /// Cached for the session, so a level asks for them freely.
    /// Art made by hand or generated and brought in with `Tools > ARPG > Import Pixel Art` (`PixelArtImporter`, names in
    /// <see cref="ArtNames"/>) lies in Resources/Art/Dungeon and takes the place of the code-drawn piece it names: a floor
    /// style's imported variants replace all of its code-drawn ones, while anything not imported is still drawn here.
    /// </summary>
    public static class DungeonArt
    {
        // The drawing resolution: 128 pixels a unit.
        const int TileW = 128;
        const int TileH = 64;
        const int PropSize = 128;
        public const int Variants = 3;

        // The game's: PixelArt.PixelsPerUnit a unit.
        const int Unit = PixelArt.PixelsPerUnit;

        static readonly Dictionary<int, Tile> floors = new Dictionary<int, Tile>();
        static readonly Dictionary<PropKind, Tile> props = new Dictionary<PropKind, Tile>();
        static Tile wall;
        static Tile lowWall;

        /// <summary>Where the importer puts the processed art, under Resources.</summary>
        public const string ImportedFolder = "Art/Dungeon";

        // The imported sprites by kind, loaded once at first use; empty lists where nothing was imported.
        static bool loaded;
        static readonly List<Sprite>[] importedFloors = new List<Sprite>[ArtNames.FloorStyles.Length];
        static readonly Dictionary<int, List<Sprite>> importedDecals = new Dictionary<int, List<Sprite>>();
        static readonly Dictionary<int, Sprite> importedProps = new Dictionary<int, Sprite>();
        static Sprite importedWall;
        static Sprite importedLowWall;

        // Domain reload is off: a new play forgets the last one's tiles, so art imported in between shows.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            floors.Clear();
            props.Clear();
            wall = null;
            lowWall = null;
            loaded = false;
        }

        static void LoadImported()
        {
            if (loaded)
                return;
            loaded = true;
            for (var i = 0; i < importedFloors.Length; i++)
                importedFloors[i] = new List<Sprite>();
            importedDecals.Clear();
            importedProps.Clear();
            importedWall = null;
            importedLowWall = null;
            var all = Resources.LoadAll<Sprite>(ImportedFolder);
            // By name, so variant 1 comes before 2 whatever order the folder lists them in.
            System.Array.Sort(all, (x, y) => string.CompareOrdinal(x.name, y.name));
            var floorSlots = new SortedList<int, Sprite>[importedFloors.Length];
            var decalSlots = new Dictionary<int, SortedList<int, Sprite>>();
            foreach (var sprite in all)
            {
                switch (ArtNames.Parse(sprite.name, out var index, out var variant))
                {
                    case ArtKind.Floor:
                        (floorSlots[index] ??= new SortedList<int, Sprite>())[variant] = sprite;
                        break;
                    case ArtKind.Decal:
                        if (!decalSlots.TryGetValue(index, out var list))
                            decalSlots[index] = list = new SortedList<int, Sprite>();
                        list[variant] = sprite;
                        break;
                    case ArtKind.Wall:
                        importedWall = sprite;
                        break;
                    case ArtKind.LowWall:
                        importedLowWall = sprite;
                        break;
                    case ArtKind.Prop:
                        importedProps[index] = sprite;
                        break;
                }
            }
            for (var i = 0; i < floorSlots.Length; i++)
                if (floorSlots[i] != null)
                    importedFloors[i].AddRange(floorSlots[i].Values);
            foreach (var pair in decalSlots)
                importedDecals[pair.Key] = new List<Sprite>(pair.Value.Values);
        }

        /// <summary>How many variants a floor style has: its imported ones, else the code-drawn <see cref="Variants"/>.</summary>
        public static int VariantCount(int style)
        {
            LoadImported();
            var imported = style >= 0 && style < importedFloors.Length ? importedFloors[style].Count : 0;
            return imported > 0 ? imported : Variants;
        }

        /// <summary>
        /// Repaints the town's ground in the pixel-art floors: packed earth, with worn flagstone where a slow noise says a
        /// path or square runs (the scene's own tiles were smooth placeholders at 128 pixels a unit).
        /// </summary>
        public static void PaveTown(Tilemap ground)
        {
            var bounds = ground.cellBounds;
            var tiles = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
            var i = 0;
            foreach (var cell in bounds.allPositionsWithin)
            {
                if (ground.HasTile(cell))
                {
                    var paved = Fractal(cell.x * 0.09f, cell.y * 0.09f, 404) > 0.55f;
                    var count = VariantCount(paved ? 0 : 2);
                    var variant = (int)(Hash(cell.x, cell.y, 9) * count) % count;
                    var decal = Hash(cell.x, cell.y, 21) < 0.05f ? (int)DecalKind.Cracks : Hash(cell.x, cell.y, 22) < 0.02f ? (int)DecalKind.Rubble : -1;
                    tiles[i] = Floor(paved ? 0 : 2, variant, decal);
                }
                i++;
            }
            ground.SetTilesBlock(bounds, tiles);
        }

        /// <summary>A wall block, full height or cut low (the camera-side walls, WallRules).</summary>
        public static Tile Wall(bool low)
        {
            var cached = low ? lowWall : wall;
            if (cached != null && cached.sprite != null)
                return cached;
            LoadImported();
            var imported = low ? importedLowWall : importedWall;
            if (imported != null)
            {
                var importedTile = SpriteTile(imported, Tile.ColliderType.Grid);
                if (low)
                    lowWall = importedTile;
                else
                    wall = importedTile;
                return importedTile;
            }
            // Half a unit tall, or an eighth cut low, standing on a diamond whose middle is a quarter up the full frame.
            var height = low ? 16 : 64;
            var hiH = 64 + height;
            var hi = new Color32[TileW * hiH];
            DrawWall(hi, hiH, height);
            var loH = Mathf.RoundToInt(hiH * Unit / 128f);
            var lo = PixelArt.Downscale(hi, TileW, hiH, Unit, loH);
            PixelArt.Process(lo, Unit, loH, false);
            var tile = MakeTile(lo, Unit, loH, new Vector2(0.5f, (Unit / 4f) / loH), Tile.ColliderType.Grid, low ? "Low Wall" : "Wall");
            if (low)
                lowWall = tile;
            else
                wall = tile;
            return tile;
        }

        /// <summary>A floor tile of a style and variant, with a decal drawn on it or none (-1).</summary>
        public static Tile Floor(int style, int variant, int decal = -1)
        {
            var key = (style * 16 + variant) * 16 + decal + 1;
            if (floors.TryGetValue(key, out var tile) && tile != null && tile.sprite != null)
                return tile;
            LoadImported();
            var imported = style >= 0 && style < importedFloors.Length && importedFloors[style].Count > 0
                ? importedFloors[style][Mathf.Abs(variant) % importedFloors[style].Count] : null;
            if (imported != null && decal < 0)
            {
                tile = SpriteTile(imported, Tile.ColliderType.None);
                floors[key] = tile;
                return tile;
            }

            Color32[] lo;
            var seed = variant * 31 + style * 7 + decal;
            if (imported != null)
            {
                lo = PixelsOf(imported, Unit, Unit / 2);
                if (lo == null || !StampImportedDecal(lo, decal, seed))
                {
                    // No imported decal of this kind: draw the code's on the imported floor at the drawing size.
                    var big = PixelArt.Enlarge(lo ?? FloorPixels(style, variant), lo != null ? Unit : TileW, lo != null ? Unit / 2 : TileH, TileW, TileH);
                    DrawDecal(big, (DecalKind)decal, seed);
                    lo = PixelArt.Downscale(big, TileW, TileH, Unit, Unit / 2);
                }
            }
            else
            {
                var pixels = FloorPixels(style, variant);
                if (decal >= 0)
                    DrawDecal(pixels, (DecalKind)decal, seed);
                lo = PixelArt.Downscale(pixels, TileW, TileH, Unit, Unit / 2);
            }
            // The diamond is cut again at the low size, a touch wide, so neighbouring tiles meet with no gap.
            PixelArt.CutDiamond(lo, Unit);
            PixelArt.Process(lo, Unit, Unit / 2, false);
            tile = MakeTile(lo, Unit, Unit / 2, new Vector2(0.5f, 0.5f), Tile.ColliderType.None, $"Floor {style}.{variant}.{decal}");
            floors[key] = tile;
            return tile;
        }

        /// <summary>A prop's tile: drawn over the floor, blocking like a wall.</summary>
        public static Tile Prop(PropKind kind)
        {
            if (props.TryGetValue(kind, out var tile) && tile != null && tile.sprite != null)
                return tile;
            LoadImported();
            if (importedProps.TryGetValue((int)kind, out var imported) && imported != null)
            {
                tile = SpriteTile(imported, Tile.ColliderType.Grid);
                props[kind] = tile;
                return tile;
            }
            var pixels = new Color32[PropSize * PropSize];
            DrawProp(pixels, kind);
            // The soft shadow drawn under a prop would outline as a dark blob: dropped before the outline.
            var lo = PixelArt.Downscale(pixels, PropSize, PropSize, Unit, Unit);
            PixelArt.Process(lo, Unit, Unit, true);
            tile = MakeTile(lo, Unit, Unit, new Vector2(0.5f, 0.25f), Tile.ColliderType.Grid, "Prop " + kind);
            props[kind] = tile;
            return tile;
        }

        static Tile MakeTile(Color32[] pixels, int w, int h, Vector2 pivot, Tile.ColliderType collider, string name)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            // Tiles that collide stay readable: the walls tilemap's collider reads their outline even for grid colliders
            // ("Sprite outline generation failed" once per prop and wall kind on entering the dungeon on iOS). They are
            // 40 px squares; the floors, which do not collide, let their pixels go.
            texture.Apply(false, collider == Tile.ColliderType.None);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            // A full-rect mesh and no physics shape: Unity would otherwise trace the sprite's outline from its pixels,
            // which the texture no longer keeps on the CPU ("Sprite outline generation failed" on iOS, 2026-09-30; the
            // editor keeps a readable copy, so it passed there). Tiles need neither.
            tile.sprite = Sprite.Create(texture, new Rect(0, 0, w, h), pivot, Unit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            tile.colliderType = collider;
            return tile;
        }

        /// <summary>A tile showing an imported sprite as it is.</summary>
        static Tile SpriteTile(Sprite sprite, Tile.ColliderType collider)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = sprite.name;
            tile.sprite = sprite;
            tile.colliderType = collider;
            return tile;
        }

        /// <summary>An imported sprite's pixels when it has the size asked for and its texture is readable, else null.</summary>
        static Color32[] PixelsOf(Sprite sprite, int w, int h)
        {
            var rect = sprite.rect;
            if ((int)rect.width != w || (int)rect.height != h || !sprite.texture.isReadable)
            {
                Debug.LogWarning($"Imported art '{sprite.name}' is {rect.width} x {rect.height} or unreadable; expected {w} x {h}. Run Tools > ARPG > Import Pixel Art.");
                return null;
            }
            var colors = sprite.texture.GetPixels((int)rect.x, (int)rect.y, w, h);
            var pixels = new Color32[colors.Length];
            for (var i = 0; i < colors.Length; i++)
                pixels[i] = colors[i];
            return pixels;
        }

        /// <summary>Lays an imported decal of the kind (a variant picked by the seed) over a floor's opaque pixels;
        /// false when none of that kind was imported.</summary>
        static bool StampImportedDecal(Color32[] floor, int decal, int seed)
        {
            if (!importedDecals.TryGetValue(decal, out var list) || list.Count == 0)
                return false;
            var mark = PixelsOf(list[Mathf.Abs(seed) % list.Count], Unit, Unit / 2);
            if (mark == null)
                return false;
            for (var i = 0; i < floor.Length; i++)
                if (floor[i].a > 0 && mark[i].a > 0)
                    floor[i] = new Color32(mark[i].r, mark[i].g, mark[i].b, 255);
            return true;
        }

        /// <summary>An iso block of stone: faces lit from the upper left, courses and staggered joints, a lighter top.</summary>
        static void DrawWall(Color32[] p, int frameH, int height)
        {
            var stone = new Color(0.36f, 0.34f, 0.33f);
            for (var x = 0; x < TileW; x++)
            {
                var dx = x + 0.5f - TileW * 0.5f;
                var ax = Mathf.Abs(dx);
                var bottom = ax / 2f;                 // the front corner's edge, down to the diamond's lowest point
                var topFrontEdge = height + ax / 2f;  // where the front faces meet the top face
                var topBack = height + TileH - ax / 2f;
                var left = dx < 0f;
                for (var y = 0; y < frameH; y++)
                {
                    if (y < bottom || y > topBack)
                        continue;
                    Color c;
                    if (y <= topFrontEdge)
                    {
                        // A front face: courses every 11 pixels along the slope, joints staggered course to course.
                        var along = y - bottom;
                        var course = Mathf.FloorToInt(along / 11f);
                        var joint = Mathf.Abs(((ax + course * 13) % 26) - 0.5f) < 1.2f;
                        var mortar = along % 11f < 1.3f || joint;
                        var f = (left ? 0.95f : 0.66f) * (0.9f + Hash(course, Mathf.FloorToInt((ax + course * 13) / 26f) + (left ? 0 : 50), 3) * 0.2f);
                        c = Shade(stone, mortar ? f * 0.62f : f);
                        if (y > topFrontEdge - 2f)
                            c = Shade(stone, 1.25f); // the lit lip along the top edge
                    }
                    else
                    {
                        c = Shade(stone, 1.12f + Fractal(x * 0.08f, y * 0.12f, 11) * 0.12f);
                    }
                    p[x + y * TileW] = c;
                }
            }
        }

        // ---------- noise ----------

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519u);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xffffff) / (float)0xffffff;
            }
        }

        static float Smooth(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            var a = Mathf.Lerp(Hash(x0, y0, seed), Hash(x0 + 1, y0, seed), fx);
            var b = Mathf.Lerp(Hash(x0, y0 + 1, seed), Hash(x0 + 1, y0 + 1, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        static float Fractal(float x, float y, int seed) =>
            Smooth(x, y, seed) * 0.55f + Smooth(x * 2.1f, y * 2.1f, seed + 17) * 0.3f + Smooth(x * 4.3f, y * 4.3f, seed + 41) * 0.15f;

        static Color32 Shade(Color c, float f) => new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);

        // ---------- floors ----------

        static readonly Color[] StyleColors =
        {
            new Color(0.40f, 0.37f, 0.33f), // flagstone
            new Color(0.33f, 0.22f, 0.19f), // dark brick
            new Color(0.34f, 0.27f, 0.19f), // packed earth
            new Color(0.30f, 0.34f, 0.29f), // mossy stone
        };

        /// <summary>Where a pixel of a floor tile lies along the two iso axes (0 to 1 each inside the diamond).</summary>
        static bool IsoCoords(int px, int py, out float a, out float b)
        {
            var dx = (px + 0.5f - TileW * 0.5f) / (TileW * 0.5f);
            var dy = (py + 0.5f - TileH * 0.5f) / (TileH * 0.5f);
            a = (dx + dy) * 0.5f + 0.5f;
            b = (dx - dy) * 0.5f + 0.5f;
            return Mathf.Abs(dx) + Mathf.Abs(dy) <= 1f;
        }

        static Color32[] FloorPixels(int style, int variant)
        {
            var pixels = new Color32[TileW * TileH];
            var baseColor = StyleColors[Mathf.Clamp(style, 0, StyleColors.Length - 1)];
            var seed = style * 101 + variant * 13;
            for (var py = 0; py < TileH; py++)
                for (var px = 0; px < TileW; px++)
                {
                    if (!IsoCoords(px, py, out var a, out var b))
                        continue;
                    // Broad, gentle patches: at pixel-art size, fine noise snaps back and forth between palette steps
                    // and reads as speckle (the town's earth on 2026-09-30).
                    var n = Smooth(a * 2.5f, b * 2.5f, seed);
                    var f = 0.93f + n * 0.12f;
                    var c = baseColor;
                    switch (style)
                    {
                        case 0: // Four worn slabs, each its own shade, seams between.
                        {
                            var slab = (a < 0.5f ? 0 : 1) + (b < 0.5f ? 0 : 2);
                            f *= 0.9f + Hash(slab, variant, seed) * 0.2f;
                            var seam = Mathf.Min(Mathf.Abs(a - 0.5f), Mathf.Abs(b - 0.5f), a, b, 1f - a, 1f - b);
                            if (seam < 0.025f)
                                f *= 0.55f;
                            else if (seam < 0.045f)
                                f *= 0.85f;
                            break;
                        }
                        case 1: // Bricks in running bond along one axis.
                        {
                            var row = Mathf.FloorToInt(b * 4f);
                            var along = a * 2f + (row % 2) * 0.5f;
                            f *= 0.88f + Hash(row, Mathf.FloorToInt(along), seed) * 0.24f;
                            var mortar = Mathf.Min(Mathf.Abs(b * 4f - Mathf.Round(b * 4f)) / 4f, Mathf.Abs(along - Mathf.Round(along)) / 2f);
                            if (mortar < 0.018f)
                                f *= 0.45f;
                            break;
                        }
                        case 2: // Earth with pebbles.
                        {
                            f = 0.9f + n * 0.16f;
                            if (Hash(px / 6, py / 3, seed + 5) > 0.95f)
                                c = new Color(0.46f, 0.43f, 0.38f);
                            break;
                        }
                        default: // Old stone with moss creeping in.
                        {
                            var seam = Mathf.Min(Mathf.Abs(a - 0.5f), Mathf.Abs(b - 0.5f), a, b, 1f - a, 1f - b);
                            if (seam < 0.03f)
                                f *= 0.6f;
                            var moss = Fractal(a * 3f + 10f, b * 3f, seed + 9);
                            if (moss > 0.58f)
                                c = Color.Lerp(c, new Color(0.24f, 0.36f, 0.18f), Mathf.Clamp01((moss - 0.58f) * 6f));
                            break;
                        }
                    }
                    // Variant 2 is cracked.
                    if (variant == 2 && Mathf.Abs(Fractal(a * 4f, b * 4f, seed + 77) - 0.5f) < 0.012f)
                        f *= 0.5f;
                    var edge = Mathf.Min(a, b, 1f - a, 1f - b);
                    if (edge < 0.02f)
                        f *= 0.8f;
                    pixels[px + py * TileW] = Shade(c, f);
                }
            return pixels;
        }

        static void DrawDecal(Color32[] pixels, DecalKind kind, int seed)
        {
            var random = new System.Random(seed);
            switch (kind)
            {
                case DecalKind.Cracks:
                    for (var k = 0; k < 2; k++)
                    {
                        float x = 40 + random.Next(48), y = 20 + random.Next(24);
                        var angle = random.Next(360) * Mathf.Deg2Rad;
                        for (var s = 0; s < 34; s++)
                        {
                            angle += (float)(random.NextDouble() - 0.5) * 0.9f;
                            x += Mathf.Cos(angle) * 1.2f;
                            y += Mathf.Sin(angle) * 0.6f;
                            Blend(pixels, TileW, TileH, (int)x, (int)y, new Color(0.08f, 0.07f, 0.06f, 0.8f));
                        }
                    }
                    break;
                case DecalKind.Blood:
                    Blot(pixels, 64, 32, 22, 9, new Color(0.35f, 0.03f, 0.03f, 0.85f), random);
                    for (var k = 0; k < 6; k++)
                        Blot(pixels, 44 + random.Next(40), 20 + random.Next(24), 3, 2, new Color(0.35f, 0.03f, 0.03f, 0.8f), random);
                    break;
                case DecalKind.Moss:
                    Blot(pixels, 64, 32, 26, 11, new Color(0.22f, 0.33f, 0.14f, 0.7f), random);
                    break;
                case DecalKind.Puddle:
                    Blot(pixels, 64, 32, 20, 8, new Color(0.06f, 0.08f, 0.1f, 0.85f), random);
                    Blot(pixels, 58, 35, 6, 2, new Color(0.35f, 0.4f, 0.45f, 0.5f), random);
                    break;
                case DecalKind.Rubble:
                    for (var k = 0; k < 7; k++)
                    {
                        int x = 40 + random.Next(48), y = 18 + random.Next(28), r = 2 + random.Next(3);
                        Blot(pixels, x + 1, y - 1, r, r * 0.6f, new Color(0f, 0f, 0f, 0.5f), random);
                        Blot(pixels, x, y, r, r * 0.7f, new Color(0.45f, 0.43f, 0.4f, 1f), random);
                    }
                    break;
                case DecalKind.Bones:
                    for (var k = 0; k < 4; k++)
                        Bone(pixels, TileW, TileH, 42 + random.Next(44), 20 + random.Next(24), random.Next(180) * Mathf.Deg2Rad, 10 + random.Next(8));
                    break;
                case DecalKind.Skull:
                    Bone(pixels, TileW, TileH, 52, 26, 0.3f, 14);
                    Skull(pixels, TileW, TileH, 70, 30, 1f);
                    break;
            }
        }

        // ---------- props ----------

        static void DrawProp(Color32[] p, PropKind kind)
        {
            const int cx = 64, gy = 32; // where the prop stands: the cell's middle
            var random = new System.Random((int)kind * 97 + 3);
            switch (kind)
            {
                case PropKind.Barrel:
                    Cylinder(p, cx, gy, 15, 40, new Color(0.42f, 0.27f, 0.15f), y => 1f + 0.18f * Mathf.Sin(Mathf.PI * y));
                    foreach (var band in new[] { 0.15f, 0.5f, 0.85f })
                        Ring(p, cx, gy + (int)(40 * band), 15f * (1f + 0.18f * Mathf.Sin(Mathf.PI * band)), new Color(0.18f, 0.17f, 0.17f));
                    Ellipse(p, cx, gy + 40, 15, 6, new Color(0.33f, 0.21f, 0.12f));
                    break;
                case PropKind.Crate:
                    IsoBox(p, cx, gy, 18, 28, new Color(0.45f, 0.32f, 0.18f), true);
                    break;
                case PropKind.Urn:
                    Cylinder(p, cx, gy, 13, 34, new Color(0.5f, 0.3f, 0.2f), y => y < 0.75f ? 0.7f + 0.5f * Mathf.Sin(Mathf.PI * y / 0.75f) : 0.5f + (y - 0.75f));
                    Ellipse(p, cx, gy + 34, 7, 3, new Color(0.12f, 0.08f, 0.06f));
                    break;
                case PropKind.BonePile:
                    Blot(p, cx, gy + 4, 20, 9, new Color(0.3f, 0.27f, 0.22f), random, PropSize, PropSize);
                    for (var k = 0; k < 9; k++)
                        Bone(p, PropSize, PropSize, cx - 16 + random.Next(32), gy + random.Next(14), random.Next(180) * Mathf.Deg2Rad, 12 + random.Next(8));
                    Skull(p, PropSize, PropSize, cx + 4, gy + 14, 1.4f);
                    break;
                case PropKind.Rubble:
                    for (var k = 0; k < 10; k++)
                    {
                        int x = cx - 18 + random.Next(36), y = gy - 4 + random.Next(16) + (k > 6 ? 10 : 0), r = 4 + random.Next(6);
                        Blot(p, x, y, r, r * 0.8f, Shade(new Color(0.42f, 0.4f, 0.37f), 0.8f + (float)random.NextDouble() * 0.35f), random, PropSize, PropSize);
                    }
                    break;
                case PropKind.BrokenColumn:
                    IsoBox(p, cx, gy, 20, 8, new Color(0.38f, 0.37f, 0.36f), false);
                    Cylinder(p, cx, gy + 8, 12, 42, new Color(0.5f, 0.49f, 0.47f), y => 1f);
                    // A jagged break across the top.
                    for (var x = -12; x <= 12; x++)
                    {
                        var cut = (int)(Hash(x, 0, 5) * 10);
                        for (var y = 0; y < cut; y++)
                            Set(p, PropSize, PropSize, cx + x, gy + 50 - y, new Color(0, 0, 0, 0));
                    }
                    break;
                case PropKind.Sarcophagus:
                    IsoBox(p, cx, gy, 26, 18, new Color(0.4f, 0.39f, 0.37f), false);
                    // A cross carved into the lid.
                    for (var t = -7; t <= 7; t++)
                    {
                        Set(p, PropSize, PropSize, cx + t * 2, gy + 18 + 13 + t, new Color(0.2f, 0.2f, 0.2f));
                        if (Mathf.Abs(t) <= 3)
                            Set(p, PropSize, PropSize, cx + 4 + t * 2, gy + 18 + 15 - t, new Color(0.2f, 0.2f, 0.2f));
                    }
                    break;
                case PropKind.Brazier:
                    for (var leg = -1; leg <= 1; leg++)
                        for (var y = 0; y < 26; y++)
                            Set(p, PropSize, PropSize, cx + leg * (10 - y / 4), gy + y, new Color(0.15f, 0.14f, 0.14f));
                    Cylinder(p, cx, gy + 24, 14, 8, new Color(0.22f, 0.2f, 0.19f), y => 0.7f + 0.3f * y);
                    Flame(p, cx, gy + 34, 12, 20, random);
                    break;
                case PropKind.Candles:
                    for (var k = 0; k < 5; k++)
                    {
                        int x = cx - 14 + k * 7, h = 10 + random.Next(16), y0 = gy + random.Next(6) - 2;
                        Cylinder(p, x, y0, 2, h, new Color(0.85f, 0.8f, 0.66f), y => 1f);
                        Flame(p, x, y0 + h + 1, 2, 6, random);
                    }
                    break;
            }
        }

        // ---------- drawing helpers (y up, 0 at the bottom row) ----------

        static void Set(Color32[] p, int w, int h, int x, int y, Color c)
        {
            if (x >= 0 && y >= 0 && x < w && y < h)
                p[x + y * w] = c;
        }

        static void Blend(Color32[] p, int w, int h, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h)
                return;
            var under = (Color)p[x + y * w];
            if (under.a <= 0f)
                return; // decals stay on the tile
            p[x + y * w] = Color.Lerp(under, new Color(c.r, c.g, c.b, under.a), c.a);
        }

        static void Blot(Color32[] p, int cx, int cy, float rx, float ry, Color c, System.Random random, int w = TileW, int h = TileH)
        {
            var seed = random.Next(1000);
            for (var y = (int)(cy - ry - 2); y <= cy + ry + 2; y++)
                for (var x = (int)(cx - rx - 2); x <= cx + rx + 2; x++)
                {
                    var d = ((x - cx) / rx) * ((x - cx) / rx) + ((y - cy) / ry) * ((y - cy) / ry);
                    var wobble = 0.75f + Smooth(x * 0.25f, y * 0.25f, seed) * 0.5f;
                    if (d > wobble)
                        continue;
                    if (w == TileW && h == TileH)
                        Blend(p, w, h, x, y, c);
                    else if (x >= 0 && y >= 0 && x < w && y < h)
                    {
                        var under = (Color)p[x + y * w];
                        p[x + y * w] = under.a <= 0f ? c : Color.Lerp(under, new Color(c.r, c.g, c.b, 1f), c.a);
                    }
                }
        }

        static void Ellipse(Color32[] p, int cx, int cy, float rx, float ry, Color c)
        {
            for (var y = (int)(cy - ry); y <= cy + ry; y++)
                for (var x = (int)(cx - rx); x <= cx + rx; x++)
                    if (((x - cx) / rx) * ((x - cx) / rx) + ((y - cy) / ry) * ((y - cy) / ry) <= 1f)
                        Set(p, PropSize, PropSize, x, y, c);
        }

        /// <summary>An upright round body standing at (cx, gy): radius by height fraction, lit from the upper left.</summary>
        static void Cylinder(Color32[] p, int cx, int gy, float radius, int height, Color c, System.Func<float, float> profile)
        {
            for (var y = 0; y <= height; y++)
            {
                var r = radius * profile(y / (float)height);
                for (var x = -Mathf.CeilToInt(r); x <= r; x++)
                {
                    var across = x / Mathf.Max(1f, r);
                    var light = 0.55f + 0.6f * (1f - Mathf.Abs(across + 0.35f));
                    // The body's bottom follows the ellipse of its base.
                    var drop = (int)(Mathf.Sqrt(Mathf.Max(0f, 1f - across * across)) * r * 0.35f);
                    Set(p, PropSize, PropSize, cx + x, gy + y - drop, Shade(c, light));
                }
            }
        }

        static void Ring(Color32[] p, int cx, int y, float r, Color c)
        {
            for (var x = -Mathf.CeilToInt(r); x <= r; x++)
            {
                var across = x / Mathf.Max(1f, r);
                var drop = (int)(Mathf.Sqrt(Mathf.Max(0f, 1f - across * across)) * r * 0.35f);
                Set(p, PropSize, PropSize, cx + x, y - drop, c);
                Set(p, PropSize, PropSize, cx + x, y - drop + 1, c);
            }
        }

        /// <summary>An iso box standing on the cell's middle: half-width in pixels (the footprint is twice as wide as
        /// deep, like a tile), height, and optionally planks.</summary>
        static void IsoBox(Color32[] p, int cx, int gy, int half, int height, Color c, bool planks)
        {
            var depth = half / 2;
            for (var x = -half; x <= half; x++)
            {
                // Bottom edge of the front faces: a V down to the front corner.
                var bottom = gy - depth + Mathf.Abs(x) / 2;
                var left = x < 0;
                for (var y = bottom; y < bottom + height; y++)
                {
                    var f = left ? 0.95f : 0.7f;
                    if (planks && ((y - bottom) % 7 == 0 || Mathf.Abs(x) == half || x == 0))
                        f *= 0.6f;
                    Set(p, PropSize, PropSize, cx + x, y, Shade(c, f));
                }
                // The top face: a diamond above.
                var topLow = bottom + height;
                var topHigh = gy + depth - Mathf.Abs(x) / 2 + height;
                for (var y = topLow; y <= topHigh; y++)
                    Set(p, PropSize, PropSize, cx + x, y, Shade(c, planks && (x + y) % 8 == 0 ? 0.8f : 1.15f));
            }
        }

        static void Bone(Color32[] p, int w, int h, int x, int y, float angle, int length)
        {
            var c = new Color(0.82f, 0.78f, 0.68f, 1f);
            for (var t = -length / 2; t <= length / 2; t++)
            {
                var bx = x + (int)(Mathf.Cos(angle) * t);
                var by = y + (int)(Mathf.Sin(angle) * t * 0.5f);
                if (w == TileW && h == TileH)
                {
                    Blend(p, w, h, bx, by, c);
                    Blend(p, w, h, bx, by - 1, new Color(0.1f, 0.1f, 0.1f, 0.5f));
                }
                else
                    Set(p, w, h, bx, by, c);
            }
            foreach (var end in new[] { -length / 2, length / 2 })
                for (var d = -1; d <= 1; d++)
                {
                    var ex = x + (int)(Mathf.Cos(angle) * end);
                    var ey = y + (int)(Mathf.Sin(angle) * end * 0.5f) + d;
                    if (w == TileW && h == TileH)
                        Blend(p, w, h, ex, ey, c);
                    else
                        Set(p, w, h, ex, ey, c);
                }
        }

        static void Skull(Color32[] p, int w, int h, int x, int y, float scale)
        {
            var bone = new Color(0.86f, 0.82f, 0.72f, 1f);
            var dark = new Color(0.08f, 0.06f, 0.05f, 1f);
            var r = 4f * scale;
            for (var dy = (int)-r; dy <= r; dy++)
                for (var dx = (int)-r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r * r)
                    {
                        if (w == TileW && h == TileH)
                            Blend(p, w, h, x + dx, y + dy, bone);
                        else
                            Set(p, w, h, x + dx, y + dy, bone);
                    }
            foreach (var ex in new[] { -1.6f, 1.6f })
            {
                var px = x + (int)(ex * scale);
                var py = y + (int)(0.5f * scale);
                if (w == TileW && h == TileH)
                    Blend(p, w, h, px, py, dark);
                else
                    Set(p, w, h, px, py, dark);
            }
        }

        static void Flame(Color32[] p, int cx, int y0, float radius, int height, System.Random random)
        {
            for (var y = 0; y < height; y++)
            {
                var t = y / (float)height;
                var r = radius * (1f - t) * (0.8f + 0.4f * (float)random.NextDouble());
                for (var x = -Mathf.CeilToInt(r); x <= r; x++)
                {
                    var hot = 1f - Mathf.Abs(x) / Mathf.Max(1f, r);
                    var c = Color.Lerp(new Color(0.85f, 0.25f, 0.05f), new Color(1f, 0.85f, 0.4f), hot * (1f - t));
                    Set(p, PropSize, PropSize, cx + x, y0 + y, c);
                }
            }
        }
    }
}
