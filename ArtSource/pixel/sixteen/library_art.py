# The Burnt Library's art in the 16x16 style (2026-11-16, the owner: "start building the third level, the one after the
# gaol. build enemies, assets, etc."): the walls (shelves of books, burnt-out shelves, a fallen shelf, scorched stone), the
# cut corners, the free-standing bookcases and pillars, the stair doorway, the props (lecterns, reading desks, book piles, a
# fallen shelf, burning books, ash, an urn), ash on the floor and the scorched flagstones. Built on gaol_art.py's blocks
# (its helpers are imported; the palette is replaced for the library): light from the top left, dark outlines, each drawn
# pixel 2 game pixels, 27 pixels a unit.
#
# Writes into Assets/_Project/Resources/Art (World, Dungeon, Ground), the list ArtSource/pixel/sixteen/library_art_sizes.txt
# (as gaol_art_sizes.txt; Tools > ARPG > Configure Gaol Art reads both) and a preview, previews/library_art_16.png.
# Run: python3 ArtSource/pixel/sixteen/library_art.py (plain Python, no Blender needed)
import os, sys, random, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gaol_art as g
from gaol_art import Img, write_png, hash01, block, flat_block, cap_bottom, face_uv, CW, CH, WALL_H, LOW_H, SCALE, K, ART, P, HERE

# The library's palette over the gaol's: warm scorched stone, charred wood, leather and paper.
g.PAL["stone"] = ["1e1a1c", "322c2e", "4a4244", "645a5c", "826f6e"]
g.PAL["top"] = ["171315", "251f21", "342b2c"]
g.PAL["floor"] = ["17141a", "231e22", "2e282c", "3a3236"]
g.PAL["wood"] = ["2a1a12", "4a2e1e", "6e4a2c", "92683c"]
PAL = g.PAL
PAGE = ["a89a78", "d8cca8", "f0e8d0"]
SOOT = ["0e0c0e", "1a1618", "2a2426"]
EMBER = ["8a2a14", "d8541c", "ff8a2a", "ffd04a"]
BOOKS = ["8a2e28", "b2602c", "46683e", "3e5088", "d0b078", "6a4226", "7a3a5a"]
ASH = ["3a3638", "58545a", "7a747a", "a09aa0"]


def shade(col, face):
    r, gg, b = int(col[0:2], 16), int(col[2:4], 16), int(col[4:6], 16)
    k = 1.0 if face == 0 else 0.72
    return "%02x%02x%02x" % (int(r * k), int(gg * k), int(b * k))


def shelf_feature(mode, seed):
    """Books on boards across a face. mode: 0 full, 1 burnt out, 2 a fallen shelf, 3 bare scorched stone."""
    def f(face, u, v):
        if mode == 3:
            if v < 5 and hash01(seed, u, v, 1) < 0.55:
                return shade(SOOT[1], face)
            if hash01(seed, u, v // 3, 2) > 0.93 and v > 6:
                return shade(EMBER[0], face)
            return None
        if u == 0 or v < 3 or v >= WALL_H - 2:
            return None
        row, inrow = (v - 3) // 6, (v - 3) % 6
        if inrow == 5:
            return shade(PAL["wood"][2 if face == 0 else 1], face)
        if mode == 2 and row >= 1 and abs((u - 1) - (v - 9) * 5 / 12) < 1.1:
            return shade(PAL["wood"][3], face)             # a board hanging down across the shelves
        h = hash01(seed, row, u, face * 7)
        if mode == 1:
            if h < 0.45:
                return shade(PAL["wood"][0], face)         # an empty, charred shelf
            col = SOOT[int(h * 7) % 3]
            if h > 0.93:
                col = EMBER[1 + int(h * 100) % 2]
            return shade(col, face)
        if inrow == 0 and h < 0.25:
            return shade(PAL["wood"][0], face)             # a gap above a short book
        col = BOOKS[int(h * 100) % len(BOOKS)]
        if inrow == 1 and h > 0.8:
            col = PAGE[1]                                  # a pale spine
        if h > 0.97:
            col = EMBER[0]
        return shade(col, face)
    return f


def shelf_wall(seed, mode, height=WALL_H, half=None):
    return block(seed, height=height, half=half, feature=shelf_feature(mode, seed))


WALLS = {
    "library_wall_1": lambda: shelf_wall(31, 0),
    "library_wall_2": lambda: shelf_wall(32, 1),
    "library_wall_3": lambda: shelf_wall(33, 2),
    "library_wall_4": lambda: shelf_wall(34, 3),
    "library_wall_low_1": lambda: block(35, LOW_H),
    "library_wall_low_2": lambda: block(36, LOW_H),
    "library_diag_b": lambda: flat_block(37),
    "library_diag_f": lambda: block(38, half="f"),
    "library_diag_l": lambda: block(39, half="l"),
    "library_diag_r": lambda: block(40, half="r"),
    "library_diag_b_low": lambda: flat_block(41, LOW_H),
    "library_diag_f_low": lambda: block(42, LOW_H, half="f"),
    "library_diag_l_low": lambda: block(43, LOW_H, half="l"),
    "library_diag_r_low": lambda: block(44, LOW_H, half="r"),
}


# Free-standing cases: a bookcase run, books on its long face and a plain wood end on the other; and a stone pillar -------
SHELF_H = 22


def bookcase(along_x, seed):
    books = shelf_feature(0, seed)

    def feature(face, u, v):
        long_face = 1 if along_x else 0
        if face == long_face:
            return books(face, u, v)
        if u == 0 or v >= SHELF_H - 1:
            return None
        return shade(PAL["wood"][1 + (v % 6 == 2)], face)
    return block(seed, height=SHELF_H, feature=feature)


def pillar():
    im = block(51)
    s = PAL["stone"]
    for y in range(im.h):
        for x in range(im.w):
            if x < 3 or x > CW - 4:
                im.c[y][x] = None
    # A lit left edge and a dark capital and base.
    for y in range(im.h):
        for x in range(im.w):
            if im.c[y][x] is not None and y > 3 and (y in (5, 6, im.h - 3, im.h - 4)):
                im.c[y][x] = s[1]
    return im


SHELVES = {"library_shelf_x_1": lambda: bookcase(True, 61), "library_shelf_x_2": lambda: bookcase(True, 62),
           "library_shelf_y_1": lambda: bookcase(False, 63), "library_shelf_y_2": lambda: bookcase(False, 64),
           "library_pillar": pillar}


# The stair doorway: gaol_art's arch in the library's stone, with a candle either side --------------------------------------
STAIRS = {"stairs_down_x_library": lambda: g.doorway(True, False), "stairs_up_x_library": lambda: g.doorway(False, False),
          "stairs_down_y_library": lambda: g.doorway(True, True), "stairs_up_y_library": lambda: g.doorway(False, True)}


# Props ---------------------------------------------------------------------------------------------------------------------
def book(im, x, y, w, col, lean=0):
    im.rect(x, y, x + w - 1, y + 1, col)
    im.set(x, y, shade(col, 1))
    im.rect(x + 1, y - 1 + lean, x + w - 2, y - 1 + lean, PAGE[1]) if w > 3 else None


def lectern():
    im = Img(13, 22)
    w = PAL["wood"]
    im.rect(5, 8, 7, 18, w[1]); im.rect(5, 8, 5, 18, w[2])        # the stem
    im.rect(2, 19, 10, 20, w[1]); im.rect(2, 19, 10, 19, w[2])     # the foot
    im.rect(1, 4, 11, 8, w[2]); im.rect(1, 4, 11, 4, w[3])         # the slanted top
    im.rect(2, 3, 6, 6, PAGE[1]); im.rect(7, 3, 11, 6, PAGE[0])    # an open book
    im.line(6, 3, 6, 7, K)
    for y in (4, 6):
        im.line(3, y, 5, y, PAGE[0]); im.line(8, y, 10, y, PAGE[1])
    im.set(9, 4, EMBER[0]); im.set(10, 5, SOOT[1])                 # scorched
    return im.outline()


def reading_desk():
    im = Img(22, 17)
    w = PAL["wood"]
    im.rect(2, 6, 19, 9, w[2]); im.rect(2, 6, 19, 6, w[3]); im.rect(2, 10, 19, 10, w[1])
    for x in (3, 17):
        im.rect(x, 11, x + 1, 15, w[1])
    for x0, c0 in ((4, PAGE[1]), (9, PAGE[2]), (14, PAGE[0])):
        im.rect(x0, 4, x0 + 3, 6, c0); im.set(x0 + 1, 5, PAGE[0]); im.set(x0 + 3, 4, EMBER[0])
    b = PAL["wood"]
    im.rect(16, 2, 16, 5, PAGE[2]); im.set(16, 1, EMBER[2]); im.set(16, 0, EMBER[1])   # a candle
    return im.outline()


def book_pile():
    im = Img(15, 10)
    rows = [(1, 7, 11, BOOKS[0]), (2, 5, 9, BOOKS[3]), (3, 3, 7, BOOKS[1])]
    for x, y, w, col in rows:
        im.rect(x, y, x + w - 1, y + 2, col); im.rect(x, y, x + w - 1, y, shade(col, 0))
        im.rect(x + 1, y + 1, x + w - 2, y + 1, PAGE[1])
    im.rect(8, 1, 12, 3, BOOKS[4]); im.rect(8, 1, 12, 1, PAGE[2])
    im.set(5, 8, EMBER[0]); im.set(11, 7, SOOT[0])
    return im.outline()


def fallen_shelf():
    im = Img(26, 13)
    w = PAL["wood"]
    im.line(1, 10, 23, 3, w[2]); im.line(1, 11, 23, 4, w[1]); im.line(1, 9, 23, 2, w[3])
    for x in (4, 10, 17):
        im.line(x, 10 - (x * 7) // 22, x + 1, 5 - (x * 7) // 22, w[1])
    for x, y, col in ((6, 8, BOOKS[0]), (11, 6, BOOKS[3]), (3, 11, BOOKS[4]), (15, 8, BOOKS[1]), (20, 5, BOOKS[2])):
        im.rect(x, y, x + 2, y + 2, col); im.set(x + 1, y + 1, PAGE[1])
    im.set(9, 10, EMBER[1])
    return im.outline()


def burning_books():
    im = Img(15, 20)
    f = EMBER
    im.blob(7.5, 15, 6.5, 3.6, [SOOT[0], PAL["wood"][1], BOOKS[0], BOOKS[1]])
    for x, y, col in ((4, 14, BOOKS[3]), (9, 13, BOOKS[1]), (6, 16, PAGE[0])):
        im.rect(x, y, x + 3, y + 1, col)
    im.blob(7.5, 8.5, 4.4, 6.2, [f[0], f[1], f[2], f[3]], light=(0.0, 0.9))
    im.set(7, 1, f[2]); im.set(6, 2, f[1]); im.set(8, 3, f[2])
    return im.outline()


def ash_heap():
    im = Img(17, 9)
    im.blob(8.5, 5.8, 8, 3.2, ASH)
    rng = random.Random(7)
    for _ in range(9):
        x, y = rng.randint(3, 13), rng.randint(3, 7)
        if im.get(x, y) is not None:
            im.set(x, y, ASH[rng.choice((0, 3))])
    im.set(6, 4, EMBER[1]); im.set(11, 5, EMBER[0]); im.set(9, 3, EMBER[2])
    return im.outline()


def urn():
    im = Img(10, 15)
    c = ["4a2e22", "7a4a34", "a8683e", "c88a58"]
    im.blob(5, 9.5, 3.8, 4.6, c)
    im.rect(3, 3, 6, 4, c[1]); im.rect(2, 2, 7, 2, c[2]); im.rect(3, 12, 6, 13, c[0])
    im.line(2, 8, 7, 8, c[0]); im.set(4, 6, c[3])
    return im.outline()


PROPS = {"prop_lectern": lectern, "prop_reading_desk": reading_desk, "prop_book_pile": book_pile,
         "prop_fallen_shelf": fallen_shelf, "prop_burning_books": burning_books, "prop_ash_heap": ash_heap, "prop_urn": urn}


def ash_decal(seed):
    rng = random.Random(seed)
    im = Img(27, 14)

    def inside(x, y):
        return abs(x + 0.5 - 13.5) / 13.5 + abs(y + 0.5 - 7) / 7 < 0.8
    for _ in range(40):
        x, y = rng.randint(2, 24), rng.randint(2, 11)
        if inside(x, y):
            c0 = ASH[rng.choice((0, 0, 1, 1, 2))]
            im.set(x, y, c0)
            if rng.random() < 0.5:
                im.set(x + 1, y, c0)
    for _ in range(3):
        x, y = rng.randint(4, 22), rng.randint(3, 10)
        if inside(x, y):
            im.set(x, y, EMBER[rng.choice((0, 1))])
    return im


DECALS = {"decal_ash_1": lambda: ash_decal(1), "decal_ash_2": lambda: ash_decal(2)}


def floor(size=108):
    f = PAL["floor"]
    pitch = 13.5
    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            a, b = (x + 2 * y) / pitch, (x - 2 * y) / pitch
            i, j = math.floor(a), math.floor(b)
            fa, fb = a - i, b - j
            p, q = (i + j) % 16, (i - j) % 32
            tone = hash01(p, q, 11)
            joint = fa < 0.12 or fb < 0.12
            if joint:
                col = f[0]
            else:
                k = 1 if tone < 0.35 else 3 if tone > 0.85 else 2
                if fa < 0.22 or fb > 0.92:
                    k = min(3, k + 1)
                speck = hash01(x, y, 17)
                if speck < 0.05:
                    k = max(1, k - 1)
                col = f[k]
                # Soot and ash dust.
                if speck > 0.985:
                    col = ASH[1]
                # An ember crack across a few slabs.
                if tone > 0.965 and abs(fa - fb) < 0.045 and 0.2 < fa < 0.85:
                    col = EMBER[1] if hash01(x, y, 5) > 0.35 else EMBER[0]
            row.append((int(col[0:2], 16), int(col[2:4], 16), int(col[4:6], 16), 255))
        rows.append(row)
    return rows


def main():
    written, preview = [], []
    for group, folder in ((WALLS, "World"), (SHELVES, "World"), (STAIRS, "World"), (PROPS, "Dungeon"), (DECALS, "Dungeon")):
        for name, make in group.items():
            im = make()
            write_png(os.path.join(ART, folder, name + ".png"), im.rows())
            px, py = g.pivot_of(name, im)
            written.append(f"{folder}/{name} {im.w * SCALE} {im.h * SCALE} {px:g} {py:g}")
            preview.append(im)
    write_png(os.path.join(ART, "Ground", "library_floor.png"), floor())
    with open(os.path.join(HERE, "library_art_sizes.txt"), "w") as fh:
        fh.write("\n".join(written) + "\n")

    # A corner of a hall to look at: the floor, two walls, a bookcase run, props.
    w, h = 150, 125
    sc = Img(w, h)
    fl = floor()
    for y in range(h):
        for x in range(w):
            r, gg, b, _ = fl[y % 108][x % 108]
            sc.set(x, y, f"{r:02x}{gg:02x}{b:02x}")

    def at(cx, cy):
        return round(75 + (cx - cy) * 6.75), round(112 - (cx + cy) * 3.375)
    names = ["library_wall_1", "library_wall_1", "library_wall_2", "library_wall_1", "library_wall_3", "library_wall_1", "library_wall_4",
             "library_wall_1", "library_wall_2"]
    cells = []
    for k in range(0, 9):
        cells.append((k, 9, "x"))
        cells.append((9, k, "y"))
    cells.append((9, 9, "c"))
    for cx, cy, kind in sorted(cells, key=lambda c: -(c[0] + c[1])):
        ox, oy = at(cx, cy)
        name = names[(cx if kind == "x" else cy) % len(names)] if kind != "c" else "library_wall_4"
        sc.paste(WALLS[name](), ox - 7, oy - WALL_H - 3)
    for name, cx, cy in (("library_shelf_x_1", 3, 5), ("library_shelf_x_2", 4, 5), ("library_shelf_x_1", 5, 5), ("library_pillar", 7, 3)):
        ox, oy = at(cx, cy)
        p = SHELVES[name]()
        sc.paste(p, ox - 7, oy - p.h + 4)
    for name, cx, cy in (("prop_reading_desk", 7, 7), ("prop_lectern", 3, 7), ("prop_book_pile", 5, 2), ("prop_burning_books", 7, 5),
                         ("prop_fallen_shelf", 2, 3), ("prop_ash_heap", 5, 7), ("prop_urn", 8, 2), ("prop_candles", 3, 2)):
        ox, oy = at(cx, cy)
        mk = PROPS.get(name) or g.PROPS.get(name)
        p = mk()
        sc.paste(p, ox - p.w // 2, oy - p.h + 3)
    write_png(os.path.join(P, "ArtSource", "pixel", "previews", "library_scene_16.png"), sc.rows(6))
    rows, row, width = [], [], 0
    for im in preview:
        if width + im.w > 250:
            rows.append(row); row, width = [], 0
        row.append(im); width += im.w + 3
    rows.append(row)
    sheet_h = sum(max(i.h for i in r) + 3 for r in rows)
    sheet = Img(256, sheet_h)
    for yy in range(sheet_h):
        for xx in range(256):
            sheet.set(xx, yy, "2a2c38")
    y = 0
    for r in rows:
        hh = max(i.h for i in r)
        x = 2
        for im in r:
            sheet.paste(im, x, y + hh - im.h)
            x += im.w + 3
        y += hh + 3
    write_png(os.path.join(P, "ArtSource", "pixel", "previews", "library_art_16.png"), sheet.rows(4))
    print("wrote", len(written), "pieces and the floor")


if __name__ == "__main__":
    main()
