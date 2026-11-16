# The Tome Golem (2026-11-16): the Burnt Library's brute, a lumbering heap of bound ledgers and iron clasps, two
# glowing ember eyes between the covers, loose pages fluttering from its shoulders. It slams, and throws pages.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

FRONT = fit([c("KK  KK"), c("KpKKKKpK"), c("KpbbbbpK"), c("KbBEbbEBbK"), c("KbbbbbbbbK"), c("KKbbKKbbKK"),
             c("KpKKbbbbKKpK"), c("KPPKbBbBbKPPK"), c("KpPKbbbbbKPpK"), c("KpPKBbbBbKPpK"), c("KppKbBbbBbKppK"),
             c("KKKKbbbbbbKKKK"), c("KbKKbBbbBbKKbK"), c("KKbbKKKKKKbbKK"), c("KbbbbK  KbbbbK"), c("KKKKKK  KKKKKK")])
SIDE = variant(FRONT, {3: c("KbbBEbBbK"), 4: c("KbbbbbbbK"), 5: c("KKbbbKbbKK")})
BACK = variant(FRONT, {3: c("KbBbbbbBbK"), 4: c("KbbbbbbbbK"), 5: c("KKbbbbbbKK")})
kit.Character("tome_golem", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "7a4a30", "B": "523020", "p": "d8cca8", "P": "f0e8d0", "E": "ff7a2a"}, role="melee",
              timing={"walk": (1.1, 1.0)}).build()
