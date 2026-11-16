# The Ash Penitent (2026-11-16, redrawn the same day with its own side and back views): the Burnt Library's swarmer, a
# monk the fire never let go: a charred hooded habit split by glowing ember cracks, an ashen face with ember eyes and an
# open mouth, grey hands. He shuffles and flicks embers.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E,
         ".....KKKKKK.....",
         "....KHHHHHHK....",
         "...KHhhhhhhHK...",
         "...KHhsssshHK...",
         "...KhsEssEshK...",
         "...KhhsKKshhK...",
         "....KhhsshhK....",
         "..KKhHhhhhHhKK..",
         ".KhhKhhehhhKhhK.",
         ".KhHKhheehhKHhK.",
         ".KhhKhhhehhKhhK.",
         ".KssKhehhhhKssK.",
         "..KKKheehhhKKK..",
         "....KhhhhehK....",
         "....KaKKaKaK...."]
SIDE = [E,
        "......KKKKK.....",
        ".....KHHHHHK....",
        "....KHhhhhhHK...",
        "....KHhhhsssK...",
        "....KhhhhsEsK...",
        "....KhhhhsKsK...",
        ".....KhhhhsK....",
        "....KhHhhhK.....",
        "...KhhhhhhhKKK..",
        "...KhhehhhsssK..",
        "...KhheehhKKK...",
        "...KhhhehhK.....",
        "...KhehhhhK.....",
        "...KhhhhehK.....",
        "...KaKKaKaK....."]
BACK = FRONT[:4] + ["...KHhhhhhhHK...", "...KhhhhhhhhK...", "...KhhhHHhhhK...", "....KhhhhhhK...."] + FRONT[8:]
kit.Character("ash_penitent", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"h": "3a2e2e", "H": "5a4a48", "e": "ff7a2a", "E": "ffb040", "s": "b4a69c", "a": "6e6462"}, role="melee",
              step_rows=1, timing={"walk": (1.0, 1.0)}).build()
