# (2026-10-27, the owner's separate bones: one picture held several, each cut out by its shape before cutout.py.)
# Keeps only the connected shapes under the given seed points (image coordinates, y down); the rest goes magenta.
#   Blender -b -P isolate.py -- <source> <out.png> x,y [x,y...]
import bpy, sys, numpy as np
from collections import deque
args = sys.argv[sys.argv.index("--") + 1:]
src, out, seeds = args[0], args[1], [tuple(int(v) for v in a.split(",")) for a in args[2:]]
im = bpy.data.images.load(src); w, h = im.size
a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
solid = (np.minimum(a[..., 0], a[..., 2]) - a[..., 1]) < 0.3
keep = np.zeros_like(solid)
for sx, sy in seeds:
    y0, x0 = h - 1 - sy, sx
    if not solid[y0, x0]:
        print("SEED NOT SOLID", sx, sy); continue
    q = deque([(y0, x0)]); keep[y0, x0] = True
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and solid[ny, nx] and not keep[ny, nx]:
                keep[ny, nx] = True; q.append((ny, nx))
# Grow the kept area a few pixels so the soft edge comes with it.
grown = keep.copy()
for _ in range(3):
    g = grown.copy()
    g[1:] |= grown[:-1]; g[:-1] |= grown[1:]; g[:, 1:] |= grown[:, :-1]; g[:, :-1] |= grown[:, 1:]
    grown = g
a[~grown] = (1.0, 0.0, 1.0, 1.0)
ys, xs = np.nonzero(keep)
print("KEPT height", ys.max() - ys.min() + 1, "width", xs.max() - xs.min() + 1)
o = bpy.data.images.new("o", w, h); o.pixels = a.ravel(); o.filepath_raw = out; o.file_format = "PNG"; o.save()
