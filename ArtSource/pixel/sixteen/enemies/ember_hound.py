# The Ember Hound (2026-11-16, redrawn the same day): the Burnt Library's hound, a gaunt black dog whose ribs glow like
# coals, ember eyes, a mane and a tail tip of flame. It lunges. A beast shows its side in the diagonal views.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
SIDE = [E] * 5 + [
        ".........e.KK.K.",
        "........eeKbbKbK",
        "Ke......eKbbbbEK",
        "KeK.KKKKKebbbbwK",
        ".KbKbbebebebbwwK",
        ".KbbbebebebbbKK.",
        "..KbbbebebbbK...",
        "..KbbKKKKbbbK...",
        "..KbKbK..KbKbK..",
        "..KbKbK..KbKbK..",
        "..KdKdK..KdKdK.."]
FRONT = [E] * 4 + [
         "......e..e......",
         "....KKe..eKK....",
         "...KbbKeeKbbK...",
         "...KbbbbbbbbK...",
         "...KbEbbbbEbK...",
         "...KbbwwwwbbK...",
         "..KKbwwKKwwbKK..",
         "..KbebebbebebK..",
         "..KbbebbbbebbK..",
         "...KbbKbbKbbK...",
         "...KbbK..KbbK...",
         "...KddK..KddK..."]
BACK = FRONT[:7] + ["...KbbbeebbbK...", "...KbbbeebbbK...", "...KbbbbbbbbK...", "..KKbbbbbbbbKK..", "..KbebbbebbebK..",
                    "..KbbebbebebbK.."] + FRONT[13:]
kit.Character("ember_hound", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "2e262c", "d": "18141c", "e": "ff7a2a", "E": "ffc040", "w": "7a6a70"}, role="charger",
              timing={"run": (0.4, 3.6)}).build()
