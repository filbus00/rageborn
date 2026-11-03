# Livens a forest enemy's texture (2026-11-03, the owner: "the enemies look really washed out, gray and colorless ...
# redoing the textures on the bandits so that they look a bit more lively and not just like washed out blobs"). The
# Gemini pictures are painted in grey-olive tones, and the projection's blurred back (project_texture.py) greys them
# further, so at 92 px a figure is one dark grey-brown blob. This regrades the texture by hue, in HSV:
#   - dull olive and grey-brown (leather, cloth) turned toward a warm leather brown, or a deep green where the look says
#     green (the bowman's cloak), and made about twice as saturated;
#   - reds (bandanas, sashes, the King's loincloth) made a clear crimson;
#   - golds and brass made gold;
#   - near-greys left grey (steel, fur), only given contrast;
#   - every value lifted and given contrast, so dark parts stay dark and light parts read.
# The original texture stays in ArtSource/models/forest/<shape>/albedo.png; this writes the game's copy.
#   Blender -b -P ArtSource/tools/liven_texture.py -- <in.png> <out.png> <look>
# Looks: cutthroat, bowman, king (the table below).
import bpy, sys, os
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
src, out, look = args[0], args[1], args[2]

# Per look: the hue (degrees) leather and cloth are pulled to, how far (0 to 1), the saturation gain, whether greens are
# kept green (the hue they go to), the value lift and contrast.
LOOKS = {
    # A brown leather outlaw with a crimson bandana and belts.
    "cutthroat": dict(leather=26, pull=0.75, sat=2.2, green=None, lift=1.5, contrast=1.35, red=2.6),
    # A forest green hood and cloak over brown leather, a red sash.
    "bowman": dict(leather=28, green_from=40, pull=0.8, sat=2.2, green=105, lift=1.5, contrast=1.35, red=2.4),
    # Steel and grey fur kept, warm leather, a deep red cloth, gold.
    "king": dict(leather=27, pull=0.6, sat=2.0, green=None, lift=1.4, contrast=1.35, red=2.4),
}
cfg = LOOKS[look]

img = bpy.data.images.load(os.path.abspath(src))
w, h = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
rgb = px[..., :3]

# RGB to HSV.
mx = rgb.max(axis=2)
mn = rgb.min(axis=2)
d = mx - mn
v = mx
s = np.where(mx > 1e-5, d / np.maximum(mx, 1e-5), 0.0)
r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
hue = np.zeros_like(mx)
nz = d > 1e-5
rm = nz & (mx == r)
gm = nz & (mx == g) & ~rm
bm = nz & ~rm & ~gm
hue[rm] = ((g - b)[rm] / d[rm]) % 6
hue[gm] = (b - r)[gm] / d[gm] + 2
hue[bm] = (r - g)[bm] / d[bm] + 4
hue *= 60.0

# How grey a pixel is: steel and grey fur (below about 0.07) stay grey.
colourful = np.clip((s - 0.05) / 0.08, 0.0, 1.0)

# Reds: saturated and not bright, so skin (pinkish, light, less saturated) is not taken for a bandana.
red = ((hue < 11) | (hue > 335)) & (s > 0.3) & ~((v > 0.55) & (s < 0.5))
gold = (hue >= 38) & (hue < 58) & (s > 0.32) & (v > 0.35) & (cfg["green"] is None or hue < cfg.get("green_from", 62))
# Olive counts as green where the look keeps greens (the bowman's cloak is olive in his picture).
greenish = (hue >= cfg.get("green_from", 62)) & (hue < 170)
dull = ~red & ~gold

new_h = hue.copy()
new_s = s.copy()

# Leather and cloth toward the warm brown (greens toward green where the look keeps them).
target = np.full_like(hue, cfg["leather"])
if cfg["green"] is not None:
    target = np.where(greenish, cfg["green"], target)
pull = cfg["pull"] * colourful
delta = ((target - hue + 180) % 360) - 180
new_h = np.where(dull, (hue + delta * pull) % 360, new_h)
new_s = np.where(dull, s * (1 + (cfg["sat"] - 1) * colourful), new_s)

# Reds clear crimson, golds gold.
new_h = np.where(red, np.where(hue > 180, np.maximum(hue, 352), np.minimum(hue, 4)), new_h)
new_s = np.where(red, s * cfg["red"], new_s)
new_h = np.where(gold, 44, new_h)
new_s = np.where(gold, s * 1.6, new_s)
new_s = np.clip(new_s, 0.0, 0.85)

# Value: lifted, and a contrast curve round the middle tone.
new_v = np.clip(v * cfg["lift"], 0.0, 1.0)
mid = 0.42
new_v = np.clip(mid + (new_v - mid) * cfg["contrast"], 0.0, 1.0)

# HSV to RGB.
hh = (new_h / 60.0) % 6
c = new_v * new_s
x = c * (1 - np.abs(hh % 2 - 1))
m = new_v - c
z = np.zeros_like(c)
sector = hh.astype(np.int32)
choices = [
    np.stack([c, x, z], -1), np.stack([x, c, z], -1), np.stack([z, c, x], -1),
    np.stack([z, x, c], -1), np.stack([x, z, c], -1), np.stack([c, z, x], -1),
]
outrgb = np.zeros_like(rgb)
for k in range(6):
    outrgb = np.where((sector == k)[..., None], choices[k], outrgb)
outrgb += m[..., None]
px[..., :3] = np.clip(outrgb, 0.0, 1.0)

res = bpy.data.images.new("livened", w, h, alpha=True)
res.pixels = px.ravel()
res.filepath_raw = os.path.abspath(out)
res.file_format = "PNG"
res.save()
print("REPORT livened", look, "->", out)
