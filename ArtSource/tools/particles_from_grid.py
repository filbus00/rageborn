# Cuts an AI-drawn grid of effect particles into one picture each (2026-10-31, the owner's Gemini particles): the source
# is split into <cols> x <rows> equal cells, each keyed off its magenta (as cutout.py), cropped to its particle, padded to
# a square (its shape kept) and area-scaled to <size> pixels. Written to Assets/_Project/Resources/Art/Effects/<name>.png,
# one per cell, top row first, left to right; "-" skips a cell. The game draws each one world unit across, like
# TelegraphArt.Ember, scaled by the particle's size (ArrowFx).
#   Blender -b -P ArtSource/tools/particles_from_grid.py -- <source> <cols> <rows> <size> <name> [<name>...]
import bpy, sys, os
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
source, cols, rows, size, names = args[0], int(args[1]), int(args[2]), int(args[3]), args[4:]
OUT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_Project", "Resources", "Art", "Effects"))

img = bpy.data.images.load(os.path.abspath(source))
w, h = img.size
full = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
# Top row first: Blender's rows run bottom up.
full = full[::-1]

for k, name in enumerate(names):
    if name == "-":
        continue
    cy, cx = divmod(k, cols)
    a = full[cy * h // rows:(cy + 1) * h // rows, cx * w // cols:(cx + 1) * w // cols].copy()
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pink = np.minimum(r, b) - g
    alpha = np.clip((0.42 - pink) / 0.2, 0.0, 1.0)
    spill = (pink > 0.03) & (np.abs(r - b) < 0.22) & (alpha < 0.95)
    a[..., 0] = np.where(spill, np.minimum(r, g), r)
    a[..., 2] = np.where(spill, np.minimum(b, g), b)
    # The glowing ones leave a pink halo: shave the outline by 3 pixels.
    shaved = alpha.copy()
    for _ in range(3):
        step = shaved.copy()
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                step = np.minimum(step, np.roll(np.roll(shaved, dy, axis=0), dx, axis=1))
        shaved = step
    a[..., 3] = shaved
    ys, xs = np.nonzero(shaved > 0.5)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    side = max(y1 - y0, x1 - x0)
    square = np.zeros((side, side, 4), dtype=np.float32)
    oy, ox = (side - (y1 - y0)) // 2, (side - (x1 - x0)) // 2
    square[oy:oy + (y1 - y0), ox:ox + (x1 - x0)] = a[y0:y1, x0:x1]
    pre = square.copy()
    pre[..., :3] *= pre[..., 3:4]
    idx = (np.arange(size + 1) * side / size).astype(int)
    out = np.zeros((size, size, 4), dtype=np.float32)
    for j in range(size):
        band = pre[idx[j]:max(idx[j] + 1, idx[j + 1])]
        for i in range(size):
            out[j, i] = band[:, idx[i]:max(idx[i] + 1, idx[i + 1])].reshape(-1, 4).mean(axis=0)
    out[..., :3] = np.clip(out[..., :3] / np.maximum(out[..., 3:4], 1e-4), 0, 1)
    result = bpy.data.images.new(name, size, size, alpha=True)
    result.pixels = out[::-1].ravel()
    result.filepath_raw = os.path.join(OUT, name + ".png")
    result.file_format = "PNG"
    result.save()
    print("REPORT", name, "from", side)
