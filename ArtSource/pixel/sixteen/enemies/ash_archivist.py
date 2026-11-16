# The Ash Archivist (2026-11-16, redrawn the same day with his own side and back views): the Burnt Library's last boss
# (level 3-12), its keeper burnt to ash and still at his work: a charcoal cowl crowned with flame, a dark face with two
# ember eyes over a beard of ash, a great open book held before him, its pages lit from within, embers climbing the hem
# of his robe. 26 drawn pixels tall, drawn at his VisualScale (1.6).
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit


def over(rows, pixels):
    """Rows with single pixels drawn over them: (x, y, letter)."""
    out = [list(r) for r in rows]
    for x, y, ch in pixels:
        out[y][x] = ch
    return ["".join(r) for r in out]

FRONT = ["...........ee...........",
         "..........eyye..........",
         ".........eeyyee.........",
         "........KKeyyeKK........",
         ".......KccccccccK.......",
         "......KccCCCCCCccK......",
         "......KcCqqqqqqCcK......",
         "......KcCqEqqEqCcK......",
         "......KcCqqqqqqCcK......",
         "......KccCaaaaCccK......",
         ".......KccaaaaccK.......",
         "....KKKKccaaaaccKKKK....",
         "...KccccccCaaCccccccK...",
         "..KccCcKKKKKKKKKKcCccK..",
         "..KccCKpPPPPKKPPPPpKccK.",
         "..KssKKpPePPKKPPePpKssK.",
         "...KKKKpPPePKKPePPpKKK..",
         "......KKKKKKKKKKKKK.....",
         ".......KcCccccccCcK.....",
         ".......KcCccccccCcK.....",
         "......KccCccccccCccK....",
         "......KccCccceccCccK....",
         ".....KcccCcceeccCcccK...",
         ".....KcecCccceccCcecK...",
         "....KcceccCccccCcceccK..",
         "....KKKKKKKKKKKKKKKKKK.."]
SIDE = ["...........ee...........",
        "..........eyye..........",
        ".........eeyyee.........",
        ".........KKeyyeK........",
        "........KccccccccK......",
        ".......KccccCCCCcK......",
        ".......KcccCqqqqqK......",
        ".......KcccCqqEqqK......",
        ".......KcccCqqqqK.......",
        ".......KccccCaaaK.......",
        "........KccccaaaK.......",
        "......KKKccccaaK........",
        ".....KccccccccaKKKKKK...",
        "....KccCccccccKpPPPPpK..",
        "....KccCccccsssKPePPpK..",
        "....KccCcccKKKKKPPePpK..",
        "....KccCccccK..KKKKKK...",
        "....KccCccccK...........",
        "....KccCccccK...........",
        "....KccCcccccK..........",
        "...KcccCcccccK..........",
        "...KcccCccecccK.........",
        "..KccccCceecccK.........",
        "..KcecccCcecceK.........",
        ".KcceccccCcccecK........",
        ".KKKKKKKKKKKKKKK........"]
BACK = FRONT[:4] + [".......KccccccccK.......", "......KccccccccccK......", "......KccCccccCccK......", "......KccCccccCccK......",
                    "......KccCccccCccK......", "......KcccCccCcccK......", ".......KccccccccK.......", "....KKKKccccccccKKKK....",
                    "...KccccccCccCccccccK...", "..KccCccccCccCccccCccK..", "..KccCcKccCccCccKcCccK..", "..KssK.KccCccCccK.KssK..",
                    "...KK..KccCccCccK..KK...", ".......KccCccCccK.......", ".......KcCccccccCcK.....", ".......KcCccccccCcK....."] + FRONT[20:]
kit.Character("ash_archivist", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"c": "5e5a66", "C": "3e3a46", "q": "1a161e", "a": "b0acb4", "e": "ff7a2a", "y": "ffd84a", "E": "ffb040",
               "p": "c8b890", "P": "f0e4c4", "s": "8e8a94"},
              role="caster", baked_scale=1.6, step_rows=1, margin=(8, 10, 8, 2), glow="y",
              hand={"s": (12, 15), "se": (18, 14), "e": (18, 14), "ne": (18, 14), "n": (12, 15)},
              timing={"run": (0.6, 3.0), "walk": (1.1, 1.0)}).build()
