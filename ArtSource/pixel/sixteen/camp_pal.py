# The camp's shared palette and polygon fill (split out of camp_art.py, 2026-11-22).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

PAL = {
    "canvas": ["1c3a26", "2e5a3a", "437c4a", "6aa060"],     # the company's green canvas, shadow to light
    "cloth": ["4a1414", "7a1e1e", "a82e2a", "d0483a"],      # the company's red
    "wood": ["2e1c12", "4e3220", "74502e", "9a6a3a"],
    "iron": ["15151c", "3a3e4a", "6a7080", "9aa2b2"],
    "stone": ["2a2c38", "3f4253", "5e6278", "8a90a6"],
    "parch": ["6a5a3e", "a08a5e", "d0bc8a", "ece0b8"],
    "fire": ["c8321a", "ff8a2a", "ffd84a", "fff6c0"],
    "straw": ["6a5216", "a8842a", "d8b848", "f0dc80"],
    "earth": ["2a1c12", "42301e", "5e4630", "7a5e40"],
    "grass": ["1c3a26", "2e5a34", "437c3e"],
    "skin": ["7a4a34", "b07a58", "d8a07a", "f0c09a"],
    "brass": ["6a4a10", "b88a22", "e8c050", "fff0a0"],
    "bone": ["8a7c66", "c8bca4", "f0e8d8"],
    "grey": ["34363e", "5a5e6a", "8a8e9a", "c0c4cc"],
}


def poly(im, pts, col):
    """Fills a polygon (scanline, half-pixel centres)."""
    n = len(pts)
    for y in range(im.h):
        for x in range(im.w):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = n - 1
            for i in range(n):
                xi, yi = pts[i]
                xj, yj = pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside:
                im.set(x, y, col)


def shadow(im, cx, cy, rx, ry):
    """No ground shadow is drawn into a piece: a patch of dark pixels reads as a black blob on the grass. The game's
    lights shade the pieces."""
    return
