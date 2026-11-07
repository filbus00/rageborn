# The Ash Jackal (2026-11-07): the Wolf kind's swarm, lean and sand-coloured, a dark stripe down its back, tall ears,
# ember eyes. A beast shows its side in the diagonal views. Run: Blender -b -P ArtSource/pixel/sixteen/enemies/ash_jackal.py
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit

E = "................"
SIDE = [E, E, E, E,
        "............K.K.",
        "...........KbKbK",
        "..........KbbbbK",
        "KK.......KbbbEbK",
        "KbK.KKKKKbbbbbbK",
        "KdbKbbbbbbbbbbK.",
        ".KdbbdddbbbbwK..",
        "..KbbbbbbbbbwK..",
        "...KbKbK.KbKbK..",
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
         "...KdbbwwbbdK...",
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
        "...KdbbddbbdK...",
        "...KbbbddbbbK...",
        "...KbbbddbbbK...",
        "....KbbddbbK....",
        "....KbKddKbK....",
        "....KbK..KbK....",
        "....KdK..KdK...."]

kit.Character("ash_jackal", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "c9b48a", "d": "7a6a4e", "w": "efe4c8", "E": "ff6a2a"}, role="charger",
              timing={"run": (0.4, 3.6)}).build()
