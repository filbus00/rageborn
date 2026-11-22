import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gaol_art import Img, write_png
import camp_people as cp
sheet = Img(110, 40)
for y in range(sheet.h):
    for x in range(sheet.w):
        sheet.set(x, y, "1c3a26" if (x // 4 + y // 4) % 2 == 0 else "20402a")
x = 3
for f in (cp.brunhilde, cp.thorin, cp.lyriel, cp.wolf):
    im = f()
    sheet.paste(im, x, 40 - im.h - 2)
    x += im.w + 3
out = sys.argv[1] if len(sys.argv) > 1 else "/tmp/people.png"
write_png(out, sheet.rows(8))
