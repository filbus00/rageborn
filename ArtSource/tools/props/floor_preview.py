# Lays rendered floor tiles out as the game does (40 x 20 diamonds on the isometric grid, a random variant per cell)
# for each style, enlarged 3x, to check that they join up. Run with Blender:
#   Blender -b -P ArtSource/tools/props/floor_preview.py -- <out.png> <floor folder>
import sys, os, glob, random
import numpy as np
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit import STONE, WOOD, BLOOD, SKIN, MOSS, COLD, EMBER, BONE, VIOLET, EARTH

argv = sys.argv[sys.argv.index("--") + 1:]
out_path, folder = argv[0], argv[1]
PALETTE = np.array(STONE + WOOD + BLOOD + SKIN + MOSS + COLD + EMBER + BONE + VIOLET + EARTH + [(14, 10, 10)], dtype=np.float32)
W, H, N, ZOOM = 40, 20, 7, 3


def tile(path):
    image = bpy.data.images.load(path)
    w, h = image.size
    px = np.array(image.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(image)
    f = w // W
    rgb = px[:H * f, :W * f, :3].reshape(H, f, W, f, 3).mean(axis=(1, 3)) * 255
    d = (((rgb[:, :, None, :] - PALETTE[None, None]) ** 2) * np.array([0.3, 0.59, 0.11])).sum(axis=3)
    rgb = PALETTE[d.argmin(axis=2)] / 255
    ys, xs = np.mgrid[0:H, 0:W]
    inside = np.abs((xs + 0.5) - W / 2) / (W / 2) + np.abs((ys + 0.5) - H / 2) / (H / 2) <= 1.0 + 1e-6
    return rgb, inside


panels = []
for style in ("flagstone", "brick", "earth", "moss"):
    paths = glob.glob(os.path.join(folder, "floor_%s_*.png" % style))
    paths.sort(key=lambda p: int(p.rsplit("_", 1)[1][:-4]))
    tiles = [tile(p) for p in paths]
    if not tiles:
        continue
    pw, ph = W * N, H * N
    panel = np.zeros((ph, pw, 3), dtype=np.float32)
    rng = random.Random(1)
    for cx in range(-N, 2 * N):
        for cy in range(-N, 2 * N):
            ox = (cx - cy) * W // 2 + pw // 2 - W // 2
            oy = (cx + cy) * H // 2 - ph // 2
            # A continuous style (16 pieces) by the cell's place in the 4 x 4 repeat, as DungeonArt.VariantAt.
            rgb, inside = tiles[(cx % 4) + 4 * (cy % 4)] if len(tiles) == 16 else rng.choice(tiles)
            for y in range(H):
                for x in range(W):
                    if inside[y, x]:
                        px, py = ox + x, oy + y
                        if 0 <= px < pw and 0 <= py < ph:
                            panel[py, px] = rgb[y, x]
    panels.append(np.repeat(np.repeat(panel, ZOOM, 0), ZOOM, 1))

sheet = np.concatenate(panels, axis=1)
sheet = np.concatenate([sheet, np.ones(sheet.shape[:2] + (1,), dtype=np.float32)], axis=2)
image = bpy.data.images.new("floors", sheet.shape[1], sheet.shape[0], alpha=True)
image.colorspace_settings.name = "Non-Color"
image.pixels = sheet.ravel()
image.filepath_raw = out_path
image.file_format = "PNG"
image.save()
print("REPORT wrote", out_path)
