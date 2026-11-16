# The Candle Bearer (2026-11-16, redrawn the same day): the Burnt Library's support, a bent old monk in a brown habit,
# white-bearded, his hood crowned with burning candle stubs, a great candle held before him. Those near his light hit
# harder; he throws hot wax.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [".....y.yy.y.....",
         ".....w.ww.w.....",
         "....KwKwwKwK.Y..",
         "...KbbbbbbbbKyY.",
         "...KbbssssbbKyy.",
         "...KbsEssEsbKww.",
         "...KbbwwwwbbKww.",
         "....KbwwwwbKKww.",
         "..KKbbbwwbbbKww.",
         ".KbbKbbbbbbKbssK",
         ".KbBKbbccbbKKwwK",
         ".KssKbbbbbbK.ww.",
         "..KKKbBbbBbK.KK.",
         "...KbbBbbBbbK...",
         "...KbbbbbbbbK...",
         "...KKKKKKKKKK..."]
SIDE = ["......y.y.......",
        "......w.w...Y...",
        ".....KwKwK..yY..",
        "....KbbbbbK.yy..",
        "....KbbbbssKww..",
        "....KbbbsEsKww..",
        "....KbbbwwwKww..",
        ".....KbbwwK.ww..",
        "....KbbbbwKKww..",
        "...KbbbbbbbsswK.",
        "...KbbBbccKKwwK.",
        "...KbbBbbbK.KK..",
        "...KbbBbbbK.....",
        "...KbbBbbbbK....",
        "...KbbbbbbbK....",
        "...KKKKKKKKK...."]
BACK = FRONT[:4] + ["...KbbbbbbbbKyy.", "...KbbbBBbbbKww.", "...KbbbbbbbbKww.", "....KbbbbbbKKww.", "..KKbbbbbbbbKww."] + FRONT[9:]
kit.Character("candle_bearer", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "6e4c34", "B": "4a3022", "s": "d8b8a0", "E": "241c28", "w": "f4ecd8", "c": "c8b080", "y": "ffd84a",
               "Y": "ff9a2a"}, role="melee", step_rows=1).build()
