# The Bookworm (2026-11-16): the Burnt Library's swarm, a fat pale grub with a brown head, a ring of tiny legs, bits of
# scorched paper stuck to its back.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

SIDE = [c("") ] * 0
SIDE = fit([c("KKKKKK",16,1), c("KpPpPpKK",16,0), c("KPpPpPpbbK",16,0), c("KpPpPpPpbEK",16,0), c("KwKwKwKwKKK",16,0)], 16)
FRONT = fit([c("KKKK"), c("KbbbbK"), c("KbEbbEbK"), c("KbbKKbbK"), c("KpPpPpPpK"), c("KPpPpPpPK"), c("KwKKwwKKwK")])
BACK = fit([c("KKKK"), c("KpPpPpK"), c("KPpPpPpK"), c("KpPpPpPK"), c("KpPpPpPpK"), c("KwKKwwKKwK")])
kit.Character("bookworm", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"p": "d8d0b8", "P": "f0e8d0", "b": "7a5a3a", "E": "ff5a3a", "w": "b8a888"}, role="swarm", step_rows=1,
              timing={"run": (0.35, 3.6)}).build()
