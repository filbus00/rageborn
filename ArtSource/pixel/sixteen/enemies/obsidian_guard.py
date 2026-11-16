# The Obsidian Guard (2026-11-16): the Rift Caves' armoured guard, a knight in plate of black glass edged with violet
# light, a crested helm with a glowing slit, a tall shield, a glass blade. Slow and heavy; he throws a blade that bounces.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [".......KK.......",
         ".....KKooKK.....",
         "....KoOooOoK....",
         "....KooooooK..v.",
         "....KoEEEEoK.KvK",
         "....KoOooOoK.KvK",
         ".KKKKKooooKK.KvK",
         "KooooKooooooKKvK",
         "KoOvoKoOooOoKsvK",
         "KovvoKooooooKKK.",
         "KoOvoKooOOooK...",
         "KooooKooooooK...",
         ".KooK.KooooK....",
         "..KK..KoKKoK....",
         "......KoKKoK....",
         "......KKKKKK...."]
SIDE = ["........KK......",
        "......KKooK.....",
        ".....KoOoooK....",
        ".....KoooooK....",
        ".....KooEEEK....",
        ".....KoOoooK....",
        "......KoooK.....",
        "....KKoooooK..v.",
        "...KoooOooKKKKvK",
        "...KoooooosssKvK",
        "...KoOoooKKKKKvK",
        "...KoooooK....K.",
        "....KooooK......",
        "....KoKKoK......",
        "....KoKKoK......",
        "....KKKKKK......"]
BACK = FRONT[:4] + ["....KooooooK.KvK", "....KoOooOoK.KvK"] + FRONT[6:8] + ["KoOooKoOooOoKsvK", "KooooKooooooKKK.", "KoOooKooooooK..."] + FRONT[11:]
kit.Character("obsidian_guard", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"o": "3c3852", "O": "6e6890", "E": "d090ff", "v": "c080ff", "s": "8a8494"}, role="melee",
              timing={"walk": (1.1, 1.0)}).build()
