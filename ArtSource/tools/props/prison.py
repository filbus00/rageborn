# The prison (2026-10-12, the owner's "Prison dungeon concept art": cell blocks, dungeon props, atmosphere), depths 13
# to 24. Built like the crypt (themes.py) in darker, rougher stone, its faces toward the rooms lined with barred cells,
# shackles and cell doors; cages of iron bars stand in its halls. Faces toward the camera face -x and -y.
#
#   prison_wall_1..5        1 plain rough stone, 2 a barred cell (a dark recess behind bars), 3 shackles and chains,
#                           4 a cell door (iron-banded planks, a barred window), 5 bloodied stone with a chain from a ring
#   prison_wall_low_1..2    the same stone cut low, 2 with a rusted chain along its top
#   prison_diag_<o>[_low]   a cut corner in the prison's stone
#   prison_bars_<sides>     a cage's bars in a cell: a run to each side named (w -x, e +x, s -y, n +y), a post in the
#                           middle where a run ends or turns; "post" for a lone post
#
# Props (dungeon pixel art, Import Pixel Art): prop_hanging_cage (a gibbet: a post and arm, a cage of bars hanging from
# it, bones inside), prop_torture_bench (a plank table with straps and blood, a stool), prop_stocks (a pillory),
# prop_floor_grate (an iron grate over a dark drain), prop_straw_pile (a prisoner's straw with a bowl).
import math
from kit import *
import themes
from themes import HALF, WALL_H, LOW_H, DIAG, _rng, _ashlar_line, _crypt_trim, _crypt_top, _Frame

WORLD = themes.WORLD


def _stones():
    return [mat(STONE[2], 0.35, STONE[0], scale=12), mat(STONE[1], 0.35, STONE[0], scale=12),
            mat(EARTH[2], 0.35, STONE[0], scale=12), mat(STONE[2], 0.35, MOSS[0], scale=12)]


def _iron():
    return mat(STONE[1], 0.3, WOOD[0], metal=0.55, rough=0.6, scale=14)


def _faces(rng, h, low):
    """The two camera faces in rough courses, with their foot and cornice."""
    stones = _stones()
    top = h - (0.03 if low else 0.12)
    _ashlar_line((-HALF, HALF), (-HALF, -HALF), (-1, 0), top, rng, stones, z0=0.1, course=0.15, proud=0.022)
    _ashlar_line((-HALF, -HALF), (HALF, -HALF), (0, -1), top, rng, stones, z0=0.1, course=0.15, proud=0.022)
    _crypt_trim((-HALF, HALF), (-HALF, -HALF), (-1, 0), h, low)
    _crypt_trim((-HALF, -HALF), (HALF, -HALF), (0, -1), h, low)
    _crypt_top([(-HALF, -HALF), (HALF, -HALF), (HALF, HALF), (-HALF, HALF)], h)


def _cell(f, rng):
    """A barred cell in a face: a dark recess with a stone frame, bars in front, a glimpse of straw and bone."""
    dark = mat(STONE[0], 0.0)
    frame = mat(STONE[4], 0.3, STONE[2], scale=10)
    # Bars lighter than the stone's iron, so they read against the dark behind them.
    iron = mat(STONE[3], 0.3, WOOD[1], metal=0.55, rough=0.5, scale=14)
    w, top = 0.27, 1.45
    f.box(-0.03, 0.0, -w, w, 0.1, top, dark)
    f.box(-0.05, -0.0, -w - 0.06, -w, 0.1, top + 0.06, frame, bevel=0.006)
    f.box(-0.05, -0.0, w, w + 0.06, 0.1, top + 0.06, frame, bevel=0.006)
    f.box(-0.06, 0.0, -w - 0.08, w + 0.08, top, top + 0.1, frame, bevel=0.006)
    straw = mat(EARTH[4], 0.4, EARTH[2], scale=20)
    f.box(-0.035, -0.02, -w + 0.02, w - 0.02, 0.1, 0.16, straw)
    if rng.random() < 0.5:
        bone = mat(BONE[0], 0.3, BONE[0])
        x, y, _ = f.at(-0.04, rng.uniform(-0.15, 0.15))
        sphere(0.04, (x, y, 0.2), bone, scale=(1, 1, 0.9), segments=8)
    n = 7
    for k in range(n):
        v = -w + 0.02 + (2 * w - 0.04) * k / (n - 1)
        f.box(-0.07, -0.05, v - 0.011, v + 0.011, 0.1, top, iron)
    for z in (0.35, 0.95):
        f.box(-0.075, -0.05, -w, w, z, z + 0.035, iron)


def _shackles(f, rng):
    iron = _iron()
    rust = mat(WOOD[1], 0.4, BLOOD[0], scale=8)
    for v in (-0.17, 0.17):
        x, y, _ = f.at(-0.03, v)
        f.box(-0.035, 0.0, v - 0.03, v + 0.03, 1.32, 1.38, iron)
        for k in range(6):
            z = 1.3 - k * 0.07
            f.box(-0.05, -0.03, v - 0.012 + (0.01 if k % 2 else 0), v + 0.012 + (0.01 if k % 2 else 0), z - 0.05, z, iron)
        f.box(-0.07, -0.025, v - 0.045, v + 0.045, 0.82, 0.88, iron, bevel=0.005)
        f.box(-0.03, -0.0, v - 0.04, v + 0.06, 0.5, 0.82, rust)


def _door(f, rng):
    wood = mat(WOOD[1], 0.35, WOOD[0], scale=14)
    iron = _iron()
    frame = mat(STONE[3], 0.3, STONE[1], scale=10)
    w, top = 0.24, 1.4
    f.box(-0.04, 0.0, -w - 0.07, w + 0.07, 0.1, top + 0.09, frame, bevel=0.006)
    f.box(-0.06, -0.02, -w, w, 0.1, top, wood)
    for k in range(4):
        v = -w + 2 * w * (k + 0.5) / 4
        f.box(-0.065, -0.055, v - 0.004, v + 0.004, 0.1, top, mat(WOOD[0], 0.0))
    for z in (0.3, 0.75, 1.2):
        f.box(-0.075, -0.055, -w, w, z, z + 0.05, iron)
    dark = mat(STONE[0], 0.0)
    f.box(-0.07, -0.06, -0.09, 0.09, 1.0, 1.18, dark)
    for k in range(4):
        v = -0.075 + 0.05 * k
        f.box(-0.085, -0.07, v - 0.008, v + 0.008, 1.0, 1.18, iron)
    f.box(-0.09, -0.06, 0.15, 0.2, 0.62, 0.72, iron)


def _blood(f, rng):
    blood = mat(BLOOD[1], 0.3, BLOOD[0], scale=10)
    iron = _iron()
    for k in range(5):
        v = rng.uniform(-0.25, 0.25)
        z = rng.uniform(0.2, 0.9)
        f.box(-0.026, -0.0, v - 0.03, v + 0.03, z, z + rng.uniform(0.15, 0.5), blood)
    f.box(-0.04, 0.0, -0.05, 0.05, 1.6, 1.66, iron)
    for k in range(12):
        z = 1.6 - k * 0.07
        f.box(-0.055, -0.035, -0.012 + (0.01 if k % 2 else 0), 0.012 + (0.01 if k % 2 else 0), z - 0.05, z, iron)


def prison_wall(variant, low=False):
    rng = _rng("prison", variant, low)
    h = LOW_H if low else WALL_H
    box((CELL - 0.01, CELL - 0.01, h - 0.02), (0, 0, 0), mat(STONE[0], 0.0))
    _faces(rng, h, low)
    if low:
        if variant == 2:
            iron = _iron()
            for axis in "xy":
                f = _Frame(axis)
                for k in range(9):
                    v = -HALF + 0.04 + k * 0.075
                    f.box(-0.03, 0.0, v, v + 0.05, h - 0.01, h + 0.025, iron)
        return
    add = {2: _cell, 3: _shackles, 4: _door, 5: _blood}.get(variant)
    if add is not None:
        for axis in "xy":
            add(_Frame(axis), rng)
    # Damp and grime down the stone.
    grime = mat(STONE[0], 0.4, MOSS[0], scale=6)
    for axis in "xy":
        f = _Frame(axis)
        for k in range(2):
            v = rng.uniform(-HALF + 0.05, HALF - 0.05)
            f.box(-0.03, -0.0, v - 0.05, v + 0.05, h * rng.uniform(0.6, 0.8), h - 0.15, grime)


def prison_diag(orient, low=False):
    rng = _rng("pdiag", orient, low)
    h = LOW_H if low else WALL_H
    tri, (a, b), normal = DIAG[orient]
    prism(tri, h - 0.02, (0, 0, 0), mat(STONE[0], 0.0))
    stones = _stones()
    _ashlar_line(a, b, normal, h - (0.03 if low else 0.12), rng, stones, z0=0.1, course=0.15, proud=0.022)
    _crypt_trim(a, b, normal, h, low)
    for (p, q, n) in (((-HALF, HALF), (-HALF, -HALF), (-1, 0)), ((-HALF, -HALF), (HALF, -HALF), (0, -1))):
        mid = ((p[0] + q[0]) / 2, (p[1] + q[1]) / 2)
        if mid[0] * normal[0] + mid[1] * normal[1] < -0.01:
            _ashlar_line(p, q, n, h - (0.03 if low else 0.12), rng, stones, z0=0.1, course=0.15, proud=0.022)
            _crypt_trim(p, q, n, h, low)
    _crypt_top(tri, h)


BARS_H = 1.55
SIDES = {"w": (-1, 0), "e": (1, 0), "s": (0, -1), "n": (0, 1)}


def prison_bars(sides):
    """A cage's bars in one cell: from the cell's middle out to each side named, a run of bars between a foot rail and
    a top rail; a post in the middle unless the cell is a straight run."""
    iron = _iron()
    straight = sides in ("we", "sn")
    for s in sides:
        dx, dy = SIDES[s]
        # Half a cell from the middle to the edge (a straight run does both halves).
        def at(t):
            return (dx * t, dy * t)
        x, y = at(HALF / 2)
        size = (HALF, 0.05, 0.05) if dx else (0.05, HALF, 0.05)
        box(size, (x, y, 0.0), iron)
        box(size, (x, y, BARS_H - 0.05), iron)
        box((size[0], size[1], 0.035), (x, y, BARS_H * 0.55), iron)
        for k in range(4):
            t = HALF * (k + 0.5) / 4
            bx, by = at(t)
            box((0.022, 0.022, BARS_H), (bx, by, 0.0), iron)
            # Spikes over the top rail.
            cone(0.018, 0.07, (bx, by, BARS_H), iron, 4)
    if not straight:
        box((0.08, 0.08, BARS_H + 0.08), (0, 0, 0), iron, bevel=0.006)
        box((0.11, 0.11, 0.04), (0, 0, BARS_H + 0.08), iron, bevel=0.006)


# ---------------------------------------------------------------- props (sized in metres, as models.py)

M = METRE
PROP = (160, 260, 80, 40)
TALL = (160, 400, 80, 40)


def hanging_cage():
    """A gibbet: a post with an arm, a cage of bars hung from it on a chain, a skeleton slumped inside."""
    wood = mat(WOOD[1], 0.35, WOOD[0], scale=12)
    iron = _iron()
    bone = mat(BONE[0], 0.3, BONE[0])
    px, py, top = 0.2 * M, 0.2 * M, 1.45 * M
    cx, cy = -0.12 * M, 0.2 * M
    box((0.09 * M, 0.09 * M, top), (px, py, 0), wood)
    box((0.42 * M, 0.08 * M, 0.08 * M), ((px + cx) / 2 - 0.03 * M, py, top - 0.08 * M), wood)
    box((0.22 * M, 0.06 * M, 0.06 * M), (px - 0.08 * M, py, top - 0.28 * M), wood, rot=(0, -45, 0))
    # The cage hangs from the arm's end on a short chain, its foot clear of the ground.
    z0, hgt, r = 0.2 * M, 0.85 * M, 0.2 * M
    for k in range(5):
        box((0.02 * M, 0.02 * M, 0.06 * M), (cx, cy, top - 0.14 * M - 0.07 * M * k), iron)
    for k in range(10):
        a = 2 * math.pi * k / 10
        box((0.018 * M, 0.018 * M, hgt), (cx + r * math.cos(a), cy + r * math.sin(a), z0), iron)
    for z in (z0, z0 + hgt * 0.5, z0 + hgt):
        lathe([(r + 0.015 * M, 0), (r + 0.015 * M, 0.03 * M), (r - 0.015 * M, 0.03 * M), (r - 0.015 * M, 0)], (cx, cy, z), iron, 14, smooth=False)
    lathe([(r, 0), (0.0, 0.12 * M)], (cx, cy, z0 + hgt), iron, 10, smooth=False)
    sphere(0.055 * M, (cx + 0.04 * M, cy, z0 + 0.32 * M), bone, scale=(1, 1, 0.9), segments=8)
    box((0.1 * M, 0.07 * M, 0.18 * M), (cx, cy, z0 + 0.04 * M), bone)
    for k in range(3):
        box((0.018 * M, 0.018 * M, 0.2 * M), (cx - 0.05 * M + 0.05 * M * k, cy - 0.06 * M, z0 + 0.02 * M), bone, rot=(70, 0, 20 * k))


def torture_bench():
    """A heavy plank table with leather straps and dried blood, a stool beside it."""
    M = METRE * 0.72
    wood = mat(WOOD[2], 0.35, WOOD[0], scale=12)
    dark = mat(WOOD[1], 0.35, WOOD[0], scale=12)
    leather = mat(WOOD[0], 0.3, STONE[0])
    blood = mat(BLOOD[1], 0.35, BLOOD[0], scale=8)
    iron = _iron()
    for x in (-0.36, 0.36):
        for y in (-0.16, 0.16):
            box((0.07 * M, 0.07 * M, 0.5 * M), (x * M, y * M, 0), dark)
    box((0.9 * M, 0.45 * M, 0.07 * M), (0, 0, 0.5 * M), wood, bevel=0.005)
    for x in (-0.25, 0.05, 0.3):
        box((0.06 * M, 0.47 * M, 0.02 * M), (x * M, 0, 0.57 * M), leather)
        box((0.07 * M, 0.03 * M, 0.04 * M), (x * M, -0.24 * M, 0.55 * M), iron)
    sphere(0.16 * M, (0.05 * M, 0.02 * M, 0.575 * M), blood, scale=(1.6, 1, 0.06), segments=10)
    sphere(0.1 * M, (0.2 * M, -0.3 * M, 0.0), blood, scale=(1.4, 1, 0.05), segments=10)
    cyl(0.12 * M, 0.04 * M, (-0.25 * M, -0.45 * M, 0.36 * M), dark, 10)
    for k in range(3):
        a = 2 * math.pi * k / 3
        box((0.035 * M, 0.035 * M, 0.36 * M), (-0.25 * M + 0.08 * M * math.cos(a), -0.45 * M + 0.08 * M * math.sin(a), 0), dark)
    box((0.22 * M, 0.04 * M, 0.02 * M), (0.25 * M, 0.1 * M, 0.58 * M), iron, rot=(0, 0, 30))


def stocks():
    """A pillory: two posts, a hinged board with three holes, a lock."""
    M = METRE * 0.68
    wood = mat(WOOD[2], 0.35, WOOD[0], scale=12)
    dark = mat(WOOD[1], 0.35, WOOD[0], scale=12)
    iron = _iron()
    hole = mat(STONE[0], 0.0)
    for x in (-0.38, 0.38):
        box((0.09 * M, 0.09 * M, 1.1 * M), (x * M, 0, 0), dark)
    box((0.95 * M, 0.08 * M, 0.3 * M), (0, 0, 0.7 * M), wood, bevel=0.005)
    box((0.95 * M, 0.09 * M, 0.02 * M), (0, 0, 0.85 * M), dark)
    for x, r in ((-0.22, 0.05), (0.0, 0.08), (0.22, 0.05)):
        cyl(r * M, 0.1 * M, (x * M, -0.0, 0.85 * M), hole, 10, rot=(90, 0, 0))
    box((0.06 * M, 0.1 * M, 0.08 * M), (0.42 * M, 0, 0.8 * M), iron)
    box((1.0 * M, 0.3 * M, 0.06 * M), (0, 0, 0), dark)


def floor_grate():
    """An iron grate over a dark drain, set in a stone kerb."""
    stone = mat(STONE[2], 0.3, STONE[0], scale=10)
    dark = mat(STONE[0], 0.0)
    iron = _iron()
    box((0.62 * M, 0.62 * M, 0.02 * M), (0, 0, 0), stone, bevel=0.004)
    box((0.5 * M, 0.5 * M, 0.024 * M), (0, 0, 0), dark)
    for k in range(6):
        t = (-0.21 + 0.084 * k) * M
        box((0.03 * M, 0.5 * M, 0.035 * M), (t, 0, 0.0), iron)
        box((0.5 * M, 0.03 * M, 0.035 * M), (0, t, 0.0), iron)
    box((0.5 * M, 0.04 * M, 0.04 * M), (0, -0.27 * M, 0.0), iron)


def straw_pile():
    """A prisoner's bed of straw, a wooden bowl, a bone."""
    rng = _rng("straw")
    straw = [mat(EARTH[4], 0.4, EARTH[2], scale=20), mat(EARTH[5], 0.4, EARTH[3], scale=20), mat(WOOD[3], 0.4, EARTH[2], scale=20)]
    for k in range(90):
        x, y = rng.uniform(-0.35, 0.35) * M, rng.uniform(-0.25, 0.25) * M
        if (x / (0.35 * M)) ** 2 + (y / (0.25 * M)) ** 2 > 1:
            continue
        box((0.18 * M, 0.01 * M, 0.01 * M), (x, y, rng.uniform(0, 0.06) * M), rng.choice(straw), rot=(rng.uniform(-10, 10), 0, rng.uniform(0, 180)))
    lathe([(0.04 * M, 0), (0.08 * M, 0.04 * M), (0.075 * M, 0.045 * M), (0.0, 0.01 * M)], (0.3 * M, -0.25 * M, 0), mat(WOOD[2], 0.3, WOOD[1]), 10, cap=False)
    box((0.2 * M, 0.025 * M, 0.025 * M), (-0.25 * M, -0.28 * M, 0.0), mat(BONE[1], 0.3, BONE[0]), rot=(0, 0, 25))


def register(all_pieces):
    for n in range(1, 6):
        all_pieces["prison_wall_%d" % n] = ("world", (lambda n=n: prison_wall(n)), WORLD)
    for n in range(1, 3):
        all_pieces["prison_wall_low_%d" % n] = ("world", (lambda n=n: prison_wall(n, True)), WORLD)
    for o in "bflr":
        all_pieces["prison_diag_" + o] = ("world", (lambda o=o: prison_diag(o)), WORLD)
        all_pieces["prison_diag_%s_low" % o] = ("world", (lambda o=o: prison_diag(o, True)), WORLD)
    for mask in range(16):
        sides = "".join(s for i, s in enumerate("wesn") if mask >> i & 1)
        all_pieces["prison_bars_" + (sides or "post")] = ("world", (lambda s=sides: prison_bars(s)), WORLD)
    all_pieces["prop_hanging_cage"] = ("dungeon", hanging_cage, TALL)
    all_pieces["prop_torture_bench"] = ("dungeon", torture_bench, PROP)
    all_pieces["prop_stocks"] = ("dungeon", stocks, PROP)
    all_pieces["prop_floor_grate"] = ("dungeon", floor_grate, PROP)
    all_pieces["prop_straw_pile"] = ("dungeon", straw_pile, PROP)
