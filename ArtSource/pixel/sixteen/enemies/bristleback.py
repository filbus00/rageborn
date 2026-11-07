# The Bristleback (2026-11-07): the TuskedBoar kind, a rust-red boar with a dark bristle ridge and white tusks; it charges.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
SIDE = [E, E, E, E, E,
        "..KKKK..........",
        ".KBKBKKKK.......",
        "KbbbbbbbbKKK....",
        "KbbbbbbbbbbbKK..",
        "KbbbbbbbbbbEbbK.",
        "KbbbbbbbbbbbbbsK",
        "KbbbbbbbbbbbtbsK",
        ".KbbbbbbbbbbKtK.",
        ".KbKbK..KbKbKK..",
        ".KbKbK..KbKbK...",
        ".KKKKK..KKKKK..."]
FRONT = [E, E, E, E, E, E,
         "....KK....KK....",
         "...KbbKKKKbbK...",
         "..KbbbbbbbbbbK..",
         "..KbEbbbbbbEbK..",
         "..KbbbbbbbbbbK..",
         ".KtKbbssssbbKtK.",
         ".KtKbsKssKsbKtK.",
         "..KKbbbbbbbbKK..",
         "...KbK....KbK...",
         "...KKK....KKK..."]
BACK = [E, E, E, E, E, E,
        "....KK....KK....",
        "...KbbKKKKbbK...",
        "..KbbBBBBBBbbK..",
        "..KbbbBBBBbbbK..",
        "..KbbbbbbbbbbK..",
        "..KbbbbbbbbbbK..",
        "..KbbbbKKbbbbK..",
        "..KKbbbKKbbbKK..",
        "...KbK....KbK...",
        "...KKK....KKK..."]
kit.Character("bristleback", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "b0582e", "B": "5a2414", "E": "ffdd55", "s": "e8a088", "t": "fff8e8"}, role="charger",
              timing={"run": (0.4, 3.4)}).build()
