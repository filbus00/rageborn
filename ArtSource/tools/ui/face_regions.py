# The regions of a base face for the archers' portraits (2026-11-07, the owner: "auto generate different looks ... so
# that no amazon is identical"; Bases + variations): reads ArtSource/ui/faces/<base>.png and its .json (the face's
# polygons, the neck, the hair and the cloak, the irises, as flat x, y lists) and writes <base>_regions.png, each pixel's region in its red
# channel (0 none, 1 skin, 2 hair, 3 iris, 4 cloak) and how much it takes in its green, for PortraitPainter to
# recolour.
#   Blender -b -P ArtSource/tools/ui/face_regions.py -- ArtSource/ui/faces/archer_1
import bpy, sys, json, numpy as np

base = sys.argv[sys.argv.index("--") + 1]
spec = json.load(open(base + ".json"))
for key in ("face", "neck", "hair", "cloak", "eyes"):
    flat = spec[key]
    spec[key] = [flat[i:i + 2] for i in range(0, len(flat), 2)]
im = bpy.data.images.load(base + ".png")
w, h = im.size
p = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1, :, :3]  # top row first
r, g, b = p[..., 0], p[..., 1], p[..., 2]
light = r * 0.3 + g * 0.55 + b * 0.15
ys, xs = np.mgrid[0:h, 0:w] + 0.5


def inside(poly):
    poly = np.array(poly, np.float32)
    hit = np.zeros((h, w), bool)
    j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]
        xj, yj = poly[j]
        cross = ((yi > ys) != (yj > ys)) & (xs < (xj - xi) * (ys - yi) / (yj - yi + 1e-9) + xi)
        hit ^= cross
        j = i
    return hit


face = inside(spec["face"])
neck = inside(spec["neck"])
hair_area = inside(spec["hair"])
warm = (r > g) & (g >= b * 0.9)
# The cloak: green inside its polygon (the backdrop is dark green too).
green = (g > r * 1.02) & (g > b) & inside(spec["cloak"])
# How much of a pixel the recolouring takes (the green channel): all of it, but the cloak fades out into the dark
# backdrop.
strength = np.ones((h, w), np.float32)

region = np.zeros((h, w), np.uint8)
region[green] = 4
strength[green] = np.clip((light[green] - 0.05) / 0.08, 0, 1)
region[hair_area & ~face & ~neck & warm & (light > 0.03)] = 2
# Skin: the face and the neck where they are warm (shadow too: by colour alone shaded skin is dark hair). A light warm
# pixel in the hair stays hair: taken for an ear, a lit strand came out skin.
region[(face | neck) & warm & (light > 0.05)] = 1
# The irises: the darker pixels in a small box round each eye's centre.
for ex, ey in spec["eyes"]:
    box = (np.abs(xs - (ex + 0.5)) <= 2.2) & (np.abs(ys - (ey + 0.5)) <= 1.6)
    region[box & (light < 0.42)] = 3

out = np.zeros((h, w, 4), np.float32)
out[..., 0] = region / 255.0
out[..., 1] = strength
out[..., 3] = 1.0
img = bpy.data.images.new("regions", w, h, alpha=True)
img.colorspace_settings.name = "Non-Color"
img.pixels = out[::-1].ravel()
img.filepath_raw = base + "_regions.png"
img.file_format = "PNG"
img.save()
# A look at the regions (skin red, hair yellow, iris cyan, cloak blue over the face), when a second path is given.
args = sys.argv[sys.argv.index("--") + 1:]
if len(args) > 1:
    tint = {1: (1, 0, 0), 2: (1, 1, 0), 3: (0, 1, 1), 4: (0, 0, 1)}
    view = p.copy()
    for k, c in tint.items():
        view[region == k] = view[region == k] * 0.4 + np.array(c) * 0.6
    look = bpy.data.images.new("look", w, h)
    look.pixels = np.concatenate([view, np.ones((h, w, 1), np.float32)], 2)[::-1].ravel()
    look.filepath_raw = args[1]
    look.file_format = "PNG"
    look.save()
print("REPORT", {k: int((region == v).sum()) for k, v in {"skin": 1, "hair": 2, "iris": 3, "cloak": 4}.items()})
