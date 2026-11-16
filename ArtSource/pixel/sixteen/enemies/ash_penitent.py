# The Ash Penitent (2026-11-16): the Burnt Library's swarmer, a monk the fire never let go: a charred hooded robe split by
# glowing ember cracks, an ashen face with orange eyes, hands clasped. He shuffles and flicks embers.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

FRONT = fit([c("KKKK"), c("KhhhhK"), c("KhHhhHhK"), c("KhsEssEshK"), c("KhsssssshK"), c("KhhsKKshhK"), c("KKhhhhhhhhKK"),
             c("KhhHheehHhhK"), c("KhHhhhehhHhK"), c("KhhhhsshhhhK"), c("KhhHhhhhhhHhhK"), c("KhhehhhhhhhhehhK"),
             c("KhhhhhhhhhhhhhhK"), c("KhhhhhhhhhhhhK"), c("KKKKKKKKKKKKKK")])
SIDE = variant(FRONT, {3: c("KhhhsEsshK"), 4: c("KhhhsssshK"), 5: c("KhhhhsKshK")})
BACK = variant(FRONT, {3: c("KhHhhhhHhK"), 4: c("KhhhhhhhhK"), 5: c("KhhhhhhhhK"), 9: c("KhhhhhhhhhhK"), 11: c("KhheHhhhhhHehhK")})
kit.Character("ash_penitent", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"h": "3a2e2c", "H": "5c4c46", "e": "ff7a2a", "E": "ffb040", "s": "c4b4a4"}, role="melee",
              timing={"walk": (1.0, 1.0)}).build()
