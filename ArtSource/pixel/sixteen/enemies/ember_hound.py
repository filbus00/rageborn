# The Ember Hound (2026-11-16): the Burnt Library's hound, a gaunt black dog whose ribs glow like coals, ember eyes and
# a smoking mane. It lunges. A beast shows its side in the diagonal views.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

SIDE = fit([c("KK.K"), c("KbbKbK"), c("KbbbbEK",16,2), c("KKKbbbbbbbbK",16,1), c("KbeKebeKbbbbK",16,0), c("KbbbbbbbbbbK",16,0), c("KbKbK.KbKbK",16,0), c("KbKbK.KbKbK",16,0)])
FRONT = fit([c("KK    KK"), c("KbbKKKKbbK"), c("KbbbbbbbbK"), c("KbEbbbbEbK"), c("KbbbwwbbbK"), c("KKbwwwwbKK"), c("KbbebebbbK"),
             c("KbbbeebbbK"), c("KbbK..KbbK"), c("KddK..KddK")])
BACK = fit([c("KK    KK"), c("KbbKKKKbbK"), c("KbbbbbbbbK"), c("KbbbbbbbbK"), c("KbbbddbbbK"), c("KbbbddbbbK"), c("KbbebebbbK"),
            c("KbbbbbbbbK"), c("KbbK..KbbK"), c("KddK..KddK")])
kit.Character("ember_hound", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "2a2428", "d": "16121a", "e": "ff7a2a", "E": "ffb040", "w": "6a5a60"}, role="charger",
              timing={"run": (0.4, 3.6)}).build()
