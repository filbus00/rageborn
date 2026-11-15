# The Cell Rat (2026-11-14): the Iron Gaol's swarm, a fat grey rat with a pink tail and nose and red eyes.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

SIDE = [E] * 10 + [
        "..........K.K...",
        ".....KKKKKgKgK..",
        "...KKgggggggEgK.",
        "pKKdgggggggggpK.",
        "..pKgKgK.KgKgK..",
        "....K.K...K.K..."]
FRONT = [E] * 10 + [
         ".....K....K.....",
         "....KgK..KgK....",
         "....KggKKggK....",
         "....KgEggEgK....",
         "....KggppggK....",
         ".....KKKKKK....."]
BACK = FRONT[:13] + ["....KggggggK....", "....KgggggpK....", ".....KKKKKKp...."]
kit.Character("cell_rat", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"g": "74645a", "d": "463a3c", "p": "e89aa0", "E": "ff3b30"}, role="swarm", step_rows=1,
              timing={"run": (0.35, 3.6)}).build()
