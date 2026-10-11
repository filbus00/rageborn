# The town remade dark (the owner, 2026-10-11, from his concept art: "the town is way too vibrant. remake it. Make the
# stairs leading down into the dungeon be in some sort of mausoleum, aside from the town main square where all the
# npcs are"): soot-dark stone ground floors under half-timbered upper storeys, steep slate roofs, warm windows; burnt
# ruins; a graveyard with an iron fence and a mausoleum whose open door leads down. Every piece faces the camera with
# its -x and -y sides and stands on its footprint's middle (TownLayout in the game). Rendered as "world" pieces.
#
#   house_1..4        two-storey houses: 1 wide with a chimney, 2 tall and narrow, 3 a long hall with a lean-to,
#                     4 a stone house with a half-fallen roof under scaffolding
#   ruin_1..2         burnt stone shells, roofless, with gaping arches and rubble
#   mausoleum         the way down: a stone tomb with a columned porch and an open door, steps going down inside
#   iron_fence_x|y    four cells of spiked iron railing
#   grave_cross, tomb a leaning stone cross, a low tomb slab
#   cage_stall        the Pet Vendor's stall: a dark awning over stacked wooden cages
#   forge             a smith's hearth with glowing coals, its chimney and an anvil
#   torch_post        a burning torch on a pole
import math
from kit import *

WORLD = (1600, 1600, 800, 500)


def _rng(*keys):
    import random
    import zlib
    r = random.Random()
    r.seed(zlib.crc32(repr(keys).encode()))
    return r


def _stone():
    return [mat(STONE[2], 0.35, STONE[1], scale=9), mat(STONE[3], 0.35, STONE[1], scale=9), mat(EARTH[2], 0.35, STONE[1], scale=9)]


def _courses(a, b, normal, z0, z1, rng, stones, course=0.2, proud=0.02):
    """Rough stone courses along a face from a to b (x, y), facing normal, from z0 to z1."""
    ax, ay = a
    bx, by = b
    length = math.hypot(bx - ax, by - ay)
    dx, dy = (bx - ax) / length, (by - ay) / length
    nx, ny = normal
    turn = math.degrees(math.atan2(dy, dx))
    z = z0
    row = 0
    while z < z1 - 0.02:
        ch = min(course, z1 - z)
        t = 0.0 if row % 2 == 0 else -rng.uniform(0.1, 0.2)
        while t < length - 0.01:
            piece = rng.uniform(0.28, 0.45)
            t0, t1 = max(0.0, t), min(length, t + piece)
            if t1 - t0 > 0.05:
                mid = (t0 + t1) / 2
                box((t1 - t0 - 0.02, proud * 2, ch - 0.02), (ax + dx * mid + nx * proud, ay + dy * mid + ny * proud, z + 0.01),
                    rng.choice(stones), rot=(0, 0, turn), bevel=0.01)
            t += piece
        z += course
        row += 1


class Face:
    """A wall face toward -x ("x") or -y ("y"): u along the face, z up; out is toward the camera."""

    def __init__(self, axis, at, half):
        self.x = axis == "x"
        self.at = at        # the face's coordinate across (x for an -x face, y for a -y face)
        self.half = half    # half its length

    def p(self, u, out=0.0, z=0.0):
        return (self.at - out, u, z) if self.x else (u, self.at - out, z)

    def box(self, u0, u1, z0, z1, depth, material, out=0.0, bevel=0.0):
        size = (depth, u1 - u0, z1 - z0) if self.x else (u1 - u0, depth, z1 - z0)
        box(size, self.p((u0 + u1) / 2, out - depth / 2 + 0.001, z0), material, bevel=bevel)

    @property
    def ends(self):
        return (self.p(-self.half)[:2], self.p(self.half)[:2])

    @property
    def normal(self):
        return (-1, 0) if self.x else (0, -1)


def _window(f, u, z, w, h, lit, frame):
    glass = mat(EMBER[3], 0.25, EMBER[1], emit=1.6) if lit else mat(STONE[0], 0.0)
    f.box(u - w / 2, u + w / 2, z, z + h, 0.02, glass, out=0.045)
    f.box(u - w / 2 - 0.05, u + w / 2 + 0.05, z - 0.06, z, 0.08, frame, out=0.04)
    f.box(u - 0.02, u + 0.02, z, z + h, 0.05, frame, out=0.03)
    f.box(u - w / 2, u + w / 2, z + h / 2 - 0.02, z + h / 2 + 0.02, 0.05, frame, out=0.03)


def _arch(f, u, z0, w, h, material, frame):
    """A dark arched opening (door or window) with a stone frame."""
    dark = mat(STONE[0], 0.0)
    spring = z0 + h - w / 2
    # In front of the stone courses (proud 0.04), so the opening shows.
    f.box(u - w / 2, u + w / 2, z0, spring, 0.02, material if material else dark, out=0.045)
    for k in range(9):
        a = math.pi * k / 8
        uu = u + math.cos(a) * w / 2
        zz = spring + math.sin(a) * w / 2
        f.box(uu - 0.07, uu + 0.07, zz - 0.06, zz + 0.07, 0.07, frame, out=0.04, bevel=0.01)
        f.box(uu - 0.05, uu + 0.05, spring, zz, 0.02, material if material else dark, out=0.045)
    for s in (-1, 1):
        f.box(u + s * w / 2 - 0.06, u + s * w / 2 + 0.06, z0, spring, 0.07, frame, out=0.04, bevel=0.01)


def _townhouse(sx, sy, stone_h, upper_h, roof_h, key, chimney=False, lit=(True, False, True), door_face="y", jetty=0.12):
    """A stone ground floor with an arched door, a jettied half-timbered upper storey, a steep slate roof (ridge along
    x). sx, sy the ground floor's size."""
    rng = _rng("house", key)
    stones = _stone()
    core = mat(STONE[1], 0.3, STONE[0], scale=6)
    plaster = mat(EARTH[2], 0.35, EARTH[1], scale=5)
    beam = mat(WOOD[1], 0.25, WOOD[0], scale=10)
    frame = mat(STONE[3], 0.3, STONE[2], scale=10)
    slate = mat(STONE[1], 0.4, COLD[0], scale=18)
    slate2 = mat(STONE[2], 0.4, COLD[1], scale=18)
    door = mat(WOOD[1], 0.3, WOOD[0], scale=14)
    # Ground floor: stone.
    box((sx, sy, stone_h), (0, 0, 0), core)
    fx = Face("x", -sx / 2, sy / 2)
    fy = Face("y", -sy / 2, sx / 2)
    for f in (fx, fy):
        a, b = f.ends
        _courses(a, b, f.normal, 0.0, stone_h, rng, stones)
    entry = fy if door_face == "y" else fx
    _arch(entry, entry.half * 0.25, 0.0, 0.7, 1.45, door, frame)
    other = fx if entry is fy else fy
    _arch(other, -other.half * 0.3, 0.55, 0.45, 0.75, None, frame)
    # The jettied upper storey: dark plaster between black beams.
    ux, uy = sx + 2 * jetty, sy + 2 * jetty
    box((ux, uy, upper_h), (0, 0, stone_h), plaster)
    box((ux + 0.04, uy + 0.04, 0.12), (0, 0, stone_h - 0.02), beam)
    gx = Face("x", -ux / 2, uy / 2)
    gy = Face("y", -uy / 2, ux / 2)
    t = 0.09
    for f in (gx, gy):
        n = max(2, int(round(f.half * 2 / 0.75)))
        for k in range(n + 1):
            u = -f.half + 2 * f.half * k / n
            f.box(u - t / 2, u + t / 2, stone_h, stone_h + upper_h, 0.05, beam, out=0.02)
        f.box(-f.half, f.half, stone_h + upper_h - t, stone_h + upper_h, 0.06, beam, out=0.025)
        f.box(-f.half, f.half, stone_h + upper_h * 0.45, stone_h + upper_h * 0.45 + t, 0.05, beam, out=0.02)
        # Braces in alternate bays.
        for k in range(0, n, 2):
            u0 = -f.half + 2 * f.half * k / n
            u1 = -f.half + 2 * f.half * (k + 1) / n
            length = math.hypot(u1 - u0, upper_h * 0.45)
            angle = math.degrees(math.atan2(upper_h * 0.45, u1 - u0))
            x, y, _ = f.p((u0 + u1) / 2, 0.02, stone_h + upper_h * 0.22)
            if f.x:
                box((0.05, length, t), (x - 0.02, y, stone_h + upper_h * 0.2), beam, rot=(angle, 0, 0))
            else:
                box((length, 0.05, t), (x, y - 0.02, stone_h + upper_h * 0.2), beam, rot=(0, -angle, 0))
    for i, f in enumerate((gx, gy)):
        n = max(1, int(f.half * 2 / 1.4))
        for k in range(n):
            u = -f.half + 2 * f.half * (k + 0.5) / n
            _window(f, u, stone_h + upper_h * 0.55, 0.32, 0.38, lit[(i + k) % len(lit)], beam)
    # A steep slate roof, ridge along x, overhanging.
    top = stone_h + upper_h
    o = 0.25
    hx, hy = ux / 2 + o, uy / 2 + o
    verts = [(-hx, -hy, top - 0.08), (hx, -hy, top - 0.08), (hx, 0, top + roof_h), (-hx, 0, top + roof_h), (-hx, hy, top - 0.08), (hx, hy, top - 0.08)]
    poly(verts, [(0, 1, 2, 3), (3, 2, 5, 4)], slate)
    rows = int(roof_h / 0.16)
    for k in range(1, rows):
        f_ = k / rows
        box((2 * hx, 0.035, 0.03), (0, -hy * (1 - f_) - 0.01, top - 0.08 + roof_h * f_ - 0.02), slate2 if k % 2 else slate)
    box((2 * hx + 0.06, 0.18, 0.12), (0, 0, top + roof_h - 0.08), mat(STONE[0], 0.2))
    poly([(-hx, -hy, top - 0.08), (hx, -hy, top - 0.08), (hx, -hy, top - 0.22), (-hx, -hy, top - 0.22)], [(0, 1, 2, 3)], mat(STONE[0], 0.0))
    # The gable end toward the camera, plastered and beamed.
    gable = mat(EARTH[2], 0.35, EARTH[1], scale=5)
    poly([(-ux / 2, -uy / 2, top - 0.01), (-ux / 2, uy / 2, top - 0.01), (-ux / 2, 0, top + roof_h - 0.12)], [(0, 1, 2)], gable)
    poly([(ux / 2, -uy / 2, top - 0.01), (ux / 2, uy / 2, top - 0.01), (ux / 2, 0, top + roof_h - 0.12)], [(0, 1, 2)], gable)
    box((0.06, 0.09, roof_h * 0.85), (-ux / 2 - 0.03, 0, top), beam)
    box((0.06, uy, 0.09), (-ux / 2 - 0.03, 0, top), beam)
    _window(Face("x", -ux / 2, uy / 2), 0.0, top + roof_h * 0.3, 0.26, 0.3, lit[0], beam)
    if chimney:
        cx, cy = sx * 0.3, sy * 0.1
        box((0.45, 0.45, roof_h + 0.9), (cx, cy, top), mat(STONE[2], 0.35, STONE[1], scale=8))
        box((0.55, 0.55, 0.1), (cx, cy, top + roof_h + 0.9), mat(STONE[1], 0.2))
    return top


def house_1():
    _townhouse(4.0, 2.9, 1.9, 1.6, 2.0, 1, chimney=True)
    from models import barrel
    barrel((-2.3, -0.8, 0), 0.9)
    barrel((-2.35, -1.25, 0), 0.85)


def house_2():
    # Tall and narrow, gable to the square.
    _townhouse(2.6, 3.4, 2.0, 2.0, 2.2, 2, chimney=False, lit=(False, True), door_face="x")


def house_3():
    # A long hall with a lean-to on its right front.
    _townhouse(5.0, 3.0, 1.8, 1.4, 1.8, 3, chimney=True, lit=(True, True, False))
    from models import crate
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=10)
    roofm = mat(STONE[1], 0.4, COLD[0], scale=18)
    for x in (-2.1, -0.5):
        box((0.1, 0.1, 1.4), (x, -2.25, 0), wood)
    poly([(-2.4, -1.55, 1.85), (-0.2, -1.55, 1.85), (-0.2, -2.45, 1.4), (-2.4, -2.45, 1.4)], [(0, 1, 2, 3)], roofm)
    for i in range(3):
        crate((-1.8 + i * 0.5, -2.0, 0), 0.4, turn=i * 15)


def house_4():
    # A stone house whose roof has half fallen in, under scaffolding (the concept's left houses).
    rng = _rng("house4")
    sx, sy, h = 3.4, 3.0, 3.2
    stones = _stone()
    core = mat(STONE[1], 0.3, STONE[0], scale=6)
    frame = mat(STONE[3], 0.3, STONE[2], scale=10)
    beam = mat(WOOD[1], 0.25, WOOD[0], scale=10)
    box((sx, sy, h), (0, 0, 0), core)
    fx = Face("x", -sx / 2, sy / 2)
    fy = Face("y", -sy / 2, sx / 2)
    for f in (fx, fy):
        a, b = f.ends
        _courses(a, b, f.normal, 0.0, h, rng, stones)
    _arch(fy, 0.4, 0.0, 0.7, 1.45, mat(WOOD[1], 0.3, WOOD[0], scale=14), frame)
    _arch(fx, -0.6, 1.6, 0.45, 0.8, None, frame)
    _window(fy, -0.9, 1.9, 0.3, 0.4, True, beam)
    # Half a slate roof, the other half bare rafters.
    slate = mat(STONE[1], 0.4, COLD[0], scale=18)
    hx, hy, rh = sx / 2 + 0.2, sy / 2 + 0.2, 1.8
    poly([(-hx, 0, h + rh), (0.2, 0, h + rh), (0.2, hy, h - 0.05), (-hx, hy, h - 0.05)], [(0, 1, 2, 3)], slate)
    poly([(-hx, -hy, h - 0.05), (-0.6, -hy, h - 0.05), (-0.6, 0, h + rh), (-hx, 0, h + rh)], [(0, 1, 2, 3)], slate)
    for k in range(5):
        x = -0.4 + k * 0.45
        length = math.hypot(hy, rh)
        box((0.07, length, 0.07), (x, -hy / 2, h + rh / 2 - 0.05), beam, rot=(-math.degrees(math.atan2(rh, hy)), 0, 0))
    box((2 * hx, 0.12, 0.1), (0, 0, h + rh - 0.05), beam)
    poly([(-sx / 2, -sy / 2, h), (-sx / 2, sy / 2, h), (-sx / 2, 0, h + rh - 0.1)], [(0, 1, 2)], core)
    # Scaffolding on the right front face: poles, ledgers and planks.
    for x in (-1.5, -0.2, 1.1):
        box((0.07, 0.07, h + 0.6), (x, -sy / 2 - 0.45, 0), beam)
    for z in (1.2, 2.4):
        box((2.8, 0.06, 0.06), (-0.2, -sy / 2 - 0.45, z), beam)
        box((2.8, 0.4, 0.04), (-0.2, -sy / 2 - 0.25, z + 0.06), mat(WOOD[2], 0.3, WOOD[1], scale=14))
    box((0.06, 0.06, 2.8), (-0.85, -sy / 2 - 0.47, 0.1), beam, rot=(0, 50, 0))


def _ruin(key, sx, sy, h):
    rng = _rng("ruin", key)
    stones = _stone()
    core = mat(STONE[1], 0.3, STONE[0], scale=6)
    frame = mat(STONE[3], 0.3, STONE[2], scale=10)
    char = mat(WOOD[0], 0.3, STONE[0], scale=10)
    # Two walls standing (the back ones), the front ones broken low, so the burnt inside shows.
    box((0.35, sy, h), (sx / 2 - 0.17, 0, 0), core)
    box((sx, 0.35, h * 0.85), (0, sy / 2 - 0.17, 0), core)
    for f, top in ((Face("x", sx / 2 - 0.35, sy / 2), h), (Face("y", sy / 2 - 0.35, sx / 2), h * 0.85)):
        a, b = f.ends
        _courses(a, b, f.normal, 0.0, top, rng, stones)
        _arch(f, 0.0, 1.1, 0.6, 1.2, None, frame)
    # The front walls, broken off at uneven heights.
    steps = 6
    for i in range(steps):
        u0 = -sx / 2 + sx * i / steps
        hh = h * rng.uniform(0.15, 0.55) * (1.0 if i < 2 else 0.5)
        box((sx / steps + 0.01, 0.35, hh), (u0 + sx / steps / 2, -sy / 2 + 0.17, 0), core)
    for i in range(steps):
        v0 = -sy / 2 + sy * i / steps
        hh = h * rng.uniform(0.15, 0.6) * (0.4 if i > 3 else 1.0)
        box((0.35, sy / steps + 0.01, hh), (-sx / 2 + 0.17, v0 + sy / steps / 2, 0), core)
    # Charred beams fallen across, and rubble.
    for k in range(4):
        box((sx * 0.8, 0.12, 0.12), (rng.uniform(-0.4, 0.4), rng.uniform(-0.6, 0.6), rng.uniform(0.1, 0.8)), char,
            rot=(rng.uniform(-20, 20), rng.uniform(-25, 25), rng.uniform(-40, 40)))
    for k in range(18):
        r = rng.uniform(0.08, 0.2)
        sphere(r, (rng.uniform(-sx / 2 - 0.3, sx / 2), rng.uniform(-sy / 2 - 0.3, sy / 2), r * 0.3), rng.choice(stones),
               scale=(1.2, 1, 0.6), segments=6, smooth=False)


def ruin_1():
    _ruin(1, 3.6, 3.0, 3.4)


def ruin_2():
    _ruin(2, 2.8, 2.6, 2.8)


def mausoleum():
    """The way down: a stone tomb on a stepped plinth, a porch of four columns under a pediment on its -y face, an open
    iron gate and a dark doorway with steps going down inside, lit red from below; urns and braziers at the steps."""
    from models import flame
    rng = _rng("mausoleum")
    sx, sy, h = 3.6, 3.2, 2.9
    stones = [mat(STONE[3], 0.3, STONE[2], scale=12), mat(STONE[4], 0.3, STONE[2], scale=12), mat(STONE[2], 0.3, STONE[1], scale=12)]
    pale = mat(STONE[4], 0.25, STONE[2], scale=10)
    dark = mat(STONE[0], 0.0)
    core = mat(STONE[2], 0.3, STONE[1], scale=6)
    moss = mat(MOSS[1], 0.4, MOSS[0], scale=10)
    dw, dh = 0.7, 1.75
    front = -sy / 2 + 1.2
    back = sy / 2
    # The plinth: three steps all round and the porch's floor in front, cut where the stair goes down through it.
    for k in range(3):
        g = 0.3 * (3 - k)
        x0, x1 = -sx / 2 - g / 2, sx / 2 + g / 2
        y0, y1 = -sy / 2 - 1.2 - g / 2, back + g / 2
        z = 0.12 * k
        box((x1 - x0, front - y0, 0.12), ((x0 + x1) / 2, (y0 + front) / 2, z), stones[k % 2], bevel=0.01)
        for a, b in ((x0, -dw), (dw, x1)):
            box((b - a, y1 - front, 0.12), ((a + b) / 2, (front + y1) / 2, z), stones[k % 2], bevel=0.01)
        box((2 * dw, y1 - (back - 0.5), 0.12), (0, (back - 0.5 + y1) / 2, z), stones[k % 2])
    base = 0.36
    # The tomb's body behind the porch, built round its doorway: two flanks, a lintel block, the back.
    body_y = (front + back) / 2
    for a, b in ((-sx / 2, -dw), (dw, sx / 2)):
        box((b - a, back - front, h), ((a + b) / 2, body_y, base), core)
    box((2 * dw, back - front, h - dh), (0, body_y, base + dh), core)
    box((2 * dw, 0.5, h), (0, back - 0.25, base), core)
    fx = Face("x", -sx / 2, (back - front) / 2)
    a, b = fx.ends
    _courses((a[0], a[1] + body_y), (b[0], b[1] + body_y), fx.normal, base, base + h, rng, stones, course=0.18)
    fy = Face("y", front, sx / 2)
    for u0, u1 in ((-sx / 2, -dw), (dw, sx / 2)):
        _courses(fy.p(u0)[:2], fy.p(u1)[:2], fy.normal, base, base + h, rng, stones, course=0.18)
    _courses(fy.p(-dw)[:2], fy.p(dw)[:2], fy.normal, base + dh, base + h, rng, stones, course=0.18)
    # The stair: dark sides and back, steps going down through the plinth into the dark, a red glow below.
    for s_ in (-1, 1):
        box((0.04, back - 0.5 - front, base + h), (s_ * (dw - 0.02), (front + back - 0.5) / 2, -1.5), dark)
    box((2 * dw, 0.04, dh + 1.5), (0, back - 0.52, -1.5), dark)
    box((2 * dw, back - 0.5 - front, 0.04), (0, body_y - 0.25, base + dh - 0.04), dark)
    steps = 9
    run = (back - 0.5) - (front - 0.25)
    for k in range(steps):
        top = base - 0.17 * (k + 1)
        y0 = front - 0.25 + run * k / steps
        box((2 * dw - 0.06, run / steps + 0.01, 0.12), (0, y0 + run / steps / 2, top - 0.12), stones[k % 2])
        box((2 * dw - 0.06, 0.03, 0.17), (0, y0, top - 0.02), dark)
    light("POINT", (0, back - 0.9, -0.8), 160, (1.0, 0.35, 0.2), 0.3)
    # Light on the top steps, so the way down reads from the square.
    light("POINT", (0, front + 0.3, base + 0.6), 90, (1.0, 0.45, 0.25), 0.2)
    import themes
    themes._holdout_rect(-dw, dw, front - 0.6, back - 0.5)
    # Door frame and lintel, a skull carved over it.
    fy.box(-dw - 0.14, -dw, base, base + dh + 0.1, 0.12, pale, out=0.06, bevel=0.01)
    fy.box(dw, dw + 0.14, base, base + dh + 0.1, 0.12, pale, out=0.06, bevel=0.01)
    fy.box(-dw - 0.2, dw + 0.2, base + dh + 0.1, base + dh + 0.3, 0.14, pale, out=0.07, bevel=0.01)
    bonem = mat(BONE[1], 0.25, BONE[0], scale=10)
    x, y, _ = fy.p(0.0, 0.12)
    sphere(0.13, (x, y, base + dh + 0.48), bonem, scale=(1, 0.7, 0.9), segments=10)
    for s in (-1, 1):
        sphere(0.035, (x + s * 0.05, y - 0.08, base + dh + 0.5), dark, segments=6)
    iron = mat(STONE[0], 0.2, COLD[0], metal=0.6, rough=0.5)
    # The porch: four columns and a pediment over them.
    py_ = -sy / 2 - 0.1
    top = base + h
    for u in (-1.45, -0.95, 0.95, 1.45):
        cyl(0.13, h - 0.25, (u, py_ + 0.15, base + 0.12), pale, 12)
        box((0.36, 0.36, 0.12), (u, py_ + 0.15, base), stones[0], bevel=0.01)
        box((0.36, 0.36, 0.13), (u, py_ + 0.15, top - 0.13), stones[0], bevel=0.01)
    box((sx + 0.3, 1.5, 0.22), (0, front - 0.6, top), pale, bevel=0.015)
    ped = 0.85
    poly([(-sx / 2 - 0.15, py_ - 0.05, top + 0.22), (sx / 2 + 0.15, py_ - 0.05, top + 0.22), (0, py_ - 0.05, top + 0.22 + ped)], [(0, 1, 2)], pale)
    poly([(-sx / 2 - 0.15, py_ - 0.05, top + 0.22), (-sx / 2 - 0.15, sy / 2, top + 0.22), (0, sy / 2, top + 0.22 + ped), (0, py_ - 0.05, top + 0.22 + ped)],
         [(0, 1, 2, 3)], mat(STONE[1], 0.35, MOSS[0], scale=14))
    poly([(sx / 2 + 0.15, py_ - 0.05, top + 0.22), (0, py_ - 0.05, top + 0.22 + ped), (0, sy / 2, top + 0.22 + ped), (sx / 2 + 0.15, sy / 2, top + 0.22)],
         [(0, 1, 2, 3)], mat(STONE[1], 0.35, MOSS[0], scale=14))
    box((sx, back - front, 0.1), (0, body_y, top), core)
    # A stone angel's pedestal on the apex, moss and cracks.
    box((0.3, 0.3, 0.35), (0, py_ + 0.1, top + 0.22 + ped - 0.15), pale)
    for k in range(6):
        blob_x, blob_y = rng.uniform(-sx / 2, sx / 2), front - 0.02
        box((rng.uniform(0.1, 0.3), 0.02, rng.uniform(0.05, 0.15)), (blob_x, blob_y, base + rng.uniform(0.0, 0.4)), moss)
    # Braziers each side of the steps.
    for s in (-1, 1):
        x0, y0 = s * (sx / 2 + 0.35), -sy / 2 - 0.7
        cyl(0.05, 0.8, (x0, y0, 0), iron, 6)
        lathe([(0.06, 0), (0.22, 0.14), (0.24, 0.2)], (x0, y0, 0.8), iron, 10, smooth=False, cap=False)
        sphere(0.17, (x0, y0, 0.92), mat(EMBER[1], 0.5, EMBER[0], emit=2.5, scale=20), scale=(1, 1, 0.35), segments=8)
        flame((x0, y0, 0.95), 0.16, 8)
    # Urns on the plinth corners.
    clay = mat(STONE[3], 0.3, STONE[2], scale=10)
    for x0 in (-sx / 2 - 0.1, sx / 2 + 0.1):
        lathe([(0.06, 0), (0.14, 0.12), (0.15, 0.3), (0.08, 0.42), (0.11, 0.48)], (x0, sy / 2 - 0.2, base), clay, 10)


def iron_fence(axis):
    iron = mat(STONE[0], 0.2, COLD[0], metal=0.6, rough=0.5)
    stone = mat(STONE[2], 0.3, STONE[1], scale=10)
    length = CELL * 4
    size = (length, 0.18, 0.2) if axis == "x" else (0.18, length, 0.2)
    box(size, (0, 0, 0), stone, bevel=0.01)
    bars = 16
    for k in range(bars + 1):
        t = -length / 2 + length * k / bars
        x, y = (t, 0) if axis == "x" else (0, t)
        tall = 1.15 if k % 4 == 0 else 0.95
        cyl(0.018, tall, (x, y, 0.2), iron, 5)
        cone(0.035, 0.1, (x, y, 0.2 + tall), iron, 4)
    for z in (0.35, 1.0):
        size = (length, 0.03, 0.04) if axis == "x" else (0.03, length, 0.04)
        box(size, (0, 0, z), iron)
    for t in (-length / 2, length / 2):
        x, y = (t, 0) if axis == "x" else (0, t)
        box((0.22, 0.22, 1.3), (x, y, 0), stone, bevel=0.01)
        sphere(0.11, (x, y, 1.38), stone, segments=8)


def iron_fence_x():
    iron_fence("x")


def iron_fence_y():
    iron_fence("y")


def grave_cross():
    rng = _rng("cross")
    stone = mat(STONE[3], 0.35, STONE[2], scale=8)
    moss = mat(MOSS[1], 0.4, MOSS[0], scale=10)
    for k, (x, y, lean) in enumerate(((-0.6, 0.1, 8), (0.5, -0.1, -12))):
        box((0.12, 0.12, 1.0), (x, y, 0), stone, rot=(0, lean, 0), bevel=0.01)
        box((0.5, 0.11, 0.11), (x + math.sin(math.radians(lean)) * 0.72, y, 0.68), stone, rot=(0, lean, 0), bevel=0.01)
        sphere(0.25, (x, y - 0.3, 0.0), mat(EARTH[2], 0.4, EARTH[1], scale=10), scale=(1, 2.2, 0.35), segments=8)
        box((0.2, 0.05, 0.12), (x, y - 0.07, 0.0), moss)


def tomb():
    stone = mat(STONE[3], 0.3, STONE[2], scale=10)
    lid = mat(STONE[4], 0.3, STONE[2], scale=10)
    moss = mat(MOSS[1], 0.4, MOSS[0], scale=10)
    box((1.4, 0.75, 0.55), (0, 0, 0), stone, bevel=0.02)
    box((1.55, 0.88, 0.12), (0, 0, 0.55), lid, rot=(0, 0, 4), bevel=0.02)
    box((0.5, 0.1, 0.25), (-0.3, -0.4, 0.1), moss)
    box((0.3, 0.05, 0.3), (0.3, -0.39, 0.15), mat(STONE[0], 0.0))


def cage_stall():
    """The Pet Vendor's stall (the concept's cages): a sagging dark awning on poles over stacked wooden cages, eyes in
    the dark of some."""
    rng = _rng("cages")
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=12)
    bars = mat(WOOD[2], 0.3, WOOD[1], scale=12)
    canvas = mat(STONE[1], 0.35, EARTH[0], scale=6)
    dark = mat(STONE[0], 0.0)
    eye = mat(EMBER[3], 0.0, emit=3.0)
    straw = mat(EMBER[1], 0.45, WOOD[2], scale=20)
    sx, sy = 3.0, 1.6
    for x in (-sx / 2, sx / 2):
        for y in (-sy / 2, sy / 2):
            box((0.09, 0.09, 2.2 if y > 0 else 1.9), (x, y, 0), wood)
    poly([(-sx / 2 - 0.2, -sy / 2 - 0.4, 1.85), (sx / 2 + 0.2, -sy / 2 - 0.4, 1.85), (sx / 2 + 0.2, sy / 2 + 0.1, 2.25), (0, sy / 2 + 0.1, 2.1),
          (-sx / 2 - 0.2, sy / 2 + 0.1, 2.25)], [(0, 1, 2, 3, 4)], canvas)

    def cage(x, y, z, s):
        box((s, s * 0.8, 0.04), (x, y, z), wood)
        box((s, s * 0.8, 0.04), (x, y, z + s * 0.75), wood)
        box((s - 0.04, s * 0.8 - 0.04, s * 0.75), (x, y, z + 0.04), dark)
        for k in range(6):
            t = -s / 2 + s * k / 5
            cyl(0.012, s * 0.75, (x + t, y - s * 0.4, z + 0.04), bars, 4)
            cyl(0.012, s * 0.75, (x - s / 2, y - s * 0.4 + s * 0.8 * k / 5, z + 0.04), bars, 4)
        if rng.random() < 0.6:
            for s_ in (-1, 1):
                sphere(0.02, (x - s / 2 - 0.01, y - s * 0.1 + s_ * 0.05, z + s * 0.4), eye, segments=6)
        box((s * 0.9, s * 0.7, 0.04), (x, y, z + 0.02), straw)

    for i, x in enumerate((-0.95, 0.0, 0.95)):
        cage(x, 0.1, 0.0, 0.8)
        if i != 1:
            cage(x + 0.05, 0.15, 0.62, 0.62)
    from models import crate
    crate((1.9, -0.6, 0), 0.4, turn=10)


def forge():
    """A smith's open hearth: a brick hearth with glowing coals under a stone hood and chimney, an anvil on a stump, a
    quench barrel and tools."""
    from models import flame, barrel
    brick = [mat(BLOOD[1], 0.35, STONE[1], scale=12), mat(STONE[2], 0.35, BLOOD[0], scale=12)]
    stone = mat(STONE[2], 0.35, STONE[1], scale=8)
    iron = mat(STONE[0], 0.2, COLD[0], metal=0.7, rough=0.4)
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=10)
    rng = _rng("forge")
    box((1.6, 1.2, 0.9), (0.3, 0.4, 0), stone)
    f = Face("y", -0.2, 0.8)
    _courses((-0.5, -0.2), (1.1, -0.2), (0, -1), 0.0, 0.9, rng, brick, course=0.12)
    sphere(0.45, (0.3, 0.45, 0.88), mat(EMBER[2], 0.5, EMBER[0], emit=3.5, scale=20), scale=(1.4, 1, 0.25), segments=10)
    flame((0.25, 0.4, 0.9), 0.14, 8)
    flame((0.45, 0.5, 0.9), 0.1, 8)
    lathe([(0.9, 0), (0.35, 0.9)], (0.3, 0.45, 1.6), stone, 4, smooth=False, rot=(0, 0, 45))
    for x in (-0.45, 1.05):
        box((0.12, 0.12, 0.75), (x, -0.1, 0.9), stone)
    box((0.5, 0.5, 1.6), (0.3, 0.6, 2.4), stone)
    light("POINT", (0.3, 0.1, 1.2), 80, (1.0, 0.5, 0.2), 0.3)
    cyl(0.25, 0.55, (-1.0, -0.6, 0), wood, 8, smooth=False)
    box((0.55, 0.22, 0.2), (-1.0, -0.6, 0.55), iron)
    lathe([(0.1, 0), (0.0, 0.3)], (-0.72, -0.6, 0.65), iron, 6, rot=(0, 90, 0), smooth=False)
    barrel((-1.0, 0.5, 0), 0.85)
    for k in range(3):
        box((0.04, 0.04, 0.7), (1.25 + k * 0.1, -0.35, 0.0), wood, rot=(0, -15 + k * 6, 0))


def torch_post():
    from models import flame
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=10)
    iron = mat(STONE[0], 0.2, COLD[0], metal=0.6, rough=0.5)
    cyl(0.06, 1.9, (0, 0, 0), wood, 6, smooth=False)
    lathe([(0.04, 0), (0.1, 0.12), (0.11, 0.18)], (0, 0, 1.9), iron, 8, smooth=False, cap=False)
    flame((0, 0, 2.0), 0.16, 8)


# ---------------------------------------------------------------- the walled town (2026-10-12, concept 2)
#
# The owner: "Make a wall around the town, space the NPCs further, they should all have their own areas/shops".
#   town_wall_x|y      four cells of crenellated stone wall along x or y
#   town_tower         a square corner tower
#   town_gate_x|y      a gatehouse four cells long: two towers and an open arch between them
#   shop_<kind>        a stall under a dark awning: goods (the merchant), herbs (the healer), lamps (Pellam, lit),
#                      dice (the gambler)
#   training_yard      straw dummies and a weapon rack on a trodden patch (the trainer)
#   vault              iron-bound chests and strongboxes behind a rail (the stash keeper)

WALL_H_TOWN = 1.9
WALL_T = 0.5


def _wall_run(axis, length, h, rng, stones, gap=None):
    """A run of wall along x or y centred on the origin, courses on both long faces, merlons on top. gap is an
    (from, to) stretch along the run left open (a gate's arch)."""
    core = mat(STONE[1], 0.3, STONE[0], scale=6)
    pieces = [(-length / 2, length / 2)] if gap is None else [(-length / 2, gap[0]), (gap[1], length / 2)]
    for a, b in pieces:
        mid, ln = (a + b) / 2, b - a
        size = (ln, WALL_T, h) if axis == "x" else (WALL_T, ln, h)
        box(size, (mid, 0, 0) if axis == "x" else (0, mid, 0), core)
        for side in (-1, 1):
            off = side * WALL_T / 2
            p0 = (a, off) if axis == "x" else (off, a)
            p1 = (b, off) if axis == "x" else (off, b)
            normal = (0, side) if axis == "x" else (side, 0)
            _courses(p0, p1, normal, 0.0, h, rng, stones)
        # Merlons along the top.
        t = a + 0.12
        while t < b - 0.12:
            m = (t + 0.11, 0, h) if axis == "x" else (0, t + 0.11, h)
            box((0.22, WALL_T + 0.04, 0.22) if axis == "x" else (WALL_T + 0.04, 0.22, 0.22), m, rng.choice(stones), bevel=0.01)
            t += 0.42


def town_wall(axis):
    rng = _rng("twall", axis)
    _wall_run(axis, CELL * 4 + 0.02, WALL_H_TOWN, rng, _stone())
    moss = mat(MOSS[1], 0.4, MOSS[0], scale=10)
    for k in range(3):
        t = rng.uniform(-1.2, 1.2)
        box((0.3, 0.02, 0.15) if axis == "x" else (0.02, 0.3, 0.15), (t, -WALL_T / 2 - 0.02, 0.0) if axis == "x" else (-WALL_T / 2 - 0.02, t, 0.0), moss)


def town_wall_x():
    town_wall("x")


def town_wall_y():
    town_wall("y")


def _tower(cx, cy, size, h, rng, stones):
    core = mat(STONE[1], 0.3, STONE[0], scale=6)
    box((size, size, h), (cx, cy, 0), core)
    for f in (Face("x", cx - size / 2, size / 2), Face("y", cy - size / 2, size / 2)):
        a, b = f.ends
        a = (a[0], a[1] + (cy if f.x else 0)) if f.x else (a[0] + cx, a[1])
        b = (b[0], b[1] + (cy if f.x else 0)) if f.x else (b[0] + cx, b[1])
        _courses(a, b, f.normal, 0.0, h, rng, stones)
    for dx in (-1, 1):
        for dy in (-1, 1):
            box((0.26, 0.26, 0.28), (cx + dx * (size / 2 - 0.13), cy + dy * (size / 2 - 0.13), h), rng.choice(stones), bevel=0.01)
    box((size + 0.08, size + 0.08, 0.1), (cx, cy, h - 0.1), mat(STONE[2], 0.3, STONE[1], scale=8))


def town_tower():
    rng = _rng("ttower")
    _tower(0, 0, CELL * 2, WALL_H_TOWN + 0.7, rng, _stone())


def town_gate(axis):
    """A gatehouse along x or y, four cells long (one wall run): two towers and the arch between, open, a portcullis
    raised."""
    from models import flame
    rng = _rng("tgate", axis)
    stones = _stone()
    length = CELL * 4
    arch = CELL * 0.8
    tower = CELL * 1.2
    for side in (-1, 1):
        c = side * (length / 2 - tower / 2)
        _tower(c if axis == "x" else 0, 0 if axis == "x" else c, tower, WALL_H_TOWN + 0.9, rng, stones)
    # The span over the arch.
    core = mat(STONE[1], 0.3, STONE[0], scale=6)
    frame = mat(STONE[3], 0.3, STONE[2], scale=10)
    span = (2 * arch, WALL_T, WALL_H_TOWN - 1.35) if axis == "x" else (WALL_T, 2 * arch, WALL_H_TOWN - 1.35)
    box(span, (0, 0, 1.35), core)
    for k in range(11):
        a = math.pi * k / 10
        u = math.cos(a) * arch
        z = 0.95 + math.sin(a) * arch * 0.5
        box((0.12, WALL_T + 0.06, 0.12) if axis == "x" else (WALL_T + 0.06, 0.12, 0.12), (u, 0, z) if axis == "x" else (0, u, z), frame, bevel=0.01)
    # The portcullis, raised into the arch's top.
    iron = mat(STONE[0], 0.2, COLD[0], metal=0.6, rough=0.5)
    for k in range(7):
        u = -arch + 0.15 + k * (2 * arch - 0.3) / 6
        box((0.03, 0.03, 0.3), (u, -WALL_T / 2 - 0.03, 1.2) if axis == "x" else (-WALL_T / 2 - 0.03, u, 1.2), iron)
    # Torches either side of the arch, on the outer face.
    for side in (-1, 1):
        u = side * (arch + 0.25)
        x, y = (u, -WALL_T / 2 - 0.08) if axis == "x" else (-WALL_T / 2 - 0.08, u)
        cyl(0.03, 0.22, (x, y, 1.1), mat(WOOD[1], 0.3, WOOD[0]), 6)
        flame((x, y, 1.34), 0.08, 7)


def town_gate_x():
    town_gate("x")


def town_gate_y():
    town_gate("y")


def _awning_stall(cloth, key):
    """A stall under a sagging dark awning on four poles, a counter at the front; returns the counter's top height."""
    rng = _rng("stall", key)
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=12)
    sx, sy = 2.2, 1.4
    for x in (-sx / 2, sx / 2):
        for y in (-sy / 2, sy / 2):
            box((0.08, 0.08, 2.1 if y > 0 else 1.85), (x, y, 0), wood)
    poly([(-sx / 2 - 0.2, -sy / 2 - 0.35, 1.8), (sx / 2 + 0.2, -sy / 2 - 0.35, 1.8), (sx / 2 + 0.2, sy / 2 + 0.1, 2.15),
          (-sx / 2 - 0.2, sy / 2 + 0.1, 2.15)], [(0, 1, 2, 3)], cloth)
    # A ragged valance along the front edge.
    for k in range(8):
        x0 = -sx / 2 - 0.2 + (sx + 0.4) * k / 8
        poly([(x0, -sy / 2 - 0.35, 1.8), (x0 + (sx + 0.4) / 8, -sy / 2 - 0.35, 1.8), (x0 + (sx + 0.4) / 16, -sy / 2 - 0.36, 1.62 - 0.05 * (k % 2))],
             [(0, 1, 2)], cloth)
    box((sx, 0.5, 0.85), (0, -sy / 2 + 0.25, 0), wood, bevel=0.01)
    return 0.85, sx, sy, rng


def shop_goods():
    from models import barrel, crate
    cloth = mat(BLOOD[1], 0.35, BLOOD[0], scale=6)
    top, sx, sy, rng = _awning_stall(cloth, "goods")
    clay = mat(SKIN[1], 0.3, SKIN[0])
    for k in range(5):
        lathe([(0.06, 0), (0.1, 0.09), (0.05, 0.18)], (-0.85 + k * 0.4, -sy / 2 + 0.25, top), clay, 10)
    sphere(0.22, (-1.4, -0.9, 0.18), mat(WOOD[3], 0.3, WOOD[2]), scale=(1, 1, 1.1), segments=10)
    barrel((1.4, -0.4, 0), 0.8)
    crate((1.45, 0.3, 0), 0.45, turn=12)
    rug = mat(BLOOD[2], 0.35, BLOOD[0], scale=8)
    box((1.0, 0.7, 0.01), (-0.3, -1.3, 0), rug)


def shop_herbs():
    cloth = mat(MOSS[1], 0.35, MOSS[0], scale=6)
    top, sx, sy, rng = _awning_stall(cloth, "herbs")
    glass = [mat(MOSS[3], 0.2, MOSS[1], rough=0.3), mat(COLD[3], 0.2, COLD[1], rough=0.3), mat(BLOOD[3], 0.2, BLOOD[1], rough=0.3)]
    for k in range(7):
        lathe([(0.04, 0), (0.06, 0.06), (0.03, 0.14)], (-0.95 + k * 0.3, -sy / 2 + 0.25 + rng.uniform(-0.1, 0.1), top), rng.choice(glass), 8)
    herb = mat(MOSS[2], 0.45, MOSS[1], scale=14)
    for k in range(6):
        x = -0.9 + k * 0.36
        box((0.02, 0.02, 0.2), (x, -sy / 2 - 0.25, 1.55), mat(WOOD[1], 0.3, WOOD[0]))
        cone(0.07, 0.22, (x, -sy / 2 - 0.25, 1.35), herb, 6, rot=(180, 0, 0))


def shop_lamps():
    from models import flame
    cloth = mat(EMBER[0], 0.35, WOOD[0], scale=6)
    top, sx, sy, rng = _awning_stall(cloth, "lamps")
    iron = mat(STONE[0], 0.2, COLD[0], metal=0.6, rough=0.5)
    glow = mat(EMBER[4], 0.0, emit=3.5)
    for k in range(4):
        x = -0.8 + k * 0.55
        box((0.16, 0.16, 0.22), (x, -sy / 2 + 0.25, top), glow)
        lathe([(0.12, 0), (0.0, 0.1)], (x, -sy / 2 + 0.25, top + 0.22), iron, 4, smooth=False)
    for k in range(3):
        x = -0.6 + k * 0.6
        box((0.02, 0.02, 0.25), (x, -sy / 2 - 0.25, 1.55), iron)
        box((0.13, 0.13, 0.18), (x, -sy / 2 - 0.25, 1.37), glow)
    light("POINT", (0, -sy / 2 - 0.2, 1.2), 40, (1.0, 0.7, 0.35), 0.3)


def shop_dice():
    from models import barrel
    cloth = mat(VIOLET[0], 0.35, STONE[0], scale=6)
    top, sx, sy, rng = _awning_stall(cloth, "dice")
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=12)
    felt = mat(MOSS[0], 0.3, STONE[0], scale=8)
    box((0.8, 0.8, 0.7), (0.2, -1.5, 0), wood, bevel=0.01)
    box((0.74, 0.74, 0.02), (0.2, -1.5, 0.7), felt)
    bone_m = mat(BONE[2], 0.1, BONE[1])
    for k in range(3):
        box((0.06, 0.06, 0.06), (0.05 + k * 0.12, -1.55 + 0.05 * k, 0.72), bone_m, rot=(0, 0, 20 * k))
    for k in range(4):
        box((0.1, 0.14, 0.004), (0.35 - k * 0.06, -1.35, 0.725), mat(BONE[1], 0.2, BONE[0]), rot=(0, 0, 15 * k))
    for dx, dy in ((-0.4, -1.5), (0.8, -1.5)):
        barrel((dx, dy, 0), 0.6)
    gold = mat(EMBER[3], 0.2, EMBER[1], metal=0.6)
    for k in range(6):
        cyl(0.04, 0.01, (-0.6 + k * 0.08, -sy / 2 + 0.2, top + 0.01 * k), gold, 8)


def training_yard():
    """Straw dummies on posts, a weapon rack and a trodden patch (the trainer)."""
    rng = _rng("yard")
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=12)
    straw = mat(WOOD[2], 0.45, EMBER[0], scale=20)
    sack = mat(WOOD[2], 0.35, WOOD[1], scale=10)
    dirt = mat(EARTH[1], 0.4, EARTH[0], scale=8)
    box((2.6, 2.0, 0.01), (0, 0, 0), dirt)
    for k, (x, y) in enumerate(((-0.8, 0.3), (0.5, -0.2), (0.0, 0.8))):
        cyl(0.05, 1.5, (x, y, 0), wood, 6)
        box((0.7, 0.06, 0.06), (x, y, 1.15), wood)
        sphere(0.2, (x, y, 1.05), straw, scale=(1, 0.8, 1.4), segments=8)
        sphere(0.13, (x, y, 1.5), sack, segments=8)
        if k == 1:
            for n in range(3):
                box((0.4, 0.015, 0.015), (x - 0.2, y - 0.05 + n * 0.03, 1.1 + n * 0.08), mat(WOOD[2], 0.3, WOOD[1]), rot=(0, rng.uniform(-20, 20), rng.uniform(10, 40)))
    iron = mat(STONE[2], 0.2, STONE[1], metal=0.6, rough=0.4)
    box((0.1, 1.2, 0.08), (1.1, 0, 1.0), wood)
    for y in (-0.55, 0.55):
        box((0.08, 0.08, 1.2), (1.1, y, 0), wood)
    for k in range(4):
        box((0.03, 0.03, 1.1), (1.05, -0.4 + k * 0.27, 0.0), iron, rot=(0, -8, 0))


def vault():
    """Iron-bound chests and strongboxes behind a rail (the stash keeper)."""
    wood = mat(WOOD[1], 0.3, WOOD[0], scale=12)
    iron = mat(STONE[0], 0.2, COLD[0], metal=0.6, rough=0.5)
    gold = mat(EMBER[3], 0.2, EMBER[1], metal=0.6)
    for k, (x, y, s) in enumerate(((-0.6, 0.2, 0.55), (0.1, 0.25, 0.5), (0.75, 0.2, 0.45), (-0.25, -0.3, 0.4))):
        box((s * 1.4, s, s * 0.8), (x, y, 0), wood, bevel=0.01)
        for dx in (-0.5, 0.5):
            box((0.04, s + 0.02, s * 0.8 + 0.02), (x + dx * s, y, 0), iron)
        box((0.08, 0.02, 0.1), (x, y - s / 2 - 0.01, s * 0.55), gold)
    box((0.5, 0.4, 0.3), (-0.3, 0.3, 0.44), wood, bevel=0.01)
    for x in (-1.2, 1.2):
        box((0.08, 0.08, 0.9), (x, -0.8, 0), wood)
    box((2.5, 0.06, 0.06), (0, -0.8, 0.8), wood)
    box((2.5, 0.06, 0.06), (0, -0.8, 0.45), wood)


def register(all_pieces):
    for name, build in (("house_1", house_1), ("house_2", house_2), ("house_3", house_3), ("house_4", house_4),
                        ("ruin_1", ruin_1), ("ruin_2", ruin_2), ("mausoleum", mausoleum),
                        ("iron_fence_x", iron_fence_x), ("iron_fence_y", iron_fence_y), ("grave_cross", grave_cross),
                        ("tomb", tomb), ("cage_stall", cage_stall), ("forge", forge), ("torch_post", torch_post),
                        ("town_wall_x", town_wall_x), ("town_wall_y", town_wall_y), ("town_tower", town_tower),
                        ("town_gate_x", town_gate_x), ("town_gate_y", town_gate_y), ("shop_goods", shop_goods),
                        ("shop_herbs", shop_herbs), ("shop_lamps", shop_lamps), ("shop_dice", shop_dice),
                        ("training_yard", training_yard), ("vault", vault)):
        all_pieces[name] = ("world", build, WORLD)
