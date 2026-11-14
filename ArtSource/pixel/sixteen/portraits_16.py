# The portraits (remade 2026-11-12, the owner: "remake the character portrait and the character images in menus, like
# brunhilde and thorin and lyriel"): the ten base faces of the archers (PortraitFaces paints each archer's skin, hair and
# eyes and her marks on one of them by her name) and the seven keepers. Every shape is lit as a rounded volume from the
# top left and drawn in five tones (its own dark line, shadow, middle, light, shine); the faces' features are placed
# pixel by pixel. The archer is the drawn Wild Arrow: green tunic, red cape, blonde on the base picture.
# The archers' busts are 66 x 81 (shown at two menu pixels a pixel in the company's header, whole in the Bag); the
# keepers 35 x 32, written doubled at 70 x 64, their heads filling the frame as hers fills the header's window.
# Writes ArtSource/ui/faces/archer_<n>.png, _regions.png and .json, and ArtSource/ui/pixel/portrait_<name>.png; Tools >
# ARPG > Import Menu Art copies them into Resources/UI. A preview goes to ArtSource/pixel/previews/portraits_16.png.
# Run: python3 ArtSource/pixel/sixteen/portraits_16.py (plain Python, no Blender needed)
import os, json, math, random, struct, zlib

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
FACES = os.path.join(P, "ArtSource", "ui", "faces")
PIXEL = os.path.join(P, "ArtSource", "ui", "pixel")
K = "140c12"
SKIN, HAIR, IRIS = 1, 2, 3
LIGHT = (-0.48, -0.56, 0.675)

# Five tones each: the shape's own dark line, shadow, middle, light, shine.
R = {
    "skin": ["5a2e22", "9a5a3e", "cc8a60", "e8b088", "f8d4b0"],
    "pale": ["3e3e5a", "7a7a9a", "b0b2cc", "d8dae8", "f4f6ff"],
    "brown": ["24120a", "44220f", "6e3c1c", "98602e", "c08a4c"],
    "blonde": ["6a4612", "a47424", "d6a842", "f0d070", "fcecac"],
    "ginger": ["4a160a", "8a2a12", "c84e20", "ee7a36", "ffb06a"],
    "black": ["08080c", "16161e", "2a2a36", "44445a", "6c6c86"],
    "grey": ["34343a", "5c5c64", "8e8e98", "bcbcc6", "e6e6ee"],
    "green": ["10281a", "1c4428", "2e6a38", "4a8e48", "78b464"],
    "red": ["3c0a0e", "6e1416", "a02220", "c83a30", "e6604a"],
    "leather": ["2a160c", "4e2a16", "7e4a26", "ac7240", "d09a62"],
    "steel": ["20242e", "48505e", "828c9c", "bac4d0", "eef2f8"],
    "gold": ["5a3c0c", "94681a", "d8a832", "f6d860", "fff4b0"],
    "white": ["5c5c68", "9a9aa8", "c8c8d4", "e8e8f0", "ffffff"],
    "navy": ["080c20", "121c40", "22346a", "38509a", "5a78c8"],
    "teal": ["083634", "126a66", "22b0a4", "62e0d0", "c0faf0"],
    "fur": ["3a2a1c", "66503a", "9a8260", "c8b490", "eee0c4"],
    "apron": ["1e120c", "3a2416", "5e3e26", "84603e", "a8845c"],
    "wood": ["3a1e0e", "6a3c1c", "a8702e", "d49a4e", "f0c480"],
    "cream": ["6a5e48", "a09274", "d0c4a4", "eee6cc", "fffbee"],
}
LIP = ["8a3432", "c05a54"]
LASH = "24100e"
WHITE, WHITE_SHADE = "f6f0e6", "cfc4b8"
BG = ["0c120c", "121a12", "18241a"]


def ellipse(cx, cy, rx, ry):
    return lambda x, y: ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1


def poly(pts):
    def inside(x, y):
        n, c = len(pts), False
        for i in range(n):
            (ax, ay), (bx, by) = pts[i], pts[(i + 1) % n]
            if (ay > y) != (by > y) and x < ax + (y - ay) * (bx - ax) / (by - ay):
                c = not c
        return c
    return inside


def bounds(pts):
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    return ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, (max(xs) - min(xs)) * 0.62 + 0.5, (max(ys) - min(ys)) * 0.62 + 0.5)


class Img:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.c = [[None] * w for _ in range(h)]
        self.r = [[0] * w for _ in range(h)]

    def ok(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h

    def set(self, x, y, col, region=0):
        x, y = int(math.floor(x)), int(math.floor(y))
        if self.ok(x, y) and col is not None:
            self.c[y][x] = col
            self.r[y][x] = region

    def get(self, x, y):
        return self.c[y][x] if self.ok(x, y) else None

    def tone(self, x, y, ramp, k, region=0, only=None):
        """One pixel in a ramp's tone; with `only`, just where that region already is."""
        x, y = int(x), int(y)
        if self.ok(x, y) and (only is None or self.r[y][x] == only):
            self.set(x, y, R[ramp][k], region)

    def shape(self, mask, vol, ramp, region=0, rim="shade", bias=0.0, tex=None, cast=None, maxk=4, whole=None):
        """Fills the mask, lit as the ellipsoid `vol` (cx, cy, rx, ry). rim: "full" a dark line all round, "shade" only on
        the sides away from the light, None none. tex(x, y, k) may change a pixel's tone. cast: the skin right under
        the shape's lower edge goes to shadow. maxk: the lightest tone the light gives. whole: a wider mask this shape is a part
        of, so no line is drawn where the parts join."""
        r = R[ramp] if isinstance(ramp, str) else ramp
        cx, cy, rx, ry = vol
        x0, x1 = max(0, int(cx - rx * 3) - 2), min(self.w, int(cx + rx * 3) + 3)
        hit = set()
        for y in range(self.h):
            for x in range(self.w):
                if mask(x + 0.5, y + 0.5):
                    hit.add((x, y))
        for (x, y) in hit:
            dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            d = dx * dx + dy * dy
            if d > 1:
                s = math.sqrt(d)
                dx, dy, dz = dx / s, dy / s, 0.0
            else:
                dz = math.sqrt(1 - d)
            l = dx * LIGHT[0] + dy * LIGHT[1] + dz * LIGHT[2] + bias
            k = min(maxk, 1 if l < 0.30 else 2 if l < 0.70 else 3 if l < 0.93 else 4)
            if tex:
                k = max(1, min(4, tex(x, y, k)))
            if rim:
                sides = ((1, 0), (0, 1)) if rim == "shade" else ((1, 0), (0, 1), (-1, 0), (0, -1))
                if any((x + ox, y + oy) not in hit and not (whole and whole(x + ox + 0.5, y + oy + 0.5)) for ox, oy in sides):
                    k = 0
            self.set(x, y, r[k], region)
        if cast:
            for (x, y) in hit:
                if (x, y + 1) not in hit and self.ok(x, y + 1) and self.r[y + 1][x] == SKIN and self.c[y + 1][x] in R[cast][2:]:
                    self.c[y + 1][x] = R[cast][1]
        return hit

    def line(self, x0, y0, x1, y1, col, region=0):
        n = int(max(abs(x1 - x0), abs(y1 - y0), 1))
        for k in range(n + 1):
            self.set(x0 + (x1 - x0) * k / n + 0.5, y0 + (y1 - y0) * k / n + 0.5, col, region)

    def background(self):
        self.back = [[True] * self.w for _ in range(self.h)]
        for y in range(self.h):
            for x in range(self.w):
                d = math.hypot((x - self.w / 2) / self.w, (y - self.h * 0.4) / self.h)
                self.c[y][x] = BG[2] if d < 0.32 else BG[1] if d < 0.5 else BG[0]
        rng = random.Random(self.w * 31 + self.h)
        for _ in range(self.w * self.h // 40):
            x, y = rng.randrange(self.w), rng.randrange(self.h)
            self.c[y][x] = BG[min(2, BG.index(self.c[y][x]) + 1)]

    def outline(self):
        """A dark line round the figure, on the background."""
        out = [row[:] for row in self.c]
        for y in range(self.h):
            for x in range(self.w):
                if self.c[y][x] in BG:
                    for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        n = self.get(x + ox, y + oy)
                        if n is not None and n not in BG:
                            out[y][x] = K
                            break
        self.c = out

    def rows(self, scale=1):
        out = []
        for y in range(self.h):
            row = []
            for x in range(self.w):
                h = self.c[y][x]
                px = (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255) if h else (0, 0, 0, 0)
                row.extend([px] * scale)
            out.extend([row] * scale)
        return out

    def region_rows(self):
        return [[(self.r[y][x], 255, 0, 255) if self.r[y][x] else (0, 0, 0, 255) for x in range(self.w)] for y in range(self.h)]


def save(rows, path):
    h, w = len(rows), len(rows[0])
    raw = b"".join(b"\x00" + bytes(v for px in row for v in px) for row in rows)
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xffffffff)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


# A head ----------------------------------------------------------------------------------------------------------
class Head:
    """cx is the middle between two pixel columns (33.0) or the middle of one (17.5); big heads have the detailed face."""

    def __init__(self, cx, cy, rx, ry, jaw=0.0):
        self.cx, self.cy, self.rx, self.ry, self.jaw = cx, cy, rx, ry, jaw
        self.big = rx >= 11
        self.L0 = int(math.ceil(cx)) - 1          # the column left of (or on) the middle
        self.R0 = int(math.floor(cx))             # the column right of (or on) the middle
        self.ey = int(cy + ry * 0.1)
        self.vol = (cx, cy - ry * 0.05, rx * 1.12, ry * 1.2)

    def cols(self, d):
        return self.L0 - d, self.R0 + d

    def half_width(self, y):
        t = (y - self.cy) / self.ry
        j = 0.8 + 0.12 * self.jaw
        if t < -1 or t > 1.08:
            return -1
        if t <= 0:
            return self.rx * math.sqrt(1 - t * t)
        if t <= 0.5:
            return self.rx * (1 - (1 - j) * (t / 0.5) ** 2)
        u = (t - 0.5) / 0.58
        chin = 0.14 + 0.24 * self.jaw
        return self.rx * (chin + (j - chin) * math.sqrt(max(0, 1 - u ** 1.6)))

    def mask(self, x, y):
        return abs(x - self.cx) <= self.half_width(y)

    def face(self, im, skin="skin", eyes="3e8a4a", brows="blonde", stern=0, old=False, glow=None, smile=False, ears=True, mouth=True, lips=True):
        cx, cy, rx, ry = self.cx, self.cy, self.rx, self.ry
        ey, big = self.ey, self.big
        if ears:
            for side in (-1, 1):
                im.shape(ellipse(cx + side * rx * 0.99, cy + ry * 0.16, rx * 0.17, ry * 0.2), (cx, cy, rx * 1.3, ry * 1.2), skin, SKIN, rim=None, bias=-0.15)
        im.shape(self.mask, self.vol, skin, SKIN, bias=0.33, maxk=3)

        def both(d, y, ramp, k, region=0, left=True, right=True):
            a, b = self.cols(d)
            if left:
                im.tone(a, y, ramp, k, region)
            if right:
                im.tone(b, y, ramp, k, region)

        def px(d, y, col, region=0):
            a, b = self.cols(d)
            im.set(a, y, col, region)
            im.set(b, y, col, region)

        if big:
            # Eyes five wide: a lash line with a wing, the white, an iris two by two with a light in it, a lower lid.
            for d in range(3, 8):
                px(d, ey - 1, LASH)
            px(8, ey, LASH)
            if glow:
                for d in range(3, 8):
                    px(d, ey, glow); px(d, ey + 1, glow) if 3 < d < 7 else None
            else:
                for d in (3, 6, 7):
                    px(d, ey, WHITE)
                px(3, ey + 1, WHITE_SHADE); px(6, ey + 1, WHITE)
                iris = [int(eyes[i:i + 2], 16) for i in (0, 2, 4)]
                dark = "".join(f"{int(v * 0.55):02x}" for v in iris)
                lit = "".join(f"{min(255, int(v * 1.3)):02x}" for v in iris)
                for d in (4, 5):
                    px(d, ey, eyes, IRIS); px(d, ey + 1, lit, IRIS)
                im.set(self.L0 - 4, ey, dark, IRIS); im.set(self.R0 + 5, ey, dark, IRIS)      # the pupils, the light beside them
                im.set(self.L0 - 5, ey, "ffffff"); im.set(self.R0 + 4, ey, "ffffff")
                both(7, ey + 1, skin, 1, SKIN)
                for d in (3, 4, 5, 6):
                    both(d, ey + 2, skin, 2 if d in (3, 6) else 1, SKIN)
            # Brows: arched, or drawn down toward the nose when she is stern.
            for d, up in ((3, 3 if stern else 4), (4, 4 if stern else 5), (5, 5), (6, 5), (7, 4)):
                both(d, ey - up, brows, 1, HAIR)
            # Nose: light down its left, shade on its right, the nostrils' shadow.
            for y in (ey + 3, ey + 4, ey + 5):
                im.tone(self.L0, y, skin, 4, SKIN)
            im.tone(self.R0 + 1, ey + 5, skin, 1, SKIN); im.tone(self.R0, ey + 5, skin, 2, SKIN)
            im.tone(self.L0 - 1, ey + 6, skin, 2, SKIN); im.tone(self.L0, ey + 6, skin, 1, SKIN); im.tone(self.R0, ey + 6, skin, 1, SKIN); im.tone(self.R0 + 1, ey + 6, skin, 1, SKIN)
            if mouth:
                my = ey + 9
                for d in (0, 1, 2):
                    px(d, my, LIP[0])
                both(3, my - (1 if smile else 0), skin, 1, SKIN)
                if lips:
                    px(0, my + 1, LIP[1]); px(1, my + 1, LIP[1])
                    im.set(self.L0, my + 1, "e08a80")
                    both(2, my + 1, skin, 2, SKIN)
                for d in (0, 1):
                    both(d, my + 2, skin, 1 if lips else 2, SKIN)
            if old:
                both(8, ey + 2, skin, 1, SKIN); both(4, ey + 7, skin, 2, SKIN)
        else:
            # Eyes three wide: a lash line, the white each side of the iris.
            for d in range(3, 6):
                px(d, ey - 1, LASH)
            if glow:
                for d in range(3, 6):
                    px(d, ey, glow)
            else:
                px(3, ey, WHITE); px(5, ey, WHITE)
                px(4, ey, eyes, IRIS)
                both(4, ey + 1, skin, 2, SKIN)
            for d in range(3, 6):
                both(d, ey - 3 + (1 if stern and d == 3 else 0), brows, 1, HAIR)
            if stern:
                both(6, ey - 4, brows, 1, HAIR)
            im.tone(self.L0, ey + 2, skin, 4, SKIN)
            im.tone(self.R0 + 1, ey + 2, skin, 2, SKIN)
            im.tone(self.L0, ey + 3, skin, 1, SKIN); im.tone(self.R0 + 1, ey + 3, skin, 1, SKIN)
            if mouth:
                my = ey + 6
                a, b = self.cols(1)
                for x in range(a, b + 1):
                    im.set(x, my, LIP[0])
                both(2, my - (1 if smile else 0), skin, 1, SKIN)
                if lips:
                    for x in range(a + 1, b):
                        im.set(x, my + 1, LIP[1])
                else:
                    for x in range(a + 1, b):
                        im.tone(x, my + 1, skin, 1, SKIN)
            if old:
                both(6, ey + 2, skin, 1, SKIN); both(3, ey + 4, skin, 2, SKIN)

    def spots(self):
        """Where the painter's marks go (PortraitFaces): eyes, cheeks, brow, chin."""
        ey = self.ey
        return {
            "eyes": [self.L0 - 4, ey, self.R0 + 4, ey],
            "cheeks": [self.L0 - 6, ey + 5, self.R0 + 6, ey + 5],
            "brow": [self.R0, ey - 7],
            "chin": [self.R0, ey + 12],
        }


# Hair ------------------------------------------------------------------------------------------------------------
def strands(h, every=4):
    """Hair's texture: a broken band of shine across the crown, darker strands lower down."""
    top = h.cy - h.ry * 0.12
    def tex(x, y, k):
        u, v = (x + 0.5 - h.cx) / h.rx, (y + 0.5 - top) / (h.ry * 1.05)
        band = v + 0.3 * u * u
        if -0.66 < band < -0.44 and x % 3 != 0:
            return k + 1
        if v > -0.25 and (x + (y // 5)) % every == 1:
            return k - 1
        return k
    return tex


def hair_back(im, h, style, ramp):
    cx, cy, rx, ry = h.cx, h.cy, h.rx, h.ry
    tex = strands(h)
    vol = (cx, cy, rx * 1.5, ry * 1.6)
    if style in ("long", "band", "braid", "twin", "shaved"):
        drop = 2.3 if style in ("long", "band") else 0.9
        def m(x, y):
            if ellipse(cx, cy - ry * 0.1, rx * 1.3, ry * 1.14)(x, y):
                return True
            return cy < y < cy + ry * drop and abs(x - cx) < rx * (1.3 + 0.04 * math.sin(y * 0.7)) - max(0, y - cy - ry * (drop - 0.5)) * 0.8
        im.shape(m, vol, ramp, HAIR, tex=tex, maxk=3)
    elif style == "short":
        im.shape(lambda x, y: ellipse(cx, cy - ry * 0.05, rx * 1.26, ry * 1.12)(x, y) and y < cy + ry * 0.8, vol, ramp, HAIR, tex=tex, maxk=3)
    elif style == "wavy":
        im.shape(ellipse(cx, cy - ry * 0.1, rx * 1.3, ry * 1.14), vol, ramp, HAIR, tex=tex, maxk=3)
        for side in (-1, 1):
            for k in range(7):
                im.shape(ellipse(cx + side * rx * (1.12 + 0.16 * (k % 2)), cy + ry * (0.1 + k * 0.33), rx * 0.42, ry * 0.3), vol, ramp, HAIR)
    elif style == "curly":
        rng = random.Random(7)
        for _ in range(46):
            a, d = rng.uniform(0, math.tau), rng.uniform(0.55, 1.0)
            im.shape(ellipse(cx + math.cos(a) * rx * 1.45 * d, cy + math.sin(a) * ry * 1.25 * d + ry * 0.15, rx * 0.36, rx * 0.36), vol, ramp, HAIR)
    elif style == "ponytail":
        im.shape(ellipse(cx, cy - ry * 0.12, rx * 1.16, ry * 1.08), vol, ramp, HAIR, tex=tex, maxk=3)
        tail = [(cx + rx * 0.5, cy - ry * 1.15), (cx + rx * 1.3, cy - ry * 1.05), (cx + rx * 1.85, cy - ry * 0.3), (cx + rx * 1.9, cy + ry * 0.9),
                (cx + rx * 1.5, cy + ry * 1.7), (cx + rx * 1.3, cy + ry * 0.9), (cx + rx * 1.25, cy - ry * 0.1), (cx + rx * 0.8, cy - ry * 0.6)]
        im.shape(poly(tail), (cx + rx * 1.3, cy, rx * 0.9, ry * 1.8), ramp, HAIR, tex=tex, maxk=3)
    elif style == "buns":
        im.shape(ellipse(cx, cy - ry * 0.12, rx * 1.16, ry * 1.08), vol, ramp, HAIR, tex=tex, maxk=3)
        for side in (-1, 1):
            im.shape(ellipse(cx + side * rx * 0.8, cy - ry * 1.0, rx * 0.5, rx * 0.48), (cx + side * rx * 0.8, cy - ry * 1.0, rx * 0.6, rx * 0.6), ramp, HAIR, rim="full")
    elif style == "bun":
        im.shape(ellipse(cx, cy - ry * 0.12, rx * 1.14, ry * 1.06), vol, ramp, HAIR, tex=tex, maxk=3)
        im.shape(ellipse(cx, cy - ry * 1.12, rx * 0.55, rx * 0.45), (cx, cy - ry * 1.12, rx * 0.7, rx * 0.6), ramp, HAIR, rim="full")


def hairline(h, style, x):
    u = (x - h.cx) / h.rx
    cy, ry = h.cy, h.ry
    if style in ("long", "wavy", "braid", "band"):
        return cy - ry * 0.64 + abs(u) * ry * 0.5                    # parted in the middle
    if style in ("ponytail", "shaved", "curly", "bun"):
        return cy - ry * 0.62 + (0.5 - u) * ry * 0.26 if u < 0.5 else cy - ry * 0.62 + (u - 0.5) * ry * 0.6   # swept from a side parting
    return cy - ry * 0.3 + (1 if int(x) % 3 == 0 else 0) - (1 - u * u) * ry * 0.04      # a fringe


def hair_front(im, h, style, ramp):
    cx, cy, rx, ry = h.cx, h.cy, h.rx, h.ry
    if style in ("bald", "none"):
        return
    tex = strands(h)
    cap = ellipse(cx, cy - ry * 0.12, rx * 1.14, ry * 1.06)
    def m(x, y):
        if not cap(x, y):
            return False
        if style == "shaved" and x < cx - rx * 0.45:
            return y < cy - ry * 0.78
        side = abs(x - cx) > rx * (0.8 if style in ("long", "wavy", "band", "braid") else 0.88) and y < cy + ry * (0.5 if style in ("long", "wavy", "band") else 0.22)
        return y < hairline(h, style, x) or side
    locks = style in ("long", "wavy", "band")
    def lock(x, y):
        xi, t = abs(x - cx) / rx, (y - cy) / ry
        if not locks or t < -0.3 or t > 2.45:
            return False
        return 0.8 + max(0, t - 0.5) * 0.06 <= xi <= 1.32 - max(0, t - 1.5) * 0.5 + (0.08 * math.sin(y * 0.9) if style == "wavy" else 0)
    both = lambda x, y: m(x, y) or lock(x, y)
    if locks:
        for side in (-1, 1):
            im.shape(lambda x, y, side=side: lock(x, y) and (x - cx) * side > 0 and y > cy - ry * 0.1, (cx + side * rx * 1.0, cy + ry * 1.2, rx * 0.5, ry * 1.9),
                     ramp, HAIR, rim="full", tex=strands(h, 3), maxk=3, whole=both)
    im.shape(m, (cx, cy - ry * 0.1, rx * 1.25, ry * 1.3), ramp, HAIR, tex=tex, cast="skin", maxk=3, whole=both)
    if style == "curly":
        rng = random.Random(3)
        for _ in range(9):
            im.shape(ellipse(cx + rng.uniform(-0.8, 0.8) * rx, cy - ry * rng.uniform(0.5, 0.95), rx * 0.3, rx * 0.28), (cx, cy - ry, rx * 1.4, ry), ramp, HAIR)
    if style == "band":
        y = int(cy - ry * 0.52)
        for x in range(int(cx - rx * 1.1), int(cx + rx * 1.1) + 1):
            if cap(x + 0.5, y + 0.5):
                im.tone(x, y, "teal", 3 if x < cx else 2); im.tone(x, y + 1, "teal", 1)


def hair_over(im, h, style, ramp):
    """What falls in front of her shoulders: locks and braids."""
    cx, cy, rx, ry = h.cx, h.cy, h.rx, h.ry
    tex = strands(h, 3)
    if style in ("braid", "twin"):
        for side in ((1,) if style == "braid" else (-1, 1)):
            x, y = cx + side * rx * 0.98, cy + ry * 0.42
            n = 7 if h.big else 5
            for k in range(n):
                bx, by = x + side * k * rx * 0.035, y + k * ry * 0.27
                im.shape(ellipse(bx, by, rx * 0.25, ry * 0.17), (bx - 1, by - 1, rx * 0.36, ry * 0.3), ramp, HAIR, rim="full", bias=0.1 if k % 2 else -0.1)
            im.shape(ellipse(bx, by + ry * 0.24, rx * 0.2, ry * 0.1), (bx, by + ry * 0.24, rx * 0.3, ry * 0.2), "teal", rim="full")
            im.shape(ellipse(bx, by + ry * 0.42, rx * 0.14, ry * 0.13), (bx, by + ry * 0.4, rx * 0.3, ry * 0.3), ramp, HAIR, rim="full")
    if style == "shaved":
        x0 = cx + rx * 0.98
        lock = [(x0 - rx * 0.15, cy + ry * 0.1), (x0 + rx * 0.3, cy + ry * 0.1), (x0 + rx * 0.36, cy + ry * 0.9), (x0 + rx * 0.05, cy + ry * 1.25), (x0 - rx * 0.12, cy + ry * 0.8)]
        im.shape(poly(lock), (x0, cy + ry * 0.6, rx * 0.5, ry * 0.9), ramp, HAIR, rim="full", tex=tex)


# The archer's bust: 66 x 81 ---------------------------------------------------------------------------------------
ARCHERS = [
    # hair style, hair ramp, jaw, stern brows, hood
    ("long", "blonde", 0.0, 0, False),
    ("braid", "brown", 0.2, 0, False),
    ("ponytail", "ginger", 0.0, 1, False),
    ("short", "brown", 0.5, 0, False),
    ("curly", "black", 0.3, 0, False),
    ("buns", "brown", 0.0, 0, False),
    ("shaved", "blonde", 0.6, 1, False),
    ("twin", "ginger", 0.1, 0, False),
    ("band", "brown", 0.3, 1, False),
    ("short", "brown", 0.2, 0, True),
]


def archer(style, ramp, jaw, stern, hood):
    im = Img(66, 81)
    im.background()
    h = Head(33.0, 33.0, 12.0, 14.0, jaw)
    # Her bow behind her left shoulder, the quiver's arrows behind her right.
    for y in range(6, 81):
        u = (y - 46) / 40
        x = 6 + 8 * u * u
        im.tone(x, y, "wood", 3); im.tone(x + 1, y, "wood", 2); im.tone(x + 2, y, "wood", 0)
    for y in (40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50):
        im.tone(6, y, "leather", 2); im.tone(7, y, "leather", 1)
    im.line(14, 6, 14, 80, "cfd6de")
    for k in range(3):
        x0 = 49 + k * 4
        im.line(x0, 56, x0 + 3, 30 + k * 2, R["wood"][2])
        feather = [(x0 + 2, 25 + k * 2), (x0 + 5, 24.5 + k * 2), (x0 + 6, 32 + k * 2), (x0 + 3, 33.5 + k * 2)]
        im.shape(poly(feather), bounds(feather), "cream", rim="full")
        im.tone(x0 + 3, 25 + k * 2, "red", 3); im.tone(x0 + 4, 25 + k * 2, "red", 2)
    if hood:
        im.shape(ellipse(33, 33, 18.5, 21), (33, 33, 22, 25), "red", rim="full")
    hair_back(im, h, style, ramp)
    # Shoulders: the green tunic, the quiver's strap across it, the red cape over both shoulders, tied at the throat.
    tunic = [(14, 81), (17, 62), (25, 53), (41, 53), (49, 62), (52, 81)]
    im.shape(poly(tunic), (33, 70, 24, 24), "green", rim=None)
    neck = [(27.5, 42), (38.5, 42), (39.5, 54), (33, 61), (26.5, 54)]
    im.shape(poly(neck), (33, 50, 9, 14), "skin", SKIN, rim=None, bias=-0.12)
    for y in range(47, 52):
        for x in range(27, 39):
            if im.r[y][x] == SKIN and abs(x - 32.5) < 6.5 - (y - 47) * 1.2:
                im.tone(x, y, "skin", 1, SKIN)
    for k, (x0, x1) in enumerate(((26, 39), (27, 38), (28, 37), (29, 36), (30, 35), (31, 34), (32, 33))):      # the tunic's collar, a V
        y = 54 + k
        im.tone(x0, y, "green", 4); im.tone(x1, y, "green", 3)
        im.tone(x0 - 1, y, "green", 0); im.tone(x1 + 1, y, "green", 0)
    for k in range(-1, 2):
        im.line(46 + k, 55, 24 + k, 81, R["leather"][2 if k == 0 else 3 if k < 0 else 1])
    im.line(48, 55, 26, 81, R["leather"][0]); im.line(44, 55, 22, 81, R["leather"][0])
    im.tone(36, 68, "gold", 3); im.tone(37, 68, "gold", 2); im.tone(36, 69, "gold", 2); im.tone(37, 69, "gold", 1)
    for side in (-1, 1):
        s = lambda x: 33 + side * (x - 33)
        cape = [(s(0), 81), (s(1), 66), (s(7), 57), (s(17), 52), (s(26), 50.5), (s(28), 55), (s(24), 66), (s(19), 81)]
        fold = lambda x, y, k, side=side: k - 1 if int(x * side + y * 0.45) % 6 == 0 and y > 60 else k
        im.shape(poly(cape), (s(12), 72, 15, 24), "red", rim="full", tex=fold, bias=0.08 if side < 0 else -0.05, maxk=3)
        roll = [(s(18), 51), (s(27), 49), (s(29), 54), (s(27), 57), (s(19), 56)]
        im.shape(poly(roll), (s(23), 53, 6, 4.5), "red", rim="full", bias=0.12)
    im.shape(ellipse(33, 63, 2.6, 2.6), (33, 63, 3, 3), "gold", rim="full")
    hair_over(im, h, "", ramp)
    h.face(im, jaw and "skin" or "skin", stern=stern, brows=ramp)
    hair_front(im, h, style, ramp)
    hair_over(im, h, style, ramp)
    if hood:
        def hood_shape(x, y):
            inner = ((x - 33) / 12.6) ** 2 + ((y - 34.5) / 16.5) ** 2 < 1
            return ellipse(33, 31.5, 17.5, 19.5)(x, y) and not inner and y < 52
        im.shape(hood_shape, (33, 31, 21, 23), "red", rim="full", cast="skin")
        for x in range(21, 46):       # the hood's shade on her brow
            for y in range(19, 24):
                if im.r[y][x] == SKIN and im.c[y][x] in R["skin"][3:]:
                    im.tone(x, y, "skin", 2, SKIN)
    im.outline()
    return im, h


# The keepers: 35 x 32, written at 70 x 64 -------------------------------------------------------------------------
def shoulders(im, ramp, top=25, slope=4, bias=0.0, rim="full"):
    pts = [(-1, 33), (0, top + slope), (8, top), (27, top), (35, top + slope), (36, 33)]
    return im.shape(poly(pts), (17.5, 34, 22, 12), ramp, rim=rim, bias=bias)


def neck(im, h, skin="skin"):
    im.shape(poly([(13.5, 21), (21.5, 21), (22.5, 28), (12.5, 28)]), (17.5, 24, 7, 9), skin, rim=None, bias=-0.3)


def keeper(name):
    im = Img(35, 32)
    im.background()
    cx = 17.5
    if name == "brunhilde":
        # The guard captain: an open steel helm with a gold ridge, blonde braids, a scarred cheek, steel shoulders over red.
        h = Head(cx, 14.5, 8.5, 10.0, 0.5)
        hair_back(im, h, "twin", "blonde")
        shoulders(im, "red", 25, 3)
        neck(im, h)
        for side in (-1, 1):
            im.shape(ellipse(cx + side * 13.5, 29.5, 8, 6), (cx + side * 13.5 - 1, 30, 9, 7), "steel", rim="full")
            im.tone(cx + side * 9 - 0.5, 28, "gold", 3)
        im.shape(poly([(12, 25), (23, 25), (21, 29), (17.5, 31), (14, 29)]), (17.5, 27, 7, 5), "steel", rim="full")
        h.face(im, eyes="3a6a9a", brows="blonde", stern=1, ears=False)
        im.tone(11, 18, "skin", 4); im.tone(12, 19, "skin", 1); im.tone(12, 18, "skin", 1); im.tone(13, 20, "skin", 1)
        hair_over(im, h, "twin", "blonde")
        helm = lambda x, y: ellipse(cx, 12.6, 10.2, 11.2)(x, y) and (y < 10.6 or (abs(x - cx) > 7.2 and y < 19))
        im.shape(helm, (cx, 11, 12, 13), "steel", rim="full", cast="skin")
        for x in range(8, 27):
            if helm(x + 0.5, 10.1):
                im.tone(x, 10, "gold", 3 if x < 16 else 2); im.tone(x, 9, "gold", 1 if x > 20 else 2)
        for y in range(1, 10):
            im.tone(17, y, "gold", 4 if y < 5 else 3); im.tone(18, y, "gold", 1)
            im.tone(16, y, "gold", 2)
    elif name == "thorin":
        # The smith: bald and broad, soot on his brow, a great ginger beard with gold rings, a leather apron on bare shoulders.
        h = Head(cx, 13.5, 9.2, 10.0, 1.0)
        im.shape(poly([(-1, 33), (-1, 28), (5, 24), (10, 22), (25, 22), (30, 24), (36, 28), (36, 33)]), (17.5, 34, 24, 13), "skin", rim="full", maxk=3)
        for side in (-1, 1):
            im.line(cx + side * 6.5, 32, cx + side * 9.5, 23, R["apron"][2]); im.line(cx + side * 7.5, 32, cx + side * 10.5, 23, R["apron"][0])
            im.tone(cx + side * 13 - 0.5, 26, "skin", 4 if side < 0 else 3); im.tone(cx + side * 14 - 0.5, 27, "skin", 4 if side < 0 else 3)
        im.shape(poly([(9, 33), (10, 27), (25, 27), (26, 33)]), (17.5, 32, 10, 7), "apron", rim="full")
        h.face(im, eyes="5a3a1a", brows="ginger", stern=1, old=True, mouth=False)
        for d in (3, 4, 5, 6):        # heavy brows
            a, b = h.cols(d)
            im.tone(a, h.ey - 2, "ginger", 2 if d > 3 else 1); im.tone(b, h.ey - 2, "ginger", 2 if d > 3 else 1)
        im.tone(13, 6, "skin", 4); im.tone(14, 5, "skin", 4); im.tone(14, 6, "skin", 4)      # the light on his bald head
        for (x, y) in ((20, 7), (21, 7), (21, 8), (22, 8)):
            im.tone(x, y, "skin", 1)                                                         # soot
        def beard(x, y):
            u = abs(x - cx)
            if y < 17.2:
                return u > 7.2 and y > 14 and h.mask(x, y)
            top = 17.2 + (0.9 if u < 1 else 0)
            return y > top and u < 9.6 - max(0, y - 22) * 0.62 + (0.6 if int(y) % 3 == 0 else 0)
        btex = lambda x, y, k: k - 1 if (x + y // 2) % 3 == 0 and y > 20 else k
        im.shape(beard, (cx, 22, 12, 12), "ginger", rim="full", tex=btex)
        for d in (0, 1, 2, 3, 4):     # the moustache over the mouth
            a, b = h.cols(d)
            im.tone(a, 18 + (1 if d > 2 else 0), "ginger", 4 if d < 3 else 3); im.tone(b, 18 + (1 if d > 2 else 0), "ginger", 3)
            im.tone(a, 19 + (1 if d > 2 else 0), "ginger", 0 if d else 1); im.tone(b, 19 + (1 if d > 2 else 0), "ginger", 0 if d else 1)
        for x in (14, 20):            # two braids in the beard, ringed with gold
            for y in range(25, 31):
                im.tone(x, y, "ginger", 3 if y % 2 else 1); im.tone(x + 1, y, "ginger", 2 if y % 2 else 0)
            im.tone(x, 27, "gold", 4); im.tone(x + 1, 27, "gold", 2)
            im.tone(x, 30, "gold", 3); im.tone(x + 1, 30, "gold", 1)
    elif name == "lyriel":
        # The scout: an elf under a green hood, ginger hair, pointed ears, teal paint under her eyes, a fur collar.
        h = Head(cx, 14.5, 8.2, 9.6, 0.0)
        im.shape(ellipse(cx, 13.5, 12.5, 13.5), (cx, 13, 15, 16), "green", rim="full")
        im.shape(ellipse(cx, 15, 9.6, 11), (cx, 15, 9.6, 11), ["06100a"] * 5, rim=None)
        shoulders(im, "green", 25, 3, bias=-0.05)
        h.face(im, eyes="2a8a5a", brows="ginger", ears=False, smile=True)
        for side, k in ((-1, 3), (1, 2)):
            a = h.L0 - 4 if side < 0 else h.R0 + 4
            im.tone(a - 1, h.ey + 2, "teal", k); im.tone(a, h.ey + 2, "teal", k); im.tone(a + 1, h.ey + 2, "teal", k)
            im.tone(a, h.ey + 3, "teal", k - 1)
        hair_front(im, h, "ponytail", "ginger")
        for side in (-1, 1):          # locks down out of the hood
            x0 = cx + side * 8.2
            lock = [(x0 - side * 1.6, 16), (x0 + side * 1.4, 15), (x0 + side * 2.4, 22), (x0 + side * 0.6, 27.5), (x0 - side * 0.8, 23)]
            im.shape(poly(lock), (x0 - 0.5, 20, 3.5, 8), "ginger", rim="full", tex=strands(h, 3))
        def hood(x, y):
            inner = ((x - cx) / 9.3) ** 2 + ((y - 15.2) / 11.2) ** 2 < 1
            return ellipse(cx, 13.2, 12.5, 13.6)(x, y) and not inner and y < 25
        im.shape(hood, (cx, 12, 15, 16), "green", rim="full", cast="skin")
        for side in (-1, 1):          # the ears, out through the hood
            ear = [(cx + side * 7.6, 14), (cx + side * 13.4, 9), (cx + side * 9.2, 17.5)]
            im.shape(poly(ear), (cx + side * 10, 13, 4, 5), "skin", rim="full", bias=0.2)
        rng = random.Random(5)
        for x in range(-1, 37, 2):    # the fur collar
            y = 25.5 + rng.choice((0, 0.8, 1.4)) + abs(x - 17.5) * 0.12
            im.shape(ellipse(x + 0.5, y, 2.4, 2.6), (x, y, 3.4, 3.4), "fur", rim="shade", bias=rng.uniform(-0.15, 0.12))
        im.tone(8, 3, "cream", 4); im.tone(7, 2, "cream", 3); im.tone(6, 1, "cream", 3); im.tone(7, 3, "teal", 2); im.tone(6, 2, "teal", 2)   # a feather in the hood
    elif name == "miranda":
        # The innkeeper: a warm face in a white coif with a gold band, brown hair, a gold pendant.
        h = Head(cx, 14.5, 8.4, 9.8, 0.2)
        im.shape(ellipse(cx, 13.5, 12, 13), (cx, 13, 15, 16), "white", rim="full")
        shoulders(im, "white", 25, 3, bias=-0.1)
        im.shape(ellipse(cx, 15, 9.4, 10.8), (cx, 15, 9.4, 10.8), ["140c0c"] * 5, rim=None)
        neck(im, h)
        h.face(im, eyes="6a4420", brows="brown", smile=True)
        im.tone(11, 19, "skin", 3); im.tone(23, 19, "skin", 3)
        hair_front(im, h, "braid", "brown")
        coif = lambda x, y: ellipse(cx, 13.2, 12, 13.2)(x, y) and not (((x - cx) / 9.2) ** 2 + ((y - 15.2) / 11) ** 2 < 1) and y < 25
        im.shape(coif, (cx, 12, 15, 16), "white", rim="full", cast="skin")
        for x in range(9, 26):
            y = 4 + int(abs(x - 17) ** 2 / 40)
            im.tone(x, y, "gold", 3 if x < 17 else 2); im.tone(x, y + 1, "gold", 1)
        im.tone(17, 4, "teal", 3); im.tone(17, 5, "teal", 2)
        im.line(14, 26, 17, 29, R["gold"][2]); im.line(20, 26, 17, 29, R["gold"][2])
        im.shape(ellipse(17.5, 30, 1.8, 1.8), (17.5, 30, 2, 2), "gold", rim="full")
    elif name == "sylthara":
        # The informant: a deep navy hood, a pale mask of a face with lit eyes.
        h = Head(cx, 15.5, 7.6, 9.0, 0.0)
        im.shape(ellipse(cx, 13.5, 13, 14), (cx, 13, 16, 17), "navy", rim="full")
        shoulders(im, "navy", 24, 4, bias=-0.1)
        im.shape(ellipse(cx, 16, 9.2, 10.6), (cx, 16, 9.2, 10.6), ["04040a"] * 5, rim=None)
        h.face(im, skin="pale", glow="aef0ff", ears=False, lips=False, brows="navy")
        hoodm = lambda x, y: ellipse(cx, 13.2, 13, 14.2)(x, y) and not (((x - cx) / 8.8) ** 2 + ((y - 17) / 10.4) ** 2 < 1) and y < 26
        im.shape(hoodm, (cx, 11, 16, 17), "navy", rim="full")
        for x in range(10, 25):       # the hood's shade across the brow
            for y in range(7, 12):
                if im.c[y][x] in R["pale"][2:]:
                    im.c[y][x] = R["pale"][1]
        im.shape(ellipse(17.5, 28, 1.8, 1.8), (17.5, 28, 2, 2), "teal", rim="full")
        im.line(13, 25, 16, 28, R["navy"][4]); im.line(22, 25, 19, 28, R["navy"][3])
    elif name == "garrett":
        # The merchant: a broad red hat with a gold band and a feather, a black beard, a red doublet with gold buttons.
        h = Head(cx, 15.0, 8.6, 9.6, 0.8)
        shoulders(im, "red", 25, 3)
        im.line(17, 26, 17, 32, R["red"][0])
        for y in (27, 30):
            im.tone(19, y, "gold", 4); im.tone(20, y, "gold", 2)
        im.shape(poly([(11, 24), (24, 24), (22, 27), (17.5, 28.5), (13, 27)]), (17.5, 25, 8, 4), "cream", rim="full")
        h.face(im, eyes="3a2a1a", brows="black", old=True, mouth=False)
        def beard(x, y):
            u = abs(x - cx)
            if y < 18.2:
                return u > 6.8 and y > 14.5 and h.mask(x, y)
            return y > 18.2 + (0.9 if u < 1 else 0) and u < 8.9 - max(0, y - 21) * 1.1
        im.shape(beard, (cx, 21, 11, 9), "black", rim="full", tex=lambda x, y, k: k + 1 if (x * 3 + y) % 5 == 0 else k)
        for d in (0, 1, 2, 3):
            a, b = h.cols(d)
            im.tone(a, 19 + (1 if d > 2 else 0), "black", 4 if d < 3 else 3); im.tone(b, 19 + (1 if d > 2 else 0), "black", 3)
        im.set(17, 21, LIP[0]); im.set(16, 21, LIP[0]); im.set(18, 21, LIP[0])
        brim = [(1, 9.5), (9, 7), (26, 7), (34, 9.5), (30, 12.5), (17.5, 11), (5, 12.5)]
        crown = lambda x, y: ellipse(cx, 7, 8.4, 6.6)(x, y) and y < 9
        im.shape(crown, (cx, 7, 10, 8), "red", rim="full")
        im.shape(poly(brim), (17.5, 12, 19, 6), "red", rim="full", cast="skin")
        for x in range(10, 26):
            im.tone(x, 7, "gold", 3 if x < 16 else 2); im.tone(x, 8, "gold", 1)
        feather = [(23, 6.5), (29, 0.5), (32.5, 0.5), (31, 4), (26, 7.5)]
        im.shape(poly(feather), bounds(feather), "cream", rim="full")
    elif name == "elara":
        # The scholar: black hair in a bun, gold spectacles, a navy robe with a key on a chain.
        h = Head(cx, 15.0, 8.2, 9.6, 0.0)
        hair_back(im, h, "bun", "black")
        shoulders(im, "navy", 25, 3)
        neck(im, h)
        im.shape(poly([(12, 25), (23, 25), (17.5, 30)]), (17.5, 26, 6, 5), "cream", rim="full")
        h.face(im, eyes="2a3a6a", brows="black")
        hair_front(im, h, "bun", "black")
        for side in (-1, 1):
            a = h.L0 - 4 if side < 0 else h.R0 + 4
            for (ox, oy) in ((-2, -1), (-2, 0), (-2, 1), (2, -1), (2, 0), (2, 1), (-1, 2), (0, 2), (1, 2), (-1, -2), (0, -2), (1, -2)):
                if not (oy == -2):
                    im.tone(a + ox, h.ey + oy, "gold", 3 if side < 0 else 2)
        for x in range(h.L0 - 1, h.R0 + 2):
            im.tone(x, h.ey - 1, "gold", 2)
        im.line(13, 26, 17, 30, R["gold"][2]); im.line(22, 26, 18, 30, R["gold"][1])
        im.tone(17, 31, "gold", 4); im.tone(18, 31, "gold", 2)
    im.outline()
    return im


def main():
    busts, small = [], []
    for i, spec in enumerate(ARCHERS, 1):
        im, head = archer(*spec)
        base = os.path.join(FACES, f"archer_{i}")
        save(im.rows(), base + ".png")
        save(im.region_rows(), base + "_regions.png")
        spots = head.spots()
        spots["comment"] = "A base face (portraits_16.py, remade 2026-11-12). Pixels from the top left, as x, y pairs: the irises' centres, the cheeks (where paint and freckles go), the brow and the chin."
        with open(base + ".json", "w") as f:
            json.dump(spots, f, indent=2)
        busts.append(im.rows(4))
        if i == 1:
            save(im.rows(2), os.path.join(PIXEL, "portrait_archer.png"))
    for name in ("brunhilde", "thorin", "lyriel", "miranda", "sylthara", "garrett", "elara"):
        im = keeper(name)
        save(im.rows(2), os.path.join(PIXEL, f"portrait_{name}.png"))
        small.append(im.rows(8))
    pad = (13, 15, 13, 255)
    def strip(pictures, gap=8):
        hgt = max(len(p) for p in pictures)
        rows = []
        for y in range(hgt):
            row = []
            for p in pictures:
                row.extend(p[y] if y < len(p) else [pad] * len(p[0]))
                row.extend([pad] * gap)
            rows.append(row)
        return rows
    parts = [strip(busts[:5]), strip(busts[5:]), strip(small[:4]), strip(small[4:])]
    wide = max(len(p[0]) for p in parts)
    sheet = []
    for p in parts:
        sheet.extend([row + [pad] * (wide - len(row)) for row in p])
        sheet.extend([[pad] * wide] * 8)
    save(sheet, os.path.join(P, "ArtSource", "pixel", "previews", "portraits_16.png"))
    print("wrote portraits")


main()
