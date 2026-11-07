# The Bell Hermit's sheets (2026-11-07, the owner: "do all directions and animations and put it in game").
# Five views drawn by hand as 16x16 letter grids (front, three-quarter front, side, three-quarter back, back; the other
# three are mirrors), the staff and its bell drawn by code so they can swing and rise. Every clip is built from the views:
# idle (the bell sways, a blink), walk and run (a waddle, one foot tucked under the cape), attack (the staff raised, the
# bell rung, rings of sound), hit (a jolt) and death (he sinks under his hat, the staff falls). Each pixel is shown as 2
# of the game's pixels (27 a unit), so he stands 32 pixels, about two thirds of her.
# Writes ArtSource/pixel/characters/bell_hermit (sheets and sheet.json) for Tools > ARPG > Import Drawn Characters, and
# a preview to ArtSource/pixel/previews. Run: Blender -b -P ArtSource/pixel/sixteen/bell_hermit_sheets.py
import bpy, os, json
import numpy as np

PALETTE = {
    "K": "1a1018",  # outline
    "y": "ffe08a", "Y": "e8b84e", "o": "a8772a",  # straw hat: light, base, shade
    "d": "2a1f2e",  # the shadow under the hat
    "E": "6ff2ff", "e": "ffffff",  # the eye and its glint
    "q": "c98a4a", "R": "a4532a", "r": "6e3420",  # straw cape: light, base, shade
    "L": "8a5a36",  # root feet (lighter than the cape's shade, to show under it)
    "W": "7a4e2a",  # staff
    "G": "f2c14a", "g": "9a6a1e", "j": "fff2b0",  # brass bell: base, shade, light
    "n": "bff8ff",  # rings of sound
}

HAT = [
    "................",
    ".....KK.........",
    "....KyYK........",
    "...KyYYoK.......",
    "..KyYYYYoK......",
    ".KyYYYYYYoK.....",
    "KyYYYYYYYYoK....",
    ".KKKKKKKKKKK....",
]
CAPE = [
    "KqRrRqRrRRrK....",
    "KqRrRqRrRRrK....",
    ".KRrRqRrRRrK....",
    "..KrKqRKRrKK....",
]
# The rows between the hat and the cape (rows 8 to 10) for each view.
FACES = {
    "s": ["..KddddddddK....", "..KdddEeddKK....", ".KqqRddRRRrK...."],
    "se": ["..KddddddddK....", "..KddddEeddK....", ".KqqRRddRRrK...."],
    "e": ["..KRRrdddddK....", "..KRRrddEedK....", ".KqRRrRdddrK...."],
    "ne": ["..KRRrRRrRRK....", "..KRrRRrRddK....", ".KqRrRRrRRrK...."],
    "n": ["..KRRrRRrRRK....", "..KRrRRrRRrK....", ".KqRrRRrRRrK...."],
}
# The feet under the cape (first and last column of each, outline included), and the staff's column, for each view.
FEET = {"s": [(3, 6), (8, 11)], "se": [(4, 7), (8, 11)], "e": [(4, 7), (7, 10)], "ne": [(3, 6), (8, 11)], "n": [(3, 6), (8, 11)]}
STAFF_X = 12

# The sheet's rows, top to bottom, and the view each is drawn from (mirrored when the second item is True).
DIRECTIONS = [("s", "s", False), ("sw", "se", True), ("w", "e", True), ("nw", "ne", True), ("n", "n", False),
              ("ne", "ne", False), ("e", "e", False), ("se", "se", False)]

CW, CH = 25, 20   # a cell in the drawing's pixels; the body sits at (X0, Y0)
X0, Y0 = 5, 4
SCALE = 2         # game pixels a drawn pixel
SWAP = {"y": "o", "o": "y", "q": "r", "r": "q"}  # mirrored views keep their light on the left


def body(view, eye="open"):
    rows = HAT + FACES[view] + CAPE
    rows = [list(r) for r in rows]
    for row in rows:
        for x, ch in enumerate(row):
            if ch == "E" and eye == "narrow":
                row[x] = "E"
            if ch == "e" and eye == "narrow":
                row[x] = "E"
            if ch in "Ee" and eye == "shut":
                row[x] = "d"
    return rows


def put(cell, x, y, ch, over=True):
    if 0 <= x < CW and 0 <= y < CH and ch != "." and (over or cell[y][x] == "."):
        cell[y][x] = ch


def draw_body(cell, rows, dx=0, dy=0, floor=CH):
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if Y0 + y + dy < floor:
                put(cell, X0 + x + dx, Y0 + y + dy, ch)


def draw_feet(cell, view, tucked=None, dy=0):
    """The feet at the ground row; a tucked foot is hidden under the cape. When the body is lifted (dy < 0) the
    planted feet reach up to it."""
    for i, (a, b) in enumerate(FEET[view]):
        if i == tucked:
            continue
        for y in range(Y0 + 15 + dy, Y0 + 16):
            for x in range(a, b + 1):
                put(cell, X0 + x, y, "K" if x in (a, b) else "L")


def draw_staff(cell, top=1, bottom=15, bell_dx=0, bell_dy=0, rings=0, dx=0):
    """The staff in his hand, its hook and the bell on its cord. top and bottom are body rows."""
    x = X0 + STAFF_X + dx
    t, b = Y0 + top, Y0 + bottom
    for y in range(t, b + 1):
        put(cell, x - 1, y, "K")
        put(cell, x + 1, y, "K")
        put(cell, x, y, "W")
    # The hook: up and over toward the bell.
    for xx in (x, x + 1, x + 2):
        put(cell, xx, t - 1, "K")
    put(cell, x + 1, t, "W")
    put(cell, x + 2, t, "W")
    put(cell, x + 3, t, "K")
    # The cord and the bell under the hook's end.
    cx, cy = x + 2 + bell_dx, t + 1 + bell_dy
    for yy in range(t + 1, cy + 1):
        put(cell, x + 2 + (bell_dx if yy == cy else 0), yy, "g")
    bell = [(0, 0, "G"), (-1, 1, "G"), (0, 1, "G"), (1, 1, "K"), (-1, 2, "j"), (0, 2, "G"), (1, 2, "g"),
            (-1, 3, "G"), (0, 3, "G"), (1, 3, "g"), (-1, 4, "K"), (0, 4, "g"), (1, 4, "K")]
    for bx, by, ch in bell:
        put(cell, cx + bx, cy + 1 + by, ch)
    put(cell, cx + 2, cy + 1, "K", over=False)
    put(cell, cx + 2, cy + 2, "K", over=False)
    put(cell, cx + 2, cy + 3, "K", over=False)
    put(cell, cx - 2, cy + 2, "K", over=False)
    put(cell, cx - 2, cy + 3, "K", over=False)
    # Rings of sound round the bell: short arcs either side, further out each beat.
    for r in range(rings):
        d = 3 + 2 * r
        for yy in (cy + 2, cy + 3):
            put(cell, cx - d, yy, "n", over=False)
            put(cell, cx + d, yy, "n", over=False)
        put(cell, cx - d + 1, cy + 1, "n", over=False)
        put(cell, cx + d - 1, cy + 1, "n", over=False)
        put(cell, cx - d + 1, cy + 4, "n", over=False)
        put(cell, cx + d - 1, cy + 4, "n", over=False)


def blank():
    return [["."] * CW for _ in range(CH)]


def frame(view, eye="open", dx=0, dy=0, tucked=None, bell_dx=0, bell_dy=0, top=1, rings=0, staff_behind=False):
    cell = blank()
    if staff_behind:
        draw_staff(cell, top, 15 + dy, bell_dx, bell_dy, rings, dx)
    draw_feet(cell, view, tucked, dy)
    draw_body(cell, body(view, eye), dx, dy)
    if not staff_behind:
        draw_staff(cell, top + dy, 15 + dy, bell_dx, bell_dy, rings, dx)
    return cell


def death_frame(view, sink):
    cell = blank()
    ground = Y0 + 15
    if sink > 0:
        # The staff lies on the ground behind him, the bell at its end.
        for x in range(X0 + 5, X0 + 17):
            put(cell, x, ground - 1, "K")
            put(cell, x, ground, "W")
        put(cell, X0 + 17, ground, "K")
        put(cell, X0 + 18, ground - 1, "G")
        put(cell, X0 + 18, ground, "g")
        put(cell, X0 + 19, ground, "K")
        rows = body(view, "shut")
        # The hat drops less than the body sinks, so he goes down under it.
        for y, row in enumerate(rows):
            shift = sink if y < len(HAT) else sink + sink // 2
            if Y0 + y + shift <= ground:
                for x, ch in enumerate(row):
                    put(cell, X0 + x, Y0 + y + shift, ch)
        return cell
    draw_feet(cell, view)
    draw_body(cell, body(view, "narrow"))
    draw_staff(cell)
    return cell


def clips(view):
    side = view == "e"
    return {
        # The bell sways and settles; he blinks once.
        "idle": [frame(view), frame(view, bell_dy=1), frame(view), frame(view, eye="shut", bell_dx=-1)],
        # A waddle: a foot tucked under the cape and the body lifted, the bell swinging behind.
        "walk": [frame(view), frame(view, tucked=0, dy=-1, bell_dx=-1), frame(view), frame(view, tucked=1, dy=-1, bell_dx=1)],
        "run": [frame(view, bell_dx=-1), frame(view, tucked=0, dy=-1, bell_dx=-2, bell_dy=1), frame(view, bell_dx=-1),
                frame(view, tucked=1, dy=-1, bell_dx=-2, bell_dy=1)],
        # The staff raised high, then the bell rung with rings of sound, then lowered.
        "attack": [frame(view, top=0), frame(view, top=-2, eye="narrow"), frame(view, top=-3, eye="narrow", bell_dx=-2),
                   frame(view, top=-3, bell_dx=2, rings=1), frame(view, top=-3, bell_dx=-2, rings=2),
                   frame(view, top=-1, bell_dx=1, rings=3)],
        "hit": [frame(view, eye="narrow", dx=-1 if not side else -1, dy=-1, bell_dx=2), frame(view, eye="narrow", bell_dx=-1)],
        "death": [death_frame(view, s) for s in (0, 1, 2, 4, 6, 8)],
    }


TIMING = {"idle": (1.6, 0), "walk": (0.8, 1.2), "run": (0.48, 2.6), "attack": (0.75, 0), "hit": (0.25, 0), "death": (0.9, 0)}


def to_rgba(cell, mirror):
    out = np.zeros((CH, CW, 4), np.float32)
    for y in range(CH):
        for x in range(CW):
            ch = cell[y][x]
            if mirror:
                ch = SWAP.get(cell[y][CW - 1 - x], cell[y][CW - 1 - x])
            if ch != ".":
                h = PALETTE[ch]
                out[y, x] = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1]
    return np.repeat(np.repeat(out, SCALE, 0), SCALE, 1)


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(P, "ArtSource", "pixel", "characters", "bell_hermit")
os.makedirs(OUT, exist_ok=True)

views = {v: clips(v) for v in FACES}
sheets = {}
for name in TIMING:
    rows = []
    for _, view, mirror in DIRECTIONS:
        rows.append(np.concatenate([to_rgba(f, mirror) for f in views[view][name]], 1))
    sheets[name] = np.concatenate(rows, 0)
    save(sheets[name], os.path.join(OUT, f"bell_hermit_{name}.png"))

sheet = {
    "cellWidth": CW * SCALE, "cellHeight": CH * SCALE,
    # The pivot at his feet, in the middle of the cell (the mirror's axis), from the cell's bottom.
    "pivotX": CW * SCALE // 2, "pivotY": SCALE // 2, "directions": 8,
    "animations": [{"name": n, "frames": len(views["s"][n]), "seconds": s, "speed": v} for n, (s, v) in TIMING.items()],
}
with open(os.path.join(OUT, "sheet.json"), "w") as f:
    json.dump(sheet, f)

# The preview: every clip's frames in every direction, enlarged 3 times on the forest green.
BG = np.array([0.16, 0.21, 0.13, 1], np.float32)
parts = []
for name, img in sheets.items():
    img = np.where(img[..., 3:4] > 0.5, img, BG)
    parts.append(img)
width = max(p.shape[1] for p in parts)
parts = [np.concatenate([p, np.tile(BG, (p.shape[0], width - p.shape[1], 1))], 1) for p in parts]
gap = np.tile(np.array([0.08, 0.1, 0.06, 1], np.float32), (4, width, 1))
preview = np.concatenate(sum([[p, gap] for p in parts], [])[:-1], 0)
preview = np.repeat(np.repeat(preview, 2, 0), 2, 1)
save(preview, os.path.join(P, "ArtSource", "pixel", "previews", "bell_hermit_sheets.png"))
print("wrote", OUT)

# A close look: the s, e and n rows of each clip, enlarged 5 times.
close = []
for name, img in sheets.items():
    c = SCALE * CH
    for row in (0, 2, 4, 6):
        strip = img[row * c:(row + 1) * c]
        strip = np.where(strip[..., 3:4] > 0.5, strip, BG)
        close.append(np.concatenate([strip, np.tile(BG, (c, 6 * CW * SCALE - strip.shape[1], 1))], 1))
close = np.concatenate(close, 0)
save(np.repeat(np.repeat(close, 3, 0), 3, 1), os.path.join(P, "ArtSource", "pixel", "previews", "bell_hermit_close.png"))
