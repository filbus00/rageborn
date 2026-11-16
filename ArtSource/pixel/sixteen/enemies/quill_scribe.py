# The Quill Scribe (2026-11-16): the Burnt Library's marksman, a thin scribe in an ink-black cowl and grey robe, a bundle
# of long quills over his shoulder, an ink-stained hand holding one like a dart. He looses three quills.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

FRONT = fit([c("KKKK"), c("KkkkkK"), c("KkKkkKkK"), c("KksEssEskK"), c("KkssssskK"), c("KKkssssKKK"), c("KKggggggKK"),
             c("KgGgggggGgK"), c("KgGgggggGgK"), c("KsKggwwggKsK"), c("KKKggwwggKKK"), c("KggGggggGggK"),
             c("KggGggggGggK"), c("KgggKKKKgggK"), c("KKKKK  KKKKK"), ])
SIDE = variant(FRONT, {3: c("KkksEsskK"), 4: c("KkkssskkK"), 9: c("KgKgggsKwwK"), 10: c("KKKgggKKwK")})
BACK = variant(FRONT, {3: c("KkKkkkkKkK"), 4: c("KkkkkkkkK"), 5: c("KKkkkkkkKK"), 7: c("KgGqqwqqGgK"), 8: c("KgGqwqwqGgK")})
kit.Character("quill_scribe", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"k": "2a2a38", "g": "7a7a88", "G": "9a9aa8", "s": "d0b8a0", "E": "241c28", "w": "f0f0f4", "q": "d8c898"},
              role="archer", hand={"s": (13, 9), "se": (13, 9), "e": (13, 9), "ne": (13, 9), "n": (3, 9)}).build()
