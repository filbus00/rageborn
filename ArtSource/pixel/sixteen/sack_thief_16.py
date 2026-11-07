# The Sack Thief drawn as a 16x16 sprite (2026-11-07, the owner: "lets just try designing one enemy in 16x16 style").
# Each letter is one pixel, from the palette below; "." is clear. Writes the sprite and a preview beside the current
# remade bake. Run: Blender -b -P ArtSource/pixel/sixteen/sack_thief_16.py
import bpy, os, re
import numpy as np

PALETTE = {
    "K": "1a1018",  # outline
    "R": "a8243a", "r": "6b1730", "h": "d8475a",  # crimson hood: base, shade, light
    "M": "8a8f99", "m": "4b4f5a",  # iron mask
    "E": "ffd23f",  # glowing eyes
    "y": "cfa870", "B": "b08a55", "b": "7a5a34",  # burlap: light, base, shade
    "S": "d2ab6c", "s": "94713f",  # the sack
    "L": "8c3f22",  # rust leather: belt and boots
    "C": "d4dbe2", "c": "6f7882",  # cleaver steel
    "W": "6b4423",  # wood
}

FRONT = [
    "...........KK...",
    "..........KhRK..",
    "......KKKKRRK...",
    ".....KhhRRRRKKK.",
    "....KhRRRRRRrLLK",
    "....KRMMMMMMrSsK",
    "....KrMEmmEMrSSK",
    "....KrMKmKmMrSsK",
    "...KKKrMMMMrKssK",
    "..KKKBbrrrrbBKK.",
    ".KCCKyBBBBBBbBK.",
    "KCCcWKyLLLLBKBK.",
    "KCcc.KyBBBBBbK..",
    ".KKK.KbBKKBbK...",
    "....KLLK..KLLK..",
    "...KLLLK..KLLLK.",
]


def rgba(grid):
    assert len(grid) == 16 and all(len(row) == 16 for row in grid), [len(r) for r in grid]
    out = np.zeros((16, 16, 4), np.float32)
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch != ".":
                h = PALETTE[ch]
                out[y, x] = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1]
    return out  # top row first


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()


HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sprite = rgba(FRONT)
save(sprite, os.path.join(HERE, "sack_thief_16.png"))


def remade_frame(name, frame):
    """A frame of the remade bake (27 a unit), cut by its rect, feet at the bottom."""
    path = f"{P}/Assets/_Project/Resources/Characters/{name}/{name}_idle.png"
    im = bpy.data.images.load(path)
    w, h = im.size
    px = np.array(im.pixels[:], np.float32).reshape(h, w, 4)
    meta = open(path + ".meta").read()
    m = re.search(frame + r"\n\s+rect:\n\s+serializedVersion: 2\n\s+x: (\S+)\n\s+y: (\S+)\n\s+width: (\S+)\n\s+height: (\S+)", meta)
    x, y, rw, rh = (int(float(v)) for v in m.groups())
    return px[y:y + rh, x:x + rw][::-1]


# The preview: on the forest green, at the screen's pixels (each game pixel 4 here): the remade Sack Thief, then the
# 16x16 at 1, 2 and 3 game pixels a pixel; and the 16x16 enlarged 16 times on its own.
BG = np.array([0.16, 0.21, 0.13, 1], np.float32)
Z = 4
current = remade_frame("sack_thief", "sack_thief_idle_s_00")
pieces = [current] + [np.repeat(np.repeat(sprite, k, 0), k, 1) for k in (1, 2, 3)]
H = max(p.shape[0] for p in pieces) + 8
Wd = sum(p.shape[1] + 12 for p in pieces) + 12
row = np.tile(BG, (H, Wd, 1))
x = 12
for p in pieces:
    ph, pw = p.shape[:2]
    y0 = H - 4 - ph
    a = p[..., 3:4] > 0.5
    row[y0:y0 + ph, x:x + pw] = np.where(a, p, row[y0:y0 + ph, x:x + pw])
    x += pw + 12
row = np.repeat(np.repeat(row, Z, 0), Z, 1)
big = np.repeat(np.repeat(sprite, 16, 0), 16, 1)
big = np.where(big[..., 3:4] > 0.5, big, BG)
pad = np.tile(BG, (max(row.shape[0], big.shape[0] + 32), row.shape[1] + big.shape[1] + 48, 1))
pad[pad.shape[0] - row.shape[0]:, :row.shape[1]] = row
pad[16:16 + big.shape[0], row.shape[1] + 24:row.shape[1] + 24 + big.shape[1]] = big
save(pad, f"{P}/ArtSource/pixel/previews/sack_thief_16_preview.png")
print("wrote preview")
