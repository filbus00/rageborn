# The Bookworm (2026-11-16, redrawn the same day): the Burnt Library's swarm, a fat pale grub in ringed segments with a
# brown head, red eyes and mandibles, humping along on a row of stub legs.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
SIDE = [E] * 10 + [
        ".....KKKK.......",
        "...KKpPpPKK.KKK.",
        "..KpPpPpPpPKbbEK",
        ".KPpPpPpPpPpbbbK",
        ".KKwKwKwKwKwKKmK",
        "..K.K.K.K.K....."]
FRONT = [E] * 9 + [
         ".....KKKKKK.....",
         "....KpPPPPpK....",
         "...KpKbbbbKpK...",
         "...KpbEbbEbpK...",
         "...KpbbbbbbpK...",
         "....KmKbbKmK....",
         ".....K.KK.K....."]
BACK = FRONT[:11] + ["...KpPppppPpK...", "...KpPPPPPPpK...", "...KpPppppPpK...", "....KpPPPPpK....", ".....KKKKKK....."]
kit.Character("bookworm", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"p": "d8d0b8", "P": "f4ecd4", "b": "7a4a2e", "E": "ff3b30", "w": "a89878", "m": "3a2418"}, role="swarm",
              step_rows=1, timing={"run": (0.35, 3.6)}).build()
