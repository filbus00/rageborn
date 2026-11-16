# The Voidspitter (2026-11-16): the Rift Caves' marksman, a squat toad of a demon: eyes on stalks, a wide mouth, a throat
# sac swollen with violet light. It spits three bolts of the void.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E] * 4 + [
         "....KK....KK....",
         "...KEEKKKKEEK...",
         "...KggggggggK...",
         "..KggggggggggK..",
         "..KgKKKKKKKKgK..",
         "..KgKvvvvvvKgK..",
         ".KKggKKKKKKggKK.",
         "KggKgvvvvvvgKggK",
         "KggKgvVVVVvgKggK",
         ".KKKggvvvvggKKK.",
         "..KggKKKKKKggK..",
         "..KKKK....KKKK.."]
SIDE = [E] * 4 + [
        ".......KK.......",
        "......KEEK......",
        "....KKggggKK....",
        "...KgggggggKKK..",
        "...KggggggKvvK..",
        "...KgggggggKKK..",
        "..KggggvvvvK....",
        "..KgggvVVVvK....",
        "..KggggvvvgK....",
        "..KgKgggggKgK...",
        "..KgK.KKK.KgK...",
        "..KKK.....KKK..."]
BACK = FRONT[:5] + ["...KggKKKKggK...", FRONT[6], FRONT[7], "..KggGggggGggK..", "..KggggggggggK..", ".KKggGggggGggKK.",
                    "KggKggggggggKggK", "KggKggGggGggKggK", ".KKKggggggggKKK."] + FRONT[14:]
kit.Character("voidspitter", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"g": "4e6a5a", "G": "34483e", "v": "9a5ae0", "V": "e0b8ff", "E": "f0e060"}, role="archer", step_rows=1, glow="V",
              hand={"s": (8, 9), "se": (12, 8), "e": (12, 8), "ne": (12, 8), "n": (8, 9)}).build()
