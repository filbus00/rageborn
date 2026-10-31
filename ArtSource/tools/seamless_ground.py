# Readies an AI-drawn ground picture for GroundPainter (2026-10-27, the owner's Gemini forest grass): makes it repeat
# without a seam (the picture blended with itself shifted by half, the shifted copy taking over toward the edges), area-
# scales it to <size> pixels square (40 pixels a cell in the game) and writes Assets/_Project/Resources/Art/Ground/<name>.png.
#   Blender -b -P ArtSource/tools/seamless_ground.py -- <source image> <name> <size> [clean]
# clean (2026-10-31): fills the picture's near-black spots (holes in the grass, which made its repeats easy to see) with the
# colour around them.
import bpy, sys, os
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
source, name, size = args[0], args[1], int(args[2])
clean = "clean" in args[3:]
OUT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_Project", "Resources", "Art", "Ground"))

img = bpy.data.images.load(os.path.abspath(source))
w, h = img.size
side = min(w, h)
a = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[:side, :side, :3]

def box(img, r):
    # A wrapping box blur of radius r along both axes.
    out = img.copy()
    for axis in (0, 1):
        acc = np.zeros_like(out)
        for d in range(-r, r + 1, max(1, r // 6)):
            acc += np.roll(out, d, axis=axis)
        out = acc / len(range(-r, r + 1, max(1, r // 6)))
    return out

if clean:
    light = a @ np.array([0.3, 0.59, 0.11], dtype=np.float32)
    dark = (light < light.mean() * 0.3).astype(np.float32)
    dark = np.clip(box(dark[..., None], 3)[..., 0] * 3, 0, 1)
    keep = (1 - dark)[..., None]
    around = box(a * keep, 24) / np.maximum(box(keep, 24), 1e-3)
    a = a * keep + around * (1 - keep)
    print("CLEANED", int(dark.sum()), "pixels")

shifted = np.roll(np.roll(a, side // 2, axis=0), side // 2, axis=1)
# The original's weight: 1 in the middle, falling to 0 at every edge (where the shifted copy, whose edges meet, shows).
t = np.abs(np.arange(side) + 0.5 - side / 2) / (side / 2)
edge = np.clip((1.0 - t) / 0.35, 0.0, 1.0)
weight = np.minimum(edge[:, None], edge[None, :])[..., None]
seamless = a * weight + shifted * (1.0 - weight)

# Area-average down to the game's size.
yi = (np.arange(size + 1) * side / size).astype(int)
out = np.zeros((size, size, 4), dtype=np.float32)
for j in range(size):
    rows = seamless[yi[j]:max(yi[j] + 1, yi[j + 1])]
    for i in range(size):
        out[j, i, :3] = rows[:, yi[i]:max(yi[i] + 1, yi[i + 1])].reshape(-1, 3).mean(axis=0)
out[..., 3] = 1.0

result = bpy.data.images.new(name, size, size, alpha=True)
result.pixels = out.ravel()
result.filepath_raw = os.path.join(OUT, name + ".png")
result.file_format = "PNG"
result.save()
print("REPORT", name, size, "from", side)
