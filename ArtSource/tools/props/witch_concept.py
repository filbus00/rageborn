# The Hedge Witch designed in Blender (2026-11-03, the owner: "Lets move on to the next enemy type", the Hedge Witch): the
# Crow Mother, the fourth shape beside the squat Sack Thief, the tall Scarecrow Bowman and the top-heavy Stump Hauler: a
# stooped triangle, widest at the ground. A long bell of a cloak in tattered violet feathers flaring to a ragged hem, a
# bone-white crow skull for a mask with a long beak and glowing eyes, a crown of pale twigs like antlers, a crooked staff
# taller than her with a glowing lantern hanging from its top, a crow on the staff and one on her shoulder.
# The big shapes at 92 px: the cloak's wide bell, the white skull and beak, the staff and its light.
#   Blender -b -P ArtSource/tools/props/witch_concept.py -- <out folder> [rest]
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from concept_kit import *

COLOURS.update({
    "cloak": (0.26, 0.08, 0.36), "cloak_dark": (0.12, 0.04, 0.2), "feather_sheen": (0.1, 0.28, 0.34),
    "bone": (0.92, 0.88, 0.76), "void": (0.02, 0.01, 0.01), "eye": (1.0, 0.85, 0.3), "skin": (0.5, 0.52, 0.42),
    "twig": (0.78, 0.7, 0.55), "wood": (0.3, 0.17, 0.08), "iron": (0.16, 0.16, 0.18), "glow": (1.0, 0.86, 0.3),
    "crow": (0.06, 0.06, 0.08), "beak": (0.75, 0.6, 0.2), "rope": (0.42, 0.26, 0.08), "brass": (0.95, 0.58, 0.1),
})
FABRIC.update({"cloak", "cloak_dark", "feather_sheen", "skin", "rope"})
METAL.update({"iron", "brass"})
SOFT.update({"cloak", "skin", "crow"})
FINE.extend(["feather", "rag", "twig", "claw"])
GLOW["glow"] = 3


def crow(at, facing, scale=1.0):
    """A crow perched: a plump body, a round head, a sharp yellow beak, a fanned tail, folded wings."""
    f = Vector(facing).normalized()
    at = Vector(at)
    ball("crow_body", at + Vector((0, 0, 0.07)) * scale, (0.07 * scale, 0.09 * scale, 0.07 * scale), "crow",
         rot=(0, 0, math.atan2(f.x, -f.y)))
    ball("crow_head", at + f * 0.08 * scale + Vector((0, 0, 0.14)) * scale, (0.045 * scale,) * 3, "crow")
    flap(at + f * 0.11 * scale + Vector((0, 0, 0.14)) * scale, f + Vector((0, 0, -0.2)), 0.07 * scale, 0.02 * scale, "beak")
    for side in (-1, 1):
        ball("crow_eye", at + f * 0.115 * scale + f.cross(Vector((0, 0, 1))) * 0.03 * side * scale + Vector((0, 0, 0.155)) * scale,
             (0.009 * scale,) * 3, "eye", seg=8)
    flap(at - f * 0.08 * scale + Vector((0, 0, 0.06)) * scale, -f + Vector((0, 0, -0.5)), 0.13 * scale, 0.05 * scale, "crow")


# The cloak: a tall bell of violet from her shoulders to the ground, its hem torn into feathered points, rows of
# feathers over it.
bpy.ops.mesh.primitive_cone_add(vertices=28, radius1=0.5, radius2=0.17, depth=1.12, location=(0, 0.04, 0.62))
finish(bpy.context.object, "cloak", smooth=False).name = "cloak_bell"


def on_bell(z):
    """The bell's radius at a height (its slope, from 0.5 at the ground to 0.17 at 1.18)."""
    return 0.5 + (0.17 - 0.5) * (z - 0.06) / 1.12


slope = (0.5 - 0.17) / 1.12
for row, (z, n, length) in enumerate(((0.36, 22, 0.32), (0.64, 18, 0.3), (0.9, 15, 0.28), (1.14, 12, 0.24))):
    for k in range(n):
        a = 2 * math.pi * (k + 0.5 * row) / n
        out = Vector((math.cos(a), math.sin(a), 0))
        at = Vector((0, 0.04, z)) + out * (on_bell(z) + 0.01)
        colour = "feather_sheen" if (k + row) % 5 == 0 else ("cloak_dark" if (k + row) % 2 else "cloak")
        feather(at, out + Vector((0, 0, slope)), out * slope + Vector((0, 0, -1)), length + 0.04 * ((k * 7) % 3), 0.055, colour)
# The hem: long ragged points reaching to the ground.
for k in range(24):
    a = 2 * math.pi * (k + 0.25) / 24
    out = Vector((math.cos(a), math.sin(a), 0))
    at = Vector((0, 0.04, 0.16)) + out * (on_bell(0.16) + 0.012)
    feather(at, out, out * 0.45 + Vector((0, 0, -1)), 0.16 + 0.03 * ((k * 5) % 3), 0.06, "cloak_dark" if k % 2 else "cloak", "rag")
# Her feet: bare, grey and clawed, just showing under the front of the hem.
for side in (-1, 1):
    ball("foot%d" % side, (0.12 * side, -0.4, 0.04), (0.06, 0.09, 0.04), "skin")
    for c in (-1, 0, 1):
        flap(Vector((0.12 * side + 0.03 * c, -0.47, 0.04)), Vector((0.02 * c, -1, -0.4)), 0.06, 0.012, "bone").name = "claw"

# Shoulders and a feathered mantle, hunched forward.
ball("shoulders", (0, -0.04, 1.2), (0.26, 0.18, 0.12), "cloak", rot=(0.35, 0, 0))
for k in range(12):
    a = math.pi * (1.1 + 0.8 * k / 11)
    out = Vector((math.cos(a), -math.sin(a) * 0.7, 0))
    feather(Vector((0, -0.04, 1.27)) + out * 0.2, out + Vector((0, 0, 0.6)), out * 0.7 + Vector((0, 0, -0.7)), 0.18, 0.05,
            "cloak_dark" if k % 2 else "feather_sheen")

# The head: low and forward, a violet cowl round a bone-white crow skull, its long beak hooked down, glowing eyes deep in
# its sockets; a crown of pale twigs branching up like antlers.
head = Vector((0, -0.2, 1.3))
ball("cowl", head + Vector((0, 0.05, 0.03)), (0.15, 0.15, 0.16), "cloak")
ball("skull", head + Vector((0, -0.07, 0.02)), (0.085, 0.09, 0.085), "bone")
hood_tail([head + Vector(p) for p in ((0, -0.13, 0.0), (0, -0.22, -0.02), (0, -0.3, -0.06), (0, -0.35, -0.12))],
          [0.045, 0.034, 0.02, 0.008], "bone")
for side in (-1, 1):
    ball("socket%d" % side, head + Vector((0.045 * side, -0.13, 0.04)), (0.026, 0.02, 0.024), "void", seg=12)
    ball("eye%d" % side, head + Vector((0.045 * side, -0.142, 0.04)), (0.013, 0.008, 0.013), "eye", seg=10)
for side in (-1, 1):
    base = head + Vector((0.07 * side, 0.02, 0.12))
    tip = base + Vector((0.16 * side, 0.04, 0.3))
    limb("twig", base, tip, 0.012, 0.006, "twig")
    for t, out in ((0.45, Vector((0.1 * side, -0.04, 0.1))), (0.75, Vector((-0.03 * side, 0.02, 0.12)))):
        p = base.lerp(tip, t)
        limb("twig_branch", p, p + out, 0.008, 0.004, "twig")

# Arms: thin, in long ragged sleeves, grey clawed hands. In the concept her left hand grips the staff and her right
# reaches out, fingers spread, casting; for the rig an A-pose, hands empty (the staff is a held piece).
if REST:
    poses = [(1, "l", Vector((0.36, -0.06, 1.0)), Vector((0.48, -0.08, 0.8))),
             (-1, "r", Vector((-0.36, -0.06, 1.0)), Vector((-0.48, -0.08, 0.8)))]
else:
    poses = [(1, "l", Vector((0.34, -0.2, 1.05)), Vector((0.36, -0.36, 1.12))),
             (-1, "r", Vector((-0.32, -0.28, 1.12)), Vector((-0.34, -0.54, 1.2)))]
for side, tag, el, wr in poses:
    sh = Vector((0.2 * side, -0.06, 1.2))
    limb("upper_" + tag, sh, el, 0.06, 0.055, "cloak")
    limb("fore_" + tag, el, wr, 0.06, 0.07, "cloak")
    feather(wr - (wr - el).normalized() * 0.03 + Vector((0, 0, -0.05)), Vector((0, -1, 0)), Vector((0, 0, -1)), 0.16, 0.06, "cloak_dark")
    d = (wr - el).normalized()
    ball("hand_" + tag, wr + d * 0.04, (0.035, 0.035, 0.035), "skin")
    across = d.cross(Vector((0, 0, 1))).normalized()
    for c in (-1, 0, 1):
        limb("claw_finger", wr + d * 0.05 + across * 0.02 * c, wr + d * 0.12 + across * 0.035 * c + Vector((0, 0, -0.02)),
             0.01, 0.005, "skin")

if not REST:
    # The staff: a crooked branch through her left fist, taller than her, a hook at its top from which a lantern hangs,
    # its core glowing; a crow on the hook.
    grip = poses[0][3] + (poses[0][3] - poses[0][2]).normalized() * 0.04
    path = [grip + Vector(p) for p in ((0.02, 0.02, -1.18), (-0.02, 0.0, -0.7), (0.02, -0.01, -0.25), (0, 0, 0.0), (0.03, 0.01, 0.35),
                                       (0.0, 0.0, 0.62), (-0.08, -0.02, 0.78), (-0.18, -0.04, 0.8))]
    hood_tail(path, [0.03, 0.03, 0.028, 0.028, 0.026, 0.024, 0.02, 0.016], "wood")
    hook = path[-1]
    limb("lantern_chain", hook, hook + Vector((0, 0, -0.14)), 0.006, 0.006, "iron")
    lantern = hook + Vector((0, 0, -0.24))
    ball("lantern_core", lantern, (0.05, 0.05, 0.07), "glow")
    for k in range(4):
        a = math.pi / 2 * k + math.pi / 4
        limb("lantern_bar", lantern + Vector((math.cos(a) * 0.06, math.sin(a) * 0.06, 0.09)),
             lantern + Vector((math.cos(a) * 0.06, math.sin(a) * 0.06, -0.09)), 0.007, 0.007, "iron")
    for dz in (0.1, -0.1):
        ring("lantern_rim", lantern + Vector((0, 0, dz)), 0.065, 0.01, "iron")
    crow(path[-3] + Vector((0, 0.0, 0.02)), Vector((0.4, -1, 0)), 1.0)
# A crow on her right shoulder.
crow(Vector((-0.22, 0.02, 1.27)), Vector((-0.3, -1, 0)), 0.9)

present("crow_mother", target=Vector((0, -0.05, 1.05)), size=2.5)
