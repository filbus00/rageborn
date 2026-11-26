# The item pictures' drawing kit (2026-11-26, the owner: "rework all the gear art in menues, it looks bad"): pictures drawn at
# 32 pixels a backpack cell (twice item_art.py's 16), each piece built from shaped parts that are lit as rounded forms from
# the top left (a height from each part's distance to its edge, turned into a normal and shaded in five tones), metals with
# a hard highlight, a coloured outline (the darkest tone of what it borders, not plain black), and a dark line where one part
# lies over another. Plain Python and numpy, no Blender.
import math
import numpy as np

K = np.array([20, 14, 22], np.float32)          # the darkest outline
LIGHT = np.array([-0.62, -0.78, 1.05], np.float32)
LIGHT /= np.linalg.norm(LIGHT)


def hexrgb(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def ramp(base, n=5, warm=True, spread=1.0):
    """Five tones round a base colour: shadows darker and cooler, lights lighter and warmer (hue shifting)."""
    import colorsys
    r, g, b = hexrgb(base) / 255.0
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    out = []
    for k in range(n):
        t = (k - 1.6) / 2.4 * spread          # about -0.67 .. 1.0, base near index 1.6
        lk = min(0.97, max(0.04, l + t * (0.30 if t > 0 else 0.34) * (1.0 if l < 0.8 else 0.5)))
        hk = (h + (-0.025 * t if warm else 0.0)) % 1.0
        sk = min(1.0, max(0.0, s * (1.0 - 0.18 * t) if t > 0 else s * (1.0 + 0.12 * -t)))
        out.append(np.array(colorsys.hls_to_rgb(hk, lk, sk), np.float32) * 255.0)
    return out


def fixed(*hexes):
    return [hexrgb(h) for h in hexes]


# The materials' tones.
M = {
    "wood": ramp("8a5a32"), "darkwood": ramp("5a3420"), "redwood": ramp("8a3a26"), "pale_wood": ramp("b08a5a"),
    "leather": ramp("8a5232"), "dark_leather": ramp("4e2e1e"), "tan": ramp("b88a56"), "red_leather": ramp("7a2a22"),
    "cloth": ramp("5a6e42"), "green": ramp("3e7a3e"), "moss": ramp("6a7a44"), "red": ramp("a82a24"), "crimson": ramp("7a1420"),
    "blue": ramp("2e4a8a"), "navy": ramp("22305a"), "teal": ramp("22a89a"), "violet": ramp("6a3aa8"), "black": ramp("2a2630"),
    "iron": ramp("5a5e6a", warm=False), "steel": ramp("7e8898", warm=False), "bright": ramp("a4aebe", warm=False),
    "gold": ramp("d8a832"), "bronze": ramp("b07a32"), "copper": ramp("c06a36"), "silver": ramp("9aa2b0", warm=False),
    "bone": ramp("d8ccb0"), "ivory": ramp("e8dcc4"), "fur": ramp("9a9088"), "grey_fur": ramp("8a8c92", warm=False),
    "rope": ramp("b0905a"), "parch": ramp("d8c494"), "ember": ramp("e8601e"), "fire": ramp("f0a030"), "ice": ramp("8ac4e8", warm=False),
    "string": fixed("8a8478", "b8b2a4", "d8d2c4", "eee8dc", "fffaf0"), "flint": ramp("7a7e86", warm=False),
    "gem_red": ramp("d0202a"), "gem_teal": ramp("20c8b4"), "gem_violet": ramp("8a40e0"), "gem_blue": ramp("3a7ae8"),
    "gem_green": ramp("30b040"), "glow_teal": ramp("60f0dc"), "glow_violet": ramp("b080ff"), "glow_ember": ramp("ff8a30"),
    "feather_red": ramp("c0302a"), "feather_white": ramp("e0dcd0"), "feather_black": ramp("3a3440"), "feather_green": ramp("3a9a44"),
    "feather_brown": ramp("8a6236"), "skin": ramp("c88a64"),
}
METAL = {"iron", "steel", "bright", "gold", "bronze", "copper", "silver", "flint"}
GEM = {"gem_red", "gem_teal", "gem_violet", "gem_blue", "gem_green", "glow_teal", "glow_violet", "glow_ember", "ice"}


def grid(w, h):
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    return x + 0.5, y + 0.5


class Pic:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.alpha = np.zeros((h, w), bool)
        self.owner = np.full((h, w), -1, np.int32)
        self.dark = np.zeros((h, w, 3), np.float32)        # each pixel's darkest tone, for the outline
        self.parts = 0
        self.X, self.Y = grid(w, h)

    # --- Masks --------------------------------------------------------------------------------------------------------
    def ellipse(self, cx, cy, rx, ry, angle=0.0):
        dx, dy = self.X - cx, self.Y - cy
        c, s = math.cos(angle), math.sin(angle)
        u, v = dx * c + dy * s, -dx * s + dy * c
        return (u / rx) ** 2 + (v / ry) ** 2 <= 1.0

    def rect(self, x0, y0, x1, y1, r=0.0):
        m = (self.X >= x0) & (self.X <= x1) & (self.Y >= y0) & (self.Y <= y1)
        if r > 0:
            for cx, cy in ((x0 + r, y0 + r), (x1 - r, y0 + r), (x0 + r, y1 - r), (x1 - r, y1 - r)):
                corner = ((self.X < x0 + r) if cx < (x0 + x1) / 2 else (self.X > x1 - r)) & \
                         ((self.Y < y0 + r) if cy < (y0 + y1) / 2 else (self.Y > y1 - r))
                m &= ~(corner & ((self.X - cx) ** 2 + (self.Y - cy) ** 2 > r * r))
        return m

    def poly(self, pts):
        x, y = self.X, self.Y
        inside = np.zeros_like(x, bool)
        n = len(pts)
        for i in range(n):
            (ax, ay), (bx, by) = pts[i], pts[(i + 1) % n]
            if ay == by:
                continue
            cross = ((ay > y) != (by > y)) & (x < ax + (y - ay) * (bx - ax) / (by - ay))
            inside ^= cross
        return inside

    def stroke(self, pts, width, width_end=None):
        """A thick line through points (a polyline), its width tapering from width to width_end."""
        width_end = width if width_end is None else width_end
        m = np.zeros((self.h, self.w), bool)
        total = sum(math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1)) or 1.0
        run = 0.0
        for i in range(len(pts) - 1):
            (ax, ay), (bx, by) = pts[i], pts[i + 1]
            seg = math.dist(pts[i], pts[i + 1]) or 1e-6
            dx, dy = bx - ax, by - ay
            t = np.clip(((self.X - ax) * dx + (self.Y - ay) * dy) / (seg * seg), 0, 1)
            px, py = ax + t * dx, ay + t * dy
            along = (run + t * seg) / total
            wd = width + (width_end - width) * along
            m |= (self.X - px) ** 2 + (self.Y - py) ** 2 <= (wd / 2) ** 2
            run += seg
        return m

    def curve(self, p0, p1, p2, n=24):
        """Points along a quadratic Bezier."""
        return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
                 (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in (k / n for k in range(n + 1))]

    # --- Painting -------------------------------------------------------------------------------------------------------
    def part(self, mask, mat, round_=3.0, shine=None, edge=True, bias=0.0, tex=0.0, flat=False, seed=1):
        """Paints a part: mask, its material (a name in M or a ramp), how far in from its edge it rounds (pixels), whether
        it shines (metals and gems by default), a dark line where it lies over another part."""
        tones = M[mat] if isinstance(mat, str) else mat
        mask = mask.copy()
        if not mask.any():
            return mask
        shine = (mat in METAL or mat in GEM) if shine is None and isinstance(mat, str) else bool(shine)
        if flat:
            height = np.zeros(mask.shape, np.float32)
        else:
            d = distance(mask)
            r = max(0.5, round_)
            t = np.clip(d / r, 0, 1)
            height = np.sqrt(np.clip(1 - (1 - t) ** 2, 0, 1)) * r
        gy, gx = np.gradient(height)
        n = np.dstack([-gx, -gy, np.ones_like(gx)])
        n /= np.linalg.norm(n, axis=2, keepdims=True)
        lit = (n @ LIGHT)                                   # about 0.2 .. 1
        # A gentle top-to-bottom fall-off over the whole part, so a flat face still reads lit at its top left.
        ys, xs = np.nonzero(mask)
        y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
        fall = ((self.Y - y0) / max(1, y1 - y0) * 0.55 + (self.X - x0) / max(1, x1 - x0) * 0.45)
        val = lit - fall * 0.45
        # Each part uses its whole ramp: its own lit and shaded ends set the scale, so every form reads round.
        inside = val[mask]
        lo, hi = np.percentile(inside, 4), np.percentile(inside, 97)
        val = (val - lo) / max(1e-3, hi - lo) + bias
        if tex:
            noise = np.vectorize(lambda a, b: hash01(a, b, seed))(xs, ys)
            val[ys, xs] += (noise - 0.5) * tex * 0.9
        if flat:
            val = np.full(val.shape, 0.5 + bias, np.float32)
        idx = np.digitize(val, [0.16, 0.40, 0.68, 0.9]).astype(int)
        if shine:
            spec = (n @ (LIGHT + np.array([0, 0, 1], np.float32)) / np.linalg.norm(LIGHT + np.array([0, 0, 1], np.float32))) ** 18
            idx = np.where((spec > 0.55) & (idx >= 2), 4, idx)
        idx = np.clip(idx, 0, len(tones) - 1)
        colours = np.stack(tones)[idx]
        pid = self.parts
        self.parts += 1
        if edge:
            # Where this part lies over another, its own edge pixels toward that part go dark.
            under = self.alpha & ~mask
            ring = mask & ~erode(mask)
            touching = ring & dilate(under)
            colours[touching] = tones[0]
        self.rgb[mask] = colours[mask]
        self.alpha |= mask
        self.owner[mask] = pid
        self.dark[mask] = tones[0]
        return mask

    def dot(self, x, y, mat, k=4):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            tones = M[mat] if isinstance(mat, str) else mat
            self.rgb[y, x] = tones[min(k, len(tones) - 1)]
            self.alpha[y, x] = True
            self.dark[y, x] = tones[0]

    def line(self, pts, mat, k=0):
        """A one-pixel line in a tone (seams, stitches, grain)."""
        tones = M[mat] if isinstance(mat, str) else mat
        for i in range(len(pts) - 1):
            (ax, ay), (bx, by) = pts[i], pts[i + 1]
            n = int(max(abs(bx - ax), abs(by - ay), 1))
            for s in range(n + 1):
                x, y = int(ax + (bx - ax) * s / n), int(ay + (by - ay) * s / n)
                if 0 <= x < self.w and 0 <= y < self.h and self.alpha[y, x]:
                    self.rgb[y, x] = tones[k]

    def tone_where(self, mask, mat, k):
        tones = M[mat] if isinstance(mat, str) else mat
        mask = mask & self.alpha
        self.rgb[mask] = tones[k]

    def clear(self, mask):
        self.alpha &= ~mask

    def gem(self, cx, cy, r, mat):
        """A cut stone: a round part with a facet line and a white glint."""
        m = self.part(self.ellipse(cx, cy, r, r), mat, round_=r, shine=True)
        self.dot(cx - r * 0.4, cy - r * 0.45, fixed("ffffff"), 0)
        return m

    def rivet(self, x, y, mat="iron"):
        self.dot(x, y, mat, 3)
        self.dot(x + 1, y + 1, mat, 0)

    def finish(self):
        """The outline: every clear pixel beside the picture takes the darkest tone of what it borders, darkened toward
        black; the lit (top left) side lighter than the shaded."""
        out = self.rgb.copy()
        a = self.alpha
        nb = dilate(a) & ~a
        ys, xs = np.nonzero(nb)
        alpha = a.copy()
        for y, x in zip(ys, xs):
            cols = []
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < self.w and 0 <= yy < self.h and a[yy, xx]:
                    cols.append(self.dark[yy, xx])
            if not cols:
                continue
            c = np.mean(cols, axis=0)
            shade = 0.42 if (x < self.w * 0.5 and y < self.h * 0.5) else 0.3
            out[y, x] = K * (1 - shade) + c * shade
            alpha[y, x] = True
        self.rgb, self.alpha = out, alpha
        return self

    def rgba(self):
        out = np.zeros((self.h, self.w, 4), np.uint8)
        out[..., :3] = np.clip(self.rgb, 0, 255).astype(np.uint8)
        out[..., 3] = np.where(self.alpha, 255, 0)
        return out


def hash01(*v):
    h = 2166136261
    for a in v:
        h = ((h ^ (int(a) & 0xffffffff)) * 16777619) & 0xffffffff
    h ^= h >> 13
    h = (h * 0x5bd1e995) & 0xffffffff
    h ^= h >> 15
    return (h & 0xffffff) / float(0x1000000)


def erode(m):
    e = m.copy()
    e[1:, :] &= m[:-1, :]
    e[:-1, :] &= m[1:, :]
    e[:, 1:] &= m[:, :-1]
    e[:, :-1] &= m[:, 1:]
    e[0, :] = e[-1, :] = False
    e[:, 0] = e[:, -1] = False
    return e & m


def dilate(m):
    d = m.copy()
    d[1:, :] |= m[:-1, :]
    d[:-1, :] |= m[1:, :]
    d[:, 1:] |= m[:, :-1]
    d[:, :-1] |= m[:, 1:]
    return d


def distance(mask, limit=12):
    """Each pixel's distance in from the mask's edge, by repeated erosion (1 at the edge)."""
    d = np.zeros(mask.shape, np.float32)
    cur = mask.copy()
    for k in range(1, limit + 1):
        d[cur] = k
        cur = erode(cur)
        if not cur.any():
            break
    # Smooth the steps a little so the light rolls rather than terraces.
    s = d.copy()
    s[1:-1, 1:-1] = (d[1:-1, 1:-1] * 4 + d[:-2, 1:-1] + d[2:, 1:-1] + d[1:-1, :-2] + d[1:-1, 2:]) / 8
    return np.where(mask, s, 0)


def write_png(path, arr):
    import struct, zlib
    h, w = arr.shape[:2]
    raw = b"".join(b"\x00" + arr[y].tobytes() for y in range(h))

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xffffffff)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) +
                chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))
