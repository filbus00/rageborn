# The inventory's item pictures (the owner, 2026-10-19: Gemini pixel art in the game's style, "the pictures are not
# organized, scan them and implement them. You will need to cut apart some images"). The sources are in
# ArtSource/pixel/items_src: single bows, and sheets of several items in cells. Each item is cut from its cell, its
# magenta background (and the legendaries' pink glow) keyed out, the legendary bows stood upright (they were drawn
# diagonally), then sampled down to clean pixels at 24 a backpack cell (a bow's 2 x 4 cells are 48 x 96) with hard
# alpha, and written as Assets/_Project/Resources/UI/ItemArt/<name>.png, where ItemIcons loads them by ItemArtNames.
# The belts and the Quiver of the Hollow Hound came later (the hound quiver a screenshot, a stray red dot painted out).
#   python3 ArtSource/tools/ui/import_item_art.py
import os, struct, subprocess, tempfile, zlib
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
SRC = os.path.join(ROOT, "ArtSource", "pixel", "items_src")
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "UI", "ItemArt")
PER_CELL = 24

# Backpack cells (width, height) by kind.
CELLS = {"bow": (2, 4), "quiver": (2, 3), "chest": (2, 3), "helm": (2, 2), "gloves": (2, 2), "boots": (2, 2),
         "belt": (2, 1), "ring": (1, 1), "amulet": (1, 1), "blueprint": (1, 1)}


def load(name):
    """The source as an RGB array, decoded with macOS sips."""
    with tempfile.TemporaryDirectory() as tmp:
        bmp = os.path.join(tmp, "x.bmp")
        subprocess.run(["sips", "-s", "format", "bmp", os.path.join(SRC, name), "--out", bmp], check=True, capture_output=True)
        data = open(bmp, "rb").read()
    offset = struct.unpack_from("<I", data, 10)[0]
    width, height = struct.unpack_from("<ii", data, 18)
    bits = struct.unpack_from("<H", data, 28)[0]
    step = bits // 8
    stride = (width * step + 3) & ~3
    rows = np.frombuffer(data, np.uint8, stride * abs(height), offset).reshape(abs(height), stride)
    image = rows[:, :width * step].reshape(abs(height), width, step)[:, :, 2::-1][:, :, :3]
    return image[::-1] if height > 0 else image


def background(rgb, glow=False):
    """Magenta: red and blue both well above green. With glow (the legendary sheet), also the pink halo the glow left
    round each item: red well above green and blue above it."""
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    key = (np.minimum(r, b) - g > 55) & (r > 120) & (b > 110)
    if glow:
        key |= (r - g > 55) & (b - g > 20) & (r > 130)
    return key


def upright(rgb, mask):
    """Turns the item about its middle so its long axis is vertical (the diagonal legendary bows)."""
    ys, xs = np.nonzero(mask)
    cx, cy = xs.mean(), ys.mean()
    cov = np.cov(np.stack([xs - cx, ys - cy]))
    vals, vecs = np.linalg.eigh(cov)
    ax = vecs[:, np.argmax(vals)]
    angle = np.arctan2(ax[0], ax[1])  # from vertical
    size = int(np.hypot(*mask.shape)) + 4
    out_rgb = np.zeros((size, size, 3), np.uint8)
    out_mask = np.zeros((size, size), bool)
    yy, xx = np.mgrid[0:size, 0:size]
    dx, dy = xx - size / 2, yy - size / 2
    ca, sa = np.cos(angle), np.sin(angle)
    sx = np.round(cx + dx * ca + dy * sa).astype(int)
    sy = np.round(cy - dx * sa + dy * ca).astype(int)
    inside = (sx >= 0) & (sy >= 0) & (sx < mask.shape[1]) & (sy < mask.shape[0])
    out_rgb[inside] = rgb[sy[inside], sx[inside]]
    out_mask[inside] = mask[sy[inside], sx[inside]]
    return out_rgb, out_mask


def pixelate(rgb, mask, kind):
    """Fits the item into its kind's canvas, one target pixel per footprint of the source: drawn if most of it is the
    item, coloured by the item's pixels there (the footprint's middle when it is the item)."""
    cw, ch = CELLS[kind]
    width, height = cw * PER_CELL, ch * PER_CELL
    rows = np.nonzero(mask.sum(1) >= 3)[0]
    cols = np.nonzero(mask.sum(0) >= 3)[0]
    y0, y1, x0, x1 = rows[0], rows[-1] + 1, cols[0], cols[-1] + 1
    scale = min((width - 2) / (x1 - x0), (height - 2) / (y1 - y0))
    tw, th = max(1, round((x1 - x0) * scale)), max(1, round((y1 - y0) * scale))
    ox, oy = (width - tw) // 2, (height - th) // 2
    out = np.zeros((height, width, 4), np.uint8)
    for j in range(th):
        for i in range(tw):
            sx0 = x0 + int(i / scale); sx1 = max(sx0 + 1, x0 + int((i + 1) / scale))
            sy0 = y0 + int(j / scale); sy1 = max(sy0 + 1, y0 + int((j + 1) / scale))
            m = mask[sy0:sy1, sx0:sx1]
            if m.mean() < 0.5:
                continue
            cy, cx = (sy0 + sy1) // 2, (sx0 + sx1) // 2
            colour = rgb[cy, cx] if mask[cy, cx] else rgb[sy0:sy1, sx0:sx1][m].mean(0)
            out[oy + j, ox + i, :3] = colour
            out[oy + j, ox + i, 3] = 255
    return out


def save(name, rgba):
    os.makedirs(OUT, exist_ok=True)
    h, w = rgba.shape[:2]
    raw = b"".join(b"\x00" + rgba[y].tobytes() for y in range(h))
    def chunk(tag, body):
        return struct.pack(">I", len(body)) + tag + body + struct.pack(">I", zlib.crc32(tag + body) & 0xFFFFFFFF)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + \
        chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    open(os.path.join(OUT, name + ".png"), "wb").write(png)


def item(source, box, name, kind, inset=10, stand=False, glow=False):
    rgb = source[box[1] + inset:box[3] - inset, box[0] + inset:box[2] - inset]
    mask = ~background(rgb, glow)
    # The sheets' dark grid lines, where a cell's edge reached them, are not the item.
    if stand:
        rgb, mask = upright(rgb, mask)
    save(name, pixelate(rgb, mask, kind))
    return name


def grid(w, h, cols, rows):
    return [[(round(c * w / cols), round(r * h / rows), round((c + 1) * w / cols), round((r + 1) * h / rows)) for c in range(cols)] for r in range(rows)]


def main():
    made = []
    for file, name in [("bow_hunting_bow", "hunting_bow"), ("bow_recurve_bow", "recurve_bow"), ("bow_horn_bow", "horn_bow"),
                       ("bow_yew_longbow", "yew_longbow"), ("bow_war_bow", "war_bow"), ("bow_great_bow", "great_bow")]:
        s = load(file + ".jpeg")
        made.append(item(s, (0, 0, s.shape[1], s.shape[0]), name, "bow"))

    s = load("sheet_quivers.jpeg"); g = grid(848, 1264, 2, 2)
    for (r, c), name in zip([(0, 0), (0, 1), (1, 0)], ["hide_quiver", "studded_quiver", "bone_quiver"]):
        made.append(item(s, g[r][c], name, "quiver"))

    s = load("sheet_chests.jpeg"); g = grid(848, 1264, 2, 3)
    for (r, c), name in zip([(0, 0), (0, 1), (1, 0), (1, 1), (2, 0)], ["ranger", "brigand", "scale", "plate", "knight"]):
        made.append(item(s, g[r][c], name, "chest"))

    s = load("sheet_helms.jpeg"); g = grid(1024, 1024, 3, 3)
    for (r, c), name in zip([(0, 0), (0, 1), (1, 0), (0, 2), (2, 0)], ["hood", "mask", "barbute", "visored", "horned"]):
        made.append(item(s, g[r][c], name, "helm"))

    s = load("sheet_gloves.jpeg"); g = grid(1024, 1024, 2, 3)
    for (r, c), name in zip([(0, 0), (0, 1), (1, 0), (1, 1), (2, 0)], ["wraps", "gloves", "bracers", "gauntlets", "claws"]):
        made.append(item(s, g[r][c], name, "gloves"))

    s = load("sheet_boots.jpeg")
    for box, name in zip([(0, 0, 345, 490), (345, 0, 690, 490), (690, 0, 1024, 490), (0, 490, 512, 1024), (512, 490, 1024, 1024)],
                         ["wrapped", "leather_boots", "strapped", "sabatons", "spiked"]):
        made.append(item(s, box, name, "boots", inset=4))

    for file, names, kind, split in [("sheet_rings.jpeg", ["iron_ring", "copper_ring", "silver_ring", "gold_ring", "jewelled_ring"], "ring", 338),
                                     ("sheet_amulets.jpeg", ["bone_charm", "copper_amulet", "silver_amulet", "gold_amulet", "jewelled_amulet"], "amulet", 510)]:
        s = load(file)
        boxes = [(0, 0, 341, split), (341, 0, 683, split), (683, 0, 1024, split), (0, split, 512, 1024), (512, split, 1024, 1024)]
        for box, name in zip(boxes, names):
            made.append(item(s, box, name, kind))

    s = load("sheet_belts.jpeg")
    for box, name in zip([(0, 0, 480, 330), (480, 0, 970, 330), (970, 0, 1456, 330), (0, 330, 480, 720), (480, 330, 1000, 720)],
                         ["rope", "pouch_belt", "studded", "tassets", "war_girdle"]):
        made.append(item(s, box, name, "belt", inset=4))

    s = load("quiver_of_the_hollow_hound.png").copy()
    # A red dot from the screenshot on the strap, painted the strap's brown.
    r, g, b = s[..., 0].astype(int), s[..., 1].astype(int), s[..., 2].astype(int)
    s[(r > 180) & (g < 110) & (b < 110)] = (112, 96, 70)
    made.append(item(s, (0, 0, s.shape[1], s.shape[0]), "quiver_of_the_hollow_hound", "quiver", inset=2, stand=True, glow=True))

    s = load("blueprint.jpeg")
    made.append(item(s, (0, 0, 1024, 1024), "blueprint", "blueprint"))

    s = load("sheet_legendaries.jpeg"); g = grid(848, 1264, 4, 6)
    legendaries = [
        ((0, 0), "splinterbough", "bow", True), ((0, 1), "ember_tongue", "bow", True), ((0, 2), "hunters_promise", "bow", True),
        ((0, 3), "galeheart", "bow", True), ((1, 0), "widows_draw", "bow", True), ((1, 1), "gallowsreach", "bow", True),
        ((1, 2), "stillwater_yew", "bow", True), ((5, 3), "the_long_silence", "bow", True),
        ((2, 0), "quiver_of_endless_splinters", "quiver", False), ((2, 1), "ashfall_quiver", "quiver", False),
        ((3, 0), "wind_sworn_quiver", "quiver", False), ((3, 1), "magpies_nest", "quiver", False),
        ((2, 2), "falconer", "helm", False), ((2, 3), "unblinking_crown", "helm", False),
        ((3, 2), "stag_hide", "chest", False), ((3, 3), "cinder_jerkin", "chest", False),
        ((4, 0), "fletcher", "gloves", False), ((4, 1), "bloodletter", "gloves", False),
        ((4, 2), "windrunner", "boots", False), ((4, 3), "stalker", "boots", False),
        ((5, 0), "bandolier", "belt", False), ((5, 1), "pack_leaders_signet", "ring", False), ((5, 2), "eye_of_the_storm", "amulet", False),
    ]
    for (r, c), name, kind, stand in legendaries:
        made.append(item(s, g[r][c], name, kind, inset=3, stand=stand, glow=True))
    print(len(made), "pictures written to", OUT)


main()
