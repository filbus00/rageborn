# The Bell Hermit (2026-11-07, the owner: "design a totally new and original enemy in 16x16"): a hunched forest hermit
# under a huge straw hat and a straw rain cape, one glowing eye in the hat's shadow, ringing a brass bell on a staff.
# Each letter is one pixel from the palette; "." is clear. Run: Blender -b -P ArtSource/pixel/sixteen/bell_hermit_16.py
import bpy, os
import numpy as np

PALETTE = {
    "K": "1a1018",  # outline
    "y": "ffe08a", "Y": "e8b84e", "o": "a8772a",  # straw hat: light, base, shade
    "d": "2a1f2e",  # the shadow under the hat
    "E": "6ff2ff", "e": "ffffff",  # the eye and its glint
    "q": "c98a4a", "R": "a4532a", "r": "6e3420",  # straw cape: light, base, shade
    "L": "5a3a24",  # root feet
    "W": "7a4e2a",  # staff
    "G": "f2c14a", "g": "9a6a1e",  # brass bell
}

IDLE = [
    [  # the bell still, the eye open
        "............KKK.",
        ".....KK....KWWWK",
        "....KyYK...KWKgK",
        "...KyYYoK..KWKGK",
        "..KyYYYYoK.KWGGK",
        ".KyYYYYYYoKKWyGg",
        "KyYYYYYYYYoKWGGg",
        ".KKKKKKKKKKKWKgK",
        "..KddddddddKWK..",
        "..KdddEeddKKWK..",
        ".KqqRddRRRrKWK..",
        "KqRrRqRrRRrKWK..",
        "KqRrRqRrRRrKWK..",
        ".KRrRqRrRRrKWK..",
        "..KrKqRKRrKKWK..",
        "...KLLK.KLLKKWK.",
    ],
    [  # the bell swung low, the eye narrowed
        "............KKK.",
        ".....KK....KWWWK",
        "....KyYK...KWKgK",
        "...KyYYoK..KWKgK",
        "..KyYYYYoK.KWKGK",
        ".KyYYYYYYoKKWGGK",
        "KyYYYYYYYYoKWyGg",
        ".KKKKKKKKKKKWGGg",
        "..KddddddddKWKgK",
        "..KdddEEddKKWK..",
        ".KqqRddRRRrKWK..",
        "KqRrRqRrRRrKWK..",
        "KqRrRqRrRRrKWK..",
        ".KRrRqRrRRrKWK..",
        "..KrKqRKRrKKWK..",
        "...KLLK.KLLKKWK.",
    ],
]


def rgba(grid):
    assert len(grid) == 16 and all(len(row) == 16 for row in grid), [len(r) for r in grid]
    out = np.zeros((16, 16, 4), np.float32)
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch != ".":
                h = PALETTE[ch]
                out[y, x] = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1]
    return out


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()


HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
frames = [rgba(f) for f in IDLE]
save(np.concatenate(frames, 1), os.path.join(HERE, "bell_hermit_16.png"))

# Preview on the forest green: both frames enlarged 16 times, then at 1, 2 and 3 game pixels a pixel (each shown 4x).
BG = np.array([0.16, 0.21, 0.13, 1], np.float32)
def on_bg(img):
    return np.where(img[..., 3:4] > 0.5, img, BG)
def up(img, k):
    return np.repeat(np.repeat(img, k, 0), k, 1)
big = np.concatenate([on_bg(up(f, 16)) for f in frames] + [np.tile(BG, (256, 32, 1))], 1)
small = np.tile(BG, (64, 16 + 32 + 48 + 48, 1))
x = 8
for k in (1, 2, 3):
    s = up(frames[0], k)
    h, w = s.shape[:2]
    region = small[60 - h:60, x:x + w]
    small[60 - h:60, x:x + w] = np.where(s[..., 3:4] > 0.5, s, region)
    x += w + 12
small = up(small, 4)
out = np.tile(BG, (256, big.shape[1] + small.shape[1] + 16, 1))
out[:, :big.shape[1]] = big
out[256 - small.shape[0]:, big.shape[1] + 16:] = small
save(out, f"{P}/ArtSource/pixel/previews/bell_hermit_16_preview.png")
print("wrote preview")
