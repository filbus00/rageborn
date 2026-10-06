# The pets (2026-10-06, the owner: "make models for the pets"): the Wolf, the Raven and the Boar sold at the Pet Vendor,
# built in Blender from code like the Ash Wolf (wolf.py), each on a rig of its own with its five clips keyed in code.
# Every part is a solid weighted wholly to one bone. They are companions, so warmer than the enemies: the wolf tawny
# with a cream muzzle and a red leather collar, the boar dark brown with a bristled back, tusks and a harness, the raven
# blue-black with a silver ring on its leg. The raven flies: it is modelled standing on the ground and the game lifts it
# (PetController.HoverHeight). Clips (24 a second): idle, run, attack, hit and death (knocked out).
# Writes Assets/_Project/Art/Models/Pets/<name>/<name>.fbx and <name>_albedo.png (a generic rig:
# EnemyBakeSetup.GenericBodies), and previews in ArtSource/pixel/gear_preview. Then Tools > ARPG > Sprite Bake > Bake Pets.
#   Blender -b -P ArtSource/tools/props/pets.py -- pet_wolf      (or pet_boar, pet_raven)
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh, mathutils
import numpy as np
from kit import STONE, EMBER, BLOOD, BONE, WOOD, EARTH, COLD, SKIN

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
NAME = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "pet_wolf"
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Pets", NAME)
PREVIEW = os.path.join(ROOT, "ArtSource", "pixel", "gear_preview")
FPS = 24
V = mathutils.Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS

# ---------------------------------------------------------------- the rigs (every pet faces -Y, which is Unity's +Z)


def quad_bones(hip, chest, neck, head, jaw, tails, legs):
    """A four-legged rig: legs is ((x, front_y, hind_y), shoulder_z, knee_z, foot_z)."""
    bones = {"root": ((0, 0, 0), (0, 0, 0.2), None), "hips": hip + ("root",), "chest": chest + ("hips",),
             "neck": neck + ("chest",), "head": head + ("neck",), "jaw": jaw + ("head",)}
    parent = "hips"
    for i, (h, t) in enumerate(tails):
        bones["tail%d" % (i + 1)] = (h, t, parent)
        parent = "tail%d" % (i + 1)
    (x, fy, hy), top, knee, foot = legs
    for side, sx in (("L", x), ("R", -x)):
        bones["front_upper_" + side] = ((sx, fy, top), (sx, fy - 0.03, knee), "chest")
        bones["front_lower_" + side] = ((sx, fy - 0.03, knee), (sx, fy - 0.01, foot), "front_upper_" + side)
        bones["front_paw_" + side] = ((sx, fy - 0.01, foot), (sx, fy - 0.09, 0.02), "front_lower_" + side)
        bones["hind_upper_" + side] = ((sx, hy, top), (sx, hy + 0.09, knee), "hips")
        bones["hind_lower_" + side] = ((sx, hy + 0.09, knee), (sx, hy + 0.03, foot), "hind_upper_" + side)
        bones["hind_paw_" + side] = ((sx, hy + 0.03, foot), (sx, hy - 0.04, 0.02), "hind_lower_" + side)
    return bones


RIGS = {
    "pet_wolf": quad_bones(((0, 0.3, 0.6), (0, 0.0, 0.62)), ((0, 0.0, 0.62), (0, -0.3, 0.66)),
                           ((0, -0.3, 0.66), (0, -0.44, 0.82)), ((0, -0.44, 0.82), (0, -0.7, 0.79)),
                           ((0, -0.5, 0.75), (0, -0.72, 0.71)),
                           [((0, 0.32, 0.64), (0, 0.5, 0.74)), ((0, 0.5, 0.74), (0, 0.66, 0.8))],
                           ((0.11, -0.22, 0.27), 0.56, 0.31, 0.06)),
    "pet_boar": quad_bones(((0, 0.3, 0.5), (0, 0.0, 0.52)), ((0, 0.0, 0.52), (0, -0.3, 0.54)),
                           ((0, -0.3, 0.54), (0, -0.42, 0.5)), ((0, -0.42, 0.5), (0, -0.74, 0.38)),
                           ((0, -0.55, 0.37), (0, -0.74, 0.33)),
                           [((0, 0.42, 0.54), (0, 0.5, 0.42))],
                           ((0.13, -0.24, 0.26), 0.42, 0.22, 0.05)),
    "pet_raven": {
        "root": ((0, 0, 0), (0, 0, 0.2), None),
        "body": ((0, 0.1, 0.13), (0, -0.1, 0.13), "root"),
        "head": ((0, -0.12, 0.16), (0, -0.3, 0.16), "body"),
        "tail": ((0, 0.12, 0.13), (0, 0.32, 0.12), "body"),
        "wing_L": ((0.05, -0.02, 0.15), (0.26, 0.0, 0.15), "body"),
        "wing_tip_L": ((0.26, 0.0, 0.15), (0.5, 0.05, 0.15), "wing_L"),
        "wing_R": ((-0.05, -0.02, 0.15), (-0.26, 0.0, 0.15), "body"),
        "wing_tip_R": ((-0.26, 0.0, 0.15), (-0.5, 0.05, 0.15), "wing_R"),
        "leg_L": ((0.025, 0.02, 0.08), (0.025, 0.03, 0.0), "body"),
        "leg_R": ((-0.025, 0.02, 0.08), (-0.025, 0.03, 0.0), "body"),
    },
}
BONES = RIGS[NAME]

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

# ---------------------------------------------------------------- the parts

COLORS = {
    "pet_wolf": {"fur": WOOD[4], "fur_dark": WOOD[2], "saddle": EARTH[2], "cream": BONE[1], "eye": EMBER[3],
                 "nose": STONE[0], "mouth": BLOOD[1], "tongue": BLOOD[3], "teeth": BONE[2], "collar": BLOOD[2],
                 "brass": EMBER[3], "paw": EARTH[1]},
    "pet_boar": {"hide": WOOD[2], "hide_dark": WOOD[1], "bristle": WOOD[0], "belly": WOOD[3], "snout": SKIN[1],
                 "tusk": BONE[2], "eye": EMBER[2], "hoof": STONE[0], "strap": EARTH[2], "iron": STONE[4],
                 "mouth": BLOOD[1]},
    "pet_raven": {"black": COLD[0], "sheen": COLD[1], "feather": STONE[0], "edge": COLD[2], "beak": STONE[1],
                  "eye": EMBER[3], "foot": STONE[1], "ring": BONE[2]},
}[NAME]
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
            verts.append(centre + V((math.sin(phi) * math.cos(t) * radii[0], math.sin(phi) * math.sin(t) * radii[1],
                                     math.cos(phi) * radii[2])))
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


def slab(points, z, thick, bone, color):
    """A flat plate: a polygon in the ground plane (x, y) at height z, thick in z (wings and tail fans)."""
    n = len(points)
    verts = [V((x, y, z - thick / 2)) for x, y in points] + [V((x, y, z + thick / 2)) for x, y in points]
    faces = [list(reversed(range(n))), list(range(n, 2 * n))]
    faces += [[k, (k + 1) % n, n + (k + 1) % n, n + k] for k in range(n)]
    add(verts, faces, bone, color)


def ring(centre, axis, radius, width, bone, color, sides=10):
    """A band round a part (a collar, a harness)."""
    c, d = V(centre), V(axis).normalized()
    limb(c - d * width / 2, c + d * width / 2, radius, radius, bone, color, sides)


def wolf():
    # Torso: a deep chest, a narrower waist and haunches, a darker saddle along the back, a cream underside.
    blob((0, -0.14, 0.62), (0.15, 0.24, 0.17), "chest", "fur")
    blob((0, -0.14, 0.52), (0.11, 0.2, 0.08), "chest", "cream")
    blob((0, 0.18, 0.62), (0.12, 0.2, 0.13), "hips", "fur")
    blob((0, -0.02, 0.76), (0.08, 0.34, 0.05), "chest", "saddle")
    blob((0, 0.2, 0.73), (0.07, 0.14, 0.04), "hips", "saddle")
    # Neck with a ruff and the collar, the head, a cream muzzle, big upright ears, amber eyes, the open mouth panting.
    blob((0, -0.36, 0.73), (0.12, 0.1, 0.12), "neck", "fur_dark")
    blob((0, -0.36, 0.68), (0.09, 0.08, 0.08), "neck", "cream")
    ring((0, -0.37, 0.73), (0, -0.4, 0.45), 0.115, 0.04, "neck", "collar")
    block((0, -0.45, 0.62), (0.035, 0.012, 0.045), "neck", "brass")
    blob((0, -0.5, 0.84), (0.1, 0.1, 0.09), "head", "fur")
    block((0, -0.64, 0.8), (0.085, 0.16, 0.065), "head", "cream")
    block((0, -0.72, 0.825), (0.04, 0.03, 0.03), "head", "nose")
    for x in (-0.055, 0.055):
        limb((x, -0.47, 0.9), (x * 1.25, -0.46, 1.04), 0.04, 0.006, "head", "fur_dark", 4)
        block((x, -0.585, 0.86), (0.025, 0.012, 0.02), "head", "eye")
    block((0, -0.62, 0.745), (0.07, 0.15, 0.03), "jaw", "cream")
    block((0, -0.62, 0.765), (0.055, 0.12, 0.012), "jaw", "mouth")
    block((0, -0.66, 0.75), (0.035, 0.08, 0.01), "jaw", "tongue")
    block((0, -0.66, 0.775), (0.06, 0.06, 0.012), "head", "teeth")
    # The tail carried high and bushy, a cream tip.
    limb((0, 0.32, 0.64), (0, 0.5, 0.74), 0.05, 0.07, "tail1", "fur")
    limb((0, 0.5, 0.74), (0, 0.66, 0.8), 0.07, 0.025, "tail2", "cream")
    # Legs: thick upper parts, thin lower parts, dark paws.
    for side, x in (("L", 0.11), ("R", -0.11)):
        limb((x, -0.22, 0.58), (x, -0.25, 0.31), 0.08, 0.05, "front_upper_" + side, "fur")
        limb((x, -0.25, 0.31), (x, -0.23, 0.06), 0.045, 0.038, "front_lower_" + side, "fur_dark")
        block((x, -0.27, 0.035), (0.07, 0.11, 0.055), "front_paw_" + side, "paw")
        limb((x, 0.25, 0.6), (x, 0.36, 0.33), 0.095, 0.055, "hind_upper_" + side, "fur")
        limb((x, 0.36, 0.33), (x, 0.3, 0.07), 0.048, 0.038, "hind_lower_" + side, "fur_dark")
        block((x, 0.26, 0.035), (0.07, 0.11, 0.055), "hind_paw_" + side, "paw")


def boar():
    # A heavy front: a great chest and shoulders, lighter haunches, a paler belly.
    blob((0, -0.08, 0.5), (0.2, 0.34, 0.21), "chest", "hide")
    blob((0, 0.22, 0.48), (0.17, 0.2, 0.17), "hips", "hide")
    blob((0, -0.02, 0.37), (0.14, 0.3, 0.09), "chest", "belly")
    # The bristled ridge from the nape to the rump.
    for i in range(9):
        y = -0.36 + i * 0.08
        z = 0.72 - abs(y + 0.12) * 0.25
        bone = "chest" if y < 0.05 else "hips"
        limb((0, y, z - 0.05), (0, y + 0.05, z + 0.07 - i * 0.006), 0.035, 0.004, bone, "bristle", 4)
    # The harness across the chest with an iron ring on the back.
    ring((0, -0.2, 0.5), (0, 1, 0.15), 0.205, 0.05, "chest", "strap")
    limb((0, -0.2, 0.71), (0, -0.2, 0.75), 0.03, 0.03, "chest", "iron", 6)
    # A thick neck, a long wedge of a head, the snout's disc, small ears, tusks curling up from the jaw.
    blob((0, -0.36, 0.5), (0.16, 0.12, 0.17), "neck", "hide_dark")
    blob((0, -0.5, 0.46), (0.12, 0.13, 0.12), "head", "hide")
    limb((0, -0.56, 0.43), (0, -0.74, 0.37), 0.085, 0.06, "head", "hide_dark", 8)
    block((0, -0.755, 0.365), (0.1, 0.02, 0.08), "head", "snout")
    for x in (-0.07, 0.07):
        limb((x, -0.44, 0.55), (x * 1.6, -0.4, 0.65), 0.035, 0.006, "head", "hide_dark", 4)
        block((x, -0.6, 0.48), (0.02, 0.012, 0.018), "head", "eye")
        limb((x * 0.9, -0.66, 0.34), (x * 1.4, -0.69, 0.43), 0.018, 0.01, "jaw", "tusk", 5)
        limb((x * 1.4, -0.69, 0.43), (x * 1.1, -0.65, 0.49), 0.01, 0.003, "jaw", "tusk", 5)
    block((0, -0.63, 0.33), (0.08, 0.14, 0.035), "jaw", "hide_dark")
    block((0, -0.68, 0.35), (0.06, 0.05, 0.01), "jaw", "mouth")
    # A thin tail with a tuft.
    limb((0, 0.42, 0.54), (0, 0.5, 0.42), 0.015, 0.012, "tail1", "hide_dark", 4)
    blob((0, 0.5, 0.41), (0.025, 0.025, 0.035), "tail1", "bristle", 6)
    # Short, thick legs and hooves.
    for side, x in (("L", 0.13), ("R", -0.13)):
        limb((x, -0.24, 0.44), (x, -0.27, 0.22), 0.075, 0.05, "front_upper_" + side, "hide")
        limb((x, -0.27, 0.22), (x, -0.25, 0.05), 0.04, 0.035, "front_lower_" + side, "hide_dark")
        block((x, -0.27, 0.03), (0.06, 0.08, 0.05), "front_paw_" + side, "hoof")
        limb((x, 0.24, 0.46), (x, 0.35, 0.22), 0.09, 0.05, "hind_upper_" + side, "hide")
        limb((x, 0.35, 0.22), (x, 0.29, 0.05), 0.04, 0.035, "hind_lower_" + side, "hide_dark")
        block((x, 0.27, 0.03), (0.06, 0.08, 0.05), "hind_paw_" + side, "hoof")


def raven():
    # A sleek body, a deep chest with a blue sheen, the shaggy throat, the heavy beak.
    blob((0, 0.0, 0.13), (0.07, 0.16, 0.07), "body", "black")
    blob((0, -0.07, 0.12), (0.066, 0.08, 0.066), "body", "sheen")
    blob((0, -0.17, 0.165), (0.056, 0.065, 0.056), "head", "black")
    blob((0, -0.18, 0.13), (0.04, 0.04, 0.035), "head", "feather")
    limb((0, -0.22, 0.17), (0, -0.33, 0.155), 0.024, 0.003, "head", "beak", 5)
    for x in (-0.034, 0.034):
        block((x, -0.205, 0.185), (0.012, 0.016, 0.014), "head", "eye")
    # A wedge of a tail.
    slab([(-0.035, 0.1), (0.035, 0.1), (0.075, 0.34), (0.0, 0.37), (-0.075, 0.34)], 0.125, 0.014, "tail", "feather")
    # Wings spread: the arm, then the hand with its fingered primaries; a lighter edge along the coverts.
    for side, s in (("L", 1), ("R", -1)):
        slab([(s * 0.04, -0.05), (s * 0.26, -0.03), (s * 0.27, 0.1), (s * 0.04, 0.12)], 0.15, 0.016,
             "wing_" + side, "black")
        slab([(s * 0.05, -0.055), (s * 0.25, -0.035), (s * 0.25, -0.005), (s * 0.05, -0.02)], 0.16, 0.012,
             "wing_" + side, "edge")
        slab([(s * 0.25, -0.03), (s * 0.44, 0.0), (s * 0.42, 0.11), (s * 0.25, 0.1)], 0.15, 0.014,
             "wing_tip_" + side, "black")
        for i, (dx, dy) in enumerate(((0.12, 0.0), (0.11, 0.035), (0.095, 0.07), (0.075, 0.1))):
            x0, y0 = 0.42 - i * 0.012, 0.01 + i * 0.03
            slab([(s * x0, y0), (s * (x0 + dx), y0 + dy * 0.3 + 0.005), (s * (x0 + dx - 0.01), y0 + dy * 0.3 + 0.03),
                  (s * x0, y0 + 0.028)], 0.148, 0.01, "wing_tip_" + side, "feather")
        # Legs and feet, and the silver ring of a kept bird on the left.
        limb((s * 0.025, 0.02, 0.08), (s * 0.025, 0.03, 0.01), 0.008, 0.006, "leg_" + side, "foot", 4)
        block((s * 0.025, 0.0, 0.006), (0.03, 0.06, 0.012), "leg_" + side, "foot")
    limb((0.025, 0.024, 0.04), (0.025, 0.026, 0.055), 0.013, 0.013, "leg_L", "ring", 6)


{"pet_wolf": wolf, "pet_boar": boar, "pet_raven": raven}[NAME]()

mesh = bpy.data.meshes.new(NAME)
bm.normal_update()
bm.to_mesh(mesh)
bm.free()
body = bpy.data.objects.new(NAME, mesh)
scene.collection.objects.link(body)
for name, index in sorted(groups.items(), key=lambda p: p[1]):
    body.vertex_groups.new(name=name)
# Every bone in a vertex group (a speck of nothing is fine), so none is stripped on import.
for name in BONES:
    if name not in groups:
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


def ease(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def wave(t, period, phase=0.0):
    return math.sin(2 * math.pi * (t / period + phase))


def leg_pose(phase, stride, bend):
    s = math.sin(2 * math.pi * phase)
    c = math.cos(2 * math.pi * phase)
    return (stride * s, 0, 0), (bend * max(0.0, c) + 5, 0, 0)


def quad_run(period, stride, bob):
    def run(t):
        p = t / period
        pose = {"root_loc": (0, 0, abs(math.sin(2 * math.pi * p)) * bob)}
        pose["chest"] = (math.sin(2 * math.pi * p) * 6, 0, 0)
        pose["hips"] = (-math.sin(2 * math.pi * p) * 6, 0, 0)
        pose["neck"] = (-math.sin(2 * math.pi * p) * 5 - 6, 0, 0)
        pose["head"] = (math.sin(2 * math.pi * p) * 4 + 6, 0, 0)
        pose["tail1"] = (-10 + math.sin(2 * math.pi * p) * 10, 0, 0)
        pose["tail2"] = (-6, 0, 0)
        pose["jaw"] = (8, 0, 0)
        for side, offset in (("L", 0.0), ("R", 0.12)):
            up, low = leg_pose(p + offset, stride, 40)
            pose["front_upper_" + side], pose["front_lower_" + side] = up, low
            up, low = leg_pose(p + 0.5 + offset, stride * 0.9, -40)
            pose["hind_upper_" + side], pose["hind_lower_" + side] = up, low
        return pose
    return run


def quad_hit(t):
    k = math.sin(min(1.0, t / 0.35) * math.pi)
    return {"chest": (-8 * k, 0, 6 * k), "neck": (15 * k, 0, -10 * k), "head": (10 * k, 0, 0), "jaw": (15 * k, 0, 0),
            "root_loc": (0, 0.05 * k, 0)}


def quad_death(t):
    k = ease(t / 0.7)
    pose = {"root": (0, -88 * k, 0), "root_loc": (0, 0, -0.04 * k), "neck": (20 * k, 0, 0), "head": (12 * k, 0, 0),
            "tail1": (10 * k, 0, 0)}
    for side in ("L", "R"):
        pose["front_upper_" + side] = (20 * k, 0, 0)
        pose["hind_upper_" + side] = (-20 * k, 0, 0)
        pose["front_lower_" + side] = (25 * k, 0, 0)
        pose["hind_lower_" + side] = (-25 * k, 0, 0)
    return pose


def wolf_idle(t):
    # Panting, the tail wagging, ears and head turning to look about: a friendly dog, not the Ash Wolf's stalk.
    pant = wave(t, 0.5)
    look = wave(t, 2.0, 0.15)
    return {"chest": (pant * 1.5, 0, 0), "neck": (-4, 0, look * 10), "head": (2, 0, look * 6), "jaw": (10 + pant * 5, 0, 0),
            "tail1": (8, 0, wave(t, 0.5) * 22), "tail2": (6, 0, wave(t, 0.5, 0.15) * 26),
            "root_loc": (0, 0, pant * 0.003)}


def wolf_attack(t):
    crouch = ease(t / 0.3) * (1 - ease((t - 0.3) / 0.15))
    spring = ease((t - 0.3) / 0.2) * (1 - ease((t - 0.62) / 0.28))
    bite = math.sin(min(1.0, max(0.0, (t - 0.45) / 0.2)) * math.pi)
    pose = {"root_loc": (0, -0.18 * spring, -0.08 * crouch + 0.08 * spring),
            "chest": (-10 * crouch + 8 * spring, 0, 0), "hips": (6 * crouch - 6 * spring, 0, 0),
            "neck": (-20 * crouch + 15 * spring, 0, 0), "head": (10 * crouch - 10 * spring, 0, 0),
            "jaw": (35 * bite, 0, 0), "tail1": (-25 * spring + 10 * crouch, 0, 0)}
    for side in ("L", "R"):
        pose["front_upper_" + side] = (-25 * crouch + 45 * spring, 0, 0)
        pose["front_lower_" + side] = (30 * crouch - 10 * spring, 0, 0)
        pose["hind_upper_" + side] = (35 * crouch - 30 * spring, 0, 0)
        pose["hind_lower_" + side] = (-45 * crouch + 15 * spring, 0, 0)
    return pose


def boar_idle(t):
    # Snuffling at the ground: the head down and rooting, the tail flicking, a grunt of breath.
    root = max(0.0, wave(t, 2.0)) ** 2
    snuff = wave(t, 0.25) * root
    return {"chest": (wave(t, 1.0) * 1.2, 0, 0), "neck": (14 * root, 0, wave(t, 4.0) * 8), "head": (10 * root + snuff * 3, 0, 0),
            "jaw": (4 * root, 0, 0), "tail1": (0, 0, wave(t, 0.4) * 25), "root_loc": (0, 0, wave(t, 1.0) * 0.003)}


def boar_attack(t):
    # Head down, a short charge and an upward toss of the tusks.
    lower = ease(t / 0.3)
    toss = ease((t - 0.42) / 0.14) * (1 - ease((t - 0.6) / 0.3))
    lunge = ease((t - 0.25) / 0.2) * (1 - ease((t - 0.6) / 0.3))
    pose = {"root_loc": (0, -0.2 * lunge, 0.02 * toss), "chest": (-6 * lunge, 0, 0),
            "neck": (22 * lower * (1 - toss) - 30 * toss, 0, 0), "head": (12 * lower * (1 - toss) - 20 * toss, 0, 0),
            "jaw": (10 * toss, 0, 0), "tail1": (-30 * lunge, 0, 0)}
    for side in ("L", "R"):
        pose["front_upper_" + side] = (20 * lunge, 0, 0)
        pose["hind_upper_" + side] = (-15 * lunge, 0, 0)
    return pose


def raven_flap(period, beat, pitch, bob):
    def fly(t):
        w = wave(t, period)
        # The arm leads and the hand follows a little later, so the wing bends through the stroke.
        return {"wing_L": (0, 0, 0), "wing_R": (0, 0, 0),
                "wing_L_flap": w, "body": (pitch + w * 4, 0, 0), "head": (-pitch * 0.7 - w * 3, 0, 0),
                "tail": (-6 - w * 6, 0, 0), "leg_L": (-50, 0, 0), "leg_R": (-50, 0, 0),
                "root_loc": (0, 0, -w * bob),
                "_beat": beat, "_w": w, "_w2": wave(t, period, -0.12)}
    return fly


def raven_pose(values):
    """Turns a flap value into wing rotations (rotation about each wing bone's own x lifts its tip)."""
    if "_w" in values:
        beat, w, w2 = values["_beat"], values["_w"], values["_w2"]
        for side in ("L", "R"):
            values["wing_" + side] = (beat * w, 0, 0)
            values["wing_tip_" + side] = (beat * 0.6 * w2, 0, 0)
    return values


def raven_attack(t):
    # Wings swept up and back, a dive at the target, the beak's stab, and a climb back to the hover.
    dive = ease(t / 0.3) * (1 - ease((t - 0.45) / 0.25))
    stab = math.sin(min(1.0, max(0.0, (t - 0.3) / 0.15)) * math.pi)
    up = ease(t / 0.2) * (1 - ease((t - 0.45) / 0.25))
    values = {"root_loc": (0, -0.22 * dive, -0.1 * dive), "body": (-35 * dive, 0, 0), "head": (20 * dive - 25 * stab, 0, 0),
              "tail": (15 * dive, 0, 0), "leg_L": (-60, 0, 0), "leg_R": (-60, 0, 0)}
    flap = wave(t, 0.3) * (1 - up)
    for side in ("L", "R"):
        values["wing_" + side] = (55 * up + 40 * flap, 0, 0)
        values["wing_tip_" + side] = (-40 * up + 25 * flap, 0, 0)
    return values


def raven_hit(t):
    k = math.sin(min(1.0, t / 0.35) * math.pi)
    values = {"body": (20 * k, 0, 10 * k), "head": (-20 * k, 0, 0), "root_loc": (0, 0.06 * k, 0.03 * k),
              "leg_L": (-50, 0, 0), "leg_R": (-50, 0, 0)}
    for side in ("L", "R"):
        values["wing_" + side] = (60 * k, 0, 0)
        values["wing_tip_" + side] = (30 * k, 0, 0)
    return values


def raven_death(t):
    # Wings crumple, it tumbles onto its side; the game lowers it to the ground meanwhile.
    k = ease(t / 0.8)
    values = {"root": (0, -80 * k, 0), "body": (25 * k, 0, 0), "head": (30 * k, 0, 0), "tail": (10 * k, 0, 0)}
    for side in ("L", "R"):
        values["wing_" + side] = (-30 * k + 40 * (1 - k) * math.sin(t * 30), 0, 0)
        values["wing_tip_" + side] = (-60 * k, 0, 0)
    return values


if NAME == "pet_raven":
    def wrap(f):
        return lambda t: raven_pose(f(t))
    clips = (("idle", 0.6, wrap(raven_flap(0.6, 38, -6, 0.015))), ("run", 0.4, wrap(raven_flap(0.4, 46, -18, 0.02))),
             ("attack", 0.8, raven_attack), ("hit", 0.35, raven_hit), ("death", 1.0, raven_death))
elif NAME == "pet_boar":
    clips = (("idle", 2.0, boar_idle), ("run", 0.42, quad_run(0.42, 32, 0.04)), ("attack", 0.9, boar_attack),
             ("hit", 0.35, quad_hit), ("death", 1.0, quad_death))
else:
    clips = (("idle", 2.0, wolf_idle), ("run", 0.5, quad_run(0.5, 38, 0.05)), ("attack", 0.9, wolf_attack),
             ("hit", 0.35, quad_hit), ("death", 1.0, quad_death))
for name, seconds, pose in clips:
    key(name, seconds, pose)

bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, NAME + ".fbx"), use_selection=True, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         path_mode="STRIP", embed_textures=False)

# Looks from the game's camera, for checking without Unity: a strip of each clip.
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.type = "ORTHO"
cam.data.ortho_scale = 1.8 if NAME != "pet_raven" else 1.2
cam.rotation_euler = (math.radians(60), 0, math.radians(-45 + 30))
cam.location = V((-2.2, -2.2, 2.4))
if NAME == "pet_raven":
    cam.location = V((-2.2, -2.2, 2.2))
scene.collection.objects.link(cam)
scene.camera = cam
direction = cam.rotation_euler.to_matrix() @ V((0, 0, -1))
target = V((0, -0.1, 0.4 if NAME != "pet_raven" else 0.12))
cam.location = target - direction * 4
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 4
sun.rotation_euler = (math.radians(40), 0, math.radians(-30))
scene.collection.objects.link(sun)
scene.render.engine = "BLENDER_EEVEE"
scene.render.film_transparent = True
scene.render.resolution_x = scene.render.resolution_y = 240
os.makedirs(PREVIEW, exist_ok=True)
for clip, seconds, _ in clips:
    arm.animation_data.action = bpy.data.actions[clip]
    for i, fraction in enumerate((0.0, 0.3, 0.55, 0.8)):
        scene.frame_set(int(round(fraction * seconds * FPS)))
        scene.render.filepath = os.path.join(PREVIEW, "%s_%s_%d.png" % (NAME, clip, i))
        bpy.ops.render.render(write_still=True)
print("REPORT wrote", OUT, len(mesh.polygons), "faces,", len(groups), "bones")
