# The Reliquary Guard (2026-11-16, redrawn the same day): the Burnt Library's armoured guard, a knight in gilded plate
# with a relic glowing behind a window in his breastplate, a flat great helm with a gold cross, a halberd at his side.
# Slow and heavy; he throws blessed bombs.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [".............KK.",
         ".....KKKKKK.KIIK",
         "....KIiiiidKKIiK",
         "....KIiggidK.Kw.",
         "....KEEggEEK..w.",
         "....KIiggidK..w.",
         "....KKiiidKK..w.",
         "..KKggggggggKKw.",
         ".KIiKigRRgiKiKw.",
         ".KIiKigRRgiKssw.",
         ".KiiKiggggiKKKw.",
         ".KssKiiiiiiK..w.",
         "..KKKggggggK..w.",
         "....KiiKKiiK..w.",
         "....KidKKidK..w.",
         "....KKKKKKKK..w."]
SIDE = ["............KK..",
        "......KKKKKKIIK.",
        ".....KIiiiiKIiK.",
        ".....KIiiggKKw..",
        ".....KiiiEEK.w..",
        ".....KIiiggK.w..",
        ".....KKiiiKK.w..",
        "....KggggggK.w..",
        "...KIiigRgiKKw..",
        "...KIiiiiisssw..",
        "...KiiggggKKKw..",
        "...KiiiiiiK..w..",
        "....KggggK...w..",
        "....KiiKiiK..w..",
        "....KidKidK..w..",
        "....KKKKKKK..w.."]
BACK = FRONT[:3] + ["....KIiiiidK..w.", "....KIiiiidK..w.", "....KIiiiidK..w."] + FRONT[6:8] + [".KIiKiiiiiiKiKw.", ".KIiKiiggiiKssw.",
                                                                                  ".KiiKiiiiiiKKKw."] + FRONT[11:]
kit.Character("reliquary_guard", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"i": "6a7280", "I": "98a2b2", "d": "444a56", "g": "d8b040", "R": "ff6a3a", "E": "ffb040", "s": "c89878",
               "w": "8a6238"}, role="melee", timing={"walk": (1.1, 1.0)}).build()
