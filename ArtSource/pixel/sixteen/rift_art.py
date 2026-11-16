# The Rift Caves' art in the 16x16 style (2026-11-16, the owner: "start on the rift caves, ... you can build it"): the
# rock walls (plain, veined with violet crystal, split by a glowing fissure), the camera-side low rock, free-standing
# stalagmites, the old Watch's stair gate, the props the Rift leaves (rubble, spikes, a flesh pod, a violet fire, a
# stake), cracks and gore on the floor, and the rock floor with glowing cracks. Built on gaol_art.py's blocks (its
# helpers are imported; the palette is replaced for the caves): light from the top left, dark outlines, each drawn
# pixel 2 game pixels, 27 pixels a unit.
#
# Writes into Assets/_Project/Resources/Art (World, Dungeon, Ground), the list ArtSource/pixel/sixteen/rift_art_sizes.txt
# (as gaol_art_sizes.txt; Tools > ARPG > Configure Gaol Art reads all three) and previews (rift_art_16.png,
# rift_scene_16.png). The names are the old caves' (cave_wall_<n>, cave_wall_low_<n>, stalagmite_<n>,
# stairs_<down|up>_<x|y>_cave), so ThemePieces needs nothing new.
# Run: python3 ArtSource/pixel/sixteen/rift_art.py (plain Python, no Blender needed)
import os, sys, random, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gaol_art as g
from gaol_art import Img, write_png, hash01, block, cap_bottom, CW, CH, WALL_H, LOW_H, SCALE, K, ART, P, HERE

# The caves' palette over the gaol's: dark basalt with a violet cast, and the Rift's light.
g.PAL["stone"] = ["1c1824", "362e44", "4e445e", "6a5e80", "8a7ca0"]
g.PAL["top"] = ["15111c", "211b2a", "30283c"]
g.PAL["floor"] = ["14101a", "1e1926", "282232", "342c40"]
g.PAL["fire"] = ["5a2aa0", "a060f0", "d8a8ff", "f4e8ff"]
PAL = g.PAL
ROCK = ["17131f", "27202f", "3a3144", "4e4360", "6a5e7e"]
CRYSTAL = ["4a2290", "7a42d0", "a870f4", "d8b8ff", "f4ecff"]
FLESH = ["3a0e22", "6a1a3a", "8a3458", "b04c78", "d880a0"]
BONE = ["8a7c66", "c8bca4", "f0e8d8"]
WOOD = ["2a1a12", "4a2e1e", "6e4a2c", "92683c"]


def shade(col, face):
    r, gg, b = int(col[0:2], 16), int(col[2:4], 16), int(col[4:6], 16)
    k = 1.0 if face == 0 else 0.72
    return "%02x%02x%02x" % (int(r * k), int(gg * k), int(b * k))


def lattice(seed, x, y, period=None):
    """Value noise 0..1 at a point, smooth between lattice corners; tileable when a period (in cells) is given."""
    x0, y0 = math.floor(x), math.floor(y)
    fx, fy = x - x0, y - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)

    def corner(i, j):
        if period:
            i, j = i % period, j % period
        return hash01(seed, i, j)
    a = corner(x0, y0) * (1 - fx) + corner(x0 + 1, y0) * fx
    b = corner(x0, y0 + 1) * (1 - fx) + corner(x0 + 1, y0 + 1) * fx
    return a * (1 - fy) + b * fy


# The rock wall: strata across a face, blotches of tone, an edge lit on the left; modes 0 plain, 1 veined with violet
# crystal, 2 split by a glowing fissure, 3 plain with a ledge --------------------------------------------------------
def rock_feature(mode, seed, height):
    def f(face, u, v):
        if v == height - 1 or u == 0:
            return shade(ROCK[0], face)                       # the foot, and the seam between two cells
        n = lattice(seed + face * 17, u * 0.55, v * 0.32)
        m = lattice(seed + 91 + face * 5, u * 1.3, v * 0.9)
        tone = 1 if n < 0.36 else 3 if n > 0.74 else 2
        if m > 0.82:
            tone = min(4, tone + 1)
        elif m < 0.16:
            tone = max(1, tone - 1)
        # Strata: a dark line every few rows, broken now and then; a lit lip under it.
        band = (v + int(hash01(seed, face, 3) * 5)) % 7
        if band == 0 and hash01(seed, u, v // 7, face) > 0.28:
            tone = max(0, tone - 1)
        elif band == 1 and tone < 3:
            tone += 1 if hash01(seed, u, v, 8) > 0.5 else 0
        # The front corner is lit.
        if u == 6 and face == 0:
            tone = max(tone, 3)
        col = ROCK[min(tone, 4)]
        if mode == 1:
            # Crystal clusters: a few spikes growing out of the face.
            for k in range(3):
                cu = 1 + int(hash01(seed, k, 1) * 5)
                cv = 5 + int(hash01(seed, k, 2) * (height - 12))
                dx, dy = u - cu, v - cv
                if face == int(hash01(seed, k, 3) * 2) and dy <= 0 and -dy < 5 and abs(dx) <= (4 + dy) // 2:
                    col = CRYSTAL[3 if dx <= 0 else 2] if -dy < 3 else CRYSTAL[1 + (dx > 0)]
                    if dy == -4 and dx == 0:
                        col = CRYSTAL[4]
        if mode == 2:
            # A fissure running down the face, lit from inside.
            wob = int(2 * math.sin(v * 0.45 + seed))
            if face == (seed % 2) and abs(u - (3 + wob)) <= (1 if v % 5 else 0) and 6 < v < height - 3:
                col = CRYSTAL[3] if u == 3 + wob else CRYSTAL[2]
            elif face == (seed % 2) and abs(u - (3 + wob)) == 2 and 6 < v < height - 3 and v % 3 == 0:
                col = CRYSTAL[0]
        if mode == 3 and v in (14, 15) and u > 0:
            col = ROCK[4 if v == 14 else 1]
        return shade(col, face)
    return f


def rock_wall(seed, mode, height=WALL_H):
    return block(seed, height=height, feature=rock_feature(mode, seed, height))


WALLS = {
    "cave_wall_1": lambda: rock_wall(101, 0),
    "cave_wall_2": lambda: rock_wall(102, 0),
    "cave_wall_3": lambda: rock_wall(103, 1),
    "cave_wall_4": lambda: rock_wall(104, 1),
    "cave_wall_5": lambda: rock_wall(105, 2),
    "cave_wall_6": lambda: rock_wall(106, 3),
    "cave_wall_low_1": lambda: rock_wall(111, 0, LOW_H),
    "cave_wall_low_2": lambda: rock_wall(112, 0, LOW_H),
    "cave_wall_low_3": lambda: rock_wall(113, 0, LOW_H),
}


# Stalagmites: a free-standing clump of rock spikes on a cell, lit from the left, a violet glint at the tip of one --------
def spike(im, cx, base, w, h, ramp, glint=False):
    top = base - h
    for y in range(top, base + 1):
        half = (w / 2.0) * (y - top + 0.5) / (h + 0.5)
        for x in range(int(cx - half - 0.5), int(cx + half + 1.5)):
            d = (x - cx) / max(half, 0.6)
            if abs(x + 0.5 - cx) > half + 0.3:
                continue
            k = 3 if d < -0.45 else 2 if d < 0.1 else 1 if d < 0.6 else 0
            if y < top + 2 and d < 0.2:
                k = 4
            im.set(x, y, ramp[min(k, len(ramp) - 1)])
    if glint:
        im.set(cx, top, CRYSTAL[3]); im.set(cx, top + 1, CRYSTAL[2])


def stalagmite(seed):
    rng = random.Random(seed)
    im = Img(20, 30)
    spikes = [(10, 27, 9, 19), (5, 27, 6, 11), (15, 27, 6, 13)]
    if seed % 2:
        spikes = [(9, 27, 8, 22), (14, 27, 7, 12), (4, 27, 6, 9)]
    for cx, base, w, h in sorted(spikes, key=lambda s: -s[1]):
        spike(im, cx, base, w, h, ROCK, glint=(h > 15 and seed % 3 == 0))
    # Rubble at the foot.
    for _ in range(6):
        x, y = rng.randint(2, 17), rng.randint(26, 28)
        im.set(x, y, ROCK[rng.choice((1, 2, 3))])
    return im.outline()


SPIKES = {f"stalagmite_{n}": (lambda n=n: stalagmite(n + 40)) for n in (1, 2, 3)}


# The stair gate: gaol_art's arch, in the Watch's old dark stone, a violet fire either side -----------------------------
STAIRS = {"stairs_down_x_cave": lambda: g.doorway(True, False), "stairs_up_x_cave": lambda: g.doorway(False, False),
          "stairs_down_y_cave": lambda: g.doorway(True, True), "stairs_up_y_cave": lambda: g.doorway(False, True)}


# Props ---------------------------------------------------------------------------------------------------------------------
def rubble():
    im = Img(19, 10)
    rng = random.Random(5)
    for cx, cy, rx, ry in ((6, 6.5, 4.6, 2.8), (12.5, 6, 5.4, 3.2), (9.5, 7.8, 7, 2.0), (15, 8, 3, 1.6)):
        im.blob(cx, cy, rx, ry, ROCK)
    for _ in range(5):
        x, y = rng.randint(3, 15), rng.randint(3, 8)
        if im.get(x, y) is not None:
            im.set(x, y, ROCK[rng.choice((0, 4))])
    im.set(10, 4, CRYSTAL[2]); im.set(10, 3, CRYSTAL[3]); im.set(9, 4, CRYSTAL[1])
    return im.outline()


def floor_spikes():
    """Three bone-white spikes out of a cracked patch of floor, a violet glow at their foot."""
    im = Img(17, 18)
    im.blob(8.5, 15, 7.4, 2.6, [ROCK[0], ROCK[1], ROCK[1], CRYSTAL[1]])
    for x, y in ((4, 15), (8, 16), (12, 15), (6, 14), (11, 14)):
        im.set(x, y, CRYSTAL[2])
    for cx, base, w, h in ((5, 15, 4, 10), (12, 15, 4, 12), (8.5, 16, 5, 15)):
        spike(im, int(cx), base, w, h, BONE if False else ["5e5472", "8e84a4", "bcb2cc", "e4dcf0"])
    return im.outline()


def flesh_pod():
    """An egg of wine-red flesh on a bed of tendrils, veined, a violet light turning inside it."""
    im = Img(15, 18)
    for x, y in ((2, 16), (3, 15), (4, 16), (12, 16), (11, 15), (10, 16), (1, 14), (13, 14)):
        im.set(x, y, FLESH[1])
    im.blob(7.5, 10.5, 5.6, 6.6, FLESH, light=(-0.6, -0.7))
    im.blob(7.5, 10.5, 2.4, 3.4, [CRYSTAL[0], CRYSTAL[1], CRYSTAL[2], CRYSTAL[3]], light=(0.0, 0.2))
    for x, y in ((4, 7), (5, 6), (10, 7), (11, 8), (5, 14), (9, 15)):
        im.set(x, y, FLESH[0])
    im.set(6, 8, FLESH[4]); im.set(5, 9, FLESH[4])
    return im.outline()


def rift_fire():
    """A violet fire in a ring of cracked stone."""
    im = Img(17, 22)
    f = CRYSTAL
    for x, y, c in ((2, 17, 2), (4, 18, 3), (7, 19, 2), (10, 19, 3), (13, 18, 2), (15, 17, 3), (3, 16, 1), (14, 16, 1)):
        im.rect(x - 1, y - 1, x + 1, y + 1, ROCK[c])
        im.set(x - 1, y - 1, ROCK[4])
    im.blob(8.5, 17.5, 6.2, 2.0, [ROCK[0], ROCK[1], ROCK[1], f[0]])
    # The flame: wide at its skirts, tongues above.
    for y in range(3, 18):
        t = (17 - y) / 14.0
        half = 5.2 * (1 - t) ** 0.7
        wob = math.sin(y * 1.7) * 0.7 * t
        for x in range(int(8.5 - half + wob), int(8.5 + half + wob) + 1):
            d = abs(x + 0.5 - 8.5 - wob) / max(half, 0.5)
            k = 4 if d < 0.25 and t < 0.45 else 3 if d < 0.55 and t < 0.7 else 2 if d < 0.8 else 1
            im.set(x, y, f[k])
    im.set(7, 2, f[2]); im.set(9, 1, f[1]); im.set(10, 3, f[0])
    return im.outline()


def stake():
    """A stake of old black wood with a skull on its point."""
    im = Img(11, 24)
    im.rect(5, 8, 6, 21, WOOD[1]); im.rect(5, 8, 5, 21, WOOD[2])
    im.set(5, 22, WOOD[0]); im.set(6, 22, WOOD[0])
    im.rect(3, 22, 8, 22, ROCK[1])
    b = BONE
    im.blob(5.5, 5, 3.4, 3.2, b)
    im.rect(3, 7, 7, 8, b[0]); im.rect(4, 8, 7, 8, b[1])
    im.set(4, 5, K); im.set(7, 5, K); im.set(5, 7, K); im.set(6, 7, K)
    im.set(5, 6, CRYSTAL[2]) if False else None
    return im.outline()


PROPS = {"prop_rubble": rubble, "prop_spikes": floor_spikes, "prop_flesh_pod": flesh_pod, "prop_hellfire": rift_fire,
         "prop_stake": stake}


def glow_cracks(seed):
    rng = random.Random(seed)
    im = Img(27, 14)

    def inside(x, y):
        return abs(x + 0.5 - 13.5) / 13.5 + abs(y + 0.5 - 7) / 7 < 0.85
    # A few forking cracks, dark with a violet heart.
    for _ in range(3):
        x, y = rng.randint(4, 22), rng.randint(3, 10)
        dx, dy = rng.choice((-1, 1)), rng.choice((-1, 0, 1))
        for _ in range(rng.randint(5, 10)):
            if inside(x, y):
                im.set(x, y, ROCK[0])
                if rng.random() < 0.45:
                    im.set(x, y + 1, CRYSTAL[1 + (rng.random() < 0.3)])
            x += dx
            if rng.random() < 0.45:
                y += dy if rng.random() < 0.6 else rng.choice((-1, 1))
    return im


def gore(seed):
    rng = random.Random(seed)
    im = Img(27, 14)

    def inside(x, y):
        return abs(x + 0.5 - 13.5) / 13.5 + abs(y + 0.5 - 7) / 7 < 0.8
    for _ in range(4):
        cx, cy = rng.randint(7, 19), rng.randint(4, 9)
        for _ in range(14):
            x, y = cx + rng.randint(-4, 4), cy + rng.randint(-2, 2)
            if inside(x, y):
                im.set(x, y, FLESH[rng.choice((0, 1, 1, 2))])
    for _ in range(3):
        x, y = rng.randint(6, 20), rng.randint(4, 9)
        if inside(x, y):
            im.set(x, y, FLESH[3]); im.set(x + 1, y, FLESH[4] if rng.random() < 0.4 else FLESH[2])
    return im


DECALS = {"decal_cracks_1": lambda: glow_cracks(1), "decal_cracks_2": lambda: glow_cracks(2), "decal_gore_1": lambda: gore(1),
          "decal_gore_2": lambda: gore(2), "decal_gore_3": lambda: gore(3)}


# The floor: rock, in blotches of tone and ridges, seamless at 108 as the gaol's; here and there a crack with a violet heart -
def floor(size=108):
    f = PAL["floor"]
    rows = []
    period = 8
    for y in range(size):
        row = []
        for x in range(size):
            # Blotches in two scales, stretched across the screen as the ground lies (twice as wide as tall).
            n1 = lattice(7, x / 13.5, y / 6.75, period)
            n2 = lattice(13, x / 5.0, y / 2.5, size // 5 + 1) if False else lattice(13, x / 4.5, y / 2.25, 24)
            v = n1 * 0.7 + n2 * 0.3
            k = 0 if v < 0.28 else 1 if v < 0.5 else 2 if v < 0.74 else 3
            speck = hash01(x, y, 5)
            if speck < 0.04:
                k = max(0, k - 1)
            elif speck > 0.985:
                k = min(3, k + 1)
            # A pocket of crystal dust in the rock, rarely.
            dust = lattice(53, x / 3.0, y / 1.5, 36)
            col = f[k]
            # A ridge: a lighter line along one diagonal in a few places.
            ridge = lattice(29, (x + 2 * y) / 22.0, (x - 2 * y) / 22.0, 5)
            if abs(ridge - 0.5) < 0.012 and n1 > 0.4:
                col = f[3]
            # A crack: a dark line with a violet heart now and then.
            crack = lattice(41, (x + 2 * y) / 17.0, (x - 2 * y) / 9.0, 6)
            if abs(crack - 0.5) < 0.014:
                col = f[0]
                if n1 > 0.5 and hash01(x // 3, y // 3, 9) > 0.7:
                    col = CRYSTAL[0] if hash01(x, y, 4) > 0.5 else f[1]
            row.append((int(col[0:2], 16), int(col[2:4], 16), int(col[4:6], 16), 255))
        rows.append(row)
    return rows


def main():
    written, preview = [], []
    for group, folder in ((WALLS, "World"), (SPIKES, "World"), (STAIRS, "World"), (PROPS, "Dungeon"), (DECALS, "Dungeon")):
        for name, make in group.items():
            im = make()
            write_png(os.path.join(ART, folder, name + ".png"), im.rows())
            px, py = g.pivot_of(name, im)
            written.append(f"{folder}/{name} {im.w * SCALE} {im.h * SCALE} {px:g} {py:g}")
            preview.append(im)
    write_png(os.path.join(ART, "Ground", "rift_floor.png"), floor())
    with open(os.path.join(HERE, "rift_art_sizes.txt"), "w") as fh:
        fh.write("\n".join(written) + "\n")

    # A corner of a cave to look at: the floor, two walls, stalagmites, props.
    w, h = 150, 125
    sc = Img(w, h)
    fl = floor()
    for y in range(h):
        for x in range(w):
            r, gg, b, _ = fl[y % 108][x % 108]
            sc.set(x, y, f"{r:02x}{gg:02x}{b:02x}")

    def at(cx, cy):
        return round(75 + (cx - cy) * 6.75), round(112 - (cx + cy) * 3.375)
    order = ("cave_wall_1", "cave_wall_3", "cave_wall_2", "cave_wall_5", "cave_wall_1", "cave_wall_4", "cave_wall_6", "cave_wall_2",
             "cave_wall_3")
    cells = []
    for k in range(0, 9):
        cells.append((k, 9, "x"))
        cells.append((9, k, "y"))
    cells.append((9, 9, "c"))
    for cx, cy, kind in sorted(cells, key=lambda c: -(c[0] + c[1])):
        ox, oy = at(cx, cy)
        name = order[(cx if kind == "x" else cy) % len(order)] if kind != "c" else "cave_wall_1"
        sc.paste(WALLS[name](), ox - 7, oy - WALL_H - 3)
    for name, cx, cy in (("stalagmite_1", 3, 6), ("stalagmite_2", 6, 3), ("stalagmite_3", 8, 7)):
        ox, oy = at(cx, cy)
        p = SPIKES[name]()
        sc.paste(p, ox - p.w // 2, oy - p.h + 3)
    for name, cx, cy in (("prop_hellfire", 6, 6), ("prop_flesh_pod", 4, 4), ("prop_spikes", 2, 7), ("prop_rubble", 7, 2),
                         ("prop_stake", 8, 4), ("prop_torch", 1, 3)):
        ox, oy = at(cx, cy)
        mk = PROPS.get(name) or g.PROPS.get(name)
        p = mk()
        sc.paste(p, ox - p.w // 2, oy - p.h + 3)
    write_png(os.path.join(P, "ArtSource", "pixel", "previews", "rift_scene_16.png"), sc.rows(6))
    rows, row, width = [], [], 0
    for im in preview:
        if width + im.w > 250:
            rows.append(row); row, width = [], 0
        row.append(im); width += im.w + 3
    rows.append(row)
    sheet_h = sum(max(i.h for i in r) + 3 for r in rows)
    sheet = Img(256, sheet_h)
    for yy in range(sheet_h):
        for xx in range(256):
            sheet.set(xx, yy, "2a2c38")
    y = 0
    for r in rows:
        hh = max(i.h for i in r)
        x = 2
        for im in r:
            sheet.paste(im, x, y + hh - im.h)
            x += im.w + 3
        y += hh + 3
    write_png(os.path.join(P, "ArtSource", "pixel", "previews", "rift_art_16.png"), sheet.rows(4))
    print("wrote", len(written), "pieces and the floor")


if __name__ == "__main__":
    main()
