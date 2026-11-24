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


# The keepers, drawn the archer's size and way (2026-11-24, the owner: "redraw the keepers, they look bad", "give them idle
# animations"): 21 pixels tall in a 24-pixel cell like wild_arrow_archer_21.png, an 8-row head, eyes a single dark pixel
# each, a dark outline (A). Each row below is the figure's pixels, centred on the cell by keeper(); what they hold (her
# sword, his hammer, her bow, the feather, the braid) is laid over by coordinates. Four idle frames each: still, a breath
# (all above the waist a pixel down), their own small move, a blink.
CELL = 24
OUTLINE = "140f1c"
FACE = {"A": OUTLINE, "E": "ecbe9a", "R": "c0866a", "G": "241c2c", "I": "e29684"}


def _centre(rows):
    out = []
    for r in rows:
        lead = int(round(11.5 - len(r) / 2.0 + 0.01))
        out.append(("." * lead + r).ljust(CELL, ".")[:CELL])
    return ["." * CELL] * (CELL - len(rows)) + out


def _over(rows, marks):
    g = [list(r) for r in rows]
    for x, y, c in marks:
        if 0 <= y < len(g) and 0 <= x < CELL:
            g[y][x] = c
    return ["".join(r) for r in g]


# Brunhilde, the guard captain: an auburn braid, the company's green tabard with its red hem, steel pauldrons, a red cape,
# her sword at her hip.
BRUNHILDE_BODY = [
    "AAAAAA",
    "AcCCCCcA",
    "AcCCccCCcA",
    "AccCccccCccA",
    "AccEEEEEEccA",
    "AcBEGEEGEBcA",
    "AcBIEEEEIBcA",
    "AcBBERREBBcA",
    "AKLKANNNNAKLKA",
    "AKKKMNOONMKKKA",
    "AMKKMNOONMKKSA",
    "AMEKMNNNNMKESA",
    "AMEAHHJJHHAESA",
    "AMSANNOONNASSA",
    "AMSANNOONNASSA",
    "AMSAMMMMMMASSA",
    "AAAAFFAAFFAAAA",
    "AFFAAFFA",
    "AFFAAFFA",
    "AQQAAQQA",
    "AA..AA",
]
BRUNHILDE_PAL = dict(FACE, c="a85a2e", C="d07a3e", B="7a3a1e", K="8a92a2", L="d8dce4", k="4a5060", M="c02a2e", S="7a1626",
                     N="3e8442", O="74b65c", H="54341e", J="e6c25c", F="84562e", Q="32221e")


def _brunhilde_marks(frame):
    sway = 1 if frame == 2 else 0
    braid = [(16 + sway, 11, "C"), (16 + sway, 12, "c"), (16, 13, "c"), (16, 14, "C")]
    sword = [(18, 14, "J"), (19, 14, "J"), (20, 14, "J"), (19, 13, "H")] + [(19, y, "L") for y in range(15, 21)] + [(19, 21, "k")]
    return braid + sword


# Thorin, the smith: bald, a white beard, a leather apron, his sleeves rolled, a hammer in his hand.
THORIN_BODY = [
    "AAAAAA",
    "AEDDEEEA",
    "AEDEEEEEEA",
    "AwEEEEEEEEwA",
    "AwREEEEEERwA",
    "AwEGEEEEGEwA",
    "AwRWWEEWWRwA",
    "AAWWWWWWWWAA",
    "ATTWWWVVWWWTTA",
    "ATTAWWVVWWATTA",
    "AEEAFWVVWFAEEA",
    "AEEAFFWWFFAEEA",
    "AREAFFFFFFAERA",
    "AEEAFHFFHFAEEA",
    "AAAAFFFFFFAAAA",
    "AFHFFFFHFA",
    "AFFFFFFFFA",
    "AHHHHHHHHA",
    "AHHAAHHA",
    "AQQAAQQA",
    "AA..AA",
]
THORIN_PAL = dict(FACE, D="f6d8b8", w="a8aab6", W="e4e4ec", V="9a9cab", T="9a6836", F="84562e", H="54341e", Q="32221e",
                  K="8a92a2", L="d8dce4", k="4a5060")


def _thorin_marks(frame):
    lift = 2 if frame == 2 else 0
    handle = [(19, y - lift, "F") for y in range(15, 19)]
    head = [(18, 19 - lift, "L"), (19, 19 - lift, "K"), (20, 19 - lift, "K"), (21, 19 - lift, "K"),
            (18, 20 - lift, "K"), (19, 20 - lift, "k"), (20, 20 - lift, "k"), (21, 20 - lift, "k")]
    return handle + head


# Lyriel, the scout: a green hood with a white feather, a short green cloak over a brown tunic, a short bow on her back.
LYRIEL_BODY = [
    "AAAAAA",
    "ANOOOONA",
    "ANOONNNNPA",
    "ANNhhhhhhNPA",
    "ANhEEEEEEhPA",
    "ANhEGEEGEhPA",
    "ANNIEEEEIPPA",
    "ANNPEEEEPPPA",
    "ANNOOFFFFOONNA",
    "ANOOOFFJFFOONA",
    "ANOOEFFFFFEONA",
    "ANONHHHJHHHNOA",
    "ANOOFFFFFFFOPA",
    "ANOPFFFFFFPOPA",
    "ANNOPPPPPPOPNA",
    "AANNPPPPPPNNAA",
    "AAPPAAPPAA",
    "APPAAPPA",
    "AFFAAFFA",
    "AQQAAQQA",
    "AA..AA",
]
LYRIEL_PAL = dict(FACE, N="356a3c", O="5a8e4c", P="1f4a2c", h="3e2a1e", F="84562e", H="54341e", J="e6c25c", Q="32221e",
                  D="f2eede", M="c02a2e", b="9a6836", s="e4e0ca")


def _lyriel_marks(frame):
    tip = 1 if frame == 2 else 0
    feather = [(14, 2, "M"), (15 + tip, 1, "D"), (16 + tip, 0, "D")]
    bow = [(19, 5, "b"), (19, 6, "b"), (19, 18, "b"), (19, 19, "b")] + [(20, y, "b") for y in range(7, 18)] + \
          [(19, y, "s") for y in range(7, 18)]
    return feather + bow


# The row the breath takes out (a row that repeats its neighbour), so the figure keeps its feet where they are.
KEEPERS = {
    "brunhilde": (BRUNHILDE_BODY, BRUNHILDE_PAL, _brunhilde_marks, 17),
    "thorin": (THORIN_BODY, THORIN_PAL, _thorin_marks, 19),
    "lyriel": (LYRIEL_BODY, LYRIEL_PAL, _lyriel_marks, 16),
}


def keeper_rows(name, frame=0):
    body, pal, marks, squash = KEEPERS[name]
    rows = _over(_centre(body), marks(frame))
    if frame == 1:
        # The breath: everything above the waist a pixel lower, a repeated row taken out.
        rows = ["." * CELL] + rows[:squash] + rows[squash + 1:]
    if frame == 3:
        rows = [r.replace("G", "R") for r in rows]
    return rows


def keeper(name, frame=0):
    return from_grid(keeper_rows(name, frame), KEEPERS[name][1])


def brunhilde():
    return keeper("brunhilde")


def thorin():
    return keeper("thorin")


def lyriel():
    return keeper("lyriel")


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


def wolf():
    return from_grid(scaled(WOLF, 15), WOLF_PAL)
