# The Rift Stalker (2026-11-16): the Rift Caves' hound, a lean hairless hunter out of the Rift: violet spines down its
# back, a long snout of teeth, pale claws, a whip tail. It lunges. A beast shows its side in the diagonal views.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
SIDE = [E] * 5 + [
        "......v.v..KK.K.",
        ".....KvKvKKddKdK",
        "K...KKdKdKddddEK",
        "KdK.KdddddddddwK",
        ".KdKdddDddDddwwK",
        ".KddddddddddKKK.",
        "..KddDdddDddK...",
        "..KddKKKKdddK...",
        "..KdKdK..KdKdK..",
        "..KdKdK..KdKdK..",
        "..KcKcK..KcKcK.."]
FRONT = [E] * 5 + [
         "....KK....KK....",
         "...KddKKKKddK...",
         "...KddddddddK...",
         "...KdEddddEdK...",
         "...KddwwwwddK...",
         "..KKdwwKKwwdKK..",
         "..KddDddddDddK..",
         "..KddddddddddK..",
         "...KddKddKddK...",
         "...KddK..KddK...",
         "...KccK..KccK..."]
BACK = FRONT[:5] + ["....KK.v..KK....", "...KddKvKKddK...", "...KdddvddddK...", "...KddddvdddK...", "...KdddvddddK...",
                    "..KKddddvdddKK..", "..KddDdvddDddK.."] + FRONT[12:]
kit.Character("rift_stalker", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"d": "5a3a5e", "D": "3c2442", "v": "c080ff", "w": "e8e0d0", "c": "e0d4b8", "E": "ffd84a"}, role="charger",
              timing={"run": (0.4, 3.6)}).build()
