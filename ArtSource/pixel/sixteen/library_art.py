# The Burnt Library's art in the 16x16 style (2026-11-16, the owner: "start building the third level, the one after the
# gaol. build enemies, assets, etc."): the walls (shelves of books, burnt-out shelves, a fallen shelf, scorched stone), the
# cut corners, what hangs on the stone piers (a sconce, a portrait, a hanging), the free-standing bookcases and pillars, the stair doorway, the props (lecterns, reading desks, book piles, a
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

# The library's palette over the gaol's: warm sandstone lighter than the floor, charred wood, old leather and paper.
g.PAL["stone"] = ["221b1c", "4c3e3c", "685652", "867066", "a48c7c"]
g.PAL["top"] = ["191415", "292122", "3a3030"]
g.PAL["floor"] = ["1c161c", "2c2428", "382e32", "4c4042", "5a4c4c"]
g.PAL["wood"] = ["24160f", "46291a", "6c4628", "94683a"]
PAL = g.PAL
PAGE = ["a89a78", "d8cca8", "f0e8d0"]
SOOT = ["161214", "262022", "3c3436"]
EMBER = ["8a2a14", "d8541c", "ff8a2a", "ffd04a"]
# Old leather: oxblood, tan, green, navy, cream, brown, plum. Each as (spine, band).
BOOKS = [("7a2a26", "c88a3a"), ("9a5a2c", "e0c080"), ("3e5a3a", "c8b060"), ("34447a", "c8c8d8"), ("b89a68", "7a2a26"),
         ("5e3a24", "c88a3a"), ("6a3450", "d8b070")]
ASH = ["3a3638", "58545a", "7a747a", "a09aa0"]


def shade(col, face):
    r, gg, b = int(col[0:2], 16), int(col[2:4], 16), int(col[4:6], 16)
    k = 1.0 if face == 0 else 0.7
    return "%02x%02x%02x" % (int(r * k), int(gg * k), int(b * k))


# A wall of shelves: a stone cornice and plinth, a wooden case between them with four shelves, each of books two or one
# pixel thick, some shorter than the shelf, a band on the taller spines, a gap here and there ---------------------------
CASE_TOP, SHELF_ROWS, SHELVES_N = 3, 5, 4
SPLITS = [(2, 1, 2, 1), (1, 2, 1, 2), (2, 2, 1, 1), (1, 1, 2, 2), (2, 1, 1, 2), (1, 2, 2, 1)]


def book_at(seed, shelf, face, u):
    """The book a column of a shelf belongs to: (index, first column of it)."""
    split = SPLITS[int(hash01(seed, shelf, face, 3) * len(SPLITS)) % len(SPLITS)]
    x = 1
    for k, w in enumerate(split):
        if u < x + w:
            return k, u == x
        x += w
    return len(split) - 1, False


def shelf_feature(mode, seed, rows=SHELVES_N, plinth=True):
    """mode: 0 full shelves, 1 burnt out, 2 ransacked, 3 bare scorched stone, 4 a rack of scrolls."""
    wood = PAL["wood"]
    case_bottom = CASE_TOP + rows * SHELF_ROWS

    def f(face, u, v):
        if mode == 3:
            if v < 6 and hash01(seed, u, v, 1) < 0.5 - v * 0.07:
                return shade(SOOT[1], face)
            return None
        if v < CASE_TOP - 1 or (plinth and v >= case_bottom):
            # Soot licking up the cornice over a burnt case.
            if mode == 1 and v < CASE_TOP - 1 and hash01(seed, u, v, 9) < 0.6:
                return shade(SOOT[1], face)
            return None
        charred = mode == 1
        if v == CASE_TOP - 1:
            return shade(SOOT[2] if charred else wood[3], face)                 # the case's top board, lit
        if u == 0:
            return shade(SOOT[1] if charred else wood[1], face)                 # the upright between two cases
        shelf, inrow = (v - CASE_TOP) // SHELF_ROWS, (v - CASE_TOP) % SHELF_ROWS
        if inrow == SHELF_ROWS - 1:
            return shade(SOOT[2] if charred else wood[2], face)                 # the board the books stand on
        back = shade(SOOT[0] if charred else wood[0], face)
        if mode == 4:
            # Pigeonholes: two to a shelf, a scroll's pale end or two in most.
            if u == 3:
                return shade(wood[1], face)
            hole = hash01(seed, shelf, u > 3, face)
            if hole < 0.8 and inrow in (1, 2) and u in ((1, 2) if u < 3 else (4, 5)):
                first = u in (1, 4)
                if (inrow == 1) == (hole < 0.4) or hole < 0.2:
                    return shade(PAGE[2 if first else 0], face)
            if hole > 0.45 and inrow == 3 and u in (2, 5, 6):
                return shade(PAGE[1 if u != 6 else 0], face)
            return back
        book, first = book_at(seed, shelf, face, u)
        h = hash01(seed, shelf, face, book, 5)
        if charred:
            if h < 0.55:
                # An ember still glowing on the lowest shelves.
                glow = hash01(seed, shelf, u, face, 6)
                if shelf >= rows - 2 and inrow == SHELF_ROWS - 2 and glow > 0.72:
                    return EMBER[2 if glow > 0.9 else 1]
                return back
            return shade(SOOT[2] if (inrow > 0 or h > 0.8) else SOOT[0], face)
        if mode == 2:
            # Ransacked: most books gone, one or two left lying flat on a shelf.
            lying = hash01(seed, shelf, face, 77)
            if lying < 0.6 and (inrow == 3 and 1 <= u <= 4 or inrow == 2 and 2 <= u <= 4 and lying < 0.3):
                spine, band = BOOKS[int(lying * 311 + inrow) % len(BOOKS)]
                return shade(PAGE[1] if u == 4 else spine, face)
            if h < 0.6 or lying < 0.6:
                return back
        elif h < 0.06:
            return back                                                           # a gap where a book was taken
        # Mostly brown and oxblood leather, a green, a blue or a pale one among them.
        pick = hash01(seed, shelf, face, book, 13)
        spine, band = BOOKS[(0, 5, 0, 1, 5, 2, 3, 6, 4, 1)[int(pick * 10) % 10]]
        short = hash01(seed, shelf, face, book, 8) < 0.35
        if inrow == 0 and short:
            return back
        if inrow == 1 and not short and hash01(seed, shelf, face, book, 11) < 0.3:
            return shade(band, face)
        return shade(spine, face)
    return f


def shelf_wall(seed, mode, height=WALL_H, half=None):
    return block(seed, height=height, half=half, feature=shelf_feature(mode, seed))


WALLS = {
    "library_wall_1": lambda: shelf_wall(31, 0),
    "library_wall_2": lambda: shelf_wall(32, 1),
    "library_wall_3": lambda: shelf_wall(33, 2),
    "library_wall_4": lambda: shelf_wall(34, 3),
    "library_wall_5": lambda: shelf_wall(45, 4),
    "library_wall_6": lambda: shelf_wall(46, 0),
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


# What hangs on the stone piers between the cases: the block's size, only the fitting drawn, on the left face (_x) or
# mirrored on the right (_y), as the gaol's. The sconce's flame is where the gaol's torch burns ----------------------------
def fitting(kind):
    im = Img(CW, WALL_H + CH)
    i, f = PAL["iron"], PAL["fire"]

    def put(u, v, col):
        im.set(u, cap_bottom(u) + 1 + v, col)
    if kind == "torch":
        # A brass sconce of three candles.
        b = PAL["brass"]
        for u in (2, 3, 4):
            put(u, 14, b[1])
        put(3, 15, b[0]); put(3, 16, b[0]); put(1, 13, b[1]); put(5, 13, b[1])
        for u, top in ((1, 10), (3, 8), (5, 10)):
            for v in range(top, 13 if u != 3 else 14):
                put(u, v, PAGE[2])
            put(u, top - 1, f[2]); put(u, top - 2, f[1])
        put(3, 5, f[0])
    elif kind == "chains":
        # A framed portrait, scorched at a corner.
        w = PAL["wood"]
        for v in range(6, 16):
            for u in range(1, 6):
                edge = v in (6, 15) or u in (1, 5)
                put(u, v, PAL["brass"][1] if edge else "4a5a4a")
        put(3, 8, "d8b898"); put(3, 9, "d8b898"); put(2, 9, "d8b898"); put(4, 9, "c8a888"); put(3, 7, "5a3a24"); put(2, 8, "5a3a24"); put(4, 8, "5a3a24")
        for v in (10, 11, 12, 13, 14):
            put(2, v, "8a2e28"); put(3, v, "a83a30"); put(4, v, "8a2e28")
        put(3, 10, "e0c080")
        for u, v in ((4, 14), (5, 14), (5, 13), (4, 15), (5, 15)):
            put(u, v, SOOT[1])
    else:
        # A long hanging in the library's red and gold, an open book on it, its foot burnt away.
        c, b = PAL["cloth"], PAL["brass"]
        for u in range(1, 6):
            put(u, 4, b[1])
        for v in range(5, 19):
            for u in range(1, 6):
                burnt = v > 14 and hash01(u, v, 3) < (v - 14) * 0.22
                if burnt:
                    continue
                put(u, v, b[0] if u in (1, 5) else c[2] if u < 4 else c[1])
        put(2, 8, PAGE[2]); put(4, 8, PAGE[1]); put(2, 9, PAGE[1]); put(4, 9, PAGE[1]); put(3, 9, b[2]); put(3, 8, b[0])
        put(3, 12, b[1]); put(2, 15, EMBER[1])
    return im


FITTINGS = {}
for _kind in ("torch", "chains", "banner"):
    FITTINGS[f"library_wall_{_kind}_x"] = (lambda k=_kind: fitting(k))
    FITTINGS[f"library_wall_{_kind}_y"] = (lambda k=_kind: fitting(k).mirrored())


# Free-standing cases: a bookcase run, books on its long faces and a wood end; and a round stone column -------------------
SHELF_H = 22


def bookcase(along_x, seed):
    books = shelf_feature(0 if seed % 2 else 1 if seed % 5 == 0 else 0, seed, rows=4, plinth=False)
    wood = PAL["wood"]

    def feature(face, u, v):
        if v >= SHELF_H - 1:
            return shade(wood[0], face)
        # Books on both faces: an aisle two cells thick shows either side, and a case's end is seldom seen.
        c = books(face, u, v)
        return c or shade(wood[2] if v < 2 else wood[1], face)
    im = block(seed, height=SHELF_H, feature=feature)
    # A wooden top in place of the stone one.
    for y in range(CH):
        for x in range(CW):
            c = im.c[y][x]
            if c is not None and c != K:
                im.c[y][x] = wood[2] if y == cap_bottom(x) else wood[1] if (x // 3 + y) % 2 else wood[0]
    return im


def pillar():
    """A round column on a square base, a carved capital under the ceiling."""
    im = Img(CW, WALL_H + CH)
    s = PAL["stone"]
    top = 2
    for y in range(top, im.h - 1):
        cap = y < top + 4
        base = y > im.h - 6
        half = 5 if (cap and y < top + 2) or (base and y > im.h - 4) else 4 if cap or base else 3
        for x in range(7 - half, 7 + half):
            k = (x - (7 - half)) / (2 * half - 1)
            tone = 4 if k < 0.2 else 3 if k < 0.45 else 2 if k < 0.75 else 1
            if not cap and not base and (y - top) % 7 == 3:
                tone = max(1, tone - 1)                         # the joint between two drums
            if cap and y == top + 3 or base and y == im.h - 6:
                tone = max(1, tone - 1)
            im.set(x, y, s[tone])
    # Soot at its head.
    for y in range(top, top + 5):
        for x in range(CW):
            if im.get(x, y) is not None and hash01(x, y, 4) < 0.3:
                im.set(x, y, SOOT[2])
    return im.outline()


SHELVES = {"library_shelf_x_1": lambda: bookcase(True, 61), "library_shelf_x_2": lambda: bookcase(True, 65),
           "library_shelf_y_1": lambda: bookcase(False, 63), "library_shelf_y_2": lambda: bookcase(False, 75),
           "library_pillar": pillar}


# The stair doorway: gaol_art's arch in the library's stone, with a torch either side ---------------------------------------
STAIRS = {"stairs_down_x_library": lambda: g.doorway(True, False), "stairs_up_x_library": lambda: g.doorway(False, False),
          "stairs_down_y_library": lambda: g.doorway(True, True), "stairs_up_y_library": lambda: g.doorway(False, True)}


# Props: drawn from the front, lit from the top left, outlined -------------------------------------------------------------
def tome(im, x, y, w, h, book, pages="r"):
    """A closed book lying flat, its spine to the left: the cover, the page block showing along one side."""
    spine, band = book
    im.rect(x, y, x + w - 1, y + h - 1, spine)
    im.rect(x, y, x + w - 1, y, shade(spine, 0) if h < 2 else band if h > 2 else spine)
    im.rect(x, y, x, y + h - 1, shade(spine, 1))
    if h >= 2:
        if pages == "r":
            im.rect(x + w - 1, y + 1 if h > 2 else y, x + w - 1, y + h - 1, PAGE[1])
        else:
            im.rect(x + 2, y + h - 1, x + w - 1, y + h - 1, PAGE[1])


def flame(im, cx, base, r, tall):
    """A fire: red at its skirts, orange, a yellow heart, a few tongues above."""
    f = EMBER
    for y in range(int(base - tall), int(base) + 1):
        t = (base - y) / tall
        half = r * (1 - t) ** 0.7 * (1.0 if t > 0.12 else 0.8)
        wob = math.sin(y * 1.9) * 0.6 * t
        for x in range(int(cx - half + wob), int(cx + half + wob) + 1):
            d = abs(x - cx - wob) / max(half, 0.5)
            k = 3 if d < 0.3 and t < 0.5 else 2 if d < 0.6 and t < 0.75 else 1 if d < 0.9 else 0
            im.set(x, y, f[k])
    im.set(cx - 1, base - tall - 2, f[1]); im.set(cx + 2, base - tall, f[0])


def lectern():
    im = Img(15, 23)
    w = PAL["wood"]
    im.rect(6, 9, 8, 19, w[1]); im.rect(6, 9, 6, 19, w[2])                       # the stem
    im.rect(5, 12, 9, 12, w[2]); im.rect(5, 17, 9, 17, w[2])                     # its turned rings
    im.rect(3, 20, 11, 21, w[1]); im.rect(3, 20, 11, 20, w[2]); im.rect(2, 21, 12, 21, w[0])
    for k in range(4):                                                          # the slanted desk
        im.rect(1, 5 + k, 13, 5 + k, w[2] if k else w[3])
    im.rect(1, 9, 13, 9, w[0])
    # An open book on it, a ribbon down the gutter, one page scorched.
    im.rect(2, 2, 6, 6, PAGE[2]); im.rect(8, 2, 12, 6, PAGE[1]); im.rect(7, 2, 7, 7, "7a2a26")
    for y in (3, 5):
        im.line(3, y, 5, y, PAGE[0]); im.line(9, y, 11, y, PAGE[0])
    im.set(12, 2, SOOT[2]); im.set(11, 2, EMBER[0]); im.set(12, 3, SOOT[1])
    return im.outline()


def reading_desk():
    im = Img(26, 18)
    w = PAL["wood"]
    im.rect(1, 7, 24, 9, w[2]); im.rect(1, 7, 24, 7, w[3]); im.rect(1, 10, 24, 10, w[0])      # the top
    im.rect(2, 11, 23, 12, w[1])                                                              # the apron
    for x in (2, 21):
        im.rect(x, 11, x + 2, 16, w[1]); im.rect(x, 11, x, 16, w[2])
    im.rect(4, 14, 21, 14, w[0])                                                              # the stretcher
    # An open book, a stack, an inkwell with a quill, a candle.
    im.rect(4, 5, 7, 6, PAGE[2]); im.rect(9, 5, 12, 6, PAGE[1]); im.rect(8, 5, 8, 6, "34447a")
    im.line(5, 5, 6, 5, PAGE[0]); im.line(10, 6, 11, 6, PAGE[0])
    tome(im, 14, 5, 5, 2, BOOKS[0]); tome(im, 15, 3, 4, 2, BOOKS[2])
    im.rect(20, 5, 21, 6, "2a2a38"); im.line(21, 4, 23, 0, PAGE[2]); im.set(23, 1, PAGE[1])
    im.rect(1, 3, 1, 6, PAGE[2]); im.set(1, 2, EMBER[3]); im.set(1, 1, EMBER[2])
    return im.outline()


def book_pile(kind):
    if kind == 0:                    # a tidy stack
        im = Img(13, 14)
        tome(im, 1, 10, 11, 3, BOOKS[0]); tome(im, 2, 7, 10, 3, BOOKS[3]); tome(im, 1, 5, 9, 2, BOOKS[4])
        tome(im, 3, 2, 8, 3, BOOKS[2])
        return im.outline()
    if kind == 1:                    # a stack that slid over, one book open on the floor
        im = Img(19, 12)
        tome(im, 1, 8, 10, 3, BOOKS[5]); tome(im, 2, 5, 9, 3, BOOKS[1]); tome(im, 5, 3, 8, 2, BOOKS[6])
        im.rect(12, 9, 14, 10, PAGE[2]); im.rect(16, 9, 18, 10, PAGE[1]); im.rect(15, 9, 15, 10, "7a2a26")
        im.set(13, 9, PAGE[0]); im.set(17, 10, PAGE[0])
        return im.outline()
    im = Img(17, 13)                 # a heap with a scorched top and a scroll
    tome(im, 1, 9, 9, 3, BOOKS[2]); tome(im, 7, 10, 9, 2, BOOKS[0]); tome(im, 3, 6, 9, 3, BOOKS[3])
    tome(im, 5, 4, 7, 2, ("2a2426", "3a3436"))
    im.set(8, 4, EMBER[1]); im.set(10, 5, EMBER[0])
    im.rect(11, 7, 15, 8, PAGE[1]); im.set(11, 7, PAGE[2]); im.set(15, 8, PAGE[0]); im.set(11, 8, "7a2a26")
    return im.outline()


def fallen_shelf():
    """A bookcase on its side: the frame, its shelves upright now, books spilled out before it."""
    im = Img(28, 15)
    w = PAL["wood"]
    im.rect(1, 2, 26, 10, w[0])
    im.rect(1, 2, 26, 2, w[3]); im.rect(1, 10, 26, 10, w[1]); im.rect(1, 2, 1, 10, w[2]); im.rect(26, 2, 26, 10, w[1])
    for x in (7, 13, 20):
        im.rect(x, 3, x, 9, w[2])
    # Books still wedged in two of the bays, lying on their sides.
    for bay, rows in ((2, ((8, 0), (6, 3), (4, 1))), (14, ((8, 6), (6, 2)))):
        for y, b in rows:
            tome(im, bay, y, 5, 2, BOOKS[b])
    im.rect(21, 4, 25, 9, SOOT[1]); im.set(22, 8, EMBER[1]); im.set(24, 9, EMBER[0]); im.set(23, 6, SOOT[2])
    # Spilled before it.
    tome(im, 3, 12, 6, 2, BOOKS[4]); tome(im, 11, 11, 6, 3, BOOKS[0]); tome(im, 19, 12, 5, 2, BOOKS[3])
    im.rect(8, 13, 9, 13, PAGE[2]); im.rect(25, 13, 26, 13, PAGE[1])
    return im.outline()


def burning_books():
    im = Img(19, 22)
    tome(im, 1, 18, 9, 3, BOOKS[0]); tome(im, 9, 19, 9, 2, BOOKS[3]); tome(im, 4, 15, 10, 3, ("2a2426", "3a3436"))
    tome(im, 6, 13, 7, 2, BOOKS[1])
    im.set(2, 20, SOOT[1]); im.set(15, 20, SOOT[1])
    flame(im, 9, 14, 5.2, 11)
    im.set(5, 16, EMBER[1]); im.set(12, 16, EMBER[2]); im.set(8, 17, EMBER[1])
    return im.outline()


def ash_heap():
    im = Img(19, 10)
    im.blob(9.5, 6.6, 9, 3.4, ASH)
    rng = random.Random(7)
    for _ in range(10):
        x, y = rng.randint(3, 15), rng.randint(4, 8)
        if im.get(x, y) is not None:
            im.set(x, y, ASH[rng.choice((0, 3))])
    # The corner of a cover and a curled page left in it.
    im.rect(4, 3, 7, 4, "2a2426"); im.set(4, 3, "7a2a26"); im.rect(11, 3, 13, 3, PAGE[0]); im.set(13, 2, PAGE[1])
    im.set(8, 5, EMBER[1]); im.set(12, 6, EMBER[0]); im.set(10, 4, EMBER[2])
    return im.outline()


def urn():
    im = Img(11, 16)
    c = ["4a2e22", "7a4a34", "a8683e", "c88a58"]
    im.blob(5.5, 10, 4.4, 4.8, c)
    im.rect(4, 3, 6, 5, c[1]); im.rect(3, 2, 7, 2, c[2]); im.rect(4, 14, 6, 14, c[0]); im.rect(3, 15, 7, 15, c[1])
    im.line(2, 8, 8, 8, PAL["brass"][1]); im.line(2, 12, 8, 12, c[0]); im.set(4, 7, c[3]); im.set(3, 9, c[3])
    return im.outline()


PROPS = {"prop_lectern": lectern, "prop_reading_desk": reading_desk, "prop_book_pile": lambda: book_pile(0),
         "prop_book_pile_2": lambda: book_pile(1), "prop_book_pile_3": lambda: book_pile(2),
         "prop_fallen_shelf": fallen_shelf, "prop_burning_books": burning_books, "prop_ash_heap": ash_heap, "prop_urn": urn}


def ash_decal(seed):
    rng = random.Random(seed)
    im = Img(27, 14)

    def inside(x, y):
        return abs(x + 0.5 - 13.5) / 13.5 + abs(y + 0.5 - 7) / 7 < 0.8
    for _ in range(46):
        x, y = rng.randint(2, 24), rng.randint(2, 11)
        if inside(x, y):
            c0 = ASH[rng.choice((0, 0, 1, 1, 2))]
            im.set(x, y, c0)
            if rng.random() < 0.5:
                im.set(x + 1, y, c0)
    # A loose page or two.
    for _ in range(2):
        x, y = rng.randint(6, 19), rng.randint(4, 9)
        im.rect(x, y, x + 2, y + 1, PAGE[1]); im.set(x + 2, y, PAGE[0]); im.set(x, y + 1, PAGE[2])
    x, y = rng.randint(5, 21), rng.randint(4, 9)
    im.set(x, y, EMBER[1])
    return im


DECALS = {"decal_ash_1": lambda: ash_decal(1), "decal_ash_2": lambda: ash_decal(2)}


# The floor: square tiles laid as a chequer, dark and darker, drawn as they lie on screen (seamless at 108, as the gaol's),
# scorched in patches, a drift of ash on some, a rare ember --------------------------------------------------------------
def floor(size=108):
    f = PAL["floor"]
    pitch = 13.5

    def slab(i, j):
        return (i + j) % 16, (i - j) % 32
    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            a, b = (x + 2 * y) / pitch, (x - 2 * y) / pitch
            i, j = math.floor(a), math.floor(b)
            fa, fb = a - i, b - j
            p, q = slab(i, j)
            tone = hash01(p, q, 11)
            scorch = hash01(p // 4, q // 4, 23) < 0.3 and hash01(p, q, 29) < 0.75      # a burnt patch of tiles
            if fa < 0.1 or fb < 0.1:
                col = f[0]
            else:
                light = (i + j) % 2 == 0
                k = 3 if light else 2
                if tone < 0.18:
                    k -= 1
                if scorch:
                    k = 1
                if (fa < 0.2 or fb > 0.93) and not scorch:
                    k = min(4, k + 1)                           # a lit upper edge
                speck = hash01(x, y, 17)
                if speck < 0.04:
                    k = max(1, k - 1)
                col = f[k]
                if speck > 0.988 or (scorch and speck > 0.95):
                    col = ASH[1 if speck > 0.994 else 0]
                # A crack across a tile, an ember still in a burnt one.
                if tone > 0.93 and abs(fa - fb) < 0.05 and 0.25 < fa < 0.8:
                    col = f[0]
                if scorch and tone > 0.9 and 0.45 < fa < 0.6 and 0.45 < fb < 0.6:
                    col = EMBER[1 if speck > 0.5 else 0]
            row.append((int(col[0:2], 16), int(col[2:4], 16), int(col[4:6], 16), 255))
        rows.append(row)
    return rows


def main():
    written, preview = [], []
    for group, folder in ((WALLS, "World"), (FITTINGS, "World"), (SHELVES, "World"), (STAIRS, "World"), (PROPS, "Dungeon"), (DECALS, "Dungeon")):
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
    def wall_at(k):
        if k % 4 == 0:
            return "library_wall_4"
        return ("library_wall_1", "library_wall_6", "library_wall_2", "library_wall_1", "library_wall_5", "library_wall_3")[(k * 7 + k // 4) % 6]
    cells = []
    for k in range(0, 9):
        cells.append((k, 9, "x"))
        cells.append((9, k, "y"))
    cells.append((9, 9, "c"))
    for cx, cy, kind in sorted(cells, key=lambda c: -(c[0] + c[1])):
        ox, oy = at(cx, cy)
        name = wall_at(cx + 9 if kind == "x" else cy + 9) if kind != "c" else "library_wall_4"
        sc.paste(WALLS[name](), ox - 7, oy - WALL_H - 3)
        if name == "library_wall_4" and kind != "c":
            fit = ("torch", "banner", "chains")[(cx + cy) // 4 % 3]
            sc.paste(FITTINGS[f"library_wall_{fit}_{'y' if kind == 'x' else 'x'}"](), ox - 7, oy - WALL_H - 3)
    for name, cx, cy in (("library_shelf_x_1", 3, 5), ("library_shelf_x_2", 4, 5), ("library_shelf_x_1", 5, 5), ("library_pillar", 7, 3)):
        ox, oy = at(cx, cy)
        p = SHELVES[name]()
        sc.paste(p, ox - 7, oy - p.h + 4)
    for name, cx, cy in (("prop_reading_desk", 7, 7), ("prop_lectern", 3, 7), ("prop_book_pile", 5, 2), ("prop_burning_books", 7, 5),
                         ("prop_fallen_shelf", 2, 3), ("prop_ash_heap", 5, 7), ("prop_urn", 8, 2), ("prop_candles", 3, 2),
                         ("prop_book_pile_2", 1, 6), ("prop_book_pile_3", 6, 1)):
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
