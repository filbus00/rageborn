# A preview of the keepers' idle frames beside the archer, for scale (2026-11-24).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gaol_art import Img, write_png
import camp_people as cp

sheet = Img(4 * 27 + 4, 3 * 27 + 4)
for y in range(sheet.h):
    for x in range(sheet.w):
        sheet.set(x, y, "1c3a26" if (x // 4 + y // 4) % 2 == 0 else "20402a")
for row, name in enumerate(cp.KEEPERS):
    for frame in range(4):
        sheet.paste(cp.keeper(name, frame), 2 + frame * 27, 2 + row * 27)
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "previews", "camp_keepers_idle.png")
write_png(out, sheet.rows(6))
