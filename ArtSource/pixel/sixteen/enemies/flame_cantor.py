# The Flame Cantor (2026-11-16): the Burnt Library's caster, a tall chanting monk in a white-and-gold choir robe scorched
# black at the hem, a hood of flame for hair, open hands lit orange. He chants three fireballs.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

FRONT = fit([c("KeK"), c("KeeeK"), c("KeEeeEK"), c("KKeEEeKK"), c("KwwwwwK"), c("KwsEssEswK"), c("KwsssssswK"), c("KKwwwwwwKK"),
             c("KwGwwwwGwK"), c("KwGwwwwGwK"), c("KsKwGwwGwKsK"), c("KeKwwGGwwKeK"), c("KwwwGwwGwwwK"),
             c("KwwwwwwwwwwK"), c("KkwwwwwwwwkK"), c("KkkkkkkkkkkK")])
SIDE = variant(FRONT, {5: c("KwwsEsswK"), 6: c("KwwssswwK")})
BACK = variant(FRONT, {5: c("KwwwwwwwwK"), 6: c("KwwwwwwwwK"), 8: c("KwGwwwwGwK")})
kit.Character("flame_cantor", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"w": "e8e0d0", "G": "d8b040", "s": "d8b8a0", "E": "ffb040", "e": "ff7a2a", "k": "2a2428"}, role="caster", glow="e",
              hand={"s": (2, 10), "se": (3, 10), "e": (3, 10), "ne": (3, 10), "n": (2, 10)}).build()
