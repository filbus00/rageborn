# The forest's quick props and floor marks (2026-10-26, the owner: "Do it all, i am going to bed": forest props, a way
# down, ground scatter, more trees and forest lairs, made quickly). Registered by models.py; props render to
# ArtSource/pixel/dungeon (Import Pixel Art), named prop_<snake> after PropKind, decals decal_<snake>_<n> after DecalKind.
#   prop_fallen_log, prop_stump, prop_mushrooms, prop_ferns, prop_standing_stone, prop_axe_stump   forest props
#   prop_bandit_tent, prop_web_sac                                                                lair props
#   decal_leaves_1..2, decal_flowers_1..2, decal_pebbles_1..2                                     ground scatter
import math
from kit import *
from models import seed, rock, PROP, DECAL, TALL


def _bark():
    return mat(WOOD[1], 0.35, WOOD[0], scale=12)


def _moss():
    return [mat(MOSS[1], 0.35, MOSS[0], scale=9), mat(MOSS[2], 0.35, MOSS[1], scale=9), mat(MOSS[3], 0.3, MOSS[2], scale=9)]


def fallen_log():
    rng = seed(700)
    cyl(0.1, 0.62, (-0.28, 0.0, 0.1), _bark(), 10, rot=(0, 90, 25))
    end = mat(WOOD[4], 0.3, WOOD[2], scale=16)
    cyl(0.095, 0.01, (0.28 * math.cos(math.radians(25)) - 0.01, 0.28 * math.sin(math.radians(25)), 0.1), end, 10, rot=(0, 90, 25))
    for k in range(4):
        sphere(0.05, (rng.uniform(-0.25, 0.25), rng.uniform(-0.1, 0.1), 0.18), rng.choice(_moss()), scale=(1.4, 1, 0.5), segments=6, smooth=False)


def stump():
    rng = seed(701)
    cyl(0.13, 0.16, (0, 0, 0), _bark(), 10)
    cyl(0.125, 0.012, (0, 0, 0.16), mat(WOOD[4], 0.3, WOOD[2], scale=16), 10)
    for k in range(4):
        a = 2 * math.pi * k / 4 + rng.uniform(0, 0.6)
        cyl(0.04, 0.16, (0.12 * math.cos(a), 0.12 * math.sin(a), 0.02), _bark(), 6, rot=(0, 70, math.degrees(a)))


def mushrooms():
    rng = seed(702)
    stem = mat(BONE[1], 0.2, BONE[0], scale=10)
    caps = [mat(BLOOD[2], 0.3, BLOOD[1], scale=10), mat(EMBER[1], 0.3, WOOD[2], scale=10)]
    for k in range(rng.randint(5, 8)):
        x, y = rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2)
        h = rng.uniform(0.05, 0.14)
        cyl(0.015, h, (x, y, 0), stem, 6)
        sphere(rng.uniform(0.035, 0.06), (x, y, h), rng.choice(caps), scale=(1, 1, 0.5), segments=8, smooth=False)


def ferns():
    rng = seed(703)
    greens = _moss()
    for k in range(rng.randint(7, 10)):
        a = rng.uniform(0, 360)
        box((0.32, 0.06, 0.01), (0, 0, 0.06), rng.choice(greens), rot=(rng.uniform(15, 35), 0, a))


def standing_stone():
    rng = seed(704)
    stone = mat(STONE[3], 0.35, STONE[1], scale=8)
    box((0.22, 0.14, 0.75), (0, 0, 0), stone, rot=(rng.uniform(-4, 4), rng.uniform(-4, 4), 20), bevel=0.04)
    for k in range(3):
        sphere(0.05, (rng.uniform(-0.1, 0.1), -0.08, rng.uniform(0.1, 0.5)), rng.choice(_moss()), scale=(1, 0.4, 1.3), segments=6, smooth=False)


def axe_stump():
    stump()
    iron = mat(STONE[4], 0.2, STONE[2], metal=0.6, rough=0.4)
    cyl(0.014, 0.32, (0.05, 0.0, 0.17), mat(WOOD[3], 0.3, WOOD[2]), 6, rot=(0, -60, 10))
    box((0.1, 0.02, 0.08), (-0.02, 0.0, 0.16), iron, rot=(0, -60, 10))


def bandit_tent():
    rng = seed(705)
    cloth = mat(EARTH[3], 0.35, EARTH[1], scale=10)
    pole = mat(WOOD[2], 0.3, WOOD[1])
    h = 0.62
    poly([(-0.3, -0.3, 0), (0.3, -0.3, 0), (0, -0.3, h), (-0.3, 0.3, 0), (0.3, 0.3, 0), (0, 0.3, h)],
         [(0, 1, 2), (3, 5, 4), (0, 2, 5, 3), (1, 4, 5, 2)], cloth)
    for y in (-0.3, 0.3):
        cyl(0.012, h + 0.05, (0, y, 0), pole, 6)
    # A dark opening toward the camera.
    poly([(-0.12, -0.302, 0), (0.12, -0.302, 0), (0, -0.302, h * 0.55)], [(0, 1, 2)], mat(STONE[0], 0.0))


def web_sac():
    rng = seed(706)
    silk = mat(BONE[2], 0.3, BONE[0], scale=14)
    for k in range(3):
        sphere(rng.uniform(0.07, 0.11), (rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), rng.uniform(0.06, 0.14)), silk, scale=(1, 1, 1.4), segments=8)
    for k in range(6):
        a = rng.uniform(0, 360)
        box((0.4, 0.004, 0.004), (0, 0, 0.12), silk, rot=(rng.uniform(-20, 20), 0, a))


def _leaves(n):
    rng = seed(720 + n)
    colours = [mat(EMBER[1], 0.3, WOOD[2], scale=10), mat(WOOD[3], 0.3, WOOD[1], scale=10), mat(MOSS[2], 0.3, MOSS[1], scale=10)]
    for k in range(14):
        blob(rng.uniform(-0.3, 0.3), rng.uniform(-0.15, 0.15), 0.035, rng.choice(colours), 5, 0.4, rng, z=0.003 + 0.001 * k, squash=0.6)


def _flowers(n):
    rng = seed(730 + n)
    petals = [mat(BONE[2], 0.1), mat(VIOLET[1], 0.2, VIOLET[0]), mat(EMBER[3], 0.2, EMBER[2])]
    green = mat(MOSS[2], 0.3, MOSS[1], scale=10)
    for k in range(10):
        x, y = rng.uniform(-0.3, 0.3), rng.uniform(-0.15, 0.15)
        blob(x, y, 0.03, green, 5, 0.4, rng, z=0.003)
        blob(x, y, 0.015, rng.choice(petals), 5, 0.3, rng, z=0.006)


def _pebbles(n):
    rng = seed(740 + n)
    stones = [mat(STONE[3], 0.3, STONE[2]), mat(STONE[4], 0.3, STONE[3])]
    for k in range(9):
        rock((rng.uniform(-0.3, 0.3), rng.uniform(-0.15, 0.15), 0.0), rng.uniform(0.02, 0.04), rng.choice(stones), rng, 0.5)


def register(all_pieces):
    for name, build, frame in [("fallen_log", fallen_log, PROP), ("stump", stump, PROP), ("mushrooms", mushrooms, PROP),
                               ("ferns", ferns, PROP), ("standing_stone", standing_stone, TALL), ("axe_stump", axe_stump, PROP),
                               ("bandit_tent", bandit_tent, TALL), ("web_sac", web_sac, PROP)]:
        all_pieces["prop_" + name] = ("dungeon", build, frame)
    for n in range(1, 3):
        all_pieces["decal_leaves_%d" % n] = ("dungeon", (lambda n=n: _leaves(n)), DECAL)
        all_pieces["decal_flowers_%d" % n] = ("dungeon", (lambda n=n: _flowers(n)), DECAL)
        all_pieces["decal_pebbles_%d" % n] = ("dungeon", (lambda n=n: _pebbles(n)), DECAL)
