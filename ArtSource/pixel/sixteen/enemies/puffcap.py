# The Puffcap (2026-11-07): the Rotcap kind, a red-spotted puffball cap waddling on stubby feet; it bursts.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
CAP = [E, E, E, E, E,
       ".....KKKKKK.....",
       "...KKrrwrrrKK...",
       "..KrrrrrrwrrrK..",
       ".KrwrrrrrrrrwrK.",
       ".KrrrrwrrrrrrrK.",
       ".KKrrrrrrrwrrKK.",
       "..KKKKKKKKKKKK.."]
FRONT = CAP + ["...KcEccccEcK...", "...KccKKKKccK...", "....KccccccK....", "....KcK..KcK...."]
SIDE = CAP + ["...KcccccEcK....", "...KcccccKKK....", "....KccccccK....", "....KcK..KcK...."]
BACK = CAP + ["...KccccccccK...", "...KccccccccK...", "....KccccccK....", "....KcK..KcK...."]
kit.Character("puffcap", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": BACK, "n": BACK},
              {"r": "e0402a", "w": "fff2e0", "c": "f0dcb0", "E": "2a1018"}, role="swarm", step_rows=1).build()
