# Cuts an AI-drawn object out of its flat magenta background and readies it as a world piece (2026-10-27, the owner's
# Gemini trees): keys the magenta away (and its pink fringe from the JPEG), crops to the object, scales it to a height
# in render pixels (4 times the game's), stands it on a small margin with its base at the bottom middle, and writes the
# PNG to ArtSource/pixel/world under each name given, with their entries in manifest.json (pivot at the base of the
# object). Then Tools > ARPG > Import World Art.
#   Blender -b -P ArtSource/tools/cutout.py -- <source image> <height> <name> [<name>...]
import bpy, sys, os, json
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
source, height, names = args[0], int(args[1]), args[2:]
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "pixel", "world"))
MARGIN = 8

img = bpy.data.images.load(os.path.abspath(source))
w, h = img.size
a = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
r, g, b = a[..., 0], a[..., 1], a[..., 2]
# How magenta a pixel is: red and blue well over green.
pink = np.minimum(r, b) - g
alpha = np.clip((0.42 - pink) / 0.2, 0.0, 1.0)
# Pull the pink fringe toward the object's own colour: red and blue down to green's level wherever red and blue both
# stand over green (magenta mixed into the edge; the objects' own colours never do: greens, browns and greys have one of
# red or blue at or under green). 2026-10-27: limiting this to half-keyed pixels left pink edges on the birch's leaves.
spill = pink > 0.03
a[..., 0] = np.where(spill, np.minimum(r, g), r)
a[..., 2] = np.where(spill, np.minimum(b, g), b)
# The JPEG's halo sits in the outermost pixels: shave one off the outline (each pixel takes its darkest neighbour's
# alpha).
shaved = alpha.copy()
for dy in (-1, 0, 1):
    for dx in (-1, 0, 1):
        shaved = np.minimum(shaved, np.roll(np.roll(alpha, dy, axis=0), dx, axis=1))
alpha = shaved
a[..., 3] = alpha

ys, xs = np.nonzero(alpha > 0.5)
y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
crop = a[y0:y1, x0:x1]
ch, cw = crop.shape[:2]
scale = height / ch
nh, nw = height, max(1, int(round(cw * scale)))

# Area-average downscale (premultiplied, so edges do not go dark or pink).
def resize(src, nh, nw):
    sh, sw = src.shape[:2]
    pre = src.copy()
    pre[..., :3] *= pre[..., 3:4]
    out = np.zeros((nh, nw, 4), dtype=np.float32)
    yi = (np.arange(nh + 1) * sh / nh).astype(int)
    xi = (np.arange(nw + 1) * sw / nw).astype(int)
    for j in range(nh):
        rows = pre[yi[j]:max(yi[j] + 1, yi[j + 1])]
        for i in range(nw):
            out[j, i] = rows[:, xi[i]:max(xi[i] + 1, xi[i + 1])].reshape(-1, 4).mean(axis=0)
    rgb = out[..., :3] / np.maximum(out[..., 3:4], 1e-4)
    return np.concatenate([np.clip(rgb, 0, 1), out[..., 3:4]], axis=2)

small = resize(crop, nh, nw)
# Blender's rows run bottom up: the object's base is row 0 of the crop's flipped view.
small = small  # pixels from bpy are already bottom-up
# The base: the middle of the object's lowest rows.
base_rows = small[:max(2, nh // 25)]
cols = np.nonzero(base_rows[..., 3].max(axis=0) > 0.5)[0]
base_x = int(round(cols.mean())) if len(cols) else nw // 2

W, H = nw + 2 * MARGIN, nh + 2 * MARGIN
canvas = np.zeros((H, W, 4), dtype=np.float32)
canvas[MARGIN:MARGIN + nh, MARGIN:MARGIN + nw] = small
out = bpy.data.images.new("cutout", W, H, alpha=True)
out.pixels = canvas.ravel()

manifest_path = os.path.join(ROOT, "manifest.json")
manifest = json.load(open(manifest_path))
items = {it["name"]: it for it in manifest["items"]}
for name in names:
    out.filepath_raw = os.path.join(ROOT, name + ".png")
    out.file_format = "PNG"
    out.save()
    items[name] = {"name": name, "width": W, "height": H, "pivotX": MARGIN + base_x, "pivotY": MARGIN}
    print("REPORT", name, W, "x", H, "pivot", MARGIN + base_x, MARGIN)
json.dump({"items": [items[k] for k in sorted(items)]}, open(manifest_path, "w"), indent=1, sort_keys=True)
