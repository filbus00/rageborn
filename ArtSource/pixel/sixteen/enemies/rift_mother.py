# The Rift Mother (2026-11-16, drawn new in the 16x16 style): the Rift Caves' first boss (level 4-6), who births what
# climbs out of the Rift: a bloated demon with a crown of bone horns, three yellow eyes over a grin of teeth, a belly
# swollen with violet light, small clawed arms, a skirt of tendrils where legs should be. 26 drawn pixels tall, drawn at
# her VisualScale (1.6) so the game does not stretch her.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit

FRONT = ["....K..............K....",
         "...KhK............KhK...",
         "...KhhK..KKKKKK..KhhK...",
         "....KhhKKddddddKKhhK....",
         ".....KhKdddEEdddKhK.....",
         "......KdEEddddEEdK......",
         "......KddddddddddK......",
         "......KdKfKffKfKdK......",
         "....KKKddddddddddKKK....",
         "...KdddKddddddddKdddK...",
         "..KdDddKdvvvvvvdKddDdK..",
         ".KdDddKddvVVVVvddKddDdK.",
         ".KdddKdddvVvvVvdddKdddK.",
         ".KdddKdddvVVVVvdddKdddK.",
         ".KcdcKddddvvvvddddKcdcK.",
         "..KKKKdDddddddddDdKKKK..",
         ".....KdDddddddddDdK.....",
         "....KddDddddddddDddK....",
         "....KdddDddddddDdddK....",
         "...KddddddDddDddddddK...",
         "...KdDddddddddddddDdK...",
         "..KddDddKddddddKddDddK..",
         "..KdddKKdKddddKdKKdddK..",
         ".KdtdK.KtK.KK.KtK.KdtdK.",
         ".KtK...KK......KK...KtK.",
         "..K..................K.."]
SIDE = ["..........K.............",
        ".........KhK............",
        ".........KhhKKKKK.......",
        "........KKhKdddddK......",
        ".......KdddddddEEK......",
        ".......KddddddddEdK.....",
        ".......KddddddddddK.....",
        ".......KdddddKfKfK......",
        "......KKdddddddKK.......",
        ".....KddddddddddK.......",
        "....KdDdddddvvvvdK......",
        "...KdDddddddvVVVvdKKK...",
        "...KddddddddvVvVvdddcK..",
        "...KddddddddvVVVvKKKK...",
        "...KdDdddddddvvvdK......",
        "...KdDddddddddddK.......",
        "...KddDdddddddddK.......",
        "...KdddDddddddddK.......",
        "..KddddddDdddddddK......",
        "..KdDdddddddddddDK......",
        "..KddDddddddddddDdK.....",
        ".KdddDdKdddddKddddK.....",
        ".KdddKKdKdddKdKKddK.....",
        ".KdtK.KtK.K.KtK.KtK.....",
        ".KtK..KK......K..K......",
        "..K....................."]
BACK = FRONT[:4] + [".....KhKddddddddKhK.....", "......KddddddddddK......", "......KddDddddDddK......", "......KddddddddddK......"] + FRONT[8:10] + [
        "..KdDddKddddddddKddDdK..", ".KdDddKdddDddDdddKddDdK.", ".KdddKddddDddDddddKdddK.", ".KdddKdddddDDdddddKdddK.",
        ".KcdcKddddddddddddKcdcK."] + FRONT[15:]
kit.Character("rift_mother", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"d": "8a3458", "D": "5e2040", "h": "e0d4b8", "f": "f0e8d8", "c": "e0d4b8", "E": "ffd84a", "v": "9a5ae0",
               "V": "e0b8ff", "t": "5e2040"}, role="melee", baked_scale=1.6, step_rows=2, margin=(8, 10, 8, 2), glow="V",
              timing={"run": (0.55, 3.0), "walk": (1.1, 1.0)}).build()
