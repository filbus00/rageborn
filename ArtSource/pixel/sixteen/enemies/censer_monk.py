# The Censer Monk (2026-11-16, redrawn the same day): the Burnt Library's guard, a stout tonsured monk in a dark red habit
# with a rope belt, a brass censer swinging on a chain from his fist. He strikes twice, backs off, and swings a cloud.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit
E = "................"
FRONT = [E,
         ".....KKKKKK.....",
         "....KtsssstK....",
         "...KtsssssstK...",
         "...KssEssEssK...",
         "...KssssssssK...",
         "....KssssssK....",
         "..KKrrrrrrrrKK..",
         ".KrrKrrrrrrKrrK.",
         ".KrRKrrrrrrKRrK.",
         ".KrrKooooooKrrK.",
         ".KssKrrrrorKssK.",
         "..KcKrrRRorKKK..",
         "..KcKrrrrrrK....",
         ".KbbbKrrrrrrK...",
         ".KBbBKKKKKKKK..."]
SIDE = [E,
        "......KKKKK.....",
        ".....KttsssK....",
        "....KtttssssK...",
        "....KttsssEsK...",
        "....KtsssssK....",
        ".....KssssK.....",
        "....KrrrrrK.....",
        "...KrrrrrrrK....",
        "...KrrRrrsssK...",
        "...KooooorKcK...",
        "...KrrrrrrKcK...",
        "...KrrRrrrKcK...",
        "...KrrrrrKbbbK..",
        "...KrrrrrKBbBK..",
        "...KKKKKKKKKK..."]
BACK = FRONT[:2] + ["....KtsssstK....", "...KtsssssstK...", "...KttttttttK...", "...KttttttttK...", "....KssssssK...."] + FRONT[7:]
kit.Character("censer_monk", {"s": FRONT, "se": SIDE, "e": SIDE, "ne": SIDE, "n": BACK},
              {"r": "7a2c28", "R": "521c1e", "s": "d8b090", "t": "5a3a24", "E": "241c28", "b": "e8b838", "B": "8a6a1a",
               "c": "aab0ba", "o": "c8b080"}, role="melee", step_rows=1).build()
