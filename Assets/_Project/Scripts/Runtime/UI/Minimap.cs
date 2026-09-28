using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The dungeon mini-map (Docs/05-world-and-content.md; Docs/06-ui-ux.md: read-only, in the top corner, and the one
    /// thing up there that answers a tap, to expand it). It draws what the player has explored (<see cref="MinimapReveal"/>)
    /// into a small texture, one pixel per cell, turned 45 degrees and squashed to half height so it lies like the
    /// isometric view: up on the map is up on the screen. The small map shows a window around the player, with the
    /// stairs and chests once seen, and an arrow on its rim toward the stairs down. A tap shows the whole level large in
    /// the middle of the screen, without pausing and without blocking the stick. Added by <see cref="DungeonLevel"/>;
    /// builds its own canvas.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        // Cells across the small map's window. 80 made the rooms specks on the phone; at 48 a large room (36) nearly
        // fills it and the next room's doorway is in view.
        const int WindowCells = 48;

        // Canvas units, on the HUD's 1170 x 2532 reference.
        const float SmallWidth = 440f;
        const float LargeWidth = 1100f;

        // Redrawing the texture is cheap, but not every frame while walking.
        const float RedrawSeconds = 0.2f;

        static readonly Color32 Unexplored = new Color32(0, 0, 0, 0);
        static readonly Color32 FloorColor = new Color32(110, 104, 94, 210);
        static readonly Color32 WallColor = new Color32(196, 190, 178, 255);
        static readonly Color32 StairsUpColor = new Color32(90, 160, 255, 255);
        static readonly Color32 StairsDownColor = new Color32(90, 240, 140, 255);
        static readonly Color32 ChestColor = new Color32(255, 210, 60, 255);
        static readonly Color32 OpenChestColor = new Color32(120, 100, 50, 255);

        DungeonLayout layout;
        MinimapReveal reveal;
        string[] chestKeys;
        Transform player;

        Texture2D texture;
        Color32[] pixels;
        bool dirty = true;
        float redrawTimer;

        RawImage smallMap;
        RectTransform exitArrow;
        GameObject largeView;
        RawImage largeMap;
        RectTransform largePlayer;
        float largeSide;

        public bool IsExpanded => largeView != null && largeView.activeSelf;

        public void Init(DungeonLayout levelLayout, string levelId, Transform playerTransform)
        {
            layout = levelLayout;
            player = playerTransform;
            reveal = new MinimapReveal(layout, GameSession.Current.ExploredCells(levelId, MinimapReveal.CellCount(layout)));
            chestKeys = new string[layout.Chests.Count];
            for (var i = 0; i < chestKeys.Length; i++)
                chestKeys[i] = levelId + "/Chest " + i;

            var b = layout.Bounds;
            texture = new Texture2D(b.width, b.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Minimap",
            };
            pixels = new Color32[b.width * b.height];
            BuildUi();
        }

        void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
        }

        void Update()
        {
            if (layout == null || player == null)
                return;

            var cell = IsoMath.GroundToCell(IsoMath.WorldToGround(player.position));
            dirty |= reveal.RevealAround(cell);

            redrawTimer -= Time.unscaledDeltaTime;
            if (dirty && redrawTimer <= 0f)
            {
                Redraw();
                dirty = false;
                redrawTimer = RedrawSeconds;
            }

            // The window follows the player: its center is the player's cell.
            var b = layout.Bounds;
            smallMap.uvRect = new Rect(
                (cell.x - b.xMin + 0.5f - WindowCells / 2f) / b.width,
                (cell.y - b.yMin + 0.5f - WindowCells / 2f) / b.height,
                (float)WindowCells / b.width,
                (float)WindowCells / b.height);

            UpdateExitArrow(cell);
            if (IsExpanded)
                largePlayer.anchoredPosition = LargePosition(cell);
        }

        void Toggle()
        {
            largeView.SetActive(!largeView.activeSelf);
            if (largeView.activeSelf)
                dirty = true;
        }

        void Redraw()
        {
            var b = layout.Bounds;
            for (var y = 0; y < b.height; y++)
                for (var x = 0; x < b.width; x++)
                {
                    var cell = new Vector2Int(b.xMin + x, b.yMin + y);
                    var color = Unexplored;
                    if (reveal.IsExplored(cell))
                        color = layout.Get(cell) == DungeonCell.Wall ? WallColor : FloorColor;
                    pixels[x + y * b.width] = color;
                }

            Mark(layout.StairsUp, StairsUpColor);
            if (layout.HasStairsDown)
                Mark(layout.StairsDown, StairsDownColor);
            for (var i = 0; i < layout.Chests.Count; i++)
                Mark(layout.Chests[i], GameSession.Current.IsOpened(chestKeys[i]) ? OpenChestColor : ChestColor);

            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        /// <summary>Draws a 3 by 3 marker, only once the player has seen that spot.</summary>
        void Mark(Vector2Int center, Color32 color)
        {
            if (!reveal.IsExplored(center))
                return;
            var b = layout.Bounds;
            for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    var x = center.x + dx - b.xMin;
                    var y = center.y + dy - b.yMin;
                    if (x >= 0 && y >= 0 && x < b.width && y < b.height)
                        pixels[x + y * b.width] = color;
                }
        }

        void UpdateExitArrow(Vector2Int cell)
        {
            if (!layout.HasStairsDown)
            {
                exitArrow.gameObject.SetActive(false);
                return;
            }

            // Hidden once the stairs are inside the window: the marker itself shows them then.
            var offset = layout.StairsDown - cell;
            var inside = Mathf.Abs(offset.x) < WindowCells * 0.4f && Mathf.Abs(offset.y) < WindowCells * 0.4f;
            exitArrow.gameObject.SetActive(!inside);
            if (inside)
                return;

            // The same direction the stairs lie in on screen, placed just inside the diamond's rim.
            var screen = IsoMath.GroundToWorld(IsoMath.CellToGround(layout.StairsDown)) - IsoMath.GroundToWorld(IsoMath.CellToGround(cell));
            var dir = screen.normalized;
            var halfWidth = SmallWidth / 2f;
            var halfHeight = SmallWidth / 4f;
            var toRim = 1f / (Mathf.Abs(dir.x) / halfWidth + Mathf.Abs(dir.y) / halfHeight);
            exitArrow.anchoredPosition = dir * toRim * 0.8f;
            exitArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }

        /// <summary>Where a cell lies on the expanded map, relative to its center.</summary>
        Vector2 LargePosition(Vector2Int cell)
        {
            var b = layout.Bounds;
            var uv = largeMap.uvRect;
            var u = ((cell.x - b.xMin + 0.5f) / b.width - uv.x) / uv.width;
            var v = ((cell.y - b.yMin + 0.5f) / b.height - uv.y) / uv.height;
            var local = new Vector2((u - 0.5f) * largeSide, (v - 0.5f) * largeSide);
            // Turned 45 degrees, then squashed to half height, as the map image itself is.
            var rotated = new Vector2((local.x - local.y) * 0.70710678f, (local.x + local.y) * 0.70710678f);
            return new Vector2(rotated.x, rotated.y * 0.5f);
        }

        void BuildUi()
        {
            var canvasObject = new GameObject("Minimap Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15; // Above the HUD (10), below the inventory screen (50) and the fade (100).
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            // The small map, top right beside the portrait and bars (the owner, 2026-09-28), inside the safe area and
            // with no backdrop. Its box is the tap target that expands it.
            var safe = new GameObject("Safe Area", typeof(RectTransform));
            safe.transform.SetParent(canvasObject.transform, false);
            SafeArea.Fit((RectTransform)safe.transform);
            var box = new GameObject("Minimap", typeof(RectTransform), typeof(Image), typeof(Button));
            box.transform.SetParent(safe.transform, false);
            var boxRect = (RectTransform)box.transform;
            boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(1f, 1f);
            boxRect.anchoredPosition = new Vector2(-16f, -12f);
            boxRect.sizeDelta = new Vector2(SmallWidth, SmallWidth / 2f);
            box.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            box.GetComponent<Button>().onClick.AddListener(Toggle);

            smallMap = BuildDiamond(boxRect, SmallWidth, 0f, out _);
            NewDot(boxRect, 14f);

            var arrow = new GameObject("Exit Arrow", typeof(RectTransform), typeof(Text));
            arrow.transform.SetParent(boxRect, false);
            exitArrow = (RectTransform)arrow.transform;
            exitArrow.sizeDelta = new Vector2(48f, 48f);
            var arrowText = arrow.GetComponent<Text>();
            arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            arrowText.text = "▲";
            arrowText.fontSize = 40;
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = new Color(0.35f, 0.95f, 0.55f);
            arrowText.raycastTarget = false;

            // The expanded map, centered, see-through and not a tap target, so the stick keeps working under it.
            largeView = new GameObject("Minimap Expanded", typeof(RectTransform));
            largeView.transform.SetParent(canvasObject.transform, false);
            var largeRect = (RectTransform)largeView.transform;
            largeRect.anchorMin = largeRect.anchorMax = largeRect.pivot = new Vector2(0.5f, 0.5f);
            // High enough that its bottom tip clears the player, who stands just under the middle of the screen.
            largeRect.anchoredPosition = new Vector2(0f, 330f);
            largeRect.sizeDelta = new Vector2(LargeWidth, LargeWidth / 2f);
            largeMap = BuildDiamond(largeRect, LargeWidth, 0.7f, out _);

            // The whole level in one square view, whichever side is longer.
            var b = layout.Bounds;
            var side = Mathf.Max(b.width, b.height);
            largeMap.uvRect = new Rect(
                (b.width - side) / 2f / b.width, (b.height - side) / 2f / b.height,
                (float)side / b.width, (float)side / b.height);
            largeSide = LargeWidth / Mathf.Sqrt(2f);
            largePlayer = NewDot(largeRect, 18f);
            largeView.SetActive(false);
        }

        /// <summary>A square map image turned 45 degrees inside a half-height parent, on a dark diamond backdrop.</summary>
        RawImage BuildDiamond(RectTransform parent, float width, float backdropAlpha, out RectTransform squashed)
        {
            var squash = new GameObject("Squash", typeof(RectTransform));
            squash.transform.SetParent(parent, false);
            squashed = (RectTransform)squash.transform;
            squashed.localScale = new Vector3(1f, 0.5f, 1f);
            var side = width / Mathf.Sqrt(2f);

            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(squashed, false);
            var backdropRect = (RectTransform)backdrop.transform;
            backdropRect.sizeDelta = new Vector2(side, side);
            backdropRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var backdropImage = backdrop.GetComponent<Image>();
            // Not pure black: the dungeon's void is black, and a black backdrop vanished into it.
            backdropImage.color = new Color(0.14f, 0.13f, 0.12f, backdropAlpha);
            backdropImage.raycastTarget = false;

            var map = new GameObject("Map", typeof(RectTransform), typeof(RawImage));
            map.transform.SetParent(squashed, false);
            var mapRect = (RectTransform)map.transform;
            mapRect.sizeDelta = new Vector2(side, side);
            mapRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var image = map.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        static RectTransform NewDot(RectTransform parent, float size)
        {
            var dot = new GameObject("Player", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(parent, false);
            var rect = (RectTransform)dot.transform;
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var image = dot.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            return rect;
        }
    }
}
