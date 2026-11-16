# The Quill Scribe (2026-11-16, redrawn the same day): the Burnt Library's marksman, a thin scribe in an ink-black cowl
# and a grey robe, a great white quill in his raised hand, its nib dripping ink. He looses three quills.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E,
         ".....KKKKKK.....",
         "....KkkkkkkK....",
         "...KkkkkkkkkK..w",
         "...KkksssskkK.wW",
         "...KksEssEskK.wW",
         "...KkksssskkK.w.",
         "....KkkkkkkK..q.",
         "..KKggggggggKKq.",
         ".KggKggggggKgssK",
         ".KggKgGggGgKKKq.",
         ".KssKkkkkkkK..i.",
         "..KKKgGggGgK....",
         "...KggGggGggK...",
         "...KggggggggK...",
         "...KKKKKKKKKK..."]
SIDE = [E,
        "......KKKKK.....",
        ".....KkkkkkK....",
        "....KkkkkkkkK...",
        "....KkkkksssK...",
        "....KkkkksEsK...",
        "....KkkkssssK...",
        ".....KkkkkkK..W.",
        "....KgggggK..wW.",
        "...KgggggggK.w..",
        "...KggGggsssq...",
        "...KggGgKKKKi...",
        "...KkkkkkK......",
        "...KggGgggK.....",
        "...KgggggggK....",
        "...KKKKKKKKK...."]
BACK = FRONT[:3] + ["...KkkkkkkkkK..w", "...KkkkkkkkkK.wW", "...KkkkkkkkkK.wW", "...KkkkkkkkkK.w."] + FRONT[7:]
kit.Character("quill_scribe", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"k": "2a2a38", "g": "7c7c8a", "G": "5a5a68", "s": "d8c0a8", "E": "241c28", "w": "f4f4f8", "W": "c8ccd8",
               "q": "d8c898", "i": "3e5088"}, role="archer", step_rows=1,
              hand={"s": (14, 9), "se": (12, 10), "e": (12, 10), "ne": (12, 10), "n": (14, 9)}).build()
