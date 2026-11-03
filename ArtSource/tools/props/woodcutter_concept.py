# The Woodcutter designed in Blender (2026-11-03, the owner: "Lets move on to the next enemy type", the Woodcutter): the
# Stump Hauler, a hulking, top-heavy axeman, the third shape beside the squat Sack Thief and the tall Scarecrow Bowman: huge
# shoulders and arms on short legs, an upturned wooden bucket for a helmet with an eye slit, a bushy ginger beard spilling
# out under it, a grey fur mantle, a leather apron and a red sash, and a chopped log strapped to his back. The big
# shapes at 92 px: the bucket's block, the beard's flame of orange, the log standing up behind him.
#   Blender -b -P ArtSource/tools/props/woodcutter_concept.py -- <out folder> [rest]
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from concept_kit import *

COLOURS.update({
    "skin": (0.72, 0.45, 0.3), "beard": (0.78, 0.27, 0.04), "fur": (0.66, 0.64, 0.6), "fur_dark": (0.38, 0.36, 0.34),
    "bucket": (0.5, 0.29, 0.11), "iron": (0.16, 0.16, 0.18), "trousers": (0.12, 0.17, 0.32), "leather": (0.3, 0.13, 0.04),
    "boot": (0.2, 0.09, 0.03), "sash": (0.62, 0.04, 0.03), "bark": (0.26, 0.15, 0.07), "heartwood": (0.86, 0.66, 0.38),
    "wood": (0.4, 0.2, 0.06), "steel": (0.55, 0.55, 0.58), "steel_light": (0.88, 0.9, 0.93), "void": (0.02, 0.01, 0.01),
    "eye": (1.0, 0.85, 0.45), "brass": (0.95, 0.58, 0.1), "stitch": (0.1, 0.04, 0.015),
})
FABRIC.update({"skin", "beard", "fur", "fur_dark", "trousers", "leather", "boot", "sash", "bark"})
METAL.update({"iron", "steel", "brass"})
SOFT.update({"skin", "beard", "fur", "trousers", "boot"})
MIDDLE.extend(["hatchet_head", "slit"])


def hatchet(grip, toward, up, scale=1.0, name="hatchet"):
    """A hatchet: a short handle from the grip along toward, its head at the far end, the bit facing up."""
    toward = Vector(toward).normalized()
    up = (Vector(up) - toward * Vector(up).dot(toward)).normalized()
    end = Vector(grip) + toward * 0.42 * scale
    limb(name + "_handle", Vector(grip) - toward * 0.06 * scale, end, 0.02 * scale, 0.022 * scale, "wood")
    side = toward.cross(up).normalized()
    rot = Matrix((toward, up, side)).transposed().to_euler()
    # The head: a block on the handle's end, the blade flaring out from it toward up, its edge ground bright.
    box(name + "_head", end + up * 0.03 * scale, (0.08 * scale, 0.1 * scale, 0.035 * scale), "steel", rot=rot)
    box(name + "_blade", end + up * 0.12 * scale, (0.15 * scale, 0.1 * scale, 0.022 * scale), "steel", rot=rot)
    box(name + "_bit", end + up * 0.175 * scale, (0.17 * scale, 0.025 * scale, 0.024 * scale), "steel_light", rot=rot)


# Legs: short and thick, dark blue trousers, heavy boots, leather wraps at the shins.
for side in (-1, 1):
    hip = Vector((0.15 * side, 0.02, 0.74))
    knee = Vector((0.19 * side, -0.04, 0.42))
    ankle = Vector((0.18 * side, 0.0, 0.11))
    limb("thigh%d" % side, hip, knee, 0.13, 0.11, "trousers")
    limb("shin%d" % side, knee, ankle, 0.1, 0.09, "trousers")
    ball("boot%d" % side, (0.18 * side, -0.07, 0.08), (0.12, 0.18, 0.09), "boot")
    box("boot_cuff%d" % side, (0.18 * side, -0.02, 0.2), (0.22, 0.22, 0.08), "leather", rot=(0.06, 0, 0))
    ring("wrap", (0.185 * side, -0.02, 0.31), 0.095, 0.01, "leather")

# Body: a barrel of a chest and belly, bare-armed, a leather apron down the front, a red sash at the waist.
ball("belly", (0, -0.02, 0.86), (0.3, 0.26, 0.22), "skin")
ball("chest", (0, -0.02, 1.12), (0.36, 0.28, 0.27), "skin", rot=(0.12, 0, 0))
box("apron", (0, -0.27, 0.86), (0.36, 0.04, 0.5), "leather", rot=(0.12, 0, 0))
box("apron_pocket", (0.06, -0.3, 0.76), (0.14, 0.02, 0.1), "leather", rot=(0.12, 0, 0))
ring("sash", (0, -0.02, 0.76), 0.27, 0.05, "sash", rot=(0, 0, 0))
flap(Vector((-0.2, -0.2, 0.73)), Vector((-0.2, -0.3, -1)), 0.26, 0.07, "sash")
flap(Vector((-0.16, -0.22, 0.73)), Vector((0.05, -0.3, -1)), 0.22, 0.06, "sash")

# The fur mantle over his huge shoulders, lumps of grey pelt round his neck and down his back.
for k in range(10):
    a = 2 * math.pi * k / 10
    ball("fur", (math.cos(a) * 0.3, math.sin(a) * 0.22 + 0.03, 1.33 + 0.03 * math.sin(3 * a)), (0.14, 0.12, 0.1), "fur")
ball("fur_back", (0, 0.16, 1.22), (0.3, 0.12, 0.18), "fur")

# Head: small, sunk between the shoulders; an upturned wooden bucket over it with two iron bands and an eye slit, glowing
# eyes inside; a big ginger beard spilling out under its rim and down his chest.
head = Vector((0, -0.08, 1.46))
bpy.ops.mesh.primitive_cone_add(vertices=20, radius1=0.17, radius2=0.14, depth=0.3, location=head + Vector((0, 0, 0.08)))
finish(bpy.context.object, "bucket", smooth=False).name = "bucket"
for z in (-0.05, 0.17):
    ring("bucket_band", head + Vector((0, 0, z)), 0.17 - 0.1 * (z + 0.07), 0.012, "iron")
box("slit", head + Vector((0, -0.16, 0.06)), (0.2, 0.03, 0.035), "void")
for side in (-1, 1):
    ball("eye%d" % side, head + Vector((0.045 * side, -0.165, 0.06)), (0.018, 0.01, 0.012), "eye", seg=10)
box("bucket_handle_l", head + Vector((0.165, 0, 0.0)), (0.02, 0.05, 0.05), "iron")
box("bucket_handle_r", head + Vector((-0.165, 0, 0.0)), (0.02, 0.05, 0.05), "iron")
# The beard: big and bushy, from under the rim out over the fur and down his chest to his belly, in front of the apron.
for p, size in (((0, -0.2, 1.33), (0.16, 0.09, 0.08)), ((0.12, -0.19, 1.3), (0.09, 0.08, 0.08)), ((-0.12, -0.19, 1.3), (0.09, 0.08, 0.08)),
                ((0, -0.28, 1.22), (0.17, 0.08, 0.11)), ((0, -0.31, 1.08), (0.15, 0.07, 0.12)), ((0, -0.32, 0.95), (0.1, 0.06, 0.1)),
                ((0.07, -0.31, 0.86), (0.05, 0.05, 0.07)), ((-0.06, -0.31, 0.87), (0.05, 0.05, 0.07))):
    ball("beard", p, size, "beard")
ring("beard_bead", (0.07, -0.32, 0.8), 0.028, 0.012, "brass")

# The log on his back: a thick bark-covered round standing up behind him, its cut top pale, strapped on across his
# chest (the hatchets once stuck in its top removed, the owner: "its looks a bit wierd with the axes just sticking out").
log0 = Vector((0.0, 0.4, 0.8))
log1 = Vector((0.05, 0.46, 1.98))
# A flat-ended round (bevelled, not rounded off by the smoothing as a tube would be), its cut top a pale disc.
d = log1 - log0
bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.2, depth=d.length, location=(log0 + log1) / 2)
log = bpy.context.object
log.rotation_mode = "QUATERNION"
log.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d.normalized())
finish(log, "bark", smooth=False).name = "log"
bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.17, depth=0.02, location=log1 + d.normalized() * 0.005)
top = bpy.context.object
top.rotation_mode = "QUATERNION"
top.rotation_quaternion = log.rotation_quaternion
finish(top, "heartwood", smooth=False).name = "log_top"
ring("log_ring", log1 + Vector((0, 0, 0.015)), 0.09, 0.006, "bark")
for z in (1.0, 1.4):
    ring("log_strap", (0.02, 0.42, z), 0.21, 0.02, "leather")
limb("strap_l", (0.3, 0.05, 1.32), (0.18, -0.25, 1.05), 0.025, 0.025, "leather")
limb("strap_r", (-0.3, 0.05, 1.32), (-0.18, -0.25, 1.05), 0.025, 0.025, "leather")

# Arms: huge, bare, a leather bracer on each forearm. In the concept his right arm is raised back, a hatchet in his
# fist ready to throw; his left reaches forward. For the rig, an A-pose, hands empty (the hatchet is a held piece).
if REST:
    poses = [(-1, "r", Vector((-0.58, -0.05, 1.08)), Vector((-0.74, -0.08, 0.86))),
             (1, "l", Vector((0.58, -0.05, 1.08)), Vector((0.74, -0.08, 0.86)))]
else:
    poses = [(-1, "r", Vector((-0.6, 0.12, 1.42)), Vector((-0.5, 0.0, 1.78))),
             (1, "l", Vector((0.58, -0.3, 1.2)), Vector((0.62, -0.6, 1.16)))]
for side, tag, el, wr in poses:
    sh = Vector((0.38 * side, -0.02, 1.28))
    ball("shoulder_" + tag, sh, (0.16, 0.15, 0.15), "skin")
    limb("upper_" + tag, sh, el, 0.12, 0.1, "skin")
    limb("fore_" + tag, el, wr, 0.11, 0.085, "skin")
    ball("fist_" + tag, wr + (wr - el).normalized() * 0.05, (0.085, 0.085, 0.085), "skin")
    d = (wr - el).normalized()
    limb("bracer_" + tag, el.lerp(wr, 0.45), el.lerp(wr, 0.85), 0.1, 0.09, "leather")
    if not REST and tag == "r":
        hatchet(wr + d * 0.05, Vector((0.25, 0.15, 1)), Vector((-1, -0.3, 0.2)), 1.0, "hatchet_hand")

present("stump_hauler", target=Vector((0, 0.0, 1.12)), size=2.75)
