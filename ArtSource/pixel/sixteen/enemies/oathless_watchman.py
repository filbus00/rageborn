# The Oathless Watchman (2026-11-16): the Rift Caves' soldier, one of the old Watch that guarded the Rift, turned: a
# rusted kettle helm over a face gone dark, two violet lights for eyes, a tattered grey-blue tabard with the Watch's
# flame on it, a spear. He strikes twice and backs off, and throws his spear.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = ["..............i.",
         ".....KKKK....KiK",
         "...KKmmmmKK..KiK",
         "..KMMMMMMMMK..w.",
         "...KqqEqqEqK..w.",
         "...KqqqqqqqK..w.",
         "....KqqqqqK...w.",
         "..KKttttttttKKw.",
         ".KccKttyyttKcsw.",
         ".KccKttyyttKKKw.",
         ".KccKtttyttK..w.",
         ".KssKbbbbbbK..w.",
         "..KKKttttttK..w.",
         "....KllKKllK..w.",
         "....KllKKllK..w.",
         "....KKKKKKKK..w."]
SIDE = ["................",
        ".......KKKK.....",
        ".....KKmmmmK....",
        "....KMMMMMMMMK..",
        ".....KqqqqEqK...",
        ".....KqqqqqqK...",
        "......KqqqqK....",
        ".....KttttK.....",
        "....KtttttK.....",
        "....KttytsssK...",
        "..wwwwwwwwwwwiiK",
        "....KtttKKKK....",
        "....KbbbbK......",
        "....KllKllK.....",
        "....KllKllK.....",
        "....KKKKKKK....."]
BACK = FRONT[:4] + ["...KmmmmmmmK..w.", "...KmmmmmmmK..w.", "....KmmmmmK...w.", FRONT[7], ".KccKttttttKcsw.",
                    ".KccKttttttKKKw.", ".KccKttttttK..w."] + FRONT[11:]
kit.Character("oathless_watchman", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"m": "8a7a6a", "M": "5e5048", "q": "1e1826", "E": "c080ff", "t": "56627a", "y": "d8a040", "c": "707888",
               "s": "9a94a4", "b": "3a2e28", "l": "34303e", "w": "7a5a36", "i": "b8c0cc"}, role="melee").build()
