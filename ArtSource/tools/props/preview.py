# A contact sheet of rendered props as the game will show them, for checking without Unity: each PNG shrunk by 4
# (block average), hard alpha, snapped to the palette, outlined (not decals or walls), then enlarged 3x with nearest
# neighbour on a dark floor colour. Run with Blender:
#   Blender -b -P ArtSource/tools/props/preview.py -- <out.png> <png>...
import sys, os, math
import numpy as np
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit import STONE, WOOD, BLOOD, SKIN, MOSS, COLD, EMBER, BONE, VIOLET, EARTH

argv = sys.argv[sys.argv.index("--") + 1:]
out_path, files = argv[0], argv[1:]
PALETTE = np.array(STONE + WOOD + BLOOD + SKIN + MOSS + COLD + EMBER + BONE + VIOLET + EARTH + [(14, 10, 10)], dtype=np.float32)
OUTLINE = np.array([14, 10, 10], dtype=np.float32)
ZOOM = 3


def load(path):
    image = bpy.data.images.load(path)
    w, h = image.size
    px = np.array(image.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(image)
    return px


def to_srgb(v):
    return np.where(v <= 0.0031308, v * 12.92, 1.055 * np.power(np.clip(v, 0, 1), 1 / 2.4) - 0.055)


def process(path):
    px = load(path)  # rows bottom to top, values as stored (PNG sRGB read as float 0..1)
    h, w = px.shape[:2]
    h4, w4 = h // 4, w // 4
    px = px[:h4 * 4, :w4 * 4]
    a = px[:, :, 3:4]
    rgb = (px[:, :, :3] * a).reshape(h4, 4, w4, 4, 3).sum(axis=(1, 3))
    asum = a.reshape(h4, 4, w4, 4, 1).sum(axis=(1, 3))
    rgb = np.where(asum > 0, rgb / np.maximum(asum, 1e-6), 0) * 255
    alpha = (asum[:, :, 0] / 16) >= 0.5
    weights = np.array([0.3, 0.59, 0.11], dtype=np.float32)
    d = (((rgb[:, :, None, :] - PALETTE[None, None, :, :]) ** 2) * weights).sum(axis=3)
    snapped = PALETTE[d.argmin(axis=2)]
    name = os.path.basename(path)
    if not (name.startswith("decal_") or name.startswith("wall") or name.startswith("floor_")):
        grown = np.zeros_like(alpha)
        grown[1:, :] |= alpha[:-1, :]
        grown[:-1, :] |= alpha[1:, :]
        grown[:, 1:] |= alpha[:, :-1]
        grown[:, :-1] |= alpha[:, 1:]
        edge = grown & ~alpha
        snapped[edge] = OUTLINE
        alpha = alpha | edge
    return snapped, alpha


tiles = [process(f) for f in files]
pad = 6
cols = min(len(tiles), 6)
rows = math.ceil(len(tiles) / cols)
cw = max(t[0].shape[1] for t in tiles) * ZOOM + pad
ch = max(t[0].shape[0] for t in tiles) * ZOOM + pad
sheet = np.zeros((rows * ch, cols * cw, 4), dtype=np.float32)
sheet[:, :] = (52 / 255, 46 / 255, 44 / 255, 1)
for i, (rgb, alpha) in enumerate(tiles):
    r, c = divmod(i, cols)
    big = np.repeat(np.repeat(rgb, ZOOM, 0), ZOOM, 1) / 255
    mask = np.repeat(np.repeat(alpha, ZOOM, 0), ZOOM, 1)
    y0 = (rows - 1 - r) * ch
    x0 = c * cw
    region = sheet[y0:y0 + big.shape[0], x0:x0 + big.shape[1]]
    region[mask, :3] = big[mask]
image = bpy.data.images.new("sheet", sheet.shape[1], sheet.shape[0], alpha=True)
image.colorspace_settings.name = "Non-Color"
image.pixels = sheet.ravel()
image.filepath_raw = out_path
image.file_format = "PNG"
image.save()
print("REPORT wrote", out_path)
