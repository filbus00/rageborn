# The Gloomwing (2026-11-16): the Rift Caves' flying swarm, a bat of the deep: dark wings with a violet membrane, a
# furred body, two pale eyes and fangs. It flies and darts in.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E] * 5 + [
         ".....K....K.....",
         ".KK..KKKKKK..KK.",
         "KwwK.KbbbbK.KwwK",
         "KwvwKbEbbEbKwvwK",
         "KwvvwbbbbbbwvvwK",
         ".KwwwKbffbKwwwK.",
         "..KwK.KbbK.KwK..",
         "...K...KK...K...",
         E, E, E]
SIDE = [E] * 5 + [
        "....KK..........",
        "...KwwK...K.K...",
        "..KwvwwK.KbKbK..",
        "..KwvvwwKbbbEK..",
        ".KwwvwwbbbbbfK..",
        "..KKwwKbbbbKK...",
        "....KK.KKKK.....",
        E, E, E, E]
BACK = FRONT[:8] + ["KwvwKbbbbbbKwvwK"] + FRONT[9:10] + [".KwwwKbbbbKwwwK."] + FRONT[11:]
kit.Character("gloomwing", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"w": "342c44", "v": "7a4ab8", "b": "4a3e54", "f": "f0e8d8", "E": "f0e060"}, role="charger", step_rows=1,
              timing={"run": (0.35, 3.6), "walk": (0.5, 1.0)}).build()
