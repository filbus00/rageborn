# Builds her game sheets from the owner's two sheets, as cleaned by extract_her.py (2026-11-08): every clip the game
# plays, in 8 directions, each frame stood on its feet in one cell. Frames the sheets do not have are made from the
# ones they do: run from the walk, hit a jolt, the roll a curled frame turning, death a fall onto her side.
# Writes ArtSource/pixel/characters/wild_arrow_drawn (or the folder given) for Tools > ARPG > Import Drawn Characters:
# the same frames for every armour look (these frames have one outfit). Replaces wild_arrow_16.py's drawing of her.
# Run: Blender -b -P ArtSource/pixel/sixteen/her_sheets/build_her_sheets.py [-- out_folder]
import bpy, numpy as np, pickle, json, os, sys

S = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(S, "..", "..", "..", ".."))
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = args[0] if args else os.path.join(P, "ArtSource", "pixel", "characters", "wild_arrow_drawn")
os.makedirs(OUT, exist_ok=True)
PAL = np.load(S + "/her_palette.npy")
D = pickle.load(open(S + "/her_frames.pkl", "rb"))
SCALE = 2
CW, CH = 34, 30            # the cell, in art pixels
GROUND = 28                # the row her feet stand on
BG = np.array([0.16, 0.21, 0.13, 1], np.float32)


def clean(idx):
    """A frame's palette indices (-1 clear), trimmed, with the pale specks left by the soft white edge removed."""
    f = idx.copy()
    h, w = f.shape
    lum = np.where(f >= 0, PAL[np.maximum(f, 0)].mean(-1), 0)
    for _ in range(2):
        for y in range(h):
            for x in range(w):
                if f[y, x] < 0:
                    continue
                near = [(y + dy, x + dx) for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1))]
                open_ = sum(1 for yy, xx in near if not (0 <= yy < h and 0 <= xx < w) or f[yy, xx] < 0)
                if (lum[y, x] > 0.62 and open_ >= 1) or open_ >= 3:
                    f[y, x] = -1
    ys, xs = np.where(f >= 0)
    f = f[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    # One clean outline (2026-11-08): wherever her edge has no dark pixel, a pixel of the darkest colour outside it.
    dark = int(np.argmin(PAL.sum(1)))
    darkish = set(int(k) for k in np.argsort(PAL.sum(1))[:3])
    g = -np.ones((f.shape[0] + 2, f.shape[1] + 2), int)
    g[1:-1, 1:-1] = f
    out = g.copy()
    for y in range(g.shape[0]):
        for x in range(g.shape[1]):
            if g[y, x] >= 0:
                continue
            for yy, xx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= yy < g.shape[0] and 0 <= xx < g.shape[1] and g[yy, xx] >= 0 and g[yy, xx] not in darkish:
                    out[y, x] = dark
                    break
    ys, xs = np.where(out >= 0)
    return out[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


shoot = [clean(i) for _, i in D["shoot"] if i.shape[0] > 12]
idlewalk = [clean(i) for _, i in D["idlewalk"]]
assert len(shoot) == 24, len(shoot)
SHOT = {"sw": shoot[0:6], "e": shoot[6:12], "ne": shoot[12:18], "w": shoot[18:24]}
# The idle sheet's facings (2026-11-08, looked at one by one): front-left, three-quarter left, back, left; each with its
# two walking frames.
IDLE = {"s": idlewalk[0], "sw": idlewalk[1], "n": idlewalk[2], "w": idlewalk[3]}
WALK = {"s": idlewalk[4:6], "sw": idlewalk[6:8], "n": idlewalk[8:10], "w": idlewalk[10:12]}


def mirror(f):
    return f[:, ::-1]


def feet_x(f):
    """Where her feet are across the frame: the middle of the bottom four rows' pixels."""
    ys = np.where((f >= 0).any(1))[0]
    rows = f[max(0, ys.max() - 3):ys.max() + 1] if len(ys) else f
    xs = np.where((rows >= 0).any(0))[0]
    return (xs.min() + xs.max()) / 2 if len(xs) else f.shape[1] / 2


def place(f, dx=0, dy=0):
    """A frame in a cell, her feet on the ground row in the middle (moved by dx, dy)."""
    cell = -np.ones((CH, CW), int)
    ys = np.where((f >= 0).any(1))[0]
    f = f[ys.min():ys.max() + 1] if len(ys) else f
    h, w = f.shape
    top = GROUND - h + 1 + dy
    left = int(round(CW / 2 - feet_x(f))) + dx
    for y in range(h):
        for x in range(w):
            if f[y, x] >= 0 and 0 <= top + y < CH and 0 <= left + x < CW:
                cell[top + y, left + x] = f[y, x]
    return cell


def squash(f, rows):
    """Drops rows from her middle (curled)."""
    h = f.shape[0]
    keep = [y for y in range(h) if not (h // 2 - rows // 2 <= y < h // 2 - rows // 2 + rows)]
    return f[keep]


def kneel(f, rows):
    """Drops rows from her legs, above the boots (crouching, kneeling): she sinks and her feet stay on the ground."""
    h = f.shape[0]
    start = int(h * 0.62)
    keep = [y for y in range(h) if not (start <= y < start + rows)]
    return f[keep]


def lying(f):
    """On her side: turned a quarter, lying on the ground."""
    return np.rot90(f, 1)


# Her colours by kind, from the palette: hair (a saturated red), skin (a light warm tan) and eyes (green).
def _hsv(c):
    import colorsys
    return colorsys.rgb_to_hsv(*[float(v) for v in c])


HAIR = [k for k, c in enumerate(PAL) if (_hsv(c)[0] < 0.03 or _hsv(c)[0] > 0.93) and _hsv(c)[1] > 0.4]
SKIN = [k for k, c in enumerate(PAL) if 0.02 <= _hsv(c)[0] <= 0.11 and 0.2 <= _hsv(c)[1] <= 0.62 and _hsv(c)[2] > 0.55]
EYES = [k for k, c in enumerate(PAL) if 0.2 < _hsv(c)[0] < 0.5 and _hsv(c)[1] > 0.25]


def head_turned_away(f):
    """A side frame made into a three-quarter back one (2026-11-08, the owner: "she does not seem to have sprites for
    moving north west and north east"; the sheets have none): her face and eyes, in the head's rows, painted over with
    her hair, the hair colour by the brightness of what it covers."""
    g = f.copy()
    h = g.shape[0]
    hair = sorted(HAIR, key=lambda k: PAL[k].sum())
    if not hair:
        return g
    for y in range(int(h * 0.42)):
        for x in range(g.shape[1]):
            k = g[y, x]
            if k in SKIN or k in EYES:
                light = PAL[k].mean()
                g[y, x] = hair[min(len(hair) - 1, int(light * len(hair)))]
    return g


# The frames each of the 8 directions uses: (shot frames, idle frame, walk frames). Down shows the front-left frames,
# down-left the three-quarter ones, left the left ones; up the back; up-left the left ones with the head turned away;
# the right side mirrors the left, but for the sheet's own shot to the right.
def sources(d):
    if d == "s":
        return SHOT["sw"], IDLE["s"], WALK["s"]
    if d == "sw":
        return SHOT["sw"], IDLE["sw"], WALK["sw"]
    if d == "se":
        return [mirror(f) for f in SHOT["sw"]], mirror(IDLE["sw"]), [mirror(f) for f in WALK["sw"]]
    if d == "w":
        return SHOT["w"], IDLE["w"], WALK["w"]
    if d == "e":
        return SHOT["e"], mirror(IDLE["w"]), [mirror(f) for f in WALK["w"]]
    if d == "n":
        return SHOT["ne"], IDLE["n"], WALK["n"]
    if d == "nw":
        return [mirror(f) for f in SHOT["ne"]], head_turned_away(IDLE["w"]), [head_turned_away(f) for f in WALK["w"]]
    # ne
    return SHOT["ne"], mirror(head_turned_away(IDLE["w"])), [mirror(head_turned_away(f)) for f in WALK["w"]]


def shear(f, lean):
    """Leaning: each row moved toward where she faces, the top the most (lean pixels), the feet not at all."""
    h, w = f.shape
    pad = abs(lean)
    out = -np.ones((h, w + 2 * pad), int)
    for y in range(h):
        shift = int(round(lean * (1 - y / max(1, h - 1))))
        out[y, pad + shift:pad + shift + w] = f[y]
    return out


def ball(f, size=14):
    """Curled up: her head and shoulders (the top of the frame) squeezed into a ball about size pixels across."""
    top = f[:max(size + 2, int(f.shape[0] * 0.55))]
    rows = np.linspace(0, top.shape[0] - 1, size).round().astype(int)
    ys, xs = np.where(top >= 0)
    left, right = xs.min(), xs.max()
    cols = np.linspace(left, right, size).round().astype(int)
    return top[rows][:, cols]


def clips(d):
    shot, idle, walk = sources(d)
    back = -1 if d in ("e", "se", "ne") else 1      # away from where she faces, across the screen
    c = {}
    c["idle"] = [place(idle), place(idle), place(idle, dy=1), place(idle)]
    c["walk"] = [place(walk[0]), place(walk[0], dy=-1), place(walk[1]), place(walk[1], dy=-1)]
    c["run"] = c["walk"]
    c["run_back"] = c["walk"]
    for way in ("forward", "back", "left", "right"):
        c["walk_aim_" + way] = [place(shot[1]), place(shot[1], dy=-1), place(shot[1]), place(shot[1], dy=-1)]
    # Shots: the release on the frame the game expects (attack 3 of 5, overdraw 5 of 7).
    c["attack"] = [place(shot[i]) for i in (0, 1, 2, 3, 5)]
    c["overdraw"] = [place(shot[i]) for i in (0, 1, 2, 2, 2, 3, 5)]
    c["recoil"] = [place(shot[0]), place(shot[1]), place(shot[2]), place(shot[3], dx=back), place(shot[4])]
    for name in ("attack", "overdraw", "recoil"):
        for suffix in ("", "_back", "_left", "_right"):
            c[name + "_move" + suffix] = c[name]
    c["hit"] = [place(idle, dx=back, dy=-1), place(idle)]
    # The dive (2026-11-08, the owner: "The dive animation needs work"): she crouches, lunges low leaning the way she
    # goes, curls into a ball that turns that way through an arc off the ground, lands crouched and rises (the game
    # moves her through the dodge, so the frames stay in place).
    forward = -back if d in ("e", "se", "ne", "w", "sw", "nw") else 0
    turn = -1 if forward >= 0 else 1                 # clockwise rolling right, the other way rolling left
    curl = ball(idle)
    c["dive"] = [place(kneel(idle, 4)), place(shear(kneel(walk[0], 3), 4 * (forward or 1)), dy=-1)] + \
                [place(np.rot90(curl, turn * (k + 1)), dy=-(1, 3, 3, 1)[k]) for k in range(4)] + \
                [place(kneel(idle, 6)), place(kneel(idle, 2))]
    # Death: struck, knees going, down, lying on her side.
    lie = lying(idle) if back > 0 else lying(mirror(idle))[:, ::-1]
    c["death"] = [place(idle, dx=back, dy=-1), place(kneel(idle, 3)), place(kneel(idle, 6)), place(kneel(idle, 8)),
                  place(lie, dy=0), place(lie, dy=-1), place(lie)]
    return c


DIRS = ["s", "sw", "w", "nw", "n", "ne", "e", "se"]
TIMING = {"idle": (1.6, 0), "run": (0.5, 3.4), "walk": (0.8, 1.1), "run_back": (0.5, 2.7), "attack": (1.033, 0),
          "overdraw": (3.767, 0), "recoil": (0.7, 0), "hit": (1.267, 0), "death": (3.1, 0), "dive": (1.633, 0),
          "walk_aim_forward": (0.8, 1.1), "walk_aim_back": (0.8, 0.9), "walk_aim_right": (0.8, 1.27), "walk_aim_left": (0.8, 1.41)}
for name in ("attack", "overdraw", "recoil"):
    for suffix in ("", "_back", "_left", "_right"):
        TIMING[name + "_move" + suffix] = (0.7, 0)
LOOKS = ["bare", "ranger", "brigand", "scale", "plate", "knight", "stag_hide", "cinder_jerkin"]


def rgba(cell):
    out = np.zeros((CH, CW, 4), np.float32)
    on = cell >= 0
    out[on, :3] = PAL[cell[on]]
    out[on, 3] = 1
    return out


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


every = {d: clips(d) for d in DIRS}
write_game = True
animations = []
for clip in TIMING:
    rows = [np.concatenate([rgba(f) for f in every[d][clip]], 1) for d in DIRS]
    sheet = np.repeat(np.repeat(np.concatenate(rows, 0), SCALE, 0), SCALE, 1)
    seconds, speed = TIMING[clip]
    for look in (LOOKS if write_game else ["ranger"]):
        name = f"body_{look}_1h_{clip}"
        save(sheet, os.path.join(OUT, f"wild_arrow_drawn_{name}.png"))
        animations.append({"name": name, "frames": len(every["s"][clip]), "seconds": seconds, "speed": speed, "key": "1h_" + clip})
with open(os.path.join(OUT, "sheet.json"), "w") as fh:
    json.dump({"cellWidth": CW * SCALE, "cellHeight": CH * SCALE, "pivotX": CW * SCALE // 2,
               "pivotY": (CH - GROUND - 1) * SCALE + SCALE // 2, "directions": 8, "animations": animations}, fh)

# Previews: the clips in all 8 directions, 3 times; and a close look at a few, 6 times.
SHOWN = ["idle", "walk", "attack", "overdraw", "hit", "dive", "death"]
width = max(len(every["s"][c]) for c in SHOWN) * CW
strips = []
for clip in SHOWN:
    for d in DIRS:
        strip = np.concatenate([np.where(rgba(f)[..., 3:4] > 0.5, rgba(f), BG) for f in every[d][clip]], 1)
        strips.append(np.concatenate([strip, np.tile(BG, (CH, width - strip.shape[1], 1))], 1))
    strips.append(np.tile(np.array([0.08, 0.1, 0.06, 1], np.float32), (2, width, 1)))
save(np.repeat(np.repeat(np.concatenate(strips, 0), 3, 0), 3, 1), os.path.join(P, "ArtSource", "pixel", "previews", "wild_arrow_sheets_moves.png"))
close = []
for d, clip in (("s", "idle"), ("s", "walk"), ("s", "attack"), ("w", "idle"), ("w", "walk"), ("w", "attack"), ("n", "idle"), ("n", "attack")):
    strip = np.concatenate([np.where(rgba(f)[..., 3:4] > 0.5, rgba(f), BG) for f in every[d][clip]], 1)
    close.append(np.concatenate([strip, np.tile(BG, (CH, 8 * CW - strip.shape[1], 1))], 1))
save(np.repeat(np.repeat(np.concatenate(close, 0), 5, 0), 5, 1), os.path.join(P, "ArtSource", "pixel", "previews", "wild_arrow_sheets_close.png"))
print("built", len(animations), "sheets into", OUT)
