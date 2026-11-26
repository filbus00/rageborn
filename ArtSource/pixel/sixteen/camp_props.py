# The camp's props with more life in them (2026-11-22, the owner: "rework the assets"): each is the piece camp_art.py draws, with
# detail added on top (patches, ropes, stakes, arrows in the targets, a wreath on the grave, wood grain and worn stone) and
# outlined again. Plain Python, no Blender.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gaol_art import Img, hash01, K  # noqa: E402
from camp_pal import PAL, poly  # noqa: E402

W, I, S, C, R, F, P_, B, E, G = (PAL[k] for k in ("wood", "iron", "stone", "canvas", "cloth", "fire", "parch", "brass", "earth", "grass"))


def _art():
    import camp_art  # the plain pieces, drawn by the first pass
    return camp_art


def texture(im, seed=1):
    """Wood grain, worn stone and canvas weave: a pixel of a mid tone sometimes takes the tone below it, so the flat colours
    stop reading as flat."""
    step = {W[2]: W[1], W[3]: W[2], S[2]: S[1], S[3]: S[2], C[2]: C[1], C[3]: C[2], I[2]: I[1]}
    chance = {W[2]: 0.13, W[3]: 0.09, S[2]: 0.12, S[3]: 0.08, C[2]: 0.05, C[3]: 0.04, I[2]: 0.06}
    for y in range(im.h):
        for x in range(im.w):
            c = im.get(x, y)
            if c in step and hash01(x, y, seed) < chance[c]:
                im.set(x, y, step[c])
    return im


def arrow(im, x0, y0, x1, y1):
    """An arrow stuck in something: a shaft from its head (x0, y0) out to the fletching (x1, y1)."""
    im.line(x0, y0, x1, y1, W[3])
    im.set(x1, y1, R[2])
    im.set(x1 + (1 if x1 > x0 else -1), y1 - 1, R[1])
    im.set(x1 + (1 if x1 > x0 else -1), y1, R[2])


def tent(flip=False):
    im = _art().tent(False)
    c, r, w = C, R, W
    # Patches stitched on the canvas, one on each face.
    im.rect(3, 23, 5, 26, c[3])
    im.rect(3, 23, 5, 23, c[1]); im.rect(3, 26, 5, 26, c[1]); im.set(4, 24, c[1]); im.set(4, 25, c[2])
    im.rect(30, 16, 34, 19, c[2])
    im.rect(30, 16, 34, 16, c[0]); im.rect(30, 19, 34, 19, c[0]); im.set(32, 17, c[0]); im.set(32, 18, c[1])
    # Guy ropes down to stakes.
    im.line(2, 22, 0, 29, P_[1]); im.rect(0, 29, 1, 32, w[2]); im.set(0, 29, w[3])
    im.line(40, 22, 41, 29, P_[0]); im.rect(40, 29, 41, 32, w[1])
    # The door flap tied back, and the cord that ties it.
    im.rect(7, 21, 9, 27, c[3]); im.line(10, 19, 8, 24, P_[2])
    im.set(8, 20, c[3])
    # A bedroll in the doorway.
    im.rect(10, 26, 15, 28, w[1]); im.rect(10, 26, 15, 26, r[2]); im.rect(10, 27, 15, 27, r[1])
    # The pennant: swallow-tailed.
    poly(im, [(22, 0), (29, 0.5), (27, 1.5), (29, 3), (22, 3)], r[3])
    texture(im, 2)
    im.outline()
    return im.mirrored() if flip else im


def billboard():
    im = _art().billboard()
    # Arrows in the board, a wax seal and a ribbon on one notice, a cord with a scroll hung from the roof.
    arrow(im, 29, 18, 34, 13)
    arrow(im, 8, 21, 4, 25)
    im.rect(15, 16, 17, 18, R[1]); im.set(16, 17, R[3]); im.set(15, 16, R[3])
    im.line(13, 22, 11, 27, P_[1]); im.rect(9, 27, 13, 28, P_[3]); im.set(9, 27, P_[1]); im.set(13, 28, P_[1])
    # A hanging bell: summons.
    im.line(3, 22, 3, 24, I[2]); im.rect(2, 25, 4, 27, B[1]); im.set(2, 25, B[2]); im.set(3, 28, B[0])
    texture(im, 3)
    return im.outline()


def fire():
    im = _art().fire()
    # A pot on a stone at the fire's side, more stones, sparks and ash.
    im.blob(26, 15.5, 3.2, 2.8, [I[0], I[1], I[2], I[3]])
    im.rect(23, 13, 29, 13, I[3]); im.rect(24, 12, 28, 12, I[1])
    im.line(24, 11, 22, 8, I[2]); im.line(28, 11, 30, 8, I[2])
    im.set(26, 14, F[1]); im.set(26, 15, F[0])
    for x, y, c in ((12, 1, F[2]), (20, 0, F[3]), (8, 6, F[1]), (24, 5, F[2]), (15, 0, F[1])):
        im.set(x, y, c)
    for x, y in ((7, 18), (25, 20), (14, 21), (18, 20)):
        im.set(x, y, E[3])
    texture(im, 4)
    return im.outline()


def forge():
    im = _art().forge()
    # A rack of tools on the front-left post, a water barrel at the right, sparks over the anvil, a bellows at the hearth.
    for k, (x, tone) in enumerate(((2, I[3]), (4, I[2]), (6, W[3]))):
        im.line(x, 22, x, 27, tone)
    im.rect(1, 22, 7, 22, W[1])
    im.set(2, 28, I[3]); im.set(3, 28, I[3]); im.set(6, 28, I[2])
    im.rect(38, 30, 44, 38, W[2]); im.rect(38, 30, 44, 30, W[3]); im.rect(38, 33, 44, 33, I[2]); im.rect(38, 36, 44, 36, I[2])
    im.rect(38, 30, 38, 38, W[3]); im.rect(44, 30, 44, 38, W[1]); im.rect(39, 31, 43, 31, PAL["grass"][1])
    for x, y, c in ((9, 24, F[2]), (12, 22, F[3]), (15, 25, F[1]), (7, 25, F[1])):
        im.set(x, y, c)
    poly(im, [(21, 31), (26, 29), (26, 34), (21, 35)], W[2]); im.line(21, 31, 25, 30, W[3]); im.set(20, 33, I[2])
    texture(im, 5)
    return im.outline()


def cart():
    im = _art().cart()
    # Sacks and a barrel on the ground beside it, a lantern on the shaft, a red stripe along the cover.
    im.blob(5, 20, 3.4, 3.6, [PAL["straw"][0], PAL["straw"][1], PAL["straw"][2], PAL["straw"][3]])
    im.rect(4, 16, 6, 16, P_[1])
    im.rect(38, 17, 42, 22, W[2]); im.rect(38, 17, 42, 17, W[3]); im.rect(38, 19, 42, 19, I[2]); im.rect(38, 21, 42, 21, I[2])
    im.rect(42, 17, 42, 22, W[1])
    im.line(14, 8, 33, 14, R[2]); im.line(14, 9, 33, 15, R[1])
    im.set(3, 15, B[2]); im.set(3, 16, F[2])
    texture(im, 6)
    return im.outline()


def crates():
    im = _art().crates()
    # Rope round the top crate, a company arrow stencilled on one, a sack on the side.
    im.rect(5, 7, 13, 7, P_[2]); im.rect(9, 3, 9, 11, P_[1])
    im.line(3, 16, 8, 16, R[2]); im.set(7, 15, R[2]); im.set(7, 17, R[2])
    im.rect(13, 16, 18, 16, R[1]); im.rect(15, 18, 20, 18, W[0])
    texture(im, 7)
    return im.outline()


def target():
    im = _art().target()
    arrow(im, 9, 9, 14, 3)
    arrow(im, 7, 7, 3, 3)
    im.set(10, 11, W[3]); im.set(11, 12, W[3]); im.line(10, 11, 13, 15, W[3]); im.set(13, 16, R[2]); im.set(14, 16, R[1])
    texture(im, 8)
    return im.outline()


def dummy():
    im = _art().dummy()
    arrow(im, 7, 15, 3, 19)
    im.rect(4, 13, 5, 16, R[2]); im.rect(11, 13, 12, 16, R[1])       # the company's red armbands
    im.rect(6, 22, 10, 22, PAL["straw"][0]); im.rect(5, 15, 5, 15, PAL["straw"][0])
    texture(im, 9)
    return im.outline()


def grave():
    im = _art().grave()
    # A laurel wreath with a red ribbon on the slab, flowers and a bow leaning on the stone.
    for k in range(14):
        a = k / 14 * math.tau
        im.set(round(22 + math.cos(a) * 4), round(12 + math.sin(a) * 3.4), PAL["canvas"][2] if k % 2 else PAL["canvas"][3])
    im.set(22, 16, R[3]); im.set(21, 17, R[2]); im.set(23, 17, R[2])
    for x, y, c in ((16, 25, R[3]), (19, 26, B[2]), (27, 26, R[3]), (30, 25, B[2]), (24, 27, R[2])):
        im.set(x, y, c); im.set(x, y + 1, G[2])
    for y in range(10, 22):
        x = 12 - round(math.sin((y - 10) / 11 * math.pi) * 2)
        im.set(x, y, W[3])
    im.line(12, 10, 12, 21, P_[2])
    texture(im, 10)
    return im.outline()


def banner():
    im = _art().banner()
    # A brass ball on the pole and a wave in the cloth.
    im.rect(3, 0, 4, 0, B[2]); im.set(3, 0, B[3])
    poly(im, [(5, 22), (8.5, 19), (12, 22), (12, 25), (8.5, 22), (5, 25)], PAL["canvas"][1])
    im.line(5, 25, 8, 22, R[2]); im.line(8, 22, 12, 25, R[1])
    texture(im, 11)
    return im.outline()


def lamp():
    im = _art().lamp()
    im.set(6, 4, I[2]); im.set(7, 4, I[1]); im.set(5, 12, B[1]); im.set(8, 12, B[0])
    texture(im, 12)
    return im.outline()


def bench():
    im = _art().bench()
    for x, y in ((6, 8), (12, 6), (17, 4)):
        im.set(x, y, PAL["grass"][2])
    texture(im, 13)
    return im.outline()


def trough():
    im = _art().trough()
    for x in (3, 9, 14):
        im.set(x, 3, PAL["grass"][2])
    texture(im, 14)
    return im.outline()


def fence(alongx=True):
    im = _art().fence(alongx)
    for x, y in ((6, 14), (15, 10), (24, 6)) if alongx else ((6, 7), (15, 11), (24, 15)):
        im.set(x, y, PAL["grass"][2])
    texture(im, 15 if alongx else 16)
    return im.outline()


def fletcher_bench():
    """Oswin's bench (2026-11-26): a trestle table with a bow laid on it, bundles of shafts and a pot of feathers, and a rack
    behind it of bows hung to season."""
    im = Img(34, 30)
    # The rack behind: two posts, a bar, three bows hung from it.
    im.rect(3, 2, 4, 20, W[1]); im.rect(29, 2, 30, 20, W[1]); im.rect(3, 2, 30, 3, W[2]); im.rect(3, 2, 30, 2, W[3])
    for bx in (9, 16, 23):
        for y in range(4, 17):
            im.set(bx - round(2.2 * math.sin(math.pi * (y - 4) / 12)), y, W[3] if y < 10 else W[2])
        im.line(bx, 4, bx, 16, P_[3])
    # The table: its top in light wood, its front darker, legs.
    poly(im, [(1, 18), (31, 15), (33, 19), (3, 23)], W[2])
    poly(im, [(3, 23), (33, 19), (33, 21), (3, 25)], W[1])
    im.line(1, 18, 31, 15, W[3])
    for x, y in ((4, 24), (30, 20)):
        im.rect(x, y, x + 1, y + 5, W[0])
    # A bow being strung, laid on the table.
    im.line(5, 19, 24, 16, W[3]); im.line(5, 20, 24, 17, P_[3])
    # Bundles of shafts tied with red, and a pot of feathers.
    for x in (25, 27):
        im.rect(x, 11, x + 1, 17, P_[2]); im.set(x, 14, R[2]); im.set(x + 1, 14, R[2])
    im.rect(14, 15, 17, 18, I[1]); im.rect(14, 15, 17, 15, I[2])
    for x, c in ((14, R[3]), (15, "f2eede"), (16, R[2]), (17, "f2eede")):
        im.set(x, 14, c); im.set(x, 13, c if x % 2 else R[1])
    texture(im, 21)
    return im.outline()


def jeweller_table():
    """Mirela's table (2026-11-26): a small table under a blue cloth with a gold fringe, a lamp, a little chest open on rings
    and stones, a scale."""
    im = Img(30, 28)
    blue = ["1a2246", "2e3e7a", "4a62a8", "7a90d0"]
    # The table under its cloth.
    poly(im, [(2, 13), (24, 10), (28, 14), (6, 17)], blue[2])
    poly(im, [(6, 17), (28, 14), (28, 22), (6, 26)], blue[1])
    poly(im, [(2, 13), (6, 17), (6, 26), (2, 22)], blue[0])
    im.line(2, 13, 24, 10, blue[3])
    for x in range(6, 29, 2):
        im.set(x, 26 - round((x - 6) * 4 / 22), B[2])
    # The open chest of rings and stones.
    im.rect(8, 10, 14, 13, W[1]); im.rect(8, 7, 14, 9, W[2]); im.rect(8, 7, 14, 7, W[3])
    im.rect(9, 10, 13, 11, "3a1a24")
    for x, c in ((9, "e33a2e"), (10, B[2]), (11, "2fd4c4"), (12, B[3]), (13, "8a4ac8")):
        im.set(x, 10, c)
    # A brass scale, its two pans.
    im.rect(19, 4, 19, 11, B[1]); im.rect(16, 4, 22, 4, B[2]); im.rect(15, 6, 17, 6, B[2]); im.rect(21, 7, 23, 7, B[2])
    im.line(16, 4, 15, 6, B[0]); im.line(22, 4, 23, 7, B[0])
    # A little lamp at the back.
    im.rect(24, 4, 25, 9, B[1]); im.set(24, 3, PAL["fire"][2]); im.set(25, 2, PAL["fire"][3])
    texture(im, 22)
    return im.outline()


# name: (maker, foot) for the pieces redone here; camp_art.py takes these over its own.
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
    "camp_fletcher_bench": (fletcher_bench, 4),
    "camp_jeweller_table": (jeweller_table, 4),
}
