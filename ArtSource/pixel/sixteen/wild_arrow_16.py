# The Wild Arrow drawn in the 16x16 style (2026-11-07, the owner: "now do the player character. Recreate her into something
# original if that is easier"). A wolf-pelt hood with its ears up, copper hair in a long tail, a teal sash and war paint,
# leather and bare legs, a long bow with bone tips, a quiver of red-fletched arrows. Five views drawn as letter grids
# (16 wide, 16 rows above the legs; the other three directions mirrored); legs, the bow and the arrow drawn by code.
# Each drawn pixel is 2 game pixels, as the enemies. This first part writes still views to look at.
# Run: Blender -b -P ArtSource/pixel/sixteen/wild_arrow_16.py
import bpy, os, json
import numpy as np

PALETTE = {
    "K": "1a1018",  # outline
    "p": "dfe3e8", "P": "959daa",  # wolf pelt: light, shade
    "h": "d8582a", "H": "963016", "j": "ff8f4f",  # copper hair: base, shade, light
    "s": "e6a874", "S": "b2704a",  # skin: base, shade
    "E": "2a1810",  # eyes
    "t": "2fd4c4", "T": "17837f",  # teal sash and paint
    "l": "a0623a", "m": "6a3a22",  # leather: base, dark
    "b": "c8904a", "i": "f1e8d2", "n": "dfe6ee",  # bow: wood, bone tips, string
    "a": "d8c090", "f": "e33a2e",  # arrow: shaft, red fletching
    "q": "7a4a26",  # quiver
}

# Standing, the bow in her left hand at her side.
REST = {
    "s": [
        "....KK....KK....",
        "...KpPK..KpPK...",
        "...KppPKKppPK...",
        "..KpppppppppPK..",
        "..KpPhhhhhhpPK..",
        "..KhhsssssshhK..",
        "..KhssEssEsshK..",
        "..KhhssSSsshhK..",
        ".KhpKKssssKKphK.",
        "KhpppPKttKPpppK.",
        "KhKsKlltTllKsKh.",
        ".KKsKlltTllKsK..",
        "..KsKmlTtlmKsK..",
        "..KSKllllllKSK..",
        "...KKmlllmmKK...",
        "....KmlKlmK.....",
    ],
    "se": [
        ".....KK....KK...",
        "....KpPK..KpPK..",
        "....KppPKKppPK..",
        "...KpppppppppPK.",
        "..KhpPhhhhhpPK..",
        "..KhhhsssssshK..",
        "..KhhssEssEsK...",
        "..KhhhsssSssK...",
        ".KhhpKKssssKK...",
        ".KhppPKttKPppK..",
        "KhKKsKltTllKsK..",
        "KhKsKlltTllKsK..",
        ".KKsKmlTtlmKsK..",
        "..KSKllllllKSK..",
        "...KKmlllmmKK...",
        "....KmlKlmK.....",
    ],
    "e": [
        "......KK........",
        ".....KpPK.KK....",
        "....KppPKKpPK...",
        "...KpppppppPK...",
        "..KpppPPhhhhK...",
        "..KhpPKhhssssK..",
        ".KhhpKhsssEssK..",
        ".KhhhKhssssssK..",
        ".KhhhhKKsSSKK...",
        "KhhhKppPKsKK....",
        "KhhKpppPKtK.....",
        "KhKqKllltTK.....",
        ".KqqKlltTsK.....",
        ".KqqKmlllSK.....",
        "..KKKmlllmK.....",
        "....KmlKlmK.....",
    ],
    "ne": [
        "...KK....KK.....",
        "..KPpK..KPpK....",
        "..KPppKKPppK....",
        ".KPpppppppppK...",
        ".KPpppppppppK...",
        "..KhhhhhhhhhK...",
        "..KhhjhhhhhsK...",
        "..KhhhhhhhhSK...",
        ".KhqqKhhhKKpK...",
        "KhpqfqhhhPpppK..",
        "KhKqqlhhllKsK...",
        ".KKqqlhhtlKsK...",
        "..KqqmlhtmKsK...",
        "..KKqllllllKSK..",
        "...KKmlllmmKK...",
        "....KmlKlmK.....",
    ],
    "n": [
        "....KK....KK....",
        "...KPpK..KPpK...",
        "...KPppKKPppK...",
        "..KPpppppppppK..",
        "..KPpppppppppK..",
        "..KhhhhhhhhhhK..",
        "..KhhjhhhhjhhK..",
        "..KhhhhhhhhhhK..",
        ".KpKhqqhhhhKpK..",
        "KpppKfqfhhKpppK.",
        "KsKlKqqhhhKlKsK.",
        ".KsKlqqhhlKlKs..",
        "..KsKqqhhtKKsK..",
        "..KSKqlhhllKSK..",
        "...KKmlllmmKK...",
        "....KmlKlmK.....",
    ],
}

for name, rows in REST.items():
    assert len(rows) == 16 and all(len(r) == 16 for r in rows), (name, [len(r) for r in rows])


# Legs: each leg's column and the way its foot points, per view; the bow hand at rest and the bow's place when she aims.
LEGS = {"s": ((6, -1), (9, 1)), "se": ((6, 1), (9, 1)), "e": ((6, 1), (8, 1)), "ne": ((6, -1), (9, 1)), "n": ((6, -1), (9, 1))}
# Where the bow stands when she holds it down (its column, top and bottom rows) and when she aims (column, top, bottom,
# the way the shot goes: 1 to the right, 0 at or away from the camera), and her shoulder for the raised arm.
BOW_REST = {"s": (14, 7, 17), "se": (14, 7, 17), "e": (11, 8, 18), "ne": (13, 7, 17), "n": (1, 7, 17)}
BOW_AIM = {"s": (13, 2, 12, 0), "se": (14, 2, 12, 1), "e": (14, 2, 12, 1), "ne": (13, 1, 11, 1), "n": (2, 1, 11, 0)}
SHOULDER = {"s": (12, 9), "se": (12, 9), "e": (9, 9), "ne": (11, 9), "n": (3, 9)}

DIRECTIONS = [("s", "s", False), ("sw", "se", True), ("w", "e", True), ("nw", "ne", True), ("n", "n", False),
              ("ne", "ne", False), ("e", "e", False), ("se", "se", False)]
CW, CH = 29, 26
X0, Y0 = 6, 5
SCALE = 2


def blank():
    return [["."] * CW for _ in range(CH)]


def put(cell, x, y, ch, over=True):
    if 0 <= x < CW and 0 <= y < CH and ch != "." and (over or cell[y][x] == "."):
        cell[y][x] = ch


def outline(cell, pixels):
    taken = {(x, y) for x, y, _ in pixels}
    for x, y, _ in pixels:
        for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if (x + ox, y + oy) not in taken:
                put(cell, x + ox, y + oy, "K", over=False)


def legs(cell, view, dy=0, lift=None, tucked=False, kneel=0):
    """Thighs of skin, then boots; a lifted leg is drawn short, its foot off the ground."""
    if tucked:
        return
    ground = Y0 + 19
    for i, (x, toe) in enumerate(LEGS[view]):
        x += X0
        top = Y0 + 16 + dy
        bottom = ground - (2 if i == lift else 0) - kneel
        pixels = []
        for y in range(top, bottom + 1):
            pixels.append((x, y, "s" if y < bottom - 1 else "m"))
        pixels.append((x + toe, bottom, "m"))
        for px, py, ch in pixels:
            put(cell, px, py, ch)
        outline(cell, pixels)


def body(cell, view, dx=0, dy=0, eyes=True, rows=None):
    for y, row in enumerate(rows or REST[view]):
        for x, ch in enumerate(row):
            if ch == "E" and not eyes:
                ch = "s"
            put(cell, X0 + x + dx, Y0 + y + dy, ch)


def bow_rest(cell, view, dx=0, dy=0):
    x, top, bottom = BOW_REST[view]
    x, top, bottom = X0 + x + dx, Y0 + top + dy, Y0 + bottom + dy
    pixels = [(x, top, "i"), (x, bottom, "i")] + [(x + (1 if top + 1 < y < bottom - 1 else 0), y, "b") for y in range(top + 1, bottom)]
    for px, py, ch in pixels:
        put(cell, px, py, ch)
    outline(cell, pixels)


def bow_aim(cell, view, draw=0.0, loosed=False, dx=0, dy=0):
    """The bow raised before her, its string pulled back by draw (0 to 1), an arrow on it until loosed, and her arm from
    the shoulder to the grip."""
    if view in LEVEL:
        bow_level(cell, view, draw, loosed, dx, dy)
        return
    x, top, bottom, way = BOW_AIM[view]
    x, top, bottom = X0 + x + dx, Y0 + top + dy, Y0 + bottom + dy
    mid = (top + bottom) // 2
    bend = 1 if way else 0
    pixels = [(x - bend, top, "i"), (x - bend, bottom, "i")]
    pixels += [(x + (bend if top + 1 < y < bottom - 1 else 0), y, "b") for y in range(top + 1, bottom)]
    # The arm: skin from the shoulder to the grip.
    sx, sy = SHOULDER[view]
    sx, sy = X0 + sx + dx, Y0 + sy + dy
    steps = max(abs(x - sx), abs(mid - sy), 1)
    arm = [(round(sx + (x - sx) * k / steps), round(sy + (mid - sy) * k / steps), "s") for k in range(steps)]
    for px, py, ch in arm + pixels:
        put(cell, px, py, ch)
    outline(cell, arm + pixels)
    # The string: from the tips to the pulled point.
    pull = round(draw * 3)
    string_x = x - bend - 1 if way else x - 1
    if way:
        put(cell, string_x - pull, mid, "n")
        for y in range(top + 1, bottom):
            t = 1 - abs(y - mid) / max(1, (bottom - top) / 2)
            put(cell, string_x - round(pull * t), y, "n", over=False)
        if not loosed:
            for ax in range(string_x - pull, x + 3):
                put(cell, ax, mid, "a")
            put(cell, x + 3, mid, "n")
            put(cell, string_x - pull, mid, "f")
    else:
        if not loosed:
            put(cell, x, mid - 1, "n")
            put(cell, x, mid, "f")


# Shooting toward the camera or away from it, the bow is held level across her (its row, from and to columns, and the
# way the arrow points: 1 down the screen, -1 up it).
LEVEL = {"s": (9, 0, 15, 1), "n": (9, -1, 16, -1)}


def bow_level(cell, view, draw, loosed, dx=0, dy=0):
    row, a, b, way = LEVEL[view]
    row, a, b = Y0 + row + dy, X0 + a + dx, X0 + b + dx
    mid = (a + b) // 2
    bow = [(a, row, "i"), (b, row, "i")] + [(x, row + (way if a + 1 < x < b - 1 else 0), "b") for x in range(a + 1, b)]
    for px, py, ch in bow:
        put(cell, px, py, ch)
    outline(cell, bow)
    pull = round(draw * 3)
    if way > 0:
        # Toward the camera: the string pulled up to her chin, the arrow pointing down at the viewer.
        for x in range(a + 1, b):
            t = 1 - abs(x - mid) / max(1, (b - a) / 2)
            put(cell, x, row - round(pull * t), "n", over=False)
        if not loosed:
            for y in range(row - pull, row + 3):
                put(cell, mid, y, "a")
            put(cell, mid, row + 3, "n")
            put(cell, mid, row - pull, "f")
    else:
        # Away: the bow's ends show either side of her shoulders, the arrow's head over her head.
        if not loosed:
            put(cell, mid, Y0 + dy - 1, "n")
            put(cell, mid, Y0 + dy, "a")


def streak(cell, view, dx=0, dy=0):
    """The loosed arrow flying off: a white line ahead of the bow."""
    if view in LEVEL:
        row, a, b, way = LEVEL[view]
        mid = X0 + (a + b) // 2 + dx
        start = Y0 + row + dy + 3 if way > 0 else Y0 + dy - 2
        for k in range(5):
            put(cell, mid, start + way * k, "n", over=way > 0)
        return
    x, top, bottom, way = BOW_AIM[view]
    mid = Y0 + (top + bottom) // 2 + dy
    x = X0 + x + dx
    if way:
        for ax in range(x + 2, min(CW, x + 9)):
            put(cell, ax, mid, "n" if ax > x + 4 else "a", over=False)
    else:
        for k in range(3):
            put(cell, x, mid - 2 - k, "n", over=False)


def pose(view, aim=None, draw=0.0, loosed=False, flew=False, dx=0, dy=0, lift=None, tucked=False, eyes=True, kneel=0):
    cell = blank()
    behind = view in ("n", "ne")
    if aim is not None and view == "n":
        bow_aim(cell, view, draw, loosed, dx, dy)
        legs(cell, view, dy, lift, tucked, kneel)
        body(cell, view, dx, dy, eyes)
        if not loosed:
            put(cell, X0 + 7 + dx, Y0 + dy - 1, "n")
        if flew:
            streak(cell, view, dx, dy)
        return cell
    if aim is not None and behind:
        bow_aim(cell, view, draw, loosed, dx, dy)
    elif aim is None and behind:
        bow_rest(cell, view, dx, dy)
    legs(cell, view, dy, lift, tucked, kneel)
    body(cell, view, dx, dy, eyes)
    if aim is not None and not behind:
        bow_aim(cell, view, draw, loosed, dx, dy)
    elif aim is None and not behind:
        bow_rest(cell, view, dx, dy)
    if flew:
        streak(cell, view, dx, dy)
    return cell


def dive_frame(view, k):
    """The dive: crouched, a low leap with the body curled (rows squeezed), rolled, landing crouched."""
    if k in (0, 3):
        return pose(view, dy=2, kneel=2)
    cell = blank()
    rows = REST[view]
    squeezed = [rows[i] for i in (0, 2, 3, 4, 5, 6, 8, 9, 11, 13, 14, 15)]
    body(cell, view, dx=1 if k == 1 else 2, dy=6 if k == 1 else 7, rows=squeezed)
    return cell


def death_frame(view, k):
    if k == 0:
        return pose(view, eyes=False)
    cell = blank()
    ground = Y0 + 19
    # The bow on the ground beside her.
    for x in range(X0 + 13, X0 + 23):
        put(cell, x, ground, "b")
        put(cell, x, ground - 1, "K", over=False)
    put(cell, X0 + 12, ground, "i")
    put(cell, X0 + 23, ground, "i")
    sink = (0, 3, 6, 9, 11, 12)[k]
    rows = REST[view]
    for y, row in enumerate(rows):
        shift = sink + 4 if y < 8 else sink + 4 + sink // 2
        if Y0 + y + shift <= ground:
            for x, ch in enumerate(row):
                put(cell, X0 + x, Y0 + y + shift, "s" if ch == "E" else ch)
    return cell


def walking(view, **kw):
    return [pose(view, **kw), pose(view, lift=0, dy=-1, **kw), pose(view, **kw), pose(view, lift=1, dy=-1, **kw)]


def shot(view, legs_moving=False, hold=1):
    """A shot: raised, drawn (held for hold frames), loosed with the arrow away, settling."""
    frames = [dict(aim=1, draw=0.0), dict(aim=1, draw=0.6)] + [dict(aim=1, draw=1.0)] * hold + \
             [dict(aim=1, draw=0.0, loosed=True, flew=True), dict(aim=1, draw=0.0, loosed=True)]
    out = []
    for i, f in enumerate(frames):
        lift = (None, 0, None, 1)[i % 4] if legs_moving else None
        out.append(pose(view, lift=lift, dy=-1 if lift is not None else 0, **f))
    return out


def clips(view):
    c = {
        "idle": [pose(view), pose(view), pose(view, dy=1), pose(view, eyes=False)],
        "walk": walking(view),
        "run": walking(view),
        "attack": shot(view),
        "overdraw": shot(view, hold=3),
        "recoil": shot(view)[:3] + [pose(view, aim=1, loosed=True, flew=True, dx=-1, dy=-1), pose(view, aim=1, loosed=True, dx=-1)],
        "hit": [pose(view, dx=-1, dy=-1, eyes=False), pose(view)],
        "dive": [dive_frame(view, k) for k in range(4)],
        "death": [death_frame(view, k) for k in range(6)],
    }
    c["run_back"] = c["run"]
    for way in ("forward", "back", "left", "right"):
        c["walk_aim_" + way] = walking(view, aim=1, draw=0.4)
    for name in ("attack", "overdraw", "recoil"):
        moving = shot(view, legs_moving=True, hold=3 if name == "overdraw" else 1)
        for suffix in ("", "_back", "_left", "_right"):
            c[name + "_move" + suffix] = moving
    return c


# The armour looks (AppearanceRules.ChestLooks, the bare body and the two legendary chests), each its own leather (l, m)
# and sash (t, T) colours (the owner, 2026-11-07: "put her in game with armour tier colours").
LOOKS = {
    "bare": {"l": "d8c49a", "m": "9a8462"},
    "ranger": {},
    "brigand": {"l": "8a3f32", "m": "4e221c", "t": "e8c24a", "T": "a07a22"},
    "scale": {"l": "b8923e", "m": "6e5420"},
    "plate": {"l": "a9b3c0", "m": "5c6676", "t": "e33a2e", "T": "9a1f18"},
    "knight": {"l": "dfe5ee", "m": "7a8498", "t": "f2c14a", "T": "a6781e"},
    "stag_hide": {"l": "c99a5a", "m": "7a5530", "t": "f1e8d2", "T": "b8ac90"},
    "cinder_jerkin": {"l": "3a3238", "m": "1e1a20", "t": "ff7a2a", "T": "b8401a"},
}


def to_rgba(cell, mirror=False, look=None):
    rows = len(cell)
    cols = len(cell[0])
    out = np.zeros((rows, cols, 4), np.float32)
    colours = dict(PALETTE, **LOOKS.get(look, {}))
    for y in range(rows):
        for x in range(cols):
            ch = cell[y][cols - 1 - x] if mirror else cell[y][x]
            if ch != ".":
                h = colours[ch]
                out[y, x] = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1]
    return out


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
BG = np.array([0.16, 0.21, 0.13, 1], np.float32)
views = {v: clips(v) for v in REST}


def on_bg(img):
    return np.where(img[..., 3:4] > 0.5, img, BG)


# The moves to look at: each clip's frames in the front, left, back and right rows, enlarged 4 times on the green.
SHOWN = ["idle", "walk", "walk_aim_forward", "attack", "attack_move", "overdraw", "recoil", "dive", "hit", "death"]
strips = []
for name in SHOWN:
    for _, view, mirror in [DIRECTIONS[i] for i in (0, 2, 4, 6)]:
        frames = [on_bg(to_rgba(f, mirror)) for f in views[view][name]]
        strip = np.concatenate(frames, 1)
        width = 7 * CW
        strips.append(np.concatenate([strip, np.tile(BG, (CH, width - strip.shape[1], 1))], 1))
    strips.append(np.tile(np.array([0.08, 0.1, 0.06, 1], np.float32), (1, 7 * CW, 1)))
save(np.repeat(np.repeat(np.concatenate(strips, 0), 4, 0), 4, 1), os.path.join(P, "ArtSource", "pixel", "previews", "wild_arrow_16_moves.png"))
big = [on_bg(to_rgba(pose(v))) for v in ("s", "se", "e", "ne", "n")] + [on_bg(to_rgba(pose("e", aim=1, draw=1.0))), on_bg(to_rgba(pose("s", aim=1, draw=1.0)))]
save(np.repeat(np.repeat(np.concatenate(big, 1), 6, 0), 6, 1), os.path.join(P, "ArtSource", "pixel", "previews", "wild_arrow_16_views.png"))
print("wrote")

# A closer look at the shots, the walk and the dive, 6 times.
focus = []
for name, view, mirror in [("attack", "s", False), ("attack", "e", False), ("attack", "n", False), ("walk", "s", False),
                           ("walk", "e", False), ("dive", "e", False)]:
    strip = np.concatenate([on_bg(to_rgba(f, mirror)) for f in views[view][name]], 1)
    focus.append(np.concatenate([strip, np.tile(BG, (CH, 5 * CW - strip.shape[1], 1))], 1))
save(np.repeat(np.repeat(np.concatenate(focus, 0), 5, 0), 5, 1), os.path.join(P, "ArtSource", "pixel", "previews", "wild_arrow_16_focus.png"))

# Animated: every clip playing in four directions at once, as numbered frames for Aseprite to make a GIF of.
ANIM = ["idle", "walk", "walk_aim_forward", "attack", "attack_move", "overdraw", "recoil", "dive", "hit", "death"]
LABEL_DIRS = [DIRECTIONS[i] for i in (0, 7, 6, 4, 2)]
anim_dir = os.path.join(P, "ArtSource", "pixel", "previews", "wild_arrow_16_anim")
os.makedirs(anim_dir, exist_ok=True)
for t in range(24):
    rows = []
    for _, view, mirror in LABEL_DIRS:
        cells = []
        for name in ANIM:
            frames = views[view][name]
            # One-shot clips hold their last frame a moment before starting again.
            k = t % (len(frames) + 2)
            cells.append(on_bg(to_rgba(frames[min(k, len(frames) - 1)], mirror)))
        rows.append(np.concatenate(cells, 1))
    img = np.concatenate(rows, 0)
    save(np.repeat(np.repeat(img, 3, 0), 3, 1), os.path.join(anim_dir, f"frame_{t + 1:02d}.png"))

# The game's sheets: every clip for every look, as the body layer of the drawn character "wild_arrow_drawn" (one grip, 1h;
# the longbow's 2h falls back to it), with her baked timing keys ("1h_<clip>"). Imported by Tools > ARPG > Import Drawn
# Characters into Resources/Characters/wild_arrow_drawn.
# Seconds and ground speeds: the loops drawn here, the one-shots her baked ones (the game stretches them to the action).
TIMING = {"idle": (1.6, 0), "run": (0.5, 3.4), "walk": (0.8, 1.1), "run_back": (0.5, 2.7), "attack": (1.033, 0),
          "overdraw": (3.767, 0), "recoil": (0.7, 0), "hit": (1.267, 0), "death": (3.1, 0), "dive": (1.633, 0),
          "walk_aim_forward": (0.8, 1.1), "walk_aim_back": (0.8, 0.9), "walk_aim_right": (0.8, 1.27), "walk_aim_left": (0.8, 1.41)}
for name in ("attack", "overdraw", "recoil"):
    for suffix in ("", "_back", "_left", "_right"):
        TIMING[name + "_move" + suffix] = (0.7, 0)
OUT = os.path.join(P, "ArtSource", "pixel", "characters", "wild_arrow_drawn")
os.makedirs(OUT, exist_ok=True)
for old in os.listdir(OUT):
    if old.endswith(".png"):
        os.remove(os.path.join(OUT, old))
animations = []
for look in LOOKS:
    for clip, (seconds, speed) in TIMING.items():
        rows = [np.concatenate([to_rgba(f, mirror, look) for f in views[view][clip]], 1) for _, view, mirror in DIRECTIONS]
        sheet_name = f"body_{look}_1h_{clip}"
        save(np.repeat(np.repeat(np.concatenate(rows, 0), SCALE, 0), SCALE, 1), os.path.join(OUT, f"wild_arrow_drawn_{sheet_name}.png"))
        animations.append({"name": sheet_name, "frames": len(views["s"][clip]), "seconds": seconds, "speed": speed, "key": "1h_" + clip})
with open(os.path.join(OUT, "sheet.json"), "w") as f:
    json.dump({"cellWidth": CW * SCALE, "cellHeight": CH * SCALE, "pivotX": CW * SCALE // 2, "pivotY": SCALE // 2,
               "directions": 8, "animations": animations}, f)
# The looks side by side, standing in front view and shooting to the side, 6 times.
tiers = [np.concatenate([on_bg(to_rgba(pose("s"), False, look)), on_bg(to_rgba(pose("e", aim=1, draw=1.0), False, look))], 0) for look in LOOKS]
save(np.repeat(np.repeat(np.concatenate(tiers, 1), 4, 0), 4, 1), os.path.join(P, "ArtSource", "pixel", "previews", "wild_arrow_16_looks.png"))
print("wrote", len(animations), "sheets")
