using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>
    /// Paints the ground of the town and the dungeon when a scene loads (the owner, 2026-10-04: dirt with grass and paths
    /// in town, dirt with cobbles showing through below, one ground across every room, after Diablo 2's first act).
    /// Every floor cell gets its own tile, drawn from four seamless textures in Resources/Art/Ground (dirt, cobble,
    /// grass, path: ArtSource/tools/props/ground_sources.py), sampled at the cell's ground-space position and chosen
    /// pixel by pixel by <see cref="GroundRules"/>, so the ground runs across cells and rooms with no seams and no
    /// repeat. The tiles share atlas textures, freed when the next scene paints. Returns false when the textures are
    /// missing or not readable, and the caller falls back to <see cref="DungeonArt"/>'s floors. The forest's ground is on
    /// the screen's pixel grid (2026-11-04, the owner chose "Move to screen grid"): its tiles are drawn at twice
    /// <see cref="ScreenUnit"/> a unit, so each screen pixel is one of their pixels, and its grass and path pictures are
    /// pixel art on that grid, sampled at the screen position of each pixel; the other themes keep 40 a unit.
    /// </summary>
    public static class GroundPainter
    {
        public const string Folder = "Art/Ground";
        // The old textures' scale: 40 of their pixels a unit of ground.
        const int SampleUnit = PixelArt.PixelsPerUnit;
        /// <summary>The pixels a unit the world is rendered at on every phone (<see cref="RenderResolution.RenderedUnit"/>
        /// with no extra pixelation: 40 over the camera's 1.5).</summary>
        public const int ScreenUnit = 27;
        /// <summary>The forest's tiles: twice the screen's pixels a unit, so the 1 x 0.5 cell is a whole 54 x 27.</summary>
        public const int ForestTileUnit = 2 * ScreenUnit;
        // The tile size of the paint in progress (40 a unit, or the forest's 54), set before the worker threads start.
        static int tileW = SampleUnit, tileH = SampleUnit / 2;
        // Atlas slots leave a clear pixel on each side, so point sampling never picks up a neighbour.
        static int SlotW => tileW + 2;
        static int SlotH => tileH + 2;
        const int AtlasSize = 2048;
        const int SourceSize = 512;

        static Color32[][] sources;
        static bool triedLoading;
        // The crypt's floor as one drawn picture (2026-10-26, the owner's drawn assets), any square size, repeating at
        // 40 pixels a cell; null without it, and the crypt mixes cobble and dirt as before.
        public const string CryptFloorName = "crypt_floor";
        static Color32[] cryptFloor;
        static int cryptFloorSize;
        // The forest's grass and worn paths as drawn pictures (2026-10-27, the owner's Gemini ground), the same way; null
        // without them, and the forest darkens the grass and path textures as before.
        public const string ForestGrassName = "forest_grass";
        public const string ForestPathName = "forest_path";
        static Color32[] forestGrass, forestPath;
        static int forestGrassSize, forestPathSize;
        static float forestGrassMean;
        static readonly List<Object> made = new List<Object>();
        static readonly List<Vector3Int> positions = new List<Vector3Int>();
        static readonly List<TileBase> tiles = new List<TileBase>();
        static Color32[] cellPixels = new Color32[SampleUnit * SampleUnit / 2];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            sources = null;
            triedLoading = false;
            cryptFloor = null;
            forestGrass = null;
            forestPath = null;
            made.Clear();
        }

        /// <summary>Whether the four source textures are in and readable.</summary>
        public static bool Available => Load();

        /// <summary>The dungeon: dirt with cobbles, every listed cell, decals stamped on, each cell's shade
        /// (<see cref="LightingRules.GroundShade"/>) painted in, so the caller need not tint the tiles one by one.</summary>
        public static bool PaintDungeon(Tilemap ground, List<Vector2Int> cells, IReadOnlyDictionary<Vector2Int, DecalKind> decals) =>
            PaintDungeon(ground, cells, decals, DungeonTheme.Crypt, null, null);

        /// <summary>The dungeon in its theme (2026-10-10): the crypt paved, the caves bare earth; raised terrace cells
        /// painted lighter, so the terrace reads as higher ground, and a burial pit's darker (2026-10-20).</summary>
        public static bool PaintDungeon(Tilemap ground, List<Vector2Int> cells, IReadOnlyDictionary<Vector2Int, DecalKind> decals,
            DungeonTheme theme, HashSet<Vector2Int> raised, HashSet<Vector2Int> sunken)
        {
            dungeonTheme = theme;
            raisedCells = raised;
            sunkenCells = sunken;
            return Paint(ground, cells, false, decals, true);
        }

        // Read by the worker threads while a dungeon paints; set before and never changed during.
        static DungeonTheme dungeonTheme;
        static HashSet<Vector2Int> raisedCells;
        static HashSet<Vector2Int> sunkenCells;

        /// <summary>How much darker a burial pit's floor is drawn (2026-10-20).</summary>
        public const float SunkenShade = 0.62f;

        /// <summary>The town: every cell the scene's ground has, a paved square and roads, mud and puddles.</summary>
        public static bool PaintTown(Tilemap ground)
        {
            var cells = new List<Vector2Int>();
            foreach (var cell in ground.cellBounds.allPositionsWithin)
                if (ground.HasTile(cell))
                    cells.Add(new Vector2Int(cell.x, cell.y));
            return Paint(ground, cells, true, null, false);
        }

        // Cells are drawn on worker threads (the road's long halls, 2026-10-06: about 10,000 cells took 1.2 s on one
        // thread in the editor). Every cell has its own atlas slot, so the threads never write the same pixels; the
        // rules are pure (GroundRules, LightingRules.GroundShade). Decals are stamped afterwards on the main thread.
        static bool Paint(Tilemap ground, List<Vector2Int> cells, bool town, IReadOnlyDictionary<Vector2Int, DecalKind> decals, bool shade)
        {
            if (ground == null || !Load())
                return false;
            // The floor's tint (GradeRules.FloorTint; none since the darker floor of 2026-10-19 was too dark).
            ground.color = GradeRules.FloorTint;
            LoadTimer.Mark("ground: textures");
            tileW = ForestOnScreenGrid(town) ? ForestTileUnit : SampleUnit;
            tileH = tileW / 2;
            if (cellPixels.Length != tileW * tileH)
                cellPixels = new Color32[tileW * tileH];
            Release();
            positions.Clear();
            tiles.Clear();

            var columns = AtlasSize / SlotW;
            var rows = AtlasSize / SlotH;
            var perAtlas = columns * rows;
            var atlasCount = (cells.Count + perAtlas - 1) / perAtlas;
            var atlases = new Color32[atlasCount][];
            var heights = new int[atlasCount];
            for (var a = 0; a < atlasCount; a++)
            {
                var left = cells.Count - a * perAtlas;
                heights[a] = Mathf.NextPowerOfTwo(Mathf.Min(rows, (left + columns - 1) / columns) * SlotH);
                atlases[a] = new Color32[AtlasSize * heights[a]];
            }

            System.Threading.Tasks.Parallel.For(0, cells.Count, () => new Color32[tileW * tileH], (n, _, buffer) =>
            {
                var cell = cells[n];
                DrawCell(cell, town, buffer);
                if (shade && (decals == null || !decals.ContainsKey(cell)))
                    Shade(buffer, cell);
                Put(buffer, atlases[n / perAtlas], n % perAtlas, columns);
                return buffer;
            }, _ => { });
            LoadTimer.Mark("ground: draw");

            if (decals != null)
                for (var n = 0; n < cells.Count; n++)
                {
                    if (!decals.TryGetValue(cells[n], out var decal))
                        continue;
                    DrawCell(cells[n], town, cellPixels);
                    DungeonArt.StampDecal(cellPixels, tileW, tileH, (int)decal, cells[n].x * 31 + cells[n].y * 7, cells[n]);
                    if (shade)
                        Shade(cellPixels, cells[n]);
                    Put(cellPixels, atlases[n / perAtlas], n % perAtlas, columns);
                }

            LoadTimer.Mark("ground: decals");
            var pending = new List<(Vector2Int cell, int slot)>();
            for (var a = 0; a < atlasCount; a++)
            {
                pending.Clear();
                for (var n = a * perAtlas; n < Mathf.Min(cells.Count, (a + 1) * perAtlas); n++)
                    pending.Add((cells[n], n % perAtlas));
                Flush(atlases[a], heights[a], pending, columns);
            }

            LoadTimer.Mark("ground: atlases, tiles");
            ground.ClearAllTiles();
            ground.SetTiles(positions.ToArray(), tiles.ToArray());
            positions.Clear();
            tiles.Clear();
            LoadTimer.Mark("ground: set tiles");
            return true;
        }

        static void Flush(Color32[] atlas, int height, List<(Vector2Int cell, int slot)> pending, int columns)
        {
            var texture = new Texture2D(AtlasSize, height, TextureFormat.RGBA32, false)
            {
                name = "Ground Atlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(atlas);
            texture.Apply(false, true);
            made.Add(texture);
            foreach (var (cell, slot) in pending)
            {
                var rect = new Rect((slot % columns) * SlotW + 1, (slot / columns) * SlotH + 1, tileW, tileH);
                // A full-rect mesh and no physics shape: the atlas keeps no CPU copy (see DungeonArt.MakeTile).
                var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), tileW, 0, SpriteMeshType.FullRect, Vector4.zero, false);
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                made.Add(sprite);
                made.Add(tile);
                positions.Add(new Vector3Int(cell.x, cell.y, 0));
                tiles.Add(tile);
            }
        }

        static void Put(Color32[] pixels, Color32[] atlas, int slot, int columns)
        {
            var ox = (slot % columns) * SlotW + 1;
            var oy = (slot / columns) * SlotH + 1;
            for (var y = 0; y < tileH; y++)
                System.Array.Copy(pixels, y * tileW, atlas, ox + (oy + y) * AtlasSize, tileW);
        }

        // The cell's tint painted into its pixels, as the tile's colour would have tinted them.
        static void Shade(Color32[] pixels, Vector2Int cell)
        {
            var tint = LightingRules.GroundShade(cell);
            if (raisedCells != null && raisedCells.Contains(cell))
                tint *= 1.25f;
            else if (sunkenCells != null && sunkenCells.Contains(cell))
                tint *= SunkenShade;
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                pixels[i] = new Color32((byte)Mathf.Min(255f, c.r * tint.r), (byte)Mathf.Min(255f, c.g * tint.g), (byte)Mathf.Min(255f, c.b * tint.b), c.a);
            }
        }

        /// <summary>One cell's diamond into a buffer, a touch wide (as PixelArt.CutDiamond), clear outside.</summary>
        static void DrawCell(Vector2Int cell, bool town, Color32[] cellPixels)
        {
            var centre = IsoMath.CellToGround(cell);
            // A cell far from every path needs no per-pixel distance.
            var nearPath = town && NearestPath(centre) < GroundRules.PathHalfWidth * 1.25f + 1.2f;
            var dirt = sources[0];
            var cobble = sources[1];
            var grass = sources[2];
            var path = sources[3];
            // The forest's slow light drift, from the cell's four corners (2026-11-03, the owner: "Loading times are getting
            // long"): two noises a pixel were a third of the ground's drawing, and the drift is over 2 and 6 cells.
            var forestFloor = !town && DungeonRules.Forest && dungeonTheme == DungeonTheme.Cave && forestGrass != null && forestPath != null;
            float s00 = 0f, s10 = 0f, s01 = 0f, s11 = 0f;
            if (forestFloor)
            {
                s00 = GroundRules.ForestShade(centre + new Vector2(-0.5f, -0.5f));
                s10 = GroundRules.ForestShade(centre + new Vector2(0.5f, -0.5f));
                s01 = GroundRules.ForestShade(centre + new Vector2(-0.5f, 0.5f));
                s11 = GroundRules.ForestShade(centre + new Vector2(0.5f, 0.5f));
            }
            for (var y = 0; y < tileH; y++)
                for (var x = 0; x < tileW; x++)
                {
                    var i = x + y * tileW;
                    var dx = Mathf.Abs(x + 0.5f - tileW * 0.5f) / (tileW * 0.5f);
                    var dy = Mathf.Abs(y + 0.5f - tileH * 0.5f) / (tileH * 0.5f);
                    if (dx + dy > 1.06f)
                    {
                        cellPixels[i] = default;
                        continue;
                    }
                    // The pixel's world offset from the cell's middle, then ground space (y doubled).
                    var g = new Vector2(centre.x + (x + 0.5f - tileW * 0.5f) / tileW, centre.y + 2f * (y + 0.5f - tileH * 0.5f) / tileW);
                    if (!town && dungeonTheme == DungeonTheme.Crypt && cryptFloor != null)
                    {
                        var f = cryptFloor[Wrap(Mathf.FloorToInt(g.x * SampleUnit), cryptFloorSize) + Wrap(Mathf.FloorToInt(g.y * SampleUnit), cryptFloorSize) * cryptFloorSize];
                        var k = GroundRules.CryptFloorShade(g);
                        cellPixels[i] = new Color32((byte)Mathf.Min(255f, f.r * k), (byte)Mathf.Min(255f, f.g * k), (byte)Mathf.Min(255f, f.b * k), 255);
                        continue;
                    }
                    if (forestFloor)
                    {
                        // The grass and the path frayed into each other along their edge (2026-10-27), the amount worked
                        // out once a pixel (it was twice: once to pick the layer). The pictures are read on the screen's
                        // grid: the pixel's world position (ground y halved) at the screen's pixels a unit.
                        int gx = Mathf.FloorToInt(g.x * ScreenUnit), gy = Mathf.FloorToInt(g.y * 0.5f * ScreenUnit);
                        var blade = forestGrass[Wrap(gx, forestGrassSize) + Wrap(gy, forestGrassSize) * forestGrassSize];
                        var earth = forestPath[Wrap(gx, forestPathSize) + Wrap(gy, forestPathSize) * forestPathSize];
                        var amount = GroundRules.ForestGrassAmount(g);
                        var share = GroundRules.ForestGrassShare(amount, Light(blade), forestGrassMean, GroundRules.ForestClump(gx, gy));
                        float u = Mathf.Clamp01(g.x - centre.x + 0.5f), v = Mathf.Clamp01(g.y - centre.y + 0.5f);
                        var drift = Mathf.Lerp(Mathf.Lerp(s00, s10, u), Mathf.Lerp(s01, s11, u), v);
                        cellPixels[i] = share > 0f ? Tone(blade, drift) : Scale(earth, drift * GroundRules.PathRim(amount));
                        continue;
                    }
                    var s = Wrap(Mathf.FloorToInt(g.x * SampleUnit)) + Wrap(Mathf.FloorToInt(g.y * SampleUnit)) * SourceSize;
                    GroundLayer layer;
                    if (town)
                    {
                        var cover = nearPath ? GroundRules.PathCover(g, GroundRules.TownPaths) : 0f;
                        layer = GroundRules.TownPick(g, cover, cobble[s].a / 255f);
                    }
                    else if (DungeonRules.Forest && dungeonTheme == DungeonTheme.Cave)
                        layer = GroundRules.ForestPick(g);
                    else
                        layer = GroundRules.DungeonPick(g, cobble[s].a / 255f, dungeonTheme);
                    var c = layer switch
                    {
                        GroundLayer.Cobble => cobble[s],
                        GroundLayer.Grass => grass[s],
                        GroundLayer.Path => path[s],
                        _ => dirt[s],
                    };
                    if (!town && dungeonTheme == DungeonTheme.Prison)
                    {
                        // Damp, dark stone and black mud (2026-10-12).
                        c = layer == GroundLayer.Dirt
                            ? new Color32((byte)(c.r * 0.55f), (byte)(c.g * 0.52f), (byte)(c.b * 0.5f), 255)
                            : new Color32((byte)(c.r * 0.74f), (byte)(c.g * 0.72f), (byte)(c.b * 0.7f), 255);
                    }
                    if (!town && dungeonTheme == DungeonTheme.Library)
                    {
                        // Ash grey over the dirt, smoke-darkened stone (2026-10-11).
                        if (layer == GroundLayer.Dirt)
                        {
                            var grey = (c.r * 0.3f + c.g * 0.5f + c.b * 0.2f) * 0.9f;
                            c = new Color32((byte)grey, (byte)(grey * 0.97f), (byte)(grey * 0.94f), 255);
                        }
                        else
                            c = new Color32((byte)(c.r * 0.8f), (byte)(c.g * 0.76f), (byte)(c.b * 0.74f), 255);
                    }
                    var forest = !town && DungeonRules.Forest && dungeonTheme == DungeonTheme.Cave;
                    if (forest && forestGrass != null && forestPath != null)
                    {
                        // The grass and the path frayed into each other along their edge (2026-10-27), on the screen's grid.
                        int gx = Mathf.FloorToInt(g.x * ScreenUnit), gy = Mathf.FloorToInt(g.y * 0.5f * ScreenUnit);
                        var blade = forestGrass[Wrap(gx, forestGrassSize) + Wrap(gy, forestGrassSize) * forestGrassSize];
                        var earth = forestPath[Wrap(gx, forestPathSize) + Wrap(gy, forestPathSize) * forestPathSize];
                        var amount = GroundRules.ForestGrassAmount(g);
                        var share = GroundRules.ForestGrassShare(amount, Light(blade), forestGrassMean, GroundRules.ForestClump(gx, gy));
                        // The grass toned to the trees' gloom, the earth darker at its trodden rim, both under a slow drift.
                        var shade = GroundRules.ForestShade(g);
                        cellPixels[i] = share > 0f ? Tone(blade, shade) : Scale(earth, shade * GroundRules.PathRim(amount));
                        continue;
                    }
                    var picture = !forest ? null : layer == GroundLayer.Grass ? forestGrass : forestPath;
                    if (picture != null)
                    {
                        var size = layer == GroundLayer.Grass ? forestGrassSize : forestPathSize;
                        cellPixels[i] = picture[Wrap(Mathf.FloorToInt(g.x * ScreenUnit), size) + Wrap(Mathf.FloorToInt(g.y * 0.5f * ScreenUnit), size) * size];
                        continue;
                    }
                    if (forest)
                    {
                        // The forest floor (2026-10-26): dark grass and trodden earth.
                        c = layer == GroundLayer.Grass
                            ? new Color32((byte)(c.r * 0.55f + 6), (byte)(c.g * 0.6f + 6), (byte)(c.b * 0.5f + 4), 255)
                            : new Color32((byte)(c.r * 0.7f), (byte)(c.g * 0.66f), (byte)(c.b * 0.6f), 255);
                    }
                    if (town)
                    {
                        // The dark town (2026-10-11): wet, dark mud and stone; puddles the mud darkened toward cold water.
                        c = layer == GroundLayer.Puddle
                            ? new Color32((byte)(c.r * 0.42f + 16), (byte)(c.g * 0.44f + 19), (byte)(c.b * 0.46f + 25), 255)
                            : layer == GroundLayer.Cobble
                                ? new Color32((byte)(c.r * 0.78f), (byte)(c.g * 0.78f), (byte)(c.b * 0.8f), 255)
                                // Grass dulled toward the mud (2026-10-12), so it reads as dark tufts, not a lawn.
                                : layer == GroundLayer.Grass
                                    ? new Color32((byte)(c.r * 0.5f + 8), (byte)(c.g * 0.52f + 6), (byte)(c.b * 0.45f + 4), 255)
                                    : new Color32((byte)(c.r * 0.62f), (byte)(c.g * 0.6f), (byte)(c.b * 0.58f), 255);
                    }
                    cellPixels[i] = new Color32(c.r, c.g, c.b, 255);
                }
        }

        // Whether this paint is the forest drawn on the screen's grid: the forest's ground with its pictures in.
        static bool ForestOnScreenGrid(bool town) =>
            !town && DungeonRules.Forest && dungeonTheme == DungeonTheme.Cave && forestGrass != null && forestPath != null;

        static float NearestPath(Vector2 ground)
        {
            var paths = GroundRules.TownPaths;
            var best = float.MaxValue;
            for (var i = 0; i + 1 < paths.Length; i += 2)
                best = Mathf.Min(best, GroundRules.SegmentDistance(ground, IsoMath.CellToGround(paths[i]), IsoMath.CellToGround(paths[i + 1])));
            return best;
        }

        static int Wrap(int v) => Wrap(v, SourceSize);

        static int Wrap(int v, int size) => ((v % size) + size) % size;

        static bool Load()
        {
            if (sources != null)
                return true;
            if (triedLoading)
                return false;
            triedLoading = true;
            var names = new[] { "dirt", "cobble", "grass", "path" };
            var loaded = new Color32[names.Length][];
            for (var i = 0; i < names.Length; i++)
            {
                var texture = Resources.Load<Texture2D>(Folder + "/" + names[i]);
                if (texture == null || !texture.isReadable || texture.width != SourceSize || texture.height != SourceSize)
                {
                    Debug.LogWarning($"Ground texture {names[i]} is missing, not readable or not {SourceSize} square; the old floors are used.");
                    return false;
                }
                loaded[i] = texture.GetPixels32();
                Resources.UnloadAsset(texture);
            }
            sources = loaded;
            cryptFloor = Picture(CryptFloorName, out cryptFloorSize);
            forestGrass = Picture(ForestGrassName, out forestGrassSize);
            forestPath = Picture(ForestPathName, out forestPathSize);
            forestGrassMean = 0f;
            if (forestGrass != null)
            {
                foreach (var c in forestGrass)
                    forestGrassMean += Light(c);
                forestGrassMean /= forestGrass.Length;
            }
            return true;
        }

        /// <summary>The drawn grass toned to the forest's gloom (2026-10-31): a quarter of its colour gone, a little darker.</summary>
        static Color32 Tone(Color32 c, float shade)
        {
            var grey = (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f);
            var k = 0.86f * shade;
            return new Color32((byte)Mathf.Min(255f, Mathf.Lerp(c.r, grey, 0.25f) * k), (byte)Mathf.Min(255f, Mathf.Lerp(c.g, grey, 0.25f) * k),
                (byte)Mathf.Min(255f, Mathf.Lerp(c.b, grey, 0.25f) * k), 255);
        }

        static Color32 Scale(Color32 c, float k) =>
            new Color32((byte)Mathf.Min(255f, c.r * k), (byte)Mathf.Min(255f, c.g * k), (byte)Mathf.Min(255f, c.b * k), 255);

        static float Light(Color32 c) => (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f) / 255f;

        /// <summary>A drawn ground picture's pixels and size when it is in, readable and square, else null.</summary>
        static Color32[] Picture(string name, out int size)
        {
            size = 0;
            var texture = Resources.Load<Texture2D>(Folder + "/" + name);
            if (texture == null || !texture.isReadable || texture.width != texture.height)
                return null;
            var pixels = texture.GetPixels32();
            size = texture.width;
            Resources.UnloadAsset(texture);
            return pixels;
        }

        /// <summary>Frees the last scene's atlases, sprites and tiles.</summary>
        static void Release()
        {
            foreach (var thing in made)
                if (thing != null)
                    Object.Destroy(thing);
            made.Clear();
        }
    }
}
