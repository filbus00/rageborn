# The Ash Wolf (2026-10-05, act 1's charger, Docs/05): a four-legged body on a rig of its own, built in Blender from
# code with its five clips keyed in code, since no Mixamo rig fits a wolf. Ash-grey fur, a darker back and ember eyes.
# Every part is a solid weighted wholly to one bone. The clips (24 a second): idle (a 2 s loop of breathing, a slow tail
# and a look round), run (a 0.5 s gallop), attack (a crouch, a spring and a snapping bite, 0.9 s), hit (a flinch, 0.35 s)
# and death (it falls on its side, 1 s). Writes Assets/_Project/Art/Models/Enemies/ash_wolf/ash_wolf.fbx (all clips in
# the one file, a generic rig: EnemyBakeSetup.GenericBodies) and ash_wolf_albedo.png. Then Bake Enemies.
#   Blender -b -P ArtSource/tools/props/wolf.py
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh, mathutils
import numpy as np
from kit import STONE, EMBER, BLOOD, BONE

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
# The concept roster's beasts (2026-10-13) are built on the same rig and clips: hell_hound, nightmare, quill_rat, rat,
# arachnid, sand_worm (Blender -b -P wolf.py -- <name>).
NAME = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv and len(sys.argv) > sys.argv.index("--") + 1 else "ash_wolf"
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Enemies", NAME)
FPS = 24
V = mathutils.Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS

# ---------------------------------------------------------------- the rig (the wolf faces -Y, which is Unity's +Z)

BONES = {
    # name: (head, tail, parent)
    "root": ((0, 0, 0), (0, 0, 0.2), None),
    "hips": ((0, 0.3, 0.6), (0, 0.0, 0.62), "root"),
    "chest": ((0, 0.0, 0.62), (0, -0.3, 0.66), "hips"),
    "neck": ((0, -0.3, 0.66), (0, -0.44, 0.8), "chest"),
    "head": ((0, -0.44, 0.8), (0, -0.7, 0.77), "neck"),
    "jaw": ((0, -0.5, 0.73), (0, -0.72, 0.69), "head"),
    "tail1": ((0, 0.32, 0.62), (0, 0.55, 0.56), "hips"),
    "tail2": ((0, 0.55, 0.56), (0, 0.76, 0.42), "tail1"),
}
for side, x in (("L", 0.11), ("R", -0.11)):
    BONES["front_upper_" + side] = ((x, -0.22, 0.56), (x, -0.25, 0.31), "chest")
    BONES["front_lower_" + side] = ((x, -0.25, 0.31), (x, -0.23, 0.06), "front_upper_" + side)
    BONES["front_paw_" + side] = ((x, -0.23, 0.06), (x, -0.31, 0.02), "front_lower_" + side)
    BONES["hind_upper_" + side] = ((x, 0.27, 0.56), (x, 0.36, 0.33), "hips")
    BONES["hind_lower_" + side] = ((x, 0.36, 0.33), (x, 0.3, 0.07), "hind_upper_" + side)
    BONES["hind_paw_" + side] = ((x, 0.3, 0.07), (x, 0.23, 0.02), "hind_lower_" + side)

arm_data = bpy.data.armatures.new("Armature")
arm = bpy.data.objects.new("Armature", arm_data)
scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
for name, (h, t, parent) in BONES.items():
    eb = arm_data.edit_bones.new(name)
    eb.head, eb.tail = h, t
for name, (h, t, parent) in BONES.items():
    if parent:
        arm_data.edit_bones[name].parent = arm_data.edit_bones[parent]
        arm_data.edit_bones[name].use_connect = False
bpy.ops.object.mode_set(mode="OBJECT")

# ---------------------------------------------------------------- the body

PALETTES = {
    "ash_wolf": {"fur": STONE[3], "fur_dark": STONE[1], "belly": STONE[4], "eye": EMBER[3], "mouth": BLOOD[1],
                 "teeth": BONE[2], "ember": EMBER[1], "nose": STONE[0]},
    "hell_hound": {"fur": (130, 34, 24), "fur_dark": (60, 16, 14), "belly": (96, 44, 30), "eye": EMBER[4], "mouth": (40, 10, 8),
                   "teeth": BONE[2], "ember": EMBER[3], "nose": STONE[0]},
    "nightmare": {"fur": (34, 30, 32), "fur_dark": (20, 18, 20), "belly": (52, 46, 46), "eye": EMBER[4], "mouth": BLOOD[0],
                  "teeth": BONE[1], "ember": EMBER[3], "nose": STONE[0]},
    "quill_rat": {"fur": (110, 84, 56), "fur_dark": (70, 52, 34), "belly": (150, 120, 90), "eye": BLOOD[3], "mouth": BLOOD[1],
                  "teeth": BONE[2], "ember": BONE[1], "nose": (170, 110, 100)},
    # The rat (2026-10-25, the owner: "add enemies: rats"): a plain sewer rat, grey-brown with pink ears, feet and tail.
    # Dark, so it stands out on the crypt's grey-brown cobbles (2026-10-25); bright pink skin.
    "rat": {"fur": (54, 44, 42), "fur_dark": (28, 22, 22), "belly": (110, 96, 86), "eye": BLOOD[4], "mouth": BLOOD[1],
            "teeth": (232, 200, 120), "ember": (70, 62, 56), "nose": (214, 128, 122), "ear": (232, 150, 140),
            "claw": BONE[2], "mid": (42, 34, 32)},
    "arachnid": {"fur": (80, 26, 34), "fur_dark": (46, 16, 30), "belly": (110, 50, 70), "eye": BLOOD[4], "mouth": BLOOD[0],
                 "teeth": BONE[1], "ember": (90, 40, 110), "nose": STONE[0]},
    "sand_worm": {"fur": (120, 88, 56), "fur_dark": (84, 60, 38), "belly": (160, 120, 84), "eye": BLOOD[3], "mouth": (50, 14, 12),
                  "teeth": BONE[2], "ember": BONE[1], "nose": STONE[0]},
}
COLORS = PALETTES[NAME]
color_list = list(COLORS)
bm = bmesh.new()
deform = bm.verts.layers.deform.verify()
face_color = []
groups = {}


def group(bone):
    if bone not in groups:
        groups[bone] = len(groups)
    return groups[bone]


def add(verts, faces, bone, color):
    g = group(bone)
    vs = []
    for co in verts:
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
    up = V((0, 0, 1)) if abs(d.z) < 0.9 else V((1, 0, 0))
    a = d.cross(up).normalized()
    return a, d.cross(a).normalized()


def limb(p0, p1, r0, r1, bone, color, sides=6):
    p0, p1 = V(p0), V(p1)
    a, b = frame(p1 - p0)
    verts = []
    for p, r in ((p0, r0), (p1, r1)):
        for k in range(sides):
            t = 2 * math.pi * k / sides
            verts.append(p + (a * math.cos(t) + b * math.sin(t)) * r)
    faces = [[k, (k + 1) % sides, sides + (k + 1) % sides, sides + k] for k in range(sides)]
    faces += [list(reversed(range(sides))), list(range(sides, 2 * sides))]
    add(verts, faces, bone, color)


def blob(centre, radii, bone, color, segs=10):
    centre = V(centre)
    verts = []
    rings = segs // 2
    for i in range(rings + 1):
        phi = math.pi * i / rings
        for k in range(segs):
            t = 2 * math.pi * k / segs
            verts.append(centre + V((math.sin(phi) * math.cos(t) * radii[0], math.sin(phi) * math.sin(t) * radii[1], math.cos(phi) * radii[2])))
    faces = []
    for i in range(rings):
        for k in range(segs):
            j = (k + 1) % segs
            faces.append([i * segs + k, i * segs + j, (i + 1) * segs + j, (i + 1) * segs + k])
    add(verts, faces, bone, color)


def block(centre, size, bone, color):
    c = V(centre)
    sx, sy, sz = (s / 2 for s in size)
    verts = [c + V((x, y, z)) for x in (-sx, sx) for y in (-sy, sy) for z in (-sz, sz)]
    faces = [[0, 1, 3, 2], [4, 6, 7, 5], [0, 4, 5, 1], [2, 3, 7, 6], [0, 2, 6, 4], [1, 5, 7, 3]]
    add(verts, faces, bone, color)


def ash_wolf():
    # Torso: a deep chest, a narrower waist and haunches, a dark ridge of fur along the back.
    blob((0, -0.14, 0.62), (0.15, 0.24, 0.17), "chest", "fur")
    blob((0, -0.12, 0.52), (0.11, 0.2, 0.08), "chest", "belly")
    blob((0, 0.18, 0.62), (0.12, 0.2, 0.13), "hips", "fur")
    blob((0, -0.05, 0.76), (0.07, 0.36, 0.05), "chest", "fur_dark")
    blob((0, 0.22, 0.73), (0.06, 0.14, 0.04), "hips", "fur_dark")
    # Neck with a ruff, the head, snout, ears, eyes, nose, jaw and teeth.
    blob((0, -0.36, 0.72), (0.12, 0.1, 0.12), "neck", "fur_dark")
    blob((0, -0.5, 0.82), (0.1, 0.1, 0.09), "head", "fur")
    block((0, -0.64, 0.78), (0.09, 0.16, 0.07), "head", "fur")
    block((0, -0.72, 0.8), (0.04, 0.03, 0.03), "head", "nose")
    for x in (-0.055, 0.055):
        limb((x, -0.46, 0.88), (x * 1.3, -0.44, 1.0), 0.035, 0.005, "head", "fur_dark", 4)
        block((x, -0.585, 0.84), (0.025, 0.012, 0.018), "head", "eye")
    block((0, -0.62, 0.725), (0.075, 0.15, 0.03), "jaw", "fur")
    block((0, -0.62, 0.745), (0.06, 0.12, 0.012), "jaw", "mouth")
    block((0, -0.66, 0.755), (0.065, 0.06, 0.012), "head", "teeth")
    # Tail, bushy, darker at the tip with a few embers caught in it.
    limb((0, 0.32, 0.62), (0, 0.55, 0.56), 0.05, 0.07, "tail1", "fur")
    limb((0, 0.55, 0.56), (0, 0.76, 0.42), 0.07, 0.03, "tail2", "fur_dark")
    block((0.02, 0.66, 0.5), (0.02, 0.02, 0.02), "tail2", "ember")
    # Legs: thick upper parts, thin lower parts, paws.
    for side, x in (("L", 0.11), ("R", -0.11)):
        limb((x, -0.22, 0.58), (x, -0.25, 0.31), 0.065, 0.04, "front_upper_" + side, "fur")
        limb((x, -0.25, 0.31), (x, -0.23, 0.06), 0.035, 0.03, "front_lower_" + side, "fur")
        block((x, -0.27, 0.035), (0.06, 0.1, 0.05), "front_paw_" + side, "fur_dark")
        limb((x, 0.25, 0.6), (x, 0.36, 0.33), 0.08, 0.045, "hind_upper_" + side, "fur")
        limb((x, 0.36, 0.33), (x, 0.3, 0.07), 0.04, 0.03, "hind_lower_" + side, "fur")
        block((x, 0.26, 0.035), (0.06, 0.1, 0.05), "hind_paw_" + side, "fur_dark")



def hell_hound():
    # A hell hound: the wolf's frame in charred red hide, spikes down its back, a burning tail.
    ash_wolf()
    for k in range(6):
        y = -0.25 + k * 0.1
        limb((0, y, 0.74), (0, y + 0.03, 0.86 - abs(k - 2.5) * 0.015), 0.025, 0.004, "chest" if y < 0.05 else "hips", "fur_dark", 4)
    for k in range(3):
        blob((0.0, 0.7 + 0.04 * k, 0.48 - 0.05 * k), (0.035, 0.035, 0.05), "tail2", "ember")


def nightmare():
    # A nightmare: a black horse-like beast, a long head, a burning mane and tail, fire at its hooves.
    blob((0, -0.12, 0.64), (0.17, 0.3, 0.18), "chest", "fur")
    blob((0, 0.2, 0.64), (0.15, 0.22, 0.16), "hips", "fur")
    limb((0, -0.3, 0.7), (0, -0.48, 0.92), 0.09, 0.07, "neck", "fur")
    block((0, -0.62, 0.9), (0.09, 0.26, 0.1), "head", "fur")
    block((0, -0.74, 0.86), (0.08, 0.06, 0.08), "head", "fur_dark")
    for x in (-0.045, 0.045):
        block((x, -0.56, 0.95), (0.02, 0.02, 0.02), "head", "eye")
        limb((x, -0.5, 0.96), (x, -0.48, 1.06), 0.02, 0.004, "head", "fur_dark", 4)
    for k in range(5):
        blob((0, -0.3 - 0.04 * k, 0.8 + 0.04 * k), (0.03, 0.05, 0.06), "neck", "ember")
    limb((0, 0.38, 0.66), (0, 0.6, 0.5), 0.05, 0.05, "tail1", "ember")
    limb((0, 0.6, 0.5), (0, 0.76, 0.32), 0.05, 0.02, "tail2", "eye")
    for side, x in (("L", 0.11), ("R", -0.11)):
        limb((x, -0.22, 0.6), (x, -0.25, 0.31), 0.06, 0.04, "front_upper_" + side, "fur")
        limb((x, -0.25, 0.31), (x, -0.23, 0.06), 0.035, 0.035, "front_lower_" + side, "fur")
        block((x, -0.24, 0.03), (0.07, 0.08, 0.06), "front_paw_" + side, "ember")
        limb((x, 0.25, 0.62), (x, 0.36, 0.33), 0.07, 0.045, "hind_upper_" + side, "fur")
        limb((x, 0.36, 0.33), (x, 0.3, 0.07), 0.04, 0.035, "hind_lower_" + side, "fur")
        block((x, 0.28, 0.03), (0.07, 0.08, 0.06), "hind_paw_" + side, "ember")


def quill_rat():
    # A quill rat: a squat rodent body with a crest of long quills, a pointed snout, a bare tail.
    blob((0, -0.05, 0.5), (0.17, 0.32, 0.16), "chest", "fur")
    blob((0, 0.18, 0.5), (0.16, 0.2, 0.15), "hips", "fur")
    blob((0, -0.05, 0.42), (0.13, 0.26, 0.08), "chest", "belly")
    blob((0, -0.42, 0.56), (0.1, 0.12, 0.09), "head", "fur")
    block((0, -0.56, 0.53), (0.05, 0.12, 0.05), "head", "fur")
    block((0, -0.63, 0.53), (0.03, 0.02, 0.03), "head", "nose")
    block((0, -0.6, 0.5), (0.03, 0.02, 0.02), "jaw", "teeth")
    for x in (-0.045, 0.045):
        block((x, -0.5, 0.6), (0.02, 0.012, 0.02), "head", "eye")
        blob((x * 1.5, -0.38, 0.65), (0.035, 0.01, 0.035), "head", "nose")
    for k in range(14):
        y = -0.25 + (k // 2) * 0.08
        x = -0.06 if k % 2 else 0.06
        limb((x, y, 0.62), (x * 2.5, y + 0.2, 0.92 - abs(k - 7) * 0.015), 0.018, 0.003, "chest" if y < 0.05 else "hips", "ember" if k % 3 else "fur_dark", 4)
    limb((0, 0.32, 0.5), (0, 0.55, 0.42), 0.03, 0.02, "tail1", "nose")
    limb((0, 0.55, 0.42), (0, 0.78, 0.3), 0.02, 0.008, "tail2", "nose")
    for side, x in (("L", 0.11), ("R", -0.11)):
        limb((x, -0.22, 0.45), (x, -0.25, 0.31), 0.05, 0.035, "front_upper_" + side, "fur")
        limb((x, -0.25, 0.31), (x, -0.23, 0.06), 0.028, 0.022, "front_lower_" + side, "fur_dark")
        block((x, -0.26, 0.03), (0.05, 0.08, 0.04), "front_paw_" + side, "nose")
        limb((x, 0.25, 0.48), (x, 0.36, 0.33), 0.06, 0.04, "hind_upper_" + side, "fur")
        limb((x, 0.36, 0.33), (x, 0.3, 0.07), 0.03, 0.022, "hind_lower_" + side, "fur_dark")
        block((x, 0.27, 0.03), (0.05, 0.08, 0.04), "hind_paw_" + side, "nose")


def rat():
    # A big sewer rat (made nicer 2026-10-25): heavy haunches and a hunched back with a ragged dark stripe of fur, a
    # narrow chest, a long tapering head with yellow incisors, beady red eyes, round pink ears, a thick ringed tail.
    blob((0, 0.16, 0.42), (0.17, 0.23, 0.17), "hips", "fur", 16)
    blob((0, -0.1, 0.4), (0.13, 0.22, 0.14), "chest", "fur", 16)
    blob((0, 0.06, 0.34), (0.12, 0.3, 0.09), "chest", "belly", 14)
    blob((0, 0.04, 0.5), (0.1, 0.32, 0.09), "chest", "mid", 14)
    blob((0, 0.12, 0.555), (0.055, 0.22, 0.04), "hips", "fur_dark", 12)
    # Ragged tufts along the spine, swept back.
    for k in range(7):
        y = -0.22 + k * 0.07
        x = 0.025 if k % 2 else -0.025
        limb((x, y, 0.53 + 0.03 * math.sin(k * 0.9)), (x * 2, y + 0.11, 0.6 - abs(k - 3) * 0.012), 0.022, 0.003,
             "chest" if y < 0.05 else "hips", "fur_dark", 4)
    # The head: a rounded skull narrowing to a long snout, a pink nose, the incisors showing.
    blob((0, -0.34, 0.47), (0.095, 0.11, 0.09), "neck", "fur", 14)
    blob((0, -0.42, 0.48), (0.085, 0.1, 0.08), "head", "fur", 14)
    blob((0, -0.53, 0.45), (0.055, 0.1, 0.05), "head", "mid", 12)
    blob((0, -0.62, 0.45), (0.024, 0.024, 0.022), "head", "nose", 8)
    for x in (-0.012, 0.012):
        block((x, -0.6, 0.41), (0.016, 0.012, 0.04), "jaw", "teeth")
    blob((0, -0.54, 0.41), (0.04, 0.07, 0.022), "jaw", "belly", 10)
    for x in (-0.048, 0.048):
        blob((x, -0.47, 0.51), (0.02, 0.018, 0.02), "head", "eye", 8)
        blob((x * 1.45, -0.38, 0.575), (0.05, 0.014, 0.05), "head", "fur_dark", 10)
        blob((x * 1.45, -0.392, 0.575), (0.036, 0.008, 0.036), "head", "ear", 10)
        for k in range(3):
            limb((x * 0.5, -0.585, 0.45), (x * 3.4, -0.6 + 0.03 * k, 0.48 - 0.03 * k), 0.003, 0.0015, "head", "claw", 3)
    # A thick tail, pink and ringed, curling down behind.
    limb((0, 0.36, 0.42), (0, 0.6, 0.33), 0.04, 0.028, "tail1", "nose", 8)
    limb((0, 0.6, 0.33), (0.04, 0.84, 0.18), 0.028, 0.007, "tail2", "nose", 8)
    for k in range(4):
        t = (k + 1) / 5.0
        limb((0, 0.36 + 0.24 * t, 0.42 - 0.09 * t), (0, 0.37 + 0.24 * t, 0.42 - 0.09 * t), 0.042 - 0.012 * t, 0.042 - 0.012 * t,
             "tail1", "ear", 8)
    # Thin forelegs, big haunches, pink feet with pale claws.
    for side, x in (("L", 0.1), ("R", -0.1)):
        limb((x, -0.2, 0.38), (x, -0.25, 0.24), 0.045, 0.03, "front_upper_" + side, "fur", 8)
        limb((x, -0.25, 0.24), (x, -0.23, 0.05), 0.022, 0.016, "front_lower_" + side, "mid", 6)
        blob((x, -0.27, 0.025), (0.03, 0.045, 0.02), "front_paw_" + side, "nose", 8)
        blob((x * 1.15, 0.22, 0.38), (0.07, 0.12, 0.12), "hind_upper_" + side, "fur", 12)
        limb((x, 0.32, 0.3), (x, 0.3, 0.07), 0.03, 0.02, "hind_lower_" + side, "mid", 6)
        blob((x, 0.26, 0.025), (0.032, 0.065, 0.02), "hind_paw_" + side, "nose", 8)
        for paw, y in (("front_paw_", -0.31), ("hind_paw_", 0.2)):
            for dx in (-0.015, 0.015):
                limb((x + dx, y, 0.02), (x + dx, y - 0.02, 0.005), 0.005, 0.001, paw + side, "claw", 3)


def arachnid():
    # An arachnid: a small head and thorax, a big swollen abdomen behind, eight long jointed legs (two on each of the
    # rig's legs, the second splayed further), red eyes, fangs.
    blob((0, -0.15, 0.55), (0.14, 0.16, 0.1), "chest", "fur")
    blob((0, -0.35, 0.56), (0.09, 0.09, 0.08), "head", "fur_dark")
    for k in range(4):
        block(((k - 1.5) * 0.025, -0.43, 0.6), (0.015, 0.012, 0.015), "head", "eye")
    for x in (-0.03, 0.03):
        limb((x, -0.42, 0.52), (x * 1.2, -0.48, 0.44), 0.015, 0.003, "jaw", "teeth", 4)
    blob((0, 0.25, 0.62), (0.2, 0.28, 0.2), "hips", "fur")
    blob((0, 0.32, 0.72), (0.12, 0.16, 0.08), "hips", "ember")
    for side, sx in (("L", 1), ("R", -1)):
        for part, base_y in (("front", -0.22), ("hind", 0.22)):
            for spread in (0.0, 1.0):
                y = base_y + (0.1 if part == "front" else -0.1) * spread - (0.06 if part == "front" else -0.06)
                knee = (sx * (0.3 + 0.1 * spread), y - 0.03 * (1 if part == "front" else -1), 0.72)
                foot = (sx * (0.42 + 0.12 * spread), y - 0.08 * (1 if part == "front" else -1), 0.02)
                limb((sx * 0.08, y, 0.56), knee, 0.025, 0.02, part + "_upper_" + side, "fur_dark", 5)
                limb(knee, foot, 0.02, 0.008, part + "_lower_" + side, "fur", 5)


def sand_worm():
    # A sand worm: a thick segmented body rising from the ground, a round maw ringed with teeth; no legs.
    segments = [((0, 0.7, 0.18), "tail2", 0.13), ((0, 0.5, 0.3), "tail1", 0.17), ((0, 0.25, 0.45), "hips", 0.2),
                ((0, 0.0, 0.58), "chest", 0.21), ((0, -0.25, 0.72), "chest", 0.2), ((0, -0.42, 0.84), "neck", 0.18)]
    for i, (c, bone, r) in enumerate(segments):
        blob(c, (r, r * 0.8, r), bone, "fur" if i % 2 else "fur_dark")
        blob((c[0], c[1], c[2] - r * 0.4), (r * 0.8, r * 0.7, r * 0.5), bone, "belly")
    blob((0, -0.56, 0.9), (0.17, 0.12, 0.17), "head", "fur")
    blob((0, -0.66, 0.9), (0.11, 0.04, 0.11), "head", "mouth")
    for k in range(10):
        a = 2 * math.pi * k / 10
        limb((math.cos(a) * 0.12, -0.66, 0.9 + math.sin(a) * 0.12), (math.cos(a) * 0.06, -0.74, 0.9 + math.sin(a) * 0.06), 0.018, 0.003,
             "jaw" if math.sin(a) < 0 else "head", "teeth", 4)


{"ash_wolf": ash_wolf, "hell_hound": hell_hound, "nightmare": nightmare, "quill_rat": quill_rat, "rat": rat,
 "arachnid": arachnid, "sand_worm": sand_worm}[NAME]()

mesh = bpy.data.meshes.new(NAME)
bm.normal_update()
bm.to_mesh(mesh)
bm.free()
body = bpy.data.objects.new(NAME, mesh)
scene.collection.objects.link(body)
for name, index in sorted(groups.items(), key=lambda p: p[1]):
    body.vertex_groups.new(name=name)
body.parent = arm
mod = body.modifiers.new("Armature", "ARMATURE")
mod.object = arm

# The atlas: one 8 px square a colour.
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

# ---------------------------------------------------------------- the clips

for pb in arm.pose.bones:
    pb.rotation_mode = "XYZ"
LEGS = [n for n in BONES if n.startswith(("front_", "hind_"))]


def key(action_name, seconds, pose):
    """Keys an action: pose(t) returns {bone: (rx, ry, rz) degrees} and optionally "root_loc" for a frame at time t."""
    action = bpy.data.actions.new(action_name)
    arm.animation_data_create()
    arm.animation_data.action = action
    frames = int(round(seconds * FPS))
    for f in range(frames + 1):
        t = f / FPS
        values = pose(t)
        for pb in arm.pose.bones:
            r = values.get(pb.name, (0, 0, 0))
            pb.rotation_euler = [math.radians(a) for a in r]
            pb.keyframe_insert("rotation_euler", frame=f)
        root = arm.pose.bones["root"]
        root.location = values.get("root_loc", (0, 0, 0))
        root.keyframe_insert("location", frame=f)
    action.use_fake_user = True
    track = arm.animation_data.nla_tracks.new()
    track.name = action_name
    track.strips.new(action_name, 0, action)
    arm.animation_data.action = None
    return action


def leg_pose(phase, stride, bend):
    """Upper and lower rotation (degrees about X) of a leg at a gait phase in 0..1."""
    s = math.sin(2 * math.pi * phase)
    c = math.cos(2 * math.pi * phase)
    return (stride * s, 0, 0), (bend * max(0.0, c) + 5, 0, 0)


def idle(t):
    breath = math.sin(2 * math.pi * t / 2.0)
    look = math.sin(2 * math.pi * t / 2.0 + 1.0)
    return {
        "chest": (breath * 1.5, 0, 0), "neck": (-breath * 2, 0, look * 6), "head": (breath * 2, 0, look * 4),
        "tail1": (-8, 0, math.sin(2 * math.pi * t / 1.0) * 8), "tail2": (-6, 0, math.sin(2 * math.pi * t / 1.0 + 0.8) * 10),
        "jaw": (max(0, breath) * 4, 0, 0),
        "root_loc": (0, 0, breath * 0.004),
    }


def run(t):
    p = t / 0.5
    pose = {"root_loc": (0, 0, abs(math.sin(2 * math.pi * p)) * 0.05)}
    pose["chest"] = (math.sin(2 * math.pi * p) * 6, 0, 0)
    pose["hips"] = (-math.sin(2 * math.pi * p) * 6, 0, 0)
    pose["neck"] = (-math.sin(2 * math.pi * p) * 6 - 10, 0, 0)
    pose["head"] = (math.sin(2 * math.pi * p) * 4 + 8, 0, 0)
    pose["tail1"] = (-20 + math.sin(2 * math.pi * p) * 10, 0, 0)
    pose["tail2"] = (-10, 0, 0)
    pose["jaw"] = (10, 0, 0)
    for side, offset in (("L", 0.0), ("R", 0.12)):
        up, low = leg_pose(p + offset, 38, 40)
        pose["front_upper_" + side], pose["front_lower_" + side] = up, low
        up, low = leg_pose(p + 0.5 + offset, 34, -40)
        pose["hind_upper_" + side], pose["hind_lower_" + side] = up, low
    return pose


def ease(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def attack(t):
    crouch = ease(t / 0.3) * (1 - ease((t - 0.3) / 0.15))
    spring = ease((t - 0.3) / 0.2) * (1 - ease((t - 0.62) / 0.28))
    bite = math.sin(min(1.0, max(0.0, (t - 0.45) / 0.2)) * math.pi)
    pose = {
        "root_loc": (0, -0.18 * spring, -0.08 * crouch + 0.08 * spring),
        "chest": (-10 * crouch + 8 * spring, 0, 0), "hips": (6 * crouch - 6 * spring, 0, 0),
        "neck": (-20 * crouch + 15 * spring, 0, 0), "head": (10 * crouch - 10 * spring, 0, 0),
        "jaw": (35 * bite, 0, 0), "tail1": (-25 * spring + 10 * crouch, 0, 0),
    }
    for side in ("L", "R"):
        pose["front_upper_" + side] = (-25 * crouch + 45 * spring, 0, 0)
        pose["front_lower_" + side] = (30 * crouch - 10 * spring, 0, 0)
        pose["hind_upper_" + side] = (35 * crouch - 30 * spring, 0, 0)
        pose["hind_lower_" + side] = (-45 * crouch + 15 * spring, 0, 0)
    return pose


def hit(t):
    k = math.sin(min(1.0, t / 0.35) * math.pi)
    return {"chest": (-8 * k, 0, 6 * k), "neck": (15 * k, 0, -10 * k), "head": (10 * k, 0, 0), "jaw": (20 * k, 0, 0),
            "root_loc": (0, 0.05 * k, 0)}


def death(t):
    k = ease(t / 0.7)
    pose = {"root": (0, -88 * k, 0), "root_loc": (0, 0, -0.06 * k), "neck": (25 * k, 0, 0), "head": (15 * k, 0, 0),
            "jaw": (20 * k, 0, 0), "tail1": (10 * k, 0, 0)}
    for side in ("L", "R"):
        pose["front_upper_" + side] = (20 * k, 0, 0)
        pose["hind_upper_" + side] = (-20 * k, 0, 0)
        pose["front_lower_" + side] = (25 * k, 0, 0)
        pose["hind_lower_" + side] = (-25 * k, 0, 0)
    return pose


for name, seconds, pose in (("idle", 2.0, idle), ("run", 0.5, run), ("attack", 0.9, attack), ("hit", 0.35, hit), ("death", 1.0, death)):
    key(name, seconds, pose)

bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, NAME + ".fbx"), use_selection=True, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         path_mode="STRIP", embed_textures=False)

# A look from the game's camera, for checking without Unity.
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.type = "ORTHO"
cam.data.ortho_scale = 2.2
cam.rotation_euler = (math.radians(60), 0, math.radians(-45))
cam.location = (-2.2, -2.2, 2.4)
scene.collection.objects.link(cam)
scene.camera = cam
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 4
sun.rotation_euler = (math.radians(40), 0, math.radians(-30))
scene.collection.objects.link(sun)
scene.render.engine = "BLENDER_EEVEE"
scene.render.film_transparent = True
scene.render.resolution_x = scene.render.resolution_y = 320
arm.animation_data.action = bpy.data.actions["attack"]
scene.frame_set(int(0.55 * FPS))
scene.render.filepath = os.path.join(ROOT, "ArtSource", "pixel", "gear_preview", NAME + "_attack.png")
bpy.ops.render.render(write_still=True)
arm.animation_data.action = bpy.data.actions["run"]
scene.frame_set(3)
scene.render.filepath = os.path.join(ROOT, "ArtSource", "pixel", "gear_preview", NAME + "_run.png")
bpy.ops.render.render(write_still=True)
print("REPORT wrote", OUT, len(mesh.polygons), "faces,", len(groups), "bones")
