# The Ash Moth (2026-11-16): the Burnt Library's flying swarm, a grey moth with scorched wings edged in ember, feathered
# feelers and two red eyes. It flies and darts in.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from _rows import c, fit, variant

FRONT = fit([c("K    K",16), c("KK  KK"), c("KwwKKwwK"), c("KwWwKKwWwK"), c("KwWWwEEwWWwK"), c("KwWwwKKwwWwK"), c("KeWwwKKwwWeK"),
             c("KKeKKKKKKeKK"), c("KK  KK  KK")])
SIDE = fit([c("K"), c("KK"), c("KwwKK"), c("KwWwKEK"), c("KwWWwKKK"), c("KeWWwwK"), c("KKeWwK"), c("KKKK")], 16)
BACK = fit([c("KK  KK"), c("KwwKKwwK"), c("KwWwKKwWwK"), c("KwWWwKKwWWwK"), c("KwWwwKKwwWwK"), c("KeWwwKKwwWeK"), c("KKeKKKKKKeKK")])
kit.Character("ash_moth", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"w": "6a6670", "W": "a8a2ae", "e": "ff7a2a", "E": "ff3b30"}, role="charger", step_rows=1,
              timing={"run": (0.35, 3.6), "walk": (0.5, 1.0)}).build()
