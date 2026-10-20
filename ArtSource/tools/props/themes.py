# The dungeon's two looks (the owner, 2026-10-10, from his concept art): a gothic crypt and cathedral for depths 1 to
# 12, natural caves below. Every piece stands on one cell (CELL square, its middle at the origin) and is rendered as a
# "world" piece, cropped, so it may spill past its cell (boulders) and keeps the cell's middle as its pivot. The game
# draws them as wall tiles (DungeonLevel, by the names below). Faces toward the camera face -x and -y.
#
#   crypt_wall_1..5      a dressed stone wall: 1 plain, 2 a pilaster at the front corner, 3 and 4 a niche with a skull
#                        on the x or y face, 5 cobwebs and a drip of wax
#   crypt_wall_low_1..2  the same cut low, for walls between the camera and a room
#   crypt_diag_<o>[_low] a wall cut along the cell's diagonal, for cut corners and round rooms: o is the half the wall
#                        fills, "b" (back, +x+y), "f" (front, -x-y), "l" (left, -x+y), "r" (right, +x-y)
#   crypt_pillar_1..2    a column: 1 square with a capital, 2 round with rings
#   crypt_ledge_1..3     a dais's rim (2026-10-20): dressed stone half a man high, paved on top; 2 a balustrade, 3 a
#                        candle stand on the lip
#   crypt_stairs_<d>     dressed steps through a dais's rim, down toward d: "px", "nx", "py", "ny"
#   crypt_pit_rim[_x|_y|_xy] a burial pit's parapet (2026-10-20): paving at floor level under a balustrade; _x, _y its
#                        dressed stone going down into the pit on the side toward -x, -y
#   crypt_chasm[_x|_y|_xy] a cell of a chasm (2026-10-20): a black drop; _x, _y show the masonry of its far side (+x, +y)
#                        going down into the dark
#   cave_wall_1..6       boulders heaped higher than a man, pale spikes on some
#   cave_wall_low_1..3   a low heap of boulders
#   cave_ledge_1..3      a terrace's rim: a lip of rock
#   cave_stairs_<d>      rough steps through a rim, down toward d: "px", "nx", "py", "ny"
#   stalagmite_1..3      a clump of pale spikes on a rock foot
import math
from kit import *

HALF = CELL / 2
# Twice her height (the owner, 2026-10-11: "Make the walls of all levels higher. It looks like the main character is a
# giant"); 1.1 / 0.866 until then, about her shoulders.
WALL_H = 2.0 / 0.866
LOW_H = 0.14 / 0.866


def _rng(*keys):
    import random
    r = random.Random()
    import zlib
    r.seed(zlib.crc32(repr(keys).encode()))
    return r


def _rock(loc, size, material, rng, squash=0.7, segments=7):
    mesh = sphere(size, loc, material, scale=(1 + rng.random() * 0.45, 1 + rng.random() * 0.45, squash), segments=segments, smooth=False)
    mesh.rotation_euler = (rng.random() * 0.5, rng.random() * 0.5, rng.random() * 6.28)
    return mesh


# ---------------------------------------------------------------- crypt

def _crypt_stones():
    return [mat(STONE[3], 0.3, STONE[2], scale=14), mat(STONE[3], 0.3, EARTH[2], scale=14),
            mat(STONE[2], 0.3, STONE[1], scale=14), mat(EARTH[4], 0.3, STONE[2], scale=14)]


def _ashlar_line(a, b, normal, h, rng, stones, z0=0.0, course=0.11, proud=0.018):
    """Courses of dressed blocks along a face from point a to point b (x, y), facing normal, from z0 to h."""
    ax, ay = a
    bx, by = b
    length = math.hypot(bx - ax, by - ay)
    dx, dy = (bx - ax) / length, (by - ay) / length
    nx, ny = normal
    turn = math.degrees(math.atan2(dy, dx))
    z = z0
    row = 0
    while z < h - 0.02:
        ch = min(course, h - z)
        t = 0.0 if row % 2 == 0 else -rng.uniform(0.06, 0.1)
        while t < length - 0.01:
            piece = rng.uniform(0.17, 0.25)
            t0 = max(0.0, t)
            t1 = min(length, t + piece)
            if t1 - t0 > 0.03:
                mid = (t0 + t1) / 2
                cx = ax + dx * mid + nx * proud
                cy = ay + dy * mid + ny * proud
                box((t1 - t0 - 0.012, proud * 2, ch - 0.012), (cx, cy, z + 0.006), rng.choice(stones), rot=(0, 0, turn), bevel=0.006)
            t += piece
        z += course
        row += 1


def _crypt_trim(a, b, normal, h, low):
    """The plinth at the foot and the moulded cornice at the top of a face."""
    ax, ay = a
    bx, by = b
    length = math.hypot(bx - ax, by - ay)
    turn = math.degrees(math.atan2(by - ay, bx - ax))
    nx, ny = normal
    mx, my = (ax + bx) / 2, (ay + by) / 2
    trim = mat(STONE[4], 0.2, STONE[2], scale=10)
    dark = mat(STONE[2], 0.2, STONE[1], scale=10)
    box((length, 0.07, 0.1), (mx + nx * 0.035, my + ny * 0.035, 0), dark, rot=(0, 0, turn), bevel=0.008)
    if not low:
        box((length, 0.08, 0.05), (mx + nx * 0.04, my + ny * 0.04, h - 0.11), trim, rot=(0, 0, turn), bevel=0.008)
        box((length, 0.05, 0.04), (mx + nx * 0.025, my + ny * 0.025, h - 0.06), dark, rot=(0, 0, turn), bevel=0.006)
    else:
        box((length, 0.06, 0.035), (mx + nx * 0.03, my + ny * 0.03, h - 0.035), trim, rot=(0, 0, turn), bevel=0.006)


def _crypt_top(points, h):
    top = mat(STONE[1], 0.3, STONE[0], scale=8)
    prism(points, 0.03, (0, 0, h - 0.03), top)


def _niche(face, rng):
    """An arched recess in a face with a skull on its sill (concept 4's ossuary walls)."""
    dark = mat(STONE[0], 0.0)
    frame = mat(STONE[4], 0.2, STONE[2], scale=10)
    bone = mat(BONE[1], 0.3, BONE[0], scale=12)
    w, z0, z1 = 0.3, 0.32, 0.78
    if face == "x":
        place = lambda t, out, z: (-HALF - out, t, z)
        size = lambda along, out, tall: (out, along, tall)
    else:
        place = lambda t, out, z: (t, -HALF - out, z)
        size = lambda along, out, tall: (along, out, tall)
    box(size(w, 0.006, z1 - z0), place(0, 0.0, z0), dark)
    for side in (-1, 1):
        box(size(0.05, 0.04, z1 - z0), place(side * (w / 2 + 0.02), 0.02, z0), frame, bevel=0.005)
    for k in range(7):
        a = math.pi * k / 6
        box(size(0.06, 0.04, 0.05), place(math.cos(a) * w / 2, 0.02, z1 + math.sin(a) * 0.12), frame, bevel=0.005)
    box(size(w + 0.1, 0.06, 0.04), place(0, 0.03, z0 - 0.04), frame, bevel=0.005)
    x, y, _ = place(0, 0.04, 0)
    sphere(0.055, (x, y, z0 + 0.06), bone, scale=(1, 1, 0.9), segments=8)
    for k in range(3):
        x, y, _ = place(rng.uniform(-0.1, 0.1), 0.04, 0)
        cyl(0.014, 0.16, (x, y, z0 + 0.012), bone, segments=6, rot=(0, 90, rng.uniform(0, 180)))


def crypt_wall(variant, low=False):
    rng = _rng("crypt", variant, low)
    h = LOW_H if low else WALL_H
    stones = _crypt_stones()
    box((CELL - 0.01, CELL - 0.01, h - 0.02), (0, 0, 0), mat(STONE[0], 0.0))
    _ashlar_line((-HALF, HALF), (-HALF, -HALF), (-1, 0), h - (0.03 if low else 0.12), rng, stones, z0=0.1)
    _ashlar_line((-HALF, -HALF), (HALF, -HALF), (0, -1), h - (0.03 if low else 0.12), rng, stones, z0=0.1)
    _crypt_trim((-HALF, HALF), (-HALF, -HALF), (-1, 0), h, low)
    _crypt_trim((-HALF, -HALF), (HALF, -HALF), (0, -1), h, low)
    _crypt_top([(-HALF, -HALF), (HALF, -HALF), (HALF, HALF), (-HALF, HALF)], h)
    if low:
        return
    if variant == 2:
        # A pilaster on the front corner: base, shaft and capital.
        trim = mat(STONE[4], 0.2, STONE[2], scale=10)
        shaft = mat(STONE[4], 0.25, STONE[2], scale=12)
        box((0.22, 0.22, 0.16), (-HALF + 0.02, -HALF + 0.02, 0), trim, bevel=0.01)
        box((0.17, 0.17, h - 0.3), (-HALF + 0.02, -HALF + 0.02, 0.16), shaft, bevel=0.01)
        box((0.23, 0.23, 0.1), (-HALF + 0.02, -HALF + 0.02, h - 0.2), trim, bevel=0.01)
    elif variant in (3, 4):
        _niche("x" if variant == 3 else "y", rng)
    elif variant == 5:
        web = mat(BONE[2], 0.0, rough=1.0)
        for face in ("x", "y"):
            for k in range(5):
                t = rng.uniform(-HALF + 0.05, HALF - 0.05)
                z = rng.uniform(0.5, h - 0.15)
                loc = (-HALF - 0.03, t, z) if face == "x" else (t, -HALF - 0.03, z)
                box((0.005, 0.18, 0.004) if face == "x" else (0.18, 0.005, 0.004), loc, web, rot=(rng.uniform(-50, 50), 0, 0) if face == "x" else (0, rng.uniform(-50, 50), 0))
        wax = mat(BONE[2], 0.1, BONE[1], scale=8)
        for k in range(3):
            loc = (rng.uniform(-HALF + 0.1, HALF - 0.1), -HALF - 0.02, h - 0.1)
            box((0.03, 0.02, rng.uniform(0.08, 0.2)), (loc[0], loc[1], h - 0.12 - 0.1), wax)


DIAG = {
    # The half of the cell the wall fills, as a triangle, and the face toward the open floor (from, to, normal).
    "b": ([(-HALF, HALF), (HALF, -HALF), (HALF, HALF)], ((-HALF, HALF), (HALF, -HALF)), (-0.7071, -0.7071)),
    "f": ([(-HALF, -HALF), (HALF, -HALF), (-HALF, HALF)], ((HALF, -HALF), (-HALF, HALF)), (0.7071, 0.7071)),
    "l": ([(-HALF, -HALF), (HALF, HALF), (-HALF, HALF)], ((-HALF, -HALF), (HALF, HALF)), (0.7071, -0.7071)),
    "r": ([(-HALF, -HALF), (HALF, -HALF), (HALF, HALF)], ((HALF, HALF), (-HALF, -HALF)), (-0.7071, 0.7071)),
}


def crypt_diag(orient, low=False):
    rng = _rng("diag", orient, low)
    h = LOW_H if low else WALL_H
    tri, (a, b), normal = DIAG[orient]
    prism(tri, h - 0.02, (0, 0, 0), mat(STONE[0], 0.0))
    stones = _crypt_stones()
    _ashlar_line(a, b, normal, h - (0.03 if low else 0.12), rng, stones, z0=0.1)
    _crypt_trim(a, b, normal, h, low)
    # The cell's own straight faces toward the camera, where the wall shows them.
    for (p, q, n) in (((-HALF, HALF), (-HALF, -HALF), (-1, 0)), ((-HALF, -HALF), (HALF, -HALF), (0, -1))):
        mid = ((p[0] + q[0]) / 2, (p[1] + q[1]) / 2)
        inside = mid[0] * normal[0] + mid[1] * normal[1] < -0.01
        if inside:
            _ashlar_line(p, q, n, h - (0.03 if low else 0.12), rng, stones, z0=0.1)
            _crypt_trim(p, q, n, h, low)
    _crypt_top(tri, h)


def crypt_pillar(variant):
    rng = _rng("pillar", variant)
    h = WALL_H * 1.05
    trim = mat(STONE[4], 0.2, STONE[2], scale=10)
    shaft = mat(STONE[4], 0.25, STONE[2], scale=12)
    dark = mat(STONE[2], 0.25, STONE[1], scale=10)
    box((0.62, 0.62, 0.1), (0, 0, 0), dark, bevel=0.012)
    box((0.54, 0.54, 0.12), (0, 0, 0.1), trim, bevel=0.012)
    if variant == 1:
        box((0.42, 0.42, h - 0.42), (0, 0, 0.22), shaft, bevel=0.03)
        for z in (0.5, 0.9):
            box((0.44, 0.44, 0.03), (0, 0, z), dark)
    else:
        cyl(0.2, h - 0.42, (0, 0, 0.22), shaft, segments=14)
        for z in (0.32, h * 0.55, h - 0.28):
            cyl(0.225, 0.04, (0, 0, z), trim, segments=14)
    box((0.58, 0.58, 0.12), (0, 0, h - 0.2), trim, bevel=0.012)
    box((0.66, 0.66, 0.08), (0, 0, h - 0.08), dark, bevel=0.01)
    web = mat(BONE[2], 0.0, rough=1.0)
    if rng.random() < 0.7:
        box((0.005, 0.2, 0.004), (-0.28, -0.1, h - 0.3), web, rot=(35, 0, 0))


# ---------------------------------------------------------------- caves

def _cave_rocks():
    return [mat(STONE[2], 0.35, STONE[1], scale=9), mat(EARTH[2], 0.35, EARTH[1], scale=9),
            mat(STONE[3], 0.35, EARTH[1], scale=9), mat(EARTH[3], 0.35, STONE[1], scale=9)]


def _heap(h, rng, rocks, size, front=0.08):
    """A dense heap of boulders filling the cell to height h: a jittered 3 x 3 grid in layers, bigger and further
    out at the foot toward the camera, so no core shows."""
    layers = max(2, int(round(h / (size * 0.9))))
    for layer in range(layers):
        z = h * layer / layers
        shrink = 1.0 - 0.25 * layer / layers
        for i in range(3):
            for j in range(3):
                x = -HALF + CELL * (i + 0.5) / 3 + rng.uniform(-0.06, 0.06)
                y = -HALF + CELL * (j + 0.5) / 3 + rng.uniform(-0.06, 0.06)
                if layer == 0:
                    x -= front * (1 if i == 0 else 0)
                    y -= front * (1 if j == 0 else 0)
                r = size * shrink * rng.uniform(0.8, 1.2)
                _rock((x, y, z + r * 0.45), r, rng.choice(rocks), rng, squash=rng.uniform(0.6, 0.85))


def _spikes(cx, cy, z, count, tall, rng):
    pale = [mat(BONE[1], 0.25, STONE[4], scale=10), mat(STONE[6], 0.2, BONE[0], scale=10), mat(STONE[5], 0.3, STONE[4], scale=10)]
    for k in range(count):
        r = rng.uniform(0.02, 0.06)
        ht = tall * rng.uniform(0.35, 1.0)
        cone(r, ht, (cx + rng.uniform(-0.12, 0.12), cy + rng.uniform(-0.12, 0.12), z - 0.02), rng.choice(pale), 6,
             rot=(rng.uniform(-8, 8), rng.uniform(-8, 8), 0))


def cave_wall(variant, low=False):
    rng = _rng("cave", variant, low)
    rocks = _cave_rocks()
    h = (0.3 if low else WALL_H * rng.uniform(0.85, 1.15))
    _heap(h, rng, rocks, 0.17 if low else 0.21)
    # A crown of rock points on top.
    if not low:
        for k in range(rng.randint(2, 4)):
            x, y = rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2)
            cone(rng.uniform(0.08, 0.14), rng.uniform(0.15, 0.35), (x, y, h * 0.95), rng.choice(rocks), 6, rot=(rng.uniform(-15, 15), rng.uniform(-15, 15), 0))
        if variant in (2, 5):
            _spikes(rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1), h, rng.randint(5, 9), 0.45, rng)
        if variant == 4:
            # Pale spikes growing out of the foot, toward the floor.
            for k in range(3):
                _spikes(rng.uniform(-HALF, 0), -HALF - 0.05, 0.0, 3, 0.3, rng)
    elif variant == 2:
        _spikes(0, 0, h * 0.9, 4, 0.22, rng)


def cave_ledge(variant):
    """A terrace's rim: a lip of flat-topped rock with a rough face, half a man high (2026-10-11: at a third, the owner
    could not tell the ground was changing height)."""
    rng = _rng("ledge", variant)
    rocks = _cave_rocks()
    h = LEDGE_H
    _heap(h, rng, rocks, 0.15, front=0.05)
    # A flatter top, as a lip of the terrace.
    for k in range(4):
        _rock((rng.uniform(-HALF * 0.6, HALF * 0.6), rng.uniform(-HALF * 0.6, HALF * 0.6), h), rng.uniform(0.14, 0.2), rng.choice(rocks), rng, squash=0.35)
    if variant == 3:
        _spikes(0.05, 0.05, h - 0.02, 4, 0.25, rng)


LEDGE_H = 0.45

STAIRS = {"px": (1, 0), "nx": (-1, 0), "py": (0, 1), "ny": (0, -1)}


def cave_stairs(direction):
    """A flight of cut steps through a terrace's rim, the lowest toward the way down (2026-10-11, the owner: the old
    rough steps "do not look like stairs and it is not clear that the elevation is changing"): five pale treads with
    dark risers climbing to the rim's height across the whole cell, so the three cells of a flight join into one wide
    stair with the rim's rock either side. The game builds flights only coming down toward the camera, risers in view."""
    rng = _rng("stairs", direction)
    dx, dy = STAIRS[direction]
    tread = [mat(STONE[5], 0.3, STONE[4], scale=12), mat(STONE[6], 0.3, STONE[4], scale=12)]
    riser = mat(STONE[1], 0.3, STONE[0], scale=10)
    count = 5
    depth = CELL / count
    rise = LEDGE_H / count
    across = CELL + 0.01
    for k in range(count):
        # Step k is k from the low side; it runs from its front edge to the top of the flight, so no gap shows below.
        front = HALF - depth * k
        mid = (front - HALF) / 2
        top = rise * (k + 1)
        length = front + HALF
        size = (length, across, top) if dx else (across, length, top)
        box(size, (dx * mid, dy * mid, 0), riser, bevel=0.004)
        # The tread: a pale slab on top, a little proud of the riser at its front edge.
        t = front - depth / 2 + 0.006
        size = (depth + 0.012, across, 0.025) if dx else (across, depth + 0.012, 0.025)
        box(size, (dx * t, dy * t, top - 0.025), rng.choice(tread), bevel=0.006)


def crypt_ledge(variant):
    """A raised dais's rim in the crypt (the owner, 2026-10-20: "Add verticality, stairs, platforms"): a block of dressed
    stone half a man high, as the cave's ledge, its camera faces coursed, a plinth at the foot, a moulded lip, its top
    paved in the paler stone of the dais. 2 a balustrade of stout posts under a rail along the front, 3 a candle stand
    on the lip."""
    from models import candles
    rng = _rng("crypt_ledge", variant)
    h = LEDGE_H
    stones = _crypt_stones()
    box((CELL - 0.01, CELL - 0.01, h - 0.02), (0, 0, 0), mat(STONE[0], 0.0))
    _ashlar_line((-HALF, HALF), (-HALF, -HALF), (-1, 0), h - 0.05, rng, stones, z0=0.1)
    _ashlar_line((-HALF, -HALF), (HALF, -HALF), (0, -1), h - 0.05, rng, stones, z0=0.1)
    _crypt_trim((-HALF, HALF), (-HALF, -HALF), (-1, 0), h, True)
    _crypt_trim((-HALF, -HALF), (HALF, -HALF), (0, -1), h, True)
    # The paving on top: four pale slabs with dark joints.
    pale = [mat(STONE[4], 0.3, STONE[3], scale=12), mat(STONE[5], 0.3, STONE[3], scale=12)]
    box((CELL - 0.01, CELL - 0.01, 0.01), (0, 0, h - 0.03), mat(STONE[1], 0.0))
    q = CELL / 4
    for sx in (-1, 1):
        for sy in (-1, 1):
            box((CELL / 2 - 0.02, CELL / 2 - 0.02, 0.025), (sx * q, sy * q, h - 0.025), rng.choice(pale), bevel=0.005)
    if variant == 2:
        post = mat(STONE[4], 0.25, STONE[2], scale=12)
        rail = mat(STONE[5], 0.2, STONE[3], scale=10)
        for k in range(3):
            t = -HALF + 0.1 + k * (CELL - 0.2) / 2
            cyl(0.04, 0.28, (-HALF + 0.08, t, h), post, segments=8)
            cyl(0.04, 0.28, (t, -HALF + 0.08, h), post, segments=8)
        box((0.09, CELL, 0.05), (-HALF + 0.08, 0, h + 0.28), rail, bevel=0.008)
        box((CELL, 0.09, 0.05), (0, -HALF + 0.08, h + 0.28), rail, bevel=0.008)
    elif variant == 3:
        iron = mat(STONE[0], 0.2, STONE[1], scale=8, metal=0.4)
        cyl(0.02, 0.5, (0, 0, h), iron, segments=6)
        box((0.22, 0.22, 0.02), (0, 0, h + 0.5), iron)
        candles((0, 0, h + 0.52), count=4, seed_n=91, spread=0.08)


def crypt_stairs(direction):
    """Dressed stone steps through a dais's rim (2026-10-20), as the cave's flights: five pale treads with dark risers
    climbing to the rim's height across the whole cell, so three cells join into one wide stair."""
    rng = _rng("crypt_stairs", direction)
    dx, dy = STAIRS[direction]
    tread = [mat(STONE[4], 0.3, STONE[3], scale=12), mat(STONE[5], 0.3, STONE[3], scale=12)]
    riser = mat(STONE[2], 0.3, STONE[1], scale=10)
    count = 5
    depth = CELL / count
    rise = LEDGE_H / count
    across = CELL + 0.01
    for k in range(count):
        front = HALF - depth * k
        mid = (front - HALF) / 2
        top = rise * (k + 1)
        length = front + HALF
        size = (length, across, top) if dx else (across, length, top)
        box(size, (dx * mid, dy * mid, 0), riser, bevel=0.004)
        t = front - depth / 2 + 0.006
        size = (depth + 0.012, across, 0.025) if dx else (across, depth + 0.012, 0.025)
        box(size, (dx * t, dy * t, top - 0.025), rng.choice(tread), bevel=0.006)


def crypt_pit_rim(faces):
    """A burial pit's parapet (2026-10-20, the owner: the heights "do not read as hight differences"): the pit's floor is
    drawn one level down, so its rim is paving at floor level with a balustrade along the front edges, and on the sides
    that face the pit (seen by the camera when the pit lies toward -x or -y) dressed stone going down a level."""
    rng = _rng("pit_rim", faces)
    stones = _crypt_stones()
    dark = [mat(STONE[1], 0.3, STONE[0], scale=14), mat(STONE[2], 0.3, STONE[0], scale=14)]
    pale = [mat(STONE[4], 0.3, STONE[3], scale=12), mat(STONE[5], 0.3, STONE[3], scale=12)]
    box((CELL - 0.01, CELL - 0.01, 0.02), (0, 0, -0.02), mat(STONE[1], 0.0))
    q = CELL / 4
    for sx in (-1, 1):
        for sy in (-1, 1):
            box((CELL / 2 - 0.02, CELL / 2 - 0.02, 0.025), (sx * q, sy * q, 0.0), rng.choice(pale), bevel=0.005)
    for face in faces:
        if face == "x":
            a, b, normal = (-HALF, HALF), (-HALF, -HALF), (-1, 0)
            back = ((0.05, CELL, LEDGE_H), (-HALF + 0.03, 0, -LEDGE_H))
        else:
            a, b, normal = (-HALF, -HALF), (HALF, -HALF), (0, -1)
            back = ((CELL, 0.05, LEDGE_H), (0, -HALF + 0.03, -LEDGE_H))
        box(back[0], back[1], mat(STONE[0], 0.0))
        _ashlar_line(a, b, normal, -0.02, rng, stones, z0=-LEDGE_H * 0.5)
        _ashlar_line(a, b, normal, -LEDGE_H * 0.5, rng, dark, z0=-LEDGE_H)
    post = mat(STONE[4], 0.25, STONE[2], scale=12)
    rail = mat(STONE[5], 0.2, STONE[3], scale=10)
    for k in range(3):
        t = -HALF + 0.1 + k * (CELL - 0.2) / 2
        cyl(0.035, 0.26, (-HALF + 0.08, t, 0.025), post, segments=8)
        cyl(0.035, 0.26, (t, -HALF + 0.08, 0.025), post, segments=8)
    box((0.08, CELL, 0.045), (-HALF + 0.08, 0, 0.285), rail, bevel=0.008)
    box((CELL, 0.08, 0.045), (0, -HALF + 0.08, 0.285), rail, bevel=0.008)


CHASM_DROP = 1.3


def crypt_chasm(faces):
    """A cell of a chasm across a crypt hall (the owner, 2026-10-20, okayed: "Bridges over chasms"): the ground opens on
    a drop into the dark. The far sides (+x, +y) are the ones the camera sees: on those, dressed stone goes down from the
    floor's edge, lit at the top and darkening into the depth; below, a black floor with a few fallen stones. A holdout
    at ground level hides everything below ground outside the cell."""
    rng = _rng("chasm", faces)
    holdout(HALF, 6.0, 0.0)
    black = mat((8, 6, 6), 0.0)
    # The camera looks down at 30 degrees, so through the hole it sees the floor of the drop well past the cell, toward
    # +x and +y: the black floor runs out that way.
    box((6.0, 6.0, 0.02), (2.5, 2.5, -CHASM_DROP - 0.02), black)
    rocks = [mat(STONE[0], 0.2, (8, 6, 6), scale=8), mat(STONE[1], 0.2, STONE[0], scale=8)]
    for k in range(3):
        _rock((rng.uniform(-HALF * 0.7, HALF * 0.7), rng.uniform(-HALF * 0.7, HALF * 0.7), -CHASM_DROP), rng.uniform(0.05, 0.1), rng.choice(rocks), rng, squash=0.5)
    upper = _crypt_stones()
    lower = [mat(STONE[1], 0.3, STONE[0], scale=14), mat(STONE[2], 0.3, STONE[0], scale=14)]
    deepest = [mat(STONE[0], 0.2, (8, 6, 6), scale=14)]
    coping = mat(STONE[4], 0.2, STONE[2], scale=10)
    for face in faces:
        if face == "x":
            a, b, normal = (HALF, -HALF), (HALF, HALF), (-1, 0)
            back = ((0.06, CELL + 0.02, CHASM_DROP), (HALF + 0.03, 0, -CHASM_DROP))
            lip = ((0.08, CELL, 0.05), (HALF - 0.02, 0, -0.05))
        else:
            a, b, normal = (-HALF, HALF), (HALF, HALF), (0, -1)
            back = ((CELL + 0.02, 0.06, CHASM_DROP), (0, HALF + 0.03, -CHASM_DROP))
            lip = ((CELL, 0.08, 0.05), (0, HALF - 0.02, -0.05))
        box(back[0], back[1], black)
        _ashlar_line(a, b, normal, -0.06, rng, upper, z0=-0.45)
        _ashlar_line(a, b, normal, -0.45, rng, lower, z0=-0.9)
        _ashlar_line(a, b, normal, -0.9, rng, deepest, z0=-CHASM_DROP)
        box(lip[0], lip[1], coping, bevel=0.006)


def stalagmite(variant):
    rng = _rng("stalagmite", variant)
    rocks = _cave_rocks()
    for k in range(3):
        _rock((rng.uniform(-0.15, 0.15), rng.uniform(-0.15, 0.15), 0.04), rng.uniform(0.12, 0.2), rng.choice(rocks), rng, squash=0.5)
    pale = [mat(BONE[1], 0.25, STONE[4], scale=10), mat(STONE[6], 0.2, BONE[0], scale=10), mat(STONE[5], 0.3, STONE[3], scale=10)]
    for k in range(rng.randint(4, 7)):
        r = rng.uniform(0.04, 0.09)
        ht = rng.uniform(0.35, 1.1) * (1.2 if variant == 3 else 1.0)
        cone(r, ht, (rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), 0.0), rng.choice(pale), 7, rot=(rng.uniform(-6, 6), rng.uniform(-6, 6), 0))


WORLD = (1600, 1600, 800, 500)


def register(all_pieces):
    for n in range(1, 6):
        all_pieces["crypt_wall_%d" % n] = ("world", (lambda n=n: crypt_wall(n)), WORLD)
    for n in range(1, 3):
        all_pieces["crypt_wall_low_%d" % n] = ("world", (lambda n=n: crypt_wall(n, True)), WORLD)
        all_pieces["crypt_pillar_%d" % n] = ("world", (lambda n=n: crypt_pillar(n)), WORLD)
    for o in "bflr":
        all_pieces["crypt_diag_" + o] = ("world", (lambda o=o: crypt_diag(o)), WORLD)
        all_pieces["crypt_diag_%s_low" % o] = ("world", (lambda o=o: crypt_diag(o, True)), WORLD)
    for n in range(1, 7):
        all_pieces["cave_wall_%d" % n] = ("world", (lambda n=n: cave_wall(n)), WORLD)
    for n in range(1, 4):
        all_pieces["cave_wall_low_%d" % n] = ("world", (lambda n=n: cave_wall(n, True)), WORLD)
        all_pieces["cave_ledge_%d" % n] = ("world", (lambda n=n: cave_ledge(n)), WORLD)
        all_pieces["stalagmite_%d" % n] = ("world", (lambda n=n: stalagmite(n)), WORLD)
    for d in STAIRS:
        all_pieces["cave_stairs_" + d] = ("world", (lambda d=d: cave_stairs(d)), WORLD)
        all_pieces["crypt_stairs_" + d] = ("world", (lambda d=d: crypt_stairs(d)), WORLD)
    for n in range(1, 4):
        all_pieces["crypt_ledge_%d" % n] = ("world", (lambda n=n: crypt_ledge(n)), WORLD)
    for name, faces in (("crypt_pit_rim", ""), ("crypt_pit_rim_x", "x"), ("crypt_pit_rim_y", "y"), ("crypt_pit_rim_xy", "xy")):
        all_pieces[name] = ("world", (lambda faces=faces: crypt_pit_rim(faces)), WORLD)
    for name, faces in (("crypt_chasm", ""), ("crypt_chasm_x", "x"), ("crypt_chasm_y", "y"), ("crypt_chasm_xy", "xy")):
        all_pieces[name] = ("world", (lambda faces=faces: crypt_chasm(faces)), WORLD)


# ---------------------------------------------------------------- the burnt library (2026-10-11)
#
# The owner: "first crypt, then library, then caves"; the library burnt ("Charred shelves, heaps of ash and half-burnt
# books, embers still glowing in the rubble"). Built like the crypt (its stone and cut corners), its faces toward the
# rooms lined with tall bookcases.
#   library_wall_1..4       1 a full bookcase, 2 one burnt out (charred books, a few embers), 3 a bookcase fallen
#                           forward, books spilled, 4 bare scorched stone
#   library_wall_low_1..2   the same cut low: a row of books on a low stone base
#   library_diag_<o>[_low]  a cut corner in scorched stone
#   library_shelf_x|y_1..2  a free-standing double bookcase along x or y (an aisle), 2 burnt
#   library_pillar          a scorched stone column


def _library_stone():
    return [mat(STONE[2], 0.35, STONE[1], scale=14), mat(STONE[1], 0.35, STONE[0], scale=14), mat(EARTH[1], 0.35, STONE[0], scale=14)]


def _books(face, u0, u1, z0, height, rng, burnt, depth=0.16):
    """A shelf's row of books along a face from u0 to u1 standing on z0: spines of muted leathers, some leaning, burnt
    ones black and gaps where they fell."""
    leathers = [mat(BLOOD[1], 0.3, BLOOD[0], scale=12), mat(WOOD[2], 0.3, WOOD[1], scale=12), mat(MOSS[1], 0.3, MOSS[0], scale=12),
                mat(COLD[1], 0.3, COLD[0], scale=12), mat(WOOD[3], 0.3, WOOD[2], scale=12)]
    char = mat(STONE[0], 0.3, WOOD[0], scale=12)
    ember = mat(EMBER[2], 0.3, EMBER[0], emit=2.5)
    u = u0
    while u < u1 - 0.03:
        w = rng.uniform(0.025, 0.05)
        if rng.random() < (0.25 if burnt else 0.08):
            u += w * rng.uniform(1, 3)
            continue
        h = height * rng.uniform(0.65, 0.95)
        black = burnt and rng.random() < 0.75
        material = char if black else rng.choice(leathers)
        lean = rng.uniform(-12, 12) if rng.random() < 0.15 else 0
        x, y, _ = face(u + w / 2, depth / 2)
        if lean:
            box((depth, w, h) if face.x else (w, depth, h), (x, y, z0), material, rot=(lean, 0, 0) if face.x else (0, lean, 0))
        else:
            box((depth, w, h) if face.x else (w, depth, h), (x, y, z0), material)
        if black and rng.random() < 0.12:
            box((depth * 0.3, w * 0.8, 0.02) if face.x else (w * 0.8, depth * 0.3, 0.02), face(u + w / 2, 0.01, z0 + h * 0.5), ember)
        u += w + 0.004


class _LFace:
    """A wall face toward -x ("x") or -y ("y"), u along it; p(u, out, z) is a point that far out from the face."""

    def __init__(self, axis):
        self.x = axis == "x"

    def __call__(self, u, out=0.0, z=0.0):
        return (-HALF - out, u, z) if self.x else (u, -HALF - out, z)


def _bookcase(face, burnt, rng, h, fallen=False):
    """A tall bookcase across a face: dark wood sides, top and shelves, books on every shelf."""
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=12) if not burnt else mat(STONE[0], 0.35, WOOD[0], scale=12)
    depth = 0.2
    top = h - 0.1
    def board(u0, u1, z0, z1, out0, out1):
        a = face(u0, out0)
        b = face(u1, out1)
        cx, cy = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        sx, sy = abs(a[0] - b[0]) or 0.02, abs(a[1] - b[1]) or 0.02
        box((sx, sy, z1 - z0), (cx, cy, z0), wood, bevel=0.004)
    if fallen:
        # The case has pitched forward onto the floor: it lies on its face, its back up, books spilled round it.
        x, y, _ = face(0.0, 0.5)
        box((0.9, CELL - 0.06, 0.24) if face.x else (CELL - 0.06, 0.9, 0.24), (x, y, 0.0), wood, bevel=0.01)
        for k in range(3):
            px, py, _ = face(0.0, 0.15 + 0.3 * k)
            box((0.03, CELL - 0.1, 0.03) if face.x else (CELL - 0.1, 0.03, 0.03), (px, py, 0.24), mat(STONE[0], 0.3, WOOD[0], scale=12))
        char = [mat(STONE[0], 0.3, WOOD[0], scale=12), mat(BLOOD[1], 0.3, BLOOD[0], scale=12), mat(WOOD[2], 0.3, WOOD[1], scale=12)]
        for n in range(22):
            px, py, _ = face(rng.uniform(-HALF, HALF), rng.uniform(0.1, 0.9))
            box((0.07, 0.1, 0.025), (px, py, rng.uniform(0, 0.08)), rng.choice(char), rot=(rng.uniform(-20, 20), rng.uniform(-20, 20), rng.uniform(0, 180)))
        return
    for u in (-HALF + 0.02, HALF - 0.04):
        board(u, u + 0.04, 0.0, top, 0.0, depth)
    board(-HALF, HALF, top, top + 0.06, 0.0, depth + 0.02)
    shelves = 5
    for k in range(shelves):
        z = 0.12 + (top - 0.2) * k / shelves
        board(-HALF + 0.04, HALF - 0.04, z, z + 0.035, 0.0, depth)
        if k < shelves:
            _books(face, -HALF + 0.07, HALF - 0.07, z + 0.035, (top - 0.2) / shelves - 0.05, rng, burnt, depth - 0.03)


def library_wall(variant, low=False):
    rng = _rng("library", variant, low)
    h = LOW_H if low else WALL_H
    stones = _library_stone()
    box((CELL - 0.01, CELL - 0.01, h - 0.02), (0, 0, 0), mat(STONE[0], 0.0))
    if low:
        _ashlar_line((-HALF, HALF), (-HALF, -HALF), (-1, 0), h - 0.03, rng, stones, z0=0.0)
        _ashlar_line((-HALF, -HALF), (HALF, -HALF), (0, -1), h - 0.03, rng, stones, z0=0.0)
        for axis in "xy":
            _books(_LFace(axis), -HALF + 0.05, HALF - 0.05, h - 0.02, 0.16, rng, variant == 2, 0.12)
        _crypt_top([(-HALF, -HALF), (HALF, -HALF), (HALF, HALF), (-HALF, HALF)], h)
        return
    fallen_axis = "x" if rng.random() < 0.5 else "y"
    for axis, a, b, n in (("x", (-HALF, HALF), (-HALF, -HALF), (-1, 0)), ("y", (-HALF, -HALF), (HALF, -HALF), (0, -1))):
        _ashlar_line(a, b, n, h - 0.12, rng, stones, z0=0.1)
        if variant in (1, 2):
            _bookcase(_LFace(axis), variant == 2, rng, h * 0.82)
        elif variant == 3:
            # One face's case has fallen forward; the other still stands, burnt.
            _bookcase(_LFace(axis), True, rng, h * 0.82, fallen=axis == fallen_axis)
    # Soot up the stone, and the cornice.
    _crypt_trim((-HALF, HALF), (-HALF, -HALF), (-1, 0), h, False)
    _crypt_trim((-HALF, -HALF), (HALF, -HALF), (0, -1), h, False)
    _crypt_top([(-HALF, -HALF), (HALF, -HALF), (HALF, HALF), (-HALF, HALF)], h)
    soot = mat(STONE[0], 0.4, WOOD[0], scale=6)
    for axis in "xy":
        f = _LFace(axis)
        for k in range(3):
            x, y, _ = f(rng.uniform(-HALF, HALF), 0.03)
            box((0.01, 0.2, 0.6) if f.x else (0.2, 0.01, 0.6), (x, y, h * rng.uniform(0.55, 0.75)), soot)


def library_diag(orient, low=False):
    rng = _rng("ldiag", orient, low)
    h = LOW_H if low else WALL_H
    tri, (a, b), normal = DIAG[orient]
    prism(tri, h - 0.02, (0, 0, 0), mat(STONE[0], 0.0))
    stones = _library_stone()
    _ashlar_line(a, b, normal, h - (0.03 if low else 0.12), rng, stones, z0=0.1)
    _crypt_trim(a, b, normal, h, low)
    for (p, q, n) in (((-HALF, HALF), (-HALF, -HALF), (-1, 0)), ((-HALF, -HALF), (HALF, -HALF), (0, -1))):
        mid = ((p[0] + q[0]) / 2, (p[1] + q[1]) / 2)
        if mid[0] * normal[0] + mid[1] * normal[1] < -0.01:
            _ashlar_line(p, q, n, h - (0.03 if low else 0.12), rng, stones, z0=0.1)
            _crypt_trim(p, q, n, h, low)
    _crypt_top(tri, h)


def library_shelf(axis, variant):
    """A free-standing double bookcase along x or y, filling its cell's length so a row of cells reads as one case."""
    rng = _rng("lshelf", axis, variant)
    burnt = variant == 2
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=12) if not burnt else mat(STONE[0], 0.35, WOOD[0], scale=12)
    h = 1.75
    thick = 0.36
    length = CELL + 0.01
    size = lambda along, across, tall: (along, across, tall) if axis == "x" else (across, along, tall)
    box(size(length, 0.04, h), (0, 0, 0), wood)
    box(size(length, thick + 0.04, 0.06), (0, 0, h), wood, bevel=0.005)
    box(size(length, thick, 0.06), (0, 0, 0), wood)
    for k in range(5):
        z = 0.1 + (h - 0.2) * k / 5
        box(size(length, thick, 0.03), (0, 0, z), wood)
    # Books on both faces: build each face as a wall face at the case's side.
    class Side:
        def __init__(self, sign):
            self.x = axis == "y"
            self.sign = sign

        def __call__(self, u, out=0.0, z=0.0):
            # "out" is measured inward from the case's face, as a wall's books stand out from the wall.
            d = self.sign * (thick / 2 - out)
            return (d, u, z) if self.x else (u, d, z)
    for sign in (-1, 1):
        side = Side(sign)
        for k in range(5):
            z = 0.1 + (h - 0.2) * k / 5 + 0.03
            _books(side, -length / 2 + 0.02, length / 2 - 0.02, z, (h - 0.2) / 5 - 0.05, rng, burnt, 0.15)


def library_pillar():
    rng = _rng("lpillar")
    h = WALL_H * 1.05
    stone = mat(STONE[2], 0.3, STONE[1], scale=10)
    dark = mat(STONE[1], 0.3, STONE[0], scale=10)
    box((0.58, 0.58, 0.14), (0, 0, 0), dark, bevel=0.012)
    box((0.42, 0.42, h - 0.36), (0, 0, 0.14), stone, bevel=0.03)
    box((0.58, 0.58, 0.14), (0, 0, h - 0.22), dark, bevel=0.012)
    soot = mat(STONE[0], 0.4, WOOD[0], scale=6)
    for k in range(3):
        box((0.01, 0.15, 0.5), (-0.215, rng.uniform(-0.15, 0.15), rng.uniform(0.6, 1.4)), soot)


def register_library(all_pieces):
    for n in range(1, 5):
        all_pieces["library_wall_%d" % n] = ("world", (lambda n=n: library_wall(n)), WORLD)
    for n in range(1, 3):
        all_pieces["library_wall_low_%d" % n] = ("world", (lambda n=n: library_wall(n, True)), WORLD)
        for axis in "xy":
            all_pieces["library_shelf_%s_%d" % (axis, n)] = ("world", (lambda a=axis, n=n: library_shelf(a, n)), WORLD)
    for o in "bflr":
        all_pieces["library_diag_" + o] = ("world", (lambda o=o: library_diag(o)), WORLD)
        all_pieces["library_diag_%s_low" % o] = ("world", (lambda o=o: library_diag(o, True)), WORLD)
    all_pieces["library_pillar"] = ("world", library_pillar, WORLD)


# ---------------------------------------------------------------- stairways set into a wall (2026-10-10)
#
#   stairs_<up|down>_<x|y>_<crypt|cave>  a doorway in a wall cell, its face toward -x or -y: steps rise into it (up), or
#                                        sink through a pit in the floor before it and on down under the arch (down).
#                                        The origin is the wall cell's middle; the pit lies in the floor cell in front.

def _holdout_rect(x0, x1, y0, y1, outer=4.0, z=0.0):
    """A ground plane with a rectangular hole: hides whatever lies below ground outside the hole."""
    m = bpy.data.materials.new("HoldoutRect")
    m.use_nodes = True
    nodes = m.node_tree.nodes
    for n in list(nodes):
        nodes.remove(n)
    out = nodes.new("ShaderNodeOutputMaterial")
    hold = nodes.new("ShaderNodeHoldout")
    m.node_tree.links.new(hold.outputs["Holdout"], out.inputs["Surface"])
    o = outer
    verts = [(-o, -o, z), (o, -o, z), (o, o, z), (-o, o, z), (x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)]
    faces = [(0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return poly(verts, faces, m)


class _Frame:
    """Coordinates across a wall face: u into the wall from its face, v along it, for a face toward -x or -y."""

    def __init__(self, axis):
        self.x = axis == "x"

    def at(self, u, v, z=0.0):
        return (-HALF + u, v, z) if self.x else (v, -HALF + u, z)

    def size(self, du, dv, dz):
        return (du, dv, dz) if self.x else (dv, du, dz)

    def box(self, u0, u1, v0, v1, z0, z1, material, bevel=0.0):
        box(self.size(u1 - u0, v1 - v0, z1 - z0), self.at((u0 + u1) / 2, (v0 + v1) / 2, z0), material, bevel=bevel)

    def line(self, v0, v1, u=0.0):
        return (self.at(u, v0)[:2], self.at(u, v1)[:2])

    @property
    def normal(self):
        return (-1, 0) if self.x else (0, -1)


# Three wall cells wide (2026-10-11, the owner: "The stairs between levels are too small. Make them bigger"): the piece
# stands on the middle wall cell and covers the wall cell each side of it, which the game leaves undrawn.
SPAN = 1.5 * CELL  # half the whole piece's width
OPEN_W = 0.5    # half the doorway's width
OPEN_H = 1.5    # the doorway's height at its crown


def _doorway(f, theme, rng):
    """The wall cell around a doorway: masonry piers and a lintel with an arch (crypt), or boulders heaped round a
    rough opening (cave). Behind the doorway, a dark passage."""
    h = WALL_H
    # The passage behind the doorway: dark sides, ceiling and back, open toward the camera so its steps show.
    dark = mat(STONE[0], 0.0)
    inner = mat(STONE[1], 0.2, STONE[0], scale=8)
    f.box(CELL - 0.04, CELL, -OPEN_W, OPEN_W, -1.3, OPEN_H, dark)
    f.box(0.0, CELL, -OPEN_W, OPEN_W, OPEN_H - 0.02, OPEN_H, dark)
    for v in (-OPEN_W, OPEN_W - 0.02):
        f.box(0.0, CELL, v, v + 0.02, -1.3, OPEN_H, inner)
    if theme == "crypt":
        core = mat(STONE[0], 0.0)
        stones = _crypt_stones()
        f.box(0.0, CELL, -SPAN, -OPEN_W, 0, h - 0.02, core)
        f.box(0.0, CELL, OPEN_W, SPAN, 0, h - 0.02, core)
        f.box(0.0, CELL, -OPEN_W, OPEN_W, OPEN_H, h - 0.02, core)
        for v0, v1 in ((-SPAN, -OPEN_W - 0.04), (OPEN_W + 0.04, SPAN)):
            a, b = f.line(v0, v1)
            _ashlar_line(a, b, f.normal, h - 0.12, rng, stones, z0=0.1)
        a, b = f.line(-OPEN_W, OPEN_W)
        _ashlar_line(a, b, f.normal, h - 0.12, rng, stones, z0=OPEN_H + 0.04)
        a, b = f.line(-SPAN, SPAN)
        _crypt_trim(a, b, f.normal, h, False)
        # The arch: jambs and voussoirs standing proud of the face.
        frame = mat(STONE[4], 0.2, STONE[2], scale=10)
        spring = OPEN_H - OPEN_W
        for side in (-1, 1):
            f.box(-0.04, 0.03, side * OPEN_W - 0.03, side * OPEN_W + 0.03, 0, spring, frame, bevel=0.006)
        for k in range(15):
            a = math.pi * k / 14
            v = math.cos(a) * OPEN_W
            z = spring + math.sin(a) * OPEN_W
            f.box(-0.04, 0.03, v - 0.035, v + 0.035, z - 0.03, z + 0.04, frame, bevel=0.006)
        # The spandrels: stone filling the corners between the arch and the square opening above it, in thin slices.
        slices = 16
        for k in range(slices):
            v0 = -OPEN_W + 2 * OPEN_W * k / slices
            v1 = v0 + 2 * OPEN_W / slices
            v = (v0 + v1) / 2
            z = spring + math.sqrt(max(0.0, OPEN_W * OPEN_W - v * v)) + 0.03
            if z < OPEN_H:
                f.box(0.0, 0.12, v0, v1, z, OPEN_H + 0.01, rng.choice(stones))
        top = [f.at(0, -SPAN)[:2], f.at(CELL, -SPAN)[:2], f.at(CELL, SPAN)[:2], f.at(0, SPAN)[:2]]
        _crypt_top(top, h)
    else:
        rocks = _cave_rocks()
        core = mat(EARTH[0], 0.2, STONE[0], scale=6)
        f.box(0.0, CELL, -SPAN, -OPEN_W, 0, h * 0.8, core)
        f.box(0.0, CELL, OPEN_W, SPAN, 0, h * 0.8, core)
        f.box(0.0, CELL, -OPEN_W, OPEN_W, OPEN_H, h * 0.8, core)
        # Boulders heaped up the two sides of the opening, out to the piece's edges, and over it.
        columns = int(math.ceil((SPAN - OPEN_W) / 0.22))
        for layer in range(12):
            z = h * layer / 12
            for side in (-1, 1):
                for c in range(columns):
                    for j in range(2):
                        r = 0.12 * rng.uniform(0.85, 1.15)
                        v = side * (OPEN_W + 0.13 + 0.22 * c + rng.uniform(0.0, 0.05))
                        u = CELL * (0.2 + 0.55 * j) + rng.uniform(-0.04, 0.04)
                        _rock(f.at(u - (0.05 if j == 0 else 0), v, z + r * 0.4), r, rng.choice(rocks), rng, squash=rng.uniform(0.6, 0.85))
            if z >= OPEN_H - 0.05:
                for k in range(5):
                    r = 0.16 * rng.uniform(0.85, 1.2)
                    _rock(f.at(rng.uniform(0.0, CELL * 0.8), rng.uniform(-OPEN_W, OPEN_W), z + r * 0.4), r, rng.choice(rocks), rng, squash=0.75)
        # A lintel of one long slab over the opening.
        f.box(-0.05, 0.25, -OPEN_W - 0.08, OPEN_W + 0.08, OPEN_H - 0.05, OPEN_H + 0.1, rng.choice(rocks), bevel=0.03)


def _marker(f, theme, down):
    """What makes a stairway stand out in the dark: a torch on each side of the doorway, and warm light inside the
    passage lighting its steps (from above for the way up, from below for the way down)."""
    from models import flame
    iron = mat(STONE[1], 0.2, STONE[0], metal=0.6, rough=0.5)
    wood = mat(WOOD[2], 0.3, WOOD[1])
    for side in (-1, 1):
        v = side * (OPEN_W + 0.3)
        x, y, _ = f.at(-0.05, v)
        box((0.06, 0.06, 0.1), (x, y, 0.97), iron)
        cyl(0.03, 0.24, (x, y, 1.01), wood, 8)
        lathe([(0.025, 0), (0.06, 0.07), (0.065, 0.09)], (x, y, 1.23), iron, 8, smooth=False, cap=False)
        flame((x, y, 1.27), 0.1, 8)
    light("POINT", f.at(CELL * 0.75, 0, -0.6 if down else 0.8), 60, (1.0, 0.62, 0.32), 0.2)
    light("POINT", f.at(-0.3, 0, 1.2), 40, (1.0, 0.6, 0.3), 0.2)


def _steps(f, theme, count, u0, u1, z0, rise, rng):
    """count steps from u0 to u1 along the passage, the first at z0, each rise higher (negative rise goes down)."""
    stone = [mat(STONE[5], 0.3, STONE[3], scale=10), mat(STONE[4], 0.3, STONE[3], scale=10)] if theme == "crypt" else \
            [mat(STONE[4], 0.35, STONE[2], scale=12), mat(EARTH[4], 0.35, EARTH[2], scale=12)]
    depth = (u1 - u0) / count
    for k in range(count):
        top = z0 + rise * k
        ua = u0 + depth * k
        f.box(ua, u1 if rise > 0 else ua + depth + 0.01, -OPEN_W + 0.01, OPEN_W - 0.01, min(top - 0.12, -1.3) if rise < 0 else 0, top,
              stone[k % 2], bevel=0.008 if theme == "crypt" else 0.02)


def stairs_up(axis, theme):
    rng = _rng("stairs_up", axis, theme)
    f = _Frame(axis)
    _doorway(f, theme, rng)
    _steps(f, theme, 7, 0.0, CELL, 0.11, 0.13, rng)
    _marker(f, theme, False)
    _holdout_rect(0.0, 0.0, 0.0, 0.0)  # nothing below the ground shows


def stairs_down(axis, theme):
    rng = _rng("stairs_down", axis, theme)
    f = _Frame(axis)
    _doorway(f, theme, rng)
    pit = CELL * 0.95
    # The pit in the floor before the wall: its sides go down into the dark.
    side = mat(STONE[2], 0.3, STONE[1], scale=8) if theme == "crypt" else mat(EARTH[1], 0.35, EARTH[0], scale=8)
    for v in (-OPEN_W - 0.03, OPEN_W + 0.03):
        f.box(-pit, 0.05, v - 0.03, v + 0.03, -1.3, 0, side)
    f.box(-pit - 0.04, -pit, -OPEN_W - 0.06, OPEN_W + 0.06, -1.3, 0, side)
    _steps(f, theme, 8, -pit, CELL, -0.1, -0.13, rng)
    _marker(f, theme, True)
    a = f.at(-pit, -OPEN_W - 0.06)
    b = f.at(0.06, OPEN_W + 0.06)
    _holdout_rect(min(a[0], b[0]), max(a[0], b[0]), min(a[1], b[1]), max(a[1], b[1]))
    # The kerb round the pit.
    kerb = mat(STONE[4], 0.25, STONE[2], scale=10) if theme == "crypt" else mat(STONE[3], 0.35, EARTH[1], scale=10)
    f.box(-pit - 0.1, -pit - 0.02, -OPEN_W - 0.12, OPEN_W + 0.12, 0, 0.05, kerb, bevel=0.01)
    for v in (-1, 1):
        f.box(-pit - 0.1, 0.0, v * (OPEN_W + 0.08) - 0.04, v * (OPEN_W + 0.08) + 0.04, 0, 0.05, kerb, bevel=0.01)


def register_stairs(all_pieces):
    for axis in "xy":
        for theme in ("crypt", "cave"):
            all_pieces["stairs_up_%s_%s" % (axis, theme)] = ("world", (lambda a=axis, t=theme: stairs_up(a, t)), WORLD)
            all_pieces["stairs_down_%s_%s" % (axis, theme)] = ("world", (lambda a=axis, t=theme: stairs_down(a, t)), WORLD)
