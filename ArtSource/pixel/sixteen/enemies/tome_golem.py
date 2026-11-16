# The Tome Golem (2026-11-16, redrawn the same day): the Burnt Library's brute, a body of bound books: a great clasped
# tome with gold corners for a chest, a smaller one for a head with two ember eyes, arms of stacked volumes. It slams,
# and throws pages.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E,
         ".....KKKKKK.....",
         "....KbbbbbbK....",
         "....KbEbbEbK....",
         "....KppppppK....",
         "..KKKKKKKKKKKK..",
         ".KbbKgbbbbbgKbbK",
         ".KppKbbbbbbbKppK",
         ".KuuKbbbggbbKrrK",
         ".KppKbbbggbbKppK",
         ".KrrKgbbbbbgKuuK",
         ".KppKKKKKKKKKppK",
         "..KKKppppppKKK..",
         "...KbbbKKbbbK...",
         "...KBBBKKBBBK...",
         "...KKKKKKKKKK..."]
SIDE = [E,
        "......KKKKK.....",
        ".....KbbbbbK....",
        ".....KbbbEbK....",
        ".....KpppppK....",
        "....KKKKKKKK....",
        "...KgbbbbbgK....",
        "...KbbKKKKbKKK..",
        "...KbbKuupprrK..",
        "...KbbKKKKKKKK..",
        "...KgbbbbbgK....",
        "...KKKKKKKKK....",
        "....KpppppK.....",
        "....KbbKbbK.....",
        "....KBBKBBK.....",
        "....KKKKKKK....."]
BACK = FRONT[:3] + ["....KbBBBBbK...."] + FRONT[4:6] + [".KbbKbbbbbbbKbbK", ".KppKgggggggKppK", ".KuuKbbbbbbbKrrK", ".KppKgggggggKppK",
                                                ".KrrKbbbbbbbKuuK"] + FRONT[11:]
kit.Character("tome_golem", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "7e4c30", "B": "523020", "p": "e0d4b0", "g": "e0b840", "r": "8e2e28", "u": "3e5088", "E": "ff7a2a"},
              role="melee", timing={"walk": (1.1, 1.0)}).build()
