# The Censer Monk (2026-11-16): the Burnt Library's guard, a stout monk in a dark red hooded robe with a rope belt, a
# brass censer on a chain swinging at his side, smoke curling off it. He strikes twice, backs off, and swings a cloud.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

FRONT = fit([c("KKKK"), c("KrrrrK"), c("KrRrrRrK"), c("KrsEssEsrK"), c("KrsssssrK"), c("KrrsKKsrrK"), c("KKrrrrrrrrKK"),
             c("KrrRrrrrRrrK"), c("KrrrrBBrrrrK"), c("KrrrbrrrrbrrrK"), c("KsKrrrrrrrrKsK"), c("KcKrrrrrrrrKggK"),
             c("KbBKrrrrrrKgbbK"), c("KbBKrrrrrrKKbBK"), c("KKKKrrrrrrKKKK"), c("KKKKKKKKKKKK")])
SIDE = variant(FRONT, {3: c("KrrrsEsrK"), 4: c("KrrrsssrK"), 5: c("KrrrrsKrK"), 11: c("KcKrrrrrrrrKgbK")})
BACK = variant(FRONT, {3: c("KrRrrrrRrK"), 4: c("KrrrrrrrrK"), 5: c("KrrrrrrrrK"), 8: c("KrrrrRRrrrrK")})
kit.Character("censer_monk", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"r": "5a2a24", "R": "7e3a2c", "s": "d8b090", "E": "241c28", "b": "e0b030", "B": "8a6a1a", "g": "8e8e98",
               "c": "aab0ba"}, role="melee").build()
