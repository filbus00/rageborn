# The Wound Seer (2026-11-16): the Rift Caves' caster, a priest of the Rift in a horned cowl of dark violet: where a face
# should be, a wound of light; a staff with a shard of the Rift at its head. It calls three orbs of the void.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = ["...K........K...",
         "...KK......KK..v",
         "...KpKKKKKKpK.vV",
         "...KppppppppK.vv",
         "...KppqqqqppK..w",
         "...KpqqvvqqpK..w",
         "...KpqqvvqqpK..w",
         "...KKpqvvqpKK..w",
         "..KKppppppppKK.w",
         ".KppKppppppKppsw",
         ".KppKpPppPpKKKKw",
         ".KssKccccccK...w",
         "..KKKpPppPpK...w",
         "...KppPppPppK..w",
         "...KppppppppK..w",
         "...KKKKKKKKKK..w"]
SIDE = ["......K.........",
        "......KK......v.",
        "......KpKKKK.vV.",
        ".....KpppppK.vv.",
        "....KpppppqqK.w.",
        "....KppppqqvK.w.",
        "....KppppqqvK.w.",
        "....KKpppqqK..w.",
        "....KpppppK...w.",
        "...KppPppppsssw.",
        "...KppPpKKKKKKw.",
        "...KppPpcccK..w.",
        "...KppPppppK..w.",
        "...KppPppppK..w.",
        "...KpppppppK..w.",
        "...KKKKKKKKK..w."]
BACK = FRONT[:4] + ["...KppppppppK..w", "...KppPPPPppK..w", "...KppppppppK..w", "...KKppppppKK..w"] + FRONT[8:11] + [".KssKppppppK...w"] + FRONT[12:]
kit.Character("wound_seer", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"p": "4a2e6a", "P": "30204a", "q": "140f1c", "v": "c080ff", "V": "f0d8ff", "s": "9a94a4", "c": "8a6a3a",
               "w": "5a4636"}, role="caster", glow="V", step_rows=1,
              hand={"s": (14, 2), "se": (13, 2), "e": (13, 2), "ne": (13, 2), "n": (14, 2)}).build()
