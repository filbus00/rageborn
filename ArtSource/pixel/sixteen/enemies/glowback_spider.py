# The Glowback Spider (2026-11-07): the Spider kind's swarm, violet with a glowing yellow abdomen and red eyes.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E, E, E, E, E, E, E,
         "......KKKK......",
         ".....KyyyyK.....",
         "..K.KyYyyYyK.K..",
         ".KvKKyyyyyyKKvK.",
         "Kv.KvKKyyKKvK.vK",
         "K.KvvvvvvvvvvK.K",
         ".KvKvEvvvvEvKvK.",
         "Kv.KKvvvvvvKK.vK",
         "K..K.KKKKKK.K..K"]
SIDE = [E, E, E, E, E, E, E, E,
        "..KKKK..........",
        ".KyyyyK.........",
        "KyYyyYyKKKKK....",
        "KyyyyyyKvvvvK...",
        ".KyyyyKvvvvEvK..",
        "..KKKKvKvKvKvK..",
        ".KvK.KvK.KvKvK..",
        "Kv..Kv..Kv..Kv.."]
BACK = [r.replace("E", "v") for r in FRONT]
kit.Character("glowback_spider", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"y": "ffd84a", "Y": "c08a1a", "v": "7a46b0", "E": "ff3b30"}, role="swarm", timing={"run": (0.36, 3.4)}).build()
