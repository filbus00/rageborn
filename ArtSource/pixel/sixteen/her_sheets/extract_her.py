# Her art since 2026-11-08 (the owner: "see downloads for sprite sheets for this character", "put her in the game").
# Cuts the figures out of the owner's two sprite sheets (source_shoot.png, source_idlewalk.png), finds each one's pixel grid (the
# sheets are drawn about 8 screen pixels a pixel, softly), reads one colour a pixel from each block's middle, makes the
# white background clear, snaps every pixel to one palette shared by all the frames, and writes the cleaned frames
# (her_frames.pkl, her_palette.npy) and previews. Then build_her_sheets.py.
# Run: Blender -b -P ArtSource/pixel/sixteen/her_sheets/extract_her.py
import bpy, numpy as np, json, os

S = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(S, "..", "..", "..", ".."))
SHEETS = {"shoot": os.path.join(S, "source_shoot.png"), "idlewalk": os.path.join(S, "source_idlewalk.png")}


def load(path):
    im = bpy.data.images.load(path)
    w, h = im.size
    a = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1].copy()   # top row first
    bpy.data.images.remove(im)
    return a


def boxes(mask, min_area=600):
    """Bounding boxes of the figures: runs of filled columns inside runs of filled rows (a figure's arrow streak
    beside it is its own small box)."""
    out = []
    rows = mask.any(1)
    y = 0
    H, W = mask.shape
    while y < H:
        if not rows[y]:
            y += 1
            continue
        y0 = y
        while y < H and rows[y]:
            y += 1
        band = mask[y0:y]
        cols = band.any(0)
        x = 0
        while x < W:
            if not cols[x]:
                x += 1
                continue
            x0 = x
            while x < W and cols[x]:
                x += 1
            sub = band[:, x0:x]
            ys = np.where(sub.any(1))[0]
            if sub.sum() >= min_area:
                out.append((x0, y0 + ys[0], x, y0 + ys[-1] + 1))
    return out


def grid_sample(img, box, step):
    """Reads the figure at the pixel size that best fits it (step, or (across, down) for different sizes each way):
    tries every offset, keeps the one whose blocks are the most even, and takes each block's middle."""
    sx, sy = (step, step) if not isinstance(step, tuple) else step
    x0, y0, x1, y1 = box
    best = None
    for ox in range(int(sx)):
        for oy in range(int(sy)):
            xs = np.arange(x0 - sx + ox, x1 + sx, sx)
            ys = np.arange(y0 - sy + oy, y1 + sy, sy)
            spread = 0.0
            for yy in ys[1:-1:2]:
                for xx in xs[1:-1:2]:
                    blk = img[int(yy):int(yy + sy), int(xx):int(xx + sx), :3]
                    if blk.size:
                        spread += blk.std(axis=(0, 1)).sum()
            if best is None or spread < best[0]:
                best = (spread, ox, oy)
    _, ox, oy = best
    xs = np.arange(x0 - sx + ox, x1 + sx, sx)
    ys = np.arange(y0 - sy + oy, y1 + sy, sy)
    h, w = len(ys) - 1, len(xs) - 1
    out = np.zeros((h, w, 3), np.float32)
    for j in range(h):
        for i in range(w):
            c0, c1 = int(xs[i] + sx * 0.3), int(xs[i] + sx * 0.7)
            r0, r1 = int(ys[j] + sy * 0.3), int(ys[j] + sy * 0.7)
            out[j, i] = np.median(img[r0:r1, c0:c1, :3].reshape(-1, 3), axis=0)
    return out


def kmeans(colours, k, iterations=30, seed=1):
    rng = np.random.default_rng(seed)
    centres = colours[rng.choice(len(colours), k, replace=False)]
    for _ in range(iterations):
        d = ((colours[:, None, :] - centres[None]) ** 2).sum(-1)
        lab = d.argmin(1)
        for c in range(k):
            pick = colours[lab == c]
            if len(pick):
                centres[c] = pick.mean(0)
    return centres


frames = {}
STEP = {}
for name, path in SHEETS.items():
    img = load(path)
    white = (img[..., :3] > 0.86).all(-1)
    found = boxes(~white)
    # The pixel size that fits this sheet best: the most even blocks on its first figures.
    def score(step):
        total = 0.0
        for bx in found[:3]:
            f = grid_sample(img, bx, step)
            # Sharp blocks: each sampled pixel near the colour of the block it came from; measured as how few distinct
            # neighbours differ by a little (blur gives many small differences).
            dx = np.abs(np.diff(f, axis=1)).sum(-1)
            dy = np.abs(np.diff(f, axis=0)).sum(-1)
            total += ((dx > 0.03) & (dx < 0.25)).mean() + ((dy > 0.03) & (dy < 0.25)).mean()
        return total
    steps = [7.5, 8.0, 8.5] if name == "shoot" else [10.5, 11.0, 11.5, 12.0, 12.5]
    # Both sheets at one size of her (about 24 pixels tall): the archery sheet's figures are about 190 tall, the idle
    # sheet's about 280.
    step = 8.0 if name == "shoot" else 11.6
    STEP[name] = step
    # One size of her across the sheets (2026-11-08, the owner: "she seems to grown when the attack animations are
    # triggered", then "she seems to be growing and shrinking"): the archery sheet draws her wider, not taller, than the
    # idle figures (her head a quarter wider, her height the same), so its figures are read narrower across than down:
    # her head and her height then both match standing. The walking figures are drawn about 12 percent smaller.
    def step_for(index):
        if name == "shoot":
            return (8.0 / 0.8, 8.0 / 0.98)
        return 11.6 if index < 4 else 11.6 / 1.12
    frames[name] = [(b, grid_sample(img, b, step_for(i))) for i, b in enumerate(found)]
    print(name, len(found), "figures", [ (b[2]-b[0], b[3]-b[1]) for b in found][:6])

# One palette for all: the figure's colours (not the white).
allc = np.concatenate([f.reshape(-1, 3) for v in frames.values() for _, f in v], 0)
keep = ~(allc > 0.82).all(-1)
pal = kmeans(allc[keep], 18)
pal = pal[np.argsort(pal.sum(1))]

cleaned = {}
for name, lst in frames.items():
    cleaned[name] = []
    for box, f in lst:
        h, w, _ = f.shape
        bg = (f > 0.82).all(-1)
        d = ((f[..., None, :] - pal[None, None]) ** 2).sum(-1)
        idx = d.argmin(-1)
        rgba = np.zeros((h, w, 4), np.float32)
        rgba[..., :3] = pal[idx]
        rgba[..., 3] = (~bg).astype(np.float32)
        cleaned[name].append((box, rgba, idx * (~bg) + (-1) * bg))

# Preview: each sheet's cleaned frames in their places, 6 times, on the green.
G = np.array([0.16, 0.21, 0.13, 1], np.float32)
for name, lst in cleaned.items():
    st = STEP[name]
    W = int(max(b[2] for b, _, _ in lst) / st) + 4
    H = int(max(b[3] for b, _, _ in lst) / st) + 4
    canvas = np.tile(G, (H, W, 1))
    for (x0, y0, x1, y1), rgba, _ in lst:
        h, w, _ = rgba.shape
        X, Y = int(x0 / st), int(y0 / st)
        region = canvas[Y:Y + h, X:X + w]
        hh, ww = region.shape[:2]
        a = rgba[:hh, :ww]
        canvas[Y:Y + h, X:X + w] = np.where(a[..., 3:4] > 0.5, a, region)
    big = np.repeat(np.repeat(canvas, 5, 0), 5, 1)
    o = bpy.data.images.new(name, big.shape[1], big.shape[0], alpha=True)
    o.pixels = big[::-1].ravel()
    o.filepath_raw = os.path.join(P, "ArtSource", "pixel", "previews", f"her_sheet_{name}_clean.png")
    o.file_format = "PNG"
    o.save()

np.save(f"{S}/her_palette.npy", pal)
import pickle
pickle.dump({k: [(b, idx) for b, _, idx in v] for k, v in cleaned.items()}, open(f"{S}/her_frames.pkl", "wb"))
print("palette", len(pal))
