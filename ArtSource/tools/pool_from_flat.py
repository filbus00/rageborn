# Readies an AI-drawn round ground effect for the game's pools (2026-10-31, the owner's Gemini pools): keys the magenta
# away (as cutout.py), crops to the shape, stretches it to fill a square of <size>
# pixels and writes Assets/_Project/Resources/Art/Effects/<name>.png. The game draws it one world unit across, scaled to
# the pool's radius and squashed to the ground (GroundMarker), like TelegraphArt.Pool.
#   Blender -b -P ArtSource/tools/pool_from_flat.py -- <source image> <name> [size]
import bpy, sys, os
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
source, name = args[0], args[1]
size = int(args[2]) if len(args) > 2 else 128
OUT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_Project", "Resources", "Art", "Effects"))

img = bpy.data.images.load(os.path.abspath(source))
w, h = img.size
a = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
r, g, b = a[..., 0], a[..., 1], a[..., 2]
pink = np.minimum(r, b) - g
alpha = np.clip((0.42 - pink) / 0.2, 0.0, 1.0)
spill = (pink > 0.03) & (np.abs(r - b) < 0.22) & (alpha < 0.95)
a[..., 0] = np.where(spill, np.minimum(r, g), r)
a[..., 2] = np.where(spill, np.minimum(b, g), b)
# The pools glow, and the glow leaves a pink halo a few pixels wide: their outline is shaved by 5 pixels.
shaved = alpha.copy()
for _ in range(5):
    step = shaved.copy()
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            step = np.minimum(step, np.roll(np.roll(shaved, dy, axis=0), dx, axis=1))
    shaved = step
a[..., 3] = shaved

ys, xs = np.nonzero(shaved > 0.5)
y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
# Stretched to a circle: the AI draws pools as ellipses seen at a slant, and the game squashes them to the ground itself.
crop = a[y0:y1, x0:x1]
pre = crop.copy()
pre[..., :3] *= pre[..., 3:4]
iy = (np.arange(size + 1) * (y1 - y0) / size).astype(int)
ix = (np.arange(size + 1) * (x1 - x0) / size).astype(int)
out = np.zeros((size, size, 4), dtype=np.float32)
for j in range(size):
    rows = pre[iy[j]:max(iy[j] + 1, iy[j + 1])]
    for i in range(size):
        out[j, i] = rows[:, ix[i]:max(ix[i] + 1, ix[i + 1])].reshape(-1, 4).mean(axis=0)
out[..., :3] = np.clip(out[..., :3] / np.maximum(out[..., 3:4], 1e-4), 0, 1)
side = (x1 - x0, y1 - y0)

result = bpy.data.images.new(name, size, size, alpha=True)
result.pixels = out.ravel()
result.filepath_raw = os.path.join(OUT, name + ".png")
result.file_format = "PNG"
result.save()
print("REPORT", name, size, "from", side)
