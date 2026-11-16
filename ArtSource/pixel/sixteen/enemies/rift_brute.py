# The Rift Brute (2026-11-16): the Rift Caves' brute, a hulking demon: a small horned head sunk between huge shoulders,
# violet light in the cracks of its chest, fists crusted in the cave's own rock. It slams, and pounds the ground.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E,
         "...K........K...",
         "..KhK.KKKK.KhK..",
         "..KhhKddddKhhK..",
         "...KKdEddEdKK...",
         ".KKKdddKKdddKKK.",
         "KdddKddddddKdddK",
         "KdDdKdvddvdKdDdK",
         "KdddKddvvddKdddK",
         "KdddKdddvddKdddK",
         "KrrrKddddddKrrrK",
         "KrvrKKddddKKrvrK",
         "KrrrK.KddK.KrrrK",
         ".KKK.KddddK.KKK.",
         ".....KdKKdK.....",
         ".....KKKKKK....."]
SIDE = [E,
        "........K.......",
        ".......KhK......",
        "......KhKKKK....",
        ".....KKdddEdK...",
        "....KdddddKKK...",
        "...KdddddddK....",
        "...KdDddvddKKKK.",
        "...KddddvvdddrrK",
        "...KdDdddvKKrvrK",
        "...KddddddK.KrrK",
        "...KdDddddK..KK.",
        "....KdddddK.....",
        "....KddKddK.....",
        "....KdKKdK......",
        "....KKKKKK......"]
BACK = FRONT[:4] + ["...KKddddddKK...", ".KKKddddddddKKK.", "KdddKddddddKdddK", "KdDdKddDDddKdDdK", "KdddKddDDddKdddK",
                    "KdddKddddddKdddK"] + FRONT[10:]
kit.Character("rift_brute", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"d": "7a3050", "D": "521e3a", "h": "e0d4b8", "E": "ffd84a", "v": "c080ff", "r": "5a5468"}, role="melee",
              timing={"walk": (1.1, 1.0)}).build()
