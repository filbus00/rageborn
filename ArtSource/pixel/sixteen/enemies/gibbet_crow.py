# The Gibbet Crow (2026-11-14): the Iron Gaol's flying swarm, a black crow off the gibbets with a bone beak, a red
# eye and a frayed end of noose on its leg. It flies and darts in.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

FRONT = [E] * 5 + [
         "......KKKK......",
         ".....KkkkkK.....",
         ".KK..KEkkEK..KK.",
         "KkkKKkkyykkKKkkK",
         "KkbkkkkyykkkkbkK",
         ".KkbkkkkkkkkbkK.",
         "..KKkbkkkkbkKK..",
         "....KKkkkkKK....",
         ".....KyK.KyK....",
         "......r.........",
         E]
SIDE = [E] * 6 + [
        "......KK........",
        "...KKKkkK..KKK..",
        "..KkbkkkKKKkkkK.",
        ".KkbbkkkkkkkEkyy",
        "KkkbkkkkkkkkkKK.",
        ".KKKkkkkkkkkK...",
        "....KKkkkKKK....",
        "......KyK.......",
        "......r.........",
        E]
BACK = FRONT[:7] + [".KK..KkkkkK..KK.", "KkkKKkkkkkkKKkkK", "KkbkkkkbbkkkkbkK"] + FRONT[10:]
kit.Character("gibbet_crow", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"k": "2c2c3a", "b": "4a587a", "y": "dccc94", "E": "ff3b30", "r": "b89a60"}, role="charger", step_rows=1,
              timing={"run": (0.35, 3.6), "walk": (0.5, 1.0)}).build()
