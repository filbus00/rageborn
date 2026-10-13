# The Wild Arrow's own body (2026-10-13, the owner: "remove all imported meshy models. Keep the animations for the main
# character"), built in Blender from code on her rig in place of the Meshy model, so the Pro Longbow Pack's clips
# play on it as before. Her rig is ArtSource/rigs/wild_arrow_rig.blend (the Mixamo skeleton, no mesh). A lean archer:
# auburn hair in a ponytail, a dark green tunic over the chest and hips, bare upper arms, leather bracers, trousers and
# boots, a hand of fingers on her finger bones so the bow grip still closes. Every part is a solid weighted wholly to
# one bone. Writes Assets/_Project/Art/Models/WildArrow/wild_arrow.fbx and wild_arrow_albedo.png (the gear palette's
# squares along the bottom, as wild_arrow_gear.py expects her texture). Then run wild_arrow_gear.py (every look is
# built on her) and gear.py.
#   Blender -b -P ArtSource/tools/props/wild_arrow_body.py
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, mathutils
import numpy as np
import wild_arrow_gear as g

V = mathutils.Vector
RIG = os.path.join(g.ROOT, "ArtSource", "rigs", "wild_arrow_rig.blend")
CELL = 32


def albedo():
    """The palette's squares along the bottom sixteenth (where the Builder's faces sample), the rest the tunic."""
    n = len(g.COLORS)
    w, h = n * CELL, CELL * g.STRIP
    px = np.zeros((h, w, 4), dtype=np.float32)
    px[:, :, :3] = np.array(dict(g.COLORS)["tunic"]) / 255.0
    px[:, :, 3] = 1
    for i, (_, c) in enumerate(g.COLORS):
        px[:CELL, i * CELL:(i + 1) * CELL, :3] = np.array(c) / 255.0
    image = bpy.data.images.new("albedo", w, h, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    image.pixels = px.ravel()
    image.filepath_raw = g.ALBEDO
    image.file_format = "PNG"
    image.save()
    return image


def torso_bones(y):
    """Weights by height down the trunk, blended over 4 cm at each joint, so the trunk bends instead of tearing."""
    joints = [(1.07, "Hips", "Spine"), (1.2, "Spine", "Spine1"), (1.32, "Spine1", "Spine2")]
    below, _, _ = joints[0]
    if y < joints[0][0] - 0.04:
        return [("Hips", 1.0)]
    for i, (at, lower, upper) in enumerate(joints):
        top = joints[i + 1][0] - 0.04 if i + 1 < len(joints) else 9.0
        if y < at + 0.04:
            t = (y - (at - 0.04)) / 0.08
            return [(lower, 1 - t), (upper, t)]
        if y < top:
            return [(upper, 1.0)]
    return [("Spine2", 1.0)]


# The trunk's profile, bottom to top: (height, half width, half depth, forward shift, colour of the band above it).
TRUNK = [
    (0.9, 0.135, 0.095, 0.0, "trousers"), (0.97, 0.15, 0.104, 0.0, "trousers"), (1.03, 0.148, 0.1, 0.0, "tunic_dark"),
    (1.1, 0.128, 0.088, 0.004, "tunic"), (1.18, 0.118, 0.084, 0.008, "tunic"), (1.26, 0.124, 0.088, 0.01, "tunic"),
    (1.33, 0.138, 0.096, 0.012, "tunic"), (1.4, 0.148, 0.094, 0.006, "tunic"), (1.445, 0.13, 0.08, -0.004, "tunic"),
    (1.475, 0.075, 0.055, -0.01, "skin"), (1.49, 0.04, 0.038, -0.015, "skin"),
]


def trunk(b, sides=18):
    rings = []
    for y, rx, rz, dz, _ in TRUNK:
        rings.append([V((math.cos(2 * math.pi * k / sides) * rx, y, dz + math.sin(2 * math.pi * k / sides) * rz)) for k in range(sides)])
    for i in range(len(rings) - 1):
        verts = rings[i] + rings[i + 1]
        faces = [[k, (k + 1) % sides, sides + (k + 1) % sides, sides + k] for k in range(sides)]
        if i == 0:
            faces.append(list(reversed(range(sides))))
        if i == len(rings) - 2:
            faces.append(list(range(sides, 2 * sides)))
        b.add(verts, faces, lambda co: torso_bones(co.y), TRUNK[i][4])


def build(b):
    h = b.h
    # Head: a skull of skin with its face to +z, eyes, brows, nose, mouth; hair over the crown, locks to the jaw, a
    # ponytail down the back.
    # Sized to the helms (wild_arrow_gear.HC, HRX: about 0.15 round with the hair, as the Meshy head was): a little
    # large for her body, which reads better at pixel size anyway.
    head = V((0, 1.64, 0.008))
    k = 1.3
    def at(x, y, z):
        return head + V((x * k, y * k, z * k))
    def size(*v):
        return tuple(c * k for c in v)
    b.ball(head, size(0.082, 0.1, 0.09), "Head", "skin", 14)
    b.ball(at(0, -0.055, 0.032), size(0.058, 0.04, 0.058), "Head", "skin", 10)
    for sx in (-1, 1):
        b.box(at(sx * 0.031, 0.004, 0.085), size(0.024, 0.012, 0.01), "Head", "eye_dark")
        b.box(at(sx * 0.031, 0.024, 0.083), size(0.032, 0.008, 0.01), "Head", "hair")
    b.box(at(0, -0.014, 0.094), size(0.016, 0.03, 0.016), "Head", "skin_shade")
    b.box(at(0, -0.05, 0.084), size(0.03, 0.008, 0.01), "Head", "lips")
    b.dome(at(0, 0.014, -0.01), 0.09 * k, 0.096 * k, 0.098 * k, "Head", "hair", 16)
    for sx in (-1, 1):
        b.box(at(sx * 0.077, -0.032, -0.006), size(0.022, 0.1, 0.088), "Head", "hair")
    b.box(at(0, 0.05, 0.064), size(0.13, 0.026, 0.028), "Head", "hair_hi")
    b.ball(at(0, 0.03, -0.098), size(0.035, 0.035, 0.03), "Head", "hair_hi", 8)
    b.limb(at(0, 0.02, -0.105), at(0, -0.19, -0.14), 0.03 * k, 0.013 * k, "Head", "hair", 8)
    b.limb(h("Neck"), h("Head") + V((0, 0.03, 0)), 0.036, 0.036, "Neck", "skin", 10)
    # Torso: one lofted trunk, the tunic over chest and belly, a little bust, a belt with a buckle.
    trunk(b)
    b.band(1.035, 1.07, 0.15, 0.104, 0.146, 0.1, 0.02, "Hips", "leather_dark", segs=18)
    b.box(V((0, 1.052, 0.105)), (0.04, 0.04, 0.012), "Hips", "gold")
    # The tunic's hem, front and back, short of the thighs.
    b.band(0.94, 1.04, 0.15, 0.108, 0.148, 0.104, 0.016, "Hips", "tunic", a0=45, a1=135, segs=18)
    b.band(0.94, 1.04, 0.15, 0.108, 0.148, 0.104, 0.016, "Hips", "tunic", a0=225, a1=315, segs=18)
    for side, sx in (("Left", 1), ("Right", -1)):
        # Shoulder, a short sleeve, the bare upper arm, the forearm in a bracer, the hand and its fingers.
        b.ball(h(side + "Arm") + V((-sx * 0.015, 0.0, 0)), (0.045, 0.042, 0.045), side + "Shoulder", "tunic", 10)
        arm0, arm1 = h(side + "Arm"), h(side + "ForeArm")
        b.limb(arm0, arm0 + (arm1 - arm0) * 0.35, 0.047, 0.043, side + "Arm", "tunic", 10)
        b.limb(arm0, arm1, 0.039, 0.033, side + "Arm", "skin", 10)
        fore, hand = h(side + "ForeArm"), h(side + "Hand")
        b.limb(fore, hand, 0.032, 0.026, side + "ForeArm", "skin", 10)
        b.limb(fore + (hand - fore) * 0.3, fore + (hand - fore) * 0.97, 0.037, 0.031, side + "ForeArm", "leather", 10)
        b.limb(fore + (hand - fore) * 0.92, hand + (hand - fore) * 0.02, 0.033, 0.033, side + "ForeArm", "leather_dark", 10)
        d = (b.t(side + "Hand") - hand).normalized()
        b.box(hand + d * 0.05 + V((0, -0.004, 0.0)), (0.09, 0.026, 0.07) if sx else (0.09, 0.026, 0.07), side + "Hand", "skin")
        for finger in ("Thumb", "Index", "Middle", "Ring"):
            for i in (1, 2, 3):
                bone = "mixamorig:%sHand%s%d" % (side, finger, i)
                nxt = "mixamorig:%sHand%s%d" % (side, finger, i + 1)
                if bone not in b.arm.data.bones or nxt not in b.arm.data.bones:
                    continue
                p0 = V(b.arm.data.bones[bone].head_local)
                p1 = V(b.arm.data.bones[nxt].head_local)
                b.limb(p0, p1 + (p1 - p0) * (0.4 if i == 3 else 0.0), 0.011, 0.009, "%sHand%s%d" % (side, finger, i), "skin", 6)
        # Leg: a full buckskin thigh, a leather knee patch, the dark boot to below the knee with a pale cuff, the foot.
        # (2026-10-13, the owner: the run looked like "spaghetti legs". Trousers and boots were one dark brown, as the
        # trunk's bottom, so in a stride both legs read as one thin arc; now the thigh, knee and boot each show.)
        up, knee, ankle = h(side + "UpLeg"), h(side + "Leg"), h(side + "Foot")
        b.limb(up + V((0, 0.03, 0)), knee, 0.104, 0.066, side + "UpLeg", "trousers", 10)
        b.ball(knee + V((0, 0.005, 0.02)), (0.05, 0.055, 0.045), side + "Leg", "leather", 10)
        b.limb(knee, ankle, 0.058, 0.042, side + "Leg", "trousers", 10)
        b.limb(knee + (ankle - knee) * 0.2, ankle + V((0, -0.02, 0)), 0.058, 0.047, side + "Leg", "boot", 10)
        b.band(knee.y - (knee.y - ankle.y) * 0.24, knee.y - (knee.y - ankle.y) * 0.17, 0.064, 0.064, 0.064, 0.064, 0.014,
               side + "Leg", "leather_hi", cx=knee.x + (ankle.x - knee.x) * 0.2, cz=knee.z + (ankle.z - knee.z) * 0.2, segs=12)
        toe = h(side + "ToeBase")
        end = h(side + "Toe_End")
        b.limb(ankle + V((0, -0.03, -0.02)), toe + V((0, 0.0, 0)), 0.045, 0.04, side + "Foot", "boot", 10)
        b.limb(toe, end + V((0, 0.0, 0.0)), 0.04, 0.03, side + "ToeBase", "boot", 10)


def main():
    bpy.ops.wm.open_mainfile(filepath=RIG)
    arm = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
    arm.animation_data_clear()
    image = albedo()
    b = g.Builder(arm)
    build(b)
    body = b.mesh("Body")
    material = bpy.data.materials.new("wild_arrow")
    material.use_nodes = True
    tex = material.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = image
    material.node_tree.links.new(tex.outputs["Color"], material.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    body.data.materials.append(material)
    g.export(arm, [body], g.SOURCE)
    print("REPORT body", len(body.data.polygons), "faces,", len(body.vertex_groups), "bones,", len(g.COLORS), "colours")


main()
