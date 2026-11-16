# The Flame Cantor (2026-11-16, redrawn the same day): the Burnt Library's caster, a chanting monk in a white choir robe
# with a gold stole, scorched black at the hem, his head a crown of flame, mouth open, both hands raised and lit. He
# chants three fireballs.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [".......e........",
         "......eye.e.....",
         ".....eeyyee.....",
         "....KeyyyyeK....",
         "....KssssssK....",
         "....KsEssEsK....",
         "....KssKKssK....",
         ".e...KssssK...e.",
         "eyeKKwwGGwwKKeye",
         "KsKwwwwGGwwwwKsK",
         ".KKwKwwGGwwKwKK.",
         "...KKwwGGwwKK...",
         "...KwwwGGwwwK...",
         "...KwwwwwwwwK...",
         "...KkwkwwkwkK...",
         "...KKKKKKKKKK..."]
SIDE = [".......e........",
        "......eye.......",
        ".....eeyye......",
        ".....KeyyyeK....",
        ".....KeesssK....",
        ".....KeesEsK....",
        ".....KessKsK....",
        "......KsssK..eye",
        "....KKwwwKKKKsK.",
        "...KwwwGwwwwwKK.",
        "...KwwwGwwKKK...",
        "...KwwwGwwK.....",
        "...KwwwGwwK.....",
        "...KwwwwwwwK....",
        "...KkwkwwkwK....",
        "...KKKKKKKKK...."]
BACK = FRONT[:4] + ["....KeyyyyeK....", "....KeeyyeeK....", "....KeeeeeeK....", ".e...KeeeeK...e.", "eyeKKwwwwwwKKeye",
                    "KsKwwwwwwwwwwKsK", ".KKwKwwwwwwKwKK.", "...KKwwwwwwKK...", "...KwwwwwwwwK..."] + FRONT[13:]
kit.Character("flame_cantor", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"w": "ece4d4", "G": "d8b040", "s": "d8b8a0", "E": "241c28", "e": "ff7a2a", "y": "ffd84a", "k": "2a2428"},
              role="caster", glow="y", step_rows=1,
              hand={"s": (14, 8), "se": (14, 7), "e": (14, 7), "ne": (14, 7), "n": (14, 8)}).build()
