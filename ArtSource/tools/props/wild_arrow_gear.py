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
from kit import STONE, WOOD, BLOOD, EMBER, BONE, SKIN, COLD, MOSS, VIOLET, EARTH

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
FOLDER = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "WildArrow")
SOURCE = os.path.join(FOLDER, "wild_arrow.fbx")
ALBEDO = os.path.join(FOLDER, "wild_arrow_albedo.png")
V = mathutils.Vector

# The gear's colours (2026-10-05, the owner: the gear "looks bad"; remade darker and with more contrast, so it reads at
# pixel size): blackened and worn iron with lighter edges, dark leathers, deep red and green cloth, gold and ember for
# accents. Each piece is built over her (pauldrons, plates, helms, horns), not only wrapped on her, so it changes her
# outline.
COLORS = [
    ("iron", STONE[2]), ("iron_hi", STONE[3]), ("iron_edge", STONE[4]), ("iron_dark", STONE[1]), ("black", STONE[0]),
    ("leather", WOOD[2]), ("leather_dark", WOOD[1]), ("leather_hi", WOOD[3]), ("strap", WOOD[0]),
    ("red", BLOOD[1]), ("red_hi", BLOOD[2]), ("green", MOSS[1]), ("green_hi", MOSS[2]),
    ("gold", EMBER[2]), ("gold_dark", EMBER[1]), ("bronze", WOOD[4]),
    ("fur", EARTH[3]), ("fur_hi", EARTH[5]), ("fur_dark", EARTH[1]),
    ("bone", BONE[0]), ("bone_hi", BONE[1]), ("ember", EMBER[4]), ("ember_mid", EMBER[3]),
    ("violet", VIOLET[1]), ("violet_dark", VIOLET[0]), ("eye", BLOOD[4]),
    ("feather", BONE[2]), ("feather_dark", EARTH[2]), ("cloth", EARTH[2]), ("cloth_dark", EARTH[1]), ("slit", (14, 10, 10)),
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

    def band(self, y0, y1, rx0, rz0, rx1, rz1, thick, bone, color, a0=0.0, a1=360.0, cz=0.0, segs=16, cx=0.0):
        """A solid wall around the y axis between heights y0 and y1: an ellipse of radii (rx0, rz0) at the bottom and
        (rx1, rz1) at the top, thick inward, over the angles a0 to a1 (degrees; 90 is her front, +z), centred on
        (cx, cz). Closed at the ends, so it reads from every side."""
        full = (a1 - a0) >= 359.9
        n = segs if full else max(2, int(segs * (a1 - a0) / 360.0) + 1)
        verts = []
        for (y, rx, rz) in ((y0, rx0, rz0), (y1, rx1, rz1)):
            for inner in (0, 1):
                k = 1.0 - (thick / max(rx, 1e-3)) * inner
                for i in range(n):
                    ang = math.radians(a0 + (a1 - a0) * (i / n if full else i / (n - 1)))
                    verts.append(V((cx + math.cos(ang) * rx * k, y, cz + math.sin(ang) * rz * k)))
        # rows: 0 bottom outer, 1 bottom inner, 2 top outer, 3 top inner
        def idx(row, i):
            return row * n + (i % n)
        faces = []
        last = n if full else n - 1
        for i in range(last):
            faces.append([idx(0, i), idx(0, i + 1), idx(2, i + 1), idx(2, i)])      # outside
            faces.append([idx(3, i), idx(3, i + 1), idx(1, i + 1), idx(1, i)])      # inside
            faces.append([idx(2, i), idx(2, i + 1), idx(3, i + 1), idx(3, i)])      # top
            faces.append([idx(1, i), idx(1, i + 1), idx(0, i + 1), idx(0, i)])      # bottom
        if not full:
            faces.append([idx(0, 0), idx(2, 0), idx(3, 0), idx(1, 0)])
            faces.append([idx(1, n - 1), idx(3, n - 1), idx(2, n - 1), idx(0, n - 1)])
        self.add(verts, faces, bone, color)

    def ball(self, centre, radii, bone, color, segs=10, rot=None):
        """A closed ellipsoid."""
        verts = []
        rings = segs // 2
        for i in range(rings + 1):
            phi = math.pi * i / rings
            for k in range(segs):
                a = 2 * math.pi * k / segs
                p = V((math.sin(phi) * math.cos(a) * radii[0], math.cos(phi) * radii[1], math.sin(phi) * math.sin(a) * radii[2]))
                verts.append(centre + (rot @ p if rot is not None else p))
        faces = []
        for i in range(rings):
            for k in range(segs):
                j = (k + 1) % segs
                faces.append([i * segs + k, i * segs + j, (i + 1) * segs + j, (i + 1) * segs + k])
        self.add(verts, faces, bone, color)

    def spike(self, base, tip, radius, bone, color, sides=6):
        """A cone from a base point to a tip."""
        d = (tip - base).normalized()
        up = V((0, 1, 0)) if abs(d.y) < 0.9 else V((1, 0, 0))
        a = d.cross(up).normalized()
        b = d.cross(a).normalized()
        verts = [base + (a * math.cos(2 * math.pi * k / sides) + b * math.sin(2 * math.pi * k / sides)) * radius for k in range(sides)]
        verts.append(tip)
        faces = [[k, (k + 1) % sides, sides] for k in range(sides)]
        faces.append(list(reversed(range(sides))))
        self.add(verts, faces, bone, color)

    def horn(self, points, r0, r1, bone, color):
        """A curved horn through points, tapering from r0 to a point."""
        for i in range(len(points) - 1):
            ra = r0 + (r1 - r0) * i / (len(points) - 1)
            rb = r0 + (r1 - r0) * (i + 1) / (len(points) - 1)
            if i == len(points) - 2:
                self.spike(points[i], points[i + 1], ra, bone, color)
            else:
                self.limb(points[i], points[i + 1], ra, rb, bone, color, 6)

    def lames(self, centre, rx, ry, rz, count, bone, color, edge, drop=0.035, shrink=0.12):
        """A layered pauldron: domes stepping down and out from the shoulder, each with a lighter rim."""
        for i in range(count):
            c = centre + V((0.025 * i, -drop * i, 0))
            s = 1.0 - shrink * i
            self.dome(c, rx * s, ry * s, rz * s, bone, color, 12)
            self.band(c.y - 0.006, c.y + 0.006, rx * s + 0.004, rz * s + 0.004, rx * s + 0.004, rz * s + 0.004, 0.012, bone, edge, cx=c.x, cz=c.z, segs=14)

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


def arm_keep(b, share):
    """Her upper arms from the shoulder out a share of the way to the elbow."""
    def keep(p, bone):
        for side in ("Left", "Right"):
            if bone == side + "Arm":
                return True if share >= 1 else abs(p.x) < abs(b.h(side + "Arm").x) + (abs(b.h(side + "ForeArm").x) - abs(b.h(side + "Arm").x)) * share
        return False
    return keep


def any_of(*keeps):
    return lambda p, bone: any(k(p, bone) for k in keeps)


def feet_keep(b, up):
    def keep(p, bone):
        if bone is None:
            return False
        side = "Left" if bone.startswith("Left") else "Right" if bone.startswith("Right") else None
        if side is None:
            return False
        if bone in (side + "Foot", side + "ToeBase", side + "Toe_End"):
            return True
        if bone == side + "Leg" and up > 0:
            ankle, knee = b.h(side + "Foot"), b.h(side + "Leg")
            return p.y < ankle.y + (knee.y - ankle.y) * up
        return False
    return keep


def hands_keep(b, up):
    def keep(p, bone):
        if bone is None:
            return False
        for side in ("Left", "Right"):
            if bone.startswith(side + "Hand"):
                return True
            if bone == side + "ForeArm" and up > 0:
                wrist, elbow = b.h(side + "Hand"), b.h(side + "ForeArm")
                return (p - wrist).length < (elbow - wrist).length * up
        return False
    return keep


SIDES = (("Left", 1), ("Right", -1))

# Her measures at rest (metres, armature space): hips 1.00, waist 1.15, chest 1.30 to 1.45, neck 1.46, head 1.53 to 1.75.
CHEST_RX, CHEST_RZ = 0.175, 0.14
WAIST_RX, WAIST_RZ = 0.15, 0.12
HIP_RX, HIP_RZ = 0.18, 0.14


# ---------------------------------------------------------------- chest armour (the body layer)

def stitched(base, line, rows=14):
    """A colour function: base, with darker seams every so often."""
    return lambda c, n: line if (c.y * rows) % 1 < 0.13 else base


def under(b, body, color, arms=0.5):
    """The cloth or leather under the armour: her torso, upper arms and the top of her thighs, wrapped."""
    keep = any_of(torso_keep(b, (), 0.08), arm_keep(b, arms), thigh_keep(b, 0.25))
    return [shell(b.arm, body, keep, 0.014, color, 2, "Under")]


def collar(b, color, rim=None, y=1.43, height=0.06, r=0.2):
    b.band(y, y + height, r, r * 0.85, r * 0.8, r * 0.7, 0.03, "Spine2", color, cz=-0.01)
    if rim:
        b.band(y + height - 0.008, y + height + 0.004, r * 0.82, r * 0.72, r * 0.8, r * 0.7, 0.012, "Spine2", rim, cz=-0.01)


def pauldrons(b, color, edge, count=2, size=1.0):
    for side, sx in SIDES:
        c = b.h(side + "Arm") + V((sx * 0.03, 0.035, -0.005))
        for i in range(count):
            cc = c + V((sx * 0.028 * i, -0.032 * i, 0))
            s = size * (1 - 0.12 * i)
            b.dome(cc, 0.105 * s, 0.075 * s, 0.11 * s, side + "Arm", color, 12)
            b.band(cc.y - 0.006, cc.y + 0.006, 0.108 * s, 0.113 * s, 0.108 * s, 0.113 * s, 0.014, side + "Arm", edge, cx=cc.x, cz=cc.z, segs=14)


def ranger(b, body):
    parts = under(b, body, stitched("leather_dark", "strap"))
    collar(b, "fur", None, 1.42, 0.07, 0.21)
    b.ball(V((0, 1.47, -0.02)), (0.2, 0.05, 0.16), "Spine2", "fur_hi", 12)
    for side, sx in SIDES:
        b.dome(b.h(side + "Arm") + V((sx * 0.02, 0.03, 0)), 0.09, 0.06, 0.095, side + "Arm", "leather", 10)
    # A strap across the chest, buckled at the heart.
    b.limb(V((0.13, 1.44, 0.12)), V((-0.12, 1.12, 0.13)), 0.018, 0.018, "Spine1", "strap", 6)
    b.box(V((0.03, 1.32, 0.15)), (0.035, 0.035, 0.012), "Spine1", "gold")
    return parts


def brigand(b, body):
    def studs(c, n):
        if abs(c.y - b.h("Hips").y - 0.03) < 0.02:
            return "strap"
        if (int(c.y * 50) % 3 == 0) and (int((c.x + 1) * 50) % 3 == 0):
            return "gold_dark"
        return "red" if (c.y * 10) % 1 > 0.1 else "red_hi"
    parts = under(b, body, studs, 0.6)
    pauldrons(b, "leather", "leather_hi", 2, 0.95)
    collar(b, "leather_dark", "leather_hi", 1.43, 0.05, 0.19)
    for side, sx in SIDES:
        hip = b.h(side + "UpLeg")
        b.box(V((hip.x + sx * 0.02, hip.y - 0.1, 0.06)), (0.11, 0.17, 0.02), side + "UpLeg", "leather_dark")
        b.box(V((hip.x + sx * 0.07, hip.y - 0.1, -0.02)), (0.02, 0.16, 0.12), side + "UpLeg", "leather_dark")
    return parts


def scale(b, body):
    def scales(c, n):
        row = int(c.y * 45)
        col = int((c.x + 1.0) * 45 + (row % 2) * 0.5)
        if (c.y * 45) % 1 < 0.22:
            return "iron_dark"
        return "iron_hi" if (row + col) % 3 == 0 else "iron"
    parts = under(b, body, scales, 0.7)
    parts += [shell(b.arm, body, thigh_keep(b, 0.45), 0.02, scales, 2, "ScaleSkirt")]
    pauldrons(b, "iron", "iron_edge", 2, 1.0)
    collar(b, "leather_dark", "iron_edge", 1.43, 0.05, 0.19)
    return parts


def breastplate(b, metal, edge, accent=None):
    # Breast and back plates over the chest, a second lame over the belly, both with lighter rims; a ridge down the
    # middle of the breast catches the light.
    b.band(1.27, 1.47, CHEST_RX + 0.02, CHEST_RZ + 0.03, CHEST_RX - 0.005, CHEST_RZ, 0.02, "Spine2", metal, 15, 165, segs=20)
    b.band(1.27, 1.45, CHEST_RX + 0.02, CHEST_RZ + 0.01, CHEST_RX, CHEST_RZ - 0.01, 0.02, "Spine2", metal, 195, 345, segs=20)
    b.band(1.13, 1.28, WAIST_RX + 0.03, WAIST_RZ + 0.04, CHEST_RX + 0.02, CHEST_RZ + 0.03, 0.02, "Spine1", metal, 20, 160, segs=18)
    b.band(1.455, 1.47, CHEST_RX + 0.0, CHEST_RZ + 0.005, CHEST_RX - 0.01, CHEST_RZ, 0.014, "Spine2", edge, 15, 165, segs=20)
    b.band(1.27, 1.285, CHEST_RX + 0.025, CHEST_RZ + 0.035, CHEST_RX + 0.025, CHEST_RZ + 0.035, 0.014, "Spine2", edge, 15, 165, segs=20)
    b.box(V((0, 1.36, CHEST_RZ + 0.03)), (0.02, 0.18, 0.02), "Spine2", accent or edge)


def faulds(b, metal, edge):
    hips = b.h("Hips")
    for i in range(2):
        y = hips.y + 0.06 - i * 0.055
        b.band(y - 0.05, y, HIP_RX + 0.03 + i * 0.012, HIP_RZ + 0.03 + i * 0.012, HIP_RX + 0.02 + i * 0.012, HIP_RZ + 0.02 + i * 0.012, 0.018, "Hips", metal, 10, 170, segs=18)
        b.band(y - 0.05, y - 0.04, HIP_RX + 0.035 + i * 0.012, HIP_RZ + 0.035 + i * 0.012, HIP_RX + 0.035 + i * 0.012, HIP_RZ + 0.035 + i * 0.012, 0.012, "Hips", edge, 10, 170, segs=18)
    for side, sx in SIDES:
        hip = b.h(side + "UpLeg")
        for i in range(2):
            b.box(V((hip.x + sx * 0.03, hip.y - 0.08 - i * 0.075, 0.07 - i * 0.005)), (0.13 - i * 0.01, 0.08, 0.018), side + "UpLeg", metal)
            b.box(V((hip.x + sx * 0.03, hip.y - 0.118 - i * 0.075, 0.08 - i * 0.005)), (0.13 - i * 0.01, 0.01, 0.012), side + "UpLeg", edge)


def plate(b, body):
    parts = under(b, body, stitched("red", "red_hi", 10), 0.7)
    breastplate(b, "iron", "iron_edge")
    pauldrons(b, "iron", "iron_edge", 3, 1.12)
    faulds(b, "iron", "iron_edge")
    collar(b, "iron", "iron_edge", 1.44, 0.05, 0.18)
    # A red tabard hanging in front, gold-trimmed.
    hips = b.h("Hips")
    b.box(V((0, hips.y - 0.09, HIP_RZ + 0.05)), (0.13, 0.26, 0.012), "Hips", "red")
    b.box(V((0, hips.y - 0.225, HIP_RZ + 0.05)), (0.135, 0.015, 0.014), "Hips", "gold")
    return parts


def knight(b, body):
    parts = under(b, body, stitched("black", "iron_dark", 10), 0.7)
    breastplate(b, "iron_dark", "gold", "gold")
    pauldrons(b, "iron_dark", "gold", 3, 1.25)
    for side, sx in SIDES:
        top = b.h(side + "Arm") + V((sx * 0.04, 0.11, -0.01))
        b.spike(top, top + V((sx * 0.06, 0.12, -0.02)), 0.026, side + "Arm", "iron_edge")
        b.spike(top + V((sx * 0.06, -0.02, 0.05)), top + V((sx * 0.11, 0.07, 0.06)), 0.02, side + "Arm", "iron_edge")
    faulds(b, "iron_dark", "gold")
    collar(b, "iron_dark", "gold", 1.44, 0.07, 0.19)
    # A tattered red cape from the shoulders to the knees, in two hinged panels.
    b.box(V((0, 1.3, -0.2)), (0.38, 0.32, 0.015), "Spine2", "red")
    b.box(V((0, 0.93, -0.22)), (0.36, 0.44, 0.015), "Hips", "red")
    for k in range(5):
        b.box(V((-0.16 + k * 0.08, 0.69, -0.22)), (0.05, 0.06 + 0.04 * (k % 2), 0.015), "Hips", "red_hi")
    b.box(V((0, 1.45, -0.19)), (0.4, 0.03, 0.03), "Spine2", "gold")
    return parts


def stag_hide(b, body):
    def dapple(c, n):
        return "fur_hi" if (math.sin(c.x * 60) + math.sin(c.y * 47) > 1.1) else "fur"
    parts = under(b, body, dapple, 0.8)
    collar(b, "fur_dark", None, 1.41, 0.08, 0.22)
    b.ball(V((0, 1.475, -0.03)), (0.22, 0.06, 0.18), "Spine2", "fur", 12)
    for side, sx in SIDES:
        base = b.h(side + "Arm") + V((sx * 0.03, 0.06, -0.02))
        b.horn([base, base + V((sx * 0.03, 0.1, -0.02)), base + V((sx * 0.07, 0.2, -0.04)), base + V((sx * 0.06, 0.3, -0.03))], 0.022, 0.004, side + "Arm", "bone_hi")
        b.horn([base + V((sx * 0.03, 0.1, -0.02)), base + V((sx * 0.1, 0.16, 0.03)), base + V((sx * 0.14, 0.2, 0.06))], 0.015, 0.004, side + "Arm", "bone")
        b.horn([base + V((sx * 0.07, 0.2, -0.04)), base + V((sx * 0.14, 0.25, -0.08))], 0.012, 0.004, side + "Arm", "bone")
    b.limb(V((0.13, 1.44, 0.12)), V((-0.12, 1.12, 0.13)), 0.02, 0.02, "Spine1", "strap", 6)
    return parts


def cinder_jerkin(b, body):
    def seams(c, n):
        if abs(math.sin(c.x * 38 + c.y * 22)) < 0.12 or abs(math.sin(c.y * 31 - c.x * 17)) < 0.08:
            return "ember"
        return "black" if (c.y * 12) % 1 > 0.15 else "leather_dark"
    parts = under(b, body, seams, 0.7)
    for side, sx in SIDES:
        c = b.h(side + "Arm") + V((sx * 0.03, 0.035, 0))
        b.dome(c, 0.1, 0.07, 0.105, side + "Arm", "black", 12)
        b.band(c.y - 0.006, c.y + 0.006, 0.104, 0.109, 0.104, 0.109, 0.014, side + "Arm", "ember_mid", cx=c.x, cz=c.z, segs=14)
    collar(b, "black", "ember", 1.43, 0.06, 0.19)
    for k in range(4):
        b.box(V((0.05 - k * 0.035, 1.47 + 0.02 * (k % 2), -0.06)), (0.02, 0.05, 0.02), "Spine2", "ember_mid")
    return parts


# ---------------------------------------------------------------- helms (their own layer, cut by her body)

# The middle of a helm and its half width and depth, clear of her head everywhere (2026-10-07). Her head and hair reach
# 0.142 to the sides, 0.167 forward (the nose) and 0.134 back from the head's middle (0, 1.64, -0.012); the helms were
# 0.128 by 0.138 around that middle, so her face and hair poked through them, and the bake, which hides a helm wherever
# her body is in front of it, cut holes there (a hood open down one side, the barbute's face plate gone).
HC = V((0, 1.64, 0.008))
HRX, HRZ = 0.155, 0.165


def helmet_shell(b, color, rx=HRX, rz=HRZ, low=1.535, rim=None):
    """A closed helmet: a dome over the crown and walls down to below the jaw."""
    b.dome(V((HC.x, HC.y + 0.02, HC.z)), rx, 0.115, rz, "Head", color, 14)
    b.band(low, HC.y + 0.03, rx * 0.95, rz * 0.95, rx, rz, 0.02, "Head", color, cz=HC.z, segs=18)
    if rim:
        b.band(low - 0.008, low + 0.012, rx * 0.97, rz * 0.97, rx * 0.97, rz * 0.97, 0.02, "Head", rim, cz=HC.z, segs=18)


def face_slit(b, y, color, width=0.12, cross=True):
    front = HC.z + HRZ + 0.004
    b.box(V((0, y, front)), (width, 0.018, 0.02), "Head", color)
    if cross:
        b.box(V((0, y - 0.05, front)), (0.02, 0.08, 0.02), "Head", color)


def hood_shape(b, color, dark):
    """A cloth hood (2026-10-07: "it does not act like a hood"): a soft crown set a little back, its tip falling down
    behind the head, sides that widen as they drape to the shoulders, a lip over the brow and the face left open."""
    b.dome(V((HC.x, HC.y + 0.01, HC.z - 0.015)), HRX + 0.006, 0.13, HRZ + 0.01, "Head", color, 14)
    b.limb(V((0, HC.y + 0.08, HC.z - 0.1)), V((0, HC.y + 0.0, HC.z - 0.25)), 0.065, 0.01, "Head", color, 8)
    b.band(1.48, HC.y + 0.03, HRX + 0.035, HRZ + 0.03, HRX + 0.008, HRZ + 0.012, 0.02, "Head", color, 135, 405, cz=HC.z, segs=18)
    b.band(HC.y + 0.035, HC.y + 0.06, HRX + 0.02, HRZ + 0.022, HRX + 0.012, HRZ + 0.018, 0.02, "Head", dark, 45, 135, cz=HC.z, segs=10)
    b.band(1.4, 1.48, 0.22, 0.19, 0.17, 0.15, 0.025, "Spine2", dark, cz=-0.01, segs=18)


def hood(b, body):
    hood_shape(b, "green", "green_hi")
    return []


def mask(b, body):
    hood_shape(b, "cloth_dark", "cloth")
    # A cloth mask over the mouth and nose: only her eyes show.
    b.band(1.54, 1.625, HRX + 0.01, HRZ + 0.015, HRX + 0.01, HRZ + 0.015, 0.02, "Head", "black", 35, 145, cz=HC.z, segs=14)
    b.band(1.61, 1.63, HRX + 0.012, HRZ + 0.017, HRX + 0.012, HRZ + 0.017, 0.012, "Head", "red", 35, 145, cz=HC.z, segs=14)
    return []


def barbute(b, body):
    helmet_shell(b, "iron", rim="iron_edge")
    b.box(V((0, HC.y + 0.025, HC.z + HRZ + 0.004)), (0.13, 0.02, 0.02), "Head", "slit")
    b.box(V((0, HC.y - 0.035, HC.z + HRZ + 0.004)), (0.03, 0.1, 0.02), "Head", "slit")
    b.box(V((0, HC.y + 0.13, HC.z)), (0.025, 0.04, 2 * HRZ - 0.02), "Head", "iron_edge")
    return []


def visored(b, body):
    # A great helm: flat-topped walls, an eye slit, breaths, a cross of reinforcement and a red crest.
    b.band(1.525, 1.755, HRX + 0.008, HRZ + 0.008, HRX, HRZ, 0.02, "Head", "iron", cz=HC.z, segs=18)
    b.dome(V((HC.x, 1.75, HC.z)), HRX, 0.045, HRZ, "Head", "iron_hi", 14)
    front = HC.z + HRZ + 0.01
    b.box(V((0, 1.665, front)), (0.18, 0.018, 0.02), "Head", "slit")
    b.box(V((0, 1.6, front)), (0.024, 0.15, 0.022), "Head", "iron_edge")
    for k in range(4):
        b.box(V((0.045, 1.575 + k * 0.02, front)), (0.028, 0.007, 0.018), "Head", "slit")
        b.box(V((-0.045, 1.575 + k * 0.02, front)), (0.028, 0.007, 0.018), "Head", "slit")
    b.band(1.52, 1.54, HRX + 0.012, HRZ + 0.012, HRX + 0.012, HRZ + 0.012, 0.02, "Head", "iron_edge", cz=HC.z, segs=18)
    for k in range(5):
        b.box(V((0, 1.79 + 0.01 * math.sin(k), HC.z + 0.1 - k * 0.05)), (0.022, 0.07, 0.05), "Head", "red" if k % 2 else "red_hi")
    return []


def horned(b, body):
    helmet_shell(b, "iron_dark", rim="gold")
    face_slit(b, HC.y + 0.025, "eye", 0.13, True)
    for side, sx in SIDES:
        base = V((sx * (HRX - 0.01), HC.y + 0.07, HC.z))
        pts = [base, base + V((sx * 0.08, 0.04, 0.02)), base + V((sx * 0.14, 0.12, 0.05)), base + V((sx * 0.15, 0.22, 0.09)), base + V((sx * 0.12, 0.3, 0.13))]
        b.horn(pts, 0.035, 0.005, "Head", "bone")
        b.band(base.y - 0.03, base.y + 0.03, 0.04, 0.04, 0.04, 0.04, 0.012, "Head", "gold", cx=base.x, cz=base.z, segs=8)
    b.box(V((0, HC.y + 0.13, HC.z)), (0.025, 0.04, 2 * HRZ - 0.02), "Head", "gold")
    return []


def falconer(b, body):
    hood_shape(b, "leather", "fur")
    # A beaked half-mask of gilded leather and a crest of falcon feathers.
    b.band(1.6, 1.69, HRX + 0.008, HRZ + 0.013, HRX + 0.008, HRZ + 0.013, 0.018, "Head", "gold_dark", 40, 140, cz=HC.z, segs=14)
    b.spike(V((0, 1.63, HC.z + HRZ + 0.005)), V((0, 1.58, HC.z + HRZ + 0.085)), 0.03, "Head", "gold")
    for k in range(7):
        a = math.radians(-60 + k * 20)
        root = V((math.sin(a) * 0.05, HC.y + 0.16, HC.z - 0.04 - math.cos(a) * 0.02))
        tip = root + V((math.sin(a) * 0.12, 0.17, -0.1))
        b.limb(root, tip, 0.022, 0.006, "Head", "feather" if k % 2 else "feather_dark", 4)
    return []


def unblinking_crown(b, body):
    helmet_shell(b, "black", rim="gold")
    # A blind mask with one great eye in the brow, under a crown of gold spikes.
    b.ball(V((0, HC.y + 0.05, HC.z + HRZ)), (0.038, 0.038, 0.026), "Head", "violet")
    b.ball(V((0, HC.y + 0.05, HC.z + HRZ + 0.018)), (0.018, 0.018, 0.012), "Head", "eye")
    b.band(HC.y + 0.09, HC.y + 0.125, HRX + 0.01, HRZ + 0.01, HRX + 0.01, HRZ + 0.01, 0.02, "Head", "gold", cz=HC.z, segs=18)
    for k in range(8):
        a = 2 * math.pi * k / 8
        base = V((math.cos(a) * (HRX + 0.005), HC.y + 0.125, HC.z + math.sin(a) * (HRZ + 0.005)))
        b.spike(base, base + V((math.cos(a) * 0.02, 0.1 + 0.04 * (k % 2), math.sin(a) * 0.02)), 0.025, "Head", "gold")
    return []


# ---------------------------------------------------------------- boots

def shin(b, side, low, high, r0, r1, color, front_only=False):
    knee, ankle = b.h(side + "Leg"), b.h(side + "Foot")
    p0 = ankle + (knee - ankle) * low
    p1 = ankle + (knee - ankle) * high
    b.limb(p0, p1, r0, r1, side + "Leg", color, 10)


def wrapped(b, body):
    parts = [shell(b.arm, body, feet_keep(b, 0.05), 0.01, lambda c, n: "cloth_dark", 2, "Shoes")]
    for side, _ in SIDES:
        for k in range(4):
            shin(b, side, 0.05 + k * 0.08, 0.1 + k * 0.08, 0.052, 0.054, "cloth" if k % 2 else "cloth_dark")
    return parts


def leather_boots(b, body):
    parts = [shell(b.arm, body, feet_keep(b, 0.05), 0.012, lambda c, n: "leather", 2, "Boots")]
    for side, _ in SIDES:
        shin(b, side, 0.02, 0.62, 0.056, 0.06, "leather")
        shin(b, side, 0.6, 0.72, 0.07, 0.074, "leather_hi")
    return parts


def strapped(b, body):
    parts = [shell(b.arm, body, feet_keep(b, 0.05), 0.012, lambda c, n: "leather_dark", 2, "Boots")]
    for side, sx in SIDES:
        shin(b, side, 0.02, 0.75, 0.056, 0.062, "leather_dark")
        shin(b, side, 0.72, 0.84, 0.072, 0.076, "leather")
        for k in range(3):
            shin(b, side, 0.15 + k * 0.2, 0.19 + k * 0.2, 0.064, 0.066, "strap")
            knee, ankle = b.h(side + "Leg"), b.h(side + "Foot")
            p = ankle + (knee - ankle) * (0.17 + k * 0.2)
            b.box(p + V((sx * 0.045, 0, 0.03)), (0.02, 0.025, 0.012), side + "Leg", "gold")
    return parts


def sabatons(b, body):
    parts = [shell(b.arm, body, feet_keep(b, 0.05), 0.016, lambda c, n: "iron_dark" if (c.z * 40) % 1 < 0.2 else "iron", 2, "Sabatons")]
    for side, sx in SIDES:
        shin(b, side, 0.02, 0.88, 0.06, 0.066, "iron")
        shin(b, side, 0.86, 0.9, 0.07, 0.07, "iron_edge")
        knee = b.h(side + "Leg")
        b.ball(knee + V((0, 0.0, 0.06)), (0.06, 0.06, 0.035), side + "Leg", "iron_hi")
        b.box(knee + V((0, -0.02, 0.095)), (0.03, 0.05, 0.015), side + "Leg", "iron_edge")
    return parts


def spiked(b, body):
    parts = [shell(b.arm, body, feet_keep(b, 0.05), 0.016, lambda c, n: "black", 2, "Greaves")]
    for side, sx in SIDES:
        shin(b, side, 0.02, 0.9, 0.062, 0.07, "iron_dark")
        shin(b, side, 0.3, 0.33, 0.07, 0.072, "gold")
        knee = b.h(side + "Leg")
        b.ball(knee + V((0, 0.0, 0.06)), (0.065, 0.065, 0.04), side + "Leg", "iron_dark")
        b.spike(knee + V((0, 0.0, 0.09)), knee + V((0, 0.03, 0.2)), 0.03, side + "Leg", "iron_edge")
        ankle = b.h(side + "Foot")
        b.spike(ankle + V((sx * 0.05, 0.03, -0.02)), ankle + V((sx * 0.12, 0.05, -0.06)), 0.018, side + "Leg", "iron_edge")
    return parts


def windrunner(b, body):
    parts = [shell(b.arm, body, feet_keep(b, 0.05), 0.012, lambda c, n: "green", 2, "Boots")]
    for side, sx in SIDES:
        shin(b, side, 0.02, 0.55, 0.055, 0.058, "green")
        shin(b, side, 0.53, 0.6, 0.064, 0.064, "gold")
        ankle = b.h(side + "Foot")
        for k in range(4):
            root = ankle + V((sx * 0.055, 0.04 + k * 0.025, -0.02))
            b.limb(root, root + V((sx * 0.07, 0.06 + k * 0.01, -0.09 - k * 0.015)), 0.016, 0.004, side + "Leg", "feather" if k % 2 else "feather_dark", 4)
    return parts


def stalker(b, body):
    parts = [shell(b.arm, body, feet_keep(b, 0.05), 0.014, lambda c, n: "black", 2, "Boots")]
    for side, sx in SIDES:
        shin(b, side, 0.02, 0.6, 0.06, 0.062, "black")
        for k in range(3):
            shin(b, side, 0.55 + k * 0.08, 0.63 + k * 0.08, 0.08 - k * 0.005, 0.078 - k * 0.005, "fur_dark" if k % 2 else "fur")
        toe = b.h(side + "ToeBase")
        for k in (-1, 0, 1):
            b.spike(toe + V((k * 0.025, 0.01, 0.02)), toe + V((k * 0.035, -0.005, 0.08)), 0.012, side + "Foot", "bone_hi")
    return parts


# ---------------------------------------------------------------- belts

def girdle(b, color, low=0.0, high=0.05, extra=0.0, rim=None):
    y = b.h("Hips").y
    b.band(y + low, y + high, HIP_RX + 0.02 + extra, HIP_RZ + 0.025 + extra, WAIST_RX + 0.035 + extra, WAIST_RZ + 0.04 + extra, 0.025, "Hips", color, cz=0.01, segs=20)
    if rim:
        b.band(y + high - 0.008, y + high + 0.004, WAIST_RX + 0.04 + extra, WAIST_RZ + 0.045 + extra, WAIST_RX + 0.04 + extra, WAIST_RZ + 0.045 + extra, 0.012, "Hips", rim, cz=0.01, segs=20)


def front(extra=0.0):
    return HIP_RZ + 0.05 + extra


def rope(b, body):
    girdle(b, "cloth", 0.01, 0.035)
    y = b.h("Hips").y
    b.ball(V((0.06, y + 0.02, front())), (0.025, 0.022, 0.02), "Hips", "cloth_dark")
    b.limb(V((0.06, y + 0.01, front())), V((0.08, y - 0.12, front() + 0.01)), 0.008, 0.006, "Hips", "cloth", 5)
    return []


def pouch_belt(b, body):
    girdle(b, "leather", 0.0, 0.045)
    y = b.h("Hips").y
    b.box(V((0, y + 0.022, front())), (0.05, 0.05, 0.015), "Hips", "gold")
    b.box(V((-0.13, y - 0.03, 0.09)), (0.07, 0.08, 0.05), "Hips", "leather_dark")
    b.box(V((0.14, y - 0.025, 0.06)), (0.06, 0.07, 0.045), "Hips", "leather_hi")
    b.limb(V((0.17, y, -0.05)), V((0.19, y - 0.18, -0.07)), 0.014, 0.008, "Hips", "iron_edge", 4)
    return []


def studded(b, body):
    girdle(b, "leather_dark", -0.005, 0.055, rim="gold_dark")
    y = b.h("Hips").y
    for k in range(9):
        a = math.radians(20 + k * 17.5)
        b.ball(V((math.cos(a) * (WAIST_RX + 0.06), y + 0.025, 0.01 + math.sin(a) * (WAIST_RZ + 0.065))), (0.01, 0.01, 0.01), "Hips", "gold")
    for side, sx in SIDES:
        hip = b.h(side + "UpLeg")
        b.box(V((hip.x + sx * 0.03, hip.y - 0.07, 0.08)), (0.1, 0.14, 0.015), side + "UpLeg", "leather")
    b.box(V((0, y - 0.07, front())), (0.09, 0.16, 0.012), "Hips", "leather_dark")
    return []


def tassets(b, body):
    girdle(b, "iron", -0.01, 0.06, rim="iron_edge")
    y = b.h("Hips").y
    b.box(V((0, y + 0.025, front(0.01))), (0.06, 0.06, 0.02), "Hips", "iron_edge")
    for side, sx in SIDES:
        hip = b.h(side + "UpLeg")
        for i in range(3):
            b.box(V((hip.x + sx * 0.035, hip.y - 0.06 - i * 0.06, 0.075)), (0.12 - i * 0.008, 0.065, 0.016), side + "UpLeg", "iron")
            b.box(V((hip.x + sx * 0.035, hip.y - 0.092 - i * 0.06, 0.084)), (0.12 - i * 0.008, 0.008, 0.012), side + "UpLeg", "iron_edge")
    b.box(V((0, y - 0.11, front())), (0.11, 0.24, 0.012), "Hips", "red")
    return []


def war_girdle(b, body):
    girdle(b, "black", -0.02, 0.075, 0.01, rim="gold")
    y = b.h("Hips").y
    # A skull for a buckle, and chains hanging from it.
    b.ball(V((0, y + 0.03, front(0.02))), (0.045, 0.05, 0.035), "Hips", "bone_hi")
    for sx in (-1, 1):
        b.box(V((sx * 0.017, y + 0.04, front(0.05))), (0.016, 0.014, 0.01), "Hips", "slit")
    for k in range(5):
        b.box(V((-0.07 + k * 0.035, y - 0.03 - (k % 2) * 0.02, front(0.01))), (0.012, 0.07, 0.012), "Hips", "iron_edge")
    for side, sx in SIDES:
        hip = b.h(side + "UpLeg")
        for i in range(2):
            b.box(V((hip.x + sx * 0.035, hip.y - 0.07 - i * 0.07, 0.08)), (0.13, 0.075, 0.018), side + "UpLeg", "iron_dark")
            b.box(V((hip.x + sx * 0.035, hip.y - 0.105 - i * 0.07, 0.09)), (0.13, 0.008, 0.012), side + "UpLeg", "gold")
    return []


def bandolier(b, body):
    girdle(b, "leather_dark", 0.0, 0.045)
    # A strap across the chest, hung with shrunken heads.
    b.limb(V((0.15, 1.45, 0.11)), V((-0.14, 1.07, 0.14)), 0.022, 0.022, "Spine1", "leather", 6)
    b.limb(V((0.15, 1.45, -0.11)), V((-0.14, 1.07, -0.12)), 0.022, 0.022, "Spine1", "leather", 6)
    y = b.h("Hips").y
    for k, x in enumerate((-0.11, -0.04, 0.03, 0.1)):
        c = V((x, y - 0.06 - 0.02 * (k % 2), front(0.01)))
        b.ball(c, (0.03, 0.036, 0.028), "Hips", "fur" if k % 2 else "leather_hi")
        for sx in (-1, 1):
            b.box(c + V((sx * 0.011, 0.006, 0.026)), (0.01, 0.008, 0.006), "Hips", "slit")
        b.ball(c + V((0, 0.035, 0)), (0.032, 0.012, 0.03), "Hips", "black")
    return []


# ---------------------------------------------------------------- gloves

def bracer(b, side, low, high, r0, r1, color):
    elbow, wrist = b.h(side + "ForeArm"), b.h(side + "Hand")
    b.limb(wrist + (elbow - wrist) * low, wrist + (elbow - wrist) * high, r0, r1, side + "ForeArm", color, 10)


def hands(b, body, color, up=0.25):
    return [shell(b.arm, body, hands_keep(b, up), 0.008, color, 1, "Hands")]


def wraps(b, body):
    parts = hands(b, body, lambda c, n: "cloth" if (c.x * 40) % 1 < 0.5 else "cloth_dark")
    for side, _ in SIDES:
        for k in range(3):
            bracer(b, side, 0.05 + k * 0.1, 0.11 + k * 0.1, 0.042, 0.044, "cloth" if k % 2 else "cloth_dark")
    return parts


def gloves(b, body):
    parts = hands(b, body, lambda c, n: "leather")
    for side, _ in SIDES:
        bracer(b, side, 0.02, 0.22, 0.05, 0.062, "leather_hi")
    return parts


def bracers(b, body):
    parts = hands(b, body, lambda c, n: "leather_dark")
    for side, sx in SIDES:
        bracer(b, side, 0.05, 0.75, 0.05, 0.056, "leather")
        for k in range(3):
            bracer(b, side, 0.15 + k * 0.22, 0.19 + k * 0.22, 0.058, 0.06, "strap")
        elbow = b.h(side + "ForeArm")
        b.ball(elbow + V((0, 0.0, -0.04)), (0.045, 0.045, 0.035), side + "ForeArm", "leather_dark")
    return parts


def gauntlets(b, body):
    parts = hands(b, body, lambda c, n: "iron_edge" if (abs(c.x) * 60) % 1 < 0.2 else "iron", 0.1)
    for side, _ in SIDES:
        bracer(b, side, 0.0, 0.2, 0.055, 0.08, "iron_hi")
        bracer(b, side, 0.18, 0.72, 0.052, 0.058, "iron")
        bracer(b, side, 0.7, 0.75, 0.062, 0.062, "iron_edge")
    return parts


def claws(b, body):
    parts = hands(b, body, lambda c, n: "black", 0.1)
    for side, sx in SIDES:
        bracer(b, side, 0.0, 0.2, 0.055, 0.085, "iron_dark")
        bracer(b, side, 0.18, 0.75, 0.054, 0.06, "iron_dark")
        bracer(b, side, 0.2, 0.22, 0.062, 0.062, "gold")
        hand = b.h(side + "Hand")
        tip = b.t(side + "Hand")
        d = (tip - hand).normalized()
        for k in (-1, 0, 1):
            base = tip + V((0, 0.01, k * 0.022))
            b.spike(base, base + d * 0.1 + V((0, -0.02, k * 0.01)), 0.009, side + "Hand", "iron_edge")
        elbow = b.h(side + "ForeArm")
        b.spike(elbow + V((0, 0.01, -0.03)), elbow + V((-sx * 0.04, 0.03, -0.11)), 0.022, side + "ForeArm", "iron_edge")
    return parts


def fletcher(b, body):
    parts = hands(b, body, lambda c, n: "leather")
    for side, sx in SIDES:
        bracer(b, side, 0.05, 0.7, 0.05, 0.055, "green")
        bracer(b, side, 0.05, 0.1, 0.057, 0.057, "gold")
        bracer(b, side, 0.66, 0.7, 0.059, 0.059, "gold")
        elbow, wrist = b.h(side + "ForeArm"), b.h(side + "Hand")
        for k in range(3):
            root = wrist + (elbow - wrist) * (0.3 + k * 0.12) + V((0, 0.05, 0))
            b.limb(root, root + V((-sx * 0.06, 0.07, -0.02)), 0.014, 0.004, side + "ForeArm", "feather" if k % 2 else "red_hi", 4)
    return parts


def bloodletter(b, body):
    parts = hands(b, body, lambda c, n: "red", 0.1)
    for side, sx in SIDES:
        bracer(b, side, 0.0, 0.18, 0.054, 0.075, "red_hi")
        bracer(b, side, 0.16, 0.72, 0.052, 0.058, "black")
        elbow, wrist = b.h(side + "ForeArm"), b.h(side + "Hand")
        for k in range(3):
            p0 = wrist + (elbow - wrist) * (0.2 + k * 0.2) + V((0, 0.055, 0))
            b.spike(p0, p0 + (elbow - wrist).normalized() * 0.07 + V((0, 0.05, 0)), 0.014, side + "ForeArm", "bone_hi")
    return parts


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


# The looks, by tier (AppearanceRules: five bands of item level) and the named legendaries' own (2026-10-05).
BODIES = {"bare": None, "ranger": ranger, "brigand": brigand, "scale": scale, "plate": plate, "knight": knight,
          "stag_hide": stag_hide, "cinder_jerkin": cinder_jerkin}
WORN = {
    "helm": {"hood": hood, "mask": mask, "barbute": barbute, "visored": visored, "horned": horned,
             "falconer": falconer, "unblinking_crown": unblinking_crown},
    "boots": {"wrapped": wrapped, "leather_boots": leather_boots, "strapped": strapped, "sabatons": sabatons,
              "spiked": spiked, "windrunner": windrunner, "stalker": stalker},
    "belt": {"rope": rope, "pouch_belt": pouch_belt, "studded": studded, "tassets": tassets, "war_girdle": war_girdle,
             "bandolier": bandolier},
    "gloves": {"wraps": wraps, "gloves": gloves, "bracers": bracers, "gauntlets": gauntlets, "claws": claws,
               "fletcher": fletcher, "bloodletter": bloodletter},
}

def main():
    wanted = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []

    # One texture for every export (WildArrowBakeSetup puts it on each).
    texture(os.path.join(FOLDER, "wild_arrow_gear_albedo.png"))

    for look, build in BODIES.items():
        if wanted and look not in wanted:
            continue
        arm, body = load()
        squeeze_her_uvs(body)
        parts = [body]
        if build is not None:
            b = Builder(arm)
            parts += build(b, body)
            if b.colors:
                armour = b.mesh("Armour")
                armour.data.materials.append(body.data.materials[0])
                parts.append(armour)
        export(arm, parts, os.path.join(FOLDER, "wild_arrow_body_%s.fbx" % look))

    for code, looks in WORN.items():
        for look, build in looks.items():
            if wanted and look not in wanted:
                continue
            arm, body = load()
            b = Builder(arm)
            parts = build(b, body)
            if b.colors:
                piece = b.mesh("Gear")
                piece.data.materials.append(body.data.materials[0])
                parts.append(piece)
            bpy.data.objects.remove(body, do_unlink=True)
            export(arm, parts, os.path.join(FOLDER, "wild_arrow_%s_%s.fbx" % (code, look)))


if __name__ == "__main__":
    main()
