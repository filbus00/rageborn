# The Iron Gaol's art in the 16x16 style (2026-11-14, the owner: "lets start building the next stage. enemies, assets,
# levels"): the walls (plain stone, a barred cell, shackles, a cell door, bloodied stone), the cut corners, the cages'
# bars, the stair doorway, wall fittings (torch, chains, banner), the flagstone floor, the gaol's props and two floor
# marks. Drawn from shapes by code with one palette, light from the top left, each drawn pixel 2 game pixels, at the
# screen's 27 pixels a unit like the forest's (forest_art.py).
#
# A wall cell is a block: the cell's diamond (14 x 7 drawn) as its top, and the two faces the camera sees below it, the
# left one (toward -x) lit, the right one (toward -y) in shade. Each cell's faces are their own stones between dark
# joints, so neighbouring blocks need not line up to the pixel (cells are 13.5 drawn pixels apart).
#
# Writes into Assets/_Project/Resources/Art (World, Dungeon, Ground), the list ArtSource/pixel/sixteen/gaol_art_sizes.txt
# ("Folder/name width height pivotX pivotY", in game pixels from the left and the bottom; Tools > ARPG > Configure Gaol
# Art sets each sprite's size and pivot from it) and a preview, ArtSource/pixel/previews/gaol_art_16.png.
# Run: python3 ArtSource/pixel/sixteen/gaol_art.py (plain Python, no Blender needed)
import os, random, struct, zlib, math

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
ART = os.path.join(P, "Assets", "_Project", "Resources", "Art")
K = "140f1c"
PAL = {
    "stone": ["1c1d28", "2c2e3c", "3f4253", "565a6e", "74798e"],   # joint, shade, middle, light, edge
    "top": ["15161f", "22232e", "30323f"],
    "iron": ["15151c", "3a3e4a", "6a7080", "9aa2b2", "c8d0dc"],
    "rust": ["5a2a1c", "8a4426"],
    "blood": ["4a1210", "7a1a14", "a82a20"],
    "wood": ["2e1c12", "4e3220", "74502e", "9a6a3a"],
    "fire": ["c8321a", "ff8a2a", "ffd84a", "fff6c0"],
    "bone": ["8a7c66", "c8bca4", "f0e8d8"],
    "straw": ["6a5216", "a8842a", "d8b848", "f0dc80"],
    "cloth": ["3e1014", "6a1a1e", "962a2a"],
    "brass": ["8a6a1a", "d8a830", "f8dc70"],
    "floor": ["191a23", "252630", "2e303c", "383b49"],
    "moss": ["2a3a30", "3a5240"],
    "dark": ["07080c", "0d0e14", "14161e"],
}
CW, CH, WALL_H, LOW_H = 14, 7, 27, 4
SCALE = 2


class Img:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.c = [[None] * w for _ in range(h)]

    def set(self, x, y, col):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h and col is not None:
            self.c[y][x] = col

    def get(self, x, y):
        return self.c[y][x] if 0 <= x < self.w and 0 <= y < self.h else None

    def rect(self, x0, y0, x1, y1, col):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, col)

    def line(self, x0, y0, x1, y1, col):
        n = int(max(abs(x1 - x0), abs(y1 - y0), 1))
        for k in range(n + 1):
            self.set(round(x0 + (x1 - x0) * k / n), round(y0 + (y1 - y0) * k / n), col)

    def blob(self, cx, cy, rx, ry, ramp, light=(-0.6, -0.7)):
        for y in range(self.h):
            for x in range(self.w):
                dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
                d = dx * dx + dy * dy
                if d <= 1.0:
                    lit = -(dx * light[0] + dy * light[1]) * 0.8 + (1 - d) * 0.35
                    k = 0 if lit < -0.35 else 1 if lit < 0.15 else 2 if lit < 0.55 else 3
                    self.set(x, y, ramp[min(k, len(ramp) - 1)])

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

    def paste(self, other, ox, oy):
        for y in range(other.h):
            for x in range(other.w):
                if other.c[y][x] is not None:
                    self.set(ox + x, oy + y, other.c[y][x])

    def mirrored(self):
        m = Img(self.w, self.h)
        m.c = [row[::-1] for row in self.c]
        return m

    def rows(self, scale=SCALE):
        out = []
        for row in self.c:
            line = []
            for c in row:
                px = (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), 255) if c else (0, 0, 0, 0)
                line += [px] * scale
            out += [line] * scale
        return out


def write_png(path, rows):
    h, w = len(rows), len(rows[0])
    raw = b"".join(b"\x00" + bytes(v for px in row for v in px) for row in rows)

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xffffffff)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) +
                chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def hash01(*v):
    h = 2166136261
    for a in v:
        h = ((h ^ (int(a) & 0xffffffff)) * 16777619) & 0xffffffff
    h ^= h >> 13
    h = (h * 0x5bd1e995) & 0xffffffff
    h ^= h >> 15
    return (h & 0xffffff) / float(0x1000000)


# The block ---------------------------------------------------------------------------------------------------------
def in_diamond(x, y):
    return abs(x + 0.5 - CW / 2) / (CW / 2) + abs(y + 0.5 - CH / 2) / (CH / 2) <= 1.0


def cap_bottom(x):
    """The last row of the top's diamond in a column."""
    m = x if x < CW // 2 else CW - 1 - x
    return 3 + m // 2


def face_uv(x, y):
    """A pixel of a block's faces: (face, u, v) with face 0 the left (lit), 1 the right, u from the block's outer edge to
    its front corner (0..6), v down the face from its top; None above the faces."""
    v = y - cap_bottom(x) - 1
    if v < 0:
        return None
    return (0, x, v) if x < CW // 2 else (1, CW - 1 - x, v)


def stone(face, u, v, height, seed):
    """The masonry of a face: courses four pixels high, their joints staggered, each stone its own tone."""
    s = PAL["stone"]
    course = v // 4
    if v == height - 1:
        return s[0]
    if u == 0:
        return s[0]                                   # the joint between this cell and the next along the wall
    if v % 4 == 3:
        return s[0]
    joints = (3,) if course % 2 == 0 else (2, 5)
    if u in joints:
        return s[0] if face == 0 else s[0]
    index = sum(1 for j in joints if u > j)
    tone = hash01(seed, face, course, index)
    base = 2 if face == 0 else 1
    if tone < 0.22:
        base -= 1
    elif tone > 0.84:
        base += 1
    base = max(1, base)
    # A light upper edge on each stone, and the lit front corner.
    if v % 4 == 0 and tone > 0.3:
        base += 1
    if u == 6 and face == 0:
        base = max(base, 3)
    return s[min(base, 4 if face == 0 else 3)]


def block(seed, height=WALL_H, half=None, feature=None):
    """A wall cell. half: None the whole block; "f" the near half (a cut corner whose floor is behind it), "l" the left
    half, "r" the right half. feature draws on the faces: f(face, u, v) -> colour or None."""
    im = Img(CW, height + CH)
    t = PAL["top"]
    for y in range(CH):
        for x in range(CW):
            if not in_diamond(x, y):
                continue
            if half == "f" and y < 3:
                continue
            if half == "l" and x >= CW // 2:
                continue
            if half == "r" and x < CW // 2:
                continue
            # The top: dark slabs, a lighter rim along the front edges, the far edges outlined.
            col = t[1] if hash01(seed, x // 3, y // 2, 7) > 0.25 else t[0]
            if y == cap_bottom(x):
                col = t[2]
            if not in_diamond(x, y - 1) or (half == "f" and y == 3):
                col = K
            im.set(x, y, col)
    for y in range(CH - 4, height + CH):
        for x in range(CW):
            if half == "l" and x >= CW // 2:
                continue
            if half == "r" and x < CW // 2:
                continue
            f = face_uv(x, y)
            if f is None or f[2] >= height:
                continue
            face, u, v = f
            col = feature(face, u, v) if feature else None
            im.set(x, y, col or stone(face, u, v, height, seed))
    return im


def flat_block(seed, height=WALL_H):
    """The cut corner whose floor lies before it ("b"): the far half of the cell, one flat face toward the camera."""
    im = Img(CW, height + CH)
    t = PAL["top"]
    s = PAL["stone"]
    for y in range(4):
        for x in range(CW):
            if in_diamond(x, y):
                im.set(x, y, K if not in_diamond(x, y - 1) else t[2] if y == 3 else t[1])
    for v in range(height):
        for x in range(CW):
            course = v // 4
            joints = (4, 10) if course % 2 == 0 else (1, 7, 12)
            if v == height - 1 or v % 4 == 3 or x in joints or x in (0, CW - 1):
                col = s[0]
            else:
                tone = hash01(seed, course, sum(1 for j in joints if x > j), 3)
                base = 2 if tone > 0.25 else 1
                if v % 4 == 0 and tone > 0.4:
                    base += 1
                col = s[base]
            im.set(x, 4 + v, col)
    return im


# What a wall carries, by face pixel -----------------------------------------------------------------------------------
def cell_window(face, u, v):
    i = PAL["iron"]
    if 1 <= u <= 5 and 7 <= v <= 17:
        if v == 7:
            return PAL["stone"][4 if face == 0 else 3]          # the lintel
        if v == 17:
            return PAL["stone"][3 if face == 0 else 2]          # the sill
        return i[3 if face == 0 else 2] if u in (1, 3, 5) else PAL["dark"][0]
    return None


def shackles(face, u, v):
    i = PAL["iron"]
    for cu in (2, 4):
        if u == cu and v == 6:
            return i[4]
        if u == cu and 7 <= v <= 13:
            return i[2] if v % 2 else i[3]
        if abs(u - cu) <= 0 and v in (14, 15):
            return i[1]
    if u == 3 and v == 15:
        return i[1]
    return None


def cell_door(face, u, v):
    w, i = PAL["wood"], PAL["iron"]
    if 1 <= u <= 5 and v >= 8:
        if v == 8:
            return PAL["stone"][4 if face == 0 else 3]
        if v in (12, 21):
            return i[2]                                        # iron bands
        if 14 <= v <= 16 and 2 <= u <= 4:
            return i[3] if u == 3 else PAL["dark"][0]          # the grille
        if u == 5 and v == 18:
            return PAL["brass"][1]                             # the lock
        return w[2 if (u + (0 if face == 0 else 1)) % 2 else 1] if face == 0 else w[1 if u % 2 else 0]
    return None


def bloodied(face, u, v):
    b, i = PAL["blood"], PAL["iron"]
    if face == 0:
        if u == 2 and 5 <= v <= 16:
            return b[2] if v < 9 else b[1]
        if u == 3 and 6 <= v <= 11:
            return b[1]
        if u == 4 and 5 <= v <= 20:
            return b[1] if v < 14 else b[0]
        if u in (1, 5) and v == 5:
            return b[1]
    else:
        if u == 3 and v == 5:
            return i[4]
        if u == 3 and 6 <= v <= 18:
            return i[2] if v % 2 else i[3]
        if u in (2, 4) and v == 19:
            return i[1]
    return None


WALLS = {
    "prison_wall_1": lambda: block(1),
    "prison_wall_2": lambda: block(2, feature=cell_window),
    "prison_wall_3": lambda: block(3, feature=shackles),
    "prison_wall_4": lambda: block(4, feature=cell_door),
    "prison_wall_5": lambda: block(5, feature=bloodied),
    "prison_wall_low_1": lambda: block(6, LOW_H),
    "prison_wall_low_2": lambda: block(7, LOW_H),
    "prison_diag_b": lambda: flat_block(8),
    "prison_diag_f": lambda: block(9, half="f"),
    "prison_diag_l": lambda: block(10, half="l"),
    "prison_diag_r": lambda: block(11, half="r"),
    "prison_diag_b_low": lambda: flat_block(12, LOW_H),
    "prison_diag_f_low": lambda: block(13, LOW_H, half="f"),
    "prison_diag_l_low": lambda: block(14, LOW_H, half="l"),
    "prison_diag_r_low": lambda: block(15, LOW_H, half="r"),
}


# Wall fittings: the block's size, only the fitting drawn, on the left face (_x) or mirrored on the right (_y) -------------
def fitting(kind):
    im = Img(CW, WALL_H + CH)
    i = PAL["iron"]

    def put(u, v, col):
        x = u
        im.set(x, cap_bottom(x) + 1 + v, col)
    if kind == "torch":
        for v in range(11, 16):
            put(3, v, PAL["wood"][2])
        put(2, 13, i[2]); put(4, 13, i[2]); put(3, 16, i[1])
        f = PAL["fire"]
        put(3, 10, f[1]); put(3, 9, f[2]); put(3, 8, f[3]); put(2, 9, f[1]); put(4, 9, f[1]); put(3, 7, f[2]); put(3, 6, f[0])
        put(2, 10, f[0]); put(4, 10, f[0])
    elif kind == "chains":
        for cu, length in ((2, 12), (5, 9)):
            put(cu, 4, i[4])
            for v in range(5, 5 + length):
                put(cu, v, i[2] if v % 2 else i[3])
            put(cu, 5 + length, i[1])
    else:
        c = PAL["cloth"]
        put(1, 4, i[3]); put(2, 4, i[3]); put(3, 4, i[3]); put(4, 4, i[3]); put(5, 4, i[3])
        for v in range(5, 18):
            for u in range(1, 6):
                if v > 14 and (u + v) % 2 == 0:
                    continue                                   # the tattered end
                put(u, v, c[2] if u < 3 else c[1])
        # A key in brass.
        b = PAL["brass"]
        put(3, 7, b[2]); put(2, 8, b[1]); put(4, 8, b[1]); put(3, 9, b[1]); put(3, 10, b[1]); put(3, 11, b[1]); put(4, 11, b[1]); put(3, 12, b[0])
    return im


FITTINGS = {}
for _kind in ("torch", "chains", "banner"):
    FITTINGS[f"gaol_wall_{_kind}_x"] = (lambda k=_kind: fitting(k))
    FITTINGS[f"gaol_wall_{_kind}_y"] = (lambda k=_kind: fitting(k).mirrored())


# The cages' bars: a post at the cell's middle and an arm of bars toward each neighbour with bars -----------------------
BAR_H = 19


def bars(sides):
    im = Img(7, BAR_H + 5)
    i = PAL["iron"]
    base = BAR_H + 2            # the post's foot row
    # dx along an arm, and the rows it climbs: the x run (w, e) rises to the right, the y run (s, n) to the left.
    arms = {"w": (range(-3, 0), -0.5), "e": (range(1, 4), -0.5), "s": (range(1, 4), 0.5), "n": (range(-3, 0), 0.5)}
    for side in sides:
        steps, slope = arms[side]
        for dx in steps:
            foot = base + round(dx * slope)
            x = 3 + dx
            im.set(x, foot - BAR_H + 4, i[2])       # the top rail
            im.set(x, foot - 1, i[2])               # the bottom rail
            if abs(dx) == 2:
                for y in range(foot - BAR_H + 3, foot + 1):
                    im.set(x, y, i[2] if (y % 5) else i[3])
    for y in range(base - BAR_H, base + 1):
        im.set(3, y, i[3] if y % 6 else i[4])
    im.set(3, base - BAR_H - 1, i[4])
    im.set(3, base - BAR_H, i[2])
    im.set(3, base, i[1])
    return im


BARS = {"prison_bars_post": lambda: bars("")}
for _n in range(1, 16):
    _s = "".join(ch for bit, ch in zip((1, 2, 4, 8), "wesn") if _n & bit)
    BARS["prison_bars_" + _s] = (lambda s=_s: bars(s))


# The stair doorway: three wall cells along the far wall, an arch in the middle one's face ------------------------------
def doorway(down, mirror):
    w, h = CW * 2, WALL_H + CH * 2
    im = Img(w, h)
    # Far cell (up left), the middle, the near one (down right): the wall runs along y, its -x faces toward the floor.
    for ox, oy, seed in ((0, 0, 21), (7, 3, 22), (14, 7, 23)):
        im.paste(block(seed), ox, oy)
    s, d = PAL["stone"], PAL["dark"]
    # The opening: across the middle face and a little of its neighbours, following the wall's slope.
    for x in range(5, 16):
        top = cap_bottom(x - 7 if 7 <= x < 14 else 0) + 1
        slope = (x - 7) // 2 + 3 + 4            # the face's top row at this column
        arch = 8 + (2 if x in (5, 15) else 1 if x in (6, 14) else 0)
        y0 = slope + arch
        y1 = slope + WALL_H - 1
        for y in range(y0, y1 + 1):
            depth = (y - y0) / max(1, y1 - y0)
            if down:
                col = d[0] if depth < 0.55 else d[1] if depth < 0.8 else d[2]
                if y in (y1 - 1, y1 - 4) and 7 <= x <= 13:
                    col = s[1]                               # steps going down into the dark
            else:
                col = d[2] if depth < 0.3 else d[1]
                if (y1 - y) % 4 == 0 and 7 <= x <= 13:
                    col = s[3] if y > y1 - 9 else s[2]       # steps climbing toward the light
                if depth < 0.2 and 8 <= x <= 12:
                    col = "3a3a2e"
            im.set(x, y, col)
        im.set(x, y0 - 1, s[4])                              # the arch's stones
        im.set(x, y0 - 2, s[3] if x % 2 else s[4])
    for x, extra in ((4, 0), (16, 0)):
        slope = (x - 7) // 2 + 3 + 4
        for y in range(slope + 9, slope + WALL_H):
            im.set(x, y, s[4] if y % 4 else s[3])
    # A torch either side.
    f = PAL["fire"]
    for x in (2, 18):
        slope = (x - 7) // 2 + 3 + 4
        for y in range(slope + 11, slope + 15):
            im.set(x, y, PAL["wood"][2])
        im.set(x, slope + 10, f[1]); im.set(x, slope + 9, f[2]); im.set(x, slope + 8, f[3]); im.set(x, slope + 7, f[0])
    return im.mirrored() if mirror else im


STAIRS = {"stairs_down_x_gaol": lambda: doorway(True, False), "stairs_up_x_gaol": lambda: doorway(False, False),
          "stairs_down_y_gaol": lambda: doorway(True, True), "stairs_up_y_gaol": lambda: doorway(False, True)}


# Props ---------------------------------------------------------------------------------------------------------------
def hanging_cage():
    im = Img(13, 36)
    i, b = PAL["iron"], PAL["bone"]
    for y in range(0, 14):
        im.set(6, y, i[2] if y % 2 else i[3])
    im.rect(2, 14, 10, 15, i[2]); im.rect(4, 13, 8, 13, i[2])
    im.rect(2, 27, 10, 28, i[1])
    for x in (2, 4, 6, 8, 10):
        im.line(x, 15, x, 27, i[3] if x in (2, 6) else i[2])
    # Who was left in it.
    im.blob(6, 19, 2.2, 2.0, b)
    im.set(5, 19, K); im.set(7, 19, K)
    for y in (22, 24):
        im.line(4, y, 8, y, b[1])
    im.line(6, 21, 6, 26, b[1])
    im.outline()
    for x in range(3, 10):
        if im.get(x, 34) is None:
            im.set(x, 34, PAL["dark"][2])
    for x in range(4, 9):
        im.set(x, 35, PAL["dark"][2])
    return im


def torture_bench():
    im = Img(22, 15)
    w, i = PAL["wood"], PAL["iron"]
    im.rect(2, 5, 19, 8, w[2]); im.rect(2, 5, 19, 5, w[3]); im.rect(2, 9, 19, 9, w[1])
    for x in (3, 18):
        im.rect(x, 10, x + 1, 13, w[1])
    # The rollers and their spokes, the ropes, old blood.
    for x in (2, 19):
        im.rect(x - 1, 3, x + 1, 5, w[3]); im.set(x, 2, i[3]); im.set(x, 4, i[1])
    im.line(4, 6, 8, 6, PAL["straw"][2]); im.line(13, 6, 17, 6, PAL["straw"][2])
    im.rect(9, 6, 12, 7, PAL["blood"][1]); im.set(10, 8, PAL["blood"][2]); im.set(11, 9, PAL["blood"][1])
    im.set(8, 7, i[3]); im.set(13, 7, i[3])
    return im.outline()


def stocks():
    im = Img(17, 17)
    w, i = PAL["wood"], PAL["iron"]
    for x in (2, 13):
        im.rect(x, 3, x + 1, 15, w[1]); im.line(x, 3, x, 15, w[2])
    im.rect(1, 5, 15, 7, w[2]); im.rect(1, 5, 15, 5, w[3])
    im.rect(1, 8, 15, 10, w[2]); im.rect(1, 10, 15, 10, w[1])
    for cx in (5, 8, 11):
        im.set(cx, 7, K); im.set(cx, 8, K)
    im.set(8, 6, K); im.set(8, 9, K)
    im.set(1, 7, i[3]); im.set(15, 7, i[3]); im.set(15, 8, i[2])
    return im.outline()


def floor_grate():
    im = Img(CW, CH)
    i = PAL["iron"]
    for y in range(CH):
        for x in range(CW):
            d = abs(x + 0.5 - 7) / 7 + abs(y + 0.5 - 3.5) / 3.5
            if d <= 0.92:
                rim = d > 0.68
                bar = (x + 2 * y) % 4 == 0 or (x - 2 * y) % 4 == 0
                im.set(x, y, i[2] if rim else i[1] if bar else PAL["dark"][0])
    return im


def straw_pile():
    im = Img(17, 10)
    im.blob(8.5, 6.5, 8, 3.6, PAL["straw"])
    rng = random.Random(3)
    for _ in range(14):
        x, y = rng.randint(2, 14), rng.randint(4, 8)
        if im.get(x, y) is not None:
            im.set(x, y, PAL["straw"][rng.choice((0, 3, 3))])
    im.outline()
    for x, y in ((0, 8), (16, 7), (3, 2), (12, 2)):
        im.set(x, y, PAL["straw"][2])
    return im


def bucket():
    im = Img(9, 10)
    w, i = PAL["wood"], PAL["iron"]
    im.rect(1, 3, 7, 8, w[2]); im.rect(1, 3, 2, 8, w[3]); im.rect(6, 3, 7, 8, w[1])
    im.rect(1, 4, 7, 4, i[2]); im.rect(1, 7, 7, 7, i[2])
    im.rect(2, 2, 6, 2, PAL["dark"][2])
    im.line(1, 2, 4, 0, i[3]); im.line(4, 0, 7, 2, i[3])
    return im.outline()


def brazier():
    im = Img(13, 20)
    i, f = PAL["iron"], PAL["fire"]
    im.line(6, 10, 2, 18, i[1]); im.line(6, 10, 10, 18, i[1]); im.line(6, 10, 6, 18, i[2])
    im.rect(2, 8, 10, 10, i[2]); im.rect(2, 8, 10, 8, i[3]); im.rect(3, 11, 9, 11, i[1])
    im.blob(6.5, 5.5, 3.6, 3.6, [f[0], f[1], f[2], f[3]], light=(0.0, 0.9))
    im.set(6, 1, f[1]); im.set(5, 2, f[0]); im.set(7, 2, f[1]); im.set(4, 8, f[1]); im.set(8, 8, f[1]); im.set(6, 8, f[2])
    return im.outline()


def torch():
    im = Img(7, 20)
    w, i, f = PAL["wood"], PAL["iron"], PAL["fire"]
    im.line(3, 7, 3, 18, w[2]); im.rect(2, 18, 4, 18, i[1]); im.rect(2, 7, 4, 8, i[2])
    im.blob(3.5, 4.2, 2.4, 3.2, [f[0], f[1], f[2], f[3]], light=(0.0, 0.9))
    im.set(3, 0, f[1])
    return im.outline()


def altar():
    im = Img(19, 15)
    s, b, f = PAL["stone"], PAL["bone"], PAL["fire"]
    im.rect(1, 6, 17, 13, s[2]); im.rect(1, 6, 17, 7, s[4]); im.rect(1, 8, 17, 8, s[3])
    im.rect(3, 9, 15, 13, s[1]); im.rect(1, 13, 17, 13, s[0])
    for x in (5, 9, 13):
        im.set(x, 10, s[0]); im.set(x, 12, s[0])
    # A skull between two candles; blood run down the front.
    im.blob(9.5, 4, 2.2, 2.0, b); im.set(8, 4, K); im.set(10, 4, K)
    for x in (3, 15):
        im.rect(x, 3, x, 5, b[2]); im.set(x, 2, f[2]); im.set(x, 1, f[1])
    im.set(7, 8, PAL["blood"][2]); im.set(7, 9, PAL["blood"][1]); im.set(7, 10, PAL["blood"][1]); im.set(11, 8, PAL["blood"][1])
    return im.outline()


def candles():
    im = Img(11, 12)
    b, f = PAL["bone"], PAL["fire"]
    for x, top in ((2, 6), (5, 3), (8, 7)):
        im.rect(x, top, x, 10, b[2]); im.set(x, 10, b[1])
        im.set(x, top - 1, f[2]); im.set(x, top - 2, f[1])
    im.rect(1, 10, 9, 10, b[1]); im.set(1, 10, b[0]); im.set(9, 10, b[0])
    return im.outline()


PROPS = {"prop_hanging_cage": hanging_cage, "prop_torture_bench": torture_bench, "prop_stocks": stocks,
         "prop_floor_grate": floor_grate, "prop_straw_pile": straw_pile, "prop_bucket": bucket, "prop_brazier": brazier,
         "prop_torch": torch, "prop_altar": altar, "prop_candles": candles}


# Floor marks (27 x 14 drawn, as the forest's) ------------------------------------------------------------------------
def decal(kind, seed):
    rng = random.Random(seed)
    im = Img(27, 14)

    def inside(x, y):
        return abs(x + 0.5 - 13.5) / 13.5 + abs(y + 0.5 - 7) / 7 < 0.8
    if kind == "rubble":
        s = PAL["stone"]
        for _ in range(9):
            x, y = rng.randint(3, 23), rng.randint(3, 10)
            if inside(x, y):
                im.set(x, y, s[2]); im.set(x + 1, y, s[1]); im.set(x, y - 1, s[3])
                if rng.random() < 0.4:
                    im.set(x + 1, y - 1, s[2])
    elif kind == "straw":
        st = PAL["straw"]
        for _ in range(26):
            x, y = rng.randint(2, 24), rng.randint(2, 11)
            if inside(x, y):
                c = st[rng.choice((1, 1, 2, 2, 3))]
                im.set(x, y, c); im.set(x + 1, y + rng.choice((0, 0, 1)), c)
    else:
        b = PAL["bone"]
        x, y = rng.randint(9, 16), rng.randint(5, 8)
        im.blob(x + 0.5, y, 2.2, 1.8, b)
        im.set(x - 1, y, K); im.set(x + 1, y, K); im.set(x, y + 1, b[0])
        im.line(x + 4, y + 1, x + 7, y, b[1]); im.set(x + 7, y, b[2])
    return im


DECALS = {f"decal_{k}_{n}": (lambda k=k, n=n: decal(k, n * 13 + len(k))) for k in ("rubble", "skull", "straw") for n in (1, 2)}


# The floor: flagstones drawn as they lie on screen (the picture is read on the screen's grid), seamless at 108 ---------
def floor(size=108):
    f = PAL["floor"]
    pitch = 13.5

    def slab(i, j):
        # The same slab wherever the picture repeats: x + 108 adds (8, 8) to (i, j), y + 108 adds (16, -16).
        return (i + j) % 16, (i - j) % 32
    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            a, b = (x + 2 * y) / pitch, (x - 2 * y) / pitch
            i, j = math.floor(a), math.floor(b)
            fa, fb = a - i, b - j
            p, q = slab(i, j)
            tone = hash01(p, q, 1)
            # Some slabs are two cells long: no joint between them.
            joined_a = hash01(*slab(i - 1, j), 2) < 0.3
            joined_b = not joined_a and hash01(*slab(i, j - 1), 3) < 0.2
            joint = (fa < 0.12 and not joined_a) or (fb < 0.12 and not joined_b)
            if joint:
                col = f[0]
            else:
                k = 1 if tone < 0.3 else 3 if tone > 0.82 else 2
                # A lighter upper edge, a worn darker middle now and then.
                if (fa < 0.24 and not joined_a) or (fb > 0.9):
                    k = min(3, k + 1)
                speck = hash01(x, y, 5)
                if speck < 0.035:
                    k = max(1, k - 1)
                col = f[k]
                if speck > 0.992:
                    col = PAL["moss"][0]
                # A crack across a few slabs.
                if tone > 0.9 and abs(fa - fb) < 0.05 and 0.25 < fa < 0.8:
                    col = f[0]
            row.append((int(col[0:2], 16), int(col[2:4], 16), int(col[4:6], 16), 255))
        rows.append(row)
    return rows


# Writing -------------------------------------------------------------------------------------------------------------
def pivot_of(name, im):
    """The pivot in game pixels from the left and the bottom. A piece stands with the cell's middle a quarter unit (6.75
    pixels) up from its bottom; the bars stand on their post's foot; the doorway on its middle cell."""
    w, h = im.w * SCALE, im.h * SCALE
    if name.startswith("prison_bars"):
        return w / 2.0, 2 * SCALE + 1
    if name.startswith("stairs_"):
        return w / 2.0, 4 * SCALE + 6.75
    if name == "prop_floor_grate":
        return w / 2.0, h / 2.0
    return w / 2.0, 6.75


written, preview = [], []
for group, folder in ((WALLS, "World"), (FITTINGS, "World"), (BARS, "World"), (STAIRS, "World"), (PROPS, "Dungeon"), (DECALS, "Dungeon")):
    for name, make in group.items():
        im = make()
        write_png(os.path.join(ART, folder, name + ".png"), im.rows())
        px, py = pivot_of(name, im)
        written.append(f"{folder}/{name} {im.w * SCALE} {im.h * SCALE} {px:g} {py:g}")
        preview.append(im)
write_png(os.path.join(ART, "Ground", "gaol_floor.png"), floor())
with open(os.path.join(HERE, "gaol_art_sizes.txt"), "w") as fh:
    fh.write("\n".join(written) + "\n")


def scene():
    """A corner of a hall as the game lays it out: the floor, two walls meeting at the back, a cage, props."""
    w, h = 150, 125
    im = Img(w, h)
    fl = floor()
    for y in range(h):
        for x in range(w):
            r, g, b, _ = fl[y % 108][x % 108]
            im.set(x, y, f"{r:02x}{g:02x}{b:02x}")

    def at(cx, cy):
        # A cell's block corner on the canvas: x to the right and up, y to the left and up.
        return round(75 + (cx - cy) * 6.75), round(112 - (cx + cy) * 3.375)
    cells = []
    for k in range(0, 9):
        cells.append((k, 9, "x"))       # the wall along the top right (its -y face shows)
        cells.append((9, k, "y"))
    cells.append((9, 9, "c"))
    names = ["prison_wall_2", "prison_wall_1", "prison_wall_3", "prison_wall_2", "prison_wall_4", "prison_wall_1", "prison_wall_5",
             "prison_wall_2", "prison_wall_1"]
    for cx, cy, kind in sorted(cells, key=lambda c: -(c[0] + c[1])):
        ox, oy = at(cx, cy)
        name = names[(cx if kind == "x" else cy) % len(names)] if kind != "c" else "prison_wall_1"
        im.paste(WALLS[name](), ox - 7, oy - WALL_H - 3)
    for name, cx, cy in (("prison_bars_we", 4, 5), ("prison_bars_we", 5, 5), ("prison_bars_ws", 6, 5), ("prison_bars_sn", 6, 4),
                         ("prison_bars_n", 6, 3)):
        ox, oy = at(cx, cy)
        b = BARS[name]()
        im.paste(b, ox - 3, oy - b.h + 3 - 3)
    for name, cx, cy in (("prop_hanging_cage", 7, 7), ("prop_torture_bench", 3, 7), ("prop_stocks", 7, 2), ("prop_brazier", 5, 7),
                         ("prop_straw_pile", 5, 4), ("prop_bucket", 2, 4), ("prop_altar", 3, 2), ("prop_torch", 8, 5),
                         ("prop_floor_grate", 4, 3)):
        ox, oy = at(cx, cy)
        p = PROPS[name]()
        im.paste(p, ox - p.w // 2, oy - p.h + (3 if name != "prop_floor_grate" else 6))
    return im


rows, row, width = [], [], 0
for im in preview:
    if width + im.w > 250:
        rows.append(row); row, width = [], 0
    row.append(im); width += im.w + 3
rows.append(row)
sheet_h = sum(max(i.h for i in r) + 3 for r in rows)
sheet = Img(256, sheet_h)
for y in range(sheet_h):
    for x in range(256):
        sheet.set(x, y, "2a2c38")
y = 0
for r in rows:
    hh = max(i.h for i in r)
    x = 2
    for im in r:
        sheet.paste(im, x, y + hh - im.h)
        x += im.w + 3
    y += hh + 3
sc = scene()
write_png(os.path.join(P, "ArtSource", "pixel", "previews", "gaol_scene_16.png"), sc.rows(6))
both = Img(max(256, sc.w), sheet.h + sc.h + 4)
for yy in range(both.h):
    for xx in range(both.w):
        both.set(xx, yy, "2a2c38")
both.paste(sc, 0, 0)
both.paste(sheet, 0, sc.h + 4)
write_png(os.path.join(P, "ArtSource", "pixel", "previews", "gaol_art_16.png"), both.rows(5))
print("wrote", len(written), "pieces and the floor")
