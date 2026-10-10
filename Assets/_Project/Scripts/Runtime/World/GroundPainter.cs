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
    /// missing or not readable, and the caller falls back to <see cref="DungeonArt"/>'s floors.
    /// </summary>
    public static class GroundPainter
    {
        public const string Folder = "Art/Ground";
        const int Unit = PixelArt.PixelsPerUnit;
        const int TileH = Unit / 2;
        // Atlas slots leave a clear pixel on each side, so point sampling never picks up a neighbour.
        const int SlotW = Unit + 2;
        const int SlotH = TileH + 2;
        const int AtlasSize = 2048;
        const int SourceSize = 512;

        static Color32[][] sources;
        static bool triedLoading;
        static readonly List<Object> made = new List<Object>();
        static readonly List<Vector3Int> positions = new List<Vector3Int>();
        static readonly List<TileBase> tiles = new List<TileBase>();
        static readonly Color32[] cellPixels = new Color32[Unit * TileH];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            sources = null;
            triedLoading = false;
            made.Clear();
        }

        /// <summary>Whether the four source textures are in and readable.</summary>
        public static bool Available => Load();

        /// <summary>The dungeon: dirt with cobbles, every listed cell, decals stamped on, each cell's shade
        /// (<see cref="LightingRules.GroundShade"/>) painted in, so the caller need not tint the tiles one by one.</summary>
        public static bool PaintDungeon(Tilemap ground, List<Vector2Int> cells, IReadOnlyDictionary<Vector2Int, DecalKind> decals) =>
            PaintDungeon(ground, cells, decals, DungeonTheme.Crypt, null);

        /// <summary>The dungeon in its theme (2026-10-10): the crypt paved, the caves bare earth; raised terrace cells
        /// painted lighter, so the terrace reads as higher ground.</summary>
        public static bool PaintDungeon(Tilemap ground, List<Vector2Int> cells, IReadOnlyDictionary<Vector2Int, DecalKind> decals,
            DungeonTheme theme, HashSet<Vector2Int> raised)
        {
            dungeonTheme = theme;
            raisedCells = raised;
            return Paint(ground, cells, false, decals, true);
        }

        // Read by the worker threads while a dungeon paints; set before and never changed during.
        static DungeonTheme dungeonTheme;
        static HashSet<Vector2Int> raisedCells;

        /// <summary>The town: every cell the scene's ground has, dirt with grass spots and worn paths.</summary>
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
            Release();
            positions.Clear();
            tiles.Clear();

            const int columns = AtlasSize / SlotW;
            const int rows = AtlasSize / SlotH;
            const int perAtlas = columns * rows;
            var atlasCount = (cells.Count + perAtlas - 1) / perAtlas;
            var atlases = new Color32[atlasCount][];
            var heights = new int[atlasCount];
            for (var a = 0; a < atlasCount; a++)
            {
                var left = cells.Count - a * perAtlas;
                heights[a] = Mathf.NextPowerOfTwo(Mathf.Min(rows, (left + columns - 1) / columns) * SlotH);
                atlases[a] = new Color32[AtlasSize * heights[a]];
            }

            System.Threading.Tasks.Parallel.For(0, cells.Count, () => new Color32[Unit * TileH], (n, _, buffer) =>
            {
                var cell = cells[n];
                DrawCell(cell, town, buffer);
                if (shade && (decals == null || !decals.ContainsKey(cell)))
                    Shade(buffer, cell);
                Put(buffer, atlases[n / perAtlas], n % perAtlas, columns);
                return buffer;
            }, _ => { });

            if (decals != null)
                for (var n = 0; n < cells.Count; n++)
                {
                    if (!decals.TryGetValue(cells[n], out var decal))
                        continue;
                    DrawCell(cells[n], town, cellPixels);
                    DungeonArt.StampDecal(cellPixels, (int)decal, cells[n].x * 31 + cells[n].y * 7);
                    if (shade)
                        Shade(cellPixels, cells[n]);
                    Put(cellPixels, atlases[n / perAtlas], n % perAtlas, columns);
                }

            var pending = new List<(Vector2Int cell, int slot)>();
            for (var a = 0; a < atlasCount; a++)
            {
                pending.Clear();
                for (var n = a * perAtlas; n < Mathf.Min(cells.Count, (a + 1) * perAtlas); n++)
                    pending.Add((cells[n], n % perAtlas));
                Flush(atlases[a], heights[a], pending, columns);
            }

            ground.ClearAllTiles();
            ground.SetTiles(positions.ToArray(), tiles.ToArray());
            positions.Clear();
            tiles.Clear();
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
                var rect = new Rect((slot % columns) * SlotW + 1, (slot / columns) * SlotH + 1, Unit, TileH);
                // A full-rect mesh and no physics shape: the atlas keeps no CPU copy (see DungeonArt.MakeTile).
                var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), Unit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
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
            for (var y = 0; y < TileH; y++)
                System.Array.Copy(pixels, y * Unit, atlas, ox + (oy + y) * AtlasSize, Unit);
        }

        // The cell's tint painted into its pixels, as the tile's colour would have tinted them.
        static void Shade(Color32[] pixels, Vector2Int cell)
        {
            var tint = LightingRules.GroundShade(cell);
            if (raisedCells != null && raisedCells.Contains(cell))
                tint *= 1.25f;
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
            for (var y = 0; y < TileH; y++)
                for (var x = 0; x < Unit; x++)
                {
                    var i = x + y * Unit;
                    var dx = Mathf.Abs(x + 0.5f - Unit * 0.5f) / (Unit * 0.5f);
                    var dy = Mathf.Abs(y + 0.5f - TileH * 0.5f) / (TileH * 0.5f);
                    if (dx + dy > 1.06f)
                    {
                        cellPixels[i] = default;
                        continue;
                    }
                    // The pixel's world offset from the cell's middle, then ground space (y doubled).
                    var g = new Vector2(centre.x + (x + 0.5f - Unit * 0.5f) / Unit, centre.y + 2f * (y + 0.5f - TileH * 0.5f) / Unit);
                    var s = Wrap(Mathf.FloorToInt(g.x * Unit)) + Wrap(Mathf.FloorToInt(g.y * Unit)) * SourceSize;
                    GroundLayer layer;
                    if (town)
                    {
                        var cover = nearPath ? GroundRules.PathCover(g, GroundRules.TownPaths) : 0f;
                        layer = GroundRules.TownPick(g, cover, grass[s].a / 255f, path[s].a / 255f);
                    }
                    else
                        layer = GroundRules.DungeonPick(g, cobble[s].a / 255f, dungeonTheme);
                    var c = layer switch
                    {
                        GroundLayer.Cobble => cobble[s],
                        GroundLayer.Grass => grass[s],
                        GroundLayer.Path => path[s],
                        _ => dirt[s],
                    };
                    cellPixels[i] = new Color32(c.r, c.g, c.b, 255);
                }
        }

        static float NearestPath(Vector2 ground)
        {
            var paths = GroundRules.TownPaths;
            var best = float.MaxValue;
            for (var i = 0; i + 1 < paths.Length; i += 2)
                best = Mathf.Min(best, GroundRules.SegmentDistance(ground, IsoMath.CellToGround(paths[i]), IsoMath.CellToGround(paths[i + 1])));
            return best;
        }

        static int Wrap(int v) => ((v % SourceSize) + SourceSize) % SourceSize;

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
            return true;
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
