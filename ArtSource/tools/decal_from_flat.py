# Lays an AI-drawn floor mark onto a floor cell (2026-10-27, the owner's Gemini floor marks): the picture is drawn flat
# from above on magenta; this keys the magenta away (as cutout.py), crops to the mark, centres it in a square, and maps
# that square onto the cell's diamond as the game's camera sees it (160 x 80 render pixels, 4 times the game's 40 x 20,
# the same mapping as the carpet: RugRules.ImagePoint), four samples a pixel. Writes ArtSource/pixel/dungeon/<name>.png
# for Import Pixel Art (decal_<kind>_<n>).
#   Blender -b -P ArtSource/tools/decal_from_flat.py -- <source image> <name> [fill]
# fill: how much of the cell the mark covers across, 0 to 1 (default 0.9).
import bpy, sys, os
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
source, name = args[0], args[1]
fill = float(args[2]) if len(args) > 2 else 0.9
OUT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "pixel", "dungeon"))
W, H = 160, 80

img = bpy.data.images.load(os.path.abspath(source))
w, h = img.size
a = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
r, g, b = a[..., 0], a[..., 1], a[..., 2]
pink = np.minimum(r, b) - g
alpha = np.clip((0.42 - pink) / 0.2, 0.0, 1.0)
spill = (pink > 0.03) & (np.abs(r - b) < 0.12)
a[..., 0] = np.where(spill, np.minimum(r, g), r)
a[..., 2] = np.where(spill, np.minimum(b, g), b)
shaved = alpha.copy()
for dy in (-1, 0, 1):
    for dx in (-1, 0, 1):
        shaved = np.minimum(shaved, np.roll(np.roll(alpha, dy, axis=0), dx, axis=1))
a[..., 3] = shaved

ys, xs = np.nonzero(shaved > 0.5)
y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
side = max(y1 - y0, x1 - x0)
square = np.zeros((side, side, 4), dtype=np.float32)
oy, ox = (side - (y1 - y0)) // 2, (side - (x1 - x0)) // 2
square[oy:oy + (y1 - y0), ox:ox + (x1 - x0)] = a[y0:y1, x0:x1]

out = np.zeros((H, W, 4), dtype=np.float32)
subs = [(0.25, 0.25), (0.75, 0.25), (0.25, 0.75), (0.75, 0.75)]
for y in range(H):
    for x in range(W):
        acc = np.zeros(4, dtype=np.float32)
        for sx, sy in subs:
            dx = (x + sx - W * 0.5) / W
            dy = (y + sy - H * 0.5) / W
            u, v = dx + 2 * dy, 2 * dy - dx
            if abs(u) > 0.5 or abs(v) > 0.5:
                continue
            # The mark covers `fill` of the cell, centred.
            pu, pv = u / fill + 0.5, v / fill + 0.5
            if not (0.0 <= pu < 1.0 and 0.0 <= pv < 1.0):
                continue
            px = square[int(pv * side), int(pu * side)]
            acc[:3] += px[:3] * px[3]
            acc[3] += px[3]
        if acc[3] > 0:
            out[y, x, :3] = acc[:3] / acc[3]
            out[y, x, 3] = acc[3] / len(subs)

result = bpy.data.images.new("decal", W, H, alpha=True)
result.pixels = out.ravel()
result.filepath_raw = os.path.join(OUT, name + ".png")
result.file_format = "PNG"
result.save()
print("REPORT", name, W, "x", H, "from", side, "square")
