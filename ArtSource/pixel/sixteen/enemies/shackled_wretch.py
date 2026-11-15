# The Shackled Wretch (2026-11-14): the Iron Gaol's swarmer, a starved prisoner the gaol never let go: a shaved grey
# head with sunken yellow eyes, an iron collar, rags, broken chains hanging from the shackles on his wrists. He throws a
# link of his chain. Run: Blender -b -P ArtSource/pixel/sixteen/enemies/shackled_wretch.py
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

FRONT = [E,
         ".....KKKKKK.....",
         "....KssssssK....",
         "...KssssssssK...",
         "...KsEKssKEsK...",
         "...KssssssssK...",
         "....KsdKKdsK....",
         "...KKiiiiiiKK...",
         "..KsKrrrrrrKsK..",
         ".KsKKrrRrrrKKsK.",
         ".KsK.KrrrRrK.KsK",
         ".KiK.KrRrrrK.KiK",
         ".KcK.KrrrrRK.KcK",
         "..c..KRrKKrRK.c.",
         ".....KsK..KsK...",
         ".....KKK..KKK..."]
SIDE = [E,
        "......KKKKK.....",
        ".....KsssssK....",
        "....KsssssssK...",
        "....KssssEKsK...",
        "....KsssssssK...",
        ".....KsssdKK....",
        "....KKiiiiK.....",
        "....KrrrrrKKK...",
        "...KrrRrrsssK...",
        "...KrrrrKKiiK...",
        "...KrRrrK.KcK...",
        "...KrrrRK..c....",
        "...KRrKrK..c....",
        "....KsKsK.......",
        "....KKKKK......."]
BACK = FRONT[:4] + ["...KssssssssK...", "...KsdsssdssK...", "....KssssssK...."] + FRONT[7:]
kit.Character("shackled_wretch", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"s": "b4b09a", "d": "7e7a68", "r": "6e5c4a", "R": "4a3c30", "c": "aab0ba", "i": "5a626e", "E": "f0e85a"},
              role="melee", timing={"walk": (1.0, 1.0)}).build()
