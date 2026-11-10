# The forest's art in the 16x16 style (2026-11-07, the owner: "remake all assets to be 16x16"): the trees, bushes, props,
# floor marks, the way on (stairs) and the ground pictures, drawn from shapes by code with one palette, light from the top
# left, dark outlines, each drawn pixel 2 game pixels (the ground pictures are sampled in 2 x 2 blocks by GroundPainter).
# Every piece keeps its file name and about its old size, so the game places them as before.
# Writes into Assets/_Project/Resources/Art (World and Dungeon pieces, Ground pictures) and a preview of all of them.
# Run: Blender -b -P ArtSource/pixel/sixteen/forest_art.py
import bpy, os, math, random
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
ART = os.path.join(P, "Assets", "_Project", "Resources", "Art")

PAL = {
    "K": "140f1c",
    # Canopies: dark green, autumn orange, autumn gold, pine, each from shadow to light.
    "green": ["1c3a26", "2a5634", "3e7a40", "6aa24a"],
    "orange": ["6a2618", "a8401c", "dc7428", "f4b04a"],
    "gold": ["6a4e14", "a8801e", "dcb62e", "f6e070"],
    "pine": ["12301f", "1c4a2e", "2e6a3e", "4e8a4a"],
    "trunk": ["2e1c12", "4e3220", "74502e"],
    "birch": ["8a8a80", "c8c8bc", "eeeee4"],
    "stone": ["3e3e48", "62626e", "8e8e9a", "c0c0ca"],
    "wood": ["3e2616", "6a4224", "9a6a3a", "c8965a"],
    "bone": ["8a7c66", "c8bca4", "f0e8d8"],
    "red": ["7a1a14", "c8322a", "f0604a"],
    "fire": ["c8321a", "ff8a2a", "ffd84a", "fff6c0"],
    "cloth": ["5a2a2a", "8a3a32", "c85a40"],
    "canvas": ["6a5a3e", "a08a5e", "d0bc8a"],
    "web": ["a8a8b4", "e8e8f0"],
    "water": ["1a2a3a", "2e4a62", "6a90b0"],
    "flower": ["fff4e0", "ffd84a", "e8607a", "b0a0ff"],
    "teal": ["2fd4c4"],
}


def hexc(h):
    return [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1.0]


class Img:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.c = [[None] * w for _ in range(h)]   # a colour per pixel, or None

    def set(self, x, y, col):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h and col is not None:
            self.c[y][x] = col

    def get(self, x, y):
        return self.c[y][x] if 0 <= x < self.w and 0 <= y < self.h else None

    def blob(self, cx, cy, rx, ry, ramp, light=(-0.6, -0.7), rng=None, rough=0.0):
        """An ellipse shaded by a light from the top left, its edge roughened."""
        for y in range(self.h):
            for x in range(self.w):
                dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
                d = dx * dx + dy * dy
                edge = 1.0 + (rng.uniform(-rough, rough) if rng else 0.0)
                if d <= edge:
                    lit = -(dx * light[0] + dy * light[1]) * 0.8 + (1 - d) * 0.35
                    k = 0 if lit < -0.35 else 1 if lit < 0.15 else 2 if lit < 0.55 else 3
                    self.set(x, y, ramp[min(k, len(ramp) - 1)])

    def rect(self, x0, y0, x1, y1, col):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, col)

    def line(self, x0, y0, x1, y1, col):
        n = int(max(abs(x1 - x0), abs(y1 - y0), 1))
        for k in range(n + 1):
            self.set(round(x0 + (x1 - x0) * k / n), round(y0 + (y1 - y0) * k / n), col)

    def outline(self):
        out = [row[:] for row in self.c]
        for y in range(self.h):
            for x in range(self.w):
                if self.c[y][x] is None:
                    for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        n = self.get(x + ox, y + oy)
                        if n is not None and n != PAL["K"]:
                            out[y][x] = PAL["K"]
                            break
        self.c = out
        return self

    def rgba(self, scale=2):
        a = np.zeros((self.h, self.w, 4), np.float32)
        for y in range(self.h):
            for x in range(self.w):
                if self.c[y][x] is not None:
                    a[y, x] = hexc(self.c[y][x])
        return np.repeat(np.repeat(a, scale, 0), scale, 1)


def save(arr, path):
    h, w = arr.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = arr[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


# Trees -----------------------------------------------------------------------------------------------------------
def deciduous(w, h, ramp, seed, trunk="trunk"):
    rng = random.Random(seed)
    im = Img(w, h)
    base = h - 2
    tw = 2 if w < 24 else 3
    cx = w / 2
    im.rect(cx - tw / 2, h * 0.55, cx + tw / 2 - 1, base, PAL[trunk][1])
    im.rect(cx - tw / 2, h * 0.55, cx - tw / 2, base, PAL[trunk][2])
    im.set(cx - tw / 2 - 1, base, PAL[trunk][1])
    im.set(cx + tw / 2, base, PAL[trunk][0])
    # The canopy: clusters of small leafy bumps, each lit from the top left, drawn from the back (top) to the front,
    # darker toward the bottom right of the crown, inside the crown's outline.
    ccx, ccy, crx, cry = cx, h * 0.36, w * 0.46, h * 0.32
    bumps = []
    for _ in range(int(w * h / 14)):
        ang = rng.uniform(0, math.tau)
        r = math.sqrt(rng.random())
        bumps.append((ccx + math.cos(ang) * r * crx * 0.85, ccy + math.sin(ang) * r * cry * 0.85, rng.uniform(2.2, 3.6)))
    bumps.sort(key=lambda b: b[1] + b[0] * 0.3)
    ramp = PAL[ramp]
    for bx, by, br in bumps:
        depth = ((bx - ccx) / crx * 0.5 + (by - ccy) / cry * 0.8)
        base = 2 if depth < -0.25 else 1 if depth < 0.45 else 0
        for y in range(int(by - br), int(by + br) + 1):
            for x in range(int(bx - br), int(bx + br) + 1):
                dx, dy = (x + 0.5 - bx) / br, (y + 0.5 - by) / br
                if dx * dx + dy * dy <= 1:
                    k = base + (1 if dx + dy < -0.5 else 0) - (1 if dx + dy > 0.7 else 0)
                    im.set(x, y, ramp[max(0, min(3, k))])
    return im.outline()


def pine(w, h, seed):
    rng = random.Random(seed)
    im = Img(w, h)
    cx = w / 2
    im.rect(cx - 1, h - 6, cx, h - 2, PAL["trunk"][1])
    tiers = 4
    for t in range(tiers):
        top = 1 + t * (h - 8) / tiers * 0.9
        bottom = top + (h - 8) / tiers * 1.5
        half_bottom = (w / 2 - 1) * (0.45 + 0.55 * (t + 1) / tiers)
        for y in range(int(top), int(min(bottom, h - 5)) + 1):
            f = (y - top) / max(1, bottom - top)
            half = max(0.6, half_bottom * f)
            for x in range(int(cx - half), int(cx + half) + 1):
                side = (x - cx) / max(1, half)
                k = 3 if side < -0.5 and f < 0.6 else 2 if side < 0 else 1 if side < 0.6 else 0
                if f > 0.85:
                    k = max(0, k - 1)
                im.set(x, y, PAL["pine"][k])
    return im.outline()


def birch(w, h, ramp, seed):
    im = deciduous(w, h, ramp, seed, trunk="birch")
    cx = w / 2
    for y in range(int(h * 0.58), h - 2, 3):
        im.set(cx - 1, y, PAL["K"])
    return im


TREES = {
    "forest_tree_1": lambda: pine(19, 39, 1),
    "forest_tree_2": lambda: pine(20, 38, 2),
    "forest_tree_3": lambda: birch(20, 39, "gold", 3),
    "forest_tree_4": lambda: deciduous(36, 37, "green", 4),
    "forest_tree_5": lambda: deciduous(36, 38, "orange", 5),
    "forest_tree_6": lambda: deciduous(37, 37, "gold", 6),
    "forest_tree_7": lambda: deciduous(26, 38, "orange", 7),
    "forest_tree_8": lambda: deciduous(41, 36, "green", 8),
    "forest_tree_9": lambda: birch(20, 38, "green", 9),
}


def bush(w, h, ramp, seed):
    rng = random.Random(seed)
    im = Img(w, h)
    for bx, by, rx, ry in [(w * 0.35, h * 0.62, w * 0.3, h * 0.35), (w * 0.65, h * 0.6, w * 0.3, h * 0.38), (w * 0.5, h * 0.45, w * 0.34, h * 0.38)]:
        im.blob(bx, by, rx, ry, PAL[ramp], rng=rng, rough=0.15)
    for _ in range(3):
        x, y = rng.randint(2, w - 3), rng.randint(2, h - 4)
        if im.get(x, y) is not None:
            im.set(x, y, PAL["red"][2] if seed % 2 else PAL["flower"][0])
    return im.outline()


BUSHES = {"forest_bush_1": lambda: bush(17, 14, "green", 1), "forest_bush_2": lambda: bush(15, 13, "orange", 2),
          "forest_bush_3": lambda: bush(14, 12, "green", 3)}


# Props -----------------------------------------------------------------------------------------------------------
def stump(axe=False):
    """A cut stump: bark with grooves, roots at the foot, a pale top with growth rings and a crack. With the axe its
    blade is sunk in the top, the head standing out of the wood and the handle rising to the right."""
    W, top = (22, 7) if axe else (17, 1)
    im = Img(W, top + 15)
    cx, rx, ry = (W - 1) / 2 - (1.5 if axe else 0), 6.5, 2.6
    ty, by = top + 3, top + 9            # the centres of the cut top and of the foot
    bark = [PAL["wood"][0], PAL["wood"][1], PAL["wood"][2]]
    # The trunk: shaded across, grooves, flaring into its roots at a rounded foot.
    for y in range(int(ty), im.h):
        rr = rx + max(0.0, y - by + 1.5) * 0.8
        for x in range(W):
            dx = (x + 0.5 - cx) / rr
            if abs(dx) > 1 or y > by + ry * math.sqrt(1 - dx * dx):
                continue
            col = bark[2] if dx < -0.55 else bark[1] if dx < 0.45 else bark[0]
            if x % 3 == 1 and (y + x) % 5 != 0:
                col = bark[0] if dx > -0.55 else bark[1]
            if abs((x + 0.5 - cx) / rx) > 1:
                col = bark[1] if dx < 0 else bark[0]
            im.set(x, y, col)
    # The cut top: a bark rim, pale wood, rings, the pith, a crack.
    im.blob(cx, ty, rx + 0.3, ry + 0.3, [bark[1]] * 4)
    im.blob(cx, ty, rx - 0.7, ry - 0.5, [PAL["wood"][3]] * 4)
    for k in (0.55,):
        for a in range(48):
            t = a / 48 * math.tau
            im.set(cx + (rx - 0.7) * k * math.cos(t), ty + (ry - 0.5) * k * math.sin(t), PAL["wood"][2])
    im.set(cx, ty, bark[1])
    for x, y in [(cx - 4, ty - 1), (cx - 3, ty - 1), (cx - 2, ty - 2)]:
        im.set(x, y, "e8c48a")
    im.line(cx + 1, ty, cx + 4, ty + 1, bark[1])
    if axe:
        # The head: perpendicular to the handle, its poll up and left, its blade down and right into the wood
        # (a dark slit where it goes in); the handle from its eye up to the right, a knob at the end.
        hx, hy = cx - 1, ty - 1
        steel = PAL["stone"]
        head = [".ab.....",
                "abcb....",
                ".bccd...",
                "..bcdd..",
                "...bcde.",
                "....KKKK"]
        tone = {"a": steel[0], "b": steel[1], "c": steel[2], "d": steel[3], "e": "e8e8f0", "K": PAL["K"]}
        for r, row in enumerate(head):
            for c, ch in enumerate(row):
                if ch != ".":
                    im.set(hx - 3 + c, hy - 3 + r, tone[ch])
        handle = [PAL["trunk"][0], PAL["trunk"][2], "a87848"]
        for k in range(7):
            x, y = hx + 1 + k, hy - 2 - k
            im.set(x, y, handle[2] if k % 4 != 3 else handle[1])
            im.set(x + 1, y, handle[1]); im.set(x + 1, y + 1, handle[0])
        im.set(hx + 8, hy - 9, handle[0]); im.set(hx + 9, hy - 9, handle[0]); im.set(hx + 9, hy - 8, handle[0])
    return im.outline()


def ferns():
    im = Img(18, 13)
    rng = random.Random(4)
    for k, (tx, ty) in enumerate([(2, 4), (5, 1), (9, 0), (13, 1), (16, 5), (8, 3), (11, 5)]):
        col = PAL["green"][2 + (k % 2)]
        im.line(9, 12, tx, ty, PAL["green"][1])
        for s in range(1, 6):
            x = round(9 + (tx - 9) * s / 6)
            y = round(12 + (ty - 12) * s / 6)
            im.set(x - 1, y, col); im.set(x + 1, y, col)
    return im.outline()


def mushrooms():
    im = Img(10, 10)
    for cx, cy, r in [(3, 5, 2.6), (7, 6.5, 2.0), (5.5, 3, 1.8)]:
        im.rect(cx - 0.5, cy, cx, cy + 3, PAL["bone"][2])
        im.blob(cx, cy, r, r * 0.7, PAL["red"])
        im.set(cx - 1, cy - 1, PAL["flower"][0])
    return im.outline()


def log():
    im = Img(19, 14)
    # From the far end (up the screen) to the near one, so the near end draws in front, its cut face to the camera.
    for t in reversed(range(12)):
        x, y = 4 + t, 9 - t * 0.35
        im.blob(x, y, 2.8, 2.8, [PAL["wood"][0], PAL["wood"][1], PAL["wood"][2], PAL["wood"][2]])
    im.blob(3.2, 9.2, 2.4, 2.8, [PAL["wood"][2], PAL["wood"][3], PAL["wood"][3], PAL["wood"][3]])
    im.set(3, 9, PAL["wood"][1])
    im.set(11, 5, PAL["green"][3]); im.set(12, 5, PAL["green"][2]); im.set(8, 6, PAL["green"][2])
    return im.outline()


def standing_stone():
    im = Img(11, 20)
    im.blob(5.5, 10, 4.2, 9, PAL["stone"])
    im.rect(1, 16, 9, 18, PAL["stone"][1])
    for y in (6, 9, 12):
        im.set(5, y, PAL["teal"][0])
    im.set(4, 7, PAL["teal"][0]); im.set(6, 10, PAL["teal"][0])
    return im.outline()


def bone_pile():
    im = Img(14, 11)
    for x0, y0, x1, y1 in [(1, 9, 9, 6), (4, 6, 12, 9), (2, 7, 7, 9), (8, 5, 12, 6)]:
        im.line(x0, y0, x1, y1, PAL["bone"][1])
        im.set(x0, y0, PAL["bone"][2]); im.set(x1, y1, PAL["bone"][2])
    im.blob(7, 4, 2.6, 2.2, PAL["bone"])
    im.set(6, 4, PAL["K"]); im.set(8, 4, PAL["K"])
    return im.outline()


def web_sac():
    im = Img(14, 13)
    for x1, y1 in [(0, 0), (13, 1), (0, 12), (13, 12), (7, 0)]:
        im.line(7, 6, x1, y1, PAL["web"][0])
    im.blob(7, 7, 3.5, 4.2, [PAL["web"][0], PAL["web"][1], PAL["web"][1], PAL["web"][1]])
    im.set(6, 5, PAL["bone"][2])
    return im.outline()


def campfire():
    im = Img(12, 20)
    for k in range(7):
        a = k / 7 * math.tau
        im.blob(6 + 4.2 * math.cos(a), 16 + 2.2 * math.sin(a), 1.2, 0.9, PAL["stone"][1:])
    im.line(3, 17, 9, 14, PAL["wood"][1]); im.line(3, 14, 9, 17, PAL["wood"][2])
    for y in range(6, 16):
        half = (y - 6) * 0.28 + 0.3     # a point at the top, widest over the logs
        for x in range(int(6 - half), int(6 + half) + 1):
            d = abs(x + 0.5 - 6) / max(half, 0.5)
            k = 3 if d < 0.3 and y > 11 else 2 if d < 0.6 else 1 if d < 0.9 else 0
            im.set(x, y, PAL["fire"][k])
    return im.outline()


def sack():
    im = Img(10, 13)
    im.blob(5, 8, 4, 4.5, PAL["canvas"])
    im.rect(4, 2, 5, 4, PAL["canvas"][1]); im.set(3, 1, PAL["canvas"][2]); im.set(6, 1, PAL["canvas"][1])
    im.rect(3, 4, 6, 4, PAL["cloth"][1])
    return im.outline()


def crate():
    im = Img(12, 15)
    im.rect(1, 4, 10, 13, PAL["wood"][2])
    im.rect(1, 1, 10, 4, PAL["wood"][3])
    im.rect(8, 4, 10, 13, PAL["wood"][1])
    for y in (4, 8, 13):
        im.line(1, y, 10, y, PAL["wood"][0])
    im.line(1, 4, 7, 13, PAL["wood"][1])
    return im.outline()


def barrel():
    im = Img(11, 13)
    im.blob(5.5, 7, 4.6, 5.5, PAL["wood"])
    im.rect(1, 2, 9, 11, PAL["wood"][2])
    im.rect(7, 2, 9, 11, PAL["wood"][1])
    im.blob(5.5, 2, 4, 1.4, [PAL["wood"][3]] * 4)
    for y in (4, 9):
        im.line(1, y, 9, y, PAL["stone"][1])
    return im.outline()


def weapon_rack():
    im = Img(16, 21)
    im.rect(1, 6, 1, 19, PAL["wood"][1]); im.rect(14, 6, 14, 19, PAL["wood"][1])
    im.line(1, 8, 14, 8, PAL["wood"][2]); im.line(1, 16, 14, 16, PAL["wood"][2])
    for x in (4, 7, 10, 12):
        im.line(x, 3, x, 18, PAL["wood"][2])
        im.set(x, 2, PAL["stone"][3]); im.set(x, 1, PAL["stone"][2]); im.set(x, 3, PAL["stone"][2])
    return im.outline()


def cage():
    im = Img(15, 19)
    im.rect(1, 2, 13, 3, PAL["stone"][1])
    im.rect(1, 16, 13, 17, PAL["stone"][1])
    for x in range(1, 14, 3):
        im.line(x, 3, x, 16, PAL["stone"][2])
    im.line(7, 0, 7, 2, PAL["stone"][1])
    im.blob(7, 13, 2.5, 2, PAL["bone"])
    return im.outline()


def carcass():
    im = Img(20, 12)
    im.blob(10, 7, 8, 3.8, PAL["cloth"])
    for x in range(6, 14, 2):
        im.line(x, 4, x, 9, PAL["bone"][2])
    im.blob(17, 6, 2.2, 1.8, PAL["bone"])
    im.line(18, 4, 19, 1, PAL["bone"][1]); im.line(17, 4, 16, 1, PAL["bone"][1])
    return im.outline()


def tent():
    im = Img(22, 23)
    for y in range(2, 21):
        f = (y - 2) / 18
        half = 1 + f * 9.5
        for x in range(int(11 - half), int(11 + half) + 1):
            stripe = int((x - 11 + half) / 3) % 2
            shade = 2 if x < 11 else 1
            im.set(x, y, PAL["canvas"][shade] if stripe else PAL["cloth"][shade])
    for y in range(12, 21):
        half = (y - 12) * 0.35
        for x in range(int(11 - half), int(11 + half) + 1):
            im.set(x, y, PAL["K"])
    im.line(11, 0, 11, 2, PAL["wood"][2])
    return im.outline()


PROPS = {"prop_ferns": ferns, "prop_mushrooms": mushrooms, "prop_stump": lambda: stump(), "prop_fallen_log": log,
         "prop_standing_stone": standing_stone, "prop_axe_stump": lambda: stump(axe=True), "prop_bone_pile": bone_pile,
         "prop_web_sac": web_sac, "prop_campfire": campfire, "prop_sack": sack, "prop_crate": crate, "prop_barrel": barrel,
         "prop_weapon_rack": weapon_rack, "prop_cage": cage, "prop_carcass": carcass, "prop_bandit_tent": tent}


# Floor marks (27 x 14 drawn: two cells' diamond) -----------------------------------------------------------------
def decal(kind, seed):
    rng = random.Random(seed)
    im = Img(27, 14)

    def inside(x, y):
        return abs(x + 0.5 - 13.5) / 13.5 + abs(y + 0.5 - 7) / 7 < 0.85

    def scatter(n, cols, size=1):
        for _ in range(n):
            x, y = rng.randint(1, 25), rng.randint(1, 12)
            if inside(x, y):
                c = rng.choice(cols)
                im.set(x, y, c)
                if size > 1:
                    im.set(x + 1, y, c)
    if kind == "leaves":
        scatter(22, [PAL["orange"][2], PAL["orange"][1], PAL["gold"][2], PAL["orange"][3]], 2)
    elif kind == "flowers":
        for _ in range(9):
            x, y = rng.randint(3, 23), rng.randint(2, 11)
            if inside(x, y):
                c = rng.choice(PAL["flower"])
                im.set(x, y, c); im.set(x, y + 1, PAL["green"][2])
    elif kind == "pebbles":
        for _ in range(8):
            x, y = rng.randint(2, 24), rng.randint(2, 11)
            if inside(x, y):
                im.set(x, y, PAL["stone"][2]); im.set(x + 1, y, PAL["stone"][1]); im.set(x, y - 1, PAL["stone"][3])
    elif kind == "puddle":
        im.blob(13.5, 7, 9, 4, [PAL["water"][0], PAL["water"][1], PAL["water"][1], PAL["water"][2]])
        im.line(9, 5, 12, 5, PAL["water"][2])
    elif kind == "bones":
        for _ in range(3):
            x, y = rng.randint(5, 20), rng.randint(4, 9)
            im.line(x, y, x + 3, y + rng.choice((-1, 1)), PAL["bone"][1])
            im.set(x, y, PAL["bone"][2])
        im.blob(rng.randint(8, 18), 7, 1.5, 1.2, PAL["bone"])
    return im


DECALS = {f"decal_{k}_{i}": (lambda k=k, i=i: decal(k, i * 17 + len(k))) for k in ("leaves", "flowers", "pebbles", "puddle", "bones") for i in (1, 2)}


# The way on: a path into a dark gap between trees (stairs up and down, both orientations), drawn at 27 a unit now.
def way_on(mirror):
    im = Img(32, 41)
    rng = random.Random(11)
    # The dark hollow under the trees, then the canopy over it and the trail running into it.
    im.blob(16, 22, 11, 10, ["07080c", "0c0e12", "12161a", "1a2020"], light=(0.0, 0.8))
    for bx, by, rx, ry, ramp in [(4, 18, 5, 9, "pine"), (28, 18, 5, 9, "green"), (8, 7, 8, 6, "green"), (24, 7, 8, 6, "pine"),
                                 (16, 5, 8, 5, "pine")]:
        im.blob(bx, by, rx, ry, PAL[ramp], rng=rng, rough=0.15)
    # The trail after the outline, so it runs out into the ground with no edge.
    im.outline()
    trail = ["4e3826", "634830", "7a5a3a"]
    for y in range(24, 41):
        f = (y - 24) / 16
        half = 1 + f * 5
        cx = 16 + math.sin(f * 2.4) * 2
        for x in range(int(cx - half), int(cx + half) + 1):
            k = 2 if x < cx - half * 0.3 else 1 if x < cx + half * 0.5 else 0
            im.set(x, y, trail[k] if f > 0.15 else trail[0])
    for _ in range(6):
        x, y = rng.randint(10, 22), rng.randint(30, 40)
        if im.get(x, y) in trail:
            im.set(x, y, PAL["stone"][2])
    if mirror:
        im.c = [row[::-1] for row in im.c]
    return im


STAIRS = {"stairs_down_x_forest": lambda: way_on(False), "stairs_up_x_forest": lambda: way_on(False),
          "stairs_down_y_forest": lambda: way_on(True), "stairs_up_y_forest": lambda: way_on(True)}


# Ground pictures: seamless, 108 drawn pixels (sampled in 2 x 2 blocks, so 216 screen pixels a repeat).
def ground(kind, size=108):
    rng = np.random.default_rng(5 if kind == "grass" else 6)
    xs = np.arange(size)
    field = np.zeros((size, size))
    for _ in range(14):
        fx, fy = rng.integers(1, 5), rng.integers(1, 5)
        ph = rng.uniform(0, math.tau)
        field += np.sin(2 * math.pi * (fx * xs[None, :] + fy * xs[:, None]) / size + ph) / (fx + fy)
    field = (field - field.min()) / (field.max() - field.min())
    noise = rng.random((size, size))
    ramp = (["1a3424", "24452c", "2e5634", "3e6a3c"] if kind == "grass" else ["3a2a1e", "4e3826", "634830", "7a5a3a"])
    img = np.zeros((size, size, 4), np.float32)
    for y in range(size):
        for x in range(size):
            v = field[y, x] * 0.8 + noise[y, x] * 0.35
            k = 0 if v < 0.35 else 1 if v < 0.6 else 2 if v < 0.85 else 3
            img[y, x] = hexc(ramp[k])
    # Tufts (grass) or pebbles (path): little 2-pixel marks.
    for _ in range(size * size // 40):
        x, y = rng.integers(0, size), rng.integers(0, size)
        if kind == "grass":
            img[y, x] = hexc(ramp[3]); img[(y - 1) % size, x] = hexc("5a8a48")
        else:
            img[y, x] = hexc("8a7a62"); img[y, (x + 1) % size] = hexc("5a4a3a")
    return img


# Loot, the ritual circle, arrows and thrown things (2026-11-07), at the screen's 27 a unit like the rest.
def chest(open_=False):
    im = Img(12, 13)
    im.rect(1, 5, 10, 11, PAL["wood"][2])
    im.rect(8, 5, 10, 11, PAL["wood"][1])
    for x in (1, 5, 10):
        im.line(x, 5, x, 11, PAL["gold"][1])
    im.line(1, 8, 10, 8, PAL["gold"][1])
    if open_:
        im.rect(1, 1, 10, 3, PAL["wood"][1])
        im.rect(2, 4, 9, 5, PAL["gold"][3])
        im.set(4, 4, PAL["gold"][2]); im.set(7, 4, PAL["flower"][3])
    else:
        im.rect(1, 2, 10, 5, PAL["wood"][3])
        im.rect(8, 2, 10, 5, PAL["wood"][2])
        im.rect(5, 5, 6, 6, PAL["gold"][2])
    return im.outline()


def gold(n):
    sizes = {0: (6, 5, 1), 1: (8, 7, 3), 2: (11, 8, 6), 3: (15, 10, 10)}
    w, h, count = sizes[n]
    im = Img(w, h)
    rng = random.Random(n)
    for k in range(count):
        cx = w / 2 + rng.uniform(-w * 0.3, w * 0.3) if count > 1 else w / 2
        cy = h - 2.5 - rng.uniform(0, h * 0.35) * (k / max(1, count))
        im.blob(cx, cy, 2, 1.3, PAL["gold"])
    return im.outline()


def arrow(kind):
    im = Img(12, 5)
    im.line(1, 2, 9, 2, PAL["wood"][3])
    im.set(0, 1, PAL["red"][1]); im.set(0, 3, PAL["red"][1]); im.set(1, 1, PAL["red"][2]); im.set(1, 3, PAL["red"][2])
    head = {"plain": PAL["stone"][3], "pierce": PAL["red"][2], "fire": PAL["fire"][2], "frost": "8ad8ff",
            "lightning": "fff27a", "explosive": PAL["red"][1]}[kind]
    im.set(10, 1, head); im.set(10, 2, head); im.set(10, 3, head); im.set(11, 2, head)
    if kind == "fire":
        im.set(9, 1, PAL["fire"][1]); im.set(8, 0, PAL["fire"][2])
    if kind == "frost":
        im.set(9, 0, "dff6ff"); im.set(9, 4, "dff6ff")
    if kind == "lightning":
        im.set(8, 0, "fff27a"); im.set(7, 1, "fff27a")
    if kind == "explosive":
        im.blob(9.5, 2, 1.8, 1.8, PAL["red"])
    if kind == "pierce":
        im.line(2, 2, 9, 2, PAL["red"][0])
    return im.outline()


def thrown(kind):
    if kind == "arrow":
        return arrow("plain")
    if kind == "knife":
        im = Img(12, 4)
        im.line(0, 2, 3, 2, PAL["wood"][1]); im.line(4, 1, 10, 1, PAL["stone"][3]); im.line(4, 2, 11, 2, PAL["stone"][2])
        return im.outline()
    if kind == "axe":
        im = Img(12, 8)
        im.line(1, 6, 9, 2, PAL["wood"][2]); im.rect(7, 0, 10, 4, PAL["stone"][3]); im.set(10, 4, PAL["stone"][2])
        return im.outline()
    if kind == "blade":
        im = Img(10, 10)
        for a in range(4):
            ang = a * math.pi / 2
            im.line(5, 5, 5 + 4 * math.cos(ang), 5 + 4 * math.sin(ang), PAL["stone"][3])
            im.line(5, 5, 5 + 3 * math.cos(ang + 0.6), 5 + 3 * math.sin(ang + 0.6), PAL["stone"][2])
        im.set(5, 5, PAL["K"])
        return im.outline()
    if kind == "bomb":
        im = Img(9, 9)
        im.blob(4, 5, 3.4, 3.4, ["1a1a22", "2e2e3a", "4a4a5a", "8a8a9a"])
        im.line(5, 2, 7, 0, PAL["wood"][2]); im.set(7, 0, PAL["fire"][2])
        return im.outline()
    im = Img(15, 8)
    for k in range(9):
        im.blob(11 - k, 4, 3 - k * 0.3, 3 - k * 0.3, [PAL["fire"][0], PAL["fire"][1], PAL["fire"][2], PAL["fire"][3]])
    return im.outline()


def web_glob():
    """A spider's spit (2026-11-10, the owner: "The spiders need better looking ranged attacks"): a sticky white glob
    with strands trailing behind it, pointing along +x."""
    im = Img(13, 9)
    for y0, y1 in ((4, 1), (4, 7), (4, 4), (3, 2), (5, 6)):
        im.line(1, y1, 7, y0, PAL["web"][0])
    im.blob(9, 4.5, 3.4, 3.2, ["8a8a96", PAL["web"][0], PAL["web"][1], "ffffff"])
    im.set(10, 3, "ffffff"); im.set(8, 6, "8a8a96")
    return im.outline()


def bolt():
    """A coloured bolt of magic in greys (2026-11-10), tinted in the game by the shot's colour: a bright head, a tail
    that thins, pointing along +x."""
    im = Img(13, 7)
    for k in range(8):
        r = 0.6 + k * 0.3
        im.blob(2 + k, 3.5, r, r, ["7a7a7a", "a8a8a8", "d8d8d8", "ffffff"])
    im.blob(10, 3.5, 2.6, 2.6, ["a8a8a8", "d8d8d8", "f0f0f0", "ffffff"])
    return im.outline()


def flask(kind):
    """A thrown bottle that breaks into a pool (2026-11-10, the owner: "fire pools are molotovs, poison are poison
    bottles"), upright: a molotov is a long brown bottle with a burning rag in its neck, a poison bottle a round green
    flask with a cork and a skull mark."""
    if kind == "molotov":
        im = Img(7, 13)
        im.rect(1, 6, 5, 12, "5a3a1e"); im.rect(2, 6, 4, 11, "8a5a2a"); im.line(2, 7, 2, 11, "c8904a")
        im.rect(2, 3, 4, 5, "6a4a24"); im.set(3, 4, "a8743a")
        im.line(3, 2, 3, 3, PAL["canvas"][1]); im.set(4, 2, PAL["canvas"][2])
        im.set(3, 0, PAL["fire"][2]); im.set(2, 1, PAL["fire"][1]); im.set(3, 1, PAL["fire"][3]); im.set(4, 1, PAL["fire"][1])
        return im.outline()
    im = Img(9, 11)
    im.blob(4.5, 7, 3.8, 3.6, ["2a5a1a", "4a8a22", "7ac232", "c8f06a"])
    im.rect(3, 1, 5, 3, "3a6a2a"); im.set(4, 2, "6aa23a")
    im.rect(3, 0, 5, 0, PAL["wood"][2])
    im.set(4, 7, "f0f0e0"); im.set(3, 6, "f0f0e0"); im.set(5, 6, "f0f0e0")
    return im.outline()


EXTRAS = {"chest_closed": chest, "chest_open": lambda: chest(True), "gold_coin": lambda: gold(0), "gold_pile_1": lambda: gold(1),
          "gold_pile_2": lambda: gold(2), "gold_pile_3": lambda: gold(3), "projectile_web": web_glob, "projectile_bolt": bolt,
          "flask_molotov": lambda: flask("molotov"), "flask_poison": lambda: flask("poison")}
EXTRAS.update({f"arrow_{k}": (lambda k=k: arrow(k)) for k in ("plain", "pierce", "fire", "frost", "lightning", "explosive")})
EXTRAS.update({f"projectile_{k}": (lambda k=k: thrown(k)) for k in ("arrow", "knife", "axe", "blade", "bomb", "fireball")})


# Writing ---------------------------------------------------------------------------------------------------------
# "-- only name ..." writes just those pieces (2026-11-10: new pieces added without redrawing the rest), no ground,
# sizes list or preview.
import sys
ONLY = sys.argv[sys.argv.index("only") + 1:] if "only" in sys.argv else None
written = []
preview = []
for group, folder in ((TREES, "World"), (BUSHES, "World"), (STAIRS, "World"), (PROPS, "Dungeon"), (DECALS, "Dungeon"), (EXTRAS, "World")):
    for name, make in group.items():
        if ONLY is not None and name not in ONLY:
            continue
        im = make()
        arr = im.rgba()
        save(arr, os.path.join(ART, folder, name + ".png"))
        written.append(f"{folder}/{name} {arr.shape[1]} {arr.shape[0]}")
        preview.append(arr)
if ONLY is not None:
    print("wrote", written)
    raise SystemExit(0)
for kind, name in (("grass", "forest_grass"), ("path", "forest_path")):
    save(ground(kind), os.path.join(ART, "Ground", name + ".png"))
with open(os.path.join(HERE, "forest_art_sizes.txt"), "w") as f:
    f.write("\n".join(written) + "\n")

BG = np.array([0.16, 0.21, 0.13, 1], np.float32)
rows, row, width = [], [], 0
for arr in preview:
    a = np.where(arr[..., 3:4] > 0.5, arr, BG)
    if width + a.shape[1] > 520:
        rows.append(row); row, width = [], 0
    row.append(a); width += a.shape[1] + 4
rows.append(row)
strips = []
for r in rows:
    h = max(a.shape[0] for a in r)
    parts = []
    for a in r:
        parts.append(np.concatenate([np.tile(BG, (h - a.shape[0], a.shape[1], 1)), a], 0))
        parts.append(np.tile(BG, (h, 4, 1)))
    s = np.concatenate(parts, 1)
    strips.append(np.concatenate([s, np.tile(BG, (h, 530 - s.shape[1], 1))], 1))
    strips.append(np.tile(BG, (4, 530, 1)))
save(np.repeat(np.repeat(np.concatenate(strips, 0), 2, 0), 2, 1), os.path.join(P, "ArtSource", "pixel", "previews", "forest_art_16.png"))
print("wrote", len(written), "pieces")
