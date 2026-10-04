# The Wild Arrow's worn gear (the owner, 2026-10-04: "making models for other gear that show up on the character when
# equipped. Like chest armor and helmets"), built in Blender from code on her own rig and exported as bodies for the
# sprite bake (WildArrowBakeSetup): wild_arrow_body_<look>.fbx, her mesh with the chest armour bound to her bones
# (bare: her alone; padded, leather, mail), and wild_arrow_helm_<look>.fbx, only a helm bound to her head (cap, nasal,
# great), which the bake draws into the helm layer with her body cutting it. Every part is a solid weighted wholly to
# one bone (rigid; a skirt plate to a thigh, so it swings with the leg). All exports go through this one path, so they
# share scale and placement. The texture is hers with the armour's colours in a strip along the bottom: her UVs are
# squeezed into the top fifteen sixteenths, each armour face maps to its colour's square: wild_arrow_gear_albedo.png,
# shared by every export.
#   Blender -b -P ArtSource/tools/props/wild_arrow_gear.py
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh, mathutils
import numpy as np
from kit import STONE, WOOD, BLOOD, EMBER, BONE, SKIN, COLD

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
FOLDER = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "WildArrow")
SOURCE = os.path.join(FOLDER, "wild_arrow.fbx")
ALBEDO = os.path.join(FOLDER, "wild_arrow_albedo.png")
V = mathutils.Vector

COLORS = [
    ("linen", BONE[1]), ("linen_dark", BONE[0]), ("quilt", WOOD[4]),
    ("leather", WOOD[2]), ("leather_dark", WOOD[1]), ("strap", WOOD[0]), ("buckle", EMBER[3]),
    ("mail", STONE[4]), ("mail_dark", STONE[3]), ("plate", STONE[5]), ("tabard", BLOOD[2]), ("tabard_trim", EMBER[3]),
    ("iron", STONE[3]), ("iron_dark", STONE[1]), ("slit", STONE[0]), ("cap", WOOD[3]),
]
NAMES = [c[0] for c in COLORS]
STRIP = 16   # the strip's height is 1/STRIP of the texture


def load():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE)
    arm = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
    body = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
    return arm, body


def head(arm, name):
    return arm.matrix_world.inverted() @ (arm.matrix_world @ V(arm.data.bones["mixamorig:" + name].head_local))


class Builder:
    """Solids weighted to bones, in the armature's space (y up, z forward, x the character's left; metres)."""

    def __init__(self, arm):
        self.arm = arm
        self.bm = bmesh.new()
        self.deform = self.bm.verts.layers.deform.verify()
        self.colors = []
        self.groups = {}

    def h(self, name):
        return V(self.arm.data.bones["mixamorig:" + name].head_local)

    def t(self, name):
        return V(self.arm.data.bones["mixamorig:" + name].tail_local)

    def add(self, verts, faces, bone, color):
        g = self.groups.setdefault("mixamorig:" + bone, len(self.groups))
        vs = []
        for co in verts:
            v = self.bm.verts.new(co)
            v[self.deform][g] = 1.0
            vs.append(v)
        for f in faces:
            try:
                self.bm.faces.new([vs[i] for i in f])
                self.colors.append(NAMES.index(color))
            except ValueError:
                pass

    def box(self, centre, size, bone, color, rot=None):
        sx, sy, sz = (s / 2 for s in size)
        corners = [V((x, y, z)) for x in (-sx, sx) for y in (-sy, sy) for z in (-sz, sz)]
        if rot is not None:
            corners = [rot @ c for c in corners]
        faces = [[0, 1, 3, 2], [4, 6, 7, 5], [0, 4, 5, 1], [2, 3, 7, 6], [0, 2, 6, 4], [1, 5, 7, 3]]
        self.add([centre + c for c in corners], faces, bone, color)

    def ring(self, centre, rx, rz, height, bone, color, sides=10, top_scale=1.0):
        """A tube around the y axis (open ended), radii rx and rz, from centre up height."""
        verts = []
        for level, scale in ((0, 1.0), (1, top_scale)):
            for k in range(sides):
                a = 2 * math.pi * k / sides
                verts.append(centre + V((math.cos(a) * rx * scale, height * level, math.sin(a) * rz * scale)))
        # Wound so the faces look outward.
        faces = [[sides + k, sides + (k + 1) % sides, (k + 1) % sides, k] for k in range(sides)]
        self.add(verts, faces, bone, color)

    def dome(self, centre, rx, ry, rz, bone, color, segs=10, cut=0.0):
        """A half ellipsoid opening downward from centre (cut lifts the rim)."""
        verts = []
        rings = 5
        for i in range(rings + 1):
            phi = (math.pi / 2) * i / rings
            for k in range(segs):
                a = 2 * math.pi * k / segs
                verts.append(centre + V((math.cos(phi) * math.cos(a) * rx, math.sin(phi) * ry + cut, math.cos(phi) * math.sin(a) * rz)))
        faces = []
        for i in range(rings):
            for k in range(segs):
                j = (k + 1) % segs
                faces.append([i * segs + k, i * segs + j, (i + 1) * segs + j, (i + 1) * segs + k])
        self.add(verts, faces, bone, color)

    def limb(self, p0, p1, r0, r1, bone, color, sides=8):
        d = (p1 - p0).normalized()
        up = V((0, 1, 0)) if abs(d.y) < 0.9 else V((1, 0, 0))
        a = d.cross(up).normalized()
        b = d.cross(a).normalized()
        verts = []
        for p, r in ((p0, r0), (p1, r1)):
            for k in range(sides):
                ang = 2 * math.pi * k / sides
                verts.append(p + (a * math.cos(ang) + b * math.sin(ang)) * r)
        faces = [[k, (k + 1) % sides, sides + (k + 1) % sides, sides + k] for k in range(sides)]
        faces.append(list(reversed(range(sides))))
        faces.append(list(range(sides, 2 * sides)))
        self.add(verts, faces, bone, color)

    def mesh(self, name):
        mesh = bpy.data.meshes.new(name)
        # Outward normals everywhere (the tubes were built facing in, and the bake culls back faces).
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces[:])
        self.bm.normal_update()
        self.bm.to_mesh(mesh)
        self.bm.free()
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        for bone, index in sorted(self.groups.items(), key=lambda p: p[1]):
            obj.vertex_groups.new(name=bone)
        uv = mesh.uv_layers.new(name="UVMap").data
        for poly, c in zip(mesh.polygons, self.colors):
            u, v = (c + 0.5) / len(COLORS), 0.5 / STRIP
            for li in poly.loop_indices:
                uv[li].uv = (u, v)
        obj.parent = self.arm
        obj.matrix_parent_inverse = mathutils.Matrix.Identity(4)
        obj.modifiers.new("Armature", "ARMATURE").object = self.arm
        return obj


def extents(body, y0, y1, xlimit=0.19):
    """Her body's half width (x) and depth (z, front and back) between two heights, from her mesh at rest."""
    co = np.array([v.co[:] for v in body.data.vertices])
    world = np.array(body.matrix_world)
    pts = (np.c_[co, np.ones(len(co))] @ world.T)[:, :3]
    arm_inv = np.array(body.parent.matrix_world.inverted())
    pts = (np.c_[pts, np.ones(len(pts))] @ arm_inv.T)[:, :3]
    sel = pts[(pts[:, 1] > y0) & (pts[:, 1] < y1) & (np.abs(pts[:, 0]) < xlimit)]
    if len(sel) == 0:
        return 0.15, 0.1, -0.1
    return np.abs(sel[:, 0]).max(), sel[:, 2].max(), sel[:, 2].min()


def torso(b, body, color, flare=1.0, margin=0.025):
    """A shell over her torso from the hips to the shoulders, in three bands (hips, belly, chest)."""
    bands = [("Hips", "Spine"), ("Spine", "Spine2"), ("Spine2", "Neck")]
    for lower, upper in bands:
        y0, y1 = b.h(lower).y, b.h(upper).y
        if lower == "Hips":
            y0 -= 0.06
        half, front, back = extents(body, y0, y1)
        cz = (front + back) / 2
        rx, rz = half + margin, (front - back) / 2 + margin
        b.ring(V((0, y0, cz)), rx * flare, rz, y1 - y0, lower if lower != "Hips" else "Hips", color, 12)
    # A top plate over the shoulders.
    y = b.h("Neck").y
    half, front, back = extents(body, y - 0.08, y)
    b.box(V((0, y - 0.01, (front + back) / 2)), (half * 2 + 0.06, 0.03, front - back + 0.05), "Spine2", color)


def padded(b, body):
    torso(b, body, "linen", 1.05, 0.03)
    # Quilting: darker bands around the torso.
    for bone in ("Spine", "Spine1", "Spine2"):
        y = b.h(bone).y
        half, front, back = extents(body, y - 0.02, y + 0.02)
        b.ring(V((0, y, (front + back) / 2)), half + 0.035, (front - back) / 2 + 0.035, 0.012, bone, "quilt", 12)
    for side in ("Left", "Right"):
        b.limb(b.h(side + "Arm"), b.h(side + "Arm") + (b.h(side + "ForeArm") - b.h(side + "Arm")) * 0.8, 0.085, 0.075, side + "Arm", "linen")
        knee = b.h(side + "Leg")
        hip = b.h(side + "UpLeg")
        b.limb(hip + V((0, -0.02, 0)), hip + (knee - hip) * 0.55, 0.135, 0.125, side + "UpLeg", "linen_dark")
    b.ring(V((0, b.h("Hips").y + 0.02, 0.0)), 0.17, 0.12, 0.05, "Hips", "strap", 12)


def leather(b, body):
    torso(b, body, "leather", 1.0, 0.025)
    for side, sx in (("Left", 1), ("Right", -1)):
        arm = b.h(side + "Arm")
        b.dome(arm + V((0, 0.02, 0)), 0.11, 0.08, 0.11, side + "Arm", "leather_dark", 8)
        fa, hand = b.h(side + "ForeArm"), b.h(side + "Hand")
        b.limb(fa + (hand - fa) * 0.3, fa + (hand - fa) * 0.92, 0.06, 0.055, side + "ForeArm", "leather_dark")
        knee, hip = b.h(side + "Leg"), b.h(side + "UpLeg")
        b.box(hip + (knee - hip) * 0.35 + V((sx * 0.02, 0, 0.12)), (0.16, 0.22, 0.03), side + "UpLeg", "leather_dark")
    # Cross straps and a belt with a buckle.
    y0, y1 = b.h("Spine").y, b.h("Neck").y
    half, front, back = extents(body, y0, y1)
    for sgn in (1, -1):
        rot = mathutils.Matrix.Rotation(math.radians(35 * sgn), 3, "Z")
        b.box(V((0, (y0 + y1) / 2, front + 0.03)), (0.035, (y1 - y0) * 1.15, 0.01), "Spine1", "strap", rot)
    hips = b.h("Hips")
    b.ring(V((0, hips.y + 0.02, 0.0)), 0.17, 0.125, 0.045, "Hips", "strap", 12)
    b.box(V((0, hips.y + 0.045, 0.13)), (0.06, 0.05, 0.015), "Hips", "buckle")


def mail(b, body):
    torso(b, body, "mail", 1.05, 0.03)
    # Mail skirt plates, one per thigh, and a tabard over the front and back.
    for side, sx in (("Left", 1), ("Right", -1)):
        knee, hip = b.h(side + "Leg"), b.h(side + "UpLeg")
        b.limb(hip + V((0, -0.01, 0)), hip + (knee - hip) * 0.6, 0.14, 0.135, side + "UpLeg", "mail_dark")
        arm = b.h(side + "Arm")
        b.dome(arm + V((0, 0.03, 0)), 0.125, 0.09, 0.12, side + "Arm", "plate", 10)
        b.limb(arm, arm + (b.h(side + "ForeArm") - arm) * 0.9, 0.08, 0.072, side + "Arm", "mail_dark")
    y0, y1 = b.h("Hips").y - 0.25, b.h("Neck").y - 0.03
    half, front, back = extents(body, b.h("Spine1").y - 0.05, b.h("Spine1").y + 0.05)
    b.box(V((0, (y0 + y1) / 2, front + 0.045)), (0.2, y1 - y0, 0.01), "Spine1", "tabard")
    b.box(V((0, (y0 + y1) / 2, back - 0.045)), (0.2, y1 - y0, 0.01), "Spine1", "tabard")
    b.box(V((0, y1 - 0.12, front + 0.052)), (0.08, 0.1, 0.006), "Spine2", "tabard_trim")
    hips = b.h("Hips")
    b.ring(V((0, hips.y + 0.03, 0.0)), 0.18, 0.135, 0.045, "Hips", "strap", 12)


def helm_size(b, body):
    top = b.h("HeadTop_End").y
    base = b.h("Head").y
    half, front, back = extents(body, base, top + 0.05, 0.2)
    return base, top, half, front, back


def cap(b, body):
    # A close leather cap on the crown, above the brow.
    base, top, half, front, back = helm_size(b, body)
    cz = (front + back) / 2 - 0.01
    rx, rz = half * 0.85 + 0.012, (front - back) / 2 * 0.85 + 0.012
    y = base + (top - base) * 0.62
    b.dome(V((0, y, cz)), rx, (top - y) + 0.02, rz, "Head", "cap", 12)
    b.ring(V((0, y - 0.012, cz)), rx + 0.006, rz + 0.006, 0.024, "Head", "strap", 12)


def nasal(b, body):
    # A conical iron helm down to the brow, a band round its rim and a bar down over the nose.
    base, top, half, front, back = helm_size(b, body)
    cz = (front + back) / 2 - 0.01
    rx, rz = half * 0.88 + 0.02, (front - back) / 2 * 0.88 + 0.02
    y = base + (top - base) * 0.55
    b.dome(V((0, y, cz)), rx, (top - y) + 0.06, rz, "Head", "iron", 12)
    b.ring(V((0, y - 0.015, cz)), rx + 0.005, rz + 0.005, 0.03, "Head", "iron_dark", 12)
    b.box(V((0, y - 0.05, cz + rz + 0.004)), (0.022, 0.08, 0.012), "Head", "iron_dark")


def great(b, body):
    base, top, half, front, back = helm_size(b, body)
    cz = (front + back) / 2
    y0 = base - 0.04
    rx, rz = half + 0.04, (front - back) / 2 + 0.04
    b.ring(V((0, y0, cz)), rx, rz, top - y0 + 0.02, "Head", "plate", 12, 0.92)
    b.dome(V((0, top + 0.02, cz)), rx * 0.92, 0.035, rz * 0.92, "Head", "plate", 12)
    eye = base + (top - base) * 0.5
    b.box(V((0, eye, cz + rz - 0.004)), (rx * 1.3, 0.022, 0.02), "Head", "slit")
    b.box(V((0, (y0 + top) / 2, cz + rz + 0.002)), (0.02, top - y0, 0.012), "Head", "iron_dark")
    for k in range(3):
        b.box(V((0.035, eye - 0.05 - k * 0.025, cz + rz)), (0.012, 0.012, 0.015), "Head", "slit")
        b.box(V((-0.035, eye - 0.05 - k * 0.025, cz + rz)), (0.012, 0.012, 0.015), "Head", "slit")


def texture(path):
    """Her albedo squeezed into the top, the colours' squares along the bottom; written beside the FBX."""
    image = bpy.data.images.load(ALBEDO)
    w, h = image.size
    px = np.array(image.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(image)
    strip = h // STRIP
    rows = np.linspace(0, h - 1, h - strip).astype(int)
    out = np.zeros_like(px)
    out[strip:] = px[rows]
    cell = w // len(COLORS)
    for i, (_, c) in enumerate(COLORS):
        out[:strip, i * cell:(i + 1) * cell, :3] = np.array(c) / 255.0
        out[:strip, i * cell:(i + 1) * cell, 3] = 1
    image = bpy.data.images.new("albedo", w, h, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    image.pixels = out.ravel()
    image.filepath_raw = path
    image.file_format = "PNG"
    image.save()


def squeeze_her_uvs(body):
    for loop in body.data.uv_layers.active.data:
        loop.uv = (loop.uv[0], 1.0 / STRIP + loop.uv[1] * (1 - 1.0 / STRIP))


def export(arm, objects, path):
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for o in objects:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, add_leaf_bones=False, bake_anim=False,
                             path_mode="STRIP", embed_textures=False)
    print("REPORT wrote", os.path.basename(path))


# One texture for every export (WildArrowBakeSetup puts it on each).
texture(os.path.join(FOLDER, "wild_arrow_gear_albedo.png"))

BODIES = {"bare": None, "padded": padded, "leather": leather, "mail": mail}
HELMS = {"cap": cap, "nasal": nasal, "great": great}

for look, build in BODIES.items():
    arm, body = load()
    squeeze_her_uvs(body)
    parts = [body]
    if build is not None:
        b = Builder(arm)
        build(b, body)
        armour = b.mesh("Armour")
        # One mesh with one material slot each: hers.
        armour.data.materials.append(body.data.materials[0])
        parts.append(armour)
    export(arm, parts, os.path.join(FOLDER, "wild_arrow_body_%s.fbx" % look))

for look, build in HELMS.items():
    arm, body = load()
    b = Builder(arm)
    build(b, body)
    helm = b.mesh("Helm")
    helm.data.materials.append(body.data.materials[0])
    bpy.data.objects.remove(body, do_unlink=True)
    export(arm, [helm], os.path.join(FOLDER, "wild_arrow_helm_%s.fbx" % look))
