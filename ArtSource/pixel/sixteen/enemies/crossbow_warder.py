# The Crossbow Warder (2026-11-14): the Iron Gaol's marksman, a warder in a kettle helm and mail coif, a dark red
# tabard, a crossbow held across him. He shoots, and looses two heavy bolts.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

FRONT = [E,
         "......KKKK......",
         "....KKmmmmKK....",
         "..KKmmmmmmmmKK..",
         "..KMMMMMMMMMMK..",
         "...KcsEssEscK...",
         "...KccssssccK...",
         "....KccccccK....",
         "..KKttttttttKK..",
         ".KccKttTTttKccK.",
         ".KccKttTTttKccK.",
         "KwsKwwwwwwwwKswK",
         ".KKKKttwwttKKKK.",
         "....KlllKlllK...",
         "....KllK.KllK...",
         "....KKKK.KKKK..."]
SIDE = [E,
        ".......KKKK.....",
        ".....KKmmmmK....",
        "...KKmmmmmmmKK..",
        "...KMMMMMMMMMMK.",
        "....KccsssEsK...",
        "....KcccssssK...",
        ".....KcccccK....",
        "....KttttttK.K..",
        "...KtttTtttKKwK.",
        "...KtttTssswwwwK",
        "...KttttKKKKKwK.",
        "...KtttttK...K..",
        "....KlllllK.....",
        "....KllKllK.....",
        "....KKKKKKK....."]
BACK = FRONT[:5] + ["...KccccccccK...", "...KccccccccK...", "....KccccccK...."] + FRONT[8:11] + [".KsKttttttttKsK.", "..KKKttttttKKK.."] + FRONT[13:]
kit.Character("crossbow_warder", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"m": "a0aab6", "M": "6c7682", "s": "d8aa80", "E": "241c28", "t": "7e2a2a", "T": "561c1c", "c": "8c8c98",
               "w": "9a6a38", "l": "3e3a48"}, role="archer",
              hand={"s": (8, 11), "se": (15, 10), "e": (15, 10), "ne": (15, 10), "n": (8, 8)}).build()
