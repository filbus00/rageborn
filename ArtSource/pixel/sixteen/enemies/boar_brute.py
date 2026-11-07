# The Boar Brute (2026-11-07): the Woodcutter kind, a hulking axeman in a boar's skull with tusks, bare shoulders, a
# leather vest, a great axe (seen from the side). He throws the axe as before.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = ["....K......K....",
         "...KmK....KmK...",
         "...KmmKKKKmmK...",
         "..KmmmmmmmmmmK..",
         "..KmEmmmmmmEmK..",
         "..KmmmKmmKmmmK..",
         ".KtKmmmmmmmmKtK.",
         ".KtKKmmmmmmKKtK.",
         "KssKKKKKKKKKKssK",
         "KsssKhhhhhhKsssK",
         "KssKhhhhhhhhKssK",
         ".KsKhhhhhhhhKsK.",
         ".KsKhhllllhhKsK.",
         "..KKhhhKKhhhKK..",
         "...KpppK.KpppK..",
         "...KKKKK.KKKKK.."]
SIDE = ["......K.........",
        ".....KmK........",
        "....KmmmKKK.....",
        "...KmmmmmmmK....",
        "...KmmmmmEmmK...",
        "...KmmmmmmmmmK..",
        "...KmmmmmmKmmK..",
        "....KmmmmmmtK...",
        "..KKssKKKKKtK.aa",
        ".KssssKhhhKsKaaK",
        ".KssssKhhhKssWa.",
        ".KsssKhhhhhKsW..",
        "..KsKhhllhhKWK..",
        "...KhhhhhhhK....",
        "...KpppKpppK....",
        "...KKKKKKKKK...."]
BACK = FRONT[:4] + ["..KmmmmmmmmmmK..", "..KmmmmmmmmmmK..", ".KsKmmmmmmmmKsK.", ".KsKKmmmmmmKKsK."] + FRONT[8:]
kit.Character("boar_brute", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"m": "e8dcc8", "E": "ff3b30", "t": "fffaf0", "s": "c8845a", "h": "6a4630", "l": "3a2a20", "p": "5a4a6a",
               "a": "c8d0d8", "W": "7a4e2a"}, role="melee").build()
