# The Candle Bearer (2026-11-16): the Burnt Library's support, a bent old monk in a wax-stained brown habit, his hood
# crowned with burning candle stubs, a great candle in his hands. Those near his light hit harder; he throws hot wax.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

FRONT = fit([c("y y y y"), c("YKYKYKYK",16), c("KwKwKwK"), c("KKKKKK"), c("KbbbbbbK"), c("KbsEssEsbK"), c("KbsssssbK"), c("KKbbbbbbKK"),
             c("KbBbbbbBbK"), c("KbBbwwbBbK"), c("KsKbBwwBbKsK"), c("KbKbbwwbbKbK"), c("KbbbBbbBbbbK"),
             c("KbbbbbbbbbbK"), c("KKKKKKKKKKKK")])
SIDE = variant(FRONT, {5: c("KbbsEsbK"), 6: c("KbbsssbK")})
BACK = variant(FRONT, {5: c("KbbbbbbK"), 6: c("KbbbbbbK"), 9: c("KbBbbbbBbK")})
kit.Character("candle_bearer", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"b": "6a4a34", "B": "4a3022", "s": "d8b8a0", "E": "241c28", "w": "f0e8d0", "y": "ffd84a", "Y": "ff9a2a"},
              role="melee", step_rows=1).build()
