# The Brood Mother (2026-11-07): the Brood Mother's fight (depth 12), a huge plum spider with a glowing magenta hourglass
# on her abdomen, a crown of red eyes and fangs, long banded legs. Drawn from shapes by code (ellipses and leg lines on a
# 30 x 22 grid), at her VisualScale (1.5) so the game does not stretch her.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import kit

W, H = 30, 22


def grid():
    return [["."] * W for _ in range(H)]


def ellipse(g, cx, cy, rx, ry, c):
    for y in range(H):
        for x in range(W):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                g[y][x] = c


def line(g, pts, c):
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = int(round(max(abs(x1 - x0), abs(y1 - y0), 1)))
        for k in range(n + 1):
            x, y = round(x0 + (x1 - x0) * k / n), round(y0 + (y1 - y0) * k / n)
            if 0 <= x < W and 0 <= y < H:
                g[y][x] = c


def outline(g):
    out = [row[:] for row in g]
    for y in range(H):
        for x in range(W):
            if g[y][x] == ".":
                for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    if 0 <= x + ox < W and 0 <= y + oy < H and g[y + oy][x + ox] not in ".K":
                        out[y][x] = "K"
                        break
    return ["".join(r) for r in out]


def front(eyes=True):
    g = grid()
    for side in (-1, 1):
        for i, (ax, ay, kx, ky, fx, fy) in enumerate([(3, 12, 9, 7, 13, 12), (3, 14, 10, 10, 13, 17), (3, 15, 9, 13, 12, 21), (2, 16, 6, 15, 8, 21)]):
            pts = [(14.5 + side * ax, ay), (14.5 + side * kx, ky), (14.5 + side * fx, fy)]
            line(g, pts, "l" if i % 2 else "L")
    ellipse(g, 14.5, 6.5, 8, 6, "a")
    ellipse(g, 14.5, 6, 2.2, 3, "m")
    ellipse(g, 14.5, 6, 1, 1.5, "M")
    ellipse(g, 14.5, 14, 5, 3.5, "a")
    if eyes:
        for x in (12, 14, 15, 17):
            g[13][x] = "E"
        for x in (13, 16):
            g[12][x] = "E"
    for x in (13, 16):
        g[16][x] = "F"
        g[17][x] = "F"
    return outline(g)


def side():
    g = grid()
    for i, (ax, kx, ky, fx) in enumerate([(14, 10, 8, 6), (16, 14, 7, 12), (18, 19, 7, 20), (20, 24, 9, 27)]):
        line(g, [(ax, 14), (kx, ky), (fx, 21)], "l" if i % 2 else "L")
    ellipse(g, 9, 10, 8, 6, "a")
    ellipse(g, 7, 9, 3, 2, "m")
    ellipse(g, 7, 9, 1.4, 1, "M")
    ellipse(g, 20, 13, 5, 3.5, "a")
    g[12][23] = g[12][21] = g[11][22] = "E"
    g[15][24] = g[16][24] = "F"
    return outline(g)


def back():
    g = grid()
    for side in (-1, 1):
        for i, (ax, ay, kx, ky, fx, fy) in enumerate([(3, 14, 9, 9, 13, 14), (3, 15, 10, 11, 13, 18), (2, 16, 9, 14, 12, 21), (2, 17, 6, 16, 8, 21)]):
            line(g, [(14.5 + side * ax, ay), (14.5 + side * kx, ky), (14.5 + side * fx, fy)], "l" if i % 2 else "L")
    ellipse(g, 14.5, 13, 9, 7, "a")
    ellipse(g, 14.5, 13, 2.5, 3.5, "m")
    ellipse(g, 14.5, 13, 1, 1.8, "M")
    ellipse(g, 14.5, 5, 4, 2.5, "a")
    return outline(g)


S, E_, N = front(), side(), back()
kit.Character("brood_mother", {"s": S, "se": E_, "e": E_, "ne": E_, "n": N},
              {"a": "5a2a5e", "m": "ff3ab0", "M": "ffd0f0", "E": "ff3b30", "F": "fff2e0", "L": "8a4a8e", "l": "6a3a70"},
              role="melee", baked_scale=1.5, step_rows=6, margin=(6, 8, 6, 2), death_split=12,
              timing={"run": (0.4, 3.0)}).build()
