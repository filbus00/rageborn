# The Antler Fiend (2026-11-07): the Werewolf kind, a gaunt beast with a skull for a head, antlers, cold blue eyes,
# showing ribs and long claws; it leaps in and claws.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [".KK.K......K.KK.",
         "..KaKaK..KaKaK..",
         "...KaaK..KaaK...",
         "....KaKKKKaK....",
         ".....KmmmmK.....",
         ".....KEmmEK.....",
         ".....KmKKmK.....",
         "....KKmmmmKK....",
         "...KffKKKKffK...",
         "..KffffffffffK..",
         ".KfKffffffffKfK.",
         ".KfKffrrrrffKfK.",
         "KcKKffrrrrffKKcK",
         "K.K.KffffffK.K.K",
         "....KfK..KfK....",
         "...KKfK..KfKK..."]
SIDE = [".....KK.K.......",
        "......KaKaK.....",
        ".......KaaK.....",
        "........KaK.....",
        ".......KmmmK....",
        ".......KmmEmK...",
        ".......KmmmmmK..",
        "......KKmmKKK...",
        ".....KffKK......",
        "....KffffffK....",
        "...KffffffffK...",
        "...KffrrfffffK..",
        "...KffrrffKfffKc",
        "....KffffK..KcK.",
        "....KfK.KfK.....",
        "...KKfK.KfKK...."]
BACK = FRONT[:4] + [".....KmmmmK.....", ".....KmmmmK.....", ".....KmmmmK....."] + FRONT[7:11] +        [".KfKffffffffKfK.", "KcKKffffffffKKcK"] + FRONT[13:]
kit.Character("antler_fiend", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"a": "d8c8a8", "m": "efe8d8", "E": "6ff2ff", "f": "6e5a72", "r": "cbbcae", "c": "ffffff"}, role="charger",
              timing={"run": (0.42, 3.6)}).build()
