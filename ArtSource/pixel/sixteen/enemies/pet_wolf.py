# Replaced on 2026-11-08 by ../wolf_16.py, which writes the same pet_wolf sheets: running this would bring the old wolf back.
# The pet wolf (2026-11-07): grey with a darker back, a bushy tail, a pale chest and muzzle, a red harness; the same
# 16x16 build as the Ash Jackal, a little heavier. Replaces the baked fox-wolf (Import Drawn Characters).
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
SIDE = [E, E, E, E,
        "...........KK.K.",
        "..........KbKbK.",
        "..........KbbbbK",
        "KKK......KbbbEbK",
        "KdbK.KKKKrrbbbbwK"[:16],
        "KddbKdddrrbbbbwK",
        ".KdbbdddrrbbbwK.",
        "..KbbbbbbrbbwwK.",
        "...KbKbKKKbKbK..",
        "...KbKbK.KbKbK..",
        "...KbKbK.KbKbK..",
        "...KdKdK.KdKdK.."]
FRONT = [E, E, E, E,
         ".....K....K.....",
         "....KbK..KbK....",
         "....KbbKKbbK....",
         "...KbbbbbbbbK...",
         "...KbEbbbbEbK...",
         "...KbbbwwbbbK...",
         "....KbwKKwbK....",
         "...KrrrrrrrrK...",
         "...KbbwwwwbbK...",
         "....KbKwwKbK....",
         "....KbK..KbK....",
         "....KdK..KdK...."]
BACK = [E, E, E, E,
        ".....K....K.....",
        "....KbK..KbK....",
        "....KbbKKbbK....",
        "...KbbbbbbbbK...",
        "...KbbbbbbbbK...",
        "...KrrrrrrrrK...",
        "...KbbbddbbbK...",
        "...KbbddddbbK...",
        "....KbddddbK....",
        "....KbKddKbK....",
        "....KbK..KbK....",
        "....KdK..KdK...."]
kit.Character("pet_wolf", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "a4a8b0", "d": "5e6470", "w": "e8eaee", "E": "ffd23f", "r": "d0302a"}, role="swarm",
              timing={"walk": (0.9, 1.4), "run": (0.4, 3.6)}).build()
