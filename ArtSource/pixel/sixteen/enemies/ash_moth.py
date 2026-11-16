# The Ash Moth (2026-11-16, redrawn the same day): the Burnt Library's flying swarm, a grey moth with a dark furred body,
# scorched wings with an ember eye on each, feathered feelers and two red eyes. It flies and darts in.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E] * 4 + [
         ".....K....K.....",
         ".KKK..K..K..KKK.",
         "KwwwK.KddK.KwwwK",
         "KwWwwKdEEdKwwWwK",
         "KweWwwddddwwWewK",
         "KwwWwKddddKwWwwK",
         ".KwwwKKddKKwwwK.",
         ".KewK.KddK.KweK.",
         "..KK...KK...KK..",
         E, E, E]
SIDE = [E] * 4 + [
        "....KK..........",
        "...KwwK..K.K....",
        "..KwWwwK..K.....",
        "..KwWewwKKdK....",
        "..KwwWwwdddEK...",
        "...KwwwddddKK...",
        "....KKKdddK.....",
        "......KKK.......",
        E, E, E, E]
BACK = FRONT[:7] + ["KwWwwKddddKwwWwK"] + FRONT[8:]
kit.Character("ash_moth", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"w": "6a6670", "W": "a8a2ae", "d": "3a3440", "e": "ff7a2a", "E": "ff3b30"}, role="charger", step_rows=1,
              timing={"run": (0.35, 3.6), "walk": (0.5, 1.0)}).build()
