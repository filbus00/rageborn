# The Brand Priest (2026-11-14): the Iron Gaol's caster, a confessor in a tall pointed hood and robe of dried-blood
# red, ember eyes in the dark of the hood, a branding iron glowing at its tip. He lays burning circles and throws a
# hot iron that bursts.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

FRONT = [".......KK.......",
         "......KrrK......",
         "......KrrK......",
         ".....KrrrrK.....",
         "....KrrrrrrK....",
         "...KrrssssrrK...",
         "...KrsEssEsrK...",
         "...KrrssssrrK...",
         "..KKrrrrrrrrKK..",
         ".KrrKrrrrrrKrrKg",
         ".KrrKrRrrRrKrrKg",
         ".KssKccccccKssKi",
         "..KKKrRrrRrKKK.i",
         "...KrrRrrRrrK..i",
         "...KrrrrrrrrK...",
         "...KKKKKKKKKK..."]
SIDE = ["........KK......",
        ".......KrrK.....",
        "......KrrrK.....",
        ".....KrrrrrK....",
        "....KrrrrrrrK...",
        "....KrrrrsssK...",
        "....KrrrrsEsK...",
        "....KrrrrrssK...",
        "....KKrrrrrKK...",
        "....KrrrrrrrK..g",
        "...KrrRrrrssiiig",
        "...KrrRrcccK....",
        "...KrrRrrrrK....",
        "...KrrRrrrrK....",
        "...KrrrrrrrK....",
        "...KKKKKKKKK...."]
BACK = FRONT[:5] + ["...KrrrrrrrrK...", "...KrrrRRrrrK...", "...KrrrrrrrrK..."] + FRONT[8:]
kit.Character("brand_priest", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"r": "822e22", "R": "541c16", "s": "1e1418", "E": "ffb030", "g": "ff8a2a", "i": "6c6c76", "c": "c8b080"},
              role="caster", glow="g", step_rows=1,
              hand={"s": (15, 9), "se": (15, 10), "e": (15, 10), "ne": (15, 10), "n": (15, 9)}).build()
