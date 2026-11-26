# Every item picture in the Bag and the menus, redrawn (2026-11-26, the owner: "rework all the gear art in menues, it looks
# bad"): 32 pixels a backpack cell (the old item_art.py drew 16), so a bow is 64 x 128 and a ring 32 x 32. Each piece is
# built of shaped parts lit as rounded forms from the top left by item_kit.py, metals and stones with a hard highlight, a
# coloured outline, and fills its slot. Same names as before (ItemArtNames), written to Resources/UI/ItemArt, and a preview
# sheet to ArtSource/pixel/previews/item_art_32.png.
# Run: python3 ArtSource/pixel/sixteen/item_art_32.py            (all)
#      python3 ArtSource/pixel/sixteen/item_art_32.py preview a b  (only a preview of those, to the scratch path given by ITEM_PREVIEW)
import os, sys, math
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from item_kit import Pic, M, fixed, ramp, write_png  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(P, "Assets", "_Project", "Resources", "UI", "ItemArt")
WHITE = fixed("ffffff")


# --- Bows: 64 x 128, standing, the limbs bowed to the left, the string on the right -----------------------------------
def bow(wood, length=0.8, bulge=17, thick=6, grip="leather", tips="bone", recurve=0.0, bands=(), string="string", mark=None):
    p = Pic(64, 128)
    half = 62 * length
    mid = 64.0
    sx = 44.0

    def at(u):
        """The limb's centre at u (-1 the top tip, 1 the bottom)."""
        x = sx - bulge * (1 - u * u)
        if recurve and abs(u) > 0.6:
            x += recurve * ((abs(u) - 0.6) / 0.4) ** 2
        return x, mid + u * half

    for sign in (-1, 1):
        pts = [at(sign * k / 20) for k in range(0, 21)]
        p.part(p.stroke(pts, thick * 1.35, max(3, thick * 0.5)), wood, round_=thick * 0.6, bias=0.06 if sign < 0 else -0.04, tex=0.08, seed=3 + sign)
    top, bot = at(-1), at(1)
    # The string, from nock to nock, in front of nothing.
    sp = [(top[0] + 1, top[1] + 2), (bot[0] + 1, bot[1] - 2)]
    p.part(p.stroke(sp, 1.4), string, flat=True, edge=False)
    # The tips: capped nocks.
    for t in (top, bot):
        p.part(p.ellipse(t[0], t[1], 2.4, 3.2), tips, round_=2)
    # Bands round the limbs.
    for frac, mat in bands:
        for sign in (-1, 1):
            u = sign * frac
            cx, cy = at(u)
            p.part(p.ellipse(cx, cy, thick * 0.62, 2.4), mat, round_=1.5)
    # The grip: a wrapped handle, wider, with an arrow shelf above it.
    gx, gy = at(0)
    g = p.part(p.rect(gx - thick * 0.75, gy - 9, gx + thick * 0.75, gy + 9, 2), grip, round_=3)
    for y in range(int(gy - 8), int(gy + 9), 3):
        p.line([(gx - thick * 0.75, y), (gx + thick * 0.75, y + 2)], grip, 0)
    p.part(p.ellipse(gx + thick * 0.5, gy - 11, 2.4, 1.8), wood, round_=1.5)
    if mark:
        mark(p, at, thick)
    return p.finish()


def m_splinter(p, at, t):
    for u in (-0.8, -0.55, -0.3, 0.3, 0.55, 0.8):
        x, y = at(u)
        p.part(p.stroke([(x - 1, y), (x - 7, y - 5 if u < 0 else y + 5)], 2.4, 1), "green", round_=1)
        p.part(p.ellipse(x - 8, y - 6 if u < 0 else y + 6, 2.6, 1.6), "feather_green", round_=1)


def m_ember(p, at, t):
    for u in (-0.75, -0.45, -0.15, 0.2, 0.5, 0.8):
        x, y = at(u)
        p.part(p.ellipse(x - 1, y, 1.6, 2.6), "glow_ember", round_=1, edge=False)


def m_promise(p, at, t):
    x, y = at(0)
    p.gem(x - 1, y, 3.6, "gem_red")


def m_gale(p, at, t):
    for u in (-0.6, 0.6):
        x, y = at(u)
        d = -1 if u < 0 else 1
        p.part(p.stroke(p.curve((x - 2, y), (x - 10, y + d * 2), (x - 13, y + d * 9)), 2.4, 1), "glow_teal", round_=1)


def m_widow(p, at, t):
    for u in (-0.7, -0.4, 0.4, 0.7):
        x, y = at(u)
        p.part(p.ellipse(x - 1, y, 2.2, 1.6), "red", round_=1)
    x, y = at(0)
    p.gem(x - 1, y, 3.2, "gem_red")


def m_gallows(p, at, t):
    x, y = at(-0.82)
    p.part(p.stroke([(x + 3, y), (x + 4, y + 12)], 2), "rope", round_=1)
    noose = p.ellipse(x + 4, y + 17, 4, 4.6) & ~p.ellipse(x + 4, y + 17, 2, 2.6)
    p.part(noose, "rope", round_=1.5, tex=0.2)


def m_still(p, at, t):
    for u in (-0.75, -0.4, 0.4, 0.75):
        x, y = at(u)
        p.part(p.poly([(x - 3, y), (x, y - 3), (x + 1, y), (x, y + 3)]), "ice", round_=1)
    x, y = at(0)
    p.gem(x - 1, y, 3.4, "ice")


def m_silence(p, at, t):
    for u in (-0.7, -0.35, 0.35, 0.7):
        x, y = at(u)
        p.part(p.ellipse(x - 1, y, 1.4, 2.2), "glow_violet", round_=1, edge=False)
    x, y = at(0)
    p.gem(x - 1, y, 3.6, "gem_violet")


# --- Quivers: 64 x 96, a tube leaning right, arrows standing out of it ---------------------------------------------------
def quiver(body, trim="leather", fletch="feather_red", studs=None, ribs=False, arrows=5, mark=None, shaft="pale_wood"):
    p = Pic(64, 96)
    lean = 0.2

    def x_at(y, x):
        return x + (y - 30) * lean

    # Arrows first, behind the rim: shafts and fletchings at a few heights.
    for k in range(arrows):
        bx = 17 + k * 5.6
        top = 6 + (k % 2) * 5 + (k % 3) * 2
        p.part(p.stroke([(x_at(30, bx) + 1, 34), (bx - 1, top + 10)], 2.2), shaft, round_=1)
        for side in (-1, 1):
            vane = p.poly([(bx - 1, top + 1), (bx - 1 + side * 3.6, top + 4), (bx - 1 + side * 3.2, top + 13), (bx - 1, top + 11)])
            p.part(vane, fletch, round_=2, tex=0.1, seed=k * 2 + side, bias=0.1 if side < 0 else -0.1)
        p.line([(bx - 1, top), (bx - 1, top + 12)], shaft, 1)
        p.part(p.ellipse(bx - 1, top - 1, 1.6, 1.6), "bone", round_=1)
    # The tube.
    tube = p.poly([(x_at(28, 12), 28), (x_at(28, 44), 28), (x_at(86, 42), 86), (x_at(92, 36), 92), (x_at(92, 20), 92), (x_at(86, 14), 86)])
    p.part(tube, body, round_=9, tex=0.1, seed=7)
    # The seam stitched down it.
    for y in range(36, 84, 4):
        p.line([(x_at(y, 33), y), (x_at(y + 2, 33), y + 2)], body, 0)
    # The rim and the base cap.
    p.part(p.poly([(x_at(24, 10), 24), (x_at(24, 46), 24), (x_at(33, 46), 33), (x_at(33, 10), 33)]), trim, round_=3)
    p.part(p.poly([(x_at(82, 13), 82), (x_at(82, 43), 82), (x_at(86, 42), 86), (x_at(92, 36), 92), (x_at(92, 20), 92), (x_at(86, 14), 86)]),
           trim, round_=3)
    # The strap across it, with a buckle.
    strap = p.stroke([(4, 38), (60, 70)], 5)
    p.part(strap, "dark_leather", round_=2)
    p.part(p.rect(29, 50, 36, 57, 1), "iron", round_=2)
    p.part(p.rect(31, 52, 34, 55), "dark_leather", flat=True, edge=False)
    if studs:
        for y in range(40, 76, 9):
            for x in (17, 38):
                p.rivet(x_at(y, x), y, studs)
    if ribs:
        for y in range(40, 78, 7):
            p.part(p.stroke([(x_at(y, 13), y), (x_at(y, 43), y + 1)], 2.4), "bone", round_=1)
    if mark:
        mark(p, x_at)
    return p.finish()


def q_splinters(p, x_at):
    for k in range(6):
        x = 16 + k * 5
        p.part(p.stroke([(x_at(30, x), 30), (x - 6, 4 + (k % 3) * 3)], 2.6, 1.2), "green", round_=1)
    for y in (48, 64):
        p.part(p.ellipse(x_at(y, 26), y, 2.2, 2.2), "glow_teal", round_=1, edge=False)


def q_ashfall(p, x_at):
    for x, y in ((20, 44), (32, 56), (22, 68), (36, 76), (28, 40)):
        p.part(p.ellipse(x_at(y, x), y, 1.6, 1.6), "glow_ember", round_=1, edge=False)


def q_wind(p, x_at):
    for y in (46, 66):
        p.part(p.stroke(p.curve((x_at(y, 6), y + 2), (x_at(y, 28), y - 6), (x_at(y, 52), y - 2)), 2, 1), "glow_teal", round_=1)


def q_magpie(p, x_at):
    for k, y in enumerate(range(40, 80, 8)):
        p.part(p.poly([(x_at(y, 14), y), (x_at(y, 42), y), (x_at(y + 4, 42), y + 4), (x_at(y + 4, 14), y + 4)]),
               "feather_white" if k % 2 else "feather_black", round_=2)
    p.gem(x_at(60, 28), 60, 3.2, "gem_teal")


def q_hound(p, x_at):
    x, y = x_at(58, 27), 58
    p.part(p.ellipse(x, y, 8, 7), "bone", round_=4)
    p.part(p.rect(x - 4, y + 5, x + 4, y + 10, 2), "bone", round_=2)
    for ex in (x - 3.5, x + 3.5):
        p.part(p.ellipse(ex, y - 1, 2, 2.2), "gem_red", round_=1, shine=True)


# --- Chest pieces: 64 x 96, a coat or a cuirass seen from the front -----------------------------------------------------
def chest(body, trim="leather", sleeves=None, shoulders=None, collar=None, kind="coat", accent=None, mark=None):
    p = Pic(64, 96)
    sl = sleeves or body
    # Sleeves, then the body over them.
    for side in (-1, 1):
        cx = 32 + side * 20
        arm = p.poly([(32 + side * 12, 14), (32 + side * 25, 20), (32 + side * 29, 58), (32 + side * 21, 60), (32 + side * 16, 30)])
        p.part(arm, sl, round_=5, tex=0.06, seed=4 + side, bias=0.05 if side < 0 else -0.05)
        p.part(p.ellipse(cx + side * 5, 58, 5, 3.4), trim, round_=2)
    torso = p.poly([(16, 12), (48, 12), (50, 40), (47, 70), (50, 88), (14, 88), (17, 70), (14, 40)])
    p.part(torso, body, round_=11, tex=0.07, seed=9)
    # The neck opening and the collar.
    if collar:
        p.part(p.poly([(20, 8), (44, 8), (40, 20), (32, 24), (24, 20)]), collar, round_=4)
    p.part(p.poly([(25, 10), (39, 10), (35, 19), (32, 21), (29, 19)]), fixed("140e16", "1c141c", "241a22", "2c2028", "342630"), flat=True)
    # A belt across the waist.
    p.part(p.rect(15, 66, 49, 72, 1), trim, round_=2)
    p.part(p.rect(29, 64, 35, 74, 1), "iron" if trim != "gold" else "gold", round_=2)
    p.part(p.rect(31, 67, 33, 71), trim, flat=True, edge=False)
    if shoulders:
        for side in (-1, 1):
            p.part(p.ellipse(32 + side * 18, 17, 10, 7, angle=side * 0.25), shoulders, round_=4)
    if kind == "coat":
        p.line([(32, 22), (32, 66)], body, 0)
        for y in range(28, 64, 8):
            p.rivet(35, y, "bronze")
        p.line([(24, 74), (22, 88)], body, 0)
        p.line([(40, 74), (42, 88)], body, 0)
    if kind == "studded":
        for y in range(26, 64, 7):
            for x in range(21, 45, 6):
                p.rivet(x + (3 if (y // 7) % 2 else 0), y, "silver")
    if kind == "scales":
        for y in range(24, 64, 5):
            for x in range(18, 48, 5):
                off = 2.5 if (y // 5) % 2 else 0
                p.part(p.ellipse(x + off, y, 3, 3.2) & p.poly([(0, y - 1), (64, y - 1), (64, y + 4), (0, y + 4)]) & torso, body, round_=2)
    if kind == "plate":
        p.part(p.poly([(20, 20), (44, 20), (45, 44), (32, 62), (19, 44)]), body, round_=8, shine=True)
        p.line([(32, 22), (32, 58)], body, 0)
    if accent:
        mark_strip = p.rect(29, 22, 35, 86)
        p.part(mark_strip & torso, accent, round_=2)
    if mark:
        mark(p, torso)
    return p.finish()


def p_ranger(p, torso):
    p.part(p.stroke([(18, 16), (46, 64)], 4), "teal", round_=2)


def p_knight(p, torso):
    p.part(p.rect(30, 26, 34, 54), "gold", round_=1.5)
    p.part(p.rect(24, 34, 40, 38), "gold", round_=1.5)


def p_stag(p, torso):
    for x, y in ((22, 34), (40, 34), (24, 50), (38, 50)):
        p.part(p.ellipse(x, y, 2.6, 2), "bone", round_=1)


def p_cinder(p, torso):
    for pts in (((20, 24), (26, 36), (22, 48)), ((42, 22), (38, 34), (44, 46), (40, 60)), ((30, 50), (34, 62), (30, 78))):
        p.part(p.stroke(list(pts), 2, 1.2), "glow_ember", round_=1, edge=False)


# --- Helms: 64 x 64 --------------------------------------------------------------------------------------------------------
SHADOW = fixed("0c080e", "140e16", "1c141c", "241a22", "2c2028")


def helm(kind, mat, trim=None):
    p = Pic(64, 64)
    if kind == "hood":
        # A cowl over the shoulders: the hood rising to a soft point, folds running down it, the face deep in shadow, and
        # the mantle spreading to the shoulders below.
        mantle = p.poly([(2, 62), (6, 46), (18, 40), (46, 40), (58, 46), (62, 62)])
        p.part(mantle, mat, round_=8, tex=0.08, seed=5, bias=-0.05)
        cowl = p.poly([(10, 50), (12, 28), (20, 12), (30, 4), (38, 3), (44, 10), (52, 28), (54, 50), (32, 56)])
        p.part(cowl, mat, round_=12, tex=0.08, seed=2)
        p.part(p.ellipse(32, 33, 12, 15) & p.poly([(0, 18), (64, 18), (64, 64), (0, 64)]), SHADOW, flat=True)
        p.part(p.ellipse(32, 36, 7.5, 9), fixed("060406", "080608", "0a080a", "0e0a0e", "120e12"), flat=True, edge=False)
        for pts in (((17, 20), (14, 34), (15, 48)), ((47, 20), (50, 34), (49, 48)), ((26, 8), (24, 16))):
            p.line(list(pts), mat, 0)
        p.part(p.stroke(p.curve((19, 22), (32, 14), (45, 22)), 3.4), mat, round_=1.5, bias=0.25)
        if trim:
            p.part(p.stroke(p.curve((19, 22), (32, 14), (45, 22)), 2.4), trim, round_=1)
    elif kind == "mask":
        face = p.poly([(12, 18), (20, 8), (44, 8), (52, 18), (52, 40), (44, 54), (32, 60), (20, 54), (12, 40)])
        p.part(face, mat, round_=10, tex=0.06)
        for ex in (23, 41):
            p.part(p.ellipse(ex, 28, 5.5, 3.2, angle=0.15 if ex < 32 else -0.15), SHADOW, flat=True)
        p.part(p.poly([(32, 26), (35, 40), (32, 44), (29, 40)]), mat, round_=3)
        p.line([(26, 48), (38, 48)], mat, 0)
        if trim:
            p.part(p.stroke(p.curve((12, 18), (32, 4), (52, 18)), 3), trim, round_=1.5)
            p.part(p.stroke([(32, 8), (32, 22)], 2.4), trim, round_=1)
    elif kind in ("barbute", "visored", "horned"):
        dome = p.poly([(10, 58), (10, 26), (18, 10), (32, 4), (46, 10), (54, 26), (54, 58), (40, 60), (24, 60)])
        p.part(dome, mat, round_=14, shine=True)
        if kind == "barbute":
            p.part(p.rect(16, 26, 48, 31, 1), SHADOW, flat=True)
            p.part(p.rect(29, 26, 35, 56, 1), SHADOW, flat=True)
            p.line([(32, 6), (32, 24)], mat, 1)
        if kind == "visored":
            visor = p.poly([(11, 24), (53, 24), (53, 46), (32, 52), (11, 46)])
            p.part(visor, mat, round_=6, shine=True, bias=-0.05)
            for y in (30, 36):
                p.part(p.rect(16, y, 48, y + 2), SHADOW, flat=True)
            for x in range(20, 46, 5):
                p.rivet(x, 44, "iron")
            if trim:
                plume = p.stroke(p.curve((32, 6), (40, -6), (56, 4)), 7, 3)
                p.part(plume, trim, round_=3, tex=0.15)
        if kind == "horned":
            for side in (-1, 1):
                horn = p.stroke(p.curve((32 + side * 18, 18), (32 + side * 30, 12), (32 + side * 28, 0)), 7, 2)
                p.part(horn, "ivory", round_=3)
            p.part(p.rect(18, 28, 46, 33, 1), SHADOW, flat=True)
            p.part(p.rect(30, 28, 34, 50, 1), SHADOW, flat=True)
            if trim:
                p.part(p.stroke([(10, 40), (54, 40)], 3), trim, round_=1.5)
        p.part(p.stroke(p.curve((10, 56), (32, 62), (54, 56)), 3), mat if not trim else trim, round_=1.5)
    return p.finish()


def falconer():
    p = Pic(64, 64)
    cap = p.poly([(8, 58), (12, 30), (22, 12), (36, 8), (48, 16), (54, 36), (56, 58)])
    p.part(cap, "tan", round_=10, tex=0.1)
    p.part(p.ellipse(32, 40, 12, 15), SHADOW, flat=True)
    p.part(p.stroke(p.curve((14, 28), (32, 16), (50, 28)), 3), "gold", round_=1.5)
    for k, (c, w) in enumerate((("feather_brown", 6), ("feather_white", 5), ("feather_brown", 4))):
        p.part(p.stroke(p.curve((44, 18), (54 + k * 2, 4 - k * 2), (60, -2 + k * 4)), w, 1.5), c, round_=2, tex=0.12)
    return p.finish()


def unblinking():
    p = Pic(64, 64)
    band = p.rect(6, 30, 58, 50, 2)
    for x in (10, 21, 32, 43, 54):
        band |= p.poly([(x - 5, 32), (x, 10 if x == 32 else 16), (x + 5, 32)])
    p.part(band, "gold", round_=4)
    p.part(p.ellipse(32, 40, 8, 6), "ivory", round_=3)
    p.part(p.ellipse(32, 40, 3.6, 4.6), "gem_teal", round_=2, shine=True)
    p.part(p.ellipse(32, 40, 1.4, 3), SHADOW, flat=True, edge=False)
    for x in (15, 49):
        p.gem(x, 40, 3, "gem_red")
    return p.finish()


# --- Boots: 64 x 64, a pair, the near one in front -----------------------------------------------------------------------
def boots(mat, kind="leather", trim=None, mark=None):
    p = Pic(64, 64)
    for ox, oy, bias in ((4, 0, -0.08), (24, 4, 0.04)):
        shaft = p.poly([(ox + 6, oy + 4), (ox + 22, oy + 4), (ox + 22, oy + 38), (ox + 36, oy + 46), (ox + 36, oy + 56),
                        (ox + 4, oy + 56), (ox + 4, oy + 40)])
        p.part(shaft, mat, round_=7, bias=bias, tex=0.07, seed=ox)
        p.part(p.rect(ox + 3, oy + 54, ox + 37, oy + 59, 1), "dark_leather" if mat != "black" else "iron", round_=2)
        if kind in ("leather", "strapped"):
            p.part(p.rect(ox + 4, oy + 2, ox + 24, oy + 9, 2), trim or mat, round_=3)
        if kind == "wrapped":
            for y in range(oy + 8, oy + 50, 6):
                p.part(p.stroke([(ox + 4, y + 2), (ox + 24, y - 1)], 2.2), mat, round_=1, bias=0.12)
        if kind == "strapped":
            for y in (oy + 18, oy + 30, oy + 42):
                p.part(p.stroke([(ox + 4, y), (ox + 23, y)], 3.4), "dark_leather", round_=1.5)
                p.part(p.rect(ox + 13, y - 2, ox + 16, y + 2), "iron", round_=1)
        if kind in ("sabatons", "spiked"):
            for y in range(oy + 10, oy + 40, 7):
                p.part(p.rect(ox + 5, y, ox + 22, y + 5, 1), mat, round_=3, shine=True)
            p.part(p.poly([(ox + 20, oy + 40), (ox + 36, oy + 46), (ox + 36, oy + 54), (ox + 20, oy + 54)]), mat, round_=4, shine=True)
        if kind == "spiked":
            for y in (oy + 14, oy + 28):
                p.part(p.poly([(ox + 22, y), (ox + 30, y + 2), (ox + 22, y + 5)]), "steel", round_=1)
            if trim:
                p.part(p.rect(ox + 4, oy + 2, ox + 24, oy + 7, 1), trim, round_=2)
        if mark:
            mark(p, ox, oy)
    return p.finish()


def m_windrunner(p, ox, oy):
    for k in range(2):
        p.part(p.stroke(p.curve((ox + 5, oy + 14 + k * 8), (ox - 2, oy + 8 + k * 8), (ox - 4, oy + 2 + k * 8)), 4, 1.2),
               "feather_white", round_=2)


def m_stalker(p, ox, oy):
    for y in (oy + 18, oy + 30, oy + 42):
        p.part(p.ellipse(ox + 9, y, 1.4, 1.4), "glow_teal", round_=1, edge=False)


# --- Gloves: 64 x 64, one hand, fingers up --------------------------------------------------------------------------------
def glove(mat, kind="gloves", trim=None, cuff=None, mark=None):
    p = Pic(64, 64)
    long_ = kind in ("bracers", "gauntlets")
    c = cuff or mat
    # The cuff.
    p.part(p.poly([(18, 40), (46, 40), (50, 62 if long_ else 56), (14, 62 if long_ else 56)]), c, round_=6, tex=0.06)
    # The thumb, then the palm, then four fingers.
    p.part(p.stroke([(19, 36), (12, 26), (8, 18)], 8, 6), mat, round_=3, bias=0.05)
    p.part(p.rect(17, 22, 47, 44, 5), mat, round_=8, tex=0.05)
    for k, (x, top) in enumerate(((21, 9), (28, 4), (35, 5), (42, 11))):
        f = p.stroke([(x, 26), (x, top)], 7, 6)
        p.part(f, mat, round_=3, bias=0.04 - 0.03 * k)
        if kind == "gauntlets":
            for y in range(top + 4, 26, 5):
                p.line([(x - 3, y), (x + 3, y)], mat, 0)
        if kind == "claws":
            p.part(p.poly([(x - 2, top + 1), (x + 1, top - 7), (x + 2, top + 1)]), "bone", round_=1)
    if kind == "wraps":
        for y in range(14, 58, 5):
            p.part(p.stroke([(16, y + 3), (48, y - 1)], 2.2) & p.alpha, mat, round_=1, bias=0.15)
    if kind == "gauntlets":
        p.part(p.rect(17, 30, 47, 38, 3), mat, round_=4, shine=True)
        for x in (22, 32, 42):
            p.rivet(x, 34, "iron")
    if kind == "bracers":
        for y in (46, 54):
            p.part(p.stroke([(15, y), (49, y)], 3.2), "dark_leather", round_=1.5)
            p.part(p.rect(38, y - 2, 41, y + 2), "gold", round_=1)
    if trim:
        p.part(p.stroke([(18, 41), (46, 41)], 3), trim, round_=1.5)
    if mark:
        mark(p)
    return p.finish()


def m_fletcher(p):
    p.part(p.stroke([(46, 62), (62, 30)], 2.4), "pale_wood", round_=1)
    p.part(p.poly([(58, 34), (63, 26), (64, 36), (60, 40)]), "feather_red", round_=2)


def m_bloodletter(p):
    for x in (24, 32, 40):
        p.part(p.ellipse(x, 50, 1.6, 2.4), "gem_red", round_=1, edge=False)


# --- Belts: 64 x 32 ----------------------------------------------------------------------------------------------------------
def belt(mat, kind="plain", buckle="iron", mark=None):
    p = Pic(64, 32)
    if kind == "rope":
        for k in range(3):
            p.part(p.stroke(p.curve((2, 12 + k * 2), (32, 18 + k * 2), (62, 12 + k * 2)), 3.2), "rope", round_=1.5, bias=0.06 * k, tex=0.15, seed=k)
        p.part(p.ellipse(32, 18, 6, 5), "rope", round_=3, tex=0.15)
        p.part(p.stroke([(30, 21), (26, 31)], 3, 2), "rope", round_=1)
        p.part(p.stroke([(34, 21), (38, 31)], 3, 2), "rope", round_=1)
        return p.finish()
    wide = kind == "war_girdle"
    t, b = (6, 24) if wide else (10, 20)
    strap = p.poly([(1, t), (63, t), (63, b), (1, b)])
    p.part(strap, mat, round_=4, tex=0.08)
    for x in range(4, 62, 3):
        p.dot(x, t + 1, mat, 3 if x % 2 else 2)
        p.dot(x, b - 1, mat, 0)
    if kind == "studded":
        for x in range(6, 60, 6):
            if not 24 <= x <= 40:
                p.rivet(x, (t + b) // 2, "silver")
    if kind == "tassets":
        for x in (4, 15, 41, 52):
            p.part(p.poly([(x, b - 1), (x + 10, b - 1), (x + 9, 31), (x + 1, 31)]), "steel", round_=3, shine=True)
    if kind == "pouch":
        p.part(p.rect(42, 14, 56, 31, 3), "leather", round_=4, tex=0.1)
        p.part(p.rect(42, 14, 56, 20, 2), "dark_leather", round_=2)
        p.rivet(49, 19, "iron")
    # The buckle.
    p.part(p.rect(24, t - 3, 40, b + 3, 2), buckle, round_=3)
    p.part(p.rect(28, t + 1, 36, b - 1), mat, round_=2)
    p.part(p.rect(31, t, 33, (t + b) // 2 + 1), buckle, round_=1)
    if wide:
        p.part(p.ellipse(32, 15, 6, 6), "bone", round_=3)
        for ex in (30, 34):
            p.part(p.rect(ex - 1, 13, ex, 15), SHADOW, flat=True)
    if mark:
        mark(p)
    return p.finish()


def m_bandolier(p):
    for x in range(4, 62, 6):
        if 22 <= x <= 42:
            continue
        p.part(p.rect(x, 1, x + 3, 11, 1), "steel", round_=1.5, shine=True)


# --- Jewellery: 32 x 32 ------------------------------------------------------------------------------------------------------
def ring(mat, stone=None, signet=False):
    p = Pic(32, 32)
    band = p.ellipse(16, 19, 12, 10) & ~p.ellipse(16, 20, 7.5, 5.6)
    p.part(band, mat, round_=3.5)
    if signet:
        p.part(p.rect(9, 3, 23, 14, 3), "gold", round_=4)
        p.part(p.poly([(12, 11), (16, 5), (20, 11), (16, 9)]), "gold", round_=1, bias=-0.2)
    elif stone:
        p.part(p.rect(11, 5, 21, 13, 3), mat, round_=2)
        p.gem(16, 8, 4.6, stone)
    return p.finish()


def amulet(mat, kind="disc", stone=None):
    p = Pic(32, 32)
    chain = "rope" if kind == "tooth" else mat
    for side in (-1, 1):
        p.part(p.stroke(p.curve((16 + side * 14, 1), (16 + side * 13, 10), (16 + side * 2, 15)), 1.8), chain, round_=1, flat=False)
    if kind == "tooth":
        p.part(p.poly([(12, 14), (20, 14), (18, 22), (16, 31), (14, 22)]), "ivory", round_=3)
        p.part(p.rect(12, 13, 20, 16, 1), "leather", round_=1.5)
    else:
        p.part(p.ellipse(16, 22, 8.5, 8.5), mat, round_=4)
        p.part(p.ellipse(16, 22, 6, 6) & ~p.ellipse(16, 22, 4.6, 4.6), mat, round_=1, bias=-0.25)
        if stone:
            p.gem(16, 22, 4.2, stone)
        p.part(p.ellipse(16, 13.5, 2.2, 2), mat, round_=1)
    return p.finish()


def storm_eye():
    p = Pic(32, 32)
    for side in (-1, 1):
        p.part(p.stroke(p.curve((16 + side * 14, 1), (16 + side * 13, 10), (16 + side * 2, 15)), 1.8), "silver", round_=1)
    p.part(p.ellipse(16, 22, 9, 9), "silver", round_=4)
    p.gem(16, 22, 5.4, "ice")
    p.part(p.ellipse(16, 22, 1.4, 3.4), SHADOW, flat=True, edge=False)
    p.part(p.stroke([(26, 12), (22, 19), (28, 20), (24, 28)], 1.8), "gold", round_=1, edge=False)
    return p.finish()


def blueprint():
    p = Pic(32, 32)
    p.part(p.rect(3, 6, 29, 28, 2), "parch", round_=3, tex=0.08)
    for x in (3, 29):
        p.part(p.ellipse(x, 17, 3, 12), "parch", round_=3, bias=-0.1)
    for y in (12, 16, 20, 24):
        p.line([(9, y), (23, y)], "blue", 2)
    p.line([(12, 11), (14, 24)], "blue", 0)
    return p.finish()


# --- Trinkets (found, Mythic) and the upgrade materials: 32 x 32 ----------------------------------------------------------
def t_wolf_tooth():
    p = Pic(32, 32)
    p.part(p.ellipse(16, 5, 7, 3.6), "grey_fur", round_=2, tex=0.2)
    p.part(p.poly([(10, 8), (22, 8), (21, 17), (16, 31), (11, 17)]), "ivory", round_=4)
    p.part(p.rect(9, 7, 23, 12, 2), "leather", round_=2)
    p.part(p.stroke(p.curve((23, 9), (29, 12), (28, 20)), 2.4, 1.4), "leather", round_=1)
    return p.finish()


def t_ember_charm():
    p = Pic(32, 32)
    p.part(p.ellipse(16, 4, 3.6, 3.6) & ~p.ellipse(16, 4, 1.6, 1.6), "iron", round_=1.5)
    p.part(p.ellipse(16, 19, 8, 9), "glow_ember", round_=5, edge=False)
    p.part(p.ellipse(15, 18, 4, 5), "fire", round_=3, edge=False)
    cage = p.rect(7, 8, 25, 10) | p.rect(7, 28, 25, 30)
    for x in (7, 13, 19, 25):
        cage |= p.rect(x - 1, 8, x, 30)
    p.part(cage, "iron", round_=1, shine=True)
    return p.finish()


def t_frost_bead():
    p = Pic(32, 32)
    p.part(p.stroke([(1, 8), (31, 26)], 1.8), "rope", round_=1)
    for x, y, r in ((7, 12, 4), (16, 17, 5.4), (25, 22, 4)):
        p.gem(x, y, r, "ice")
    return p.finish()


def t_hare_foot():
    p = Pic(32, 32)
    p.part(p.ellipse(16, 4, 3.4, 3.4) & ~p.ellipse(16, 4, 1.5, 1.5), "gold", round_=1.5)
    p.part(p.rect(11, 7, 21, 12, 2), "gold", round_=2)
    p.part(p.ellipse(16, 20, 7, 10), "fur", round_=5, tex=0.18)
    for x in (12, 16, 20):
        p.part(p.ellipse(x, 29, 2, 2.4), "fur", round_=1.5, bias=-0.15)
    return p.finish()


def t_hawk_feather():
    p = Pic(32, 32)
    vane = p.stroke(p.curve((6, 28), (14, 10), (28, 2)), 10, 3)
    p.part(vane, "feather_brown", round_=4, tex=0.15)
    for k in range(5):
        p.line([(10 + k * 3, 22 - k * 4), (16 + k * 3, 24 - k * 4)], "feather_brown", 0)
    p.part(p.stroke(p.curve((4, 31), (14, 12), (28, 2)), 1.6), "ivory", round_=1, edge=False)
    p.part(p.rect(2, 26, 8, 31, 1), "gold", round_=1.5)
    return p.finish()


def t_blood_knot():
    p = Pic(32, 32)
    p.part(p.ellipse(16, 12, 10, 9) & ~p.ellipse(16, 12, 5, 4.4), "red", round_=3, tex=0.15)
    p.gem(16, 12, 4.4, "gem_red")
    for side in (-1, 1):
        p.part(p.stroke([(16 + side * 6, 19), (16 + side * 10, 29)], 2.4, 1.4), "red", round_=1)
    p.part(p.poly([(16, 21), (19.5, 26), (16, 31), (12.5, 26)]), "gem_red", round_=2, shine=True)
    return p.finish()


def t_huntsmans_knot():
    p = Pic(32, 32)
    p.part(p.ellipse(14, 11, 12, 8.5) & ~p.ellipse(14, 11, 7, 4), "green", round_=3, tex=0.2)
    p.part(p.ellipse(20, 19, 4, 3.4), "green", round_=2)
    p.part(p.poly([(21, 20), (31, 24), (27, 31), (20, 26)]), "flint", round_=2)
    return p.finish()


def t_thorn_sigil():
    p = Pic(32, 32)
    p.part(p.ellipse(16, 16, 11, 11), "iron", round_=5, shine=True)
    for k in range(10):
        a = k / 10 * math.tau
        x, y = 16 + 13 * math.cos(a), 16 + 13 * math.sin(a)
        p.part(p.poly([(16 + 10 * math.cos(a - 0.2), 16 + 10 * math.sin(a - 0.2)), (x, y), (16 + 10 * math.cos(a + 0.2), 16 + 10 * math.sin(a + 0.2))]),
               "darkwood", round_=1)
    p.part(p.ellipse(16, 16, 9, 9) & ~p.ellipse(16, 16, 7.5, 7.5), "darkwood", round_=1, tex=0.3)
    p.gem(16, 16, 4.4, "gem_green")
    return p.finish()


def mat_iron():
    p = Pic(32, 32)
    p.part(p.poly([(3, 20), (17, 14), (29, 18), (15, 25)]), "iron", round_=2, shine=True)
    p.part(p.poly([(3, 20), (15, 25), (15, 30), (3, 25)]), "iron", flat=True, bias=-0.2)
    p.part(p.poly([(15, 25), (29, 18), (29, 23), (15, 30)]), "iron", flat=True, bias=-0.4)
    p.part(p.poly([(8, 12), (17, 5), (25, 9), (17, 16)]), "steel", round_=2, shine=True)
    p.part(p.stroke([(18, 10), (26, 4)], 2.4), "rope", round_=1)
    return p.finish()


def mat_heartwood():
    p = Pic(32, 32)
    for k, y in enumerate((24, 17, 10)):
        p.part(p.stroke([(3 + k * 2, y + 4), (25 + k * 2, y - 3)], 6), "redwood", round_=3, tex=0.15, seed=k)
        p.part(p.ellipse(26 + k * 2, y - 3, 3, 3.2), "pale_wood", round_=2)
        p.dot(26 + k * 2, y - 3, "redwood", 0)
    p.part(p.stroke([(10, 2), (14, 30)], 1.8), "red", round_=1)
    return p.finish()


def mat_gems():
    p = Pic(32, 32)
    p.part(p.poly([(3, 26), (8, 16), (14, 27)]), "gem_red", round_=3, shine=True)
    p.part(p.poly([(12, 28), (17, 12), (26, 18), (23, 29)]), "gem_teal", round_=3, shine=True)
    p.part(p.poly([(17, 12), (22, 3), (29, 9), (26, 18)]), "gem_violet", round_=3, shine=True)
    for x, y in ((7, 19), (17, 16), (23, 7)):
        p.dot(x, y, WHITE, 0)
    return p.finish()


ITEMS = {
    # Bows, short and long.
    "hunting_bow": lambda: bow("wood", 0.72, 15, 6, "leather", "bone"),
    "recurve_bow": lambda: bow("redwood", 0.74, 14, 6, "red_leather", "bone", recurve=8),
    "horn_bow": lambda: bow("bone", 0.72, 15, 7, "black", "black", recurve=6, bands=((0.35, "black"), (0.7, "black"))),
    "yew_longbow": lambda: bow("wood", 0.97, 15, 7, "leather", "bone"),
    "war_bow": lambda: bow("darkwood", 0.97, 18, 8, "black", "iron", bands=((0.3, "iron"), (0.75, "iron"))),
    "great_bow": lambda: bow("redwood", 0.97, 20, 9, "gold", "steel", recurve=8, bands=((0.3, "gold"), (0.7, "gold"))),
    "splinterbough": lambda: bow("wood", 0.94, 16, 7, "green", "green", mark=m_splinter),
    "ember_tongue": lambda: bow("black", 0.76, 16, 7, "ember", "ember", string=fixed("8a4a10", "c06a20", "ffb040", "ffd060", "fff0a0"), mark=m_ember),
    "hunters_promise": lambda: bow("tan", 0.76, 16, 7, "gold", "gold", recurve=6, mark=m_promise),
    "galeheart": lambda: bow("ivory", 0.9, 16, 7, "teal", "teal", recurve=8, string=fixed("2a8a80", "40b0a4", "70d8cc", "a8f4ea", "e0fffa"), mark=m_gale),
    "widows_draw": lambda: bow("black", 0.8, 17, 7, "red", "red", recurve=9, mark=m_widow),
    "gallowsreach": lambda: bow("darkwood", 0.97, 16, 7, "rope", "iron", mark=m_gallows),
    "stillwater_yew": lambda: bow("ice", 0.97, 15, 7, "silver", "silver", string=fixed("8ab0c8", "aac8dc", "c8e0ee", "e8f8ff", "ffffff"), mark=m_still),
    "the_long_silence": lambda: bow("black", 0.97, 20, 9, "violet", "violet", recurve=6, string=fixed("6a4a9a", "8a6ac0", "a88ae0", "c8a0f4", "eee0ff"), mark=m_silence),
    # Quivers.
    "hide_quiver": lambda: quiver("tan", "leather"),
    "studded_quiver": lambda: quiver("red_leather", "dark_leather", studs="silver"),
    "bone_quiver": lambda: quiver("bone", "black", "feather_white", ribs=True),
    "quiver_of_endless_splinters": lambda: quiver("wood", "green", "feather_green", arrows=0, mark=q_splinters),
    "ashfall_quiver": lambda: quiver("black", "iron", "glow_ember", mark=q_ashfall),
    "wind_sworn_quiver": lambda: quiver("teal", "silver", "feather_white", mark=q_wind),
    "magpies_nest": lambda: quiver("rope", "black", "feather_black", mark=q_magpie),
    "quiver_of_the_hollow_hound": lambda: quiver("grey_fur", "black", "feather_red", mark=q_hound),
    # Chest pieces by tier, then the legendaries.
    "ranger": lambda: chest("leather", "dark_leather", kind="coat", mark=p_ranger),
    "brigand": lambda: chest("red_leather", "dark_leather", sleeves="leather", shoulders="leather", kind="studded"),
    "scale": lambda: chest("bronze", "dark_leather", sleeves="leather", shoulders="bronze", kind="scales"),
    "plate": lambda: chest("steel", "dark_leather", sleeves="iron", shoulders="steel", kind="plate", accent="red"),
    "knight": lambda: chest("bright", "gold", sleeves="steel", shoulders="gold", collar="gold", kind="plate", mark=p_knight),
    "stag_hide": lambda: chest("tan", "leather", collar="fur", kind="coat", mark=p_stag),
    "cinder_jerkin": lambda: chest("black", "black", collar="ember", kind="coat", mark=p_cinder),
    # Helms.
    "hood": lambda: helm("hood", "cloth"),
    "mask": lambda: helm("mask", "red_leather", "gold"),
    "barbute": lambda: helm("barbute", "steel"),
    "visored": lambda: helm("visored", "steel", "red"),
    "horned": lambda: helm("horned", "bright", "gold"),
    "falconer": falconer,
    "unblinking_crown": unblinking,
    # Boots.
    "wrapped": lambda: boots("cloth", "wrapped"),
    "leather_boots": lambda: boots("leather", "leather", "tan"),
    "strapped": lambda: boots("red_leather", "strapped"),
    "sabatons": lambda: boots("steel", "sabatons"),
    "spiked": lambda: boots("black", "spiked", "red"),
    "windrunner": lambda: boots("teal", "leather", "silver", mark=m_windrunner),
    "stalker": lambda: boots("black", "strapped", mark=m_stalker),
    # Gloves.
    "wraps": lambda: glove("cloth", "wraps"),
    "gloves": lambda: glove("leather", "gloves", "tan"),
    "bracers": lambda: glove("red_leather", "bracers", None, "leather"),
    "gauntlets": lambda: glove("steel", "gauntlets"),
    "claws": lambda: glove("black", "claws", "red"),
    "fletcher": lambda: glove("green", "gloves", "gold", "leather", mark=m_fletcher),
    "bloodletter": lambda: glove("red", "claws", None, "black", mark=m_bloodletter),
    # Belts.
    "rope": lambda: belt("rope", "rope"),
    "pouch_belt": lambda: belt("leather", "pouch", "iron"),
    "studded": lambda: belt("red_leather", "studded", "silver"),
    "tassets": lambda: belt("leather", "tassets", "steel"),
    "war_girdle": lambda: belt("black", "war_girdle", "gold"),
    "bandolier": lambda: belt("red_leather", "plain", "gold", mark=m_bandolier),
    # Rings, amulets, the blueprint.
    "iron_ring": lambda: ring("iron"),
    "copper_ring": lambda: ring("copper"),
    "silver_ring": lambda: ring("silver", "gem_teal"),
    "gold_ring": lambda: ring("gold", "gem_red"),
    "jewelled_ring": lambda: ring("gold", "gem_violet"),
    "pack_leaders_signet": lambda: ring("gold", signet=True),
    "bone_charm": lambda: amulet("bone", "tooth"),
    "copper_amulet": lambda: amulet("copper"),
    "silver_amulet": lambda: amulet("silver", stone="gem_teal"),
    "gold_amulet": lambda: amulet("gold", stone="gem_red"),
    "jewelled_amulet": lambda: amulet("gold", stone="gem_violet"),
    "eye_of_the_storm": storm_eye,
    "blueprint": blueprint,
    # Trinkets (found, 2026-11-26) and the upgrade materials.
    "trinket_wolf_tooth": t_wolf_tooth,
    "trinket_ember_charm": t_ember_charm,
    "trinket_frost_bead": t_frost_bead,
    "trinket_hare_foot": t_hare_foot,
    "trinket_hawk_feather": t_hawk_feather,
    "trinket_blood_knot": t_blood_knot,
    "trinket_huntsmans_knot": t_huntsmans_knot,
    "trinket_thorn_sigil": t_thorn_sigil,
    "material_iron": mat_iron,
    "material_heartwood": mat_heartwood,
    "material_gems": mat_gems,
}


def sheet(names, scale=3):
    """Every picture on a dark green, in rows, each in a box of its slot's size."""
    tiles = [(n, ITEMS[n]().rgba()) for n in names]
    bg = np.array([22, 30, 22, 255], np.uint8)
    cell = 68
    per = 10
    rows = []
    for i in range(0, len(tiles), per):
        chunk = tiles[i:i + per]
        hgt = max(t.shape[0] for _, t in chunk) + 4
        row = np.tile(bg, (hgt, cell * per, 1))
        for j, (_, t) in enumerate(chunk):
            h, w = t.shape[:2]
            oy, ox = (hgt - h) // 2, j * cell + (cell - w) // 2
            region = row[oy:oy + h, ox:ox + w]
            region[:] = np.where(t[..., 3:4] > 0, t, region)
        rows.append(row)
    out = np.concatenate(rows, 0)
    return np.repeat(np.repeat(out, scale, 0), scale, 1)


def main():
    args = sys.argv[1:]
    if args and args[0] == "preview":
        names = args[1:] or list(ITEMS)
        write_png(os.environ.get("ITEM_PREVIEW", "/tmp/items.png"), sheet(names))
        return
    os.makedirs(OUT, exist_ok=True)
    for name, make in ITEMS.items():
        write_png(os.path.join(OUT, name + ".png"), make().rgba())
    write_png(os.path.join(P, "ArtSource", "pixel", "previews", "item_art_32.png"), sheet(list(ITEMS), 2))
    print("wrote", len(ITEMS))


if __name__ == "__main__":
    main()
