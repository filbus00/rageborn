# The Reliquary Guard (2026-11-16): the Burnt Library's armoured guard, a gilded iron reliquary on legs: a gold-barred
# window over a glowing relic in its chest, a flat iron helm, a halberd. Slow and heavy; it throws blessed bombs.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

FRONT = fit([c("KKKK"), c("KIiiiiIK"), c("KIiIKKIiIK"), c("KIiEiiEiIK"), c("KKIiiiiIKK"), c("KIIgggggIIK"),
             c("KIgGGGGGGgIK"), c("KIgGrrrrGgIK"), c("KIgGrRRrGgIK"), c("KIgGrrrrGgIK"), c("KIgGGGGGGgIK"),
             c("KIIgggggggIIK"), c("KKIiiKKiiIKK"), c("KIiiK  KiiIK"), c("KiiiK  KiiiK"), c("KKKKK  KKKKK")])
SIDE = variant(FRONT, {3: c("KIiiEiIK"), 7: c("KIgGrrGgIK"), 8: c("KIgGrRGgIK"), 9: c("KIgGrrGgIK")})
BACK = variant(FRONT, {3: c("KIiiiiiiIK"), 7: c("KIgGGGGGGgIK"), 8: c("KIgGGGGGGgIK"), 9: c("KIgGGGGGGgIK")})
kit.Character("reliquary_guard", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"i": "6a7280", "I": "8e98a8", "g": "b8902e", "G": "d8b040", "r": "e04a3a", "R": "ffd0a0", "E": "ffb040"},
              role="melee", step_rows=1, timing={"walk": (1.1, 1.0)}).build()
