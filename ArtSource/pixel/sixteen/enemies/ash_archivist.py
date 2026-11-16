# The Ash Archivist (2026-11-16): the Burnt Library's last boss (level 3-12), keeper of the records who burned the library
# and burned with it: a stooped figure in a charcoal scholar's robe white with ash, a hood of living flame, two ember
# eyes, a great burning ledger held open in both hands. 26 drawn pixels tall, at VisualScale 1.6.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant
W, H = 24, 26
def r(s, off=0): return c(s, W, off)
FRONT = fit([r("KeK"), r("KeeeK"), r("KeEeeEK"), r("KeEEeeEEeK"), r("KKeEEEEeKK"), r("KKfffffffKK"), r("KfFFFFFFfK"),
             r("KfFqEFFEqFfK"), r("KfFqqFFqqFfK"), r("KfFFFKKFFFfK"), r("KKfFFFFFFfKK"), r("KKccccccccKK"),
             r("KcCCcccccCCcK"), r("KcCpPPPPPPPCcK"), r("KcCpPeEPPEePCcK"), r("KsKpPPeEEePPPKsK"), r("KsKpPePPPPePKsK"),
             r("KKKpPPPPPPPPKKK"), r("KcCpPPppPPpPCcK"), r("KcCCcKKKKccCCcK"), r("KcCcccccccccCcK"),
             r("KcCcccaacccccCcK"), r("KcCccaaaaccccCcK"), r("KccCccccccccCccK"), r("KKccCccccCccKK"), r("KKKKKK  KKKKKK")], H, W)
SIDE = variant(FRONT, {7: r("KfFFqEFFfK"), 8: r("KfFFqqFFfK"), 9: r("KfFFFKFFfK")})
BACK = variant(FRONT, {7: r("KfFFFFFFFFfK"), 8: r("KfFFFFFFFFfK"), 9: r("KfFFFFFFFFfK")})
kit.Character("ash_archivist", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"f": "5a5660", "F": "3a3640", "q": "1c1820", "c": "6a6670", "C": "4a4650", "a": "a8a4ac", "e": "ff7a2a",
               "E": "ffd84a", "p": "e8dcc0", "P": "c8b890", "s": "8a8690"},
              role="caster", baked_scale=1.6, step_rows=1, margin=(8, 10, 8, 2), glow="e",
              hand={"s": (3, 15), "se": (3, 15), "e": (3, 15), "ne": (3, 15), "n": (3, 15)},
              timing={"run": (0.6, 3.0), "walk": (1.1, 1.0)}).build()
