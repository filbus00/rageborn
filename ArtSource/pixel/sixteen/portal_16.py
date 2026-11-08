# The portal home, opening (2026-11-08, the owner: "make an animation of a portal opening for 5 sec before you can step
# trough"): in the 16x16 style, each drawn pixel 2 game pixels (the sheet at 13.5 drawn pixels a unit, like the fx).
# Over the opening's frames a ring of runes is drawn on the ground, a slit of light rises from it, the slit widens into a
# blue oval swirling inward and flares as it opens; then the open portal swirls in a loop.
# Writes Assets/_Project/Resources/Art/World/portal_home.png: one row, OPEN frames then LOOP frames, each CW x CH, and a
# preview. HomePortal (Core/PortalChannel.cs) cuts it into frames.
# Run: Blender -b -P ArtSource/pixel/sixteen/portal_16.py
import bpy, os, math, random
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
CW, CH = 36, 48
OPEN, LOOP = 25, 8              # 5 s at 5 frames a second, then the loop
CX, GY = CW / 2, CH - 6         # the middle and the ring's centre row
RING = (15.0, 5.4)              # the ground ring's half width and height
OVAL = (9.6, 17.5)              # the open portal's half width and height
OVAL_Y = GY - 14.0              # its centre
SWIRL = ["0e1638", "1c2e78", "2a52b8", "4a86e8", "86c0ff", "d4f0ff"]
RUNE = ["24306e", "3e5cc8", "7ab4ff", "e8f8ff"]
OUTLINE = "0a0c1e"


def hexc(h, a=1.0):
    return [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [a]


def frame(t, loop_phase=None):
    """t: how far open, 0 to 1; loop_phase: the open loop's turn (0 to 1), or None while opening."""
    img = [[None] * CW for _ in range(CH)]
    rng = random.Random(int((loop_phase if loop_phase is not None else t) * 1000) + 7)
    spin = (loop_phase if loop_phase is not None else t * 2.2) * math.tau
    # The ground ring, drawn round from the front as it opens, its runes pulsing.
    drawn = 1.0 if loop_phase is not None else min(1.0, t / 0.3)
    pulse = 0.5 + 0.5 * math.sin(spin * 2)
    for y in range(CH):
        for x in range(CW):
            dx, dy = (x + 0.5 - CX) / RING[0], (y + 0.5 - GY) / RING[1]
            d = math.hypot(dx, dy)
            if 0.82 <= d <= 1.08:
                a = (math.atan2(dx, dy) / math.tau) % 1.0          # 0 at the front, going round
                if a <= drawn:
                    rune = (int(a * 16) % 2 == 0) and d > 0.92
                    img[y][x] = RUNE[3 if rune and pulse > 0.6 else 2 if rune else 1]
            elif d < 0.82 and drawn >= 1.0:
                # The ring's floor glows faintly once it is closed.
                if (x + y) % 2 == 0 and d < 0.7:
                    img[y][x] = RUNE[0]
    # The slit of light rising, then widening into the swirl.
    rise = 1.0 if loop_phase is not None else max(0.0, min(1.0, (t - 0.25) / 0.2))
    widen = 1.0 if loop_phase is not None else max(0.0, min(1.0, (t - 0.45) / 0.5))
    if rise > 0:
        rx = max(0.6, OVAL[0] * (widen ** 0.8))
        ry = OVAL[1] * (0.25 + 0.75 * rise)
        cy = GY - ry + (OVAL[1] - (GY - OVAL_Y)) * 0 - 1
        cy = GY - 1 - ry
        for y in range(CH):
            for x in range(CW):
                dx, dy = (x + 0.5 - CX) / rx, (y + 0.5 - cy) / ry
                d = math.hypot(dx, dy)
                if d > 1.0:
                    continue
                if rx < 1.2:
                    img[y][x] = SWIRL[5] if abs(dy) < 0.8 else SWIRL[4]
                    continue
                # Spiral bands turning inward, dark in the middle, bright at the rim.
                ang = math.atan2(dy, dx)
                band = (ang / math.tau * 2 + d * 2.4 - spin / math.tau * 2) % 1.0
                k = 1 + int(band * 3) if d > 0.25 else 0
                if d > 0.86:
                    k = 4 if loop_phase is None and t > 0.92 else 4 - (1 if band > 0.5 else 0)
                if d > 0.95:
                    k = 5
                img[y][x] = SWIRL[min(5, k)]
        # A flare as it opens.
        if loop_phase is None and t > 0.9:
            for y in range(CH):
                for x in range(CW):
                    dx, dy = (x + 0.5 - CX) / (rx + 1.5), (y + 0.5 - cy) / (ry + 1.5)
                    if 0.9 < math.hypot(dx, dy) <= 1.0 and img[y][x] is None:
                        img[y][x] = SWIRL[5]
    # Sparks drifting up round it.
    for _ in range(4 if (loop_phase is not None or t > 0.3) else 1):
        a = rng.random() * math.tau
        x = int(CX + math.cos(a) * RING[0] * rng.uniform(0.3, 1.0))
        y = int(GY - rng.uniform(1, 20) * (rise if rise > 0 else 0.2))
        if 0 <= x < CW and 0 <= y < CH and img[y][x] is None:
            img[y][x] = RUNE[3] if rng.random() < 0.5 else SWIRL[4]
    # A dark rim round the light, as every sprite has.
    out = [row[:] for row in img]
    for y in range(CH):
        for x in range(CW):
            if img[y][x] is None:
                for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + ox, y + oy
                    if 0 <= nx < CW and 0 <= ny < CH and img[ny][nx] in SWIRL[1:]:
                        out[y][x] = OUTLINE
                        break
    return out


def rgba(cell):
    a = np.zeros((CH, CW, 4), np.float32)
    for y in range(CH):
        for x in range(CW):
            if cell[y][x] is not None:
                a[y, x] = hexc(cell[y][x])
    return a


def save(arr, path):
    h, w = arr.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = arr[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


frames = [frame(k / (OPEN - 1)) for k in range(OPEN)] + [frame(1.0, k / LOOP) for k in range(LOOP)]
sheet = np.concatenate([rgba(f) for f in frames], 1)
save(sheet, os.path.join(P, "Assets", "_Project", "Resources", "Art", "World", "portal_home.png"))
bg = np.array([0.16, 0.21, 0.13, 1], np.float32)
shown = np.where(sheet[..., 3:4] > 0.5, sheet, bg)
rows = [shown[:, i * CW * 11:(i + 1) * CW * 11] for i in range(3)]
rows = [np.concatenate([r, np.tile(bg, (CH, CW * 11 - r.shape[1], 1))], 1) for r in rows]
save(np.repeat(np.repeat(np.concatenate(rows, 0), 4, 0), 4, 1), os.path.join(P, "ArtSource", "pixel", "previews", "portal_home.png"))
print("wrote portal", len(frames), "frames")
