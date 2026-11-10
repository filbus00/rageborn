# The item pictures from the owner's Gemini sheet (2026-11-08, the owner: "ok check downloads", after a prompt asking
# Gemini to redraw ArtSource/pixel/previews/item_art_all.png in the game's style): source.jpeg, 800 x 1300, about 2 sheet
# pixels a picture pixel, soft. Finds each tile, cuts the item out of the tile's flat colour, reads it down to the
# item's size (16 pixels a backpack cell), snaps its colours to a few of its own, outlines it, and writes it over
# Resources/UI/ItemArt/<name>.png. Gemini's sheet follows ours but not exactly: TILES says which tile is which
# (None: a tile not used). Pictures it did not draw keep the old ones. A preview, old above new:
# ArtSource/pixel/previews/item_art_gemini.png.
# Run: Blender -b -P ArtSource/pixel/sixteen/item_gemini/extract_items.py [-- preview]   (preview: nothing written to the game)
import bpy, os, sys
import numpy as np

S = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(S, "..", "..", "..", ".."))
ART = os.path.join(P, "Assets", "_Project", "Resources", "UI", "ItemArt")
PREVIEW = os.path.join(P, "ArtSource", "pixel", "previews", "item_art_gemini.png")
ONLY_PREVIEW = "preview" in sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else False
K = np.array([0x1a, 0x10, 0x18]) / 255.0

# The tiles in reading order, section by section, named by what Gemini drew there.
TILES = [
    # Bows
    "ember_tongue", "galeheart", "gallowsreach", "great_bow", "horn_bow", "hunters_promise", "hunting_bow",
    "recurve_bow", "splinterbough", "stillwater_yew", "the_long_silence", "war_bow", "widows_draw", "yew_longbow",
    # Quivers and chests (its "bone_quiver" is dark, its fourth a wrapped bone quiver; the steel scale and the dark
    # coat in the second row were not asked for)
    "ashfall_quiver", None, "brigand", "bone_quiver", "hide_quiver", "knight", "magpies_nest", "plate",
    "quiver_of_endless_splinters", "quiver_of_the_hollow_hound",
    "ranger", "scale", None, "studded_quiver", "wind_sworn_quiver", "cinder_jerkin", None, "stag_hide",
    # Helms, gloves, boots (no bracers; a second visored, the one without the plume, not used)
    "hood", "mask", "barbute", "bloodletter", None, "fletcher", "gauntlets", "gloves", "falconer", "horned",
    "leather_boots", "claws", "sabatons", "spiked", "stalker", "strapped", "unblinking_crown", "visored",
    "windrunner", "wrapped",
    "wraps",
    # Belts
    "bandolier", "pouch_belt", "rope", "studded", "tassets", "war_girdle",
    # Amulets, rings, blueprint
    "blueprint", "bone_charm", "copper_amulet", "copper_ring", "eye_of_the_storm", "gold_amulet", "gold_ring",
    "iron_ring", "jewelled_amulet", "jewelled_ring", "pack_leaders_signet", "silver_amulet", "silver_ring",
]


def load(path):
    im = bpy.data.images.load(path)
    w, h = im.size
    a = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1].copy()   # top row first
    bpy.data.images.remove(im)
    return a


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


def runs(flags):
    out, i = [], 0
    while i < len(flags):
        if flags[i]:
            j = i
            while j < len(flags) and flags[j]:
                j += 1
            out.append((i, j))
            i = j
        else:
            i += 1
    return out


def tiles(img):
    """The tiles: rectangles lighter than the page, at least 50 pixels across and 30 down, in reading order."""
    rgb = img[..., :3]
    page = np.median(rgb[-40:, :, :].reshape(-1, 3), 0)
    lit = np.abs(rgb - page).sum(-1) > 0.08
    out = []
    for y0, y1 in runs(lit.mean(1) > 0.04):
        if y1 - y0 < 30:
            continue
        for x0, x1 in runs(lit[y0:y1].mean(0) > 0.5):
            if x1 - x0 >= 50:
                out.append((x0, y0, x1, y1))
    return out


def size_of(name):
    im = bpy.data.images.load(os.path.join(ART, name + ".png"))
    w, h = im.size
    bpy.data.images.remove(im)
    return w, h


def cut(img, box):
    """The item in a tile: every pixel unlike the tile's colour, above the name under it."""
    x0, y0, x1, y1 = box
    t = img[y0 + 4:y1 - 4, x0 + 4:x1 - 4, :3]
    flat = np.median(np.concatenate([t[:3].reshape(-1, 3), t[:, :3].reshape(-1, 3), t[:, -3:].reshape(-1, 3)]), 0)
    on = np.abs(t - flat).sum(-1) > 0.16
    # The name: the bottom 15 rows of the tile (it touches the item in the small tiles).
    on[-13:] = False
    # Small specks (JPEG noise) dropped: a pixel needs two marked neighbours.
    n = sum(np.roll(np.roll(on, dy, 0), dx, 1) for dy in (-1, 0, 1) for dx in (-1, 0, 1)) - on
    on &= n >= 2
    # Only the item: the marked pixels joined to the biggest piece (JPEG noise at the tile's rim made the small
    # pictures shrink to fit it).
    on = biggest(on)
    ys, xs = np.where(on)
    return t[ys.min():ys.max() + 1, xs.min():xs.max() + 1], on[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def biggest(on):
    """The largest piece of touching marked pixels (8 ways), with any piece within 3 pixels of it."""
    h, w = on.shape
    label = np.zeros((h, w), int)
    sizes = [0]
    for y in range(h):
        for x in range(w):
            if not on[y, x] or label[y, x]:
                continue
            n = len(sizes)
            stack, count = [(y, x)], 0
            label[y, x] = n
            while stack:
                cy, cx = stack.pop()
                count += 1
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        yy, xx = cy + dy, cx + dx
                        if 0 <= yy < h and 0 <= xx < w and on[yy, xx] and not label[yy, xx]:
                            label[yy, xx] = n
                            stack.append((yy, xx))
            sizes.append(count)
    main = label == int(np.argmax(sizes))
    near = main.copy()
    for _ in range(3):
        near = near | np.roll(near, 1, 0) | np.roll(near, -1, 0) | np.roll(near, 1, 1) | np.roll(near, -1, 1)
    keep = np.isin(label, np.unique(label[near & on]))
    return on & keep


def kmeans(colours, k, rounds=12):
    k = min(k, len(colours))
    lum = colours.sum(1)
    centres = colours[np.argsort(lum)[np.linspace(0, len(colours) - 1, k).astype(int)]].copy()
    for _ in range(rounds):
        nearest = np.argmin(((colours[:, None] - centres[None]) ** 2).sum(-1), 1)
        for i in range(k):
            if (nearest == i).any():
                centres[i] = colours[nearest == i].mean(0)
    return centres


def shrink(rgb, on, w, h):
    """Read down to fit w x h less a pixel each side, keeping its shape; each pixel the median of the item pixels it
    covers, drawn where they cover more than 40 percent of it. Centred, standing on the bottom for tall things."""
    ih, iw = on.shape
    scale = min((w - 2) / iw, (h - 2) / ih)
    tw, th = max(1, int(round(iw * scale))), max(1, int(round(ih * scale)))
    out = np.zeros((h, w, 4), np.float32)
    left, top = (w - tw) // 2, (h - th) // 2
    for y in range(th):
        for x in range(tw):
            sy0, sy1 = int(y * ih / th), max(int(y * ih / th) + 1, int((y + 1) * ih / th))
            sx0, sx1 = int(x * iw / tw), max(int(x * iw / tw) + 1, int((x + 1) * iw / tw))
            m = on[sy0:sy1, sx0:sx1]
            if m.mean() > 0.3:
                out[top + y, left + x, :3] = np.median(rgb[sy0:sy1, sx0:sx1][m], 0)
                out[top + y, left + x, 3] = 1
    return out


def finish(img, colours):
    """Its colours snapped to a few of its own (by its size), an outline round it where it has none: Gemini draws its own,
    so only the bright pixels at the edge are put outside a dark one."""
    on = img[..., 3] > 0
    if not on.any():
        return img
    pix = img[..., :3][on]
    centres = kmeans(pix, colours)
    img[..., :3][on] = centres[np.argmin(((pix[:, None] - centres[None]) ** 2).sum(-1), 1)]
    h, w = on.shape
    edge = np.zeros_like(on)
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        shifted = np.zeros_like(on)
        ys, xs = slice(max(dy, 0), h + min(dy, 0)), slice(max(dx, 0), w + min(dx, 0))
        yd, xd = slice(max(-dy, 0), h + min(-dy, 0)), slice(max(-dx, 0), w + min(-dx, 0))
        shifted[yd, xd] = ~on[ys, xs]
        edge |= on & shifted
    edge |= on & (np.arange(h)[:, None] == 0) | on & (np.arange(h)[:, None] == h - 1)
    edge |= on & (np.arange(w)[None] == 0) | on & (np.arange(w)[None] == w - 1)
    # A bright edge pixel gets the outline beside it, outside, where there is room.
    lum = img[..., :3].mean(-1)
    for y, x in zip(*np.where(edge & (lum > 0.22))):
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            yy, xx = y + dy, x + dx
            if 0 <= yy < h and 0 <= xx < w and not on[yy, xx]:
                img[yy, xx, :3] = K
                img[yy, xx, 3] = 1
    return img


src = load(os.path.join(S, "source.jpeg"))
found = tiles(src)
print("tiles", len(found), "named", len(TILES))
assert len(found) == len(TILES), (len(found), len(TILES))

pairs = []
for box, name in zip(found, TILES):
    if name is None:
        continue
    w, h = size_of(name)
    old = load(os.path.join(ART, name + ".png"))
    rgb, on = cut(src, box)
    new = finish(shrink(rgb, on, w, h), 6 if w * h <= 256 else 12)
    pairs.append((name, old, new))
    if not ONLY_PREVIEW:
        save(new, os.path.join(ART, name + ".png"))

# Preview: old over new, 4 times, on the Bag's dark fill.
BG = np.array([0.07, 0.05, 0.05, 1], np.float32)
cols, cell = 12, (34, 66)
rows = (len(pairs) + cols - 1) // cols
sheet = np.tile(BG, (rows * (cell[1] * 2 + 6), cols * (cell[0] + 4), 1))
for i, (name, old, new) in enumerate(pairs):
    r, c = divmod(i, cols)
    x = c * (cell[0] + 4) + 2
    for k, pic in enumerate((old, new)):
        y = r * (cell[1] * 2 + 6) + k * (cell[1] + 2) + 2
        h, w = pic.shape[:2]
        region = sheet[y:y + h, x:x + w]
        a = pic[..., 3:4] > 0.5
        sheet[y:y + h, x:x + w] = np.where(a, np.concatenate([pic[..., :3], np.ones_like(pic[..., :1])], -1), region)
save(np.repeat(np.repeat(sheet, 4, 0), 4, 1), PREVIEW)
print("done", len(pairs), "pictures", "(preview only)" if ONLY_PREVIEW else "written")
