# Saint Marrow (2026-11-16): the Burnt Library's first boss (level 3-6), the monks' saint, long dead and still leading
# their prayer: a tall pointed mitre of bone and gold, a skull face with two marrow-yellow lights for eyes, a cope of
# cream and gold lined in red, a staff topped with a bone cross. 26 drawn pixels tall, at VisualScale 1.6.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant
W, H = 24, 26
def r(s, off=0): return c(s, W, off)
FRONT = fit([r("KK"), r("KGGK"), r("KGgGK"), r("KGwwgGK"), r("KwwGGwwK"), r("KGGGGGGGGK"), r("KwwwwwwwwK"),
             r("KwBBBBBBwK"), r("KwBEEBBEEBwK"), r("KwBBBKKBBBwK"), r("KwwBKKKKBwwK"), r("KKwBBBBBBwKK"),
             r("KKGGwwwwwwGGKK"), r("KGgGwwwGGwwwGgGK"), r("KGgGRwwGGwwRGgGK"), r("KGgGRwwGGwwRGgGK"),
             r("KGgGRRwwwwRRGgGK"), r("KGgGRRwGGwRRGgGK"), r("KGgGwwwGGwwwGgGK"), r("KwwGGwwwwwwGGwwK"),
             r("KwwwwGGGGwwwwK"), r("KwwwwwGGwwwwwK"), r("KwwBwwwwwwBwwK"), r("KwBBwwwwwwBBwK"), r("KwBBKKKKKKBBwK"), r("KKKKK  KKKKK")], H, W)
SIDE = variant(FRONT, {8: r("KwBBEEBBwK"), 9: r("KwBBBKKBwK"), 10: r("KwwBKKKBwK")})
BACK = variant(FRONT, {8: r("KwBBBBBBBBwK"), 9: r("KwBBBBBBBBwK"), 10: r("KwwBBBBBBwwK")})
kit.Character("saint_marrow", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"w": "e8e0d0", "B": "b8ae98", "G": "d8b040", "g": "a07820", "R": "a02a2a", "E": "ffe060"},
              role="melee", baked_scale=1.6, step_rows=2, margin=(8, 10, 8, 2),
              timing={"run": (0.5, 3.0), "walk": (1.0, 1.0)}).build()
