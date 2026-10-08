# The props, one function each, sized in metres through M (kit.METRE). ALL maps a file name to (kind, builder, frame).
# Kind "dungeon" renders a fixed frame (width, height, origin x, origin y in render pixels) for the pixel art importer:
# 160 wide (one cell at 4x), the cell's middle 40 px up. Kind "world" renders a large frame and crops it; the origin is
# the middle of the piece's footprint, which is where the game stands it.
# Faces toward the camera are those facing -x (the left front on screen) and -y (the right front).
import math, random
import bpy
from kit import *

M = METRE
PROP = (160, 260, 80, 40)
TALL = (160, 400, 80, 40)
DECAL = (160, 80, 80, 40)
WORLD = (1600, 1600, 800, 500)


def seed(n):
    random.seed(n)
    return random


# ---------------------------------------------------------------- pieces

def plank_box(sx, sy, sz, loc, wood, dark, rot=(0, 0, 0)):
    box((sx, sy, sz), loc, wood, rot)


def flame(loc, size, strength=6.0):
    core = mat(EMBER[4], 0.0, emit=strength)
    outer = mat(EMBER[2], 0.0, emit=strength * 0.7)
    x, y, z = loc
    lathe([(size * 0.5, 0), (size * 0.62, size * 0.5), (size * 0.35, size * 1.2), (0.001, size * 2.0)], (x, y, z), outer, 8, smooth=True)
    lathe([(size * 0.3, 0), (size * 0.36, size * 0.4), (0.001, size * 1.3)], (x - size * 0.25, y - size * 0.25, z + size * 0.05), core, 8)


def bone(loc, length, angle, material, thick=0.025, tilt=0.0):
    x, y, z = loc
    a = math.radians(angle)
    dx, dy = math.cos(a) * length / 2, math.sin(a) * length / 2
    cyl(thick * M, length, (x, y, z + thick * M), material, 6, rot=(0, 90 - tilt, angle), smooth=True)
    # The shaft runs from loc along the angle: knobs at both ends.
    ex, ey = x + math.cos(a) * length, y + math.sin(a) * length
    for px, py in ((x, y), (ex, ey)):
        sphere(thick * 1.7 * M, (px, py, z + thick * M), material, segments=8)


def skull(loc, size, material, turn=0.0):
    x, y, z = loc
    sphere(size, (x, y, z + size * 0.9), material, scale=(1.0, 1.1, 0.95), segments=12)
    sphere(size * 0.7, (x - size * 0.35, y - size * 0.35, z + size * 0.55), material, scale=(1, 1, 0.8), segments=10)
    dark = mat(STONE[0], 0.0)
    a = math.radians(turn - 135)
    fx, fy = math.cos(a), math.sin(a)
    sx, sy = -fy, fx
    for side in (-1, 1):
        sphere(size * 0.22, (x + fx * size * 0.85 + sx * size * 0.35 * side, y + fy * size * 0.85 + sy * size * 0.35 * side, z + size * 0.95), dark, segments=8)
    sphere(size * 0.12, (x + fx * size * 1.0, y + fy * size * 1.0, z + size * 0.62), dark, segments=6)


def rock(loc, size, material, rng, squash=0.7):
    mesh = sphere(size, loc, material, scale=(1 + rng.random() * 0.4, 1 + rng.random() * 0.4, squash), segments=6, smooth=False)
    mesh.rotation_euler = (rng.random() * 0.4, rng.random() * 0.4, rng.random() * 6.28)
    return mesh


# ---------------------------------------------------------------- dungeon props

def barrel(at=(0, 0, 0), scale=1.0):
    wood = mat(WOOD[3], 0.3, WOOD[2], scale=14)
    band = mat(STONE[1], 0.0, metal=0.5, rough=0.5)
    r = 0.24 * M * scale
    h = 0.72 * M * scale
    lathe([(r * 0.86, 0), (r, h * 0.3), (r * 1.04, h * 0.5), (r, h * 0.7), (r * 0.86, h)], at, wood, 12, smooth=False)
    for z in (0.1, 0.9):
        rr = r * 0.92
        lathe([(rr, h * z - 0.03 * M), (rr, h * z + 0.03 * M)], at, band, 12, smooth=False)
    for z in (0.32, 0.68):
        rr = r * 1.03
        lathe([(rr, h * z - 0.03 * M), (rr, h * z + 0.03 * M)], at, band, 12, smooth=False)
    cyl(r * 0.8, 0.01, (at[0], at[1], at[2] + h - 0.005), mat(WOOD[2], 0.3, WOOD[1], scale=20), 12)


def crate(at=(0, 0, 0), size=0.5, turn=0):
    wood = mat(WOOD[4], 0.25, WOOD[3], scale=12)
    trim = mat(WOOD[2], 0.2, WOOD[1], scale=12)
    s = size * M
    box((s, s, s), at, wood, rot=(0, 0, turn))
    t = 0.06 * M
    x, y, z = at
    obj = []
    # Edge battens on the camera's three visible edges and the top rim.
    for dx, dy in ((-1, -1), (-1, 1), (1, -1)):
        box((t, t, s), (x + dx * (s / 2 - t / 2 + 0.005), y + dy * (s / 2 - t / 2 + 0.005), z), trim)
    for dx in (-1, 1):
        box((t, s + 0.01, t), (x + dx * (s / 2 - t / 2 + 0.005), y, z + s - t), trim)
    for dy in (-1, 1):
        box((s + 0.01, t, t), (x, y + dy * (s / 2 - t / 2 + 0.005), z + s - t), trim)
    # Diagonal braces on the two front faces.
    box((t * 0.8, 0.02, s * 1.25), (x, y - s / 2 - 0.01, z + 0.03), trim, rot=(0, 45, 0))
    box((0.02, t * 0.8, s * 1.25), (x - s / 2 - 0.01, y, z + 0.03), trim, rot=(-45, 0, 0))


def urn():
    clay = mat(SKIN[1], 0.3, SKIN[0], scale=10)
    r = 0.2 * M
    lathe([(r * 0.5, 0), (r * 0.9, 0.12 * M), (r, 0.3 * M), (r * 0.75, 0.5 * M), (r * 0.45, 0.6 * M), (r * 0.55, 0.66 * M)], (0, 0, 0), clay, 14)
    cyl(r * 0.42, 0.01, (0, 0, 0.655 * M), mat(STONE[0], 0.0), 12)
    band = mat(EMBER[1], 0.0)
    lathe([(r * 1.0, 0.27 * M), (r * 1.01, 0.33 * M)], (0, 0, 0), band, 14)
    rng = seed(3)
    for i in range(3):
        rock((0.12 * M * math.cos(i * 2.1), 0.12 * M * math.sin(i * 2.1) - 0.08, 0), 0.03 * M, clay, rng)


def bone_pile():
    rng = seed(11)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    sphere(0.22 * M, (0, 0, -0.05), mat(STONE[2], 0.4, STONE[1]), scale=(1.2, 1.2, 0.45), segments=10, smooth=False)
    for i in range(14):
        a = rng.random() * 360
        r = rng.random() * 0.2 * M
        z = 0.02 + (0.2 * M - r) * 0.35
        x = r * math.cos(math.radians(a)) - 0.08
        y = r * math.sin(math.radians(a)) - 0.08
        bone((x, y, z), (0.18 + rng.random() * 0.14) * M, rng.random() * 360, bonec, tilt=rng.random() * 25)
    skull((0.02, -0.12, 0.08 * M), 0.07 * M, bonec, turn=10)
    skull((0.15, 0.05, 0.02), 0.06 * M, bonec, turn=-30)


def rubble_pile():
    rng = seed(21)
    stones = [mat(STONE[3], 0.35, STONE[2]), mat(STONE[4], 0.35, STONE[3]), mat(STONE[2], 0.3, STONE[1])]
    for i in range(16):
        a = rng.random() * 6.28
        r = rng.random() ** 0.7 * 0.3 * M
        size = (0.05 + rng.random() * 0.08) * M * (1.2 - r / (0.3 * M) * 0.5)
        rock((r * math.cos(a), r * math.sin(a), size * 0.3 + (0.3 * M - r) * 0.25), size, rng.choice(stones), rng)
    box((0.32 * M, 0.14 * M, 0.12 * M), (0.05, -0.05, 0.0), stones[1], rot=(8, 4, 30))


def broken_column():
    stone = mat(STONE[4], 0.3, STONE[3], scale=8)
    dark = mat(STONE[3], 0.3, STONE[2], scale=8)
    box((0.5 * M, 0.5 * M, 0.12 * M), (0, 0, 0), dark)
    box((0.42 * M, 0.42 * M, 0.08 * M), (0, 0, 0.12 * M), stone)
    rng = seed(5)
    r = 0.15 * M
    segments = 10
    prof = []
    # The shaft, fluted by a faceted lathe, broken off at a slant with a jagged top.
    shaft = lathe([(r, 0), (r * 0.95, 0.75 * M)], (0, 0, 0.2 * M), stone, segments, smooth=False)
    mesh = shaft.data
    for v in mesh.vertices:
        if v.co.z > 0.5:
            v.co.z = (0.5 + 0.18 * (v.co.x / r) + rng.random() * 0.1) * M
    rock((0.2, -0.22, 0.04), 0.08 * M, stone, rng)
    rock((-0.18, -0.2, 0.03), 0.06 * M, dark, rng)


def sarcophagus():
    stone = mat(STONE[3], 0.3, STONE[2], scale=8)
    lid = mat(STONE[4], 0.3, STONE[3], scale=8)
    dark = mat(STONE[1], 0.0)
    L, W = 0.95, 0.44
    rot = (0, 0, 45)
    box((L * 1.0 / 1, W, 0.3 * M), (0, 0, 0), stone, rot=(0, 0, -45))
    box((L + 0.04, W + 0.04, 0.07 * M), (0, 0, 0.3 * M), lid, rot=(0, 0, -45))
    box((L * 0.86, W * 0.6, 0.05 * M), (0, 0, 0.37 * M), lid, rot=(0, 0, -45))
    # A carved cross on the lid.
    box((L * 0.6, 0.03, 0.012), (0, 0, 0.42 * M), dark, rot=(0, 0, -45))
    box((0.03, W * 0.45, 0.012), (-0.08, 0.08, 0.42 * M), dark, rot=(0, 0, -45))


def brazier():
    iron = mat(STONE[1], 0.2, STONE[0], metal=0.6, rough=0.5)
    for i in range(3):
        a = math.radians(i * 120 + 30)
        box((0.035 * M, 0.035 * M, 0.62 * M), (0.13 * M * math.cos(a), 0.13 * M * math.sin(a), 0), iron, rot=(math.cos(a + 1.57) * 12, math.sin(a + 1.57) * -12, 0))
    lathe([(0.08 * M, 0.5 * M), (0.24 * M, 0.62 * M), (0.27 * M, 0.7 * M), (0.25 * M, 0.7 * M), (0.0, 0.62 * M)], (0, 0, 0), iron, 14, cap=False)
    coals = mat(EMBER[2], 0.5, EMBER[0], emit=2.5, scale=25)
    sphere(0.22 * M, (0, 0, 0.66 * M), coals, scale=(1, 1, 0.25), segments=10)
    flame((0, 0, 0.68 * M), 0.12 * M, 7)
    flame((0.06, 0.03, 0.66 * M), 0.08 * M, 7)
    flame((-0.05, 0.05, 0.66 * M), 0.07 * M, 7)


def candles(at=(0, 0, 0), count=6, seed_n=7, spread=0.2):
    rng = seed(seed_n)
    wax = mat(BONE[2], 0.15, BONE[1], scale=10)
    pool = mat(BONE[1], 0.0)
    x0, y0, z0 = at
    sphere(spread * 0.9 * M, (x0, y0, z0 - 0.01), pool, scale=(1, 1, 0.1), segments=10)
    for i in range(count):
        a = rng.random() * 6.28
        r = rng.random() ** 0.6 * spread * M
        h = (0.06 + rng.random() * 0.22) * M
        x, y = x0 + r * math.cos(a), y0 + r * math.sin(a)
        cyl(0.03 * M, h, (x, y, z0), wax, 8)
        sphere(0.034 * M, (x, y, z0 + 0.012), wax, scale=(1.2, 1.2, 0.4), segments=8)
        flame((x, y, z0 + h + 0.01), 0.022 * M, 8)


def candle_cluster():
    candles()


def bucket():
    wood = mat(WOOD[4], 0.25, WOOD[3], scale=14)
    band = mat(STONE[1], 0.0, metal=0.5, rough=0.5)
    r = 0.17 * M
    h = 0.3 * M
    lathe([(r * 0.82, 0), (r, h)], (0, 0, 0), wood, 12, smooth=False)
    cyl(r * 0.93, 0.01, (0, 0, h - 0.04 * M), mat(COLD[2], 0.2, COLD[1], rough=0.15, scale=8), 12)
    for z in (0.15, 0.8):
        rr = r * (0.82 + 0.18 * z) + 0.005
        lathe([(rr, h * z - 0.022 * M), (rr, h * z + 0.022 * M)], (0, 0, 0), band, 12, smooth=False)
    # The handle: an arch of thin boxes over the top.
    for i in range(7):
        a = math.radians(i * 30)
        box((0.02 * M, 0.02 * M, 0.06 * M), (math.cos(a) * r * 1.02 * 0.707, -math.cos(a) * r * 1.02 * 0.707, h + math.sin(a) * 0.18 * M), band, rot=(0, 0, 0))
    # A second, tipped-over bucket beside it.
    tipped = lathe([(r * 0.7, 0), (r * 0.85, h * 0.85)], (0.22, -0.18, 0.12 * M), wood, 12, rot=(90, 0, 70), smooth=False)
    blob(0.26, -0.3, 0.12 * M, mat(COLD[1], 0.0, rough=0.1), rng=seed(4), squash=0.8)


def torch_stand():
    iron = mat(STONE[1], 0.2, STONE[0], metal=0.6, rough=0.5)
    for i in range(3):
        a = math.radians(i * 120 + 30)
        box((0.03 * M, 0.03 * M, 0.25 * M), (0.1 * M * math.cos(a), 0.1 * M * math.sin(a), 0), iron, rot=(math.cos(a + 1.57) * 30, math.sin(a + 1.57) * -30, 0))
    cyl(0.025 * M, 1.45 * M, (0, 0, 0.1 * M), iron, 8)
    lathe([(0.03 * M, 1.42 * M), (0.1 * M, 1.6 * M), (0.11 * M, 1.62 * M), (0.0, 1.6 * M)], (0, 0, 0), iron, 8, smooth=False, cap=False)
    wood = mat(WOOD[2], 0.3, WOOD[1])
    cyl(0.05 * M, 0.12 * M, (0, 0, 1.52 * M), wood, 8)
    flame((0, 0, 1.6 * M), 0.08 * M, 8)


# ---------------------------------------------------------------- decals (flat, on the cell's diamond)

def _blood(n):
    rng = seed(100 + n)
    deep = mat(BLOOD[2], 0.0, rough=0.25, emit=0.8)
    mid = mat(BLOOD[3], 0.0, rough=0.25, emit=0.8)
    cx, cy = (rng.random() - 0.5) * 0.15, (rng.random() - 0.5) * 0.15
    blob(cx, cy, 0.14 + rng.random() * 0.06, deep, 12, 0.5, rng)
    blob(cx - 0.02, cy - 0.02, 0.08, mid, 9, 0.5, rng, z=0.004)
    for i in range(5 + n):
        a = rng.random() * 6.28
        d = 0.18 + rng.random() * 0.12
        blob(cx + d * math.cos(a), cy + d * math.sin(a), 0.015 + rng.random() * 0.025, deep if i % 2 else mid, 6, 0.4, rng)
    if n % 2 == 0:
        # A smear, as if something was dragged.
        a = rng.random() * 6.28
        pts = []
        for t in range(6):
            s = t / 5
            pts.append((cx + math.cos(a) * s * 0.3 - math.sin(a) * 0.03 * (1 - s), cy + math.sin(a) * s * 0.3 + math.cos(a) * 0.03 * (1 - s)))
        for t in reversed(range(6)):
            s = t / 5
            pts.append((cx + math.cos(a) * s * 0.3 + math.sin(a) * 0.03 * (1 - s), cy + math.sin(a) * s * 0.3 - math.cos(a) * 0.03 * (1 - s)))
        flat(pts, deep, 0.003)


def _bones(n):
    rng = seed(200 + n)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    for i in range(3 + n):
        bone(((rng.random() - 0.5) * 0.3, (rng.random() - 0.5) * 0.3, 0), (0.14 + rng.random() * 0.1) * M, rng.random() * 360, bonec, 0.018)


def _skull(n):
    rng = seed(300 + n)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    skull((0.02, 0.02, 0), 0.075 * M, bonec, turn=(n - 1) * 40)
    for i in range(2):
        bone(((rng.random() - 0.5) * 0.3, (rng.random() - 0.5) * 0.3 - 0.1, 0), 0.12 * M, rng.random() * 360, bonec, 0.016)


def _rubble(n):
    rng = seed(400 + n)
    stones = [mat(STONE[3], 0.35, STONE[2]), mat(STONE[4], 0.35, STONE[3])]
    for i in range(7 + 3 * n):
        a = rng.random() * 6.28
        r = rng.random() * 0.24
        rock((r * math.cos(a), r * math.sin(a), 0), (0.02 + rng.random() * 0.035) * M, rng.choice(stones), rng, 0.5)


def _puddle(n):
    rng = seed(500 + n)
    water = mat(COLD[1], 0.0, rough=0.05)
    shine = mat(COLD[3], 0.0, rough=0.05)
    blob(0, 0, 0.2, water, 14, 0.35, rng)
    blob(-0.04, -0.06, 0.05, shine, 8, 0.4, rng, z=0.004, squash=0.5)
    blob(0.18, 0.12, 0.05, water, 8, 0.4, rng)


def _ritual(n):
    # A small summoning sigil scratched in blood: a ring and a five-pointed star, with candle stubs on its points.
    red = mat(BLOOD[3], 0.0, rough=0.3, emit=1.0)
    R = 0.3
    w = 0.035
    steps = 24
    for i in range(steps):
        a0 = 2 * math.pi * i / steps
        a1 = 2 * math.pi * (i + 1) / steps
        flat([(R * math.cos(a0), R * math.sin(a0)), (R * math.cos(a1), R * math.sin(a1)),
              ((R - w) * math.cos(a1), (R - w) * math.sin(a1)), ((R - w) * math.cos(a0), (R - w) * math.sin(a0))], red)
    star = [(R * 0.92 * math.cos(math.radians(90 + 144 * k + n * 20)), R * 0.92 * math.sin(math.radians(90 + 144 * k + n * 20))) for k in range(6)]
    for (x0, y0), (x1, y1) in zip(star, star[1:]):
        dx, dy = x1 - x0, y1 - y0
        l = math.hypot(dx, dy)
        nx, ny = -dy / l * w / 2, dx / l * w / 2
        flat([(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)], red, 0.003)
    if n == 2:
        wax = mat(BONE[1], 0.0)
        for x, y in star[:5:2]:
            cyl(0.02 * M, 0.04 * M, (x, y, 0), wax, 6)


# ---------------------------------------------------------------- lairs (2026-10-08)
# The owner: "make it so that it looks like the monsters and demons are truly living in them". Each pack's corner of
# a room is dressed after who lives there (Lairs.cs): bandit camps, feeding grounds, dens, shrines, tombs, the drowned's
# hold and demon nests. One cell each, standing in the cell's middle.

def lair_campfire():
    rng = seed(41)
    stones = [mat(STONE[3], 0.35, STONE[2]), mat(STONE[2], 0.35, STONE[1])]
    for k in range(9):
        a = 2 * math.pi * k / 9
        rock((0.28 * math.cos(a), 0.28 * math.sin(a), 0.02), 0.08, rng.choice(stones), rng, 0.7)
    log = mat(WOOD[2], 0.3, WOOD[1], scale=12)
    for k in range(4):
        a = 2 * math.pi * k / 4 + 0.4
        cyl(0.045, 0.42, (0.18 * math.cos(a), 0.18 * math.sin(a), 0.02), log, 6, rot=(0, -60, math.degrees(a)))
    sphere(0.18, (0, 0, 0.0), mat(EMBER[1], 0.5, EMBER[0], emit=2.0, scale=20), scale=(1, 1, 0.3), segments=10)
    flame((0, 0, 0.03), 0.13, 7)
    flame((0.07, 0.04, 0.03), 0.09, 7)


def lair_sack():
    rng = seed(42)
    cloth = [mat(EARTH[4], 0.35, EARTH[3], scale=14), mat(EARTH[5], 0.35, EARTH[4], scale=14)]
    for k, (x, y, r) in enumerate([(-0.08, 0.07, 0.17), (0.13, -0.06, 0.15), (0.0, -0.16, 0.12)]):
        sphere(r, (x, y, r * 0.75), cloth[k % 2], scale=(1, 1, 1.1), segments=10)
        cyl(r * 0.35, r * 0.4, (x, y, r * 1.55), cloth[k % 2], 8)
        cyl(r * 0.38, 0.015, (x, y, r * 1.6), mat(WOOD[1], 0.0), 8)


def lair_weapon_rack():
    wood = mat(WOOD[2], 0.4, WOOD[1], scale=20)
    iron = mat(STONE[4], 0.2, STONE[2], metal=0.6, rough=0.5)
    for y in (-0.2, 0.2):
        box((0.07, 0.07, 0.6), (0.05, y, 0), wood, rot=(0, 0, -45))
    box((0.05, 0.5, 0.05), (0.05, 0, 0.35), wood, rot=(0, 0, -45))
    box((0.05, 0.5, 0.05), (0.05, 0, 0.12), wood, rot=(0, 0, -45))
    for k, y in enumerate((-0.14, -0.04, 0.06, 0.16)):
        x, yy = 0.05 * 0.7 - y * 0.7, 0.05 * 0.7 + y * 0.7
        box((0.03, 0.03, 0.65), (x - 0.05, yy - 0.05, 0.0), wood, rot=(8, -8, 0))
        cone(0.04, 0.12, (x - 0.08, yy - 0.08, 0.53), iron, 4, rot=(8, -8, 0))
    box((0.04, 0.22, 0.2), (-0.15, -0.15, 0.0), iron, rot=(0, 0, -45))


def lair_carcass():
    rng = seed(43)
    flesh = mat(BLOOD[2], 0.4, BLOOD[1], scale=12)
    hide = mat(EARTH[3], 0.4, EARTH[2], scale=14)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    sphere(0.2, (0, 0, 0.06), hide, scale=(1.5, 0.8, 0.5), segments=10)
    sphere(0.14, (-0.06, -0.06, 0.1), flesh, scale=(1.3, 0.8, 0.55), segments=10)
    for k in range(6):
        x = -0.18 + k * 0.07
        cyl(0.012, 0.18, (x, -0.08, 0.06), bonec, 6, rot=(60, 0, 0))
    sphere(0.08, (0.28, 0.06, 0.06), hide, scale=(1.3, 0.9, 0.9), segments=8)
    blob(0, 0, 0.32, mat(BLOOD[1], 0.0, rough=0.25), 12, 0.5, rng)


def lair_altar():
    stone = mat(STONE[2], 0.4, STONE[1], scale=8)
    top = mat(STONE[3], 0.3, STONE[2], scale=8)
    blood = mat(BLOOD[2], 0.0, rough=0.25, emit=0.8)
    box((0.48, 0.3, 0.3), (0, 0, 0), stone, rot=(0, 0, -45), bevel=0.01)
    box((0.56, 0.36, 0.06), (0, 0, 0.3), top, rot=(0, 0, -45), bevel=0.01)
    box((0.3, 0.12, 0.004), (0.02, -0.02, 0.36), blood, rot=(0, 0, -45))
    candles((0, 0, 0.36), count=4, seed_n=44, spread=0.15)
    skull((-0.12, 0.12, 0.36), 0.05, mat(BONE[1], 0.3, BONE[0], scale=15), turn=10)


def lair_coffin():
    wood = mat(WOOD[1], 0.4, WOOD[0], scale=18)
    inside = mat(STONE[0], 0.0)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    pts = [(-0.38, -0.08), (-0.38, 0.08), (0.12, 0.15), (0.38, 0.1), (0.38, -0.1), (0.12, -0.15)]
    prism(pts, 0.14, (0, 0, 0), wood, rot=(0, 0, -45))
    inner = [(x * 0.85, y * 0.7) for x, y in pts]
    prism(inner, 0.02, (0, 0, 0.125), inside, rot=(0, 0, -45))
    skull((-0.08, 0.08, 0.1), 0.05, bonec, turn=-45)
    # The lid, pushed half off.
    prism(pts, 0.03, (0.12, -0.16, 0.0), wood, rot=(0, 18, -30))


def lair_flesh_pod():
    rng = seed(45)
    flesh = [mat(BLOOD[2], 0.4, BLOOD[1], scale=12), mat(SKIN[1], 0.4, BLOOD[1], scale=12), mat(BLOOD[3], 0.3, BLOOD[2], scale=12)]
    glow = mat(EMBER[1], 0.0, emit=2.5)
    for k in range(rng.randint(3, 4)):
        a = rng.random() * 6.28
        r = rng.random() * 0.14
        size = rng.uniform(0.08, 0.15)
        sphere(size, (r * math.cos(a), r * math.sin(a), size * 0.8), rng.choice(flesh), scale=(1, 1, 1.3), segments=10)
        sphere(size * 0.25, (r * math.cos(a) - size * 0.6, r * math.sin(a) - size * 0.6, size * 1.0), glow, segments=6)
    blob(0, 0, 0.3, mat(BLOOD[1], 0.0, rough=0.3), 12, 0.6, rng)


def lair_spikes():
    rng = seed(46)
    dark = [mat(STONE[1], 0.4, STONE[0], scale=10), mat(BLOOD[0], 0.4, STONE[0], scale=10)]
    for k in range(7):
        a = rng.random() * 6.28
        r = rng.random() * 0.2
        h = rng.uniform(0.25, 0.55)
        cone(rng.uniform(0.04, 0.07), h, (r * math.cos(a), r * math.sin(a), 0), rng.choice(dark), 5,
             rot=(rng.uniform(-15, 15), rng.uniform(-15, 15), 0))
    sphere(0.22, (0, 0, -0.04), dark[0], scale=(1, 1, 0.3), segments=8, smooth=False)


def lair_hellfire():
    rng = seed(47)
    rim = [mat(STONE[1], 0.4, STONE[0]), mat(BLOOD[0], 0.4, STONE[0])]
    for k in range(8):
        a = 2 * math.pi * k / 8
        cone(0.05, rng.uniform(0.12, 0.25), (0.22 * math.cos(a), 0.22 * math.sin(a), 0), rng.choice(rim), 5, rot=(math.sin(a) * 20, -math.cos(a) * 20, 0))
    sphere(0.2, (0, 0, 0.0), mat(BLOOD[3], 0.4, EMBER[1], emit=3.0, scale=20), scale=(1, 1, 0.25), segments=10)
    core = mat(BLOOD[4], 0.0, emit=6.0)
    outer = mat(BLOOD[3], 0.0, emit=4.0)
    for (x, y, size) in ((0, 0, 0.12), (0.06, 0.03, 0.08), (-0.06, 0.04, 0.07)):
        lathe([(size * 0.5, 0), (size * 0.62, size * 0.5), (size * 0.35, size * 1.4), (0.001, size * 2.4)], (x, y, 0.03), outer, 8)
        lathe([(size * 0.3, 0), (size * 0.36, size * 0.4), (0.001, size * 1.5)], (x - size * 0.25, y - size * 0.25, 0.05), core, 8)


def lair_cage():
    iron = mat(STONE[1], 0.2, STONE[0], metal=0.6, rough=0.5)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    box((0.42, 0.42, 0.04), (0, 0, 0), iron)
    box((0.42, 0.42, 0.04), (0, 0, 0.6), iron)
    for i in range(5):
        for side in (-1, 1):
            t = -0.19 + i * 0.095
            box((0.02, 0.02, 0.6), (t, side * 0.2, 0.02), iron)
            box((0.02, 0.02, 0.6), (side * 0.2, t, 0.02), iron)
    skull((0.02, 0.02, 0.04), 0.05, bonec, turn=0)
    bone((-0.1, 0.0, 0.04), 0.18, 30, bonec, 0.015)


def lair_stake():
    wood = mat(WOOD[1], 0.4, WOOD[0], scale=20)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    blood = mat(BLOOD[2], 0.0, rough=0.25)
    cyl(0.025, 0.7, (0, 0, 0), wood, 6, rot=(4, -3, 0))
    skull((0.0, 0.0, 0.62), 0.065, bonec, turn=-45)
    box((0.03, 0.03, 0.2), (0.0, -0.03, 0.42), blood)
    rng = seed(48)
    blob(0, 0, 0.12, blood, 9, 0.5, rng)


def _bedroll(n):
    rng = seed(400 + n)
    cloth = mat([EARTH[4], WOOD[3], COLD[2]][n % 3], 0.35, EARTH[2], scale=12)
    fur = mat(EARTH[5], 0.4, EARTH[3], scale=20)
    a = -45 + (n - 1) * 30
    box((0.5, 0.22, 0.02), (0, 0, 0), cloth, rot=(0, 0, a))
    cyl(0.05, 0.22, (-0.2 * math.cos(math.radians(a)), -0.2 * math.sin(math.radians(a)), 0.05), fur, 8, rot=(0, 90, a + 90))


def _straw(n):
    rng = seed(410 + n)
    straw = [mat(EMBER[3], 0.4, WOOD[3], scale=20), mat(WOOD[4], 0.4, WOOD[3], scale=20)]
    blob(0, 0, 0.28, mat(WOOD[3], 0.3, WOOD[2], scale=20), 12, 0.5, rng)
    for k in range(30):
        a = rng.random() * 180
        r = rng.random() * 0.26
        b = rng.random() * 6.28
        box((0.12, 0.008, 0.004), (r * math.cos(b), r * math.sin(b), 0.004), rng.choice(straw), rot=(0, 0, a))


def _gore(n):
    rng = seed(420 + n)
    deep = mat(BLOOD[1], 0.0, rough=0.25, emit=0.6)
    meat = mat(BLOOD[3], 0.3, BLOOD[2], scale=14)
    blob(0, 0, 0.18 + 0.04 * n, deep, 12, 0.6, rng)
    for k in range(3 + n):
        a = rng.random() * 6.28
        r = rng.random() * 0.15
        sphere(rng.uniform(0.02, 0.04), (r * math.cos(a), r * math.sin(a), 0.01), meat, scale=(1.3, 1, 0.5), segments=6)


def _sigil(n):
    rng = seed(430 + n)
    glow = mat(BLOOD[3], 0.0, rough=0.3, emit=2.0)
    R = 0.2
    for k in range(3 + n):
        a0 = rng.random() * 6.28
        a1 = a0 + rng.uniform(1.5, 3.0)
        x0, y0, x1, y1 = R * math.cos(a0), R * math.sin(a0), R * math.cos(a1), R * math.sin(a1)
        dx, dy = x1 - x0, y1 - y0
        l = math.hypot(dx, dy)
        nx, ny = -dy / l * 0.012, dx / l * 0.012
        flat([(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)], glow, 0.003)


def _ash(n):
    rng = seed(440 + n)
    blob(0, 0, 0.22, mat(STONE[1], 0.4, STONE[0], scale=14), 12, 0.5, rng)
    blob(0.02, 0.02, 0.1, mat(STONE[2], 0.4, STONE[1], scale=14), 9, 0.5, rng, z=0.004)
    for k in range(4):
        cyl(0.012, 0.12, ((rng.random() - 0.5) * 0.2, (rng.random() - 0.5) * 0.2, 0.012), mat(STONE[0], 0.0), 5, rot=(0, 90, rng.random() * 180))


def _net(n):
    rng = seed(450 + n)
    rope = mat(EARTH[4], 0.3, EARTH[2], scale=14)
    for k in range(-3, 4):
        box((0.5, 0.01, 0.006), (0, k * 0.06, 0.003), rope, rot=(0, 0, 10 * n))
        box((0.01, 0.4, 0.006), (k * 0.07, 0, 0.003), rope, rot=(0, 0, 10 * n))


# ---------------------------------------------------------------- quests (2026-10-08)

def _vigil_fire(lit):
    """Emberwatch's Vigil fire, and the drowned watch's: a wide iron bowl on a stepped stone plinth, cold ash and
    charred logs when dead, a tall fire when lit."""
    rng = seed(61)
    stone = [mat(STONE[3], 0.35, STONE[2], scale=8), mat(STONE[2], 0.35, STONE[1], scale=8)]
    iron = mat(STONE[1], 0.2, STONE[0], metal=0.6, rough=0.5)
    box((1.5, 1.5, 0.18), (0, 0, 0), stone[1], rot=(0, 0, 45), bevel=0.02)
    box((1.15, 1.15, 0.2), (0, 0, 0.18), stone[0], rot=(0, 0, 45), bevel=0.02)
    lathe([(0.25, 0.38), (0.6, 0.55), (0.72, 0.75), (0.68, 0.77), (0.0, 0.62)], (0, 0, 0), iron, 18, cap=False)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        box((0.08, 0.08, 0.4), (0.55 * math.cos(a), 0.55 * math.sin(a), 0.38), iron)
    log = mat(WOOD[0], 0.4, STONE[0], scale=12) if not lit else mat(WOOD[2], 0.3, WOOD[1], scale=12)
    for k in range(6):
        a = 2 * math.pi * k / 6 + 0.2
        cyl(0.07, 0.75, (0.3 * math.cos(a), 0.3 * math.sin(a), 0.66), log, 6, rot=(0, -58, math.degrees(a)))
    if lit:
        sphere(0.5, (0, 0, 0.66), mat(EMBER[1], 0.5, EMBER[0], emit=2.5, scale=20), scale=(1, 1, 0.3), segments=12)
        flame((0, 0, 0.7), 0.3, 8)
        flame((0.16, 0.1, 0.7), 0.22, 8)
        flame((-0.15, 0.12, 0.7), 0.2, 8)
        flame((0.05, -0.18, 0.7), 0.18, 8)
    else:
        sphere(0.5, (0, 0, 0.66), mat(STONE[2], 0.5, STONE[1], scale=20), scale=(1, 1, 0.22), segments=12)


def vigil_fire_unlit():
    _vigil_fire(False)


def vigil_fire_lit():
    _vigil_fire(True)


def quest_lectern():
    """A watch post's lectern with a book lying open on it (the Warden's log, the cartographer's notes)."""
    wood = mat(WOOD[2], 0.4, WOOD[1], scale=18)
    dark = mat(WOOD[1], 0.4, WOOD[0], scale=18)
    page = mat(BONE[2], 0.15, BONE[1], scale=10)
    box((0.3, 0.3, 0.06), (0, 0, 0), dark, rot=(0, 0, 45))
    box((0.1, 0.1, 0.8), (0, 0, 0.06), wood, rot=(0, 0, 45))
    box((0.5, 0.36, 0.05), (0, 0, 0.85), wood, rot=(-25, 0, 45))
    box((0.42, 0.3, 0.03), (0, 0, 0.9), page, rot=(-25, 0, 45))
    box((0.012, 0.3, 0.035), (0, 0, 0.9), dark, rot=(-25, 0, 45))
    box((0.04, 0.2, 0.004), (0.12, -0.02, 0.93), mat(BLOOD[2], 0.0), rot=(-25, 0, 45))


def quest_oath_stone():
    """The first watch's oath-stone: a tall slab broken across, its top half fallen beside it, letters cut in it."""
    rng = seed(62)
    stone = mat(STONE[3], 0.35, STONE[2], scale=8)
    cut = mat(EMBER[1], 0.0, emit=1.5)
    box((0.55, 0.18, 0.7), (0, 0, 0), stone, rot=(0, 0, -45), bevel=0.02)
    box((0.55, 0.18, 0.5), (0.25, -0.35, 0), stone, rot=(80, 0, -30), bevel=0.02)
    for k in range(4):
        box((0.35, 0.01, 0.03), (0.0, -0.1 * 0.7, 0.2 + k * 0.11), cut, rot=(0, 0, -45))
    rock((0.3, 0.2, 0.03), 0.08, stone, rng)


def quest_great_lamp():
    """The watch's great lamp, fallen on its side: a brass cage taller than a man, its glass cracked, still glowing."""
    brass = mat(EMBER[2], 0.3, EMBER[0], metal=0.7, rough=0.4)
    glass = mat(EMBER[3], 0.0, emit=2.0)
    lathe([(0.0, 0.0), (0.3, 0.0), (0.32, 0.1), (0.2, 0.15)], (0, 0, 0), brass, 12, rot=(0, 75, 30))
    sphere(0.28, (0.35, 0.1, 0.3), glass, scale=(1.4, 1, 1), segments=12)
    for k in range(6):
        a = 2 * math.pi * k / 6
        box((0.9, 0.03, 0.03), (0.4, 0.1 + 0.3 * math.cos(a), 0.3 + 0.3 * math.sin(a)), brass, rot=(0, 0, 0))
    lathe([(0.0, 0.0), (0.25, 0.0), (0.12, 0.2), (0.0, 0.3)], (0.9, 0.1, 0.3), brass, 10, rot=(0, 90, 0))


def quest_rift_heart():
    """A rift heart: a knot of black stone and flesh split open on a violet glow, ribs of bone curling round it."""
    rng = seed(63)
    flesh = [mat(BLOOD[2], 0.4, BLOOD[0], scale=12), mat(VIOLET[0], 0.4, STONE[0], scale=12)]
    glow = mat(VIOLET[1], 0.0, emit=5.0)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    sphere(0.55, (0, 0, 0.55), flesh[1], scale=(1, 1, 1.15), segments=14)
    sphere(0.3, (-0.25, -0.25, 0.7), glow, scale=(1, 1, 1.5), segments=10)
    for k in range(8):
        a = rng.random() * 6.28
        sphere(rng.uniform(0.12, 0.22), (0.45 * math.cos(a), 0.45 * math.sin(a), rng.uniform(0.2, 0.9)), rng.choice(flesh), segments=8)
    for k in range(6):
        a = 2 * math.pi * k / 6
        cone(0.06, 1.0, (0.62 * math.cos(a), 0.62 * math.sin(a), 0.0), bonec, 5, rot=(math.sin(a) * -25, math.cos(a) * 25, 0))
    blob(0, 0, 1.0, mat(BLOOD[1], 0.0, rough=0.3), 14, 0.5, rng)


def quest_bone_pyre():
    """A grave priests' pyre: bones stacked in a cone round a stake, burning with a cold green fire."""
    rng = seed(64)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    wood = mat(WOOD[1], 0.4, WOOD[0], scale=20)
    cyl(0.05, 1.3, (0, 0, 0), wood, 6)
    for k in range(26):
        a = rng.random() * 360
        r = rng.random() * 0.45
        z = (0.5 - r) * 0.8
        bone((r * math.cos(math.radians(a)), r * math.sin(math.radians(a)), z), rng.uniform(0.3, 0.5), rng.random() * 360, bonec, 0.03, tilt=rng.random() * 30)
    for k in range(3):
        skull((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), rng.uniform(0.1, 0.45)), 0.09, bonec, turn=rng.random() * 360)
    green = mat((120, 220, 130), 0.0, emit=5.0)
    pale = mat((190, 240, 180), 0.0, emit=6.0)
    for (x, y, size) in ((0, 0, 0.22), (0.12, 0.08, 0.15), (-0.12, 0.1, 0.14)):
        lathe([(size * 0.5, 0), (size * 0.62, size * 0.5), (size * 0.35, size * 1.4), (0.001, size * 2.4)], (x, y, 0.55), green, 8)
        lathe([(size * 0.3, 0), (size * 0.36, size * 0.4), (0.001, size * 1.5)], (x - size * 0.25, y - size * 0.25, 0.57), pale, 8)


# ---------------------------------------------------------------- walls

def _rubble_block(height, variant):
    """A wall block on one cell after Diablo 2's first act (2026-10-04): rough fieldstones of mixed grey and earth tones
    in uneven courses over a dark earth core, a top of packed earth and loose stones. Only the faces toward the camera
    (-x, -y) are built stone by stone. Variants: 1 plain, 2 a timber post and beam, 3 broken (stones fallen out, rubble
    at its foot), 4 moss and roots hanging from the top."""
    rng = seed(int(height * 1000) + variant * 7919)
    half = CELL / 2
    core = mat(EARTH[0], 0.0)
    stones = [mat(STONE[4], 0.35, STONE[3], scale=16), mat(STONE[5], 0.3, STONE[4], scale=16),
              mat(EARTH[5], 0.35, EARTH[4], scale=16), mat(EARTH[4], 0.3, EARTH[3], scale=16),
              mat(STONE[3], 0.3, EARTH[2], scale=16), mat(EARTH[6], 0.3, EARTH[5], scale=16)]
    loose = [mat(STONE[2], 0.3, STONE[1], scale=16), mat(EARTH[2], 0.3, EARTH[1], scale=16)]
    box((CELL - 0.01, CELL - 0.01, height - 0.02), (0, 0, 0), core)
    # Courses of uneven height, filling the face; stones of random length, standing a little proud by random amounts.
    z = 0.0
    course = 0
    while z < height - 0.03:
        ch = min(rng.uniform(0.085, 0.14), height - 0.02 - z)
        for face in ("x", "y"):
            t = -half + (rng.uniform(0.0, 0.12) if course % 2 else 0.0)
            if t > -half:
                # A short stone fills the start of an offset course.
                pieces = [(-half, t)]
            else:
                pieces = []
            while t < half - 0.03:
                length = min(rng.uniform(0.12, 0.26), half - t)
                pieces.append((t, t + length))
                t += length
            for t0, t1 in pieces:
                if variant == 3 and z > height * 0.45 and rng.random() < 0.3:
                    continue  # fallen out: the dark core shows
                gap = 0.012
                length = t1 - t0 - gap
                if length < 0.03:
                    continue
                mid = (t0 + t1) / 2
                proud = rng.uniform(0.012, 0.035)
                sh = ch - gap - rng.uniform(0, 0.012)
                lift = z + gap / 2 + rng.uniform(0, 0.006)
                if face == "x":
                    box((proud * 2, length, sh), (-half, mid, lift), rng.choice(stones), bevel=0.01)
                else:
                    box((length, proud * 2, sh), (mid, -half, lift), rng.choice(stones), bevel=0.01)
        z += ch
        course += 1
    # The top: dark packed earth with a few loose stones, so a run of wall reads as one dark mass from above.
    earth = mat(STONE[1], 0.45, STONE[0], scale=10)
    box((CELL + 0.02, CELL + 0.02, 0.03), (0, 0, height - 0.03), earth)
    for k in range(rng.randint(2, 4)):
        size = rng.uniform(0.04, 0.08)
        box((size, size * rng.uniform(0.7, 1.2), size * 0.5), (rng.uniform(-half + 0.08, half - 0.08), rng.uniform(-half + 0.08, half - 0.08), height),
            rng.choice(loose), rot=(0, 0, rng.uniform(0, 90)), bevel=0.008)
    if variant == 2 and height > 0.3:
        wood = mat(WOOD[2], 0.4, WOOD[1], scale=20)
        post = 0.075
        box((post, post, height + 0.02), (-half - 0.01, -half - 0.01, 0), wood, bevel=0.006)
        box((CELL, post * 0.9, post * 0.9), (0, -half - 0.02, height - 0.12), wood, bevel=0.006)
        box((post * 0.9, CELL, post * 0.9), (-half - 0.02, 0, height - 0.12), wood, bevel=0.006)
    if variant == 3:
        for k in range(rng.randint(3, 5)):
            size = rng.uniform(0.04, 0.09)
            side = rng.random() < 0.5
            along = rng.uniform(-half + 0.05, half - 0.05)
            out = rng.uniform(0.03, 0.12)
            loc = (-half - out, along, 0) if side else (along, -half - out, 0)
            box((size, size * 0.9, size * 0.6), loc, rng.choice(stones), rot=(0, 0, rng.uniform(0, 90)), bevel=0.01)
    if variant == 4:
        moss = [mat(MOSS[2], 0.4, MOSS[1], scale=20), mat(MOSS[3], 0.4, MOSS[2], scale=20)]
        for k in range(3):
            size = rng.uniform(0.18, 0.3)
            box((size, size * 0.8, 0.02), (rng.uniform(-half + 0.1, half - 0.1), rng.uniform(-half + 0.1, half - 0.1), height - 0.005), moss[k % 2])
        for face in ("x", "y"):
            for k in range(rng.randint(3, 6)):
                along = rng.uniform(-half + 0.04, half - 0.04)
                drop = rng.uniform(0.06, height * 0.6)
                w = rng.uniform(0.03, 0.07)
                if face == "x":
                    box((0.012, w, drop), (-half - 0.035, along, height - drop), rng.choice(moss))
                else:
                    box((w, 0.012, drop), (along, -half - 0.035, height - drop), rng.choice(moss))


# Tall wall faces (2026-10-08, the owner: "no real feel of traveling underground", "all walls are perfectly straight
# and nothing feels rugged", built halls "more rugged and broken down and more demonic the further down"). They stand
# only where the floor is in front and rock behind (the far side of a room or passage), so they can rise well above a
# character without hiding anything. Three themes by depth, four variants each:
#   crypt (the top levels): fieldstone courses, broken top; 1 plain, 2 timber shoring, 3 stones fallen out, 4 roots.
#   ruin (the middle): darker stone, large gaps of raw rock, cracks, boulders bulging out, rubble at the foot.
#   demonic (the bottom): blackened stone, cracks glowing ember red, veins of flesh, bones and horns set in the wall.
TALL_WALL_H = 1.1 / 0.866
THEMES = ("crypt", "ruin", "demonic")


def _tall_wall(theme, variant):
    rng = seed(theme * 1000 + variant * 7919 + 13)
    half = CELL / 2
    h = TALL_WALL_H
    if theme == 0:
        stones = [mat(STONE[4], 0.35, STONE[3], scale=16), mat(STONE[5], 0.3, STONE[4], scale=16),
                  mat(EARTH[5], 0.35, EARTH[4], scale=16), mat(EARTH[4], 0.3, EARTH[3], scale=16), mat(STONE[3], 0.3, EARTH[2], scale=16)]
        core = mat(EARTH[0], 0.0)
        missing = 0.06 if variant != 3 else 0.28
    elif theme == 1:
        stones = [mat(STONE[3], 0.35, STONE[2], scale=16), mat(STONE[4], 0.35, STONE[2], scale=16),
                  mat(EARTH[3], 0.35, EARTH[2], scale=16), mat(EARTH[4], 0.35, EARTH[2], scale=16)]
        core = mat(STONE[1], 0.5, STONE[0], scale=8)
        missing = 0.22 if variant != 3 else 0.42
    else:
        stones = [mat(STONE[2], 0.4, STONE[1], scale=16), mat(STONE[1], 0.35, STONE[0], scale=16),
                  mat(BLOOD[1], 0.4, BLOOD[0], scale=16), mat(STONE[2], 0.4, BLOOD[0], scale=16)]
        core = mat(STONE[0], 0.4, BLOOD[0], scale=8)
        missing = 0.2
    raw = mat(STONE[1], 0.6, STONE[0], scale=5) if theme > 0 else mat(EARTH[1], 0.5, EARTH[0], scale=6)

    # The broken top: the wall's height along each face, in three steps that differ by a fifth or more.
    def tops():
        return [h * rng.uniform(0.8 if theme else 0.86, 1.0) for _ in range(3)]
    face_tops = {"x": tops(), "y": tops()}

    def top_at(face, t):
        k = min(2, int((t + half) / CELL * 3))
        return face_tops[face][k]

    # The core: nine columns of rock, each as tall as the lower of the faces' steps over it, so the top is jagged.
    n = 3
    step = CELL / n
    for i in range(n):
        for j in range(n):
            cx = -half + step * (i + 0.5)
            cy = -half + step * (j + 0.5)
            ch = min(top_at("y", cx), top_at("x", cy)) * rng.uniform(0.92, 1.0)
            box((step + 0.004, step + 0.004, ch), (cx, cy, 0), core)
            # Rough rock heaped on top.
            rock((cx, cy, ch - 0.01), rng.uniform(0.05, 0.09), raw, rng, squash=0.6)

    # Courses of stone on the two faces the camera sees.
    for face in ("x", "y"):
        z = 0.0
        course = 0
        while z < h - 0.03:
            ch = rng.uniform(0.085, 0.15)
            t = -half + (rng.uniform(0.0, 0.12) if course % 2 else 0.0)
            pieces = [(-half, t)] if t > -half else []
            while t < half - 0.03:
                length = min(rng.uniform(0.12, 0.28), half - t)
                pieces.append((t, t + length))
                t += length
            for t0, t1 in pieces:
                mid = (t0 + t1) / 2
                top = top_at(face, mid)
                if z + 0.04 > top:
                    continue
                # Fallen out: higher up more often; the dark core or raw rock shows.
                fall = missing * (0.4 + 1.2 * z / h)
                if rng.random() < fall:
                    continue
                gap = 0.012
                length = t1 - t0 - gap
                if length < 0.03:
                    continue
                proud = rng.uniform(0.012, 0.04 if theme else 0.032)
                sh = min(ch - gap - rng.uniform(0, 0.014), top - z)
                lift = z + gap / 2 + rng.uniform(0, 0.006)
                tilt = rng.uniform(-4, 4) if theme else 0
                if face == "x":
                    box((proud * 2, length, sh), (-half, mid, lift), rng.choice(stones), rot=(tilt, 0, 0), bevel=0.01)
                else:
                    box((length, proud * 2, sh), (mid, -half, lift), rng.choice(stones), rot=(0, tilt, 0), bevel=0.01)
            z += ch
            course += 1

    def on_face(face, along, out, z):
        return (-half - out, along, z) if face == "x" else (along, -half - out, z)

    # Rubble fallen at the foot.
    for k in range(rng.randint(1, 3) + theme + (3 if variant == 3 else 0)):
        face = rng.choice("xy")
        rock(on_face(face, rng.uniform(-half + 0.05, half - 0.05), rng.uniform(0.03, 0.14), 0.0), rng.uniform(0.03, 0.08), rng.choice(stones), rng, squash=0.7)

    if theme == 0 and variant == 2:
        # Timber shoring, as in a mine: two posts and a lintel braced against the face.
        wood = mat(WOOD[2], 0.4, WOOD[1], scale=20)
        for face in ("x", "y"):
            for along in (-half + 0.06, half - 0.06):
                x, y, _ = on_face(face, along, 0.04, 0)
                box((0.07, 0.07, h * 0.8), (x, y, 0), wood, bevel=0.006)
            x, y, _ = on_face(face, 0, 0.05, 0)
            box((0.08, CELL, 0.07) if face == "x" else (CELL, 0.08, 0.07), (x, y, h * 0.8 - 0.07), wood, bevel=0.006)
    if theme == 0 and variant == 4 or theme == 1 and variant == 4:
        # Roots and moss hanging from the broken top.
        moss = [mat(MOSS[2], 0.4, MOSS[1], scale=20), mat(WOOD[1], 0.4, WOOD[0], scale=20)]
        for face in ("x", "y"):
            for k in range(rng.randint(4, 7)):
                along = rng.uniform(-half + 0.04, half - 0.04)
                top = top_at(face, along)
                drop = rng.uniform(0.1, top * 0.7)
                w = rng.uniform(0.015, 0.05)
                x, y, _ = on_face(face, along, 0.045, 0)
                box((0.012, w, drop) if face == "x" else (w, 0.012, drop), (x, y, top - drop), rng.choice(moss))
    if theme == 1 and variant == 2:
        # Boulders of the raw rock behind, bulging through the masonry.
        for k in range(rng.randint(2, 3)):
            face = rng.choice("xy")
            rock(on_face(face, rng.uniform(-half + 0.1, half - 0.1), 0.0, rng.uniform(0.1, h * 0.6)), rng.uniform(0.09, 0.15), raw, rng, squash=0.9)
    if theme == 2 and variant in (2, 4):
        # Cracks glowing ember red, zigzagging down the faces.
        glow = mat(EMBER[1], 0.0, emit=3.0)
        for face in ("x", "y"):
            along = rng.uniform(-half + 0.1, half - 0.1)
            z = top_at(face, along) * rng.uniform(0.6, 0.95)
            while z > 0.05:
                seg = rng.uniform(0.06, 0.14)
                x, y, _ = on_face(face, along, 0.03, 0)
                box((0.014, 0.022, seg) if face == "x" else (0.022, 0.014, seg), (x, y, z - seg), glow, rot=(rng.uniform(-25, 25), 0, 0) if face == "x" else (0, rng.uniform(-25, 25), 0))
                along = max(-half + 0.05, min(half - 0.05, along + rng.uniform(-0.06, 0.06)))
                z -= seg
    if theme == 2 and variant == 3:
        # Veins of flesh spread over the stone.
        flesh = [mat(BLOOD[2], 0.4, BLOOD[1], scale=12), mat(SKIN[1], 0.4, BLOOD[1], scale=12)]
        for face in ("x", "y"):
            for k in range(rng.randint(3, 5)):
                along = rng.uniform(-half + 0.06, half - 0.06)
                z = rng.uniform(0.1, top_at(face, along) * 0.9)
                sphere(rng.uniform(0.03, 0.06), on_face(face, along, 0.02, z), rng.choice(flesh), scale=(1.4, 1.4, 0.8), segments=8)
                length = rng.uniform(0.15, 0.4)
                x, y, _ = on_face(face, along, 0.03, 0)
                box((0.02, 0.02, length) if face == "x" else (0.02, 0.02, length), (x, y, max(0.0, z - length * 0.6)), flesh[0])
    if theme == 2 and variant == 4:
        # Bones and horns set into the wall.
        bone_m = mat(BONE[1], 0.3, BONE[0], scale=12)
        for face in ("x", "y"):
            for k in range(rng.randint(1, 3)):
                along = rng.uniform(-half + 0.1, half - 0.1)
                z = rng.uniform(0.2, top_at(face, along) * 0.85)
                x, y, _ = on_face(face, along, 0.0, 0)
                out = (-1, 0) if face == "x" else (0, -1)
                cone(0.035, 0.16, (x, y, z), bone_m, 6, rot=(0, -70, 180) if face == "x" else (70, 0, 0))
            skull(on_face(face, rng.uniform(-half + 0.1, half - 0.1), 0.04, rng.uniform(0.1, 0.4)), 0.05, bone_m, turn=0 if face == "x" else 90)


def tall_wall(n):
    """wall_5 to wall_16: theme (n - 5) // 4, variant (n - 5) % 4 + 1."""
    _tall_wall((n - 5) // 4, (n - 5) % 4 + 1)


def _demonic_low(variant):
    """wall_low_5 and wall_low_6: the cut-down camera-side wall in blackened stone, the second with a glowing crack."""
    rng = seed(500 + variant)
    half = CELL / 2
    h = 0.125 / 0.866
    stones = [mat(STONE[2], 0.4, STONE[1], scale=16), mat(STONE[1], 0.35, STONE[0], scale=16), mat(BLOOD[1], 0.4, BLOOD[0], scale=16)]
    box((CELL - 0.01, CELL - 0.01, h - 0.02), (0, 0, 0), mat(STONE[0], 0.4, BLOOD[0], scale=8))
    for k in range(9):
        size = rng.uniform(0.07, 0.13)
        rock((rng.uniform(-half + 0.08, half - 0.08), rng.uniform(-half + 0.08, half - 0.08), h - 0.04), size, rng.choice(stones), rng, squash=0.5)
    if variant == 2:
        glow = mat(EMBER[1], 0.0, emit=3.0)
        x = -half + 0.05
        for k in range(5):
            box((0.025, 0.12, 0.012), (x + k * 0.12, rng.uniform(-0.15, 0.15), h - 0.015), glow, rot=(0, 0, rng.uniform(-40, 40)))


def wall(variant=1):
    _rubble_block(0.5 / 0.866, variant)


def wall_low(variant=1):
    _rubble_block(0.125 / 0.866, variant)


# ---------------------------------------------------------------- world pieces: travel and loot

def stairs_down():
    # A square shaft one cell across, framed by a stone kerb, with steps going down into the dark along +x+y.
    stone = mat(STONE[4], 0.3, STONE[3], scale=8)
    edge = mat(STONE[5], 0.3, STONE[4], scale=8)
    dark = mat(STONE[0], 0.0)
    size = CELL * 1.6
    half = size / 2
    rim = 0.12
    box((size + rim * 2, rim, 0.06), (0, -half - rim / 2, 0), edge)
    box((size + rim * 2, rim, 0.06), (0, half + rim / 2, 0), edge)
    box((rim, size, 0.06), (-half - rim / 2, 0, 0), edge)
    box((rim, size, 0.06), (half + rim / 2, 0, 0), edge)
    # The shaft's inside: dark walls below the ground.
    box((size, size, 0.02), (0, 0, -1.2), dark)
    for i, sgn in enumerate((-1, 1)):
        box((size, 0.02, 1.2), (0, sgn * half, -1.2), mat(STONE[3], 0.3, STONE[2]))
        box((0.02, size, 1.2), (sgn * half, 0, -1.2), mat(STONE[2], 0.3, STONE[1]))
    holdout(half + rim, 6.0)
    steps = 7
    for k in range(steps):
        depth = (k + 1) * 0.12
        shade = [STONE[4], STONE[3], STONE[3], STONE[2], STONE[2], STONE[1], STONE[1]][k]
        box((size, size / steps, 0.04), (0, half - (k + 0.5) * size / steps, -depth), mat(shade, 0.25, STONE[max(0, STONE.index(shade) - 1)]))
        box((size, 0.02, 0.12), (0, half - (k + 1) * size / steps, -depth), mat(STONE[max(0, STONE.index(shade) - 1)], 0.0))


def stairs_up():
    # A flight of steps rising toward +y against a wall stub, with low side walls.
    stone = mat(STONE[4], 0.3, STONE[3], scale=8)
    side = mat(STONE[3], 0.3, STONE[2], scale=8)
    width = CELL * 1.4
    steps = 6
    run = CELL * 1.6
    rise = 0.13
    for k in range(steps):
        box((width, run / steps * (steps - k), rise), (0, -run / 2 + run / steps * k + run / steps * (steps - k) / 2, k * rise), stone)
    for sgn in (-1, 1):
        prism([(-run / 2, 0), (run / 2, 0), (run / 2, steps * rise + 0.25), (-run / 2, 0.15)], 0.1, (sgn * (width / 2 + 0.05) - 0.05, 0, 0), side, rot=(90, 0, 90))
    dark = mat(STONE[0], 0.0)
    box((width, 0.08, steps * rise + 0.6), (0, run / 2 + 0.04, 0), side)
    box((width * 0.7, 0.02, 0.5), (0, run / 2 - 0.005, steps * rise), dark)


def chest(open_lid):
    # Long side to the camera: built along x, then turned -45 degrees about z so x runs across the screen.
    wood = mat(WOOD[3], 0.3, WOOD[2], scale=12)
    lidwood = mat(WOOD[4], 0.3, WOOD[3], scale=12)
    iron = mat(STONE[2], 0.15, STONE[1], metal=0.6, rough=0.45)
    gold = mat(EMBER[4], 0.25, EMBER[2], metal=0.8, rough=0.3, emit=0.6, scale=20)
    dark = mat(WOOD[0], 0.0)
    L, W, H = 0.66 * M, 0.42 * M, 0.3 * M
    parts = [box((L, W, H), (0, 0, 0), wood)]
    for t in (-0.36, 0.36):
        parts.append(box((0.05 * M, W + 0.02, H + 0.01), (t * L, 0, 0), iron))
    parts.append(box((L + 0.02, W + 0.02, 0.035 * M), (0, 0, 0), iron))
    if open_lid:
        parts.append(box((L - 0.05, W - 0.05, 0.02), (0, 0, H - 0.03), dark))
        for i in range(8):
            parts.append(sphere(0.05 * M, ((i % 4 - 1.5) * 0.11 * M, (i // 4 - 0.5) * 0.12 * M, H - 0.01), gold, scale=(1, 1, 0.5), segments=8))
        # The lid stands open, hinged at the back edge (+y), leaning back a little past upright.
        lid = box((L, 0.06 * M, W), (0, W / 2 + 0.03 * M, H), lidwood, rot=(-12, 0, 0))
        parts.append(lid)
        for t in (-0.36, 0.36):
            parts.append(box((0.05 * M, 0.07 * M, W), (t * L, W / 2 + 0.03 * M, H), iron, rot=(-12, 0, 0)))
    else:
        parts.append(box((L, W, 0.1 * M), (0, 0, H), lidwood))
        parts.append(box((L * 0.96, W * 0.7, 0.05 * M), (0, 0, H + 0.1 * M), lidwood))
        for t in (-0.36, 0.36):
            parts.append(box((0.05 * M, W + 0.02, 0.15 * M + 0.01), (t * L, 0, H), iron))
        parts.append(box((0.08 * M, 0.03, 0.1 * M), (0, -W / 2 - 0.01, H - 0.05 * M), gold))
    turn = mathutils.Matrix.Rotation(math.radians(-45), 3, "Z")
    for o in parts:
        o.location = turn @ o.location
        o.rotation_euler.z += math.radians(-45)


def chest_closed():
    chest(False)


def chest_open():
    chest(True)


def _dais(radius, glow):
    stone = mat(STONE[4], 0.3, STONE[3], scale=7)
    edge = mat(STONE[3], 0.3, STONE[2], scale=7)
    rune = mat(COLD[4], 0.0, emit=2.5 if glow else 0.6)
    lathe([(radius, 0), (radius, 0.06), (radius * 0.92, 0.1)], (0, 0, 0), edge, 24, smooth=False)
    cyl(radius * 0.92, 0.1, (0, 0, 0), stone, 24, smooth=False)
    # Flagstone seams: dark spokes and a ring.
    dark = mat(STONE[2], 0.0)
    for k in range(8):
        a = math.radians(k * 45 + 22.5)
        box((radius * 0.85, 0.015, 0.005), (math.cos(a) * radius * 0.45, math.sin(a) * radius * 0.45, 0.1), dark, rot=(0, 0, k * 45 + 22.5))
    r1, r0 = radius * 0.62, radius * 0.56
    lathe([(r0, 0.101), (r1, 0.101)], (0, 0, 0), rune, 32, cap=False)
    for k in range(6):
        a = math.radians(k * 60)
        box((0.08, 0.025, 0.006), (math.cos(a) * radius * 0.75, math.sin(a) * radius * 0.75, 0.1), rune, rot=(0, 0, k * 60 + 90))
        box((0.025, 0.06, 0.006), (math.cos(a) * radius * 0.75, math.sin(a) * radius * 0.75, 0.1), rune, rot=(0, 0, k * 60 + 90))


def waypoint():
    _dais(0.75, True)


def waystone():
    _dais(0.85, True)
    stone = mat(STONE[3], 0.3, STONE[2], scale=6)
    rune = mat(COLD[4], 0.0, emit=3.0)
    obelisk = lathe([(0.2, 0), (0.17, 1.5), (0.0, 1.75)], (0, 0, 0.1), stone, 4, smooth=False)
    obelisk.rotation_euler.z = math.radians(45)
    for k in range(4):
        box((0.06, 0.03, 0.06), (-0.12, -0.12, 0.45 + k * 0.28), rune, rot=(0, 0, 45))


# ---------------------------------------------------------------- town

def _timber_house(sx, sy, wall_h, roof_h, plaster, roof_color, seed_n, chimney=False, stone_base=True):
    rng = seed(seed_n)
    wallm = mat(plaster, 0.2, None, scale=5)
    beam = mat(WOOD[1], 0.2, WOOD[0], scale=10)
    stone = mat(STONE[4], 0.3, STONE[3], scale=7)
    roofm = mat(roof_color[0], 0.45, roof_color[1], scale=18)
    win = mat(EMBER[3], 0.2, EMBER[1], emit=1.2)
    door = mat(WOOD[2], 0.3, WOOD[1], scale=14)
    box((sx, sy, wall_h), (0, 0, 0), wallm)
    if stone_base:
        box((sx + 0.04, sy + 0.04, 0.35), (0, 0, 0), stone)
    t = 0.09
    # Corner posts and a top beam on the two front faces, and braces.
    for x, y in ((-sx / 2, -sy / 2), (-sx / 2, sy / 2), (sx / 2, -sy / 2)):
        box((t, t, wall_h), (x, y, 0), beam)
    box((sx + 0.02, t, t), (0, -sy / 2, wall_h - t), beam)
    box((t, sy + 0.02, t), (-sx / 2, 0, wall_h - t), beam)
    box((sx + 0.02, t, t), (0, -sy / 2, wall_h * 0.5), beam)
    box((t, sy + 0.02, t), (-sx / 2, 0, wall_h * 0.5), beam)
    for k in range(1, 3):
        box((t, t, wall_h * 0.5), (-sx / 2 + sx * k / 3, -sy / 2, wall_h * 0.5), beam)
        box((t, t, wall_h * 0.5), (-sx / 2, -sy / 2 + sy * k / 3, wall_h * 0.5), beam)
    # A door on the right front face (-y) and windows.
    box((0.55, 0.06, 1.05), (sx * 0.2, -sy / 2 - 0.02, 0.0), door)
    box((0.7, 0.07, 0.08), (sx * 0.2, -sy / 2 - 0.03, 1.05), beam)
    for x in (-sx * 0.25,):
        box((0.36, 0.05, 0.36), (x, -sy / 2 - 0.02, wall_h * 0.62), win)
        box((0.44, 0.07, 0.06), (x, -sy / 2 - 0.03, wall_h * 0.62 - 0.05), beam)
    for y in (-sy * 0.2, sy * 0.22):
        box((0.05, 0.36, 0.36), (-sx / 2 - 0.02, y, wall_h * 0.62), win)
        box((0.07, 0.44, 0.06), (-sx / 2 - 0.03, y, wall_h * 0.62 - 0.05), beam)
    for y in (-sy * 0.2, sy * 0.22):
        box((0.05, 0.3, 0.3), (-sx / 2 - 0.02, y, wall_h * 0.18), win)
    # A gable roof, ridge along x, overhanging.
    o = 0.22
    hx, hy = sx / 2 + o, sy / 2 + o
    verts = [(-hx, -hy, wall_h - 0.05), (hx, -hy, wall_h - 0.05), (hx, 0, wall_h + roof_h), (-hx, 0, wall_h + roof_h),
             (-hx, hy, wall_h - 0.05), (hx, hy, wall_h - 0.05)]
    poly(verts, [(0, 1, 2, 3), (3, 2, 5, 4)], roofm)
    # Thick eaves: a slab under each roof face, so the roof has an edge.
    poly([(-hx, -hy, wall_h - 0.05), (hx, -hy, wall_h - 0.05), (hx, -hy, wall_h - 0.17), (-hx, -hy, wall_h - 0.17)], [(0, 1, 2, 3)], mat(roof_color[1], 0.0))
    # Gable ends.
    gable = mat(plaster, 0.2, None, scale=5)
    for x in (-sx / 2, sx / 2):
        poly([(x, -sy / 2, wall_h - 0.01), (x, sy / 2, wall_h - 0.01), (x, 0, wall_h + roof_h - 0.1)], [(0, 1, 2)], gable)
    box((0.06, sy, 0.08), (-sx / 2 - 0.02, 0, wall_h), beam)
    box((0.06, 0.08, roof_h * 0.9), (-sx / 2 - 0.03, 0, wall_h), beam)
    # Rows of shingles or thatch ridges across the roof.
    ridge = mat(roof_color[1], 0.3, None, scale=10)
    rows = int(roof_h / 0.18)
    for k in range(1, rows):
        f = k / rows
        z = wall_h - 0.05 + roof_h * f
        y = -hy * (1 - f)
        box((2 * hx, 0.04, 0.035), (0, y - 0.01, z - 0.02), ridge)
    box((2 * hx + 0.05, 0.16, 0.12), (0, 0, wall_h + roof_h - 0.08), ridge)
    if chimney:
        box((0.4, 0.4, roof_h + 0.5), (sx * 0.3, sy * 0.15, wall_h), stone)
        cyl(0.08, 0.02, (sx * 0.3, sy * 0.15, wall_h + roof_h + 0.5), mat(STONE[0], 0.0), 8)


def house_1():
    _timber_house(4.2, 3.0, 2.4, 1.7, BONE[1], (WOOD[5], WOOD[3]), 1, chimney=True)
    barrel((-2.35, -0.9, 0), 0.9)
    barrel((-2.35, -1.35, 0), 0.9)


def house_2():
    _timber_house(3.2, 3.6, 2.2, 1.5, SKIN[3], (COLD[2], COLD[1]), 2, chimney=False)
    crate((-1.9, 1.0, 0), 0.55)
    crate((-1.95, 1.0, 0.55 * M), 0.45, turn=20)


def house_3():
    # A long stone hall with a dark slate roof and a lean-to.
    _timber_house(5.0, 3.2, 2.0, 1.6, STONE[4], (STONE[2], STONE[1]), 3, chimney=True, stone_base=True)
    wood = mat(WOOD[2], 0.3, WOOD[1], scale=10)
    roofm = mat(WOOD[3], 0.45, WOOD[2], scale=18)
    for x in (-2.0, -0.6):
        box((0.1, 0.1, 1.4), (x, -2.3, 0), wood)
    poly([(-2.3, -1.6, 1.85), (-0.3, -1.6, 1.85), (-0.3, -2.45, 1.4), (-2.3, -2.45, 1.4)], [(0, 1, 2, 3)], roofm)
    for i in range(3):
        crate((-1.6 + i * 0.5, -2.0, 0), 0.4, turn=i * 15)


def tent(stripes, seed_n):
    rng = seed(seed_n)
    cloth = mat(stripes[0], 0.15, None, scale=6)
    cloth2 = mat(stripes[1], 0.15, None, scale=6)
    pole = mat(WOOD[2], 0.2, WOOD[1])
    dark = mat(STONE[0], 0.0)
    sx, sy, h = 2.6, 2.0, 1.9
    # An A-frame tent, ridge along x, its open end facing -x (the left front).
    hx, hy = sx / 2, sy / 2
    verts = [(-hx, -hy, 0), (hx, -hy, 0), (hx, 0, h), (-hx, 0, h), (-hx, hy, 0), (hx, hy, 0)]
    poly(verts, [(0, 1, 2, 3), (3, 2, 5, 4)], cloth)
    for k in range(1, 6, 2):
        f0, f1 = k / 6, (k + 1) / 6
        x0, x1 = -hx + sx * f0, -hx + sx * f1
        poly([(x0, -hy - 0.005, 0), (x1, -hy - 0.005, 0), (x1, -0.005, h), (x0, -0.005, h)], [(0, 1, 2, 3)], cloth2)
    poly([(hx, -hy, 0), (hx, hy, 0), (hx, 0, h)], [(0, 1, 2)], cloth)
    # The open end: dark inside, flaps pulled back.
    poly([(-hx + 0.3, -hy, 0), (-hx + 0.3, hy, 0), (-hx + 0.3, 0, h)], [(0, 1, 2)], dark)
    poly([(-hx, -hy, 0), (-hx, -hy * 0.5, 0), (-hx, 0, h)], [(0, 1, 2)], cloth2)
    poly([(-hx, hy * 0.5, 0), (-hx, hy, 0), (-hx, 0, h)], [(0, 1, 2)], cloth2)
    for x in (-hx - 0.05, hx + 0.05):
        cyl(0.04, h + 0.25, (x, 0, 0), pole, 6)


def tent_1():
    tent((BONE[1], BONE[0]), 1)


def tent_2():
    tent((BLOOD[3], BONE[1]), 2)
    crate((-1.8, -1.25, 0), 0.45)
    barrel((-1.8, -0.6, 0), 0.8)


def campfire():
    rng = seed(9)
    stones = [mat(STONE[3], 0.35, STONE[2]), mat(STONE[4], 0.35, STONE[3])]
    for k in range(10):
        a = 2 * math.pi * k / 10
        rock((0.42 * math.cos(a), 0.42 * math.sin(a), 0.04), 0.11, rng.choice(stones), rng, 0.7)
    log = mat(WOOD[2], 0.3, WOOD[1], scale=12)
    char = mat(STONE[0], 0.3, WOOD[0])
    for k in range(5):
        a = 2 * math.pi * k / 5 + 0.3
        cyl(0.06, 0.6, (0.28 * math.cos(a), 0.28 * math.sin(a), 0.03), log, 6, rot=(0, -62, math.degrees(a)))
    sphere(0.28, (0, 0, 0.0), mat(EMBER[1], 0.5, EMBER[0], emit=2.0, scale=20), scale=(1, 1, 0.3), segments=10)
    flame((0, 0, 0.05), 0.16, 7)
    flame((0.08, 0.06, 0.05), 0.11, 7)
    flame((-0.1, 0.04, 0.05), 0.1, 7)
    # A spit over it.
    iron = mat(STONE[1], 0.0, metal=0.6, rough=0.5)
    for y in (-0.55, 0.55):
        box((0.04, 0.04, 0.85), (0, y, 0), iron)
    cyl(0.025, 1.2, (0, -0.6, 0.82), iron, 6, rot=(-90, 0, 0))
    lathe([(0.0, -0.17), (0.17, -0.12), (0.2, 0.0), (0.17, 0.1), (0.1, 0.16)], (0, 0.1, 0.6), mat(STONE[1], 0.2, STONE[0], metal=0.5), 10)


def well():
    stone = mat(STONE[4], 0.35, STONE[3], scale=7)
    wood = mat(WOOD[3], 0.3, WOOD[2], scale=12)
    roofm = mat(WOOD[4], 0.45, WOOD[3], scale=18)
    water = mat(COLD[1], 0.0, rough=0.1)
    lathe([(0.7, 0), (0.7, 0.75), (0.5, 0.75), (0.5, 0.0)], (0, 0, 0), stone, 16, smooth=False, cap=False)
    cyl(0.52, 0.5, (0, 0, 0.0), water, 16)
    lathe([(0.72, 0.72), (0.72, 0.82), (0.48, 0.82), (0.48, 0.72)], (0, 0, 0), mat(STONE[5], 0.3, STONE[4], scale=7), 16, smooth=False)
    for y in (-0.6, 0.6):
        box((0.12, 0.12, 2.0), (0, y, 0.6), wood)
    cyl(0.06, 1.35, (0, -0.68, 1.6), wood, 8, rot=(-90, 0, 0))
    cyl(0.01, 0.6, (0, 0, 1.0), mat(BONE[0], 0.0), 4)
    bucket_m = mat(WOOD[4], 0.3, WOOD[3])
    lathe([(0.12, 0), (0.15, 0.22)], (0, 0, 0.82), bucket_m, 10, smooth=False)
    verts = [(-0.75, -0.85, 2.3), (0.75, -0.85, 2.3), (0.75, 0, 2.85), (-0.75, 0, 2.85), (-0.75, 0.85, 2.3), (0.75, 0.85, 2.3)]
    poly([(x, y * 1.0, z) for x, y, z in verts], [(0, 1, 2, 3), (3, 2, 5, 4)], roofm)
    for x in (-0.75, 0.75):
        poly([(x, -0.6, 2.4), (x, 0.6, 2.4), (x, 0, 2.8)], [(0, 1, 2)], wood)


def market_stall():
    wood = mat(WOOD[3], 0.3, WOOD[2], scale=12)
    awning = mat(BLOOD[3], 0.15, BLOOD[2], scale=5)
    awning2 = mat(BONE[1], 0.15, BONE[0], scale=5)
    sx, sy = 2.4, 1.2
    for x in (-sx / 2, sx / 2):
        for y in (-sy / 2, sy / 2):
            box((0.09, 0.09, 2.0 if y > 0 else 1.75), (x, y, 0), wood)
    box((sx, sy * 0.8, 0.12), (0, 0, 0.8), wood)
    box((sx - 0.1, 0.06, 0.8), (0, -sy * 0.4, 0), wood)
    # Striped, sloping awning.
    stripes = 8
    for k in range(stripes):
        x0 = -sx / 2 - 0.15 + (sx + 0.3) * k / stripes
        x1 = -sx / 2 - 0.15 + (sx + 0.3) * (k + 1) / stripes
        poly([(x0, -sy / 2 - 0.35, 1.7), (x1, -sy / 2 - 0.35, 1.7), (x1, sy / 2 + 0.1, 2.05), (x0, sy / 2 + 0.1, 2.05)], [(0, 1, 2, 3)], awning if k % 2 else awning2)
    # Wares: pots, a sack, fruit.
    rng = seed(12)
    clay = mat(SKIN[1], 0.3, SKIN[0])
    for k in range(4):
        lathe([(0.08, 0), (0.12, 0.1), (0.06, 0.2)], (-0.9 + k * 0.35, -0.1, 0.92), clay, 10)
    for k in range(6):
        sphere(0.07, (0.4 + (k % 3) * 0.15, -0.15 + (k // 3) * 0.15, 0.98), mat([EMBER[3], MOSS[3], BLOOD[3]][k % 3], 0.0), segments=8)
    sphere(0.3, (-1.5, -0.6, 0.25), mat(WOOD[4], 0.3, WOOD[3]), scale=(1, 1, 1.1), segments=10)


def cart():
    wood = mat(WOOD[3], 0.3, WOOD[2], scale=12)
    dark = mat(WOOD[1], 0.2, WOOD[0])
    iron = mat(STONE[1], 0.0, metal=0.5)
    box((2.0, 1.1, 0.12), (0, 0, 0.6), wood)
    for y in (-0.55, 0.55):
        box((2.0, 0.06, 0.4), (0, y, 0.72), wood)
    box((0.06, 1.1, 0.4), (1.0, 0, 0.72), wood)
    for y in (-0.62, 0.62):
        cyl(0.48, 0.08, (0.15, y - 0.04, 0.48), dark, 12, rot=(90, 0, 0), smooth=False)
        cyl(0.43, 0.09, (0.15, y - 0.045, 0.48), wood, 12, rot=(90, 0, 0), smooth=False)
        cyl(0.08, 0.12, (0.15, y - 0.06, 0.48), iron, 8, rot=(90, 0, 0))
    for y in (-0.3, 0.3):
        box((1.3, 0.07, 0.07), (-1.6, y, 0.55), wood, rot=(0, 12, 0))
    sphere(0.3, (0.3, 0, 0.85), mat(BONE[0], 0.3, WOOD[4]), scale=(1.3, 1, 0.7), segments=10)
    sphere(0.25, (-0.4, 0.1, 0.8), mat(MOSS[3], 0.3, MOSS[2]), scale=(1, 1, 0.6), segments=10)
    barrel((0.6, -0.2, 0.7), 0.7)


def fence(axis):
    wood = mat(WOOD[3], 0.3, WOOD[2], scale=12)
    length = CELL * 4
    for k in range(5):
        t = -length / 2 + length * k / 4
        x, y = (t, 0) if axis == "x" else (0, t)
        box((0.1, 0.1, 0.95), (x, y, 0), wood)
    for z in (0.35, 0.75):
        size = (length + 0.1, 0.06, 0.1) if axis == "x" else (0.06, length + 0.1, 0.1)
        box(size, (0, 0, z), wood)


def fence_x():
    fence("x")


def fence_y():
    fence("y")


def hay_bale():
    hay = mat(EMBER[3], 0.45, WOOD[4], scale=25)
    tie = mat(WOOD[2], 0.0)
    box((0.9, 0.55, 0.5), (0, 0, 0), hay, rot=(0, 0, 15))
    box((0.8, 0.5, 0.45), (0.05, 0.6, 0.0), hay, rot=(0, 0, -10))
    box((0.85, 0.5, 0.45), (0.05, 0.3, 0.5), hay, rot=(0, 0, 5))


def crate_stack():
    crate((0, 0, 0), 0.55)
    crate((0.75, 0.0, 0), 0.5, turn=12)
    crate((0.05, 0.05, 0.55 * M), 0.45, turn=-15)
    barrel((-0.05, 0.8, 0), 1.0)
    barrel((0.7, 0.8, 0), 1.0)


def lamp_post():
    iron = mat(STONE[1], 0.2, STONE[0], metal=0.6, rough=0.5)
    glass = mat(EMBER[4], 0.0, emit=4.0)
    box((0.25, 0.25, 0.15), (0, 0, 0), mat(STONE[3], 0.3, STONE[2]))
    cyl(0.05, 2.6, (0, 0, 0.1), iron, 8)
    box((0.5, 0.05, 0.05), (-0.22, 0, 2.55), iron)
    box((0.22, 0.22, 0.3), (-0.42, 0, 2.2), glass)
    lathe([(0.17, 0), (0.0, 0.14)], (-0.42, 0, 2.5), iron, 4, smooth=False)


def signpost():
    wood = mat(WOOD[3], 0.3, WOOD[2], scale=12)
    sign = mat(WOOD[4], 0.3, WOOD[3], scale=12)
    box((0.12, 0.12, 2.0), (0, 0, 0), wood)
    box((0.9, 0.05, 0.28), (-0.35, -0.08, 1.55), sign, rot=(0, 0, -10))
    box((0.05, 0.8, 0.25), (-0.08, -0.3, 1.15), sign, rot=(0, 0, 8))


def dead_tree():
    rng = seed(31)
    bark = mat(WOOD[1], 0.35, WOOD[0], scale=10)

    def branch(start, direction, length, radius, depth):
        x, y, z = start
        dx, dy, dz = direction
        n = math.sqrt(dx * dx + dy * dy + dz * dz)
        dx, dy, dz = dx / n, dy / n, dz / n
        # A tapering cylinder from start toward direction.
        yaw = math.degrees(math.atan2(dy, dx))
        pitch = math.degrees(math.acos(max(-1, min(1, dz))))
        lathe([(radius, 0), (radius * 0.6, length)], (x, y, z), bark, 6, rot=(0, pitch, yaw), smooth=False)
        end = (x + dx * length, y + dy * length, z + dz * length)
        if depth == 0:
            return
        for k in range(2 + (depth > 1)):
            a = rng.random() * 6.28
            nd = (dx + math.cos(a) * 0.9, dy + math.sin(a) * 0.9, dz + 0.3)
            branch(end, nd, length * (0.55 + rng.random() * 0.2), radius * 0.6, depth - 1)

    branch((0, 0, 0), (0.05, 0.05, 1), 2.0, 0.2, 3)
    for k in range(4):
        a = k * 1.6
        lathe([(0.12, 0), (0.02, 0.6)], (0, 0, 0.05), bark, 5, rot=(0, 80, math.degrees(a)), smooth=False)


def pine():
    rng = seed(41)
    bark = mat(WOOD[1], 0.3, WOOD[0])
    needles = [mat(MOSS[1], 0.4, MOSS[0], scale=14), mat(MOSS[2], 0.4, MOSS[1], scale=14)]
    cyl(0.14, 1.0, (0, 0, 0), bark, 6, smooth=False)
    for k in range(5):
        z = 0.6 + k * 0.62
        r = 1.15 - k * 0.2
        cone(r, 1.0, (0, 0, z), needles[k % 2], 9, smooth=False)


def banner():
    wood = mat(WOOD[2], 0.3, WOOD[1])
    cloth = mat(BLOOD[3], 0.2, BLOOD[2], scale=6)
    trim = mat(EMBER[3], 0.0)
    cyl(0.05, 3.0, (0, 0, 0), wood, 6)
    box((0.06, 0.8, 0.06), (0, -0.35, 2.85), wood)
    poly([(0, -0.7, 2.82), (0, 0.0, 2.82), (0, 0.0, 1.6), (0, -0.35, 1.85), (0, -0.7, 1.6)], [(0, 1, 2, 3, 4)], cloth, loc=(-0.04, 0, 0))
    box((0.02, 0.72, 0.06), (-0.05, -0.35, 2.7), trim)


def anvil_bench():
    iron = mat(STONE[1], 0.2, STONE[0], metal=0.7, rough=0.4)
    wood = mat(WOOD[2], 0.3, WOOD[1])
    cyl(0.3, 0.5, (0, 0, 0), wood, 8, smooth=False)
    box((0.6, 0.25, 0.25), (0, 0, 0.5), iron)
    lathe([(0.12, 0), (0.0, 0.35)], (0.3, 0, 0.62), iron, 6, rot=(0, 90, 0), smooth=False)


def woodpile():
    log = mat(WOOD[3], 0.3, WOOD[2], scale=12)
    end = mat(WOOD[4], 0.2, WOOD[3])
    for row in range(3):
        for k in range(5 - row):
            y = -0.6 + k * 0.28 + row * 0.14
            cyl(0.14, 1.1, (-0.55, y, 0.14 + row * 0.24), log, 8, rot=(0, 90, 0), smooth=False)
            cyl(0.12, 0.02, (-0.56, y, 0.14 + row * 0.24), end, 8, rot=(0, 90, 0), smooth=False)
    box((0.9, 0.5, 0.06), (0.6, 0.2, 0), log, rot=(0, 0, 20))


def pen_trough():
    wood = mat(WOOD[3], 0.3, WOOD[2], scale=12)
    water = mat(COLD[2], 0.0, rough=0.1)
    box((1.4, 0.5, 0.4), (0, 0, 0), wood)
    box((1.3, 0.4, 0.02), (0, 0, 0.36), water)


def gravestone(seed_n):
    rng = seed(seed_n)
    stone = mat(STONE[3], 0.35, STONE[2], scale=7)
    for k in range(3):
        x, y = (k - 1) * 0.75, (rng.random() - 0.5) * 0.3
        h = 0.6 + rng.random() * 0.35
        slab = box((0.45, 0.14, h), (x, y, 0), stone, rot=(rng.random() * 10 - 5, 0, rng.random() * 14 - 7))
        sphere(0.225, (x, y, h), stone, scale=(1, 0.31, 0.5), segments=8)
    bone((0.2, -0.5, 0), 0.25, 30, mat(BONE[1], 0.3, BONE[0]), 0.02)


def graves():
    gravestone(51)


def ritual_circle():
    # A summoning circle three cells across, painted in blood, with candles at the star's points and a skull in the
    # middle: placed in some dungeon rooms on the floor (drawn under characters).
    red = mat(BLOOD[3], 0.0, rough=0.3, emit=1.0)
    dark = mat(BLOOD[1], 0.0, rough=0.3, emit=0.8)
    R = 1.0

    def ring(radius, width, material):
        steps = 48
        for i in range(steps):
            a0 = 2 * math.pi * i / steps
            a1 = 2 * math.pi * (i + 1) / steps
            flat([(radius * math.cos(a0), radius * math.sin(a0)), (radius * math.cos(a1), radius * math.sin(a1)),
                  ((radius - width) * math.cos(a1), (radius - width) * math.sin(a1)), ((radius - width) * math.cos(a0), (radius - width) * math.sin(a0))], material)

    ring(R, 0.06, red)
    ring(R * 0.84, 0.035, dark)
    star = [(R * 0.82 * math.cos(math.radians(90 + 144 * k)), R * 0.82 * math.sin(math.radians(90 + 144 * k))) for k in range(6)]
    for (x0, y0), (x1, y1) in zip(star, star[1:]):
        dx, dy = x1 - x0, y1 - y0
        l = math.hypot(dx, dy)
        nx, ny = -dy / l * 0.025, dx / l * 0.025
        flat([(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)], red, 0.003)
    # Runes between the rings.
    for k in range(12):
        a = 2 * math.pi * k / 12 + 0.13
        x, y = math.cos(a) * R * 0.92, math.sin(a) * R * 0.92
        box((0.08, 0.02, 0.004), (x, y, 0), dark, rot=(0, 0, math.degrees(a) + (45 if k % 2 else -20)))
    rng = seed(77)
    for k in range(5):
        x, y = star[k]
        candles((x * 1.17, y * 1.17, 0), 3, 80 + k, 0.08)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    skull((0, 0, 0), 0.09 * M, bonec, turn=0)
    _blood_pool = mat(BLOOD[2], 0.0, rough=0.25, emit=0.8)
    blob(0.05, -0.1, 0.22, _blood_pool, 12, 0.5, rng)


# ---------------------------------------------------------------- floors (one cell, seen flat; the importer cuts the diamond)

def _flat_light():
    # Floors are lit by the game; here only an even light with a touch of sun for the stones' relief.
    for o in bpy.data.objects:
        if o.type == "LIGHT":
            o.data.energy = 1.0 if o.name == "Key" else 0.0
    bpy.context.scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 2.6


def _slab(x0, y0, x1, y1, material, rng, grout=0.026, height=0.02):
    gx, gy = x1 - x0 - grout, y1 - y0 - grout
    box((gx, gy, height + rng.random() * 0.006), ((x0 + x1) / 2, (y0 + y1) / 2, 0), material, bevel=0.004)


def _crack(rng, material, n=1):
    h = CELL / 2
    for k in range(n):
        x, y = (rng.random() - 0.5) * CELL * 0.7, (rng.random() - 0.5) * CELL * 0.7
        a = rng.random() * 6.28
        for s in range(5):
            a += (rng.random() - 0.5) * 1.2
            l = 0.04 + rng.random() * 0.03
            box((l, 0.008, 0.002), (x + math.cos(a) * l / 2, y + math.sin(a) * l / 2, 0.026), material, rot=(0, 0, math.degrees(a)))
            x, y = x + math.cos(a) * l, y + math.sin(a) * l


def _ground(material):
    # Larger than the cell, so the diamond's edge pixels are fully covered (a part-covered edge reads as a dark seam).
    box((CELL * 1.3, CELL * 1.3, 0.01), (0, 0, -0.005), material)


def _flagstone(n, mossy=False):
    _flat_light()
    rng = seed(600 + n + (50 if mossy else 0))
    h = CELL / 2
    _ground(mat(STONE[2], 0.0))
    base = STONE[4] if not mossy else STONE[3]
    dark = STONE[3] if not mossy else STONE[2]

    def pick():
        # One shade per style, mottled only a step darker: two shades side by side read as a checkerboard.
        return mat(base, 0.44 + rng.random() * 0.06, dark, scale=9 + rng.random() * 5)

    split = n % 3
    if split == 0:
        _slab(-h, -h, h, h, pick(), rng)
    elif split == 1:
        cut = -h + CELL * (0.35 + rng.random() * 0.3)
        if n % 2:
            _slab(-h, -h, cut, h, pick(), rng)
            _slab(cut, -h, h, h, pick(), rng)
        else:
            _slab(-h, -h, h, cut, pick(), rng)
            _slab(-h, cut, h, h, pick(), rng)
    else:
        cx = -h + CELL * (0.4 + rng.random() * 0.2)
        cy = -h + CELL * (0.4 + rng.random() * 0.2)
        _slab(-h, -h, cx, cy, pick(), rng)
        _slab(cx, -h, h, cy, pick(), rng)
        _slab(-h, cy, h, h, pick(), rng)
    if n in (2, 5):
        _crack(rng, mat(STONE[1], 0.0), 1)
    if mossy:
        green = [mat(MOSS[2], 0.2, MOSS[1], scale=30), mat(MOSS[3], 0.2, MOSS[2], scale=30)]
        for k in range(2 + n % 3):
            blob((rng.random() - 0.5) * CELL * 0.8, (rng.random() - 0.5) * CELL * 0.8, 0.05 + rng.random() * 0.07, rng.choice(green), 8, 0.6, rng, z=0.03)


def _brick(n):
    _flat_light()
    rng = seed(700 + n)
    h = CELL / 2
    _ground(mat(STONE[1], 0.0))
    colors = [SKIN[1]]
    rows = 3
    rh = CELL / rows
    for r in range(rows):
        y0 = -h + r * rh
        offset = (r % 2) * 0.5
        for k in range(-1, 3):
            x0 = -h + (k + offset) * h
            x1 = x0 + h
            x0c, x1c = max(x0, -h), min(x1, h)
            if x1c - x0c < 0.02:
                continue
            _slab(x0c, y0, x1c, y0 + rh, mat(rng.choice(colors), 0.44 + rng.random() * 0.06, SKIN[0], scale=14), rng, grout=0.03, height=0.018)
    if n in (2, 5):
        _crack(rng, mat(STONE[0], 0.0), 1)


def _earth(n):
    _flat_light()
    rng = seed(800 + n)
    dirt = mat(WOOD[3], 0.47, WOOD[2], scale=6 + n)
    _ground(dirt)
    stones = [mat(STONE[4], 0.3, STONE[3]), mat(STONE[3], 0.2, STONE[2]), mat(WOOD[4], 0.3, WOOD[3])]
    for k in range(1 + n % 3):
        rock(((rng.random() - 0.5) * CELL * 0.8, (rng.random() - 0.5) * CELL * 0.8, 0.0), (0.015 + rng.random() * 0.02) * M, rng.choice(stones), rng, 0.4)
    if n % 2 == 0:
        blob((rng.random() - 0.5) * CELL * 0.5, (rng.random() - 0.5) * CELL * 0.5, 0.07 + rng.random() * 0.05, mat(WOOD[2], 0.3, WOOD[1], scale=20), 10, 0.6, rng, z=0.003)


# ---------------------------------------------------------------- wall details (on a wall block, seen from the room)

WALL_H = 0.5 / 0.866


def _wall_mask():
    """The wall block itself as a holdout: hides whatever lies inside or behind it."""
    m = bpy.data.materials.new("HoldoutWall")
    m.use_nodes = True
    nodes = m.node_tree.nodes
    for node in list(nodes):
        nodes.remove(node)
    out = nodes.new("ShaderNodeOutputMaterial")
    hold = nodes.new("ShaderNodeHoldout")
    m.node_tree.links.new(hold.outputs["Holdout"], out.inputs["Surface"])
    box((CELL, CELL, WALL_H), (0, 0, 0), m)


def _on_face(face, along, out, z):
    """A point on a wall face: face "x" is the face toward -x (the left front), "y" toward -y (the right front)."""
    h = CELL / 2
    return (-h - out, along, z) if face == "x" else (along, -h - out, z)


def _wall_torch(face):
    _wall_mask()
    iron = mat(STONE[1], 0.2, STONE[0], metal=0.6, rough=0.5)
    wood = mat(WOOD[2], 0.3, WOOD[1])
    x, y, z = _on_face(face, 0.0, 0.02, WALL_H * 0.55)
    box((0.08, 0.08, 0.12) if face == "x" else (0.08, 0.08, 0.12), (x, y, z - 0.06), iron)
    tilt = (0, -25, 0) if face == "x" else (25, 0, 0)
    tx, ty = _on_face(face, 0.0, 0.1, 0)[:2]
    cyl(0.035, 0.28, (tx, ty, z - 0.02), wood, 8, rot=tilt)
    lathe([(0.03, 0), (0.07, 0.08), (0.075, 0.1)], (tx, ty, z + 0.2), iron, 8, smooth=False, cap=False)
    fx, fy = _on_face(face, 0.0, 0.14, 0)[:2]
    flame((fx, fy, z + 0.24), 0.09, 8)


def _wall_chains(face):
    _wall_mask()
    iron = mat(STONE[4], 0.2, STONE[3], metal=0.4, rough=0.5)
    for along in (-0.12, 0.12):
        for k in range(7):
            z = WALL_H - 0.04 - k * 0.055
            x, y, _ = _on_face(face, along + (0.01 if k % 2 else -0.01), 0.015, 0)
            size = (0.035, 0.07, 0.06) if (face == "x") == (k % 2 == 0) else (0.07, 0.035, 0.06)
            box(size, (x, y, z - 0.05), iron)
    x, y, _ = _on_face(face, 0.0, 0.02, 0)
    box((0.04, 0.04, 0.03) if face == "x" else (0.04, 0.04, 0.03), (x, y, WALL_H - 0.43), iron)
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    sx, sy, _ = _on_face(face, 0.0, 0.07, 0)
    skull((sx, sy, 0.0), 0.06 * M, bonec, turn=0 if face == "x" else -90)


def _wall_banner(face, colors):
    _wall_mask()
    cloth = mat(colors[0], 0.15, colors[1], scale=6)
    trim = mat(EMBER[3], 0.0)
    rod = mat(WOOD[1], 0.0)
    z0, z1 = WALL_H - 0.02, WALL_H - 0.5
    pts = [(-0.14, z0), (0.14, z0), (0.14, z1 + 0.06), (0.0, z1), (-0.14, z1 + 0.06)]
    if face == "x":
        poly([(-CELL / 2 - 0.012, a, z) for a, z in pts], [list(range(5))], cloth)
        box((0.02, 0.36, 0.03), (-CELL / 2 - 0.02, 0, z0 - 0.01), rod)
        box((0.01, 0.2, 0.025), (-CELL / 2 - 0.018, 0, z0 - 0.12), trim)
    else:
        poly([(a, -CELL / 2 - 0.012, z) for a, z in pts], [list(range(5))], cloth)
        box((0.36, 0.02, 0.03), (0, -CELL / 2 - 0.02, z0 - 0.01), rod)
        box((0.2, 0.01, 0.025), (0, -CELL / 2 - 0.018, z0 - 0.12), trim)


def _wall_top_candles():
    _wall_mask()
    candles((0.0, 0.0, WALL_H), 5, 91, 0.15)


def _wall_top_skulls():
    _wall_mask()
    bonec = mat(BONE[1], 0.3, BONE[0], scale=15)
    skull((-0.08, 0.06, WALL_H), 0.065 * M, bonec, turn=0)
    skull((0.1, -0.06, WALL_H), 0.06 * M, bonec, turn=-40)
    skull((0.0, 0.12, WALL_H + 0.02), 0.055 * M, bonec, turn=20)
    candles((-0.12, -0.12, WALL_H), 1, 92, 0.02)


def _wall_web():
    # A cobweb strung over the wall's front corner: threads fanning from the corner and rings across them.
    _wall_mask()
    silk = mat(BONE[2], 0.0, emit=0.4)
    h = CELL / 2
    corner = (-h - 0.01, -h - 0.01, WALL_H)
    ends = [(-h - 0.01, -h + 0.32, WALL_H), (-h - 0.01, -h - 0.01, WALL_H - 0.4), (-h + 0.32, -h - 0.01, WALL_H),
            (-h - 0.01, -h + 0.22, WALL_H - 0.25), (-h + 0.22, -h - 0.01, WALL_H - 0.25)]

    def thread(a, b, w=0.014):
        ax, ay, az = a
        bx, by, bz = b
        dx, dy, dz = bx - ax, by - ay, bz - az
        n = math.sqrt(dx * dx + dy * dy + dz * dz)
        pitch = math.degrees(math.acos(max(-1, min(1, dz / n))))
        yaw = math.degrees(math.atan2(dy, dx))
        cyl(w, n, a, silk, 4, rot=(0, pitch, yaw), smooth=False)

    for e in ends:
        thread(corner, e)
    for f in (0.35, 0.65, 0.9):
        pts = [tuple(corner[i] + (e[i] - corner[i]) * f for i in range(3)) for e in (ends[0], ends[3], ends[1], ends[4], ends[2])]
        for a, b in zip(pts, pts[1:]):
            thread(a, b, 0.011)


ALL = {
    "ritual_circle": ("world", ritual_circle, WORLD),
    "prop_barrel": ("dungeon", barrel, PROP),
    "prop_crate": ("dungeon", crate, PROP),
    "prop_urn": ("dungeon", urn, PROP),
    "prop_bone_pile": ("dungeon", bone_pile, PROP),
    "prop_rubble": ("dungeon", rubble_pile, PROP),
    "prop_broken_column": ("dungeon", broken_column, PROP),
    "prop_sarcophagus": ("dungeon", sarcophagus, PROP),
    "prop_brazier": ("dungeon", brazier, PROP),
    "prop_candles": ("dungeon", candle_cluster, PROP),
    "prop_bucket": ("dungeon", bucket, PROP),
    "prop_torch": ("dungeon", torch_stand, TALL),
    "stairs_down": ("world", stairs_down, WORLD),
    "stairs_up": ("world", stairs_up, WORLD),
    "chest_closed": ("world", chest_closed, WORLD),
    "chest_open": ("world", chest_open, WORLD),
    "waypoint": ("world", waypoint, WORLD),
    "waystone": ("world", waystone, WORLD),
    "house_1": ("world", house_1, WORLD),
    "house_2": ("world", house_2, WORLD),
    "house_3": ("world", house_3, WORLD),
    "tent_1": ("world", tent_1, WORLD),
    "tent_2": ("world", tent_2, WORLD),
    "campfire": ("world", campfire, WORLD),
    "well": ("world", well, WORLD),
    "market_stall": ("world", market_stall, WORLD),
    "cart": ("world", cart, WORLD),
    "fence_x": ("world", fence_x, WORLD),
    "fence_y": ("world", fence_y, WORLD),
    "hay_bale": ("world", hay_bale, WORLD),
    "crate_stack": ("world", crate_stack, WORLD),
    "lamp_post": ("world", lamp_post, WORLD),
    "signpost": ("world", signpost, WORLD),
    "dead_tree": ("world", dead_tree, WORLD),
    "pine": ("world", pine, WORLD),
    "banner": ("world", banner, WORLD),
    "woodpile": ("world", woodpile, WORLD),
    "trough": ("world", pen_trough, WORLD),
    "graves": ("world", graves, WORLD),
    "vigil_fire_unlit": ("world", vigil_fire_unlit, WORLD),
    "vigil_fire_lit": ("world", vigil_fire_lit, WORLD),
    "quest_lectern": ("world", quest_lectern, WORLD),
    "quest_oath_stone": ("world", quest_oath_stone, WORLD),
    "quest_great_lamp": ("world", quest_great_lamp, WORLD),
    "quest_rift_heart": ("world", quest_rift_heart, WORLD),
    "quest_bone_pyre": ("world", quest_bone_pyre, WORLD),
}

for n in range(1, 5):
    ALL["decal_blood_%d" % n] = ("dungeon", (lambda n=n: _blood(n)), DECAL)
for n in range(1, 3):
    ALL["decal_bones_%d" % n] = ("dungeon", (lambda n=n: _bones(n)), DECAL)
    ALL["decal_skull_%d" % n] = ("dungeon", (lambda n=n: _skull(n)), DECAL)
    ALL["decal_rubble_%d" % n] = ("dungeon", (lambda n=n: _rubble(n)), DECAL)
    ALL["decal_puddle_%d" % n] = ("dungeon", (lambda n=n: _puddle(n)), DECAL)
    ALL["decal_ritual_%d" % n] = ("dungeon", (lambda n=n: _ritual(n)), DECAL)

for n in range(1, 7):
    ALL["floor_flagstone_%d" % n] = ("dungeon", (lambda n=n: _flagstone(n)), DECAL)
    ALL["floor_brick_%d" % n] = ("dungeon", (lambda n=n: _brick(n)), DECAL)
    ALL["floor_earth_%d" % n] = ("dungeon", (lambda n=n: _earth(n)), DECAL)
    ALL["floor_moss_%d" % n] = ("dungeon", (lambda n=n: _flagstone(n, True)), DECAL)

for n in range(1, 5):
    ALL["wall_%d" % n] = ("dungeon", (lambda n=n: wall(n)), (160, 200, 80, 40))
    ALL["wall_low_%d" % n] = ("dungeon", (lambda n=n: wall_low(n)), (160, 120, 80, 40))
for name, build, frame in [("campfire", lair_campfire, PROP), ("sack", lair_sack, PROP), ("weapon_rack", lair_weapon_rack, PROP),
                           ("carcass", lair_carcass, PROP), ("altar", lair_altar, PROP), ("coffin", lair_coffin, PROP),
                           ("flesh_pod", lair_flesh_pod, PROP), ("spikes", lair_spikes, PROP), ("hellfire", lair_hellfire, PROP),
                           ("cage", lair_cage, PROP), ("stake", lair_stake, TALL)]:
    ALL["prop_" + name] = ("dungeon", build, frame)
for n in range(1, 4):
    ALL["decal_bedroll_%d" % n] = ("dungeon", (lambda n=n: _bedroll(n)), DECAL)
    ALL["decal_gore_%d" % n] = ("dungeon", (lambda n=n: _gore(n)), DECAL)
for n in range(1, 3):
    ALL["decal_straw_%d" % n] = ("dungeon", (lambda n=n: _straw(n)), DECAL)
    ALL["decal_sigil_%d" % n] = ("dungeon", (lambda n=n: _sigil(n)), DECAL)
    ALL["decal_ash_%d" % n] = ("dungeon", (lambda n=n: _ash(n)), DECAL)
    ALL["decal_net_%d" % n] = ("dungeon", (lambda n=n: _net(n)), DECAL)
for n in range(5, 17):
    ALL["wall_%d" % n] = ("dungeon", (lambda n=n: tall_wall(n)), (160, 320, 80, 40))
for n in range(5, 7):
    ALL["wall_low_%d" % n] = ("dungeon", (lambda n=n: _demonic_low(n - 4)), (160, 120, 80, 40))

WALL_DECOR = {
    "wall_torch_x": lambda: _wall_torch("x"),
    "wall_torch_y": lambda: _wall_torch("y"),
    "wall_chains_x": lambda: _wall_chains("x"),
    "wall_chains_y": lambda: _wall_chains("y"),
    "wall_banner_x": lambda: _wall_banner("x", (BLOOD[3], BLOOD[2])),
    "wall_banner_y": lambda: _wall_banner("y", (BLOOD[3], BLOOD[2])),
    "wall_candles": _wall_top_candles,
    "wall_skulls": _wall_top_skulls,
    "wall_web": _wall_web,
}
for name, build in WALL_DECOR.items():
    ALL[name] = ("world", build, WORLD)
