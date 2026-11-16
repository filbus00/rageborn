# The Riftspawn (2026-11-16): the Rift Caves' swarmer, a small hunched demon fresh out of the Rift: bone horns, wine-dark
# skin, yellow eyes, a mouth of teeth, long claws, a whip of a tail. It swarms, and hurls a shard of the Rift.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E, E, E,
         "...KK......KK...",
         "...KhK.KK.KhK...",
         "....KhKddKhK....",
         "....KddddddK....",
         "....KdEddEdK....",
         "....KddKKddK....",
         "..KKKddddddKKK..",
         ".KdKKdDddDdKKdK.",
         ".KdK.KddddK.KdK.",
         ".KcK.KdDDdK.KcK.",
         "..K..KddddK..K..",
         ".....KdKKdK.....",
         ".....KKKKKK....."]
SIDE = [E, E, E,
        "......KK........",
        ".....KhK........",
        "....KKhKKKK.....",
        "....KdddddddK...",
        "....KdddddEdK...",
        "....KddddKKK....",
        "...KKdddddK.....",
        "..KdKdDdddKKK...",
        ".KdK.KdddddccK..",
        "KdK..KdDddKKK...",
        ".K...KddddK.....",
        ".....KdKdK......",
        ".....KKKKK......"]
BACK = FRONT[:7] + ["....KddddddK....", "....KdDddDdK....", "..KKKddddddKKK..", ".KdKKdDddDdKKdK.", ".KdK.KddddK.KdK.",
                    ".KcK.KdDDdK.KcK.", "..K..KddddK..K..", ".....KdKKdKd....", ".....KKKKKKKd..."]
kit.Character("riftspawn", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"d": "8a3458", "D": "5e2040", "h": "e0d4b8", "c": "e0d4b8", "E": "ffd84a"}, role="melee").build()
