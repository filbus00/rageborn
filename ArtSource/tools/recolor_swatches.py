# Recolours a palette texture's swatches (2026-11-01, her signature crimson): every pixel within a small distance of a
# given colour becomes the new colour, keeping its own light (a shaded swatch stays shaded). Pairs are 0-255 RGB.
#   Blender -b -P ArtSource/tools/recolor_swatches.py -- <in.png> <out.png> r,g,b=r,g,b [r,g,b=r,g,b ...]
# A pair whose source is "white" dims every near-white pixel to the given colour's level (a quiver's pale feathers).
import bpy, sys
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
src, out, pairs = args[0], args[1], args[2:]
im = bpy.data.images.load(src)
w, h = im.size
p = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
rgb = p[..., :3].copy()
for pair in pairs:
    a, b = pair.split("=")
    to = np.array([float(v) for v in b.split(",")], dtype=np.float32) / 255.0
    if a == "white":
        light = rgb.mean(axis=2)
        mask = (light > 0.7) & (rgb.max(axis=2) - rgb.min(axis=2) < 0.15)
        rgb[mask] = rgb[mask] * (to.mean() / np.maximum(light[mask][:, None], 1e-4))
        print("REPORT white", int(mask.sum()))
        continue
    frm = np.array([float(v) for v in a.split(",")], dtype=np.float32) / 255.0
    mask = np.abs(p[..., :3] - frm).max(axis=2) < 0.012
    k = (p[..., :3].mean(axis=2) / max(frm.mean(), 1e-4))[..., None]
    rgb = np.where(mask[..., None], np.clip(to * k, 0, 1), rgb)
    print("REPORT", a, "->", b, int(mask.sum()))
p[..., :3] = rgb
o = bpy.data.images.new("o", w, h, alpha=True)
o.pixels = p.ravel()
o.filepath_raw = out
o.file_format = "PNG"
o.save()
