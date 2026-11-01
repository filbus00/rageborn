# Cuts an AI-drawn animation sheet of one ground pool into aligned frames (2026-11-01, the owner's Gemini fire pool
# sheet): the sheet is split into <cols> x <rows> equal cells (top row first), each keyed off its magenta (as cutout.py);
# the pool is found by its solid columns and rows (stray sparks ignored) and every frame is cut by the pool's own left,
# right and bottom edge, a little taller than wide for the flames, so the frames line up though the AI moved the pool
# about in its cells. Each is scaled to <size> square and written to Assets/_Project/Resources/Art/Effects/<name>_<n>.png
# (n from 1), shown one world unit across like the single pools (TelegraphArt.DrawnPoolFrames).
#   Blender -b -P ArtSource/tools/pool_frames.py -- <sheet> <cols> <rows> <name> [size]
import bpy, sys, os
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
source, cols, rows, name = args[0], int(args[1]), int(args[2]), args[3]
size = int(args[4]) if len(args) > 4 else 128
TALL = 1.12
OUT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_Project", "Resources", "Art", "Effects"))

img = bpy.data.images.load(os.path.abspath(source))
w, h = img.size
full = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]  # top row first

for k in range(cols * rows):
    cy, cx = divmod(k, cols)
    a = full[cy * h // rows:(cy + 1) * h // rows, cx * w // cols:(cx + 1) * w // cols].copy()
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pink = np.minimum(r, b) - g
    alpha = np.clip((0.42 - pink) / 0.2, 0.0, 1.0)
    spill = (pink > 0.03) & (np.abs(r - b) < 0.22) & (alpha < 0.95)
    a[..., 0] = np.where(spill, np.minimum(r, g), r)
    a[..., 2] = np.where(spill, np.minimum(b, g), b)
    shaved = alpha.copy()
    for _ in range(3):
        step = shaved.copy()
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                step = np.minimum(step, np.roll(np.roll(shaved, dy, axis=0), dx, axis=1))
        shaved = step
    a[..., 3] = shaved
    solid = shaved > 0.5
    # The pool's body: columns and rows with a good run of solid pixels (sparks and flame tips are thin).
    colsum, rowsum = solid.sum(axis=0), solid.sum(axis=1)
    xs = np.nonzero(colsum > colsum.max() * 0.25)[0]
    ys = np.nonzero(rowsum > rowsum.max() * 0.25)[0]
    x0, x1, y1 = xs.min(), xs.max() + 1, ys.max() + 1
    width = x1 - x0
    y0 = int(round(y1 - width * TALL))
    # The crop may reach above the cell: pad with clear pixels.
    pad = max(0, -y0)
    if pad:
        a = np.concatenate([np.zeros((pad, a.shape[1], 4), dtype=np.float32), a], axis=0)
        y0 += pad
        y1 += pad
    crop = a[y0:y1, x0:x1]
    pre = crop.copy()
    pre[..., :3] *= pre[..., 3:4]
    iy = (np.arange(size + 1) * crop.shape[0] / size).astype(int)
    ix = (np.arange(size + 1) * crop.shape[1] / size).astype(int)
    out = np.zeros((size, size, 4), dtype=np.float32)
    for j in range(size):
        band = pre[iy[j]:max(iy[j] + 1, iy[j + 1])]
        for i in range(size):
            out[j, i] = band[:, ix[i]:max(ix[i] + 1, ix[i + 1])].reshape(-1, 4).mean(axis=0)
    out[..., :3] = np.clip(out[..., :3] / np.maximum(out[..., 3:4], 1e-4), 0, 1)
    result = bpy.data.images.new("f", size, size, alpha=True)
    result.pixels = out[::-1].ravel()
    result.filepath_raw = os.path.join(OUT, "%s_%d.png" % (name, k + 1))
    result.file_format = "PNG"
    result.save()
    print("REPORT frame", k + 1, "pool", width, "wide at", x0, y1)
