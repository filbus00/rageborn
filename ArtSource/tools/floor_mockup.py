# A crypt floor mockup in Diablo 1's style (2026-10-26, the owner: "The floors look bad. Can you try your hand at
# generating a mockup that can have gemini base its suggestion on"): rectangular bricks in staggered rows along the
# grid, dark mortar, each brick bevelled (lit from the upper left, as the game's walls), its own shade of reddish
# grey-brown, a few cracked, chipped or sunk, dust in places. Pixel art at the game's scale (40 pixels a cell), 256
# square so it repeats seamlessly. Writes ArtSource/pixel/ai_exchange/floor_mockup_topdown.png (enlarged 4x for the AI),
# floor_mockup_256.png (game size) and floor_mockup_iso.png (how it looks on screen, tilted).
#   python3 ArtSource/tools/floor_mockup.py
import os, zlib, struct
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "ArtSource", "pixel", "ai_exchange")
N = 256
BRICK_W, BRICK_H = 64, 32  # along x, along y (1.6 by 0.8 cells); both divide 256, so the pattern repeats seamlessly
rng = np.random.default_rng(7)


def png(path, img):
    h, w = img.shape[:2]
    raw = b"".join(b"\x00" + img[y].astype(np.uint8).tobytes() for y in range(h))
    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
    open(path, "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)) +
                           chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def wrap_noise(scale, seed):
    """Smooth value noise that repeats every N pixels (a lattice of N / scale points)."""
    g = N // scale
    r = np.random.default_rng(seed).random((g, g))
    ys, xs = np.mgrid[0:N, 0:N] / scale
    x0, y0 = xs.astype(int), ys.astype(int)
    fx, fy = xs - x0, ys - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a = r[y0 % g, x0 % g]; b = r[y0 % g, (x0 + 1) % g]
    c = r[(y0 + 1) % g, x0 % g]; d = r[(y0 + 1) % g, (x0 + 1) % g]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


# Palette: reddish grey-brown stone (Diablo's cathedral floor), mortar, dust.
STONES = np.array([(96, 78, 72), (88, 72, 68), (102, 84, 76), (82, 70, 66), (94, 78, 76), (90, 76, 70)], float)
MORTAR = np.array((28, 22, 22), float)
DUST = np.array((70, 62, 54), float)

img = np.zeros((N, N, 3), float)
rows = N // BRICK_H
cols = N // BRICK_W
grain = wrap_noise(4, 1)
patch = wrap_noise(64, 2)
for row in range(rows):
    shift = (BRICK_W // 2) * (row % 2)
    for col in range(cols):
        base = STONES[rng.integers(len(STONES))] * rng.uniform(0.9, 1.08)
        sunk = rng.random() < 0.05          # a brick sunk lower, darker
        missing = rng.random() < 0.02       # gone: dirt shows
        cracked = rng.random() < 0.18
        y0 = row * BRICK_H
        x0 = col * BRICK_W + shift
        for dy in range(BRICK_H):
            for dx in range(BRICK_W):
                y = y0 + dy
                x = (x0 + dx) % N
                # Mortar: one pixel on the brick's right and bottom edges, so every brick has a gap round it.
                if dx == BRICK_W - 1 or dy == BRICK_H - 1:
                    img[y, x] = MORTAR
                    continue
                if missing:
                    img[y, x] = DUST * (0.7 + 0.3 * grain[y, x])
                    continue
                c = base * (0.94 + 0.1 * grain[y, x])
                # The bevel: lit on the top and left edges, shaded on the bottom and right.
                if dx == 0 or dy == 0:
                    c = c * 1.22
                elif dx == BRICK_W - 2 or dy == BRICK_H - 2:
                    c = c * 0.7
                if sunk:
                    c = c * 0.72
                img[y, x] = c
        if cracked and not missing:
            # A crack: a jagged dark line across the brick.
            cx = x0 + rng.integers(4, BRICK_W - 6)
            cy = y0 + 1
            while cy < y0 + BRICK_H - 2:
                img[cy, cx % N] = MORTAR * 1.3
                cy += 1
                cx += rng.integers(-1, 2)

# Grime: a slow darkening in patches across the floor.
img = img * (0.88 + 0.18 * patch[..., None])
# Pixel art: brightness in a few steps, the hue kept (snapping each channel apart made blotches of green and pink).
lum = img.mean(axis=2, keepdims=True)
steps = np.round(lum / 8) * 8
img = np.clip(img * steps / np.maximum(lum, 1), 0, 255)

os.makedirs(OUT, exist_ok=True)
png(os.path.join(OUT, "floor_mockup_256.png"), img)
png(os.path.join(OUT, "floor_mockup_topdown.png"), img.repeat(4, 0).repeat(4, 1))

# How it looks on screen: the game draws ground x and y as the two diagonals, 40 pixels a cell across and 20 down.
W, H = 828, 600
iso = np.zeros((H, W, 3), float)
for sy in range(H):
    for sx in range(W):
        wx = (sx - W / 2) / 40.0
        wy = (H / 2 - sy) / 40.0
        gx = wx + 2 * wy
        gy = 2 * wy - wx
        iso[sy, sx] = img[int(np.floor(gy * 40)) % N, int(np.floor(gx * 40)) % N]
png(os.path.join(OUT, "floor_mockup_iso.png"), iso)
print("wrote", OUT)
