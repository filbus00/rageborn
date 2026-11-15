# The Rack Brute (2026-11-14): the Iron Gaol's torturer, a huge bare-chested man in a black hood with red eye holes,
# leather straps crossed on his belly, iron cuffs, a mallet. He slams the ground and swings his chain round himself.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

FRONT = [E,
         ".....KKKKKK.....",
         "....KhhhhhhK....",
         "...KhhhhhhhhK...",
         "...KhEhhhhEhK...",
         "...KhhhhhhhhK...",
         "..KKKhhhhhhKKK..",
         ".KssKKhhhhKKssK.",
         "KssssKllllKssssK",
         "KssKlsssssslKssK",
         "KssKslsssslsKssK",
         ".KsKsslSSlssKsK.",
         ".KmKllllllllKmK.",
         "..KKppppppppKK..",
         "...KpppK.KpppK..",
         "...KKKKK.KKKKK.."]
SIDE = [E,
        "......KKKKK.....",
        ".....KhhhhhK....",
        "....KhhhhhhhK...",
        "....KhhhhhEhK...",
        "....KhhhhhhhK...",
        "....KKhhhhhK....",
        "...KssKKhhK.....",
        "..KsssslKKKK....",
        "..KssslssssKKKmm",
        "..KsslssKssssKmm",
        "..KslsssKKKKKKmm",
        "..KlllllllK.....",
        "...KpppppK......",
        "...KppKppK......",
        "...KKKKKKK......"]
BACK = FRONT[:4] + ["...KhhhhhhhhK..."] + FRONT[5:]
kit.Character("rack_brute", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"h": "2c2832", "s": "c8a088", "S": "96705c", "l": "5c3a28", "E": "ff5a3a", "p": "42384c", "m": "a0a8b2"},
              role="melee", timing={"walk": (1.0, 1.0)}).build()
