# The Stumpling (2026-11-07): the BriarThrall kind, a walking tree stump, amber eyes in dark hollows, orange fungus
# shelves, twigs on top, roots for feet. Slow and tough; its roots grasp her as before.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = ["...K.......K....",
         "..KbK..K..KbK...",
         "...KbKKbKKbK....",
         "..KrrrrrrrrrrK..",
         "..KrRrrrrrRrrK..",
         "..KbbbbbbbbbbK..",
         "..KbBbbbbbbBbK..",
         "..KbKKKbbKKKbK..",
         "..KbKEKbbKEKbK..",
         ".KbbbbbbbbbbbbK.",
         "KfKbBbbKKbbBbKfK",
         "KfKbbbbKKbbbbKfK",
         ".KKbBbbbbbbBbKK.",
         "..KbbbKbbKbbbK..",
         ".KbKbKK..KKbKbK.",
         "Kb.K.K....K.K.bK"]
BACK = [r.replace("E", "b") for r in FRONT[:7]] + ["..KbBbbbbbbBbK..", "..KbbbbbbbbbbK.."] + FRONT[9:10] +        ["KfKbBbbbbbbBbKfK", "KfKbbbbbbbbbbKfK"] + FRONT[12:]
kit.Character("stumpling", {"s": FRONT, "se": FRONT, "e": FRONT, "ne": BACK, "n": BACK},
              {"b": "9a6438", "B": "5a3a22", "r": "e0b878", "R": "b88a50", "E": "ffb030", "f": "ff7a2a"},
              role="melee", step_rows=2, timing={"walk": (1.2, 0.8), "run": (0.7, 1.8)}).build()
