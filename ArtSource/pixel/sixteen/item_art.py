# The item pictures in the 16x16 style (2026-11-08, the owner: "redraw the effects and UI in 16x16 too"): every picture
# in Resources/UI/ItemArt (ItemArtNames) drawn from shapes by code, 16 pixels a backpack cell (a bow 2 x 4 cells is 32 x 64),
# one palette, light from the top left, dark outlines, as the forest and the characters. Each armour tier takes the
# colours of her chest look of that tier (wild_arrow_16.py LOOKS); legendaries have their own colours and a mark.
# Writes over the old pictures (same names) and a preview, ArtSource/pixel/previews/item_art_16.png.
# Run: Blender -b -P ArtSource/pixel/sixteen/item_art.py
import bpy, os, math
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(P, "Assets", "_Project", "Resources", "UI", "ItemArt")
K = "1a1018"

R = {
    "leather": ["3e2414", "6a3a22", "a0623a", "c88a58"],
    "brigand": ["3a1814", "4e221c", "8a3f32", "b0604a"],
    "bronze": ["4a3410", "6e5420", "b8923e", "e6c66e"],
    "steel": ["2e343e", "5c6676", "a9b3c0", "e4e9f0"],
    "bright": ["4a5468", "7a8498", "c4ccd8", "f4f7fb"],
    "gold": ["6a4e14", "a07a22", "e8c24a", "fbe68a"],
    "red": ["5a1010", "9a1f18", "e33a2e", "f6806a"],
    "teal": ["0e4a48", "17837f", "2fd4c4", "a8f4ea"],
    "wood": ["4a2a14", "7a4a26", "c8904a", "e8b874"],
    "dark": ["16121a", "1e1a20", "3a3238", "5a5058"],
    "bone": ["6a5e4a", "a89c84", "d8ceb8", "f6f0e2"],
    "cloth": ["2e3a2a", "4a5a3a", "6e8250", "98aa70"],
    "green": ["14301e", "1e5a32", "3e8a44", "8ac85a"],
    "copper": ["4e2410", "8a4a22", "c8783a", "f0aa6a"],
    "iron": ["2a2a30", "4a4a54", "7a7a86", "aaaab4"],
    "silver": ["4a5260", "8a94a4", "c8d0dc", "ffffff"],
    "rope": ["5a4426", "8a6c3e", "c0a066", "e0c88e"],
    "violet": ["2a1040", "4e2478", "8a4ac8", "c8a0f4"],
    "ember": ["5a1a08", "b8401a", "ff7a2a", "ffd060"],
    "ice": ["2a4a6a", "4a7aa0", "9ac8e8", "e8f8ff"],
    "fur": ["4a4a4e", "7a7a80", "b4b4ba", "e4e4e8"],
    "black": ["0e0e12", "1e1e26", "34343e", "5a5a66"],
    "tan": ["5a3a1e", "7a5530", "c99a5a", "e8c48a"],
    "parch": ["8a7450", "c0a878", "e8d8b0", "fff6dc"],
    "ink": ["1e3a6a", "3a6aa8"],
}
STRING = "dfe6ee"
# Each ramp colour's ramp and step, for the bevel (the first ramp a colour appears in).
STEP = {}
for _ramp in R.values():
    if len(_ramp) == 4:
        for _i, _c in enumerate(_ramp):
            STEP.setdefault(_c, (_ramp, _i))


def hexc(h):
    return [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1.0]


class Img:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.c = [[None] * w for _ in range(h)]

    def set(self, x, y, col):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h and col is not None:
            self.c[y][x] = col

    def get(self, x, y):
        x, y = int(round(x)), int(round(y))
        return self.c[y][x] if 0 <= x < self.w and 0 <= y < self.h else None

    def fill(self, inside, ramp, box=None, bias=0.0):
        """Every pixel where inside(x, y), shaded by a light from the top left across the shape's box."""
        pts = [(x, y) for y in range(self.h) for x in range(self.w) if inside(x + 0.5, y + 0.5)]
        if not pts:
            return
        x0, y0, x1, y1 = box or (min(p[0] for p in pts), min(p[1] for p in pts), max(p[0] for p in pts), max(p[1] for p in pts))
        for x, y in pts:
            t = ((x - x0) / max(1, x1 - x0) * 0.6 + (y - y0) / max(1, y1 - y0) * 0.4) + bias
            k = 3 if t < 0.18 else 2 if t < 0.55 else 1 if t < 0.85 else 0
            self.set(x, y, ramp[min(k, len(ramp) - 1)])

    def rect(self, x0, y0, x1, y1, ramp, bias=0.0):
        self.fill(lambda x, y: x0 <= x < x1 + 1 and y0 <= y < y1 + 1, ramp, (x0, y0, x1, y1), bias)

    def ellipse(self, cx, cy, rx, ry, ramp, bias=0.0):
        self.fill(lambda x, y: ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1, ramp, (cx - rx, cy - ry, cx + rx, cy + ry), bias)

    def poly(self, pts, ramp, bias=0.0):
        def inside(x, y):
            n, c = len(pts), False
            for i in range(n):
                (ax, ay), (bx, by) = pts[i], pts[(i + 1) % n]
                if (ay > y) != (by > y) and x < ax + (y - ay) * (bx - ax) / (by - ay):
                    c = not c
            return c
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        self.fill(inside, ramp, (min(xs), min(ys), max(xs), max(ys)), bias)

    def line(self, x0, y0, x1, y1, col):
        n = int(max(abs(x1 - x0), abs(y1 - y0), 1))
        for k in range(n + 1):
            self.set(x0 + (x1 - x0) * k / n, y0 + (y1 - y0) * k / n, col)

    def clear(self, inside):
        for y in range(self.h):
            for x in range(self.w):
                if inside(x + 0.5, y + 0.5):
                    self.c[y][x] = None

    def round_shade(self, ramp, x0=0, x1=None):
        """A ramp's pixels shaded round across each row between x0 and x1 (2026-11-10): lit at the left of what the row
        holds of it, in shadow at its right, as a rounded surface under a light from the top left."""
        x1 = self.w - 1 if x1 is None else x1
        for y in range(self.h):
            xs = [x for x in range(x0, x1 + 1) if self.c[y][x] in ramp]
            if len(xs) < 3:
                continue
            a, b = min(xs), max(xs)
            for x in xs:
                f = (x - a) / max(1, b - a)
                self.c[y][x] = ramp[3] if f < 0.16 else ramp[2] if f < 0.55 else ramp[1] if f < 0.85 else ramp[0]
        return self

    def bevel(self):
        """Volume for a flat-filled piece (2026-11-10): a pixel whose neighbour up or left is clear steps a tone lighter
        in its ramp, one whose neighbour down or right is clear a tone darker (light from the top left)."""
        out = [row[:] for row in self.c]
        for y in range(self.h):
            for x in range(self.w):
                col = self.c[y][x]
                if col is None or col not in STEP:
                    continue
                ramp, i = STEP[col]
                lit = self.get(x - 1, y) is None or self.get(x, y - 1) is None
                dark = self.get(x + 1, y) is None or self.get(x, y + 1) is None
                if lit and not dark:
                    out[y][x] = ramp[min(len(ramp) - 1, i + 1)]
                elif dark and not lit:
                    out[y][x] = ramp[max(0, i - 1)]
        self.c = out
        return self

    def outline(self):
        out = [row[:] for row in self.c]
        for y in range(self.h):
            for x in range(self.w):
                if self.c[y][x] is None:
                    for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        n = self.get(x + ox, y + oy)
                        if n is not None and n != K:
                            out[y][x] = K
                            break
        self.c = out
        return self

    def rgba(self):
        a = np.zeros((self.h, self.w, 4), np.float32)
        for y in range(self.h):
            for x in range(self.w):
                if self.c[y][x] is not None:
                    a[y, x] = hexc(self.c[y][x])
        return a


def save(arr, path):
    h, w = arr.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = arr[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


def gem(im, x, y, ramp, big=False):
    if big:
        im.rect(x - 1, y - 1, x + 1, y + 1, ramp)
        im.set(x - 1, y - 1, ramp[3])
    else:
        im.set(x, y, ramp[2])
        im.set(x - 1, y, ramp[3])
        im.set(x + 1, y, ramp[1])


# Bows: 32 x 64, the limb bowed to the left, the string on the right -----------------------------------------------
def bow(limb, length=58, bulge=9, thick=3, grip="dark", tips=None, recurve=0.0, bands=(), string=STRING, mark=None):
    """A bow standing upright (redrawn 2026-11-10, the owner: the items again, the bows first): the limbs bowed to the
    left, thick at the grip and tapering to slim tips, four tones across each limb lit from the top left (the back of
    the limb bright, its belly in shadow), the upper limb lighter than the lower; a wrapped grip a pixel wider with an
    arrow shelf above it; capped tips (tips) curling back on a recurve, the string knotted on at each."""
    im = Img(32, 64)
    ramp = R[limb]
    # Bolder than the first drawing (2026-11-10): a deeper bow that fills the slot's width, a limb a pixel thicker.
    bulge, thick = bulge * 1.55, thick + 1
    top = (64 - length) // 2
    bot = top + length - 1
    mid = (top + bot) / 2
    half = (bot - top) / 2
    sx = 23
    centres, widths = {}, {}
    for y in range(top, bot + 1):
        u = (y - mid) / half
        x = sx - bulge * (1 - u * u) - 1
        if recurve and abs(u) > 0.55:
            # The tips curl forward, away from her, past the line of the string's ends.
            x += recurve * 1.8 * ((abs(u) - 0.55) / 0.45) ** 2
        w = max(2, round((thick + 1) - (thick - 1) * abs(u) ** 1.3))
        centres[y], widths[y] = x, w
    # The limbs: across each row from the back (left, lit) to the belly (right, in shadow); the lower limb a tone darker.
    for y in range(top, bot + 1):
        x, w = centres[y], widths[y]
        lower = y > mid + 1
        for k in range(w):
            f = k / max(1, w - 1)
            tone = 3 if f < 0.2 else 2 if f < 0.6 else 1
            if lower:
                tone = max(0, tone - 1) if f > 0.2 else 2
            if w >= 3 and k == w - 1:
                tone = 0
            im.set(round(x - w / 2 + k), y, ramp[tone])
    # Grain: a darker fleck now and then along the back of the limbs.
    for y in range(top + 3, bot - 2, 6):
        if abs(y - mid) > 5:
            im.set(round(centres[y] - widths[y] / 2 + 1), y, ramp[1])
    # The string from tip to tip, knotted on at each.
    xt, xb = round(centres[top] + widths[top] / 2), round(centres[bot] + widths[bot] / 2)
    for y in range(top + 1, bot):
        im.set(round(xt + (xb - xt) * (y - top) / (bot - top)), y, string)
    # The grip, a pixel wider, its wrap in two tones with a lit column, and the arrow shelf above it.
    g = R[grip]
    for y in range(int(mid) - 4, int(mid) + 5):
        x, w = centres[y], widths[y] + 1
        for k in range(w):
            col = g[3] if k == 0 else g[2] if (y + k) % 3 else g[1]
            if k == w - 1:
                col = g[0]
            im.set(round(x - w / 2 + k), y, col)
    shelf = int(mid) - 5
    im.set(round(centres[shelf] + widths[shelf] / 2), shelf, ramp[3])
    # The tips: caps of the tip colour, two pixels, the nock lit.
    tr = R[tips] if tips else ramp
    for t, d in ((top, 1), (bot, -1)):
        for k in range(3):
            yy = t + d * k
            x = centres[yy]
            im.set(round(x - 1), yy, tr[3 if k == 0 else 2])
            im.set(round(x), yy, tr[2 if k == 0 else 1])
        im.set(round(centres[t]) + 1, t, string)
    for frac, b in bands:
        y = int(top + frac * (bot - top))
        for yy in (y, y + 1):
            x, w = centres[yy], widths[yy] + 1
            for k in range(w):
                im.set(round(x - w / 2 + k), yy, R[b][3 if k == 0 else 2 if yy == y else 1])
    if mark:
        mark(im, centres, top, bot, mid)
    return im.outline()


def glow_pts(im, pts, ramp):
    for x, y in pts:
        im.set(x, y, ramp[3])
        for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if im.get(x + ox, y + oy) is None:
                im.set(x + ox, y + oy, ramp[2])


def m_splinter(im, c, top, bot, mid):
    for y in range(top + 6, bot - 4, 7):
        x = c[y]
        im.line(x - 1, y, x - 4, y - 2, R["green"][2])
        im.set(x - 4, y - 3, R["green"][3])


def m_ember(im, c, top, bot, mid):
    for y in range(top + 3, bot - 2, 5):
        im.set(c[y] - 2, y, R["ember"][3])
        im.set(c[y] - 3, y - 1, R["ember"][2])


def m_promise(im, c, top, bot, mid):
    y = int(mid)
    gem(im, c[y] - 3, y, R["red"], True)


def m_gale(im, c, top, bot, mid):
    for y in (top + 8, bot - 8):
        im.line(c[y] - 1, y, c[y] - 6, y + (3 if y < mid else -3), R["teal"][3])
        im.line(c[y] - 2, y + 1, c[y] - 5, y + (4 if y < mid else -2), R["teal"][2])


def m_widow(im, c, top, bot, mid):
    for y in range(top + 4, bot - 3, 6):
        im.set(c[y] - 2, y, R["red"][2])
    y = int(mid)
    im.rect(c[y] - 4, y - 1, c[y] - 3, y + 1, R["red"])


def m_gallows(im, c, top, bot, mid):
    y = top + 4
    for k in range(6):
        im.set(c[y] + 2, y + k, R["rope"][2 if k % 2 else 1])
    im.ellipse(c[y] + 2, y + 8, 1.6, 1.6, R["rope"])
    im.clear(lambda x, yy: (x - c[y] - 2.5) ** 2 + (yy - y - 8.5) ** 2 < 0.6)


def m_still(im, c, top, bot, mid):
    for y in range(top + 2, bot, 9):
        im.set(c[y] - 2, y, R["ice"][3])
    gem(im, c[int(mid)] - 3, int(mid), R["ice"], True)


def m_silence(im, c, top, bot, mid):
    for y in range(top + 5, bot - 3, 8):
        im.set(c[y] - 2, y, R["violet"][3])
    gem(im, c[int(mid)] - 3, int(mid), R["violet"], True)


# Quivers: 32 x 48, a tube leaning to the right, arrows showing at the top -----------------------------------------
def quiver(body, trim="leather", fletch="red", studs=None, ribs=False, arrows=4, mark=None, shafts="wood"):
    """A quiver leaning to the right (redrawn 2026-11-10): arrows standing out of it with two-tone fletching and their
    nocks, the tube shaded round (lit on the left, in shadow on the right), a stitched seam down it, a rim and a rounded
    base in the trim, a strap across it with a buckle."""
    im = Img(32, 48)
    lean = 0.22
    def at(y):
        return (y - 14) * lean
    for k in range(arrows + 1 if arrows else 0):
        x0 = 8 + k * 3.0
        tipy = 4 + (k % 2) * 2 + (k % 3)
        im.line(x0 + 2, 15, x0, tipy + 4, R[shafts][2])
        f = R[fletch]
        im.rect(x0 - 1, tipy, x0 - 1, tipy + 3, [f[1]] * 4)
        im.rect(x0, tipy, x0, tipy + 3, [f[3]] * 4)
        im.rect(x0 + 1, tipy + 1, x0 + 1, tipy + 3, [f[2]] * 4)
        im.set(x0, tipy - 1, R["bone"][2])
    b = R[body]
    for y in range(13, 46):
        x0, x1 = 7 + at(y), 21 + at(y)
        if y >= 43:
            x0, x1 = x0 + (y - 42), x1 - (y - 42)
        for x in range(int(round(x0)), int(round(x1)) + 1):
            f = (x - x0) / max(1, x1 - x0)
            im.set(x, y, b[3] if f < 0.18 else b[2] if f < 0.55 else b[1] if f < 0.85 else b[0])
    t = R[trim]
    for y in range(12, 17):
        for x in range(int(round(6 + at(y))), int(round(22 + at(y))) + 1):
            im.set(x, y, t[3] if y == 12 else t[2] if x < 14 + at(y) else t[1])
    for y in range(40, 44):
        for x in range(int(round(7 + at(y))), int(round(21 + at(y))) + 1):
            im.set(x, y, t[2] if y == 40 else t[1])
    for y in range(18, 39, 2):
        im.set(16 + at(y), y, b[0])
    im.line(5, 18, 25, 36, t[1])
    im.line(5, 19, 25, 37, t[2])
    im.rect(14, 26, 16, 28, R["iron"])
    if studs:
        for y in range(20, 40, 5):
            for x in (10, 19):
                im.set(x + at(y), y, R[studs][3])
                im.set(x + 1 + at(y), y + 1, R[studs][1])
    if ribs:
        for y in range(20, 39, 4):
            im.line(8 + at(y), y, 20 + at(y), y, R["bone"][1])
            im.line(8 + at(y), y + 1, 20 + at(y), y + 1, R["bone"][0])
    if mark:
        mark(im, at)
    return im.outline()


def q_splinters(im, at):
    for k in range(5):
        x = 8 + k * 3
        im.line(x + 1, 12, x - 2, 1, R["green"][2])
        im.set(x - 2, 0, R["green"][3])
    glow_pts(im, [(13 + at(28), 28), (16 + at(34), 34)], R["green"])


def q_ashfall(im, at):
    for x, y in ((12, 22), (17, 30), (11, 36), (18, 40)):
        im.set(x + at(y), y, R["ember"][3])
        im.set(x + at(y) + 1, y, R["ember"][2])


def q_wind(im, at):
    for y in (24, 34):
        im.line(4 + at(y), y, 24 + at(y), y - 3, R["teal"][3])
    im.set(26, 8, R["teal"][3]); im.set(27, 7, R["teal"][2])


def q_magpie(im, at):
    for k, y in enumerate(range(20, 42, 4)):
        im.rect(9 + at(y), y, 18 + at(y), y + 1, R["fur"] if k % 2 else R["black"])
    gem(im, 14 + at(30), 30, R["teal"])


def q_hound(im, at):
    x, y = 14 + at(28), 28
    im.ellipse(x, y, 4, 3.2, R["bone"])
    im.set(x - 2, y - 1, K); im.set(x + 1, y - 1, K)
    im.rect(x - 1, y + 2, x + 1, y + 3, R["bone"])
    glow_pts(im, [(x - 2, y - 1), (x + 1, y - 1)], R["red"])


# Chest pieces: 32 x 48 -------------------------------------------------------------------------------------------
def chest(body, trim, accent=None, pattern=None, collar=None, sleeves=None, shoulders=None, stitched=False):
    """A chest piece laid flat (redrawn 2026-11-10): the body shaded round, lit down its left and in shadow down its
    right, the sleeves a tone darker behind it; shoulder plates on the heavier ones, stitching down the front of the
    leather ones, a darker hem and an edged neckline."""
    im = Img(32, 48)
    sl = sleeves or body
    im.poly([(2, 11), (8, 6), (10, 8), (8, 26), (2, 25)], R[sl], bias=0.15)
    im.poly([(30, 11), (24, 6), (22, 8), (24, 26), (30, 25)], R[sl], bias=0.35)
    im.poly([(8, 6), (13, 5), (16, 9), (19, 5), (24, 6), (24, 30), (25, 44), (7, 44), (8, 30)], R[body])
    # The body shaded round across its width.
    b = R[body]
    for y in range(5, 45):
        xs = [x for x in range(7, 26) if im.get(x, y) in b]
        if not xs:
            continue
        x0, x1 = min(xs), max(xs)
        for x in xs:
            f = (x - x0) / max(1, x1 - x0)
            im.set(x, y, b[3] if f < 0.14 else b[2] if f < 0.6 else b[1] if f < 0.88 else b[0])
    if stitched:
        for y in range(12, 42, 2):
            im.set(16, y, b[0])
    if shoulders:
        sh = R[shoulders]
        for cx, bias in ((6.5, 0.0), (25.5, 0.3)):
            im.ellipse(cx, 9.5, 4.6, 3.6, sh, bias)
            im.line(cx - 3.5, 11.5, cx + 3.5, 11.5, sh[0])
    im.rect(8, 28, 24, 31, R[trim])
    im.set(16, 29, R["gold"][3]); im.set(16, 30, R["gold"][2])
    if pattern:
        pattern(im)
    if accent:
        im.line(10, 7, 21, 27, R[accent][2])
        im.line(11, 7, 22, 27, R[accent][1])
    if collar:
        im.poly([(8, 6), (13, 4), (16, 9), (19, 4), (24, 6), (22, 10), (16, 12), (10, 10)], R[collar], bias=-0.2)
    im.clear(lambda x, y: abs(x - 16) < 2.2 and y < 9 - abs(x - 16))
    # The hem a tone darker, the neckline edged.
    for x in range(7, 26):
        col = im.get(x, 43)
        if col in STEP:
            ramp, i = STEP[col]
            im.set(x, 43, ramp[max(0, i - 1)])
    for x, y in ((13, 6), (14, 7), (15, 8), (17, 8), (18, 7), (19, 6)):
        if im.get(x, y) is not None:
            im.set(x, y, R[trim][3])
    return im.bevel().outline()


def p_scales(im):
    for y in range(10, 43, 3):
        for x in range(9 + (y // 3) % 2 * 2, 24, 4):
            if im.get(x, y) is not None:
                im.set(x, y, R["bronze"][3])
                im.set(x + 1, y + 1, R["bronze"][1])
                im.set(x - 1, y + 1, R["bronze"][1])


def p_plate(im):
    im.rect(13, 9, 19, 42, R["red"])
    for y in (16, 24, 36):
        im.line(9, y, 23, y, R["steel"][0])


def p_knight(im):
    im.rect(14, 12, 18, 26, R["gold"])
    im.rect(11, 16, 21, 19, R["gold"])
    im.line(8, 33, 24, 33, R["gold"][2])
    im.line(7, 43, 25, 43, R["gold"][2])


def p_brigand(im):
    for y in range(12, 42, 5):
        for x in range(10, 24, 4):
            if im.get(x, y) is not None:
                im.set(x, y, R["gold"][3])


def p_ranger(im):
    im.line(9, 33, 23, 33, R["leather"][0])
    im.rect(20, 34, 23, 38, R["leather"])


def p_cinder(im):
    for pts in (((10, 12), (14, 20), (12, 27)), ((20, 34), (17, 40), (21, 43)), ((22, 10), (19, 16))):
        for a, b in zip(pts, pts[1:]):
            im.line(a[0], a[1], b[0], b[1], R["ember"][2])
        im.set(pts[0][0], pts[0][1], R["ember"][3])


def p_stag(im):
    for y in range(34, 44, 3):
        im.set(11, y, R["bone"][2]); im.set(21, y + 1, R["bone"][2])


# Helms: 32 x 32 --------------------------------------------------------------------------------------------------
def helm(kind, ramp, trim=None):
    im = Img(32, 32)
    if kind == "hood":
        im.poly([(16, 2), (25, 8), (28, 22), (30, 30), (2, 30), (4, 22), (7, 8)], R[ramp])
        im.ellipse(16, 19, 6.5, 8, R["dark"], bias=0.6)
        im.ellipse(16, 20, 4, 5.5, ["0a080c"] * 4)
        if trim:
            im.line(4, 29, 28, 29, R[trim][2])
        return im.outline()
    if kind == "mask":
        im.poly([(5, 8), (16, 5), (27, 8), (27, 18), (21, 26), (11, 26), (5, 18)], R[ramp])
        for x in (11, 21):
            im.rect(x - 2, 13, x + 2, 15, ["0a080c"] * 4)
        im.line(16, 9, 16, 22, R[ramp][1])
        if trim:
            im.line(5, 9, 27, 9, R[trim][2])
            im.line(7, 20, 12, 24, R[trim][2]); im.line(25, 20, 20, 24, R[trim][2])
        return im.outline()
    im.poly([(6, 10), (10, 4), (16, 3), (22, 4), (26, 10), (27, 28), (5, 28)], R[ramp])
    if kind == "barbute":
        im.rect(10, 12, 22, 14, ["0a080c"] * 4)
        im.rect(14, 14, 18, 28, ["0a080c"] * 4)
        im.line(16, 3, 16, 11, R[ramp][3])
    elif kind == "visored":
        im.poly([(5, 11), (27, 11), (28, 24), (16, 27), (4, 24)], R[ramp], bias=0.15)
        im.line(8, 15, 24, 15, "0a080c"); im.line(8, 16, 24, 16, "0a080c")
        for x in range(10, 24, 3):
            im.set(x, 20, "0a080c")
        if trim:
            im.poly([(16, 3), (20, 0), (24, 1), (18, 6)], R[trim])
    if kind in ("barbute", "visored", "horned"):
        im.round_shade(R[ramp])
        for x in range(8, 26, 4):
            if im.get(x, 26) is not None:
                im.set(x, 26, R[ramp][3]); im.set(x, 27, R[ramp][0])
    if kind == "horned":
        im.rect(10, 13, 22, 15, ["0a080c"] * 4)
        im.rect(15, 15, 17, 22, R[ramp], bias=0.3)
        for side in (-1, 1):
            pts = [(16 + side * 9, 9), (16 + side * 13, 6), (16 + side * 15, 1), (16 + side * 13, 0), (16 + side * 11, 4), (16 + side * 8, 6)]
            im.poly(pts, R["bone"])
        if trim:
            im.line(5, 27, 27, 27, R[trim][2])
    return im.bevel().outline()


def falconer():
    im = helm("hood", "cloth", "gold")
    im.poly([(20, 6), (29, 0), (31, 2), (24, 9)], R["tan"])
    im.poly([(22, 8), (30, 5), (31, 8), (24, 11)], R["fur"])
    im.set(29, 1, R["bone"][3])
    return im.outline()


def unblinking():
    im = Img(32, 32)
    im.rect(4, 14, 27, 24, R["gold"])
    for x in (5, 11, 16, 21, 26):
        im.poly([(x - 2, 15), (x, 6 if x == 16 else 9), (x + 2, 15)], R["gold"])
    im.ellipse(16, 19, 4, 3, R["bone"])
    im.ellipse(16, 19, 1.8, 2.2, R["teal"])
    im.set(16, 19, K)
    for x in (8, 24):
        gem(im, x, 19, R["red"])
    return im.outline()


# Boots: 32 x 32, a pair -----------------------------------------------------------------------------------------
def boots(ramp, kind="leather", trim=None):
    im = Img(32, 32)
    for ox, bias in ((2, 0.0), (17, 0.25)):
        top = 4 if kind in ("sabatons", "spiked", "strapped") else 7
        im.poly([(ox + 2, top), (ox + 10, top), (ox + 10, 21), (ox + 13, 23), (ox + 14, 28), (ox + 1, 28), (ox + 1, 21)], R[ramp], bias)
        im.line(ox + 1, 28, ox + 14, 28, R[ramp][0])
        # Shaded round first, so the wraps, straps and plates drawn on it keep their own tones.
        im.round_shade(R[ramp], ox, ox + 14)
        if kind == "wrapped":
            for y in range(top + 2, 24, 3):
                im.line(ox + 1, y, ox + 10, y + 1, R[ramp][3])
        elif kind == "strapped":
            for y in (9, 15, 21):
                im.line(ox + 1, y, ox + 10, y, R["leather"][0])
                im.set(ox + 8, y, R["gold"][3])
        elif kind == "sabatons":
            for y in range(top + 3, 27, 4):
                im.line(ox + 1, y, ox + 13, y, R[ramp][0])
        elif kind == "spiked":
            for y in (8, 14, 20):
                im.poly([(ox + 10, y - 1), (ox + 14, y), (ox + 10, y + 1)], R["bone"])
            im.poly([(ox + 13, 26), (ox + 16, 28), (ox + 13, 28)], R["bone"])
        if trim:
            im.rect(ox + 1, top, ox + 10, top + 2, R[trim])
        else:
            im.line(ox + 1, top + 1, ox + 10, top + 1, R[ramp][3])
        im.line(ox + 1, 29, ox + 14, 29, "2a1a12")
    for ox in (2, 17):
        im.set(ox + 12, 24, R[ramp][3]); im.set(ox + 13, 25, R[ramp][2])
    return im.bevel().outline()


def windrunner():
    im = boots("teal", "leather", "silver")
    for ox in (2, 17):
        im.poly([(ox + 1, 10), (ox - 2, 5), (ox - 1, 3), (ox + 3, 8)], R["bone"])
        im.poly([(ox + 1, 14), (ox - 2, 10), (ox, 9), (ox + 3, 12)], R["bone"])
    return im.outline()


def stalker():
    im = boots("black", "strapped")
    for ox in (2, 17):
        for y in (9, 15, 21):
            im.set(ox + 8, y, R["green"][3])
    return im.outline()


# Gloves: 32 x 32, one hand ---------------------------------------------------------------------------------------
def glove(ramp, kind="gloves", trim=None, cuff=None):
    im = Img(32, 32)
    for k, (x, top) in enumerate(((10, 4), (14, 2), (18, 3), (22, 6))):
        im.rect(x - 1, top, x + 1, 14, R[ramp], 0.1 * k)
        if kind == "claws":
            im.poly([(x - 1, top), (x, top - 4), (x + 1, top)], R["bone"])
        if kind == "gauntlets":
            for y in range(top + 3, 14, 3):
                im.line(x - 1, y, x + 1, y, R[ramp][0])
    im.poly([(8, 12), (24, 12), (24, 21), (20, 24), (11, 24), (8, 20)], R[ramp])
    im.poly([(8, 14), (4, 9), (2, 10), (5, 17), (9, 20)], R[ramp], 0.1)
    c = cuff or ramp
    long_ = kind in ("bracers", "gauntlets")
    im.rect(9, 23, 23, 31 if long_ else 28, R[c], 0.2)
    if kind == "wraps":
        for y in range(6, 28, 3):
            im.line(8, y + 1, 24, y, R[ramp][3])
    if kind == "bracers":
        for y in (25, 29):
            im.line(9, y, 23, y, R["leather"][0])
            im.set(20, y, R["gold"][3])
    if trim:
        im.line(9, 23, 23, 23, R[trim][2])
    for x in (12, 16, 20):
        for y in range(6, 13):
            if im.get(x, y) is not None and im.get(x, y - 2) is not None:
                im.set(x, y, R[ramp][0])
    for x in (10, 14, 18, 22):
        im.set(x, 14, R[ramp][3])
    return im.bevel().outline()


def fletcher():
    im = glove("green", "gloves", "gold", "leather")
    im.poly([(24, 26), (30, 16), (31, 18), (26, 28)], R["red"])
    im.line(24, 28, 31, 16, R["bone"][2])
    return im.outline()


def bloodletter():
    im = glove("red", "claws", None, "black")
    for x in (12, 16, 20):
        im.set(x, 26, R["red"][3])
    return im.outline()


# Belts: 32 x 16 -------------------------------------------------------------------------------------------------
def belt(ramp, kind="plain", buckle="iron"):
    im = Img(32, 16)
    if kind == "rope":
        for x in range(1, 31):
            im.set(x, 6 + (x % 3 == 0), R[ramp][2])
            im.set(x, 7 + (x % 3 == 1), R[ramp][1])
            im.set(x, 8, R[ramp][1 if x % 2 else 0])
        im.ellipse(15, 8, 3, 2.5, R[ramp])
        im.line(14, 10, 12, 15, R[ramp][2]); im.line(16, 10, 18, 15, R[ramp][1])
        return im.outline()
    top, bot = (4, 10) if kind != "war_girdle" else (2, 12)
    im.rect(1, top, 30, bot, R[ramp])
    if kind == "studded":
        for x in range(4, 30, 4):
            im.set(x, (top + bot) // 2, R["silver"][3])
    if kind == "tassets":
        for x in (3, 10, 19, 26):
            im.poly([(x, bot), (x + 5, bot), (x + 4, 15), (x + 1, 15)], R["steel"])
    if kind == "pouch":
        im.rect(20, 7, 27, 14, R["leather"], 0.1)
        im.line(20, 9, 27, 9, R["leather"][0])
    im.rect(12, top - 1, 17, bot + 1, R[buckle])
    im.rect(14, top + 1, 15, bot - 1, R[ramp], 0.5)
    if kind == "war_girdle":
        im.ellipse(15, 7, 3, 3, R["bone"])
        im.set(14, 6, K); im.set(16, 6, K)
    for x in range(3, 30, 2):
        if im.get(x, top + 1) is not None and not 11 <= x <= 18:
            im.set(x, top + 1, R[ramp][3] if x % 4 == 1 else im.get(x, top + 1))
    return im.bevel().outline()


def bandolier():
    im = belt("brigand", "plain", "gold")
    for x in range(3, 30, 3):
        if 11 <= x <= 18:
            continue
        im.rect(x, 1, x + 1, 4, R["steel"], -0.2)
        im.set(x, 0, R["steel"][3])
    return im.outline()


# Jewellery and the blueprint: 16 x 16 -----------------------------------------------------------------------------
def ring(ramp, stone=None, signet=False):
    im = Img(16, 16)
    im.ellipse(8, 9.5, 6, 5, R[ramp])
    im.clear(lambda x, y: ((x - 8) / 3.6) ** 2 + ((y - 10) / 2.8) ** 2 <= 1)
    if signet:
        im.rect(5, 2, 10, 6, R["gold"])
        im.set(7, 4, R["fur"][2]); im.set(8, 3, R["fur"][3]); im.set(6, 3, R["fur"][3])
    elif stone:
        im.ellipse(8, 4.5, 2.6, 2.4, R[stone])
        im.set(7, 3, "ffffff")
    return im.outline()


def amulet(ramp, kind="disc", stone=None):
    im = Img(16, 16)
    chain = R["rope"][2] if kind == "tooth" else R[ramp][2]
    im.line(2, 1, 8, 8, chain); im.line(14, 1, 8, 8, chain)
    if kind == "tooth":
        im.poly([(6, 8), (10, 8), (8, 15)], R["bone"])
    else:
        im.ellipse(8, 11, 4, 4, R[ramp])
        if stone:
            im.ellipse(8, 11, 2, 2, R[stone])
            im.set(7, 10, "ffffff")
    return im.outline()


def storm_eye():
    im = amulet("silver", "disc", "ice")
    im.set(8, 11, K)
    im.line(13, 7, 11, 10, R["gold"][3]); im.line(11, 10, 14, 11, R["gold"][3])
    return im.outline()


def blueprint():
    im = Img(16, 16)
    im.rect(2, 3, 13, 13, R["parch"])
    im.ellipse(2, 8, 1.8, 5.5, R["parch"], 0.4)
    im.ellipse(13.5, 8, 1.8, 5.5, R["parch"], -0.2)
    for y in (6, 8, 10):
        im.line(5, y, 11, y, R["ink"][1])
    im.set(9, 6, R["ink"][0]); im.line(6, 6, 7, 10, R["ink"][0])
    return im.outline()


ITEMS = {
    # Bows, short (one-handed) and long.
    "hunting_bow": lambda: bow("wood", 46, 8, 2, "leather", "bone"),
    "recurve_bow": lambda: bow("wood", 48, 7, 3, "brigand", "bone", recurve=4),
    "horn_bow": lambda: bow("bone", 46, 8, 3, "dark", "dark", recurve=3, bands=((0.3, "dark"), (0.7, "dark"))),
    "yew_longbow": lambda: bow("wood", 62, 7, 3, "leather", "bone"),
    "war_bow": lambda: bow("leather", 62, 9, 3, "dark", "iron", bands=((0.25, "iron"), (0.75, "iron"))),
    "great_bow": lambda: bow("brigand", 62, 10, 4, "gold", "steel", recurve=4, bands=((0.3, "gold"), (0.7, "gold"))),
    "splinterbough": lambda: bow("wood", 60, 8, 3, "green", "green", mark=m_splinter),
    "ember_tongue": lambda: bow("dark", 48, 8, 3, "ember", "ember", string="ffd060", mark=m_ember),
    "hunters_promise": lambda: bow("tan", 48, 8, 3, "gold", "gold", recurve=3, mark=m_promise),
    "galeheart": lambda: bow("bone", 58, 8, 3, "teal", "teal", recurve=4, string="a8f4ea", mark=m_gale),
    "widows_draw": lambda: bow("black", 50, 9, 3, "red", "red", recurve=5, mark=m_widow),
    "gallowsreach": lambda: bow("dark", 62, 8, 3, "rope", "iron", mark=m_gallows),
    "stillwater_yew": lambda: bow("ice", 62, 7, 3, "silver", "silver", string="e8f8ff", mark=m_still),
    "the_long_silence": lambda: bow("black", 62, 10, 4, "violet", "violet", recurve=3, string="c8a0f4", mark=m_silence),
    # Quivers.
    "hide_quiver": lambda: quiver("tan", "leather"),
    "studded_quiver": lambda: quiver("brigand", "leather", studs="silver"),
    "bone_quiver": lambda: quiver("bone", "dark", "bone", ribs=True),
    "quiver_of_endless_splinters": lambda: quiver("wood", "green", "green", arrows=0, mark=q_splinters),
    "ashfall_quiver": lambda: quiver("dark", "iron", "ember", mark=q_ashfall),
    "wind_sworn_quiver": lambda: quiver("teal", "silver", "bone", mark=q_wind),
    "magpies_nest": lambda: quiver("rope", "black", "black", mark=q_magpie),
    "quiver_of_the_hollow_hound": lambda: quiver("fur", "black", "red", mark=q_hound),
    # Chest pieces by tier, then the legendaries.
    "ranger": lambda: chest("leather", "leather", "teal", p_ranger, stitched=True),
    "brigand": lambda: chest("brigand", "leather", None, p_brigand, sleeves="leather", shoulders="leather"),
    "scale": lambda: chest("bronze", "leather", None, p_scales, sleeves="leather", shoulders="bronze"),
    "plate": lambda: chest("steel", "leather", None, p_plate, shoulders="steel"),
    "knight": lambda: chest("bright", "gold", None, p_knight, collar="gold", shoulders="gold"),
    "stag_hide": lambda: chest("tan", "leather", None, p_stag, collar="bone", stitched=True),
    "cinder_jerkin": lambda: chest("dark", "black", None, p_cinder, collar="ember"),
    # Helms.
    "hood": lambda: helm("hood", "cloth"),
    "mask": lambda: helm("mask", "brigand", "gold"),
    "barbute": lambda: helm("barbute", "steel"),
    "visored": lambda: helm("visored", "steel", "red"),
    "horned": lambda: helm("horned", "bright", "gold"),
    "falconer": falconer,
    "unblinking_crown": unblinking,
    # Boots.
    "wrapped": lambda: boots("cloth", "wrapped"),
    "leather_boots": lambda: boots("leather", "leather", "tan"),
    "strapped": lambda: boots("brigand", "strapped"),
    "sabatons": lambda: boots("steel", "sabatons"),
    "spiked": lambda: boots("black", "spiked", "red"),
    "windrunner": windrunner,
    "stalker": stalker,
    # Gloves.
    "wraps": lambda: glove("cloth", "wraps"),
    "gloves": lambda: glove("leather", "gloves", "tan"),
    "bracers": lambda: glove("brigand", "bracers", None, "leather"),
    "gauntlets": lambda: glove("steel", "gauntlets"),
    "claws": lambda: glove("black", "claws", "red"),
    "fletcher": fletcher,
    "bloodletter": bloodletter,
    # Belts.
    "rope": lambda: belt("rope", "rope"),
    "pouch_belt": lambda: belt("leather", "pouch", "iron"),
    "studded": lambda: belt("brigand", "studded", "silver"),
    "tassets": lambda: belt("leather", "tassets", "steel"),
    "war_girdle": lambda: belt("black", "war_girdle", "gold"),
    "bandolier": bandolier,
    # Rings, amulets, the blueprint.
    "iron_ring": lambda: ring("iron"),
    "copper_ring": lambda: ring("copper"),
    "silver_ring": lambda: ring("silver", "teal"),
    "gold_ring": lambda: ring("gold", "red"),
    "jewelled_ring": lambda: ring("gold", "violet"),
    "pack_leaders_signet": lambda: ring("gold", signet=True),
    "bone_charm": lambda: amulet("bone", "tooth"),
    "copper_amulet": lambda: amulet("copper"),
    "silver_amulet": lambda: amulet("silver", stone="teal"),
    "gold_amulet": lambda: amulet("gold", stone="red"),
    "jewelled_amulet": lambda: amulet("gold", stone="violet"),
    "eye_of_the_storm": storm_eye,
    "blueprint": blueprint,
}

existing = sorted(f[:-4] for f in os.listdir(OUT) if f.endswith(".png"))
missing = [n for n in existing if n not in ITEMS]
extra = [n for n in ITEMS if n not in existing]
print("missing", missing, "extra", extra)

import sys
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if ARGS and ARGS[0] == "gear":
    folder = ARGS[1] if len(ARGS) > 1 else OUT
    os.makedirs(folder, exist_ok=True)
    bows = {"splinterbough", "ember_tongue", "hunters_promise", "galeheart", "widows_draw", "gallowsreach", "stillwater_yew",
            "the_long_silence", "yew_longbow"}
    for name, make in ITEMS.items():
        if not (name.endswith("_bow") or name in bows):
            save(make().rgba(), os.path.join(folder, name + ".png"))
    print("wrote gear into", folder)
    raise SystemExit(0)
if ARGS and ARGS[0] == "bows":
    # Only the bows (2026-11-10), into ARGS[1] (a folder outside the game, to be looked at first) or the game's.
    folder = ARGS[1] if len(ARGS) > 1 else OUT
    os.makedirs(folder, exist_ok=True)
    for name, make in ITEMS.items():
        if name.endswith("_bow") or name in ("splinterbough", "ember_tongue", "hunters_promise", "galeheart", "widows_draw",
                                              "gallowsreach", "stillwater_yew", "the_long_silence", "yew_longbow"):
            save(make().rgba(), os.path.join(folder, name + ".png"))
    print("wrote bows into", folder)
    raise SystemExit(0)

BG = np.array([0.1, 0.13, 0.09, 1], np.float32)
tiles = []
for name, make in ITEMS.items():
    arr = make().rgba()
    save(arr, os.path.join(OUT, name + ".png"))
    t = np.tile(BG, (66, 34, 1))
    h, w = arr.shape[:2]
    oy, ox = (66 - h) // 2, (34 - w) // 2
    t[oy:oy + h, ox:ox + w] = np.where(arr[..., 3:4] > 0.5, arr, BG)
    tiles.append(t)
rows = [np.concatenate(tiles[i:i + 14] + [np.tile(BG, (66, 34, 1))] * (14 - len(tiles[i:i + 14])), 1) for i in range(0, len(tiles), 14)]
sheet = np.concatenate(rows, 0)
save(np.repeat(np.repeat(sheet, 3, 0), 3, 1), os.path.join(P, "ArtSource", "pixel", "previews", "item_art_16.png"))
print("wrote", len(ITEMS))
