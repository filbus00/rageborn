# The Magpie Cutpurse (2026-11-07, the owner: "design another original enemy in 16x16 and replace the sack thief with
# it"): a thief in a cloak of black and blue feathers, a bone-white beak mask with red eyes, two feather tufts like ears,
# a string of stolen gold coins and a hooked knife. He takes the Sack Thief's place as the Bandit kind (rolls in, two
# slashes, backs off, throws a knife), so he has its moves: idle, walk, run, attack, hit, death, dash, roll_back, throw.
# Five views drawn by hand as 16x16 letter grids (the other three mirrored), legs and the knife drawn by code so they
# can move. Each drawn pixel is 2 game pixels, as the Bell Hermit.
# Writes ArtSource/pixel/characters/magpie_cutpurse (sheets and sheet.json) for Tools > ARPG > Import Drawn Characters,
# and previews to ArtSource/pixel/previews. Run: Blender -b -P ArtSource/pixel/sixteen/magpie_cutpurse_sheets.py
import bpy, os, json
import numpy as np

PALETTE = {
    "K": "140f1c",  # outline
    "k": "2e3866", "B": "3d5bb0", "b": "6a9cff",  # the feather cloak: dark, mid, the blue sheen
    "w": "f2ecdc", "v": "b3a98c",  # the bone mask and the white chest: light, shade
    "E": "ff3b30",  # the eyes
    "G": "f2c14a",  # stolen coins
    "L": "d0742c",  # legs and feet
    "W": "6b4423", "C": "dfe6ee", "c": "7d8893",  # the knife: handle, blade, its shade
    "n": "ffffff",  # the streak of a slash or a throw
}

VIEWS = {
    "s": [
        "..KK........KK..",
        "..KbK......KbK..",
        "...KbKKKKKKbK...",
        "...KkkBBBBkkK...",
        "..KkkwwwwwwkkK..",
        "..KkwEEwwEEwkK..",
        "..KkwvwwwwvwkK..",
        "..KkKvwwwwvKkK..",
        ".KkkbKvwwvKbkkK.",
        ".KkbkBKvvKBkbkK.",
        ".KkbwGKKKKGwbkK.",
        "KkkbwwGwwGwwbkkK",
        "KkbkKwwwwwwKkbkK",
        "KkbK.KkkkkK.KbkK",
        "KkK..........KkK",
        ".K............K.",
    ],
    "se": [
        "...KK........KK.",
        "...KbK......KbK.",
        "....KbKKKKKKbK..",
        "...KkkkBBBBkkK..",
        "..KkkkwwwwwwkK..",
        "..KkkwEEwwEEwK..",
        "..KkkwvwwwwvwK..",
        "..KkkKKvwwwwvKK.",
        ".KkbkkKKvwwwvK..",
        ".KkbkBkKKvvKK...",
        ".KkbkwGKKKKGK...",
        "KkkbkwwGwwGwK...",
        "KkbkkKwwwwwwK...",
        "KkbkK.KkkkkK....",
        "KkK.............",
        ".K..............",
    ],
    "e": [
        ".....KK.........",
        ".....KbK........",
        "....KkbKK.......",
        "...KkkkkkK......",
        "..KkkkkwwwK.....",
        "..KkkkwwEwwK....",
        "..KkkkwwwwvvKK..",
        "..KkkkKwwvvvvvK.",
        ".KkbkkKKvvKKKK..",
        ".KkbkbkKKK......",
        ".KkbkbkwwK......",
        "KkbkbkkwwGK.....",
        "KkbkbkkwwwK.....",
        "KkbkKkkkkkK.....",
        "KkK.............",
        ".K..............",
    ],
    "ne": [
        "..KK........KK..",
        "..KbK......KbK..",
        "...KbKKKKKKbK...",
        "...KkkBBBBkkK...",
        "..KkkkkkkkkwkK..",
        "..KkkBBkkkkwkK..",
        "..KkkkkkkkvwkK..",
        "..KkkbkkkkKvK...",
        ".KkkbkkbbkkKK...",
        ".KkbkkbkkbkkbK..",
        ".KkbkbkkkkbkbkK.",
        "KkkbkbkkkkbkbkkK",
        "KkbkbkkbbkkbkbkK",
        "KkbK.KkkkkK.KbkK",
        "KkK..........KkK",
        ".K............K.",
    ],
    "n": [
        "..KK........KK..",
        "..KbK......KbK..",
        "...KbKKKKKKbK...",
        "...KkkBBBBkkK...",
        "..KkkkkkkkkkkK..",
        "..KkkBBkkBBkkK..",
        "..KkkkkkkkkkkK..",
        "..KkkbkkkkbkkK..",
        ".KkkbkkbbkkbkkK.",
        ".KkbkkbkkbkkbkK.",
        ".KkbkbkkkkbkbkK.",
        "KkkbkbkkkkbkbkkK",
        "KkbkbkkbbkkbkbkK",
        "KkbK.KkkkkK.KbkK",
        "KkK..........KkK",
        ".K............K.",
    ],
}
# The legs (their columns) and the way the feet point, and the hand that holds the knife, for each view.
LEGS = {"s": ((6, -1), (9, 1)), "se": ((6, 1), (9, 1)), "e": ((5, 1), (8, 1)), "ne": ((6, -1), (9, 1)), "n": ((6, -1), (9, 1))}
HAND = {"s": (15, 10), "se": (13, 10), "e": (11, 10), "ne": (15, 10), "n": (15, 10)}
# Knife poses, relative to the hand.
KNIFE = {
    "low": [(0, 0, "W"), (1, 1, "C"), (1, 2, "C"), (2, 3, "c")],
    "up": [(0, 0, "W"), (0, -1, "C"), (1, -2, "C"), (1, -3, "c")],
    "slash": [(0, 0, "W"), (1, 0, "C"), (2, 0, "C"), (3, 1, "c")],
    "none": [],
}
STREAK = [(1, -3), (2, -3), (3, -2), (4, -1), (4, 0), (4, 1)]

DIRECTIONS = [("s", "s", False), ("sw", "se", True), ("w", "e", True), ("nw", "ne", True), ("n", "n", False),
              ("ne", "ne", False), ("e", "e", False), ("se", "se", False)]

CW, CH = 25, 21
X0, Y0 = 5, 5
SCALE = 2


def blank():
    return [["."] * CW for _ in range(CH)]


def put(cell, x, y, ch, over=True):
    if 0 <= x < CW and 0 <= y < CH and ch != "." and (over or cell[y][x] == "."):
        cell[y][x] = ch


def outline(cell, pixels):
    for x, y, _ in pixels:
        for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if (x + ox, y + oy, ) not in [(px, py) for px, py, _ in pixels]:
                put(cell, x + ox, y + oy, "K", over=False)


def draw_legs(cell, view, dy, lift=None, tucked=False):
    if tucked:
        return
    ground = Y0 + 15
    for i, (x, toe) in enumerate(LEGS[view]):
        x += X0
        if i == lift:
            pixels = [(x, ground - 1 + min(dy, 0), "L"), (x + toe, ground - 1 + min(dy, 0), "L")]
        else:
            pixels = [(x, y, "L") for y in range(Y0 + 14 + dy, ground)] + [(x, ground, "L"), (x + toe, ground, "L")]
        for px, py, ch in pixels:
            put(cell, px, py, ch)
        outline(cell, pixels)


def draw_body(cell, view, dx=0, dy=0, eyes=True):
    for y, row in enumerate(VIEWS[view]):
        for x, ch in enumerate(row):
            if ch == "E" and not eyes:
                ch = "K"
            put(cell, X0 + x + dx, Y0 + y + dy, ch)


def draw_knife(cell, view, pose, dx=0, dy=0, streak=False):
    hx, hy = HAND[view]
    hx, hy = X0 + hx + dx, Y0 + hy + dy
    pixels = [(hx + px, hy + py, ch) for px, py, ch in KNIFE[pose]]
    for x, y, ch in pixels:
        put(cell, x, y, ch)
    outline(cell, pixels)
    if streak:
        for sx, sy in STREAK:
            put(cell, hx + sx, hy + sy, "n", over=False)


def frame(view, pose="low", dx=0, dy=0, lift=None, tucked=False, streak=False, eyes=True):
    cell = blank()
    draw_legs(cell, view, dy, lift, tucked)
    draw_body(cell, view, dx, dy, eyes)
    draw_knife(cell, view, pose, dx, dy, streak)
    return cell


def death_frame(view, sink):
    cell = blank()
    ground = Y0 + 15
    if sink == 0:
        return frame(view, eyes=True)
    # His knife on the ground, then the cloak folding down under the mask.
    for x, ch in ((X0 + 15, "W"), (X0 + 16, "C"), (X0 + 17, "C"), (X0 + 18, "c")):
        put(cell, x, ground, ch)
        put(cell, x, ground - 1, "K", over=False)
    rows = VIEWS[view][:14]
    for y, row in enumerate(rows):
        shift = sink if y < 10 else sink + sink // 3
        if Y0 + y + shift <= ground:
            for x, ch in enumerate(row):
                put(cell, X0 + x, Y0 + y + shift, "K" if ch == "E" else ch)
    return cell


def clips(view):
    return {
        "idle": [frame(view), frame(view), frame(view, dy=1), frame(view, eyes=False)],
        "walk": [frame(view), frame(view, lift=0, dy=-1), frame(view), frame(view, lift=1, dy=-1)],
        "run": [frame(view, "up"), frame(view, "up", lift=0, dy=-1), frame(view, "up"), frame(view, "up", lift=1, dy=-1)],
        # Two slashes: raised, crouched, cut, raised, cut, low.
        "attack": [frame(view, "up"), frame(view, "up", dy=1), frame(view, "slash", streak=True), frame(view, "up"),
                   frame(view, "slash", streak=True, dy=1), frame(view)],
        "hit": [frame(view, dx=-1, dy=-1, eyes=False), frame(view)],
        "death": [death_frame(view, s) for s in (0, 2, 4, 6, 8, 10)],
        # The roll in: crouched, a low leap with the knife out, landing.
        "dash": [frame(view, "slash", dy=2, tucked=True), frame(view, "slash", dy=-2, tucked=True, streak=True),
                 frame(view, "slash", dy=-1, tucked=True), frame(view, "slash", dy=1)],
        # Backing off: a hop up and back, landing crouched.
        "roll_back": [frame(view, "up", dy=1), frame(view, "up", dy=-3, tucked=True), frame(view, "up", dy=-2, tucked=True),
                      frame(view, dy=1)],
        # The throw: the knife raised, the arm flicked (the knife gone with a streak), another drawn.
        "throw": [frame(view, "up"), frame(view, "up", dy=1), frame(view, "none", streak=True), frame(view)],
    }


TIMING = {"idle": (1.6, 0), "walk": (0.8, 1.2), "run": (0.44, 3.0), "attack": (0.8, 0), "hit": (0.25, 0),
          "death": (0.9, 0), "dash": (0.5, 0), "roll_back": (0.6, 0), "throw": (0.6, 0)}


def to_rgba(cell, mirror):
    out = np.zeros((CH, CW, 4), np.float32)
    for y in range(CH):
        for x in range(CW):
            ch = cell[y][CW - 1 - x] if mirror else cell[y][x]
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


for name, rows in VIEWS.items():
    assert len(rows) == 16 and all(len(r) == 16 for r in rows), (name, [len(r) for r in rows])

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(P, "ArtSource", "pixel", "characters", "magpie_cutpurse")
os.makedirs(OUT, exist_ok=True)

views = {v: clips(v) for v in VIEWS}
sheets = {}
for name in TIMING:
    rows = [np.concatenate([to_rgba(f, mirror) for f in views[view][name]], 1) for _, view, mirror in DIRECTIONS]
    sheets[name] = np.concatenate(rows, 0)
    save(sheets[name], os.path.join(OUT, f"magpie_cutpurse_{name}.png"))

with open(os.path.join(OUT, "sheet.json"), "w") as f:
    json.dump({
        "cellWidth": CW * SCALE, "cellHeight": CH * SCALE, "pivotX": CW * SCALE // 2, "pivotY": SCALE // 2, "directions": 8,
        "animations": [{"name": n, "frames": len(views["s"][n]), "seconds": s, "speed": v} for n, (s, v) in TIMING.items()],
    }, f)

# A close look: the s, w, n and e rows of every clip, enlarged 3 times on the forest green.
BG = np.array([0.16, 0.21, 0.13, 1], np.float32)
close = []
c = SCALE * CH
for name, img in sheets.items():
    for row in (0, 2, 4, 6):
        strip = img[row * c:(row + 1) * c]
        strip = np.where(strip[..., 3:4] > 0.5, strip, BG)
        close.append(np.concatenate([strip, np.tile(BG, (c, 6 * CW * SCALE - strip.shape[1], 1))], 1))
    close.append(np.tile(np.array([0.08, 0.1, 0.06, 1], np.float32), (2, 6 * CW * SCALE, 1)))
close = np.concatenate(close, 0)
save(np.repeat(np.repeat(close, 3, 0), 3, 1), os.path.join(P, "ArtSource", "pixel", "previews", "magpie_cutpurse_close.png"))
print("wrote", OUT)

# The views large: idle s, se, e, ne, n and the slash, 8 times.
big = [np.where(s[..., 3:4] > 0.5, s, BG) for s in
       [to_rgba(views[v]["idle"][0], False) for v in ("s", "se", "e", "ne", "n")] + [to_rgba(views["s"]["attack"][2], False)]]
save(np.repeat(np.repeat(np.concatenate(big, 1), 4, 0), 4, 1), os.path.join(P, "ArtSource", "pixel", "previews", "magpie_cutpurse_views.png"))
