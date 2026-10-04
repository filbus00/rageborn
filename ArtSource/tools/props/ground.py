# Continuous ground textures (the owner, 2026-10-04: "it does not look good that it is all tiles. Make real ground
# textures"): each style is one seamless texture that repeats every 4 x 4 cells, drawn in the cells' own lattice
# (u along the tilemap's x, v along its y) and cut into 16 diamond pieces, floor_<style>_<1 + i + 4 j>, so stones,
# bricks, cracks and moss run across the cells' edges and no grid shows. DungeonArt.VariantAt picks the piece by the
# cell's place in the repeat. Each texture is a height map and a colour map in numpy, shaded by the height's slopes
# under a light from the upper left, with no overall gradient (the game lights the floor). Pieces are written at 4x
# (160 x 80) to ArtSource/pixel/dungeon for Tools > ARPG > Import Pixel Art.
#   Blender -b -P ArtSource/tools/props/ground.py -- [styles...]
import sys, os
import numpy as np
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit import STONE, WOOD, BLOOD, SKIN, MOSS, BONE

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "pixel", "dungeon"))
P = 4          # cells a side of the repeat
R = 64         # texture pixels a cell
S = P * R
rng = np.random.default_rng(5)


def rgb(c):
    return np.array(c, dtype=np.float32) / 255.0


def fbm(scale_cells, octaves=4, seed=0):
    """Periodic fractal noise in 0..1: white noise filtered in the frequency domain (periodic by construction)."""
    r = np.random.default_rng(seed)
    total = np.zeros((S, S), dtype=np.float32)
    amp = 1.0
    for o in range(octaves):
        white = r.standard_normal((S, S))
        f = np.fft.fftfreq(S) * S / P  # cycles per cell
        fx, fy = np.meshgrid(f, f)
        radius = np.sqrt(fx ** 2 + fy ** 2)
        cutoff = (2 ** o) / scale_cells
        mask = np.exp(-(radius / cutoff) ** 2)
        layer = np.real(np.fft.ifft2(np.fft.fft2(white) * mask))
        layer = (layer - layer.mean()) / (layer.std() + 1e-9)
        total += layer * amp
        amp *= 0.5
    total = (total - total.min()) / (total.max() - total.min())
    return total


def voronoi(points_per_cell, jitter=1.0, seed=0):
    """Periodic Voronoi: for every pixel the nearest and second nearest distances (in cells) and the nearest's id."""
    r = np.random.default_rng(seed)
    count = int(points_per_cell * P * P)
    pts = r.uniform(0, P, (count, 2))
    ys, xs = np.mgrid[0:S, 0:S] / R
    d1 = np.full((S, S), 1e9, dtype=np.float32)
    d2 = np.full((S, S), 1e9, dtype=np.float32)
    ids = np.zeros((S, S), dtype=np.int32)
    for k, (px, py) in enumerate(pts):
        for ox in (-P, 0, P):
            for oy in (-P, 0, P):
                dx = xs - (px + ox)
                dy = ys - (py + oy)
                d = np.sqrt(dx * dx + dy * dy)
                closer = d < d1
                d2 = np.where(closer, d1, np.minimum(d2, d))
                ids = np.where(closer, k, ids)
                d1 = np.where(closer, d, d1)
    return d1, d2, ids, count


def shade(height, albedo, strength=1.0):
    """Lambert from the height's slopes (periodic differences), light from the upper left of the screen (+v, a little
    -u), around 1 on flat ground so the overall level does not move."""
    gx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * R * 0.5
    gy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * R * 0.5
    nx, ny, nz = -gx * strength, -gy * strength, np.ones_like(gx)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    light = np.array([-0.35, 0.75, 1.0])
    light = light / np.linalg.norm(light)
    lam = (nx * light[0] + ny * light[1] + nz * light[2]) / length
    flat = light[2]
    factor = np.clip(1.0 + (lam - flat) * 1.4, 0.45, 1.35)
    return np.clip(albedo * factor[..., None], 0, 1)


def mix_colors(a, b, t):
    return a * (1 - t[..., None]) + b * t[..., None]


def stones(points, grout, base, light, dark, seed, mortar_color, chip=0.0):
    d1, d2, ids, count = voronoi(points, seed=seed)
    edge = d2 - d1
    r = np.random.default_rng(seed + 1)
    tone = r.uniform(0, 1, count)[ids]
    lift = r.uniform(0.0, 0.025, count)[ids]
    detail = fbm(0.6, 3, seed + 2)
    # A stone is a raised, slightly domed slab; the grout between them is low.
    inside = np.clip((edge - grout) / grout, 0, 1)
    height = inside * (0.04 + lift) + detail * 0.012 * inside
    albedo = mix_colors(rgb(dark), rgb(light), tone * 0.7 + detail * 0.3)
    albedo = mix_colors(albedo, rgb(base), 0.35 * np.ones_like(tone))
    albedo = mix_colors(rgb(mortar_color) * np.ones_like(albedo), albedo, inside)
    return height, albedo, edge


def cracks(height, albedo, seed, amount=6, color=STONE[0]):
    r = np.random.default_rng(seed)
    for k in range(amount):
        x, y = r.uniform(0, S, 2)
        a = r.uniform(0, 6.28)
        for s in range(int(r.uniform(20, 60))):
            a += r.uniform(-0.5, 0.5)
            x, y = (x + np.cos(a) * 1.5) % S, (y + np.sin(a) * 1.5) % S
            xi, yi = int(x), int(y)
            height[yi, xi] -= 0.02
            albedo[yi, xi] = rgb(color)
    return height, albedo


def flagstone(seed):
    h, a, _ = stones(1.6, 0.05, STONE[4], STONE[5], STONE[3], seed, STONE[1])
    return cracks(h, a, seed + 9, 5)


def moss(seed):
    h, a, edge = stones(1.4, 0.06, STONE[3], STONE[4], STONE[2], seed, MOSS[0])
    growth = fbm(1.2, 4, seed + 4)
    damp = np.clip((growth - 0.52) * 5, 0, 1)
    # Moss creeps from the grout onto the stones.
    near_grout = np.clip(1 - edge / 0.25, 0, 1)
    cover = np.clip(damp * 0.9 + near_grout * damp * 0.6, 0, 1)
    green = mix_colors(rgb(MOSS[2]) * np.ones_like(a), rgb(MOSS[3]) * np.ones_like(a), fbm(0.3, 2, seed + 6))
    a = mix_colors(a, green, cover)
    h = h + cover * 0.008
    return cracks(h, a, seed + 11, 3)


def brick(seed):
    # Running bond along u: 12 rows over the repeat, each brick 4/3 of a cell long, rows offset by half a brick, every
    # brick its own shade and a little worn.
    ys, xs = np.mgrid[0:S, 0:S] / R
    rows = 12
    rh = P / rows
    row = np.floor(ys / rh).astype(int)
    length = P / 3
    offset = (row % 2) * length * 0.5
    col = np.floor(((xs + offset) % P) / length).astype(int)
    fy = (ys % rh) / rh
    fx = (((xs + offset) % P) % length) / length
    joint = np.minimum(np.minimum(fy, 1 - fy) * rh, np.minimum(fx, 1 - fx) * length)
    r = np.random.default_rng(seed)
    tone = r.uniform(0, 1, (rows, 3))[row % rows, col % 3]
    detail = fbm(0.5, 3, seed + 3)
    inside = np.clip((joint - 0.025) / 0.02, 0, 1)
    height = inside * (0.03 + tone * 0.01) + detail * 0.01 * inside
    albedo = mix_colors(rgb(SKIN[0]) * np.ones((S, S, 3)), rgb(SKIN[1]) * np.ones((S, S, 3)), tone * 0.6 + detail * 0.4)
    albedo = mix_colors(rgb(STONE[1]) * np.ones_like(albedo), albedo, inside)
    return cracks(height, albedo, seed + 5, 4)


def earth(seed):
    base = fbm(0.8, 5, seed)
    patches = fbm(0.9, 3, seed + 1)
    height = base * 0.03
    albedo = mix_colors(rgb(WOOD[2]) * np.ones((S, S, 3)), rgb(WOOD[3]) * np.ones((S, S, 3)), np.clip((base - 0.25) * 2.5, 0, 1))
    albedo = mix_colors(albedo, rgb(WOOD[1]) * np.ones_like(albedo), np.clip((patches - 0.7) * 3, 0, 0.6))
    # Pebbles: small sparse bumps.
    d1, d2, ids, count = voronoi(5, seed=seed + 2)
    r = np.random.default_rng(seed + 3)
    keep = r.uniform(0, 1, count)[ids] < 0.25
    size = r.uniform(0.04, 0.09, count)[ids]
    pebble = np.clip(1 - d1 / size, 0, 1) * keep
    height = height + pebble * 0.03
    tone = r.uniform(0, 1, count)[ids]
    albedo = mix_colors(albedo, mix_colors(rgb(STONE[3]) * np.ones_like(albedo), rgb(STONE[4]) * np.ones_like(albedo), tone), np.clip(pebble * 3, 0, 1))
    return cracks(height, albedo, seed + 7, 3, WOOD[0])


STYLES = {"flagstone": flagstone, "brick": brick, "earth": earth, "moss": moss}


def cut(texture, i, j):
    """The diamond of cell (i, j) at 160 x 80, sampled from the texture in lattice coordinates (bilinear)."""
    W, H = 160, 80
    py, px = np.mgrid[0:H, 0:W]
    dx = (px + 0.5 - W / 2) / (W / 2)
    dy = ((H - 1 - py) + 0.5 - H / 2) / (H / 2)   # rows top to bottom in the image, y up in the lattice
    a = (dx + dy) / 2
    b = (dy - dx) / 2
    u = (i + 0.5 + a) * R
    v = (j + 0.5 + b) * R
    u0 = np.floor(u).astype(int)
    v0 = np.floor(v).astype(int)
    fu, fv = (u - u0)[..., None], (v - v0)[..., None]
    def at(uu, vv):
        return texture[vv % S, uu % S]
    out = (at(u0, v0) * (1 - fu) * (1 - fv) + at(u0 + 1, v0) * fu * (1 - fv) +
           at(u0, v0 + 1) * (1 - fu) * fv + at(u0 + 1, v0 + 1) * fu * fv)
    # The whole frame stays opaque: the importer cuts the diamond after shrinking, so its edge pixels are whole (cut
    # here, they averaged with clear pixels into dark seams).
    return np.concatenate([out, np.ones((H, W, 1))], axis=2).astype(np.float32)


def save(path, rgba):
    h, w = rgba.shape[:2]
    image = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    image.pixels = rgba[::-1].ravel()   # Blender's rows are bottom to top
    image.filepath_raw = path
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)


argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
for name, make in STYLES.items():
    if argv and name not in argv:
        continue
    height, albedo = make(100 + list(STYLES).index(name) * 17)
    texture = shade(height, albedo, 1.0)
    for j in range(P):
        for i in range(P):
            save(os.path.join(ROOT, "floor_%s_%d.png" % (name, 1 + i + P * j)), cut(texture, i, j))
    # The whole repeat, for a look.
    whole = np.concatenate([texture, np.ones((S, S, 1))], axis=2).astype(np.float32)
    save(os.path.join(ROOT, "..", "ground_%s_preview.png" % name), whole[::-1])
    print("REPORT", name)
