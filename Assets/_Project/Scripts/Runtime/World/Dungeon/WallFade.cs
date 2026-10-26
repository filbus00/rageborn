using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>
    /// Which walls are seen through (2026-10-26, the owner: "in diablo 1, the walls are higher and become see trough when
    /// you are behind them"): every dungeon wall is drawn at full height, and the walls standing between the camera and
    /// her fade, so she and what is near her stay in sight. Pure.
    /// </summary>
    public static class WallFadeRules
    {
        /// <summary>How many steps of x + y in front of her a wall can still cover her: a wall is about 2.1 world units
        /// tall above its cell, and each step of x + y moves a cell 0.25 down the screen.</summary>
        public const int Depth = 8;

        /// <summary>How far to either side (in steps of x - y, half a world unit each) walls fade fully; they fade back
        /// in over <see cref="Feather"/> more.</summary>
        public const int Width = 4;
        public const int Feather = 3;

        /// <summary>A faded wall's opacity.</summary>
        public const float Faded = 0.3f;

        /// <summary>How opaque the wall in a cell is drawn with her standing in another: 1 when it is not in front of her.</summary>
        public static float Alpha(Vector2Int wall, Vector2Int her)
        {
            var d = her.x + her.y - (wall.x + wall.y);
            if (d < 1 || d > Depth)
                return 1f;
            var side = Mathf.Abs(her.x - her.y - (wall.x - wall.y));
            if (side > Width + Feather)
                return 1f;
            return side <= Width ? Faded : Mathf.Lerp(Faded, 1f, (side - Width) / (float)(Feather + 1));
        }
    }

    /// <summary>
    /// Fades the dungeon's wall tiles in front of her (<see cref="WallFadeRules"/>), on every tilemap that draws walls,
    /// when she steps into another cell; the walls she leaves behind get their colour back.
    /// </summary>
    public sealed class WallFade : MonoBehaviour
    {
        readonly List<Tilemap> maps = new List<Tilemap>();
        readonly Dictionary<(Tilemap map, Vector3Int cell), Color> faded = new Dictionary<(Tilemap, Vector3Int), Color>();
        readonly List<(Tilemap map, Vector3Int cell)> leaving = new List<(Tilemap, Vector3Int)>();
        Transform player;
        Vector2Int last = new Vector2Int(int.MinValue, int.MinValue);

        /// <summary>The tilemaps whose tiles are walls (after the level is painted).</summary>
        public void Watch(IEnumerable<Tilemap> wallMaps)
        {
            Restore();
            maps.Clear();
            maps.AddRange(wallMaps);
            last = new Vector2Int(int.MinValue, int.MinValue);
        }

        void LateUpdate()
        {
            if (player == null)
            {
                var controller = FindAnyObjectByType<PlayerController>();
                if (controller == null)
                    return;
                player = controller.transform;
            }
            var her = IsoMath.GroundToCell(IsoMath.WorldToGround(player.position));
            if (her == last)
                return;
            last = her;

            // Walls no longer in front of her get their colour back.
            leaving.Clear();
            foreach (var pair in faded)
            {
                var at = new Vector2Int(pair.Key.cell.x, pair.Key.cell.y);
                if (WallFadeRules.Alpha(at, her) >= 1f)
                    leaving.Add(pair.Key);
            }
            foreach (var key in leaving)
            {
                if (key.map != null)
                    key.map.SetColor(key.cell, faded[key]);
                faded.Remove(key);
            }

            var reach = WallFadeRules.Depth + WallFadeRules.Width + WallFadeRules.Feather;
            for (var x = her.x - reach; x <= her.x; x++)
                for (var y = her.y - reach; y <= her.y; y++)
                {
                    var alpha = WallFadeRules.Alpha(new Vector2Int(x, y), her);
                    if (alpha >= 1f)
                        continue;
                    var cell = new Vector3Int(x, y, 0);
                    foreach (var map in maps)
                    {
                        if (map == null || map.GetTile(cell) == null)
                            continue;
                        var key = (map, cell);
                        if (!faded.TryGetValue(key, out var original))
                        {
                            map.SetTileFlags(cell, TileFlags.None);
                            original = map.GetColor(cell);
                            faded[key] = original;
                        }
                        map.SetColor(cell, new Color(original.r, original.g, original.b, original.a * alpha));
                    }
                }
        }

        void Restore()
        {
            foreach (var pair in faded)
                if (pair.Key.map != null)
                    pair.Key.map.SetColor(pair.Key.cell, pair.Value);
            faded.Clear();
        }
    }
}
