# Act 1's undead (2026-10-04), built in Blender from code on existing Mixamo rigs like the Cinder Warden (boss.py):
# the skeleton on the husk's rig (it moves with the husk's zombie idle, run, attack, hit and death) and the cultist on
# the bandit archer's (its draw is the cultist's cast). Each part is a solid weighted wholly to one bone. Writes
# Assets/_Project/Art/Models/Enemies/<name>/ with <name>.fbx, <name>_albedo.png and the donor's clips copied under the
# new name; EnemyBakeSetup.ClipAvatarFrom gives those clips the donor's avatar. Then Bake Enemies.
#   Blender -b -P ArtSource/tools/props/undead.py -- skeleton|cultist
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh, mathutils
import numpy as np
from kit import STONE, WOOD, BLOOD, EMBER, BONE, COLD, MOSS, VIOLET, linear

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
ENEMIES = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Enemies")
NAME = sys.argv[sys.argv.index("--") + 1]
DONORS = {"skeleton": "husk", "cultist": "bandit_archer", "cutthroat": "husk", "ember_acolyte": "bandit_archer",
          "pyre_keeper": "bandit_archer", "carrion_bloat": "ghoul"}
DONOR = DONORS[NAME]
OUT = os.path.join(ENEMIES, NAME)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(ENEMIES, DONOR, DONOR + ".fbx"))
arm = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
    bpy.data.objects.remove(o, do_unlink=True)
bones = arm.data.bones

COLORS = {
    "bone": BONE[1], "bone_dark": BONE[0], "rust": WOOD[2], "iron": STONE[2], "eye": COLD[4],
    "robe": BLOOD[1], "robe_dark": STONE[0], "trim": EMBER[3], "skin": (102, 68, 52), "fire": EMBER[4],
    "hood": STONE[1],
    # The rest of act 1's roster (2026-10-05).
    "leather": WOOD[2], "leather_dark": WOOD[1], "scarf": BLOOD[2], "steel": STONE[4], "ash": STONE[3],
    "ash_dark": STONE[2], "ember": EMBER[2], "flesh": (118, 112, 84), "flesh_dark": (78, 74, 54), "rot": MOSS[2],
    "pus": (150, 142, 88),
}
color_list = list(COLORS)
bm = bmesh.new()
deform = bm.verts.layers.deform.verify()
face_color = []
groups = {}

def head(name):
    return mathutils.Vector(bones["mixamorig:" + name].head_local)


def tail(name):
    return mathutils.Vector(bones["mixamorig:" + name].tail_local)



def group(bone):
    if bone not in groups:
        groups[bone] = len(groups)
    return groups[bone]


def add(geom_verts, faces, bone, color):
    g = group("mixamorig:" + bone)
    vs = []
    for co in geom_verts:
        v = bm.verts.new(co)
        v[deform][g] = 1.0
        vs.append(v)
    for f in faces:
        try:
            bm.faces.new([vs[i] for i in f])
            face_color.append(color_list.index(color))
        except ValueError:
            pass


def frame(direction):
    d = direction.normalized()
    up = mathutils.Vector((0, 0, 1)) if abs(d.z) < 0.9 else mathutils.Vector((1, 0, 0))
    a = d.cross(up).normalized()
    b = d.cross(a).normalized()
    return a, b


def limb(p0, p1, r0, r1, bone, color, sides=6):
    """A tapered prism from p0 to p1."""
    a, b = frame(p1 - p0)
    verts = []
    for p, r in ((p0, r0), (p1, r1)):
        for k in range(sides):
            t = 2 * math.pi * k / sides
            verts.append(p + (a * math.cos(t) + b * math.sin(t)) * r)
    faces = [[k, (k + 1) % sides, sides + (k + 1) % sides, sides + k] for k in range(sides)]
    faces.append(list(reversed(range(sides))))
    faces.append(list(range(sides, 2 * sides)))
    add(verts, faces, bone, color)


def block(centre, size, bone, color, rot=None):
    """A box of size (x, y, z) about centre, turned by rot (a Matrix) if given."""
    sx, sy, sz = (s / 2 for s in size)
    corners = [mathutils.Vector((x, y, z)) for x in (-sx, sx) for y in (-sy, sy) for z in (-sz, sz)]
    if rot is not None:
        corners = [rot @ c for c in corners]
    verts = [centre + c for c in corners]
    faces = [[0, 1, 3, 2], [4, 6, 7, 5], [0, 4, 5, 1], [2, 3, 7, 6], [0, 2, 6, 4], [1, 5, 7, 3]]
    add(verts, faces, bone, color)


def blob(centre, radii, bone, color, segs=8):
    verts = []
    rings = segs // 2
    for i in range(rings + 1):
        phi = math.pi * i / rings
        for k in range(segs):
            t = 2 * math.pi * k / segs
            verts.append(centre + mathutils.Vector((math.sin(phi) * math.cos(t) * radii[0], math.cos(phi) * radii[1], math.sin(phi) * math.sin(t) * radii[2])))
    faces = []
    for i in range(rings):
        for k in range(segs):
            j = (k + 1) % segs
            faces.append([i * segs + k, i * segs + j, (i + 1) * segs + j, (i + 1) * segs + k])
    add(verts, faces, bone, color)


V = mathutils.Vector

V = mathutils.Vector
top = head("HeadTop_End")
toe = head("LeftToe_End")
U = (top.y - toe.y) / 1.8   # about a metre in this rig's units


def mid(a, b, f=0.5):
    return head(a) + (head(b) - head(a)) * f


def skeleton():
    # Bare bones: a skull with glowing eyes, a ribcage, thin limbs with knobbed joints, a rusted blade in the right hand
    # and a dented pauldron.
    b = 0.035 * U
    h = head("Head")
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.095 * U, 0.11 * U, 0.1 * U), "Head", "bone")
    block(h + V((0, 0.03 * U, 0.06 * U)), (0.1 * U, 0.05 * U, 0.06 * U), "Head", "bone_dark")
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.11 * U, 0.085 * U)), (0.03 * U, 0.025 * U, 0.02 * U), "Head", "eye")
    limb(head("Neck"), head("Head"), 0.025 * U, 0.025 * U, "Neck", "bone_dark")
    limb(head("Hips"), head("Spine2"), 0.03 * U, 0.03 * U, "Spine", "bone")
    s2 = head("Spine2")
    for k in range(4):
        y = s2.y - k * 0.06 * U + 0.06 * U
        block(V((0, y, 0.02 * U)), (0.26 * U - k * 0.02 * U, 0.025 * U, 0.17 * U), "Spine2" if k < 2 else "Spine1", "bone")
    block(V((0, head("Hips").y, 0)), (0.22 * U, 0.08 * U, 0.12 * U), "Hips", "bone_dark")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), b, b * 0.8, side + "Arm", "bone")
        limb(head(side + "ForeArm"), head(side + "Hand"), b * 0.8, b * 0.7, side + "ForeArm", "bone")
        blob(head(side + "ForeArm"), (b * 1.4, b * 1.4, b * 1.4), side + "Arm", "bone_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.5, (b * 1.3, b * 1.0, b * 1.2), side + "Hand", "bone")
        limb(head(side + "UpLeg"), head(side + "Leg"), b * 1.1, b * 0.9, side + "UpLeg", "bone")
        limb(head(side + "Leg"), head(side + "Foot"), b * 0.9, b * 0.7, side + "Leg", "bone")
        blob(head(side + "Leg"), (b * 1.5, b * 1.5, b * 1.5), side + "Leg", "bone_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.07 * U, 0.04 * U, 0.16 * U), side + "Foot", "bone")
    # A rusted blade in the right hand, pointing along the hand.
    hand = head("RightHand")
    d = (tail("RightHand") - hand).normalized()
    limb(hand, hand + d * 0.75 * U, 0.03 * U, 0.008 * U, "RightHand", "rust", 4)
    block(hand + d * 0.04 * U, (0.16 * U, 0.03 * U, 0.03 * U), "RightHand", "iron")
    blob(head("LeftArm") + V((0, 0.03 * U, 0)), (0.09 * U, 0.05 * U, 0.08 * U), "LeftArm", "iron")


def cultist():
    # A hooded robe to the ankles, a dark face in the hood with two embers for eyes, sleeves, a bone mask, a staff
    # topped with fire in the left hand (the hand that draws the bow).
    h = head("Head")
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.12 * U, 0.14 * U, 0.12 * U), "Head", "hood")
    blob(h + V((0, 0.08 * U, 0.04 * U)), (0.08 * U, 0.09 * U, 0.07 * U), "Head", "robe_dark")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.1 * U, 0.11 * U)), (0.025 * U, 0.02 * U, 0.02 * U), "Head", "fire")
    block(h + V((0, 0.05 * U, 0.11 * U)), (0.07 * U, 0.04 * U, 0.02 * U), "Head", "bone")
    # The torso and the robe's upper part.
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.32 * U, 0.2 * U, 0.2 * U), "Spine2", "robe")
    block(V((0, head("Spine1").y, 0)), (0.28 * U, 0.12 * U, 0.19 * U), "Spine1", "robe")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U, 0.12 * U, 0.19 * U), "Spine", "trim")
    # The skirt: wide panels down each thigh and shin, so the legs' swing shows under the robe.
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.12 * U, side + "UpLeg", "robe")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.14 * U, side + "Leg", "robe_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U, 0.05 * U, 0.15 * U), side + "Foot", "robe_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.06 * U, 0.06 * U, side + "Arm", "robe")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.08 * U, side + "ForeArm", "robe")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.04 * U, 0.04 * U, 0.04 * U), side + "Hand", "skin")
    # The staff: upright through the left fist, a fire on top.
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.8 * U, 0)), hand + V((0, 0.7 * U, 0)), 0.02 * U, 0.02 * U, "LeftHand", "rust", 5)
    blob(hand + V((0, 0.78 * U, 0)), (0.06 * U, 0.08 * U, 0.06 * U), "LeftHand", "fire")
    block(hand + V((0, 0.68 * U, 0)), (0.1 * U, 0.03 * U, 0.03 * U), "LeftHand", "bone_dark")


def cutthroat():
    # A lean bandit in dark leather, a red scarf over the face, a hood, and a long knife in each hand.
    h = head("Head")
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.105 * U, 0.12 * U, 0.105 * U), "Head", "leather_dark")
    block(h + V((0, 0.06 * U, 0.075 * U)), (0.13 * U, 0.06 * U, 0.05 * U), "Head", "scarf")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.11 * U, 0.09 * U)), (0.025 * U, 0.015 * U, 0.02 * U), "Head", "skin")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.28 * U, 0.18 * U, 0.17 * U), "Spine2", "leather")
    block(V((0, head("Spine1").y, 0)), (0.25 * U, 0.12 * U, 0.16 * U), "Spine1", "leather_dark")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.26 * U, 0.06 * U, 0.17 * U), "Spine", "scarf")
    block(V((0, head("Hips").y, 0)), (0.26 * U, 0.1 * U, 0.16 * U), "Hips", "leather_dark")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.045 * U, 0.04 * U, side + "Arm", "leather")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.04 * U, 0.035 * U, side + "ForeArm", "leather_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.035 * U, 0.035 * U, 0.035 * U), side + "Hand", "skin")
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.06 * U, 0.05 * U, side + "UpLeg", "leather_dark")
        limb(head(side + "Leg"), head(side + "Foot"), 0.05 * U, 0.04 * U, side + "Leg", "leather")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.07 * U, 0.05 * U, 0.15 * U), side + "Foot", "leather_dark")
        hand = head(side + "Hand")
        d = (tail(side + "Hand") - hand).normalized()
        limb(hand, hand + d * 0.38 * U, 0.022 * U, 0.004 * U, side + "Hand", "steel", 4)
        block(hand + d * 0.02 * U, (0.08 * U, 0.025 * U, 0.025 * U), side + "Hand", "rust")


def ember_acolyte():
    # An ash-grey robe with ember-orange trim, a bare head with a shaved scalp and glowing eyes, a smoking censer staff.
    h = head("Head")
    blob(h + V((0, 0.1 * U, 0)), (0.09 * U, 0.11 * U, 0.095 * U), "Head", "skin")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.1 * U, 0.085 * U)), (0.025 * U, 0.018 * U, 0.02 * U), "Head", "fire")
    blob(h + V((0, 0.0, -0.02 * U)), (0.14 * U, 0.05 * U, 0.12 * U), "Neck", "ember")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.3 * U, 0.2 * U, 0.19 * U), "Spine2", "ash")
    block(V((0, head("Spine1").y, 0)), (0.27 * U, 0.12 * U, 0.18 * U), "Spine1", "ash")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U, 0.06 * U, 0.19 * U), "Spine", "ember")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.12 * U, side + "UpLeg", "ash")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.14 * U, side + "Leg", "ash_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U, 0.05 * U, 0.15 * U), side + "Foot", "robe_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.06 * U, 0.06 * U, side + "Arm", "ash")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.085 * U, side + "ForeArm", "ember")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.04 * U, 0.04 * U, 0.04 * U), side + "Hand", "skin")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.7 * U, 0)), hand + V((0, 0.6 * U, 0)), 0.018 * U, 0.018 * U, "LeftHand", "iron", 5)
    blob(hand + V((0, 0.66 * U, 0)), (0.06 * U, 0.06 * U, 0.06 * U), "LeftHand", "iron")
    blob(hand + V((0, 0.7 * U, 0)), (0.04 * U, 0.05 * U, 0.04 * U), "LeftHand", "fire")


def pyre_keeper():
    # A tall, broad keeper in a heavy dark robe and a cowl, a tall pole topped with a burning iron brazier.
    h = head("Head")
    blob(h + V((0, 0.11 * U, -0.01 * U)), (0.13 * U, 0.15 * U, 0.13 * U), "Head", "robe_dark")
    blob(h + V((0, 0.08 * U, 0.05 * U)), (0.07 * U, 0.08 * U, 0.06 * U), "Head", "hood")
    block(h + V((0, 0.08 * U, 0.115 * U)), (0.08 * U, 0.06 * U, 0.02 * U), "Head", "iron")
    for sx in (-1, 1):
        block(h + V((sx * 0.025 * U, 0.09 * U, 0.127 * U)), (0.02 * U, 0.012 * U, 0.01 * U), "Head", "fire")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.38 * U, 0.22 * U, 0.24 * U), "Spine2", "robe")
    blob(V((0, s2.y + 0.1 * U, 0)), (0.24 * U, 0.06 * U, 0.15 * U), "Spine2", "robe_dark")
    block(V((0, head("Spine1").y, 0)), (0.33 * U, 0.12 * U, 0.22 * U), "Spine1", "robe")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.33 * U, 0.06 * U, 0.23 * U), "Spine", "iron")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.13 * U, 0.14 * U, side + "UpLeg", "robe")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.14 * U, 0.16 * U, side + "Leg", "robe_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.09 * U, 0.05 * U, 0.16 * U), side + "Foot", "robe_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.075 * U, 0.075 * U, side + "Arm", "robe")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.075 * U, 0.09 * U, side + "ForeArm", "robe_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.045 * U, 0.045 * U, 0.045 * U), side + "Hand", "skin")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.85 * U, 0)), hand + V((0, 0.9 * U, 0)), 0.025 * U, 0.025 * U, "LeftHand", "rust", 5)
    top = hand + V((0, 0.9 * U, 0))
    for k in range(4):
        a = k * math.pi / 2
        limb(top, top + V((math.cos(a) * 0.1 * U, 0.12 * U, math.sin(a) * 0.1 * U)), 0.012 * U, 0.012 * U, "LeftHand", "iron", 4)
    blob(top + V((0, 0.1 * U, 0)), (0.1 * U, 0.04 * U, 0.1 * U), "LeftHand", "iron")
    blob(top + V((0, 0.17 * U, 0)), (0.08 * U, 0.1 * U, 0.08 * U), "LeftHand", "fire")
    blob(top + V((0, 0.13 * U, 0)), (0.09 * U, 0.05 * U, 0.09 * U), "LeftHand", "ember")


def carrion_bloat():
    # A swollen corpse: a huge belly sagging over short legs, small arms, a lolling head, pale rotting skin with dark
    # patches and pus boils.
    h = head("Head")
    blob(h + V((0, 0.06 * U, 0.04 * U)), (0.1 * U, 0.1 * U, 0.1 * U), "Head", "flesh")
    block(h + V((0, 0.02 * U, 0.12 * U)), (0.07 * U, 0.03 * U, 0.02 * U), "Head", "robe_dark")
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.08 * U, 0.12 * U)), (0.02 * U, 0.02 * U, 0.02 * U), "Head", "pus")
    s1 = head("Spine1")
    blob(V((0, s1.y, 0.06 * U)), (0.34 * U, 0.36 * U, 0.34 * U), "Spine1", "flesh", 12)
    blob(V((0.12 * U, s1.y + 0.1 * U, 0.3 * U)), (0.12 * U, 0.1 * U, 0.08 * U), "Spine1", "flesh_dark")
    blob(V((-0.16 * U, s1.y - 0.12 * U, 0.26 * U)), (0.1 * U, 0.09 * U, 0.08 * U), "Spine1", "rot")
    blob(head("Spine2") + V((0, 0.05 * U, 0)), (0.3 * U, 0.16 * U, 0.26 * U), "Spine2", "flesh_dark")
    for (x, y, z, r) in ((0.2, 0.05, 0.28, 0.05), (-0.05, -0.15, 0.36, 0.045), (-0.25, 0.12, 0.18, 0.04), (0.28, -0.1, 0.1, 0.04)):
        blob(V((x * U, s1.y + y * U, z * U)), (r * U, r * U, r * U), "Spine1", "pus")
    block(V((0, head("Hips").y, 0)), (0.36 * U, 0.14 * U, 0.26 * U), "Hips", "flesh_dark")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.07 * U, 0.06 * U, side + "Arm", "flesh")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.05 * U, side + "ForeArm", "flesh_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.06 * U, 0.05 * U, 0.06 * U), side + "Hand", "flesh")
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.12 * U, 0.1 * U, side + "UpLeg", "flesh")
        limb(head(side + "Leg"), head(side + "Foot"), 0.1 * U, 0.08 * U, side + "Leg", "flesh_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.1 * U, 0.06 * U, 0.17 * U), side + "Foot", "flesh_dark")


{"skeleton": skeleton, "cultist": cultist, "cutthroat": cutthroat, "ember_acolyte": ember_acolyte,
 "pyre_keeper": pyre_keeper, "carrion_bloat": carrion_bloat}[NAME]()

mesh = bpy.data.meshes.new(NAME)
bm.normal_update()
bm.to_mesh(mesh)
bm.free()
obj = bpy.data.objects.new(NAME, mesh)
bpy.context.scene.collection.objects.link(obj)
for name, index in sorted(groups.items(), key=lambda p: p[1]):
    obj.vertex_groups.new(name=name)
obj.parent = arm
obj.matrix_parent_inverse = mathutils.Matrix.Identity(4)
mod = obj.modifiers.new("Armature", "ARMATURE")
mod.object = arm

# The atlas: one 8 px square a colour, every face mapped to its colour's middle.
cells = int(math.ceil(math.sqrt(len(color_list))))
size = cells * 8
pixels = np.zeros((size, size, 4), dtype=np.float32)
pixels[:, :, 3] = 1
for i, key in enumerate(color_list):
    cx, cy = i % cells, i // cells
    pixels[cy * 8:(cy + 1) * 8, cx * 8:(cx + 1) * 8, :3] = np.array(COLORS[key]) / 255.0
os.makedirs(OUT, exist_ok=True)
albedo = os.path.join(OUT, NAME + "_albedo.png")
image = bpy.data.images.new("atlas", size, size, alpha=True)
image.colorspace_settings.name = "Non-Color"
image.pixels = pixels.ravel()
image.filepath_raw = albedo
image.file_format = "PNG"
image.save()
uv = mesh.uv_layers.new(name="UV").data
for poly, c in zip(mesh.polygons, face_color):
    u, v = (c % cells + 0.5) / cells, (c // cells + 0.5) / cells
    for li in poly.loop_indices:
        uv[li].uv = (u, v)
material = bpy.data.materials.new(NAME)
material.use_nodes = True
tex = material.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = bpy.data.images.load(albedo)
material.node_tree.links.new(tex.outputs["Color"], material.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
mesh.materials.append(material)

bpy.ops.object.select_all(action="DESELECT")
for o in bpy.context.scene.objects:
    o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, NAME + ".fbx"), use_selection=True, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=False, path_mode="STRIP", embed_textures=False)
# The idle is the ghoul's own (EnemyBakeSetup takes it from ghoul.fbx): the body's exported animation does not come
# through as a clip the bake finds, and a copy of ghoul.fbx as a clip file drew T-pose frames.
for clip in ("run", "attack", "hit", "death"):
    source = os.path.join(ENEMIES, DONOR, "%s_%s.fbx" % (DONOR, clip))
    shutil.copyfile(source, os.path.join(OUT, "%s_%s.fbx" % (NAME, clip)))
print("REPORT wrote", OUT, len(mesh.polygons), "faces,", len(groups), "bones")
