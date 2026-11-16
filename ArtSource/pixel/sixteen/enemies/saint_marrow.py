# Saint Marrow (2026-11-16, redrawn the same day with his own side and back views): the Burnt Library's first boss (level
# 3-6), the monks' saint, long dead and still leading their prayer: a tall mitre of gold with a red cross, a skull with
# two marrow-yellow lights for eyes, a cream cope with gold and red bands, bone hands, a gold crozier. 26 drawn pixels
# tall, drawn at his VisualScale (1.6) so the game does not stretch him.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit


def over(rows, pixels):
    """Rows with single pixels drawn over them: (x, y, letter)."""
    out = [list(r) for r in rows]
    for x, y, ch in pixels:
        out[y][x] = ch
    return ["".join(r) for r in out]

FRONT = ["...........KK...........",
         "..........KGGK..........",
         ".........KGGGGK.........",
         ".........KGwwGK.........",
         "........KGwRRwGK........",
         "........KGRRRRGK........",
         "........KGwRRwGK........",
         "........KGGGGGGK........",
         ".......KwwwwwwwwK.......",
         ".......KwKKwwKKwK.......",
         ".......KwKEwwEKwK.......",
         ".......KwwwKKwwwK.......",
         "........KwKwwKwK........",
         ".....KKKKKBBBBKKKKK.....",
         "....KGwwwGRwwRGwwwGK....",
         "...KGwwwwGRwwRGwwwwGK...",
         "...KGwwwwGRwwRGwwwwGK...",
         "...KGwwKwGRwwRGwKwwGK...",
         "...KBBBKwGRwwRGwKBBBK...",
         "....KKKKwGRwwRGwKKKK....",
         ".......KwGRwwRGwK.......",
         ".......KwGRwwRGwK.......",
         "......KwwGRwwRGwwK......",
         "......KwwGGwwGGwwK......",
         ".....KwwwwwwwwwwwwK.....",
         ".....KKKKKKKKKKKKKK....."]
CROOK = [(21, y, "g") for y in range(7, 26)] + [(21, 6, "G"), (21, 5, "G"), (21, 4, "G"), (20, 3, "G"), (19, 3, "G"), (18, 4, "G"),
                                               (18, 5, "G"), (19, 6, "G")]
FRONT = over(FRONT, CROOK)
SIDE = ["............KK..........",
        "...........KGGK.........",
        "..........KGGGGK........",
        "..........KGwwGK........",
        ".........KGwwwRGK.......",
        ".........KGwwRRGK.......",
        ".........KGwwwRGK.......",
        ".........KGGGGGGK.......",
        ".........KwwwwwwwK......",
        ".........KwwwwKKwK......",
        ".........KwwwwKEwK......",
        ".........KwwwwwwKK......",
        "..........KwwKwKwK......",
        ".......KKKKBBBKKK.......",
        "......KGwwwwwwRGK.......",
        ".....KGwwwwwwwRGK.......",
        ".....KGwwwwwwwRGKKK.....",
        ".....KGwwwwwKKKKBBK.....",
        ".....KGwwwwwwwRGKK......",
        ".....KGwwwwwwwRGK.......",
        "......KwwwwwwwRGK.......",
        "......KwwwwwwwRGK.......",
        ".....KwwwwwwwwRGK.......",
        ".....KwwwwwwwwGGK.......",
        "....KwwwwwwwwwwwwK......",
        "....KKKKKKKKKKKKKK......"]
SIDE = over(SIDE, [(19, y, "g") for y in range(7, 26) if y != 17] + [(19, 6, "G"), (19, 5, "G"), (19, 4, "G"), (20, 3, "G"), (21, 3, "G"),
                                                                    (22, 4, "G"), (22, 5, "G"), (21, 6, "G")])
BACK = FRONT[:3] + [".........KGGGGK.........", "........KGGGGGGK........", "........KGggggGK........", "........KGGGGGGK........",
                    "........KGGGGGGK........", ".......KwwwwwwwwK.......", ".......KwwwwwwwwK.......", ".......KwwBwwBwwK.......",
                    ".......KwwwwwwwwK.......", "........KwwwwwwK........", ".....KKKKKBBBBKKKKK.....", "....KGwwwwGGGGwwwwGK....",
                    "...KGwwwwwwGGwwwwwwGK...", "...KGwwwGGGGGGGGwwwGK...", "...KGwwKwwwGGwwwKwwGK...", "...KBBBKwwwGGwwwKBBBK...",
                    "....KKKKwwwGGwwwKKKK....", ".......KwwwGGwwwK.......", ".......KwwwGGwwwK.......", "......KwwwwGGwwwwK......",
                    "......KwwwwwwwwwwK......"] + FRONT[24:]
BACK = over(BACK, CROOK)
kit.Character("saint_marrow", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"w": "ece4d4", "B": "b8ae98", "G": "e0b840", "g": "a07820", "R": "a82a2a", "E": "ffe060"},
              role="melee", baked_scale=1.6, step_rows=2, margin=(8, 10, 8, 2),
              timing={"run": (0.5, 3.0), "walk": (1.0, 1.0)}).build()
