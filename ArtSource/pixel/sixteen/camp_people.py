# The camp's keepers and the pen's wolf, redrawn (2026-11-22, the owner: "redraw all characters in the starting camp ...
# Rework the characters, rework the assets"). Each is a letter grid, 22 wide, light from the top left, a dark outline
# added by Img.outline(). Letters are a palette per figure.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gaol_art import Img  # noqa: E402

SKIN = {"s": "9a6244", "S": "c48a64", "f": "e8b690", "F": "f6d0ae", "e": "140f1c", "m": "8a4034", "n": "b07454"}


def scaled(rows, height):
    """The grid shrunk to this many rows tall (the keepers match the archer's 21 pixels, 2026-11-22, the owner: "why are you
    making them double the size of the character"): each new pixel takes the commonest drawn letter of the block it covers,
    an eye or a mouth always winning, and stays empty when under half the block is drawn."""
    h, w = len(rows), max(len(r) for r in rows)
    factor = height / h
    nh, nw = height, max(1, round(w * factor))
    out = []
    for y in range(nh):
        y0, y1 = y / factor, (y + 1) / factor
        line = ""
        for x in range(nw):
            x0, x1 = x / factor, (x + 1) / factor
            counts, cells, drawn = {}, 0, 0
            for yy in range(int(y0), min(h, max(int(y0) + 1, int(round(y1))))):
                for xx in range(int(x0), min(w, max(int(x0) + 1, int(round(x1))))):
                    c = rows[yy][xx] if xx < len(rows[yy]) else "."
                    cells += 1
                    if c != ".":
                        drawn += 1
                        counts[c] = counts.get(c, 0) + 1
            if not counts or drawn * 2 < cells:
                line += "."
                continue
            pick = next((c for c in ("e", "m", "B") if c in counts), None) or max(counts, key=counts.get)
            line += pick
        out.append(line)
    return out


def from_grid(rows, palette):
    # One free pixel all round, for the outline.
    im = Img(max(len(r) for r in rows) + 2, len(rows) + 2)
    for y, row in enumerate(rows):
        for x, c in enumerate(row):
            if c != ".":
                im.set(x + 1, y + 1, palette[c])
    return im.outline()


# Brunhilde, the guard captain: auburn hair in a braid, the company's green tabard with a red trim, one steel pauldron, a red
# cape, a sword at her hip, her hand on its hilt.
BRUNHILDE = [
    "........hhhhhh........",
    "......hhHHHHHHhh......",
    ".....hHHjjHHjjHHh.....",
    "....hHHjHHHHHHjHHh....",
    "....hHjHsSSSSsHjHh....",
    "....hHHsSfFFfSsHHh....",
    "....hHjSeSffSeSjHh....",
    "....hHHSSSffSSSHHh....",
    ".....hHsSSnnSSsHh.....",
    ".....hhssSmmSssjh.....",
    "......hjsSSSSsjHh.....",
    ".......hhssssHHh......",
    "..rr...hHsSSsHhh.rr...",
    ".rRRiiiGGGllGGGIIqrr..",
    ".rRiJJIGGGlllGGGIIqr..",
    "rRRiJIIGGlGGGGGGGIIqr.",
    "rRRiIiGGGlGGrRGGGgIqrr",
    "rRqsSiGGgGGGRRgGGgSsrr",
    "rRqSfiGGgGGGqRgGGgSsrr",
    ".rRSfbbbbbbbbbbbbbSsr.",
    ".rRqsSwwwxxwwwwwxJsrr.",
    ".rRqGGGGGGGGGGGGGIIsr.",
    "..rRGgrrrrrrrrrrgIIJ..",
    "..rRGgGGGGGGGGGGgIIJ..",
    "...rrGgGGGGGGGGgg.IJ..",
    "....ddWWddddWWddd.Ii..",
    "....ddWWdd..WWdd..bb..",
    "....ddWWdd..WWdd......",
    "...dWWWWdd..WWWWd.....",
    "..dxWWWWWd..xWWWWd....",
    "..dxxWWWdd..dxxWWd....",
    "..dddddddd..dddddd....",
]
BRUNHILDE_PAL = dict(SKIN, h="5a2a14", H="8a4a24", j="b46a38", G="437c4a", g="2e5a3a", l="6aa060", r="7a1e1e", R="a82e2a",
                     q="d0483a", i="3a3e4a", I="6a7080", J="9aa2b2", b="b88a22", w="4e3220", W="74502e", x="9a6a3a", d="2a1c12")

# Thorin, the smith: bald, a grey beard, a leather apron over rolled sleeves, a hammer in his fist, tongs at his belt.
THORIN = [
    "........kkkkkk........",
    "......kkKKKKKKkk......",
    ".....kKKFFFFFFKKk.....",
    "....kKFFfFFFFfFFKk....",
    "....kKFfSSSSSSfFKk....",
    "....kFfSSfSSfSSfFk....",
    "....kFSSeSSSSeSSFk....",
    "...bkFSSSSffSSSSFkb...",
    "...bBkSSSBBBBSSSkBb...",
    "...bBBkBBBbbBBBBkBBb..",
    "....bBBBBBbbBBBBBBb...",
    "....bbBBBBBBBBBBBbb...",
    ".....bbBBBBBBBBBbb....",
    "..ddddbbbbbbbbbbbbdd..",
    ".dWWWdddAAAAAAAdddWWWd",
    "dWxWWdAAaAAAAaAAdWWxWd",
    "dWWWWdAAaaAAAAaadWWWWd",
    "dWWsSdAAAAAAAAAAdSsWWd",
    ".dsSfdAAaaAAaaAAdfSsd.",
    ".dSfFdAAAAAAAAAAdFfSd.",
    "..dsSdaAAaaaaAAadSsd..",
    "...dd.aAAAAAAAAa.dd...",
    "...tt.aAAaAAAAaa.hH...",
    "...tT.aAAAAAAAAa.hH...",
    "....uuAAAAAAAAAAuuhH..",
    "......uUUu..uUUu.hH...",
    "......uUUu..uUUu.IIII.",
    "......uUUu..uUUu.IIJJ.",
    "...kkkkkkk..kkkkkkk...",
    "...kKKKKKk..kKKKKKk...",
    "...kkkkkkk..kkkkkkk...",
]
THORIN_PAL = dict(SKIN, F="d9d9df", f="b4b6c0", S="c48a64", K="8a8c98", k="4a4c58", B="b4b6c0", b="8a8c98", A="74502e", a="4e3220",
                  d="2a1c12", W="c48a64", x="e8b690", u="3e2a1c", U="5a4028", t="6a7080", T="9aa2b2", h="4e3220", H="74502e",
                  I="6a7080", J="9aa2b2")

# Lyriel, the scout: a hood and short cloak of forest green over a brown tunic, a feather in the hood, a short bow over her
# shoulder, a leash in hand, trousers and boots.
LYRIEL_HOOD = [
    "..............wW......",
    ".............wWWb.....",
    "........ggggggwW......",
    "......ggGGGGGGgg......",
    ".....gGGlllllGGGg.....",
    "....gGGllGGGGllGGg....",
    "....gGlGgssSSsgGlGg...",
    "....gGGgsSfFFfSgGGg...",
    "....gGgsSeSffSeSgGg...",
    "....gGgsSSSffSSSgGg...",
    ".....gGgsSSnnSSsgGg...",
    ".....gGGgsSmmSsgGGg...",
    "......gGGGgsSSsGGGg...",
]


def _lyriel_rows():
    g = [list(r) for r in LYRIEL_HOOD] + [["."] * 22 for _ in range(18)]

    def put(x, y, c):
        if 0 <= x < 22 and 0 <= y < len(g):
            g[y][x] = c
    # The short cloak: shoulders then a flared hem, light on the left, dark on the right, a tunic panel down the front.
    for y in range(13, 25):
        hw = 6 + (1 if y >= 19 else 0) + (1 if y >= 22 else 0)
        for x in range(10 - hw, 11 + hw + 1):
            edge = x - (10 - hw)
            c = "l" if edge == 1 and y % 3 else "G"
            if x >= 11 + hw - 1:
                c = "g"
            if edge == 0:
                c = "g"
            put(x, y, c)
        if y < 23:
            for x in range(8, 13):
                put(x, y, "T" if y < 20 else "U")
    for x in range(7, 14):
        put(x, 17, "W")
    put(10, 17, "B")
    # Her hands, one with the leash.
    for (x, y) in ((5, 18), (6, 18), (5, 19), (6, 19)):
        put(x, y, "S" if y == 18 else "s")
    for (x, y) in ((15, 18), (16, 18), (15, 19), (16, 19)):
        put(x, y, "s")
    put(16, 20, "r"); put(16, 21, "r"); put(17, 22, "r"); put(17, 23, "R")
    # Trousers and boots.
    for y in range(25, 28):
        for x in (8, 9, 12, 13):
            put(x, y, "t")
    for y in range(28, 31):
        for x in (7, 8, 9, 10, 11, 12, 13, 14):
            if x in (10, 11):
                continue
            put(x, y, "W" if y < 30 else "d")
    # The bow over her right shoulder: a curve of wood standing behind her, with a string.
    for y in range(8, 27):
        x = 20 if y in (8, 9, 25, 26) else 21
        if g[y][x] == ".":
            g[y][x] = "W"
    for y in range(10, 25):
        if g[y][20] == ".":
            g[y][20] = "w"
    return ["".join(r) for r in g[:31]]


LYRIEL = _lyriel_rows()
LYRIEL_PAL = dict(SKIN, g="283a30", G="42584a", l="5c7a62", w="e8dcc0", W="74502e", b="9a6a3a", B="e8c050", d="2a1c12", x="9a6a3a", T="8a5a34", U="6a4426", t="2a3e32", r="a82e2a", R="d0483a")

# The wolf of the pen, sitting three-quarters to the right: pointed ears, a long muzzle, a pale chest, a haunch and the tail curled
# round, grey fur, yellow eyes, a red collar.
WOLF = [
    "..........kk..kk........",
    ".........kGGkkGGk.......",
    ".........kGgGGgGk.......",
    "........kGGGGGGGGkk.....",
    "........kGGeGGGGGGGkk...",
    "........kGGGGGGGGGnnk...",
    ".........kGGGGGGGGGnk...",
    ".........kkGGGGmGGkk....",
    "..........kRRRRRRk......",
    ".........kGGwwwwGGk.....",
    "........kGGGwwwwwGGk....",
    ".......kGGGGwwwwwGGGk...",
    ".......kGGGGGwwwwGGGk...",
    "......kGGGGGGGGwwGGGk...",
    ".....kGGGGGGGGGGGGGGk...",
    "....kGGGggGGGGGGkGGGk...",
    "...kGGGGggGGGGGGkGGGk...",
    "..kGGGGGggGGGGGGkGGGk...",
    "..kGGGGGGGGGGGGGGkGGk...",
    "..kgggGGGGGGGGkkkkkkk...",
    "...kkkkkkkkkkkgggGGk....",
]
WOLF_PAL = {"k": "34363e", "g": "5a5e6a", "G": "8a8e9a", "w": "c0c4cc", "e": "ffd84a", "n": "140f1c", "m": "6a3a3a", "R": "a82e2a"}


def _fix(rows, fixes):
    # The shrink blurs a face: set its eyes and mouth by hand (one pixel each).
    rows = list(rows)
    for i, text in fixes.items():
        assert len(text) == len(rows[i]), (i, text, rows[i])
        rows[i] = text
    return rows


def brunhilde():
    return from_grid(_fix(scaled(BRUNHILDE, 22), {4: "...hHSeSSeSHh..", 6: "...hhsSSmSsh..."}), BRUNHILDE_PAL)


def thorin():
    return from_grid(scaled(THORIN, 22), THORIN_PAL)


def lyriel():
    return from_grid(_fix(scaled(LYRIEL, 22), {6: "...gGsSeffeSggW."}), LYRIEL_PAL)


def wolf():
    return from_grid(scaled(WOLF, 15), WOLF_PAL)
