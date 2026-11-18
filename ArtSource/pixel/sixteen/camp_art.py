# The Wild Arrow Company's camp in the 16x16 style (2026-11-18, the owner: "the generate art is lame, just finish it after
# your own head"): the contracts billboard, bell tents, the fire with its benches, the forge, the pen's fence and trough, the
# archers' targets and dummy, the supply cart, crates, lamps and banners, the grave of the fallen, and the three keepers
# (Brunhilde, Thorin, Lyriel) with the pen's wolf. Drawn from shapes by code with the gaol art's helpers and one palette, light
# from the top left, dark outlines, each drawn pixel 2 game pixels, at the screen's 27 pixels a unit like the forest's.
#
# Writes into Assets/_Project/Resources/Art/World, the list ArtSource/pixel/sixteen/camp_art_sizes.txt ("Folder/name width
# height pivotX pivotY", in game pixels from the left and the bottom; Tools > ARPG > Configure Gaol Art sets each sprite's
# size and pivot from it) and a preview, ArtSource/pixel/previews/camp_art_16.png.
# Run: python3 ArtSource/pixel/sixteen/camp_art.py (plain Python, no Blender needed)
import os, sys, math, random

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gaol_art import Img, write_png, ART, P, K, SCALE  # noqa: E402

PAL = {
    "canvas": ["1c3a26", "2e5a3a", "437c4a", "6aa060"],     # the company's green canvas, shadow to light
    "cloth": ["4a1414", "7a1e1e", "a82e2a", "d0483a"],      # the company's red
    "wood": ["2e1c12", "4e3220", "74502e", "9a6a3a"],
    "iron": ["15151c", "3a3e4a", "6a7080", "9aa2b2"],
    "stone": ["2a2c38", "3f4253", "5e6278", "8a90a6"],
    "parch": ["6a5a3e", "a08a5e", "d0bc8a", "ece0b8"],
    "fire": ["c8321a", "ff8a2a", "ffd84a", "fff6c0"],
    "straw": ["6a5216", "a8842a", "d8b848", "f0dc80"],
    "earth": ["2a1c12", "42301e", "5e4630", "7a5e40"],
    "grass": ["1c3a26", "2e5a34", "437c3e"],
    "skin": ["7a4a34", "b07a58", "d8a07a", "f0c09a"],
    "brass": ["6a4a10", "b88a22", "e8c050", "fff0a0"],
    "bone": ["8a7c66", "c8bca4", "f0e8d8"],
    "grey": ["34363e", "5a5e6a", "8a8e9a", "c0c4cc"],
}


def poly(im, pts, col):
    """Fills a polygon (scanline, half-pixel centres)."""
    n = len(pts)
    for y in range(im.h):
        for x in range(im.w):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = n - 1
            for i in range(n):
                xi, yi = pts[i]
                xj, yj = pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside:
                im.set(x, y, col)


def shadow(im, cx, cy, rx, ry):
    """No ground shadow is drawn into a piece: a patch of dark pixels reads as a black blob on the grass. The game's
    lights shade the pieces."""
    return


# --- The billboard ---------------------------------------------------------------------------------------------------
def billboard():
    im = Img(36, 36)
    w, p, c, i = PAL["wood"], PAL["parch"], PAL["cloth"], PAL["iron"]
    shadow(im, 18, 33, 15, 2.5)
    # Two posts, a board between them, a little roof.
    for x0 in (5, 28):
        im.rect(x0, 11, x0 + 2, 33, w[2]); im.rect(x0, 11, x0, 33, w[3]); im.rect(x0 + 2, 11, x0 + 2, 33, w[1])
    im.rect(3, 6, 32, 22, w[1])
    im.rect(4, 7, 31, 21, w[2])
    for x in (10, 17, 24):
        im.rect(x, 7, x, 21, w[1])
    im.rect(3, 6, 32, 6, w[3]); im.rect(3, 22, 32, 22, w[0])
    poly(im, [(1, 6), (17.5, 0), (34, 6)], w[1])
    im.line(1, 5, 17, 0, w[3]); im.line(18, 0, 34, 5, w[2]); im.rect(2, 5, 33, 6, w[2])
    # The notices: parchments pinned in rows, a red one, the company's arrow over the middle.
    for (x0, y0, x1, y1, col) in ((5, 9, 10, 15, p[2]), (12, 8, 16, 13, p[1]), (19, 9, 24, 16, p[3]), (27, 9, 30, 14, p[2]),
                                  (6, 17, 12, 20, p[1]), (14, 15, 18, 20, c[2]), (20, 18, 26, 20, p[2]), (28, 16, 31, 20, p[1])):
        im.rect(x0, y0, x1, y1, col)
        im.set(x0 + 1, y0, i[2])
        for k in range(y0 + 2, y1, 2):
            im.rect(x0 + 1, k, max(x0 + 1, x1 - 2), k, p[0] if col != c[2] else c[0])
    im.line(20, 12, 24, 12, PAL["canvas"][0]); im.set(23, 11, PAL["canvas"][0]); im.set(23, 13, PAL["canvas"][0])
    # A lantern on the right post.
    im.line(31, 9, 33, 9, i[2]); im.rect(32, 10, 34, 13, PAL["brass"][1]); im.rect(33, 11, 33, 12, PAL["fire"][2])
    return im.outline()


# --- Tents -----------------------------------------------------------------------------------------------------------
def tent(flip=False):
    im = Img(42, 34)
    c, r, w = PAL["canvas"], PAL["cloth"], PAL["wood"]
    shadow(im, 21, 31, 19, 3)
    poly(im, [(21, 4), (2, 21), (2, 28), (21, 33)], c[2])       # the lit face
    poly(im, [(21, 4), (40, 21), (40, 28), (21, 33)], c[1])     # the shaded face
    im.line(21, 4, 2, 21, c[3]); im.line(21, 4, 21, 33, c[0])
    for x0, x1 in ((11, 11), (31, 31)):                           # seams
        im.line(21, 4, x0 + (x1 - x0), 31 if x0 < 21 else 30, c[1] if x0 < 21 else c[0])
    # The red band at the foot and a doorway on the lit face.
    poly(im, [(2, 24), (21, 29), (21, 33), (2, 28)], r[2])
    poly(im, [(21, 29), (40, 24), (40, 28), (21, 33)], r[1])
    poly(im, [(7, 27), (7, 21), (12, 17), (17, 20), (17, 29)], PAL["earth"][0])
    im.line(7, 21, 12, 17, c[3]); im.line(12, 17, 17, 20, c[3])
    # A pole and the company's pennant.
    im.line(21, 4, 21, 0, w[3]); poly(im, [(22, 0), (28, 1.5), (22, 3)], r[3])
    im.outline()
    return im.mirrored() if flip else im


# --- The fire ----------------------------------------------------------------------------------------------------------
def fire():
    im = Img(32, 26)
    s, w, f = PAL["stone"], PAL["wood"], PAL["fire"]
    shadow(im, 16, 20, 15, 5)
    # The stone ring: an ellipse of stones.
    for k in range(18):
        a = k / 18.0 * math.tau
        x, y = 16 + math.cos(a) * 12.5, 18 + math.sin(a) * 5.2
        tone = s[3] if (math.sin(a) < -0.2 and math.cos(a) < 0.4) else s[1] if math.sin(a) > 0.2 else s[2]
        im.rect(round(x) - 1, round(y) - 1, round(x) + 1, round(y), tone)
        im.set(round(x) - 1, round(y) - 1, s[3] if tone != s[1] else s[2])
    im.blob(16, 17.5, 9, 3.6, [PAL["earth"][0], PAL["earth"][0], PAL["earth"][1], PAL["earth"][1]])
    # The logs, crossed, and the flames.
    im.line(8, 19, 24, 15, w[2]); im.line(8, 20, 24, 16, w[1]); im.line(9, 15, 23, 20, w[3]); im.line(9, 16, 23, 21, w[2])
    im.blob(16, 10.5, 5.2, 7.2, [f[0], f[1], f[2], f[3]], light=(0.0, 0.8))
    im.blob(11.5, 14, 2.6, 3.6, [f[0], f[1], f[2]], light=(0.0, 0.8))
    im.blob(20.5, 13.5, 2.4, 3.4, [f[0], f[1], f[2]], light=(0.0, 0.8))
    for x, y, c in ((16, 2, f[1]), (14, 3, f[0]), (18, 4, f[0]), (10, 9, f[1]), (22, 9, f[1]), (13, 0, f[0]), (19, 1, f[1])):
        im.set(x, y, c)
    return im.outline()


def bench():
    im = Img(24, 11)
    w = PAL["wood"]
    shadow(im, 12, 9, 11, 2)
    im.line(2, 9, 22, 4, w[2]); im.line(2, 8, 22, 3, w[3]); im.line(2, 10, 22, 5, w[1])
    for x, y in ((4, 8), (19, 5)):
        im.rect(x, y, x + 1, y + 2, w[1])
    return im.outline()


# --- The forge ----------------------------------------------------------------------------------------------------------
def forge():
    im = Img(46, 40)
    w, i, s, f, c, r = PAL["wood"], PAL["iron"], PAL["stone"], PAL["fire"], PAL["canvas"], PAL["cloth"]
    shadow(im, 23, 36, 21, 3.5)
    # The hearth at the back: a block of stones with a chimney and the coals glowing in its mouth.
    im.rect(26, 18, 41, 35, s[1]); im.rect(26, 18, 28, 35, s[2]); im.rect(40, 18, 41, 35, s[0])
    for yy in (21, 25, 29, 33):
        im.rect(26, yy, 41, yy, s[0])
    im.rect(29, 6, 38, 18, s[1]); im.rect(29, 6, 31, 18, s[2]); im.rect(37, 6, 38, 18, s[0])
    im.rect(29, 6, 38, 6, s[3])
    im.rect(30, 26, 37, 34, PAL["earth"][0])
    im.blob(33.5, 32, 3.8, 2.4, [f[0], f[1], f[2], f[3]], light=(0.0, 0.8))
    im.blob(33.5, 29, 2.6, 2.2, [f[0], f[1], f[2]], light=(0.0, 0.8))
    # The roof over it: a canopy on four posts.
    for x0, y0 in ((3, 14), (22, 12), (3, 34), (22, 36)):
        im.rect(x0, y0, x0 + 1, 36 if y0 < 34 else 38, w[2])
    poly(im, [(1, 12), (22, 5), (43, 12), (22, 18)], c[2])
    poly(im, [(1, 12), (22, 18), (22, 21), (1, 15)], r[1])
    poly(im, [(22, 18), (43, 12), (43, 15), (22, 21)], r[0])
    im.line(1, 12, 22, 5, c[3]); im.line(22, 5, 43, 12, c[1])
    # The anvil on its stump, the hammer leaning, a bucket.
    im.rect(8, 31, 14, 36, w[2]); im.rect(8, 31, 9, 36, w[3]); im.rect(13, 31, 14, 36, w[1])
    im.rect(6, 27, 16, 30, i[2]); im.rect(6, 27, 16, 27, i[3]); im.rect(4, 28, 6, 29, i[2]); im.rect(16, 28, 18, 28, i[1])
    im.rect(9, 30, 13, 30, i[1])
    im.line(18, 36, 21, 30, w[2]); im.rect(20, 28, 23, 31, i[2]); im.rect(20, 28, 23, 28, i[3])
    return im.outline()


# --- Fence, trough ------------------------------------------------------------------------------------------------------
def fence(alongx=True):
    im = Img(34, 22)
    w = PAL["wood"]
    shadow(im, 17, 17, 15, 3)
    # Posts every 7 pixels along a rail that climbs to the right (x) or the left (y).
    for k in range(5):
        x = 3 + k * 7
        y = 17 - k * 3.5 if alongx else 17 - (4 - k) * 3.5
        x, y = int(x), int(round(y))
        im.rect(x, y - 9, x + 1, y, w[2]); im.rect(x, y - 9, x, y, w[3])
    for off, tone in ((-7, w[3]), (-3, w[2])):
        if alongx:
            im.line(3, 17 + off, 31, 3 + off, tone); im.line(3, 18 + off, 31, 4 + off, w[1])
        else:
            im.line(3, 3 + off, 31, 17 + off, tone); im.line(3, 4 + off, 31, 18 + off, w[1])
    return im.outline()


def trough():
    im = Img(20, 11)
    w, wa = PAL["wood"], ["1a2a3a", "2e4a62", "6a90b0"]
    shadow(im, 10, 9, 9, 2)
    im.rect(1, 3, 18, 8, w[2]); im.rect(1, 3, 18, 3, w[3]); im.rect(1, 8, 18, 8, w[1])
    im.rect(3, 4, 16, 5, wa[1]); im.rect(5, 4, 9, 4, wa[2])
    im.rect(1, 3, 1, 8, w[3]); im.rect(18, 3, 18, 8, w[1])
    return im.outline()


# --- The yard -------------------------------------------------------------------------------------------------------------
def target():
    im = Img(18, 26)
    w, s, c, b = PAL["wood"], PAL["straw"], PAL["cloth"], PAL["bone"]
    shadow(im, 9, 24, 7, 1.8)
    im.line(9, 12, 3, 24, w[2]); im.line(9, 12, 15, 24, w[1]); im.line(9, 12, 9, 24, w[3])
    im.blob(9, 9, 8, 8, [s[0], s[1], s[2], s[3]])
    im.blob(9, 9, 6, 6, [c[1], c[2], c[2], c[3]], light=(0, 0))
    im.blob(9, 9, 4, 4, [b[0], b[1], b[1], b[2]], light=(0, 0))
    im.blob(9, 9, 2, 2, [c[1], c[2], c[2], c[3]], light=(0, 0))
    im.line(11, 8, 15, 4, w[3]); im.set(15, 3, PAL["canvas"][2]); im.set(16, 3, PAL["canvas"][2]); im.set(15, 4, PAL["canvas"][1])
    im.line(7, 11, 4, 14, w[3]); im.set(3, 14, PAL["canvas"][2])
    return im.outline()


def dummy():
    im = Img(16, 28)
    w, s, b = PAL["wood"], PAL["straw"], PAL["parch"]
    shadow(im, 8, 26, 6, 1.8)
    im.rect(7, 12, 8, 26, w[2]); im.rect(7, 12, 7, 26, w[3])
    im.rect(1, 12, 14, 13, w[2]); im.rect(1, 12, 14, 12, w[3])
    im.blob(8, 17, 5.5, 6, [s[0], s[1], s[2], s[3]])
    im.blob(8, 7, 3.6, 3.8, [b[0], b[1], b[2], b[2]])
    im.set(7, 6, "140f1c"); im.set(10, 6, "140f1c"); im.rect(7, 9, 10, 9, "140f1c")
    for x, y in ((3, 14), (13, 14), (4, 19), (12, 20)):
        im.set(x, y, s[3]); im.set(x, y + 1, s[0])
    im.line(1, 13, 1, 17, s[2]); im.line(14, 13, 14, 17, s[2])
    return im.outline()


def cart():
    im = Img(44, 28)
    w, c, i = PAL["wood"], PAL["parch"], PAL["iron"]
    shadow(im, 22, 25, 20, 3)
    # The bed, a canvas cover over hoops, the wheel at the front, the shafts.
    poly(im, [(6, 18), (28, 12), (40, 17), (18, 24)], w[2])
    poly(im, [(6, 18), (18, 24), (18, 27), (6, 21)], w[1])
    poly(im, [(18, 24), (40, 17), (40, 20), (18, 27)], w[0])
    im.blob(24, 11, 12, 7, [c[0], c[1], c[2], c[3]], light=(-0.5, -0.8))
    for x in (13, 19, 25, 31):
        im.line(x, 7 + abs(x - 22) // 4, x - 3, 17 + abs(x - 22) // 8, c[0])
    im.line(3, 14, 10, 20, w[3]); im.line(3, 15, 10, 21, w[2])
    # The wheel at the front: an iron-shod rim, spokes, a brass hub, bigger than the bed's edge so it reads.
    cx, cy, rr = 14, 22, 6
    for k in range(40):
        a = k / 40 * math.tau
        im.set(round(cx + math.cos(a) * rr), round(cy + math.sin(a) * rr * 0.95), i[3] if math.sin(a) < 0.2 else i[2])
        im.set(round(cx + math.cos(a) * (rr - 1)), round(cy + math.sin(a) * (rr - 1) * 0.95), w[3])
    for k in range(4):
        a = k / 4 * math.pi + 0.3
        im.line(cx - math.cos(a) * (rr - 1), cy - math.sin(a) * (rr - 1), cx + math.cos(a) * (rr - 1), cy + math.sin(a) * (rr - 1), w[2])
    im.rect(cx - 1, cy - 1, cx + 1, cy + 1, PAL["brass"][2])
    return im.outline()


def crates():
    im = Img(26, 24)
    w, i = PAL["wood"], PAL["iron"]
    shadow(im, 13, 21, 12, 2.5)
    for (x0, y0, x1, y1) in ((2, 11, 12, 21), (12, 13, 22, 22)):
        im.rect(x0, y0, x1, y1, w[2]); im.rect(x0, y0, x1, y0, w[3]); im.rect(x0, y0, x0, y1, w[3]); im.rect(x1, y0, x1, y1, w[1])
        im.line(x0, y0, x1, y1, w[1]); im.line(x0, y1, x1, y0, w[1])
    im.rect(5, 3, 13, 11, w[2]); im.rect(5, 3, 13, 3, w[3]); im.rect(5, 3, 5, 11, w[3]); im.rect(13, 3, 13, 11, w[1])
    im.line(5, 3, 13, 11, w[1]); im.line(5, 11, 13, 3, w[1])
    im.blob(19, 8, 4.2, 5.2, [w[0], w[1], w[2], w[3]])
    im.rect(15, 6, 23, 6, i[2]); im.rect(15, 10, 23, 10, i[2])
    return im.outline()


def lamp():
    im = Img(9, 28)
    w, b, f = PAL["wood"], PAL["brass"], PAL["fire"]
    im.rect(4, 8, 4, 26, w[2]); im.rect(3, 26, 5, 26, w[1])
    im.line(4, 8, 7, 5, w[3]); im.rect(6, 5, 7, 5, w[3])
    im.rect(5, 6, 8, 11, b[1]); im.rect(5, 6, 8, 6, b[2]); im.rect(6, 7, 7, 10, f[2]); im.set(6, 8, f[3])
    return im.outline()


def banner():
    im = Img(14, 36)
    w, c, r = PAL["wood"], PAL["canvas"], PAL["cloth"]
    im.rect(3, 0, 4, 34, w[2]); im.rect(3, 0, 3, 34, w[3]); im.rect(2, 34, 5, 35, w[1])
    poly(im, [(5, 3), (12, 4.5), (12, 22), (8.5, 19), (5, 22)], c[1])
    poly(im, [(5, 3), (8, 3.5), (8, 20), (5, 22)], c[2])
    im.rect(5, 3, 12, 4, r[2])
    im.line(7, 12, 11, 12, r[3]); im.set(10, 11, r[3]); im.set(10, 13, r[3])
    return im.outline()


# --- The grave of the fallen ---------------------------------------------------------------------------------------------
def grave():
    im = Img(44, 30)
    s, w, e, b, f, c = PAL["stone"], PAL["wood"], PAL["earth"], PAL["bone"], PAL["fire"], PAL["cloth"]
    shadow(im, 22, 26, 20, 3.5)
    im.blob(22, 22, 19, 5.5, [e[0], e[1], e[2], e[3]])
    for x in range(6, 38, 5):
        im.set(x, 21, PAL["grass"][2]); im.set(x + 1, 22, PAL["grass"][1])
    # The stone slab with names cut into it.
    im.rect(15, 6, 29, 21, s[2]); im.rect(15, 6, 29, 6, s[3]); im.rect(15, 6, 15, 21, s[3]); im.rect(29, 6, 29, 21, s[0])
    im.rect(16, 7, 28, 7, s[3])
    for y in (10, 13, 16, 19):
        im.rect(18, y, 26 - (y % 3), y, s[0])
    poly(im, [(15, 6), (22, 1), (29, 6)], s[2]); im.line(15, 6, 22, 1, s[3])
    # Wooden crosses either side, one with a company scarf, candles on the ground.
    for x0, y0 in ((5, 11), (36, 12), (10, 14)):
        im.rect(x0, y0, x0, y0 + 11, w[2]); im.rect(x0 - 2, y0 + 3, x0 + 2, y0 + 3, w[3]); im.rect(x0, y0, x0, y0 + 2, w[3])
    im.line(5, 14, 3, 19, c[2]); im.line(6, 14, 4, 19, c[1])
    for x, y in ((13, 24), (31, 24), (22, 25)):
        im.rect(x, y - 2, x, y, b[2]); im.set(x, y - 3, f[2]); im.set(x, y - 4, f[1])
    return im.outline()


# --- The keepers ------------------------------------------------------------------------------------------------------------
def person(skin, hair, tunic, trim, legs, boots, extra, hood=None, beard=None, bald=False, w=18, h=34):
    """A figure seen from the front: head, torso, arms, legs; `extra` draws what is theirs (cape, apron, tools)."""
    im = Img(w, h)
    cx = w // 2
    shadow(im, cx, h - 2, 6.5, 1.8)
    # Legs and boots.
    im.rect(cx - 3, 22, cx - 1, 30, legs[1]); im.rect(cx + 1, 22, cx + 3, 30, legs[0])
    im.rect(cx - 4, 29, cx - 1, 32, boots[1]); im.rect(cx + 1, 29, cx + 4, 32, boots[0])
    im.rect(cx - 4, 29, cx - 4, 31, boots[2])
    # Torso and arms.
    im.rect(cx - 4, 12, cx + 4, 23, tunic[1]); im.rect(cx - 4, 12, cx - 2, 23, tunic[2]); im.rect(cx + 3, 12, cx + 4, 23, tunic[0])
    im.rect(cx - 4, 22, cx + 4, 23, trim[1])
    im.rect(cx - 6, 13, cx - 5, 21, tunic[2]); im.rect(cx + 5, 13, cx + 6, 21, tunic[0])
    im.rect(cx - 6, 21, cx - 5, 22, skin[2]); im.rect(cx + 5, 21, cx + 6, 22, skin[1])
    # The head.
    im.blob(cx, 7.5, 3.8, 4.4, [skin[0], skin[1], skin[2], skin[3]])
    if hood is not None:
        im.blob(cx, 6.5, 5, 5.4, [hood[0], hood[1], hood[2], hood[2]])
        im.blob(cx, 8.2, 3, 3.4, [skin[0], skin[1], skin[2], skin[3]])
        poly(im, [(cx - 5, 9), (cx - 3, 12), (cx + 3, 12), (cx + 5, 9), (cx + 4, 12), (cx - 4, 12)], hood[1])
    elif not bald:
        im.rect(cx - 4, 3, cx + 4, 5, hair[1]); im.rect(cx - 4, 3, cx - 1, 4, hair[2]); im.rect(cx - 4, 5, cx - 4, 8, hair[1]); im.rect(cx + 4, 5, cx + 4, 7, hair[0])
    im.set(cx - 2, 8, "140f1c"); im.set(cx + 2, 8, "140f1c")
    im.rect(cx - 1, 10, cx + 1, 10, skin[0])
    if beard is not None:
        im.rect(cx - 3, 10, cx + 3, 13, beard[1]); im.rect(cx - 3, 10, cx - 1, 13, beard[2]); im.rect(cx - 1, 13, cx + 1, 14, beard[0])
    extra(im, cx)
    return im.outline()


def brunhilde():
    sk, hr, tn, tr, lg, bt = PAL["skin"], ["5a2a14", "8a4a24", "b06a38"], PAL["canvas"], PAL["cloth"], PAL["earth"], PAL["wood"]

    def extra(im, cx):
        c = PAL["cloth"]
        poly(im, [(cx - 5, 13), (cx - 8, 28), (cx - 3, 26), (cx - 4, 13)], c[1])        # the red cape behind her shoulder
        poly(im, [(cx + 5, 13), (cx + 8, 28), (cx + 3, 26), (cx + 4, 13)], c[0])
        im.rect(cx - 4, 16, cx + 4, 16, PAL["wood"][1])                                  # her belt and the sword at her hip
        im.line(cx + 5, 17, cx + 7, 30, PAL["iron"][2]); im.rect(cx + 5, 16, cx + 7, 17, PAL["brass"][2])
        im.rect(cx - 6, 17, cx - 5, 19, PAL["wood"][3])                                  # bracers
        im.rect(cx + 5, 17, cx + 6, 19, PAL["wood"][2])
        im.line(cx - 5, 5, cx - 5, 12, "b06a38")                                         # a braid over her shoulder
        im.set(cx - 5, 13, PAL["cloth"][2])
    return person(sk, hr, tn, tr, lg, bt, extra)


def thorin():
    sk, hr, tn, tr, lg, bt = PAL["skin"], ["6a6a72", "9a9aa2", "c8c8d0"], ["2a1c12", "4e3220", "74502e"], PAL["wood"], ["2a1c12", "3e2a1c", "4e3220"], PAL["iron"]

    def extra(im, cx):
        ap = PAL["wood"]
        im.rect(cx - 4, 15, cx + 4, 28, ap[1]); im.rect(cx - 4, 15, cx - 2, 28, ap[2]); im.rect(cx + 3, 15, cx + 4, 28, ap[0])
        im.rect(cx - 3, 14, cx + 3, 14, ap[3]); im.rect(cx - 2, 20, cx + 2, 22, ap[0])
        im.rect(cx + 6, 18, cx + 8, 19, PAL["iron"][2]); im.rect(cx + 7, 19, cx + 7, 27, PAL["wood"][2])          # the hammer in his fist
        im.rect(cx + 5, 17, cx + 9, 18, PAL["iron"][3])
        im.rect(cx - 6, 17, cx - 5, 18, PAL["skin"][2])
    return person(sk, hr, tn, tr, lg, bt, extra, beard=["6a6a72", "9a9aa2", "c8c8d0"], bald=True, w=20)


def lyriel():
    sk, hr, tn, tr, lg, bt = PAL["skin"], ["2a1c12", "4e3220", "74502e"], ["283a30", "42584a", "5c7a62"], PAL["wood"], ["1c2a22", "2a3e32", "3a5240"], PAL["wood"]

    def extra(im, cx):
        g = ["283a30", "42584a", "5c7a62", "7a9a80"]
        poly(im, [(cx - 5, 12), (cx - 7, 30), (cx + 7, 30), (cx + 5, 12)], g[1])
        poly(im, [(cx - 5, 12), (cx - 7, 30), (cx - 2, 30), (cx - 3, 12)], g[2])
        im.rect(cx - 3, 13, cx + 3, 14, PAL["wood"][3])
        im.line(cx + 6, 17, cx + 8, 24, PAL["wood"][2]); im.line(cx + 7, 24, cx + 7, 29, PAL["cloth"][2])          # a leash
        im.set(cx + 4, 3, PAL["cloth"][3]); im.line(cx + 4, 2, cx + 6, 0, PAL["bone"][2])                            # a feather
    return person(sk, hr, tn, tr, lg, bt, extra, hood=["283a30", "42584a", "5c7a62"])


def wolf():
    im = Img(24, 18)
    g = PAL["grey"]
    shadow(im, 12, 16, 9, 2)
    im.blob(12, 11, 7.5, 5, [g[0], g[1], g[2], g[3]])              # the body, sitting
    im.blob(17, 6, 3.6, 3.4, [g[0], g[1], g[2], g[3]])             # the head
    poly(im, [(15, 4), (15.5, 0), (17.5, 3)], g[1]); poly(im, [(18, 3), (19.5, 0), (20.5, 4)], g[0])
    im.set(18, 6, "ffd84a"); im.rect(19, 8, 21, 8, "140f1c"); im.rect(20, 8, 21, 9, g[0])
    im.line(5, 14, 2, 10, g[2]); im.line(4, 15, 1, 11, g[1])      # the tail
    im.rect(10, 13, 11, 16, g[1]); im.rect(14, 13, 15, 16, g[0])
    return im.outline()


# --- Writing ---------------------------------------------------------------------------------------------------------------
# name: (maker, foot) where foot is the pivot's height above the bottom of the picture in drawn pixels.
PIECES = {
    "camp_billboard": (billboard, 4),
    "camp_tent_1": (lambda: tent(False), 4),
    "camp_tent_2": (lambda: tent(True), 4),
    "camp_fire": (fire, 6),
    "camp_bench": (bench, 3),
    "camp_forge": (forge, 6),
    "camp_fence_x": (lambda: fence(True), 5),
    "camp_fence_y": (lambda: fence(False), 5),
    "camp_trough": (trough, 3),
    "camp_target": (target, 3),
    "camp_dummy": (dummy, 3),
    "camp_cart": (cart, 5),
    "camp_crates": (crates, 4),
    "camp_lamp": (lamp, 2),
    "camp_banner": (banner, 2),
    "camp_grave": (grave, 6),
    "camp_brunhilde": (brunhilde, 3),
    "camp_thorin": (thorin, 3),
    "camp_lyriel": (lyriel, 3),
    "camp_wolf": (wolf, 3),
}


def main():
    written, made = [], []
    for name, (make, foot) in PIECES.items():
        im = make()
        write_png(os.path.join(ART, "World", name + ".png"), im.rows())
        written.append(f"World/{name} {im.w * SCALE} {im.h * SCALE} {im.w * SCALE / 2:g} {foot * SCALE:g}")
        made.append((name, im))
    with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "camp_art_sizes.txt"), "w") as fh:
        fh.write("\n".join(written) + "\n")

    # A preview sheet: every piece on a grass green, in rows.
    sheet = Img(300, 190)
    for y in range(sheet.h):
        for x in range(sheet.w):
            sheet.set(x, y, PAL["grass"][0] if (x // 4 + y // 4) % 2 == 0 else "20402a")
    x, y, row_h = 4, 4, 0
    for name, im in made:
        if x + im.w + 4 > sheet.w:
            x, y, row_h = 4, y + row_h + 4, 0
        sheet.paste(im, x, y)
        x += im.w + 4
        row_h = max(row_h, im.h)
    out = os.path.join(P, "ArtSource", "pixel", "previews")
    os.makedirs(out, exist_ok=True)
    write_png(os.path.join(out, "camp_art_16.png"), sheet.rows(4))
    print("wrote", len(written), "camp pieces")


if __name__ == "__main__":
    main()
