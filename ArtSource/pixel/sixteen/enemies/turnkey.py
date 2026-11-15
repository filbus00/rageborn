# The Turnkey (2026-11-14): the Iron Gaol's warder, a stout gaoler in a dark leather hood and jerkin, a ring of brass
# keys at his belt, a cudgel in his fist. He strikes twice and backs off, and throws his key ring, which bounces.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

FRONT = [E,
         ".....KKKKKK.....",
         "....KhhhhhhK....",
         "...KhhhhhhhhK...",
         "...KhsssssshK...",
         "...KhsEssEshK...",
         "...KKssssssKK...",
         "....KKssssKK....",
         "..KKjjjjjjjjKK..",
         ".KjjKjjjjjjKjjK.",
         ".KjjKjjjjjjKjjK.",
         ".KjjKJJJJJJKjjK.",
         ".KssKgbbgggKssK.",
         "..KKKggKKggKKK..",
         "....KggK.KggK...",
         "....KKKK.KKKK..."]
SIDE = [E,
        "......KKKKK.....",
        ".....KhhhhhK....",
        "....KhhhhhhhK...",
        "....KhhhssssK...",
        "....KhhhsEssK...",
        "....KKhsssssK...",
        ".....KKssssK....",
        "....KjjjjjK...w.",
        "...KjjjjjjjK.Kw.",
        "...KjjjjjsssKwK.",
        "...KjJJJJKKKKw..",
        "...KjbbggK......",
        "....KgggggK.....",
        "....KggKggK.....",
        "....KKKKKKK....."]
BACK = FRONT[:4] + ["...KhhhhhhhhK...", "...KhhhhhhhhK...", "...KKhhhhhhKK...", "....KKhhhhKK...."] + FRONT[8:12] + [".KssKggggggKssK."] + FRONT[13:]
kit.Character("turnkey", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"h": "3c3644", "s": "d4a47c", "E": "241c28", "j": "6a5440", "J": "3e3026", "b": "f0c840", "g": "4c4c5c",
               "w": "9a6a38"}, role="melee").build()
