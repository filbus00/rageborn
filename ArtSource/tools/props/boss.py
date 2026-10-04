# The Cinder Warden, act 1's boss: built in Blender from code on the ghoul's Mixamo rig, so the ghoul's idle, run,
# slam, hit and death animate him. Armour of charred stone with ember seams, a horned helm, heavy pauldrons and
# gauntlets: each part a solid weighted wholly to one bone (rigid skinning; at pixel-art size the joints do not show).
# Writes Assets/_Project/Art/Models/Enemies/cinder_warden/: cinder_warden.fbx (with skin, the ghoul's idle),
# cinder_warden_albedo.png (an atlas of the parts' colours) and the ghoul's run, attack, hit and death files copied
# under the Warden's name. Then Tools > ARPG > Sprite Bake > Bake Enemies.
#   Blender -b -P ArtSource/tools/props/boss.py
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh, mathutils
import numpy as np
from kit import STONE, WOOD, BLOOD, EMBER, BONE, COLD, linear

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
GHOUL = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Enemies", "ghoul")
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Enemies", "cinder_warden")
NAME = "cinder_warden"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(GHOUL, "ghoul.fbx"))
arm = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
    bpy.data.objects.remove(o, do_unlink=True)
bones = arm.data.bones


def head(name):
    return mathutils.Vector(bones["mixamorig:" + name].head_local)


def tail(name):
    return mathutils.Vector(bones["mixamorig:" + name].tail_local)


# Armature space: y up, z forward, x to the character's left; about 0.77 tall.
COLORS = {
    "armour": STONE[1], "plate": STONE[2], "edge": STONE[3], "ember": EMBER[3], "core": EMBER[4],
    "horn": BONE[0], "cloth": STONE[0], "eye": EMBER[4],
}
color_list = list(COLORS)
bm = bmesh.new()
deform = bm.verts.layers.deform.verify()
face_color = []
groups = {}


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
# Torso: a broad breastplate over the chest, a belly plate, an ember core glowing through a gap.
hips, spine2, neck = head("Hips"), head("Spine2"), head("Neck")
block(V((0, hips.y + 0.02, 0)), (0.2, 0.1, 0.15), "Hips", "armour")
for sx in (-1, 1):
    block(V((sx * 0.07, hips.y - 0.05, 0.03)), (0.08, 0.1, 0.03), "Hips", "plate")
    block(V((sx * 0.07, hips.y - 0.05, -0.04)), (0.08, 0.1, 0.03), "Hips", "cloth")
block(V((0, head("Spine1").y, 0.0)), (0.22, 0.09, 0.16), "Spine1", "armour")
block(V((0, spine2.y + 0.03, 0.005)), (0.3, 0.12, 0.19), "Spine2", "plate")
block(V((0, spine2.y + 0.03, 0.098)), (0.08, 0.06, 0.012), "Spine2", "core")
for sx in (-1, 1):
    block(V((sx * 0.06, spine2.y + 0.03, 0.1)), (0.035, 0.1, 0.012), "Spine2", "ember")
block(V((0, spine2.y + 0.1, -0.02)), (0.26, 0.04, 0.17), "Spine2", "edge")
# Helm: a bucket with a slit, two horns curling forward.
h = head("Head")
block(V((0, h.y + 0.04, 0.01)), (0.11, 0.12, 0.12), "Head", "armour")
block(V((0, h.y + 0.05, 0.072)), (0.08, 0.015, 0.01), "Head", "eye")
for sx in (-1, 1):
    limb(V((sx * 0.05, h.y + 0.08, 0.0)), V((sx * 0.11, h.y + 0.14, 0.03)), 0.022, 0.012, "Head", "horn")
    limb(V((sx * 0.11, h.y + 0.14, 0.03)), V((sx * 0.12, h.y + 0.17, 0.08)), 0.012, 0.003, "Head", "horn")
# Arms: pauldrons, plated arms, huge gauntlets.
for side, sx in (("Left", 1), ("Right", -1)):
    blob(head(side + "Arm") + V((0, 0.03, 0)), (0.085, 0.06, 0.08), side + "Arm", "plate")
    block(head(side + "Arm") + V((0, 0.075, 0)), (0.12, 0.02, 0.1), side + "Arm", "edge")
    limb(head(side + "Arm"), tail(side + "Arm"), 0.045, 0.04, side + "Arm", "armour")
    limb(head(side + "ForeArm"), tail(side + "ForeArm"), 0.045, 0.055, side + "ForeArm", "plate")
    limb(head(side + "ForeArm") + (tail(side + "ForeArm") - head(side + "ForeArm")) * 0.45,
         head(side + "ForeArm") + (tail(side + "ForeArm") - head(side + "ForeArm")) * 0.5, 0.058, 0.058, side + "ForeArm", "ember")
    hand = head(side + "Hand")
    d = (tail(side + "Hand") - hand)
    blob(hand + d * 0.6, (0.06, 0.055, 0.055), side + "Hand", "armour")
    limb(hand + d * 0.2, hand + d * 1.3, 0.012, 0.004, side + "Hand", "edge", 4)
    # Spikes on the knuckles.
    for k in (-1, 0, 1):
        limb(hand + d * 0.9 + V((0, 0.02 * k, 0.02)), hand + d * 1.25 + V((0, 0.02 * k, 0.03)), 0.01, 0.002, side + "Hand", "horn", 4)
# Legs: thick greaves and sabatons.
for side in ("Left", "Right"):
    limb(head(side + "UpLeg"), tail(side + "UpLeg"), 0.06, 0.05, side + "UpLeg", "armour")
    limb(head(side + "Leg"), tail(side + "Leg"), 0.05, 0.045, side + "Leg", "plate")
    blob(head(side + "Leg") + V((0, 0, 0.03)), (0.045, 0.04, 0.035), side + "Leg", "edge")
    foot = head(side + "Foot")
    toe = tail(side + "ToeBase")
    block((foot + toe) / 2 + V((0, -0.01, 0.0)), (0.07, 0.05, (toe - foot).length + 0.04), side + "Foot", "armour",
          rot=mathutils.Matrix.Rotation(math.atan2((toe - foot).x, (toe - foot).z), 3, "Y"))

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
cells = 4
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
material = bpy.data.materials.new("Warden")
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
    source = os.path.join(GHOUL, "ghoul_%s.fbx" % clip)
    shutil.copyfile(source, os.path.join(OUT, "%s_%s.fbx" % (NAME, clip)))
print("REPORT wrote", OUT, len(mesh.polygons), "faces,", len(groups), "bones")
