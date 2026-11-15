# The Iron Maiden (2026-11-14): the Iron Gaol's walking coffin, a riveted iron casket with a face plate, two red eyes
# behind its slits, gold bands, spikes at its seam, small iron feet. Slow and armoured; it shoots its spikes.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

FRONT = ["......KKKK......",
         ".....KIiiiK.....",
         "....KIiiiidK....",
         "...KIiEiiEidK...",
         "...KIiiiiiidK...",
         "...KIidKKdidK...",
         "..KggggggggggK..",
         "..KIiiiKKiiidK..",
         "..KIiriKKiridK..",
         "..KIiiiKKiiidK..",
         "..KIiiiKKiiidK..",
         "..KIiriKKiridK..",
         "..KggggggggggK..",
         "..KKiiiiiiiiKK..",
         "...KffK..KffK...",
         "...KKKK..KKKK..."]
SIDE = ["......KKKK......",
        ".....KIiiiK.....",
        "....KIiiiiiK....",
        "....KIiiiEidK...",
        "....KIiiiiidK...",
        "....KIiiidKdK...",
        "...KggggggggK...",
        "...KIiiiiiidK...",
        "...KIiiriiidK...",
        "...KIiiiiiidKr..",
        "...KIiiiiiidK...",
        "...KIiiriiidKr..",
        "...KggggggggK...",
        "...KKiiiiiiKK...",
        "....KffKKffK....",
        "....KKKKKKKK...."]
BACK = FRONT[:3] + ["...KIiiiiiidK...", "...KIiiiiiidK...", "...KIiiiiiidK..."] + FRONT[6:]
kit.Character("iron_maiden", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"i": "5c6472", "I": "8e98a8", "d": "3a404c", "r": "d0d8e0", "E": "ff3b30", "g": "b8902e", "f": "2e2e38"},
              role="melee", step_rows=1, timing={"walk": (1.1, 1.0)}).build()
