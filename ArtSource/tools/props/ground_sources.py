# The ground's source textures (the owner, 2026-10-04: "Make the town just dirt with some grassy spots and some paths.
# Make the dungeon mostly dirt and some cobble stones showing through ... Take inspiration from the first act in
# Diablo 2"). Four seamless textures drawn in ground space (40 px a ground unit, 512 px = 12.8 units a repeat), each
# already in the game's palette, with a strength in the alpha channel that the game's GroundPainter reads when it
# blends them: dirt (alpha: how dark, for its own patches), cobble (alpha: a stone's height, low in the grout, so stones
# show through the dirt top first), grass (alpha: tuft density, so grass frays into tufts at its edges) and path (alpha:
# wear). Written to Assets/_Project/Resources/Art/Ground/<name>.png.
#   Blender -b -P ArtSource/tools/props/ground_sources.py
import sys, os
import numpy as np
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit import STONE, WOOD, BLOOD, SKIN, MOSS, COLD, EMBER, BONE, VIOLET, EARTH

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art", "Ground")
S = 512
PPU = 40.0
PALETTE = np.array(STONE + WOOD + BLOOD + SKIN + MOSS + COLD + EMBER + BONE + VIOLET + EARTH + [(14, 10, 10)], dtype=np.float32)


def noise(scale_units, seed, octaves=4, rough=0.5):
    """Periodic fractal noise in 0..1; scale_units is the size of its broadest features in ground units."""
    r = np.random.default_rng(seed)
    total = np.zeros((S, S), dtype=np.float32)
    amp = 1.0
    f = np.fft.fftfreq(S) * S / (S / PPU)  # cycles per ground unit
    fx, fy = np.meshgrid(f, f)
    radius = np.sqrt(fx ** 2 + fy ** 2)
    for o in range(octaves):
        white = r.standard_normal((S, S))
        cutoff = (2 ** o) / scale_units
        layer = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(radius / cutoff) ** 2)))
        layer = (layer - layer.mean()) / (layer.std() + 1e-9)
        total += layer * amp
        amp *= rough
    return (total - total.min()) / (total.max() - total.min())


def voronoi(cell_units, seed, jitter=0.9):
    """Periodic Voronoi with points on a jittered grid: nearest and second nearest distance (ground units), nearest id."""
    r = np.random.default_rng(seed)
    n = int(round(S / PPU / cell_units))
    step = S / n
    gx, gy = np.meshgrid(np.arange(n), np.arange(n))
    px = (gx + 0.5 + (r.random((n, n)) - 0.5) * jitter) * step
    py = (gy + 0.5 + (r.random((n, n)) - 0.5) * jitter) * step
    pts = np.stack([px.ravel(), py.ravel()], 1)
    ys, xs = np.mgrid[0:S, 0:S].astype(np.float32)
    d1 = np.full((S, S), 1e9, np.float32)
    d2 = np.full((S, S), 1e9, np.float32)
    ids = np.zeros((S, S), np.int32)
    for k, (x, y) in enumerate(pts):
        for ox in (-S, 0, S):
            for oy in (-S, 0, S):
                cx, cy = x + ox, y + oy
                if abs(cx - S / 2) > S / 2 + step * 2 or abs(cy - S / 2) > S / 2 + step * 2:
                    continue
                d = np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2)
                closer = d < d1
                d2 = np.where(closer, d1, np.minimum(d2, d))
                ids = np.where(closer, k, ids)
                d1 = np.where(closer, d, d1)
    return d1 / PPU, d2 / PPU, ids, len(pts)


def lit(height, strength):
    gy, gx = np.gradient(height)
    nx, ny, nz = -gx * strength, gy * strength, np.ones_like(gx)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    light = np.array([-0.4, 0.7, 1.0])
    light /= np.linalg.norm(light)
    lam = (nx * light[0] + ny * light[1] + nz * light[2]) / length
    return np.clip(1 + (lam - light[2]) * 1.6, 0.5, 1.4)


def ramp(t, colors):
    """A value 0..1 through a list of colours (no smooth blend: the palette snap makes it steps anyway)."""
    c = np.array(colors, dtype=np.float32)
    idx = np.clip(t * (len(c) - 1), 0, len(c) - 1 - 1e-6)
    i0 = np.floor(idx).astype(int)
    f = (idx - i0)[..., None]
    return c[i0] * (1 - f) + c[np.minimum(i0 + 1, len(c) - 1)] * f


def snap(rgb):
    flat = rgb.reshape(-1, 3)
    out = np.empty_like(flat)
    w = np.array([3, 4, 2], dtype=np.float32)  # PixelArt.Snap's weights
    for i in range(0, len(flat), 65536):
        chunk = flat[i:i + 65536]
        d = (((chunk[:, None, :] - PALETTE[None]) ** 2) * w).sum(2)
        out[i:i + 65536] = PALETTE[d.argmin(1)]
    return out.reshape(rgb.shape)


def save(name, rgb, alpha):
    rgba = np.concatenate([snap(np.clip(rgb, 0, 255)) / 255.0, np.clip(alpha, 0, 1)[..., None]], axis=2).astype(np.float32)
    os.makedirs(OUT, exist_ok=True)
    image = bpy.data.images.new(name, S, S, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    image.alpha_mode = "STRAIGHT"
    image.pixels = rgba[::-1].ravel()
    image.filepath_raw = os.path.join(OUT, name + ".png")
    image.file_format = "PNG"
    image.save()
    print("REPORT", name)


def dirt():
    # Act 1's grey-olive earth: soft low-contrast patches, fine grit, a few pebbles and hairline cracks.
    broad = noise(4.0, 1)
    mid = noise(1.0, 6, 3)
    grit = noise(0.12, 2, 1)
    t = np.clip(0.5 + (broad - 0.5) * 0.7 + (mid - 0.5) * 0.45 + (grit - 0.5) * 0.55, 0, 1)
    rgb = ramp(t, [EARTH[1], EARTH[2], EARTH[3], EARTH[4], EARTH[5]])
    d1, d2, ids, n = voronoi(0.3, 3)
    r = np.random.default_rng(4)
    keep = r.random(n)[ids] < 0.1
    size = r.uniform(0.03, 0.07, n)[ids]
    pebble = np.clip(1 - d1 / size, 0, 1) * keep
    tone = r.random(n)[ids]
    rgb = np.where((pebble > 0)[..., None], ramp(tone * 0.5 + pebble * 0.5, [STONE[2], STONE[3], STONE[4]]), rgb)
    rgb = rgb * lit(pebble * 0.06 + mid * 0.004, 30)[..., None]
    crack = np.clip(0.01 - np.abs(noise(1.4, 5, 3) - 0.5), 0, 0.01) / 0.01
    crack *= noise(2.0, 18, 2) > 0.55
    rgb = np.where((crack > 0.5)[..., None], np.array(EARTH[0], np.float32), rgb)
    save("dirt", rgb, 1 - broad)


def cobble():
    # Worn cobbles, each its own shade of grey-brown, set in dark grout; a stone's top is its alpha.
    d1, d2, ids, n = voronoi(0.5, 7, 0.8)
    edge = d2 - d1
    r = np.random.default_rng(8)
    tone = r.random(n)[ids]
    inside = np.clip((edge - 0.035) / 0.07, 0, 1)
    dome = np.clip(1 - (d1 / 0.36) ** 2, 0, 1)
    height = inside * (0.6 + 0.4 * dome)
    base = ramp(tone, [STONE[2], EARTH[3], STONE[3], EARTH[4], STONE[4]])
    rgb = base * lit(height * 0.06 + noise(0.2, 9, 2) * 0.01, 25)[..., None]
    grout = np.array(EARTH[1], dtype=np.float32)
    rgb = rgb * inside[..., None] + grout * (1 - inside[..., None])
    moss = np.clip((noise(2.0, 10) - 0.62) * 4, 0, 1) * (1 - inside)
    rgb = rgb * (1 - moss[..., None]) + np.array(MOSS[1], np.float32) * moss[..., None]
    save("cobble", rgb, height)


def grass():
    # Short wild grass, dark and muted, lighter blades over darker roots, dry olive in places (alpha: density, so it
    # frays into tufts at its edges).
    density = noise(1.2, 11)
    clumps = noise(0.35, 19, 2)
    blades = noise(0.05, 12, 1)
    dry = noise(2.5, 20) > 0.62
    t = np.clip(0.5 + (blades - 0.5) * 1.6 + (clumps - 0.5) * 0.9, 0, 1)
    green = ramp(t, [MOSS[0], MOSS[1], MOSS[2], MOSS[3], MOSS[4]])
    olive = ramp(t, [EARTH[1], EARTH[2], MOSS[2], EARTH[4], MOSS[4]])
    rgb = np.where(dry[..., None], olive, green)
    save("grass", rgb, np.clip(density * 0.7 + clumps * 0.5 - 0.1, 0, 1))


def path():
    # A packed, lighter footpath with ruts and a scatter of small stones (alpha: wear).
    wear = noise(1.5, 14)
    grit = noise(0.1, 15, 2)
    t = np.clip(0.5 + (wear - 0.5) * 0.8 + (grit - 0.5) * 1.2, 0, 1)
    rgb = ramp(t, [EARTH[2], EARTH[3], EARTH[4], EARTH[4], EARTH[5]])
    d1, d2, ids, n = voronoi(0.3, 16)
    r = np.random.default_rng(17)
    keep = r.random(n)[ids] < 0.08
    pebble = np.clip(1 - d1 / 0.05, 0, 1) * keep
    rgb = np.where((pebble > 0)[..., None], np.array(STONE[4], np.float32), rgb)
    rgb = rgb * lit(pebble * 0.04 + grit * 0.01, 30)[..., None]
    save("path", rgb, wear)


dirt()
cobble()
grass()
path()
