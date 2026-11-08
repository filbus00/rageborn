# The pet wolf, redrawn (2026-11-08, the owner: "the wolf, how much better can you make it?", a wolf pack sprite sheet
# as the reference for the level of quality: a long body, a gallop in four poses, soft grey shading, dark separations).
# An original wolf, blue-grey with a pale chest, muzzle and belly, amber eyes and the pet's red harness.
# It is posed as a little model of rounded parts (capsules: a line with a radius at each end) and drawn into pixels one
# by one: each pixel takes the nearest part, lit from the top left in five steps, with a dark outline round it and a
# dark line where a near part crosses a far one. The same pose in every direction, so every clip turns as one.
# Clips the pet plays: idle, walk, run, attack, hit, death; each drawn pixel is 2 game pixels, as every 16x16 character.
# Writes ArtSource/pixel/characters/pet_wolf (Tools > ARPG > Import Drawn Characters) and previews.
# Run: Blender -b -P ArtSource/pixel/sixteen/wolf_16.py
import bpy, os, json, math
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
NAME = "pet_wolf"
SCALE = 2
CW, CH = 34, 22            # the cell, in drawn pixels
GROUND = 18                # the row its paws stand on
SIZE = 0.66                # the model's size: its back about her waist
BG = np.array([0.16, 0.21, 0.13, 1], np.float32)

RAMPS = {
    "fur": ["2e3346", "475069", "687290", "939cb6", "c2c8da"],
    "pale": ["687290", "939cb6", "c2c8da", "e0e4ee", "f4f6fa"],
    "dark": ["1f2232", "2e3346", "3e4560", "566080", "6e7898"],
    "red": ["6a1612", "9e2620", "cc3a2c", "ec5c44", "f4826a"],
    "nose": ["14121c", "1c1a26", "2a2836", "3a3848", "4e4c5e"],
}
OUTLINE = "141320"
EYE = "f2c040"
LIGHT = np.array([-0.55, -0.65, 0.55])
LIGHT /= np.linalg.norm(LIGHT)
ELEV = math.radians(30)
VIEWS = {"s": (0, 1), "se": (1, 1), "e": (1, 0), "ne": (1, -1), "n": (0, -1)}   # forward on the ground: (x, toward camera)
DIRECTIONS = [("s", "s", False), ("sw", "se", True), ("w", "e", True), ("nw", "ne", True), ("n", "n", False),
              ("ne", "ne", False), ("e", "e", False), ("se", "se", False)]


# The model ----------------------------------------------------------------------------------------------------------
STAND = {"fl": (0, 0), "fr": (0, 0), "hl": (18, -28), "hr": (18, -28)}


def leg(o, a, b, l1, l2, side, ramp):
    """A leg from its joint o: the upper part at a degrees from straight down (forward positive), the lower at a + b."""
    def d(deg):
        r = math.radians(deg)
        return np.array([math.sin(r), -math.cos(r), 0.0])
    k = o + l1 * d(a)
    p = k + l2 * d(a + b)
    top = 2.5 if side in "lr" and l1 > 4.2 else 1.9      # the hind legs' heavy thighs
    return [(o, k, top, 1.25, ramp, "leg"), (k, p, 1.2, 1.0, ramp, "leg"), (p, p + np.array([0.8, 0, 0]), 1.05, 0.95, "pale", "paw")]


def model(q):
    """The parts in the wolf's own space: x forward, y up, z to its left. q: the pose."""
    v = lambda *c: np.array(c, float)
    br = 1 + q.get("breathe", 0) * 0.06
    bx, by, pitch = q.get("bx", 0), q.get("by", 0), math.radians(q.get("pitch", 0))
    arch = q.get("arch", 0)

    def body(p):
        # Turned about the middle of the back by the pitch, then moved.
        c = v(0, 9, 0)
        r = p - c
        x = r[0] * math.cos(pitch) - r[1] * math.sin(pitch)
        y = r[0] * math.sin(pitch) + r[1] * math.cos(pitch)
        return c + v(x + bx, y + by, r[2])

    parts = []
    hip, chest = body(v(-6.0, 9.6 + arch, 0)), body(v(5.2, 9.9, 0))
    parts += [(hip, chest, 3.6 * br, 4.4 * br, "fur", "torso"),
              (body(v(-6.4, 10.0 + arch, 0)), body(v(-6.4, 10.0 + arch, 0)), 3.8 * br, 3.8 * br, "fur", "rump"),
              (body(v(5.8, 9.4, 0)), body(v(6.8, 8.4, 0)), 4.2 * br, 3.4 * br, "pale", "chest")]
    # The head on its neck.
    hdx, hdy, hp = q.get("hdx", 0), q.get("hdy", 0), math.radians(q.get("hpitch", 0))
    neck_a = body(v(6.4, 10.6, 0))
    head = body(v(10.0 + hdx, 13.4 + hdy, 0))
    parts.append((neck_a, head, 3.3, 2.8, "fur", "neck"))
    parts.append((head, head, 3.0, 3.0, "fur", "head"))
    for side in (1, -1):
        # The ruff of fur on its cheeks.
        cheek = head + v(-0.4, -0.7, 1.7 * side)
        parts.append((cheek, cheek + v(-0.6, -0.6, 0.3 * side), 2.0, 1.6, "fur", "ruff"))
    s = v(math.cos(hp), -math.sin(hp), 0)       # the snout's way
    down = v(math.sin(hp), math.cos(hp), 0)
    snout_a = head + v(1.8, -0.7, 0)
    snout_b = snout_a + 3.2 * s
    parts.append((snout_a, snout_b, 1.65, 1.05, "pale", "snout"))
    parts.append((snout_b + 0.25 * s, snout_b + 0.25 * s, 0.75, 0.75, "nose", "nose"))
    mouth = q.get("mouth", 0)
    if mouth > 0:
        # The jaw drops open: a lower snout turned down, the inside red.
        r = math.radians(-28 * mouth)
        js = v(s[0] * math.cos(r) - s[1] * math.sin(r), s[0] * math.sin(r) + s[1] * math.cos(r), 0)
        ja = snout_a - 0.9 * down
        parts.append((ja, ja + 2.6 * js, 0.9, 0.6, "pale", "jaw"))
        parts.append((snout_a - 0.4 * down + 0.6 * s, snout_a - 0.5 * down + 2.0 * s, 0.6, 0.4, "red", "mouth"))
    for side in (1, -1):
        ear = head + v(-0.7, 2.3, 1.6 * side)
        parts.append((ear, ear + v(-0.5 + q.get("ears", 0), 3.0, 0.5 * side), 1.3, 0.3, "fur", "ear"))
    # The harness: a band round the chest and a strap over the back.
    parts.append((body(v(3.4, 9.9, 0)), body(v(3.4, 9.9, 0)), 4.45 * br, 4.45 * br, "red", "band"))
    # The tail: up and waving by the pose.
    tu, wag = math.radians(q.get("tail", 0)), q.get("wag", 0)
    t0 = hip + v(-3.2, 1.0, 0)
    t1 = t0 + v(-3.3 * math.cos(tu), 3.3 * math.sin(tu) - 1.2, wag * 0.5)
    t2 = t1 + v(-3.2 * math.cos(tu * 1.3), 3.0 * math.sin(tu * 1.3) - 1.6, wag)
    parts += [(t0, t1, 1.7, 2.4, "fur", "tail"), (t1, t2, 2.4, 1.4, "fur", "tail"), (t2, t2 + (t2 - t1) * 0.35, 1.4, 0.6, "pale", "tail")]
    # Legs: front from the shoulders, hind from the hips (each moves with the body).
    legs = dict(STAND, **q.get("legs", {}))
    for key, joint, l1, l2 in (("fl", v(5.8, 8.0, 2.7), 4.0, 4.3), ("fr", v(5.8, 8.0, -2.7), 4.0, 4.3),
                               ("hl", v(-6.0, 8.6 + arch, 2.9), 4.4, 4.7), ("hr", v(-6.0, 8.6 + arch, -2.9), 4.4, 4.7)):
        a, b = legs[key]
        parts += leg(body(joint), a, b, l1, l2, key[1], "fur")
    eyes = [head + v(1.7, 0.5, 1.55), head + v(1.7, 0.5, -1.55)]
    # Roll (death): turned about its length onto its side.
    roll = math.radians(q.get("roll", 0))
    if roll:
        def turn(p):
            y, z = p[1] - 6, p[2]
            return v(p[0], 6 + y * math.cos(roll) - z * math.sin(roll), y * math.sin(roll) + z * math.cos(roll))
        parts = [(turn(a), turn(b), ra, rb, ramp, kind) for a, b, ra, rb, ramp, kind in parts]
        eyes = [turn(e) for e in eyes]
    # Stand it on the ground (or above it, in a leap).
    low = min(min(a[1] - ra, b[1] - rb) for a, b, ra, rb, ramp, kind in parts)
    lift = q.get("air", 0) - low
    shift = v(0, lift, 0)
    parts = [(a + shift, b + shift, ra, rb, ramp, kind) for a, b, ra, rb, ramp, kind in parts]
    eyes = [e + shift for e in eyes]
    return parts, eyes, q.get("shut", False)


# Drawing ------------------------------------------------------------------------------------------------------------
def project(p, view):
    """The wolf's space to the screen: its forward along the view's way on the ground, seen from 30 degrees up.
    Returns screen x, screen y (down) and nearness (toward the camera)."""
    fx, fz = VIEWS[view]
    n = math.hypot(fx, fz)
    f = np.array([fx / n, 0, fz / n])
    left = np.cross([0, 1, 0], f)
    w = p[0] * f + np.array([0, p[1], 0]) + p[2] * left
    sx = w[0]
    sy = w[2] * math.sin(ELEV) - w[1] * math.cos(ELEV)
    near = w[2] * math.cos(ELEV) + w[1] * math.sin(ELEV)
    return sx, sy, near


def render(q, view, dx=0.0, dy=0.0):
    parts, eyes, shut = model(q)
    parts = [(a * SIZE, b * SIZE, ra * SIZE, rb * SIZE, ramp, kind) for a, b, ra, rb, ramp, kind in parts]
    eyes = [e * SIZE for e in eyes]
    ox, oy = CW / 2 + dx, GROUND + dy
    depth = np.full((CH, CW), -1e9)
    colour = [[None] * CW for _ in range(CH)]
    kinds = [[None] * CW for _ in range(CH)]
    mid = project(np.array([0.0, 9.0 * SIZE, 0.0]), view)[2]
    for a, b, ra, rb, ramp, kind in parts:
        far = kind in ("leg", "paw") and project(a, view)[2] < mid - 0.7
        ax, ay, an = project(a, view)
        bx, by, bn = project(b, view)
        ax, ay, bx, by = ax + ox, ay + oy, bx + ox, by + oy
        rmax = max(ra, rb)
        for py in range(max(0, int(min(ay, by) - rmax - 1)), min(CH, int(max(ay, by) + rmax + 2))):
            for px in range(max(0, int(min(ax, bx) - rmax - 1)), min(CW, int(max(ax, bx) + rmax + 2))):
                cx, cy = px + 0.5, py + 0.5
                ex, ey = bx - ax, by - ay
                ll = ex * ex + ey * ey
                t = 0.0 if ll < 1e-9 else max(0.0, min(1.0, ((cx - ax) * ex + (cy - ay) * ey) / ll))
                qx, qy = ax + ex * t, ay + ey * t
                r = ra + (rb - ra) * t
                d2 = (cx - qx) ** 2 + (cy - qy) ** 2
                if d2 > r * r:
                    continue
                nz = math.sqrt(max(0.0, r * r - d2))
                near = an + (bn - an) * t + nz
                if near <= depth[py, px]:
                    continue
                depth[py, px] = near
                normal = np.array([(cx - qx) / r, (cy - qy) / r, nz / r])
                lit = float(normal @ LIGHT)
                k = 4 if lit > 0.66 else 3 if lit > 0.36 else 2 if lit > 0.02 else 1 if lit > -0.38 else 0
                use = ramp
                # Pale under the body and the neck, the paler fur of the chest and belly.
                if kind in ("torso", "rump", "neck") and normal[1] > 0.55:
                    use = "pale"
                    k = max(0, k - 1)
                # A darker saddle along the back.
                if kind in ("torso", "rump", "neck", "tail") and normal[1] < -0.5:
                    use = "dark"
                    k = min(4, k + 1)
                if kind == "band" and abs(normal[0]) > 0.22 and view not in ("s", "n"):
                    colour[py][px] = None
                    depth[py, px] = -1e9
                    continue
                if far:
                    use, k = ("dark" if use == "fur" else "fur"), max(0, k - 1)
                colour[py][px] = RAMPS[use][k]
                kinds[py][px] = kind
    # The band shows only as a strap where it wraps the chest: drop the parts of it the fur covers anyway.
    # Eyes: amber where the eye point is the front-most thing at its pixel.
    for e in eyes:
        ex, ey, en = project(e, view)
        x, y = int(ex + ox), int(ey + oy)
        if 0 <= x < CW and 0 <= y < CH and en >= depth[y, x] - 1.0 and colour[y][x] is not None:
            colour[y][x] = OUTLINE if shut else EYE
    # Dark lines where a near part crosses a far one, then the outline round it all.
    out = [row[:] for row in colour]
    for y in range(CH):
        for x in range(CW):
            if colour[y][x] is None:
                continue
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < CW and 0 <= ny < CH and colour[ny][nx] is not None and depth[ny, nx] - depth[y, x] > 1.8 \
                        and kinds[ny][nx] in ("leg", "paw", "head", "ear", "tail", "snout", "ruff"):
                    out[y][x] = RAMPS["dark"][0]
                    break
    for y in range(CH):
        for x in range(CW):
            if colour[y][x] is None:
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < CW and 0 <= ny < CH and colour[ny][nx] is not None:
                        out[y][x] = OUTLINE
                        break
    return out


# The clips ----------------------------------------------------------------------------------------------------------
def mix(a, b, t):
    """Two poses blended (numbers and leg angles)."""
    out = {}
    for k in set(a) | set(b):
        va, vb = a.get(k), b.get(k)
        if k == "legs":
            la, lb = dict(STAND, **(va or {})), dict(STAND, **(vb or {}))
            out[k] = {g: tuple(la[g][i] + (lb[g][i] - la[g][i]) * t for i in (0, 1)) for g in STAND}
        elif isinstance(va if va is not None else vb, bool):
            out[k] = va if t < 0.5 else vb
        else:
            va, vb = va or 0, vb or 0
            out[k] = va + (vb - va) * t
    return out


def cycle(keys, frames):
    return [mix(keys[int(i * len(keys) / frames) % len(keys)], keys[(int(i * len(keys) / frames) + 1) % len(keys)],
                (i * len(keys) / frames) % 1) for i in range(frames)]


# The gallop: stretched out in the air, the front feet landing, gathered under it, the hind feet pushing off.
RUN = [
    dict(legs={"fl": (62, -12), "fr": (48, -6), "hl": (-58, 8), "hr": (-46, 4)}, air=1.6, pitch=2, tail=8, ears=-0.6),
    dict(legs={"fl": (14, 0), "fr": (30, -8), "hl": (-22, 46), "hr": (-38, 34)}, air=0.4, pitch=-4, tail=4, ears=-0.6),
    dict(legs={"fl": (-36, 64), "fr": (-26, 54), "hl": (36, -34), "hr": (26, -24)}, air=0, pitch=0, arch=0.8, bx=-0.4, tail=0, ears=-0.6),
    dict(legs={"fl": (-14, 78), "fr": (-4, 66), "hl": (8, -6), "hr": (-8, -2)}, air=0.5, pitch=5, tail=6, ears=-0.6),
]
# The walk: legs moving in diagonal pairs, a passing pose between, the head bobbing a little.
WALK = [
    dict(legs={"fl": (22, 0), "hr": (34, -36), "fr": (-16, 8), "hl": (-2, -18)}, tail=14, hdy=0),
    dict(legs={"fl": (4, 0), "hr": (18, -28), "fr": (6, -40), "hl": (24, -52)}, tail=12, hdy=0.4, by=0.3),
    dict(legs={"fr": (22, 0), "hl": (34, -36), "fl": (-16, 8), "hr": (-2, -18)}, tail=14, hdy=0),
    dict(legs={"fr": (4, 0), "hl": (18, -28), "fl": (6, -40), "hr": (24, -52)}, tail=12, hdy=0.4, by=0.3),
]
IDLE = [dict(tail=22, wag=0), dict(tail=22, wag=1.2, breathe=1), dict(tail=22, wag=0, breathe=1), dict(tail=22, wag=-1.2, shut=True)]
ATTACK = [
    dict(bx=-1.4, pitch=-4, hdy=-1.2, hpitch=12, legs={"fl": (-10, 10), "fr": (-6, 6), "hl": (34, -40), "hr": (30, -36)}, tail=10, ears=-0.8),
    dict(bx=2.6, pitch=6, hdx=0.8, mouth=1.0, legs={"fl": (48, -10), "fr": (36, -4), "hl": (-36, 4), "hr": (-28, 0)}, air=0.8, tail=4, ears=-0.8),
    dict(bx=2.8, pitch=2, hdx=0.8, mouth=0.0, hpitch=6, legs={"fl": (20, 0), "fr": (12, 0), "hl": (-20, -10), "hr": (-14, -12)}, tail=6, ears=-0.6),
    dict(bx=1.2, legs={"fl": (6, 0), "fr": (4, 0)}, tail=14),
    dict(tail=20),
]
HIT = [dict(bx=-1.0, pitch=-6, hdy=0.6, shut=True, legs={"fl": (-14, 6), "fr": (-8, 4)}, tail=-8, ears=-1.0), dict(tail=16)]
DEATH = [
    dict(bx=-1.0, pitch=-6, shut=True, tail=-6, ears=-1.0, legs={"fl": (-10, 4)}),
    dict(roll=22, shut=True, tail=-10, legs={"fl": (20, -30), "fr": (14, -20), "hl": (30, -50), "hr": (26, -40)}, hdy=-1.0),
    dict(roll=55, shut=True, tail=-14, legs={"fl": (30, -20), "fr": (24, -14), "hl": (36, -30), "hr": (30, -24)}, hdy=-1.4),
    dict(roll=86, shut=True, tail=-16, legs={"fl": (40, -10), "fr": (30, -6), "hl": (-30, 10), "hr": (-20, 6)}, hdy=-1.6, air=0.6),
    dict(roll=92, shut=True, tail=-16, legs={"fl": (44, -8), "fr": (34, -4), "hl": (-34, 12), "hr": (-24, 8)}, hdy=-1.8),
    dict(roll=92, shut=True, tail=-16, legs={"fl": (44, -8), "fr": (34, -4), "hl": (-34, 12), "hr": (-24, 8)}, hdy=-1.8, breathe=-1),
]
CLIPS = {
    "idle": IDLE,
    "walk": cycle(WALK, 8),
    "run": cycle(RUN, 6),
    "attack": ATTACK,
    "hit": HIT,
    "death": DEATH,
}
TIMING = {"idle": (1.6, 0), "walk": (0.9, 1.4), "run": (0.4, 3.6), "attack": (0.8, 0), "hit": (0.25, 0), "death": (0.9, 0)}


# Output -------------------------------------------------------------------------------------------------------------
def rgba(cell, mirror=False):
    out = np.zeros((CH, CW, 4), np.float32)
    for y in range(CH):
        for x in range(CW):
            c = cell[y][CW - 1 - x] if mirror else cell[y][x]
            if c is not None:
                out[y, x] = [int(c[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1]
    return out


def up(img, k):
    return np.repeat(np.repeat(img, k, 0), k, 1)


def on_bg(img):
    return np.where(img[..., 3:4] > 0.5, img, BG)


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


drawn = {view: {clip: [render(q, view) for q in poses] for clip, poses in CLIPS.items()} for view in VIEWS}
OUT = os.path.join(P, "ArtSource", "pixel", "characters", NAME)
os.makedirs(OUT, exist_ok=True)
for old in os.listdir(OUT):
    if old.endswith(".png"):
        os.remove(os.path.join(OUT, old))
animations = []
for clip in CLIPS:
    rows = [np.concatenate([rgba(f, mirror) for f in drawn[view][clip]], 1) for _, view, mirror in DIRECTIONS]
    save(up(np.concatenate(rows, 0), SCALE), os.path.join(OUT, f"{NAME}_{clip}.png"))
    seconds, speed = TIMING[clip]
    animations.append({"name": clip, "frames": len(CLIPS[clip]), "seconds": seconds, "speed": speed})
with open(os.path.join(OUT, "sheet.json"), "w") as fh:
    json.dump({"cellWidth": CW * SCALE, "cellHeight": CH * SCALE, "pivotX": CW * SCALE // 2,
               "pivotY": (CH - GROUND - 1) * SCALE + SCALE // 2, "directions": 8, "animations": animations,
               "bakedScale": 0.0}, fh)

# Previews: every clip in all 8 directions, 3 times; and the run in the side and front views, 6 times.
width = max(len(p) for p in CLIPS.values()) * CW
strips = []
for clip in CLIPS:
    for _, view, mirror in DIRECTIONS:
        strip = np.concatenate([on_bg(rgba(f, mirror)) for f in drawn[view][clip]], 1)
        strips.append(np.concatenate([strip, np.tile(BG, (CH, width - strip.shape[1], 1))], 1))
    strips.append(np.tile(np.array([0.08, 0.1, 0.06, 1], np.float32), (2, width, 1)))
save(up(np.concatenate(strips, 0), 3), os.path.join(P, "ArtSource", "pixel", "previews", "pet_wolf_moves.png"))
close = [np.concatenate([on_bg(rgba(f)) for f in drawn[view][clip]] + [np.tile(BG, (CH, (8 - len(CLIPS[clip])) * CW, 1))], 1)
         for view, clip in (("e", "run"), ("se", "run"), ("s", "run"), ("e", "walk"), ("e", "attack"), ("e", "death"))]
save(up(np.concatenate(close, 0), 5), os.path.join(P, "ArtSource", "pixel", "previews", "pet_wolf_close.png"))
print("wrote", NAME, len(animations), "clips")
