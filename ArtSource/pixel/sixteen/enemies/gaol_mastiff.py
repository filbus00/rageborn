# The Gaol Mastiff (2026-11-14): the Iron Gaol's hound, heavy and near black, cropped ears, a pale jaw, red eyes and a
# spiked iron collar. It lunges. A beast shows its side in the diagonal views.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"

SIDE = [E] * 5 + [
        "...........KK.K.",
        "..........KbbKbK",
        "KK.......KbbbbEK",
        "KbK.KKKKKibbbbwK",
        ".KbKbbbbbibbbwwK",
        ".KbbbbbbbbibbKK.",
        "..KbbddbbbbbK...",
        "..KbbKKKKbbbK...",
        "..KbKbK..KbKbK..",
        "..KbKbK..KbKbK..",
        "..KdKdK..KdKdK.."]
FRONT = [E] * 5 + [
         "....KK....KK....",
         "...KbbKKKKbbK...",
         "...KbbbbbbbbK...",
         "...KbEbbbbEbK...",
         "...KbbwwwwbbK...",
         "..KKbwwKKwwbKK..",
         "..KiiiiiiiiiiK..",
         "..KbbbbbbbbbbK..",
         "...KbbKbbKbbK...",
         "...KbbK..KbbK...",
         "...KddK..KddK..."]
BACK = FRONT[:8] + ["...KbbbbbbbbK...", "...KbbbddbbbK...", "..KKbbbddbbbKK..", FRONT[11], "..KbbbbddbbbbK.."] + FRONT[13:]
kit.Character("gaol_mastiff", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "4e3e38", "d": "2e2422", "w": "9a8478", "i": "b0b8c2", "E": "ff3b30"}, role="charger",
              timing={"run": (0.4, 3.6)}).build()
