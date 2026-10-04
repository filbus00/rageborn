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


# ---------------------------------------------------------------- shells: armour wrapped on her own surface

def dominant_bones(body):
    """Each vertex's bone of greatest weight (without the mixamorig: prefix)."""
    names = {g.index: g.name.split(":")[-1] for g in body.vertex_groups}
    out = []
    for v in body.data.vertices:
        best, weight = None, 0.0
        for g in v.groups:
            if g.weight > weight:
                best, weight = names.get(g.group), g.weight
        out.append(best)
    return out


def arm_space(body):
    to_arm = body.parent.matrix_world.inverted() @ body.matrix_world
    return [to_arm @ v.co for v in body.data.vertices], to_arm


def shell(arm, body, keep, offset, color_of, smooth=2, name="Shell"):
    """A copy of her surface where keep(position, bone) holds for every corner of a face, pushed out along its normals by
    offset (metres), smoothed, its faces coloured by color_of(centre, normal) in the armature's space. It keeps her
    weights, so it bends with her."""
    positions, to_arm = arm_space(body)
    bones = dominant_bones(body)
    obj = body.copy()
    obj.data = body.data.copy()
    obj.name = name
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    wanted = [keep(positions[i], bones[i]) for i in range(len(bm.verts))]
    drop = [f for f in bm.faces if not all(wanted[v.index] for v in f.verts)]
    bmesh.ops.delete(bm, geom=drop, context="FACES_ONLY")
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    bm.normal_update()
    for v in bm.verts:
        v.co += v.normal * (offset / max(1e-6, to_arm.to_scale()[0]))
    for k in range(smooth):
        bmesh.ops.smooth_vert(bm, verts=bm.verts[:], factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    bm.normal_update()
    uv = bm.loops.layers.uv.active or bm.loops.layers.uv.new()
    for f in bm.faces:
        centre = to_arm @ f.calc_center_median()
        normal = (to_arm.to_3x3() @ f.normal).normalized()
        c = NAMES.index(color_of(centre, normal))
        for loop in f.loops:
            loop[uv].uv = ((c + 0.5) / len(COLORS), 0.5 / STRIP)
    bm.to_mesh(obj.data)
    bm.free()
    return obj


TORSO = {"Hips", "Spine", "Spine1", "Spine2", "LeftShoulder", "RightShoulder"}


def torso_keep(b, extra=(), hip_drop=0.06):
    hips_y, neck_y = b.h("Hips").y, b.h("Neck").y

    def keep(p, bone):
        if bone in TORSO:
            return hips_y - hip_drop < p.y < neck_y - 0.01
        return bone in extra
    return keep


def thigh_keep(b, length):
    def keep(p, bone):
        if bone not in ("LeftUpLeg", "RightUpLeg"):
            return False
        side = "Left" if bone.startswith("Left") else "Right"
        hip, knee = b.h(side + "UpLeg"), b.h(side + "Leg")
        return p.y > hip.y + (knee.y - hip.y) * length
    return keep


def any_of(*keeps):
    return lambda p, bone: any(k(p, bone) for k in keeps)


def padded(b, body):
    hips_y = b.h("Hips").y

    def color(c, n):
        if (c.y * 16) % 1 < 0.14:
            return "linen_dark"
        if abs(c.y - hips_y - 0.02) < 0.025:
            return "strap"
        return "linen"
    keep = any_of(torso_keep(b, ("LeftArm", "RightArm")), thigh_keep(b, 0.5))
    return [shell(b.arm, body, keep, 0.022, color, 3, "Gambeson")]


def leather(b, body):
    hips_y, neck_y = b.h("Hips").y, b.h("Neck").y
    mid = (hips_y + neck_y) / 2

    def color(c, n):
        if abs(c.y - hips_y - 0.03) < 0.025:
            return "buckle" if c.z > 0.08 and abs(c.x) < 0.03 else "strap"
        if c.z > 0 and abs(c.x - (c.y - mid) * 0.9) < 0.022:
            return "strap"
        if c.z < 0 and abs(c.x + (c.y - mid) * 0.9) < 0.022:
            return "strap"
        return "leather_dark" if (c.y * 9) % 1 < 0.12 else "leather"
    parts = [shell(b.arm, body, torso_keep(b, (), 0.08), 0.014, color, 2, "Jerkin")]
    for side in ("Left", "Right"):
        arm = b.h(side + "Arm")
        b.dome(arm + V((0, 0.02, 0)), 0.1, 0.07, 0.1, side + "Arm", "leather_dark", 10)
        fa, hand = b.h(side + "ForeArm"), b.h(side + "Hand")
        b.limb(fa + (hand - fa) * 0.35, fa + (hand - fa) * 0.92, 0.05, 0.045, side + "ForeArm", "leather_dark")
    return parts


def mail(b, body):
    hips_y, neck_y = b.h("Hips").y, b.h("Neck").y

    def color(c, n):
        if abs(c.x) < 0.085 and c.y < neck_y - 0.08 and abs(c.z) > 0.02:
            return "tabard_trim" if abs(abs(c.x) - 0.08) < 0.008 else "tabard"
        if abs(c.y - hips_y - 0.02) < 0.022:
            return "strap"
        return "mail_dark" if (int(c.x * 60) + int(c.y * 60)) % 2 else "mail"
    keep = any_of(torso_keep(b, ("LeftArm", "RightArm"), 0.1), thigh_keep(b, 0.6))
    parts = [shell(b.arm, body, keep, 0.016, color, 2, "Hauberk")]
    for side in ("Left", "Right"):
        b.dome(b.h(side + "Arm") + V((0, 0.03, 0)), 0.12, 0.085, 0.115, side + "Arm", "plate", 12)
    return parts


def helm_size(b, body):
    top = b.h("HeadTop_End").y
    base = b.h("Head").y
    half, front, back = extents(body, base, top + 0.05, 0.2)
    return base, top, half, front, back


HEAD = {"Head", "HeadTop_End", "Neck"}


def cap(b, body):
    base, top, half, front, back = helm_size(b, body)
    brow = base + (top - base) * 0.55
    keep = lambda p, bone: bone in HEAD and p.y > brow
    color = lambda c, n: "strap" if c.y < brow + 0.022 else "cap"
    return [shell(b.arm, body, keep, 0.016, color, 40, "Cap")]


def nasal(b, body):
    base, top, half, front, back = helm_size(b, body)
    brow = base + (top - base) * 0.45
    keep = lambda p, bone: bone in HEAD and p.y > brow
    color = lambda c, n: "iron_dark" if c.y < brow + 0.025 else "iron"
    parts = [shell(b.arm, body, keep, 0.024, color, 40, "Nasal")]
    b.box(V((0, brow - 0.035, front + 0.025)), (0.022, 0.08, 0.014), "Head", "iron_dark")
    return parts


def great(b, body):
    base, top, half, front, back = helm_size(b, body)
    eye = base + (top - base) * 0.48
    keep = lambda p, bone: bone in HEAD and p.y > base - 0.04
    def color(c, n):
        if c.z > (front + back) / 2 + 0.03 and abs(c.y - eye) < 0.014:
            return "slit"
        if c.z > (front + back) / 2 + 0.03 and abs(c.x) < 0.012 and c.y < eye:
            return "iron_dark"
        return "plate"
    return [shell(b.arm, body, keep, 0.034, color, 40, "GreatHelm")]


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
        parts += build(b, body)
        if b.colors:
            armour = b.mesh("Armour")
            # One mesh with one material slot each: hers.
            armour.data.materials.append(body.data.materials[0])
            parts.append(armour)
    export(arm, parts, os.path.join(FOLDER, "wild_arrow_body_%s.fbx" % look))

for look, build in HELMS.items():
    arm, body = load()
    b = Builder(arm)
    parts = build(b, body)
    if b.colors:
        helm = b.mesh("Helm")
        helm.data.materials.append(body.data.materials[0])
        parts.append(helm)
    bpy.data.objects.remove(body, do_unlink=True)
    export(arm, parts, os.path.join(FOLDER, "wild_arrow_helm_%s.fbx" % look))
