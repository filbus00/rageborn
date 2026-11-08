# The portraits in the 16x16 style (2026-11-08, the owner: "redraw the effects and UI in 16x16 too"): the ten base faces
# of the archers (PortraitFaces paints each archer's skin, hair, eyes and marks on one of them by her name) and the seven
# keepers of the Quarters, drawn from shapes by code with light from the top left and dark outlines, each drawn pixel two
# menu pixels (the archers' faces are written at one and shown at two by the header's frame).
# Writes ArtSource/ui/faces/archer_<n>.png, _regions.png and .json, and ArtSource/ui/pixel/portrait_<name>.png; Tools >
# ARPG > Import Menu Art copies them into Resources/UI. A preview goes to ArtSource/pixel/previews/portraits_16.png.
# Run: Blender -b -P ArtSource/pixel/sixteen/portraits_16.py
import bpy, os, json, math, random
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
FACES = os.path.join(P, "ArtSource", "ui", "faces")
PIXEL = os.path.join(P, "ArtSource", "ui", "pixel")
K = "1a1018"
SKIN, HAIR, IRIS = 1, 2, 3

R = {
    "skin": ["7a4a32", "b2704a", "e6a874", "f6cca0"],
    "pale": ["6a6a86", "9a9ab4", "cfd0e0", "eef0f8"],
    "hair": ["2a160c", "4e2a14", "7a4422", "a8683a"],
    "blonde": ["7a5a1e", "b08a34", "e0c060", "f8e8a0"],
    "grey": ["4a4a4e", "7a7a80", "b4b4ba", "e4e4e8"],
    "ginger": ["5a1e0c", "963016", "d8582a", "ff8f4f"],
    "black": ["0e0e12", "1e1e26", "34343e", "5a5a66"],
    "cloak": ["14301e", "1e4a2c", "2e6a3a", "4e8a4a"],
    "leather": ["3e2414", "6a3a22", "a0623a", "c88a58"],
    "steel": ["2e343e", "5c6676", "a9b3c0", "e4e9f0"],
    "gold": ["6a4e14", "a07a22", "e8c24a", "fbe68a"],
    "white": ["8a8a90", "b8b8c0", "e0e0e6", "fafaff"],
    "navy": ["0e1430", "1a2650", "2e4080", "4a64b0"],
    "red": ["4a0e0c", "7a1a14", "b8322a", "e86048"],
    "teal": ["0e4a48", "17837f", "2fd4c4", "a8f4ea"],
    "fur": ["4a3a2a", "7a6448", "b49a74", "e0ccaa"],
    "apron": ["2e1e14", "4a3020", "6e4a30", "8e6a48"],
    "wood": ["4a2a14", "7a4a26", "c8904a", "e8b874"],
}
LIP = "9a4a3a"
BG = ["0c120c", "121a12", "18241a"]


class Img:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.c = [[None] * w for _ in range(h)]
        self.r = [[0] * w for _ in range(h)]

    def set(self, x, y, col, region=0):
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < self.w and 0 <= y < self.h and col is not None:
            self.c[y][x] = col
            self.r[y][x] = region

    def get(self, x, y):
        x, y = int(math.floor(x)), int(math.floor(y))
        return self.c[y][x] if 0 <= x < self.w and 0 <= y < self.h else None

    def fill(self, inside, ramp, box, region=0, bias=0.0, strands=False):
        x0, y0, x1, y1 = box
        for y in range(max(0, int(y0) - 1), min(self.h, int(y1) + 2)):
            for x in range(max(0, int(x0) - 1), min(self.w, int(x1) + 2)):
                if inside(x + 0.5, y + 0.5):
                    t = ((x - x0) / max(1, x1 - x0) * 0.65 + (y - y0) / max(1, y1 - y0) * 0.35) + bias
                    if strands and (x * 7 + y // 3) % 5 == 0:
                        t += 0.25
                    k = 3 if t < 0.2 else 2 if t < 0.55 else 1 if t < 0.85 else 0
                    self.set(x, y, ramp[k], region)

    def ellipse(self, cx, cy, rx, ry, ramp, region=0, bias=0.0, strands=False):
        self.fill(lambda x, y: ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1, ramp, (cx - rx, cy - ry, cx + rx, cy + ry), region, bias, strands)

    def poly(self, pts, ramp, region=0, bias=0.0, strands=False):
        def inside(x, y):
            n, c = len(pts), False
            for i in range(n):
                (ax, ay), (bx, by) = pts[i], pts[(i + 1) % n]
                if (ay > y) != (by > y) and x < ax + (y - ay) * (bx - ax) / (by - ay):
                    c = not c
            return c
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        self.fill(inside, ramp, (min(xs), min(ys), max(xs), max(ys)), region, bias, strands)

    def line(self, x0, y0, x1, y1, col, region=0):
        n = int(max(abs(x1 - x0), abs(y1 - y0), 1))
        for k in range(n + 1):
            self.set(x0 + (x1 - x0) * k / n + 0.5, y0 + (y1 - y0) * k / n + 0.5, col, region)

    def outline(self, inner=True):
        """A dark line round every shape against the background, and between shapes of different regions."""
        out = [row[:] for row in self.c]
        for y in range(self.h):
            for x in range(self.w):
                if self.c[y][x] is None or self.c[y][x] in BG:
                    for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        n = self.get(x + ox, y + oy)
                        if n is not None and n not in BG and n != K:
                            out[y][x] = K
                            self.r[y][x] = 0
                            break
        self.c = out

    def background(self):
        for y in range(self.h):
            for x in range(self.w):
                d = math.hypot((x - self.w / 2) / self.w, (y - self.h * 0.4) / self.h)
                self.c[y][x] = BG[2] if d < 0.32 else BG[1] if d < 0.5 else BG[0]
        rng = random.Random(self.w * 31 + self.h)
        for _ in range(self.w * self.h // 40):
            x, y = rng.randrange(self.w), rng.randrange(self.h)
            self.c[y][x] = BG[min(2, BG.index(self.c[y][x]) + 1)]

    def rgba(self, scale=1):
        a = np.zeros((self.h, self.w, 4), np.float32)
        for y in range(self.h):
            for x in range(self.w):
                if self.c[y][x] is not None:
                    h = self.c[y][x]
                    a[y, x] = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1]
        return np.repeat(np.repeat(a, scale, 0), scale, 1)

    def regions(self):
        a = np.zeros((self.h, self.w, 4), np.float32)
        for y in range(self.h):
            for x in range(self.w):
                if self.r[y][x]:
                    a[y, x] = [self.r[y][x] / 255, 1, 0, 1]
                else:
                    a[y, x] = [0, 0, 0, 1]
        return a


def save(arr, path):
    h, w = arr.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = arr[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


# A head ----------------------------------------------------------------------------------------------------------
class Head:
    def __init__(self, cx, cy, rx, ry):
        self.cx, self.cy, self.rx, self.ry = cx, cy, rx, ry
        self.f = rx / 6.0

    def face(self, im, skin="skin", eyes="3a2a1a", jaw=0.0, glow=None, old=False, region=True, brows=None):
        cx, cy, rx, ry, f = self.cx, self.cy, self.rx, self.ry, self.f
        rs = SKIN if region else 0
        # The ears, then the face: round above, narrowing to the chin.
        for side in (-1, 1):
            im.ellipse(cx + side * rx * 0.98, cy + ry * 0.1, max(1.2, f * 1.3), max(1.6, f * 2), R[skin], rs, 0.1)
        w = 0.72 + jaw * 0.15
        im.poly([(cx - rx, cy - ry * 0.2), (cx - rx * 0.95, cy + ry * 0.45), (cx - rx * w * 0.8, cy + ry * 0.85), (cx, cy + ry * 1.08),
                 (cx + rx * w * 0.8, cy + ry * 0.85), (cx + rx * 0.95, cy + ry * 0.45), (cx + rx, cy - ry * 0.2)], R[skin], rs)
        im.ellipse(cx, cy - ry * 0.15, rx, ry * 0.85, R[skin], rs)
        # Eyes: a dark lash line, the white, the iris.
        ey = cy + ry * 0.12
        for side in (-1, 1):
            ex = cx + side * rx * 0.42
            if f < 1.5:
                im.set(ex, ey, glow or eyes, 0 if glow else (IRIS if region else 0))
                im.set(ex - side, ey, "e8e4dc" if not glow else glow)
                im.set(ex, ey - 1, K) if not glow else None
                if brows:
                    im.line(ex - 1, ey - 2, ex + 1, ey - 2, brows)
            else:
                w2 = int(round(f * 0.9))
                for dx in range(-w2, w2 + 1):
                    if abs(dx) < w2 or dx * side > 0:
                        im.set(ex + dx, ey - 1, K)
                    im.set(ex + dx, ey, "eee8e0" if not glow else glow)
                for dx in (0, side * -1) if not glow else ():
                    im.set(ex + dx, ey, eyes, IRIS if region else 0)
                if not glow:
                    im.set(ex, ey, "140c08", IRIS if region else 0)
                im.line(ex - w2 - 1, ey - 3, ex + w2, ey - 3 - (0.5 if side > 0 else 0), brows or R[skin][0])
        # The nose's shade, the mouth.
        ny = cy + ry * 0.45
        im.set(cx + 0.5, ny, R[skin][1], rs)
        if f >= 1.5:
            im.set(cx + 0.5, ny - 1, R[skin][1], rs)
            im.set(cx - 0.5, ny + 1, R[skin][1], rs)
            im.set(cx + 1.5, ny + 1, R[skin][0], rs)
        my = cy + ry * 0.72
        mw = max(1, rx * 0.28)
        im.line(cx - mw, my, cx + mw, my, LIP)
        if f >= 1.5:
            im.line(cx - mw + 1, my + 1, cx + mw - 1, my + 1, R[skin][1], rs)
        if old:
            im.set(cx - rx * 0.62, ey + 2, R[skin][1], rs)
            im.set(cx + rx * 0.62, ey + 2, R[skin][1], rs)

    def spots(self):
        """Where the painter's marks go (PortraitFaces): eyes, cheeks, brow, chin."""
        cx, cy, rx, ry = self.cx, self.cy, self.rx, self.ry
        ey = cy + ry * 0.12
        return {
            "eyes": [round(cx - rx * 0.42), round(ey), round(cx + rx * 0.42), round(ey)],
            "cheeks": [round(cx - rx * 0.5), round(cy + ry * 0.42), round(cx + rx * 0.5), round(cy + ry * 0.42)],
            "brow": [round(cx), round(cy - ry * 0.3)],
            "chin": [round(cx), round(cy + ry * 0.92)],
        }


# Hair: the part behind the head, then the part over it ------------------------------------------------------------
def hair_back(im, h, style, ramp, rg=HAIR):
    cx, cy, rx, ry = h.cx, h.cy, h.rx, h.ry
    r = R[ramp]
    if style in ("long", "wavy", "band"):
        im.poly([(cx - rx * 1.25, cy - ry * 0.4), (cx + rx * 1.25, cy - ry * 0.4), (cx + rx * 1.5, cy + ry * 2.3), (cx - rx * 1.5, cy + ry * 2.3)], r, rg, strands=True)
        im.ellipse(cx, cy - ry * 0.25, rx * 1.25, ry * 1.05, r, rg, strands=True)
    elif style == "curly":
        rng = random.Random(7)
        for _ in range(40):
            a = rng.uniform(0, math.tau)
            d = rng.uniform(0.5, 1.0)
            im.ellipse(cx + math.cos(a) * rx * 1.4 * d, cy + math.sin(a) * ry * 1.25 * d + ry * 0.2, rx * 0.4, rx * 0.4, r, rg)
    elif style == "ponytail":
        im.ellipse(cx + rx * 0.9, cy - ry * 0.7, rx * 0.5, ry * 0.45, r, rg)
        im.poly([(cx + rx * 0.9, cy - ry * 0.8), (cx + rx * 1.6, cy - ry * 0.2), (cx + rx * 1.7, cy + ry * 1.4), (cx + rx * 1.2, cy + ry * 1.8), (cx + rx * 1.1, cy)], r, rg, strands=True)
    elif style == "buns":
        for side in (-1, 1):
            im.ellipse(cx + side * rx * 0.75, cy - ry * 0.95, rx * 0.5, rx * 0.5, r, rg)
    elif style == "bun":
        im.ellipse(cx, cy - ry * 1.1, rx * 0.6, rx * 0.5, r, rg)


def hair_front(im, h, style, ramp, rg=HAIR):
    cx, cy, rx, ry = h.cx, h.cy, h.rx, h.ry
    r = R[ramp]
    if style in ("bald", "none"):
        return
    side = 0.3 if style in ("long", "braid", "shaved") else 0.0
    def cap(x, y):
        dx, dy = (x - cx) / (rx * 1.08), (y - (cy - ry * 0.15)) / (ry * 0.98)
        if dx * dx + dy * dy > 1:
            return False
        hairline = cy - ry * 0.32 + math.sin((x - cx) / rx * 3 + side * 3) * ry * 0.06 - (x - cx) / rx * side * ry * 0.25
        if style == "shaved" and x < cx - rx * 0.3:
            hairline = cy - ry * 0.7
        side_burn = abs(x - cx) > rx * 0.82 and y < cy + ry * 0.25
        return y < hairline or side_burn
    im.fill(cap, r, (cx - rx * 1.1, cy - ry * 1.15, cx + rx * 1.1, cy + ry * 0.3), rg, strands=True)
    if style in ("braid", "twin"):
        sides = (1,) if style == "braid" else (-1, 1)
        for s in sides:
            x, y = cx + s * rx * 1.0, cy + ry * 0.3
            for k in range(6):
                im.ellipse(x + s * k * 0.4, y + k * rx * 0.42, rx * 0.28, rx * 0.26, r, rg)
            im.set(x + s * 2.4, y + 6 * rx * 0.42, R["teal"][2])
    if style == "band":
        im.line(cx - rx * 1.05, cy - ry * 0.45, cx + rx * 1.05, cy - ry * 0.45, R["teal"][2])
        im.line(cx - rx * 1.0, cy - ry * 0.45 + 1, cx + rx * 1.0, cy - ry * 0.45 + 1, R["teal"][1])
    if style == "wavy":
        for k in range(3):
            im.set(cx - rx * 0.5 + k * rx * 0.5, cy - ry * 0.3, r[3], rg)


# The archer's bust: 66 x 81, shown at two menu pixels a pixel ----------------------------------------------------
ARCHERS = [
    # hair style, hair ramp, jaw, head size, hood
    ("braid", "hair", 0.0, 1.0, False),
    ("long", "black", 0.2, 1.0, False),
    ("ponytail", "ginger", 0.0, 0.97, False),
    ("short", "hair", 0.6, 1.0, False),
    ("curly", "black", 0.3, 1.0, False),
    ("buns", "hair", 0.0, 0.95, False),
    ("shaved", "blonde", 0.5, 1.0, False),
    ("twin", "ginger", 0.0, 0.95, False),
    ("band", "hair", 0.2, 1.02, False),
    ("short", "hair", 0.2, 1.0, True),
]


def archer(style, ramp, jaw, size, hood):
    im = Img(66, 81)
    im.background()
    h = Head(33, 33, 12 * size, 14 * size)
    # The bow behind her left shoulder, the quiver's arrows behind her right.
    for y in range(10, 81):
        u = (y - 50) / 40
        x = 9 + 7 * u * u
        im.set(x, y, R["wood"][2]); im.set(x + 1, y, R["wood"][1])
    im.line(15, 10, 9, 80, "dfe6ee")
    for k in range(3):
        x0 = 47 + k * 4
        im.line(x0, 52, x0 + 4, 30 + k * 2, R["wood"][2])
        im.poly([(x0 + 2, 26 + k * 2), (x0 + 6, 25 + k * 2), (x0 + 7, 32 + k * 2), (x0 + 4, 33 + k * 2)], R["red"])
    if hood:
        im.ellipse(33, 34, 19, 22, R["cloak"], 0, -0.1)
    hair_back(im, h, style, ramp)
    # Shoulders: a leather tunic under the green cloak, a strap across.
    im.poly([(2, 81), (5, 62), (14, 54), (25, 50), (41, 50), (52, 54), (61, 62), (64, 81)], R["leather"])
    im.poly([(28, 44), (38, 44), (39, 55), (27, 55)], R["skin"], SKIN, 0.45)
    im.poly([(2, 81), (4, 64), (13, 55), (24, 52), (30, 60), (22, 70), (14, 81)], R["cloak"])
    im.poly([(64, 81), (62, 64), (53, 55), (42, 52), (36, 60), (44, 70), (52, 81)], R["cloak"], 0, 0.15)
    im.poly([(24, 52), (33, 61), (42, 52), (40, 58), (33, 64), (26, 58)], R["cloak"], 0, -0.2)
    im.line(43, 55, 24, 81, R["leather"][0]); im.line(44, 55, 25, 81, R["leather"][1])
    im.set(35, 69, R["gold"][3]); im.set(36, 69, R["gold"][2])
    h.face(im, jaw=jaw)
    hair_front(im, h, style, ramp)
    if hood:
        def hood_shape(x, y):
            dx, dy = (x - 33) / 17, (y - 31) / 19
            inner = ((x - 33) / 12.5) ** 2 + ((y - 34) / 16) ** 2 < 1
            return dx * dx + dy * dy <= 1 and not inner and y < 50
        im.fill(hood_shape, R["cloak"], (16, 12, 50, 50))
    im.outline()
    return im, h


# The keepers: 35 x 32, shown at two menu pixels a pixel (written at 70 x 64) --------------------------------------
def keeper(name):
    im = Img(35, 32)
    im.background()
    h = Head(17.5, 12.5, 6, 7)
    if name == "brunhilde":
        im.poly([(0, 32), (2, 24), (9, 20), (26, 20), (33, 24), (35, 32)], R["steel"])
        im.ellipse(4, 23, 5, 4, R["steel"], 0, -0.2); im.ellipse(31, 23, 5, 4, R["steel"], 0, 0.1)
        im.poly([(14, 19), (21, 19), (20, 23), (15, 23)], R["skin"])
        h.face(im, jaw=0.4, region=False, brows="7a5a1e")
        hair_front(im, h, "twin", "blonde", 0)
        im.poly([(10, 9), (11, 3), (17.5, 1), (24, 3), (25, 9), (24, 11), (11, 11)], R["steel"])
        im.line(17.5, 1, 17.5, 10, R["gold"][2])
        for x in (10.5, 24.5):
            im.poly([(x - 1.5, 10), (x + 1.5, 10), (x + 1, 17), (x - 1, 17)], R["steel"], 0, 0.2)
        im.line(6, 27, 29, 27, R["red"][2])
    elif name == "miranda":
        im.ellipse(17.5, 14, 10, 11, R["white"])
        im.poly([(0, 32), (3, 23), (10, 20), (25, 20), (32, 23), (35, 32)], R["white"], 0, 0.2)
        im.ellipse(17.5, 14, 7, 8.5, ["0c0c10"] * 4)
        h.face(im, region=False, brows="7a4422")
        hair_front(im, h, "long", "hair", 0)
        im.line(11, 7, 24, 7, R["gold"][2]); im.set(17.5, 7, R["teal"][2])
        im.line(17.5, 22, 17.5, 32, R["gold"][1])
        im.poly([(15, 26), (20, 26), (19, 31), (16, 31)], R["gold"])
    elif name == "sylthara":
        im.ellipse(17.5, 13, 11, 12, R["navy"])
        im.poly([(0, 32), (3, 22), (10, 19), (25, 19), (32, 22), (35, 32)], R["navy"], 0, 0.1)
        im.ellipse(17.5, 14, 6.8, 8.5, ["06060c"] * 4)
        Head(17.5, 14, 5.5, 6.5).face(im, skin="pale", glow="c8f0ff", region=False)
        im.ellipse(17.5, 9.5, 6.5, 3.5, R["navy"], 0, -0.1)
        im.set(17.5, 25, R["teal"][2]); im.set(16.5, 25, R["teal"][3])
    elif name == "garrett":
        im.poly([(0, 32), (2, 23), (10, 20), (25, 20), (33, 23), (35, 32)], R["red"])
        im.line(17.5, 21, 17.5, 32, R["red"][0])
        for y in (24, 27, 30):
            im.set(19, y, R["gold"][3])
        h.face(im, jaw=0.8, region=False, old=True, brows="2a160c")
        im.poly([(11, 14), (24, 14), (24, 18), (21, 22), (17.5, 23), (14, 22), (11, 18)], R["black"], 0, 0.2)
        im.ellipse(17.5, 17.5, 2.6, 0.9, R["skin"])
        im.line(15, 18.5, 20, 18.5, LIP)
        im.ellipse(17.5, 5, 6.5, 4.5, R["red"])
        im.poly([(3, 4), (17.5, 9), (32, 4), (28, 9), (17.5, 11), (7, 9)], R["red"], 0, 0.15)
        im.line(5, 6, 17.5, 10, R["gold"][2]); im.line(30, 6, 17.5, 10, R["gold"][2])
    elif name == "thorin":
        im.poly([(0, 32), (1, 22), (9, 18), (26, 18), (34, 22), (35, 32)], R["skin"])
        im.poly([(7, 32), (8, 22), (27, 22), (28, 32)], R["apron"])
        im.line(9, 22, 4, 18, R["apron"][1]); im.line(26, 22, 31, 18, R["apron"][1])
        h.face(im, jaw=1.0, region=False, old=True, brows="963016")
        im.poly([(11, 14), (24, 14), (25, 20), (21, 28), (17.5, 30), (14, 28), (10, 20)], R["ginger"], 0, 0, True)
        im.ellipse(17.5, 17.5, 2.4, 0.9, R["skin"])
        im.line(15.5, 18.5, 19.5, 18.5, LIP)
        for y in (24, 27):
            im.line(16, y, 19, y, R["gold"][2])
        im.set(13, 9, R["skin"][3])
    elif name == "elara":
        im.poly([(0, 32), (3, 23), (10, 20), (25, 20), (32, 23), (35, 32)], R["navy"])
        im.poly([(14, 19), (21, 19), (20, 22), (15, 22)], R["skin"])
        hair_back(im, h, "bun", "black", 0)
        h.face(im, region=False, brows="1e1e26")
        hair_front(im, h, "short", "black", 0)
        for ex in (15, 20):
            im.set(ex - 1, 13, R["gold"][2]); im.set(ex + 1, 13, R["gold"][2])
            im.set(ex, 12, R["gold"][2]); im.set(ex, 14, R["gold"][2])
        im.line(16, 13, 19, 13, R["gold"][1])
        im.line(14, 22, 17.5, 27, R["gold"][1]); im.line(21, 22, 17.5, 27, R["gold"][1])
        im.poly([(16.5, 27), (18.5, 27), (18.5, 31), (16.5, 31)], R["gold"])
    elif name == "lyriel":
        im.ellipse(17.5, 13, 10, 11, R["cloak"])
        im.poly([(0, 32), (2, 22), (9, 19), (26, 19), (33, 22), (35, 32)], R["cloak"], 0, 0.1)
        for x in range(1, 35, 3):
            im.ellipse(x, 21 + (x % 2), 2.5, 2.2, R["fur"])
        im.ellipse(17.5, 14, 6.8, 8.5, ["0c120c"] * 4)
        h.face(im, region=False, brows="963016")
        hair_front(im, h, "long", "ginger", 0)
        for side in (-1, 1):
            im.poly([(17.5 + side * 5.5, 12), (17.5 + side * 8.5, 9), (17.5 + side * 6.5, 14)], R["skin"])
        im.ellipse(17.5, 8, 6.5, 3, R["cloak"], 0, -0.1)
        im.set(14, 16, R["teal"][2]); im.set(21, 16, R["teal"][2])
    im.outline()
    return im


previews = []
for i, spec in enumerate(ARCHERS, 1):
    im, head = archer(*spec)
    base = os.path.join(FACES, f"archer_{i}")
    save(im.rgba(), base + ".png")
    save(im.regions(), base + "_regions.png")
    spots = head.spots()
    spots["comment"] = "A base face in the 16x16 style (portraits_16.py, 2026-11-08). Pixels from the top left, as x, y pairs: the irises' centres, the cheeks (where paint and freckles go), the brow and the chin."
    with open(base + ".json", "w") as f:
        json.dump(spots, f, indent=2)
    previews.append(im.rgba(2))
    if i == 1:
        save(im.rgba(2), os.path.join(PIXEL, "portrait_archer.png"))
for name in ("brunhilde", "miranda", "sylthara", "garrett", "thorin", "elara", "lyriel"):
    arr = keeper(name).rgba(2)
    save(arr, os.path.join(PIXEL, f"portrait_{name}.png"))
    previews.append(np.repeat(np.repeat(arr, 2, 0), 2, 1)[:162, :])

PAD = np.array([0.05, 0.06, 0.05, 1], np.float32)
row1 = np.concatenate([np.concatenate([p, np.tile(PAD, (p.shape[0], 4, 1))], 1) for p in previews[:10]], 1)
rest = [np.concatenate([np.concatenate([p, np.tile(PAD, (162 - p.shape[0], p.shape[1], 1))], 0), np.tile(PAD, (162, 4, 1))], 1) for p in previews[10:]]
row2 = np.concatenate(rest, 1)
w = max(row1.shape[1], row2.shape[1])
pad = lambda r: np.concatenate([r, np.tile(PAD, (r.shape[0], w - r.shape[1], 1))], 1)
save(np.repeat(np.repeat(np.concatenate([pad(row1), np.tile(PAD, (4, w, 1)), pad(row2)], 0), 2, 0), 2, 1),
     os.path.join(P, "ArtSource", "pixel", "previews", "portraits_16.png"))
print("wrote portraits")
