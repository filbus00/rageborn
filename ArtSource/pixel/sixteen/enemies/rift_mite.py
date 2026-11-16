# The Rift Mite (2026-11-16): the Rift Caves' swarm, a flat grey beetle with a shard of violet crystal grown out of its
# back and one red eye.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
SIDE = [E] * 10 + [
        "......v.........",
        ".....KvK.v......",
        "...KKvvvKvK.....",
        "..KmmmvvmmmKKK..",
        ".KmmMmmmmMmmEmK.",
        ".K.K.K..K.K.KK.."]
FRONT = [E] * 10 + [
         ".......v........",
         "......KvK.......",
         "....KKvvvKK.....",
         "...KmmmvvmmK....",
         "...KmEmmmEmK....",
         "...KK.KKK.KK...."]
BACK = FRONT[:14] + ["...KmmMmMmmK....", "...KK.KKK.KK...."]
kit.Character("rift_mite", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"m": "5a5468", "M": "3a3446", "v": "c080ff", "E": "ff3b30"}, role="swarm", step_rows=1,
              timing={"run": (0.35, 3.6)}).build()
