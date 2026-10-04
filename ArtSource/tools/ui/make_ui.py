# The menus' textures (the owner, 2026-10-04: "The menus look bad, revamp them"), drawn in numpy as height fields lit
# from the upper left, in grey so the game tints them (UiStyle): Assets/_Project/Resources/UI/
#   frame.png    9-sliced, an embossed metal rim with a rounded profile, scratches, rivets at the corners and edge
#                middles, a dark lip inside; clear in the middle (the rim's colour tints it: bronze, or a rarity)
#   inset.png    9-sliced, a recessed panel: an inner shadow along the top and left, a faint light along the bottom
#                and right, soft grain in the middle (the fill's colour tints it)
#   button.png   9-sliced, a raised plate: convex, light along the top edge, dark along the bottom
#   backdrop.png tiling, dark tooled leather for the screens' backgrounds
# Run with Blender's Python for numpy:  Blender -b -P ArtSource/tools/ui/make_ui.py
import os
import numpy as np
import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "UI")
rng = np.random.default_rng(3)
LIGHT = np.array([-0.5, 0.6, 1.0])
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def lit(height, strength=1.0):
    """Lambert of a height field (rows top to bottom, so +y in the image is down), 1 on flat ground."""
    gy, gx = np.gradient(height)
    nx, ny, nz = -gx * strength, gy * strength, np.ones_like(gx)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    lam = (nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]) / length
    return lam / LIGHT[2]


def noise(size, scale, seed, periodic=True):
    r = np.random.default_rng(seed)
    white = r.standard_normal((size, size))
    f = np.fft.fftfreq(size) * size
    fx, fy = np.meshgrid(f, f)
    mask = np.exp(-(np.sqrt(fx ** 2 + fy ** 2) / (size / scale)) ** 2)
    out = np.real(np.fft.ifft2(np.fft.fft2(white) * mask))
    return (out - out.min()) / (out.max() - out.min())


def save(name, value, alpha=None, tint=(1.0, 1.0, 1.0)):
    h, w = value.shape
    rgba = np.ones((h, w, 4), dtype=np.float32)
    for c in range(3):
        rgba[:, :, c] = np.clip(value * tint[c], 0, 1)
    if alpha is not None:
        rgba[:, :, 3] = np.clip(alpha, 0, 1)
    os.makedirs(OUT, exist_ok=True)
    image = bpy.data.images.new(name, w, h, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    image.pixels = rgba[::-1].ravel()
    image.filepath_raw = os.path.join(OUT, name + ".png")
    image.file_format = "PNG"
    image.save()
    print("REPORT", name, w, h)


def edge_distance(size_x, size_y):
    ys, xs = np.mgrid[0:size_y, 0:size_x] + 0.5
    return np.minimum(np.minimum(xs, size_x - xs), np.minimum(ys, size_y - ys)), xs, ys


def frame():
    n = 96
    d, xs, ys = edge_distance(n, n)
    # The rim's profile across its width: a dark outer edge, a flat band with chamfered edges, a groove, a thin bead
    # and a dark lip; 22 px in all (the 9-slice border).
    height = np.zeros((n, n))
    band = (d >= 2) & (d < 13)
    height = np.where(band, np.minimum(1.0, np.minimum(d - 2, 13 - d) / 2.0) * 1.2, height)
    bead = (d >= 15) & (d < 19)
    height = np.where(bead, np.sin((d - 15) / 4 * np.pi) * 0.8, height)
    # Small rivets in the corners only (a 9-slice stretches the edges' middles).
    for cx, cy in ((7.5, 7.5), (n - 7.5, 7.5), (7.5, n - 7.5), (n - 7.5, n - 7.5)):
        r2 = (xs - cx) ** 2 + (ys - cy) ** 2
        height = np.maximum(height, np.where(r2 < 9, np.sqrt(np.clip(9 - r2, 0, None)) * 0.7 + 1.2, 0))
    grain = noise(n, 24, 1) * 0.5 + noise(n, 70, 2) * 0.5
    scratches = np.clip(0.04 - np.abs(noise(n, 9, 11) - 0.5), 0, 0.04) * 6
    light = lit(height + grain * 0.25, 1.2)
    value = 0.58 * light + (grain - 0.5) * 0.14 - scratches * 0.4
    value = value + np.where(band, 0.06, 0)
    value = np.where(d < 2, 0.1, value)
    value = np.where(d >= 19, 0.16, value)
    alpha = np.where(d < 22, 1.0, 0.0)
    save("frame", value, alpha)


def inset():
    n = 128
    d, xs, ys = edge_distance(n, n)
    grain = noise(n, 10, 4) * 0.5 + noise(n, 40, 5) * 0.5
    value = 0.86 + (grain - 0.5) * 0.12
    # Inner shadow: deep along the top and left, light along the bottom and right.
    top = np.exp(-ys / 10.0)
    left = np.exp(-xs / 10.0)
    bottom = np.exp(-(n - ys) / 8.0)
    right = np.exp(-(n - xs) / 8.0)
    value = value * (1 - 0.55 * np.maximum(top, left)) + 0.08 * np.maximum(bottom, right)
    save("inset", value)


def button():
    w, h = 128, 64
    d, xs, ys = edge_distance(w, h)
    t = ys / h
    value = 1.0 - 0.32 * t                         # convex: light at the top, darker below
    value = value + np.exp(-(ys - 3) ** 2 / 4.0) * 0.18     # a highlight just under the top edge
    value = value - np.exp(-(h - ys - 2) ** 2 / 3.0) * 0.35  # a dark lip along the bottom
    value = value * (0.94 + noise(128, 30, 6)[:h, :w] * 0.08)
    save("button", value)


def backdrop():
    n = 256
    big = noise(n, 6, 7)
    fine = noise(n, 50, 8)
    creases = np.abs(noise(n, 14, 9) - 0.5)
    value = 0.78 + (big - 0.5) * 0.18 + (fine - 0.5) * 0.12
    value = value - np.clip(0.05 - creases, 0, 0.05) * 3.0
    save("backdrop", value)


def glow():
    """A soft round glow, white in the middle fading to clear (tinted behind the paper doll)."""
    n = 256
    ys, xs = np.mgrid[0:n, 0:n] + 0.5
    r = np.sqrt((xs - n / 2) ** 2 + (ys - n / 2) ** 2) / (n / 2)
    alpha = np.clip(1 - r, 0, 1) ** 1.6
    save("glow", np.ones((n, n)), alpha)


frame()
inset()
glow()
button()
backdrop()
