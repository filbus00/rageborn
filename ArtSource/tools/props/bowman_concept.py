# The Bandit Bowman designed in Blender (2026-11-03, the owner: "Lets move on to the next enemy type", the Bandit
# Bowman, the way the Sack Thief was made): the Scarecrow Bowman, the Sack Thief's gang-mate and his opposite in shape,
# tall and thin where the thief is short and round. A stitched burlap sack for a head with glowing eyes and a stitched
# grin, a tall battered top hat bent over with a crow feather, a mustard scarf hanging over his chest, a plum coat with a
# tattered hem, long striped legs, a quiver bristling with red-fletched arrows, a longbow drawn to his cheek.
# The big shapes at 92 px: the hat's tall stack, the bow's arc, the quiver's fan of red.
#   Blender -b -P ArtSource/tools/props/bowman_concept.py -- <out folder>
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from concept_kit import *

COLOURS.update({
    "burlap": (0.58, 0.42, 0.2), "coat": (0.3, 0.07, 0.2), "coat_dark": (0.17, 0.04, 0.11),
    "scarf": (0.88, 0.58, 0.06), "stripe_a": (0.85, 0.6, 0.12), "stripe_b": (0.12, 0.08, 0.1),
    "hat": (0.09, 0.08, 0.1), "band": (0.6, 0.04, 0.03), "feather": (0.05, 0.05, 0.08),
    "leather": (0.22, 0.075, 0.022), "boot": (0.16, 0.06, 0.03), "rope": (0.42, 0.26, 0.08),
    "stitch": (0.1, 0.04, 0.015), "wood": (0.36, 0.14, 0.035), "string": (0.85, 0.82, 0.7),
    "fletch": (0.7, 0.04, 0.03), "steel": (0.52, 0.45, 0.38), "brass": (0.95, 0.58, 0.1),
    "straw": (0.95, 0.78, 0.3), "eye": (1.0, 0.85, 0.45), "void": (0.02, 0.01, 0.01),
})
FABRIC.update({"burlap", "coat", "coat_dark", "scarf", "stripe_a", "stripe_b", "hat", "band", "leather", "boot", "rope"})
METAL.update({"steel", "brass"})
SOFT.update({"burlap", "coat", "boot", "stripe_a", "stripe_b", "hat"})
FINE.extend(["arrow", "straw", "string", "feather"])

# Legs: long and thin, in trousers striped mustard and black (rings of each), pointed dark boots.
for side in (-1, 1):
    hip = Vector((0.09 * side, 0.0, 0.98))
    knee = Vector((0.12 * side, -0.04, 0.52))
    ankle = Vector((0.11 * side, 0.0, 0.1))
    points = [hip.lerp(knee, t) for t in (0, 0.25, 0.5, 0.75)] + [knee.lerp(ankle, t) for t in (0, 0.18, 0.36, 0.54, 0.72)]
    for k, (a, b) in enumerate(zip(points, points[1:])):
        limb("leg%d_%d" % (side, k), a, b, 0.062 - 0.002 * k, 0.06 - 0.002 * k, "stripe_a" if k % 2 == 0 else "stripe_b")
    limb("boot_shaft%d" % side, knee.lerp(ankle, 0.72), ankle, 0.055, 0.055, "boot")
    # A pointed toe, curling up a little.
    limb("boot_toe%d" % side, ankle + Vector((0, -0.02, -0.05)), ankle + Vector((0.01 * side, -0.2, -0.04)), 0.06, 0.03, "boot")
    ball("boot_tip%d" % side, ankle + Vector((0.01 * side, -0.24, -0.02)), (0.025, 0.03, 0.025), "boot")
    # Straw poking out where the trousers end.
    for k in range(4):
        a = 2 * math.pi * k / 4 + side
        box("straw_leg", knee.lerp(ankle, 0.7) + Vector((math.cos(a) * 0.06, math.sin(a) * 0.06, -0.01)), (0.008, 0.008, 0.07),
            "straw", rot=(math.sin(a) * 0.6, math.cos(a) * 0.6, 0))

# Body: narrow, a little stooped; a plum coat over it, long tattered tails hanging to the knees behind and at the sides.
ball("chest", (0, -0.02, 1.3), (0.17, 0.13, 0.19), "coat", rot=(0.15, 0, 0))
ball("belly", (0, 0.0, 1.08), (0.14, 0.11, 0.14), "coat")
box("belt", (0, 0.0, 1.0), (0.3, 0.24, 0.06), "leather")
box("buckle", (0, -0.125, 1.0), (0.06, 0.02, 0.05), "brass")
for k in range(9):
    a = math.radians(-20 + 220 * k / 8) - math.pi / 2 + math.pi  # from his left side round the back to his right
    at = Vector((math.cos(a) * 0.15, math.sin(a) * 0.12 + 0.02, 0.98))
    length = 0.42 + 0.08 * ((k * 7) % 3)
    flap(at, Vector((math.cos(a) * 0.25, math.sin(a) * 0.25, -1)), length, 0.07, "coat_dark" if k % 2 else "coat")
# Buttons and a seam.
for z in (1.18, 1.27, 1.36):
    ball("button", (0.03, -0.15, z), (0.014, 0.008, 0.014), "brass", seg=8)
stitches((-0.05, -0.15, 1.4), (-0.06, -0.12, 1.12), 5)

# Head: a burlap sack, taller than wide, tied at the neck with rope, two glowing eyes and a stitched grin; straw tufts.
head = Vector((0, -0.04, 1.63))
ball("neck", (0, -0.02, 1.48), (0.05, 0.05, 0.05), "burlap")
ball("head", head, (0.12, 0.11, 0.15), "burlap")
ring("neck_rope", (0, -0.02, 1.5), 0.06, 0.014, "rope")
for side in (-1, 1):
    ball("socket%d" % side, head + Vector((0.045 * side, -0.1, 0.03)), (0.028, 0.016, 0.03), "void", seg=12)
    ball("eye%d" % side, head + Vector((0.045 * side, -0.108, 0.03)), (0.015, 0.008, 0.017), "eye", seg=10)
for k in range(-3, 4):
    p = head + Vector((0.02 * k, -0.11 + 0.004 * abs(k), -0.05 + 0.006 * abs(k) ** 1.4))
    for turn in (0.785, -0.785):
        box("stitch_grin", p, (0.004, 0.004, 0.028), "stitch", rot=(0, turn, 0))
for k in range(6):
    a = 2 * math.pi * k / 6
    box("straw_neck", (math.cos(a) * 0.07, -0.02 + math.sin(a) * 0.06, 1.47), (0.008, 0.008, 0.08), "straw",
        rot=(math.sin(a) * 1.0, -math.cos(a) * 1.0, 0))

# The hat: a tall battered stovepipe, its crown bent over toward his left, a red band and a black crow feather.
brim_at = head + Vector((0, 0.0, 0.12))
bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.2, depth=0.02, location=brim_at, rotation=(0.12, -0.08, 0))
finish(bpy.context.object, "hat")
limb("crown_low", brim_at, brim_at + Vector((0.01, 0.01, 0.2)), 0.105, 0.11, "hat")
limb("crown_high", brim_at + Vector((0.01, 0.01, 0.2)), brim_at + Vector((0.09, 0.02, 0.36)), 0.11, 0.1, "hat")
limb("band", brim_at + Vector((0, 0, 0.025)), brim_at + Vector((0.002, 0.002, 0.06)), 0.112, 0.112, "band")
flap(brim_at + Vector((0.09, -0.04, 0.05)), Vector((0.35, 0.1, 1)), 0.3, 0.035, "feather")

# The scarf: wound round his neck, its two ends hanging down over his chest, one longer (the owner, 2026-11-03: "make
# it just drape around him and fall on his breast. No things sticking out"; it streamed out behind him at first).
ring("scarf_wrap", (0, -0.02, 1.47), 0.085, 0.035, "scarf")
hood_tail([Vector(p) for p in ((0.04, -0.1, 1.46), (0.06, -0.155, 1.36), (0.065, -0.165, 1.22), (0.06, -0.16, 1.08))],
          [0.04, 0.038, 0.036, 0.03], "scarf")
hood_tail([Vector(p) for p in ((-0.035, -0.1, 1.46), (-0.05, -0.155, 1.37), (-0.055, -0.165, 1.25))],
          [0.038, 0.035, 0.028], "scarf")

# The quiver on his back, tilted over his right shoulder, a fan of arrows with red fletching sticking out of it.
q0 = Vector((0.06, 0.16, 1.02))
q1 = Vector((-0.12, 0.2, 1.52))
limb("quiver", q0, q1, 0.07, 0.085, "leather")
ring("quiver_rim", q1, 0.085, 0.012, "brass", rot=(0.15, -0.35, 0))
for k in range(7):
    spread = (k - 3) * 0.06
    tip = q1 + Vector((spread - 0.12, 0.03 + 0.02 * ((k * 3) % 3), 0.24 + 0.03 * ((k * 5) % 2)))
    limb("arrow_shaft", q1 - Vector((0, 0, 0.05)), tip, 0.006, 0.006, "wood")
    flap(tip, (tip - q1).normalized() * -1, 0.09, 0.03, "fletch")
limb("quiver_strap", Vector((0.16, -0.13, 1.42)), Vector((-0.13, -0.12, 1.02)), 0.015, 0.015, "leather")

if REST:
    # The A-pose for the rig: both arms down and out, hands empty (the longbow is a held piece, enemy_weapons.py).
    for side, tag in ((1, "l"), (-1, "r")):
        sh = Vector((0.19 * side, -0.02, 1.4))
        el = Vector((0.34 * side, -0.04, 1.2))
        wr = Vector((0.46 * side, -0.06, 1.01))
        limb("upper_" + tag, sh, el, 0.055, 0.048, "coat")
        limb("fore_" + tag, el, wr, 0.048, 0.042, "coat")
        ball("hand_" + tag, wr + (wr - el).normalized() * 0.03, (0.045, 0.045, 0.045), "burlap")
        ball("shoulder_" + tag, sh, (0.07, 0.07, 0.06), "coat")
        d = (wr - el).normalized()
        across = d.cross(Vector((0, 1, 0))).normalized()
        for k in range(4):
            a = 2 * math.pi * k / 4
            box("straw_wrist", wr + (across * math.cos(a) + d.cross(across) * math.sin(a)) * 0.045 - d * 0.01,
                (0.007, 0.007, 0.06), "straw", rot=(math.cos(a) * 0.8, math.sin(a) * 0.8, 0))
else:
    # Arms: long and thin, the coat's sleeves, frayed with straw at the wrists. His left holds the bow out before him; his
    # right draws the string back to his cheek.
    sh_l = Vector((0.19, -0.02, 1.4))
    el_l = Vector((0.26, -0.3, 1.42))
    wr_l = Vector((0.3, -0.58, 1.44))
    sh_r = Vector((-0.19, -0.02, 1.4))
    el_r = Vector((-0.34, 0.06, 1.47))
    wr_r = Vector((-0.08, -0.14, 1.56))
    for sh, el, wr, tag in ((sh_l, el_l, wr_l, "l"), (sh_r, el_r, wr_r, "r")):
        limb("upper_" + tag, sh, el, 0.055, 0.048, "coat")
        limb("fore_" + tag, el, wr, 0.048, 0.042, "coat")
        ball("hand_" + tag, wr + (wr - el).normalized() * 0.03, (0.045, 0.045, 0.045), "burlap")
        for k in range(4):
            a = 2 * math.pi * k / 4
            d = (wr - el).normalized()
            side = d.cross(Vector((0, 0, 1))).normalized()
            box("straw_wrist", wr + (side * math.cos(a) + d.cross(side) * math.sin(a)) * 0.045 - d * 0.01,
                (0.007, 0.007, 0.06), "straw", rot=(math.cos(a) * 0.8, math.sin(a) * 0.8, 0))
    ball("shoulder_l", sh_l, (0.07, 0.07, 0.06), "coat")
    ball("shoulder_r", sh_r, (0.07, 0.07, 0.06), "coat")

    # The longbow: a tall arc through his left fist, bending away from him, a pale string drawn back to his right hand, an
    # arrow nocked on it.
    grip = wr_l + Vector((0.01, -0.03, 0))
    top = grip + Vector((0.0, 0.1, 0.62))
    bottom = grip + Vector((0.0, 0.1, -0.62))
    arc = [bottom, grip + Vector((0, 0.03, -0.42)), grip + Vector((0, -0.03, -0.18)), grip, grip + Vector((0, -0.03, 0.18)),
           grip + Vector((0, 0.03, 0.42)), top]
    hood_tail(arc, [0.02, 0.022, 0.024, 0.026, 0.024, 0.022, 0.016], "wood")
    limb("bow_grip", grip - Vector((0, 0, 0.06)), grip + Vector((0, 0, 0.06)), 0.024, 0.024, "leather")
    nock = wr_r + Vector((0.0, -0.02, 0))
    limb("string_top", top, nock, 0.004, 0.004, "string")
    limb("string_bottom", bottom, nock, 0.004, 0.004, "string")
    arrow_tip = nock + (grip - nock) * 1.25
    limb("arrow_nocked", nock, arrow_tip, 0.006, 0.006, "wood")
    flap(arrow_tip, (arrow_tip - nock).normalized(), 0.06, 0.018, "steel")
    flap(nock + (grip - nock).normalized() * 0.05, (nock - grip).normalized(), 0.07, 0.025, "fletch")

present("scarecrow_bowman", target=Vector((0, -0.1, 1.05)), size=2.5)
