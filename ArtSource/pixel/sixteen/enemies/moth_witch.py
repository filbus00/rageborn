# The Moth Witch (2026-11-07): the HedgeWitch kind, a violet-robed witch with a pale face and moth wings marked with
# eyespots, feelers over her hood; she casts from the back of the pack.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = ["....K......K....",
         ".....K....K.....",
         ".KK...KKKK...KK.",
         "KwwK.KhhhhK.KwwK",
         "KwWwKhhhhhhKwWwK",
         "KwwwKhppppKKwwwK",
         "KwWwKhEppEhKwWwK",
         "KwwwKhpppphKwwwK",
         ".KwwKKhhhhKKwwK.",
         "..KwKhhhhhhKwK..",
         "..KKhhHhhHhhKK..",
         "...KhhHhhHhhK...",
         "..KphhHhhHhhpK..",
         "..KKhhhhhhhhKK..",
         "...KhhhhhhhhK...",
         "....KKKKKKKK...."]
SIDE = ["........K.......",
        ".........K......",
        ".KKK...KKKK.....",
        "KwwwK.KhhhhK....",
        "KwWwwKhhhhhhK...",
        "KwwWwKhhhpppK...",
        "KwWwwKhhhEppK...",
        "KwwwwKhhhpppK...",
        ".KwwwKKhhhhK....",
        "..KwwKhhhhhhK...",
        "...KKhhHhhHpK...",
        "....KhhHhhHhK...",
        "....KhhHhhHhhK..",
        "...KhhhhhhhhhK..",
        "...KhhhhhhhhhK..",
        "....KKKKKKKKK..."]
BACK = FRONT[:5] + ["KwwwKhhhhhhKwwwK", "KwWwKhhhhhhKwWwK", "KwwwKhhhhhhKwwwK"] + FRONT[8:]
kit.Character("moth_witch", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"w": "ecdcb4", "W": "a07848", "h": "7a44a0", "H": "4e2a70", "p": "f2e4ec", "E": "ffe04a"}, role="caster",
              hand={"s": (12, 12), "se": (11, 10), "e": (11, 10), "ne": (11, 10), "n": (7, 12)}, step_rows=1).build()
