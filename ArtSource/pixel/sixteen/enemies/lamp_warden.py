# The Lamp Warden (2026-11-14): the Iron Gaol's support, an old warder in a grey hooded coat with a white beard,
# carrying a lit lamp on a pole. Those near his light hit harder; he throws a lamp of oil with a fuse.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

FRONT = [".............KK.",
         ".....KKKKKK.KLLK",
         "....KccccccKKyYK",
         "...KccccccccKyyK",
         "...KccssssccKLLK",
         "...KcsEssEscK.p.",
         "...KcswwwwscK.p.",
         "....KwwwwwwK..p.",
         "..KKcwwwwwwcKKp.",
         ".KccKccwwccKcsp.",
         ".KccKcCccCcKKKp.",
         ".KssKcCccCcK..p.",
         "..KKKcCccCcK..p.",
         "...KccCccCccK.p.",
         "...KccccccccK.p.",
         "...KKKKKKKKKK.K."]
SIDE = ["............KK..",
        "......KKKKKKLLK.",
        ".....KcccccKyYK.",
        "....KccccccKyyK.",
        "....KcccsssKLLK.",
        "....KcccsEsK.p..",
        "....KccwwwwK.p..",
        ".....KcwwwK..p..",
        "....KccwwcKK.p..",
        "...KcccccccsKp..",
        "...KcCccccKK.p..",
        "...KcCcccccK.p..",
        "...KcCcccccK.p..",
        "...KcCcccccK.p..",
        "...KcccccccK.p..",
        "...KKKKKKKKK.K.."]
BACK = FRONT[:4] + ["...KccccccccKLLK", "...KccccccccK.p.", "...KccCCCCccK.p.", "....KccccccK..p.", "..KKccccccccKKp.",
                    ".KccKccccccKcsp."] + FRONT[10:]
kit.Character("lamp_warden", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"c": "5c6272", "C": "3e4450", "s": "d8b090", "w": "ece8dc", "E": "241c28", "p": "7c5c36", "L": "4a4a54",
               "y": "ffe060", "Y": "fff6b0"}, role="melee", step_rows=1).build()
