# The Beacon Thrall (2026-11-16): the Rift Caves' support, a hunched thing in a watchman's grey cloak, one of the
# Watch's beacon baskets chained to its head and burning violet now. Those near its light hit harder; it lobs the fire.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [".....v.Vv.v.....",
         "......vVVv......",
         ".....KiVViK.....",
         "....KiiiiiiK....",
         "...KccccccccK...",
         "...KccqqqqccK...",
         "...KcqEqqEqcK...",
         "....KcqqqqcK....",
         "..KKccciicccKK..",
         ".KccKcciicccKccK",
         ".KcCKcciicccKCcK",
         ".KssKcccccccKssK",
         "..KKKcCccCccKKK.",
         "...KccCccCccK...",
         "...KccccccccK...",
         "...KKKKKKKKKK..."]
SIDE = ["......v.Vv......",
        ".......vVVv.....",
        "......KiVViK....",
        ".....KiiiiiK....",
        "....KcccccccK...",
        "....KccccqqqK...",
        "....KccccqEqK...",
        ".....KcccqqK....",
        "....KccccicK....",
        "...KcccccicsssK.",
        "...KccCccicKKK..",
        "...KccCccccK....",
        "...KccCccccK....",
        "...KccCcccccK...",
        "...KccccccccK...",
        "...KKKKKKKKKK..."]
BACK = FRONT[:5] + ["...KccccccccK...", "...KccCCCCccK...", "....KccccccK....", "..KKccccccccKK..", ".KccKcccccccKccK",
                    ".KcCKcccccccKCcK"] + FRONT[11:]
kit.Character("beacon_thrall", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"c": "5a5a68", "C": "3e3e4a", "q": "1e1826", "E": "c080ff", "i": "8a8e9a", "v": "9a5ae0", "V": "e0b8ff",
               "s": "9a94a4"}, role="melee", step_rows=1).build()
