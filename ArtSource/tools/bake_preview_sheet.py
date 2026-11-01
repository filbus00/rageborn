# Lays the Wild Arrow bake preview side by side (2026-11-01): for each outfit (a chest look, a helm or none, the quiver
# and the bow), the before and after sheets stacked layer on layer, one frame of a clip in all 8 directions, enlarged 4
# times on a dark forest green. Reads Assets/_Project/Art/SpriteBakeTest/WildArrowPreview (Tools > ARPG > Sprite Bake >
# Bake Wild Arrow Preview), uncropped sheets of 80 pixel cells, directions in rows from the top.
#   Blender -b -P ArtSource/tools/bake_preview_sheet.py -- <out.png> <clip> <frame>
import bpy, sys, os
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
out, clip, frame = args[0], args[1], int(args[2])
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_Project", "Art", "SpriteBakeTest", "WildArrowPreview"))
CELL, DIRS, ZOOM = 80, 8, 4
OUTFITS = [("bare", None), ("ranger", "hood"), ("brigand", None), ("scale", "barbute"), ("plate", "visored"), ("knight", "horned")]
BG = np.array([0.16, 0.21, 0.13, 1.0], dtype=np.float32)


def sheet(name):
    path = os.path.join(ROOT, name.split("_body_")[0].split("_helm_")[0].split("_offhand_")[0].split("_weapon_")[0], name + ".png")
    if not os.path.exists(path):
        return None
    im = bpy.data.images.load(path)
    w, h = im.size
    return np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]  # top row first


def cell(img, row, col):
    return img[row * CELL:(row + 1) * CELL, col * CELL:(col + 1) * CELL]


def over(dst, src):
    a = src[..., 3:4]
    dst[..., :3] = src[..., :3] * a + dst[..., :3] * (1 - a)
    return dst


rows = []
for chest, helm in OUTFITS:
    for who in ("preview_before", "preview_after"):
        layers = [f"{who}_body_{chest}_1h_{clip}"]
        if helm:
            layers.append(f"{who}_helm_{helm}_1h_{clip}")
        layers += [f"{who}_offhand_hide_quiver_1h_{clip}", f"{who}_weapon_hunting_bow_1h_{clip}"]
        sheets = [s for s in (sheet(n) for n in layers) if s is not None]
        strip = []
        for d in range(DIRS):
            c = np.tile(BG, (CELL, CELL, 1))
            for s in sheets:
                f = min(frame, s.shape[1] // CELL - 1)
                c = over(c, cell(s, d, f))
            strip.append(c)
        rows.append(np.concatenate(strip, axis=1))
grid = np.concatenate(rows, axis=0)
grid = grid.repeat(ZOOM, axis=0).repeat(ZOOM, axis=1)
img = bpy.data.images.new("preview", grid.shape[1], grid.shape[0], alpha=False)
img.pixels = grid[::-1].ravel()
img.filepath_raw = out
img.file_format = "PNG"
img.save()
print("REPORT", out, grid.shape[1], "x", grid.shape[0])
