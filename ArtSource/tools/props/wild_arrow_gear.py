# The Wild Arrow's worn gear (the owner, 2026-10-04: "making models for other gear that show up on the character when
# equipped. Like chest armor and helmets"), built in Blender from code on her own rig and exported as bodies for the
# sprite bake (WildArrowBakeSetup): wild_arrow_body_<look>.fbx, her mesh (her own body since 2026-10-13, wild_arrow_body.py) with the chest armour bound to her bones
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
    ("gold", (214, 164, 52)), ("gold_dark", (150, 106, 30)), ("bronze", WOOD[4]),
    ("fur", EARTH[3]), ("fur_hi", EARTH[5]), ("fur_dark", EARTH[1]),
    ("bone", BONE[0]), ("bone_hi", BONE[1]), ("ember", EMBER[4]), ("ember_mid", EMBER[3]),
    ("violet", VIOLET[1]), ("violet_dark", VIOLET[0]), ("eye", BLOOD[4]),
    ("feather", BONE[2]), ("feather_dark", EARTH[2]), ("cloth", EARTH[2]), ("cloth_dark", EARTH[1]), ("slit", (14, 10, 10)),
    # Her own body (2026-10-13, wild_arrow_body.py, in place of the Meshy model): skin, auburn hair, a green tunic.
    ("skin", SKIN[3]), ("skin_shade", SKIN[2]), ("lips", (150, 80, 70)), ("hair", (88, 40, 24)), ("hair_hi", (122, 60, 34)),
    ("eye_dark", (24, 18, 16)), ("tunic", (46, 70, 44)), ("tunic_dark", (32, 48, 32)), ("trousers", (84, 76, 70)),
    ("boot", (40, 26, 18)),
    # The gear after the owner's sheets (2026-11-02, ArtSource/pixel/ai_exchange/gear_sheets): each tier its own colours
    # in the strip, so no palette swap is needed for them.
    ("cloak", (66, 58, 50)), ("cloak_dark", (40, 35, 30)),
    ("coat", (96, 58, 34)), ("coat_hi", (128, 80, 46)), ("coat_dark", (58, 34, 20)),
    ("blue", (62, 92, 128)), ("blue_hi", (112, 146, 182)), ("blue_dark", (34, 50, 72)),
    ("silver", (150, 154, 162)), ("silver_hi", (205, 208, 214)), ("silver_dark", (92, 96, 106)),
    ("crimson", (128, 24, 28)), ("crimson_hi", (166, 42, 40)), ("crimson_dark", (82, 14, 18)),
    ("black_steel", (36, 34, 40)), ("black_steel_hi", (74, 72, 82)),
    ("hide", (176, 160, 128)), ("hide_hi", (206, 192, 160)), ("eye_glow", (236, 120, 40)),
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

    def __init__(self, arm, body):
        self.arm = arm
        self.her = Her(arm, body)
        self.bm = bmesh.new()
        self.deform = self.bm.verts.layers.deform.verify()
        self.colors = []
        self.groups = {}
        # Faces whose winding is set by hand (the hood: an open surface with a lining, which the recalculation turned
        # inward, so the bake culled its near side and showed the inside).
        self.fixed = set()

    def h(self, name):
        return V(self.arm.data.bones["mixamorig:" + name].head_local)

    def t(self, name):
        return V(self.arm.data.bones["mixamorig:" + name].tail_local)

    def add(self, verts, faces, bone, color, fixed=False):
        """bone is a bone name, or a function of a corner giving [(bone, weight), ...] (the hood: cloth that bends
        from the head down to the shoulders instead of tearing at a seam)."""
        vs = []
        for co in verts:
            v = self.bm.verts.new(co)
            for name, weight in (bone(co) if callable(bone) else [(bone, 1.0)]):
                if weight > 0:
                    v[self.deform][self.groups.setdefault("mixamorig:" + name, len(self.groups))] = weight
            vs.append(v)
        for f in faces:
            try:
                face = self.bm.faces.new([vs[i] for i in f])
                self.colors.append(NAMES.index(color))
                if fixed:
                    self.fixed.add(face)
            except ValueError:
                pass

    def facing(self, pts, out, bone, color):
        """One face through pts, wound so it faces the direction out."""
        n = (pts[1] - pts[0]).cross(pts[2] - pts[0])
        if n.length < 1e-12 and len(pts) > 3:
            n = (pts[2] - pts[0]).cross(pts[3] - pts[0])
        order = list(range(len(pts))) if n.dot(out) >= 0 else list(reversed(range(len(pts))))
        self.add(pts, [order], bone, color, fixed=True)

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
        bmesh.ops.recalc_face_normals(self.bm, faces=[f for f in self.bm.faces if f not in self.fixed])
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
    # Her mesh is split along its texture's seams (2026-11-02, her new model): pushed out along their normals the two
    # sides parted and her own clothes showed through in lines. Joined first, the shell is one skin.
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=0.0005)
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


# ---------------------------------------------------------------- her measures
#
# Every piece is sized from her body as it is (2026-11-02, her new model, the owner's Hunyuan3D one rigged on Mixamo):
# the old pieces were placed at fixed heights measured on her old body (neck 1.46, head 1.53 to 1.75), and on the new one
# (neck 1.39, chin 1.45, crown 1.70, a bun and a ponytail behind) the ranger's mantle sat on her chin and the knight's
# plate covered her face.

class Her:
    """Her surface at rest, in the armature's space: the trunk's and the head's width, front and back by height, a limb's
    girth along its bone, and the heights that matter."""

    TRUNK = TORSO | {"Neck", "LeftUpLeg", "RightUpLeg"}

    def __init__(self, arm, body):
        self.arm = arm
        positions, _ = arm_space(body)
        self.P = np.array([p[:] for p in positions])
        self.B = np.array([str(x) for x in dominant_bones(body)])
        self.trunk_mask = np.isin(self.B, list(self.TRUNK))
        self.head_mask = np.isin(self.B, ["Head", "Neck"])
        self.hip = self.bone("Hips").y
        self.leg = self.bone("LeftUpLeg").y
        self.neck = self.bone("Neck").y
        face = self.P[(self.B == "Head") & (self.P[:, 2] > 0.6 * self.P[self.B == "Head"][:, 2].max())]
        self.chin = float(face[:, 1].min())
        self.top = float(self.P[:, 1].max())
        self.face_x = float(face[:, 0].mean())
        ys = np.arange(self.hip + 0.03, self.neck - 0.05, 0.01)
        self.waist = float(min(ys, key=lambda y: self.trunk(y)[0]))
        ys = np.arange(self.waist, self.neck - 0.05, 0.01)
        self.bust = float(max(ys, key=lambda y: self.trunk(y)[1]))

    def bone(self, name):
        return V(self.arm.data.bones["mixamorig:" + name].head_local)

    def tail(self, name):
        return V(self.arm.data.bones["mixamorig:" + name].tail_local)

    def _row(self, mask, y, half):
        sel = self.P[mask & (np.abs(self.P[:, 1] - y) < half)]
        if len(sel) == 0:
            return 0.05, 0.05, -0.05
        return float(np.abs(sel[:, 0]).max()), float(sel[:, 2].max()), float(sel[:, 2].min())

    def trunk(self, y, half=0.015):
        """Half width, front z and back z (negative) of her trunk (and thighs) at a height."""
        return self._row(self.trunk_mask, y, half)

    def head(self, y, half=0.03):
        """The same for her head with its hair (a window of 6 cm: the envelope a helmet must clear)."""
        return self._row(self.head_mask, y, half)

    def girth(self, bone, f):
        """Her limb's greatest distance from the bone's axis a share f of the way from the bone's head to its tail."""
        h, t = np.array(self.bone(bone)[:]), np.array(self.tail(bone)[:])
        d = t - h
        length = np.linalg.norm(d)
        d /= length
        sel = self.P[self.B == bone]
        s = (sel - h) @ d
        near = sel[np.abs(s - f * length) < 0.025]
        if len(near) == 0:
            return 0.05
        perp = (near - h) - np.outer((near - h) @ d, d)
        return float(np.linalg.norm(perp, axis=1).max())


def spine_weights(her, shoulders=0.0):
    """Weights for a piece on her trunk: blended along the spine by height, and toward a shoulder by shoulders (0 to 1)
    out at her sides above the chest, so a mantle lifts with her arms instead of cutting through them."""
    chain = [(n, her.bone(n).y) for n in ("Hips", "Spine", "Spine1", "Spine2")]

    def weights(co):
        y = co.y
        out = []
        if y <= chain[0][1]:
            out = [(chain[0][0], 1.0)]
        elif y >= chain[-1][1]:
            out = [(chain[-1][0], 1.0)]
        else:
            for (n0, y0), (n1, y1) in zip(chain, chain[1:]):
                if y0 <= y <= y1:
                    t = (y - y0) / (y1 - y0)
                    out = [(n0, 1 - t), (n1, t)]
                    break
        if shoulders > 0 and y > her.bust:
            s = shoulders * min(1.0, max(0.0, (abs(co.x) - 0.09) / 0.08))
            if s > 0:
                out = [(n, w * (1 - s)) for n, w in out] + [(("Left" if co.x > 0 else "Right") + "Shoulder", s)]
        return out
    return weights


def skirt_weights(her):
    """Weights for a piece hanging below her hips: the hips, and toward the thigh on its side the lower it hangs, so it
    swings with the leg."""
    def weights(co):
        t = min(1.0, max(0.0, (her.leg - co.y) / 0.25)) * min(1.0, abs(co.x) / 0.05) * 0.7
        side = "LeftUpLeg" if co.x > 0 else "RightUpLeg"
        return [("Hips", 1 - t), (side, t)]
    return weights


def head_weights(her, low, high):
    """The head above high, the neck below low, the chest further down, blended (a hood's cloth)."""
    def smooth(a, b, y):
        t = min(1.0, max(0.0, (y - a) / (b - a)))
        return t * t * (3 - 2 * t)

    def weights(co):
        head = smooth(low, high, co.y)
        neck = (1 - head) * smooth(her.neck - 0.03, low, co.y)
        return [("Head", head), ("Neck", neck), ("Spine2", 1 - head - neck)]
    return weights


# ---------------------------------------------------------------- shapes made from rings

def lathe(b, rings, weights, color, a0=0.0, a1=360.0, segs=28, cap=None, thick=0.0, origin=None):
    """A surface through rings (y, half width, front z, back z) from top to bottom, around x = 0: each ring an ellipse
    whose front half reaches the front z and back half the back z (negative). Over the angles a0 to a1 (90 her front,
    270 her back). cap closes the top in a point at that height. thick gives it an inside and rims (and side edges when
    partial), so it reads from every side. color is a name or a function of (middle, ring index, angle in degrees)."""
    origin = origin or V((0, 0, 0))
    full = (a1 - a0) >= 359.9
    n = segs if full else max(2, int(segs * (a1 - a0) / 360.0) + 1)
    angles = [a0 + (a1 - a0) * (i / n if full else i / (n - 1)) for i in range(n)]

    def ring(r, inset):
        y, rx, zf, zb = r[:4]
        pts = []
        for ang in angles:
            a = math.radians(ang)
            s = math.sin(a)
            z = (zf - inset) * s if s > 0 else -(zb + inset) * s
            pts.append(V(((rx - inset) * math.cos(a), y, z)) + origin)
        return pts

    def paint(quad, i, k):
        if callable(color):
            mid = sum(quad, V((0, 0, 0))) / len(quad)
            return color(mid, i, angles[k])
        return color

    outer = [ring(r, 0) for r in rings]
    inner = [ring(r, thick) for r in rings] if thick > 0 else None
    last = n if full else n - 1
    centre = lambda i: V((0, rings[i][0], (rings[i][2] + rings[i][3]) / 2)) + origin
    for i in range(len(rings) - 1):
        for k in range(last):
            j = (k + 1) % n
            quad = [outer[i][k], outer[i][j], outer[i + 1][j], outer[i + 1][k]]
            mid = sum(quad, V((0, 0, 0))) / 4
            b.facing(quad, mid - centre(i), weights, paint(quad, i, k))
            if inner:
                q = [inner[i][k], inner[i][j], inner[i + 1][j], inner[i + 1][k]]
                b.facing(q, centre(i) - mid, weights, paint(quad, i, k))
    if cap is not None:
        apex = V((0, cap, (rings[0][2] + rings[0][3]) / 2)) + origin
        for k in range(last):
            j = (k + 1) % n
            tri = [apex, outer[0][k], outer[0][j]]
            b.facing(tri, sum(tri, V((0, 0, 0))) / 3 - centre(0) + V((0, 0.02, 0)), weights, paint(tri, 0, k))
    if inner:
        ends = [len(rings) - 1] if cap is not None else [0, len(rings) - 1]
        for i in ends:
            up = V((0, 1 if i == 0 else -1, 0))
            for k in range(last):
                j = (k + 1) % n
                b.facing([outer[i][k], outer[i][j], inner[i][j], inner[i][k]], up, weights, paint([outer[i][k]], i, k))
        if not full:
            for k, side in ((0, -1), (n - 1, 1)):
                a = math.radians(angles[k])
                tangent = V((-math.sin(a), 0, math.cos(a))) * side
                for i in range(len(rings) - 1):
                    b.facing([outer[i][k], outer[i + 1][k], inner[i + 1][k], inner[i][k]], tangent, weights, paint([outer[i][k]], i, k))


def around(her, y0, y1, grow0, grow1=None, steps=4, at=None):
    """Rings hugging her trunk from y0 down to y1 (y0 above), grown outward by grow0 at the top to grow1 at the bottom."""
    grow1 = grow0 if grow1 is None else grow1
    at = at or her.trunk
    rings = []
    for i in range(steps + 1):
        t = i / steps
        y = y0 + (y1 - y0) * t
        g = grow0 + (grow1 - grow0) * t
        rx, zf, zb = at(y)
        rings.append((y, rx + g, zf + g, zb - g))
    return rings


def cap(b, centre, radii, bone, color, rot=None, phi0=0.0, phi1=90.0, segs=14, rings=4):
    """A section of an ellipsoid's upper half between the latitudes phi0 and phi1 (degrees, 90 the pole), turned by rot:
    a pauldron's dome (0 to 90) or its rim (0 to a little)."""
    rot = rot or mathutils.Matrix.Identity(3)
    rows = []
    for i in range(rings + 1):
        phi = math.radians(phi0 + (phi1 - phi0) * i / rings)
        row = []
        for k in range(segs):
            a = 2 * math.pi * k / segs
            p = V((math.cos(phi) * math.cos(a) * radii[0], math.sin(phi) * radii[1], math.cos(phi) * math.sin(a) * radii[2]))
            row.append(centre + rot @ p)
        rows.append(row)
    for i in range(rings):
        for k in range(segs):
            j = (k + 1) % segs
            quad = [rows[i][k], rows[i][j], rows[i + 1][j], rows[i + 1][k]]
            mid = sum(quad, V((0, 0, 0))) / 4
            b.facing(quad, mid - centre, bone, color)


def tilt(axis):
    """The turn taking straight up to axis."""
    return V((0, 1, 0)).rotation_difference(axis.normalized()).to_matrix()


# ---------------------------------------------------------------- shells

def trunk_keep(b, y0, y1, arms=0.0, thighs=0.0, forearms=0.0):
    """Her trunk between two heights, her upper arms a share of the way to the elbow, her thighs a share of the way to
    the knee, her forearms a share of the way from the elbow."""
    keeps = [lambda p, bone: bone in TORSO and y1 < p.y < y0]
    if arms > 0:
        keeps.append(arm_keep(b, arms))
    if thighs > 0:
        keeps.append(thigh_keep(b, thighs))
    if forearms > 0:
        def fore(p, bone):
            for side in ("Left", "Right"):
                if bone == side + "ForeArm":
                    elbow, wrist = b.h(side + "ForeArm"), b.h(side + "Hand")
                    return (p - elbow).length < (wrist - elbow).length * forearms
            return False
        keeps.append(fore)
    return any_of(*keeps)


def jerkin(b, body, color_of, y0, y1, offset=0.014, arms=0.0, thighs=0.0, forearms=0.0, smooth=2, name="Jerkin"):
    return [shell(b.arm, body, trunk_keep(b, y0, y1, arms, thighs, forearms), offset, color_of, smooth, name)]


def stitched(base, line, rows=14):
    """A colour function: base, with darker seams every so often."""
    return lambda c, n: line if (c.y * rows) % 1 < 0.13 else base


def studs(base, stud, every=0.045):
    """Leather with round studs in rows, offset every other row."""
    def color(c, n):
        row = int(c.y / every)
        u = (math.atan2(c.x, c.z + 0.02) / every * 0.16 + (row % 2) * 0.5) % 1
        return stud if (c.y / every) % 1 < 0.28 and u < 0.28 else base
    return color


def scales(base, light, dark, size=0.03):
    """Overlapping scales: a dark line at each row's lower edge, every few a lighter one."""
    def color(c, n):
        row = int(c.y / size)
        col = int((math.atan2(c.x, c.z + 0.02) * 0.12 / size + (row % 2) * 0.5))
        f = (c.y / size) % 1
        if f < 0.22:
            return dark
        return light if (row + col) % 3 == 0 and f > 0.6 else base
    return color


# ---------------------------------------------------------------- chest armour (the body layer)

def collar(b, her, color, rim=None, height=0.075, grow=0.03, flare=0.02, front_open=False):
    """A standing collar round her neck from the top of her shoulders."""
    y0 = her.neck + 0.005
    rx, zf, zb = her.trunk(her.neck + 0.03)
    rings = [(y0 + height, rx + grow + flare, zf + grow + flare, zb - grow - flare), (y0, rx + grow, zf + grow, zb - grow)]
    a0, a1 = (110, 430) if front_open else (0, 360)
    lathe(b, rings, "Spine2", color, a0, a1, 20, thick=0.012)
    if rim:
        lathe(b, [(y0 + height + 0.006, rx + grow + flare + 0.006, zf + grow + flare + 0.006, zb - grow - flare - 0.006),
                  (y0 + height - 0.012, rx + grow + flare + 0.004, zf + grow + flare + 0.004, zb - grow - flare - 0.004)],
              "Spine2", rim, a0, a1, 20, thick=0.012)


def pauldron(b, her, side, sx, metal, edge, lames=2, size=1.0, step=0.045):
    """Domes over her shoulder stepping down her upper arm, each with a lighter rim."""
    joint, elbow = her.bone(side + "Arm"), her.bone(side + "ForeArm")
    along = (elbow - joint).normalized()
    r = her.girth(side + "Arm", 0.1)
    axis = V((sx * 0.5, 1.0, 0.0))
    rot = tilt(axis)
    centre = joint + V((sx * 0.02, 0.015, -0.005))
    for i in range(lames):
        s = size * (1 - 0.1 * i)
        c = centre + along * step * i
        radii = ((r + 0.03) * s, (r * 0.85 + 0.012) * s, (r + 0.035) * s)
        cap(b, c, radii, side + "Arm", metal, rot)
        cap(b, c - rot @ V((0, 0.004, 0)), (radii[0] + 0.008, radii[1] + 0.008, radii[2] + 0.008), side + "Arm", edge, rot, 0, 22, 14, 1)
    return centre, rot, r


def belt(b, her, color, y=None, width=0.05, grow=0.02, rim=None, buckle=None):
    """A belt round her hips where hers sits."""
    y = her.hip + 0.01 if y is None else y
    lathe(b, around(her, y + width / 2, y - width / 2, grow, grow, 2), "Hips", color, segs=28, thick=0.012)
    if rim:
        for yy in (y + width / 2, y - width / 2 + 0.008):
            lathe(b, around(her, yy, yy - 0.008, grow + 0.005, grow + 0.005, 1), "Hips", rim, segs=28, thick=0.01)
    if buckle:
        rx, zf, zb = her.trunk(y)
        b.box(V((0, y, zf + grow + 0.008)), (0.055, width + 0.012, 0.014), "Hips", buckle)
        b.box(V((0, y, zf + grow + 0.014)), (0.03, width - 0.012, 0.01), "Hips", color)


def flap(b, her, x, top, bottom, width, color, hem=None, front=True, lean=0.02):
    """A cloth flap hanging from her belt in front (or behind), swinging with the legs."""
    rx, zf, zb = her.trunk(top)
    z = zf + 0.03 if front else zb - 0.03
    sz = 1 if front else -1
    pts = [V((x - width / 2, top, z)), V((x + width / 2, top, z)),
           V((x + width / 2 * 1.15, bottom, z + sz * lean)), V((x - width / 2 * 1.15, bottom, z + sz * lean))]
    inner = [p - V((0, 0, sz * 0.012)) for p in pts]
    w = skirt_weights(her)
    b.facing(pts, V((0, 0, sz)), w, color)
    b.facing(list(reversed(inner)), V((0, 0, -sz)), w, color)
    b.facing([pts[3], pts[2], inner[2], inner[3]], V((0, -1, 0)), w, hem or color)
    for i0, i1, out in ((0, 3, -1), (1, 2, 1)):
        b.facing([pts[i0], pts[i1], inner[i1], inner[i0]], V((out, 0, 0)), w, color)
    if hem:
        hz = z + sz * (lean + 0.004)
        b.facing([V((x - width / 2 * 1.15, bottom + 0.025, hz)), V((x + width / 2 * 1.15, bottom + 0.025, hz)),
                  V((x + width / 2 * 1.15, bottom, hz)), V((x - width / 2 * 1.15, bottom, hz))], V((0, 0, sz)), w, hem)


def tasset(b, her, side, sx, top, rows, metal, edge, width=0.11, drop=0.065):
    """Plates hanging over her thigh's front and outer side, each lower one wider, rimmed at the bottom."""
    hip, knee = her.bone(side + "UpLeg"), her.bone(side + "Leg")
    for i in range(rows):
        y = top - i * drop * 0.8
        f = min(0.9, max(0.1, (hip.y - (y - drop / 2)) / (hip.y - knee.y)))
        r = her.girth(side + "UpLeg", f) * 0.8 + 0.025 + 0.008 * i
        centre = hip + (knee - hip) * f
        span = math.degrees(width / r)
        a0, a1 = (100 - span, 100) if sx > 0 else (80, 80 + span)
        rings = [(y, r, r, -r), (y - drop, r + 0.012, r + 0.012, -r - 0.012)]
        lathe(b, rings, side + "UpLeg", lambda m, k, a, y=y: edge if m.y < y - drop + 0.014 else metal, a0, a1, 12,
              thick=0.01, origin=V((centre.x, 0, centre.z)))


def ranger(b, body):
    """Sheet 1: a dark laced leather jerkin, a hooded mantle over the shoulders, her crimson below the belt."""
    her = b.her

    def laced(c, n):
        if abs(c.x - 0.005) < 0.012 and c.z > 0.05 and her.waist - 0.05 < c.y < her.neck - 0.03:
            return "strap" if (c.y * 60) % 1 < 0.5 else "leather_hi"
        return stitched("leather_dark", "black", 12)(c, n)
    parts = jerkin(b, body, laced, her.neck - 0.01, her.hip + 0.03, 0.013, arms=0.25)
    mantle(b, her, "cloak", "cloak_dark", her.bust + 0.03)
    return parts


def mantle(b, her, color, dark, hem, hood=True):
    """A short cape over her shoulders and chest from her neck, the hood down behind it."""
    y0 = her.neck + 0.02
    rx, zf, zb = her.trunk(her.neck)
    sx = her.bone("LeftArm").x + her.girth("LeftArm", 0.2) + 0.03
    rings = [(y0, rx * 0.6, zf + 0.02, zb - 0.03),
             (her.neck, rx + 0.03, zf + 0.05, zb - 0.045),
             ((her.neck + hem) / 2 + 0.01, sx, her.trunk((her.neck + hem) / 2)[1] + 0.035, zb - 0.06),
             (hem, sx + 0.015, her.trunk(hem)[1] + 0.03, zb - 0.075)]
    lathe(b, rings, spine_weights(her, 0.6), lambda m, i, a: dark if i == len(rings) - 2 and m.y < hem + 0.02 else color,
          segs=28, thick=0.012)
    if hood:
        c = V((0, her.neck + 0.02, zb - 0.09))
        b.ball(c, (0.11, 0.07, 0.07), "Spine2", dark, 12)
        b.ball(c + V((0, 0.02, -0.01)), (0.095, 0.055, 0.06), "Spine2", color, 12)


def brigand(b, body):
    """Sheet 1: a brown coat of studded leather to the thighs, sleeved, with a high collar, shoulder pads and a belt."""
    her = b.her
    parts = jerkin(b, body, studs("coat", "bronze"), her.neck - 0.005, her.hip - 0.08, 0.016, arms=1.0, thighs=0.4,
                   forearms=0.35)
    collar(b, her, "coat", "coat_dark", 0.085, 0.028, 0.03)
    for side, sx in SIDES:
        pauldron(b, her, side, sx, "coat_hi", "coat_dark", 2, 1.0)
    belt(b, her, "leather_dark", buckle="iron_edge", grow=0.03)
    return parts


def scale(b, body):
    """Sheet 1: blue steel scales to the thighs, ending in points, with round pauldrons and a belt."""
    her = b.her
    sc = scales("blue", "blue_hi", "blue_dark")

    def hem(c, n):
        # The skirt ends in points: the lowest band of scales is cut in teeth.
        return sc(c, n)
    parts = jerkin(b, body, sc, her.neck - 0.005, her.hip - 0.06, 0.018, arms=0.45, thighs=0.42)
    collar(b, her, "blue", "blue_hi", 0.045, 0.02, 0.01)
    for side, sx in SIDES:
        pauldron(b, her, side, sx, "blue", "blue_hi", 2, 1.1)
    belt(b, her, "leather_dark", buckle="iron_edge", grow=0.035)
    return parts


def breastplate(b, body, her, metal, light, trim, under):
    """A cuirass shaped on her: a smooth shell over her chest and belly, trimmed at the neck and the waist, with a
    V of trim under the breast; under it, cloth on her arms' tops and her hips."""
    top, low = her.neck - 0.015, her.hip + 0.025

    def color(c, n):
        if c.y > top - 0.025 or c.y < low + 0.02:
            return trim
        # A V of trim under the breast, pointing down at her middle.
        v = her.bust - 0.07 - abs(c.x) * 0.5
        if c.z > 0.03 and abs(c.y - v) < 0.012:
            return trim
        return light if n.y > 0.35 or (n.z > 0.8 and abs(c.x) < 0.04) else metal
    parts = [shell(b.arm, body, trunk_keep(b, top, low), 0.03, color, 4, "Cuirass")]
    parts += jerkin(b, body, lambda c, n: under, her.hip + 0.04, her.hip - 0.09, 0.012, arms=0.5, thighs=0.15, name="Under")
    return parts


def faulds(b, her, metal, trim, rows=2):
    """Hooped plates round her hips below the belt, each trimmed at its lower edge, flaring out."""
    y = her.hip - 0.005
    for i in range(rows):
        y0, y1 = y - i * 0.04, y - i * 0.04 - 0.05
        rings = around(her, y0, y1, 0.035 + 0.012 * i, 0.05 + 0.012 * i, 2)
        lathe(b, rings, skirt_weights(her), lambda m, k, a: trim if m.y < y1 + 0.012 else metal, 20, 160, 24, thick=0.012)
        lathe(b, rings, skirt_weights(her), lambda m, k, a: trim if m.y < y1 + 0.012 else metal, 200, 340, 18, thick=0.012)


def plate(b, body):
    """Sheet 1: a silver cuirass trimmed in gold, gold-edged pauldrons of three lames, faulds and tassets, a gorget, and a
    crimson tabard between the legs."""
    her = b.her
    parts = breastplate(b, body, her, "silver", "silver_hi", "gold", "crimson_dark")
    collar(b, her, "silver", "gold", 0.06, 0.03, 0.015)
    for side, sx in SIDES:
        pauldron(b, her, side, sx, "silver", "gold", 3, 1.12)
        tasset(b, her, side, sx, her.leg - 0.06, 2, "silver", "gold")
    belt(b, her, "leather_dark", y=her.hip + 0.015, buckle="iron_edge", grow=0.045)
    faulds(b, her, "silver", "gold", 1)
    flap(b, her, 0.0, her.hip - 0.02, her.leg - 0.33, 0.13, "crimson", "gold")
    return parts


def knight(b, body):
    """Sheet 1: blackened plate with gold trims, great angular pauldrons, a crimson tabard, and a long crimson cape."""
    her = b.her
    parts = breastplate(b, body, her, "black_steel", "black_steel_hi", "gold", "black")
    parts += jerkin(b, body, lambda c, n: "gold" if abs(c.y - her.bone("LeftForeArm").y - 0.03) < 0.012 else "black_steel",
                    her.neck + 0.2, her.neck + 0.1, 0.02, arms=1.0, name="Rerebraces")
    collar(b, her, "black_steel", "gold", 0.08, 0.032, 0.02)
    for side, sx in SIDES:
        centre, rot, r = pauldron(b, her, side, sx, "black_steel", "gold", 3, 1.3)
        # A tall swept flange on each pauldron's top, gold-edged: the knight's outline.
        base = centre + rot @ V((0, (r * 0.85 + 0.012) * 1.3 - 0.01, 0))
        for k, (h, w) in enumerate(((0.11, 0.035), (0.07, 0.03))):
            p0 = base + V((-sx * 0.035 + sx * k * 0.06, 0, 0))
            b.spike(p0, p0 + V((sx * 0.04, h, -0.01)), w, side + "Arm", "gold" if k else "black_steel_hi", 4)
        tasset(b, her, side, sx, her.leg - 0.06, 2, "black_steel", "gold", 0.12)
    belt(b, her, "black", y=her.hip + 0.015, buckle="gold", grow=0.045)
    faulds(b, her, "black_steel", "gold", 1)
    flap(b, her, 0.0, her.hip - 0.02, her.leg - 0.36, 0.13, "crimson", "gold")
    cape(b, her, "crimson", "crimson_dark", "gold")
    return parts


def cape(b, her, color, dark, clasp=None, bottom=0.18):
    """A long cape from her shoulders nearly to her ankles, wrapping her back, its hem ragged."""
    top = her.neck + 0.005
    rx, zf, zb = her.trunk(her.neck - 0.03)
    shoulder = her.bone("LeftArm").x + her.girth("LeftArm", 0.2) + 0.025
    rings = []
    for i in range(9):
        t = i / 8
        y = top + (bottom - top) * t
        w = rx * 0.7 + (shoulder - rx * 0.7) * min(1.0, t * 6) + 0.12 * t
        back = zb - 0.035 - 0.16 * t
        rings.append((y, w, 0.04 + 0.2 * t, back))

    def weights(co):
        if co.y > her.bust:
            return spine_weights(her, 0.4)(co)
        t = min(1.0, (her.bust - co.y) / (her.bust - her.hip))
        return [("Spine2", 1 - t), ("Spine1", t * 0.5), ("Hips", t * 0.5)]

    def paint(m, i, a):
        if i == len(rings) - 2 and (int(a / 9) % 2 == 0):
            return dark
        return dark if (a % 30) < 4 else color
    lathe(b, rings, weights, paint, 200, 340, 30, thick=0.01)
    if clasp:
        for sx in (1, -1):
            b.ball(V((sx * rx * 0.75, top - 0.01, zf - 0.02)), (0.022, 0.022, 0.015), "Spine2", clasp)


def stag_hide(b, body):
    """Sheet 6: a pale stag hide coat, a fur collar, antlers rising from the shoulders."""
    her = b.her

    def dapple(c, n):
        return "hide_hi" if (math.sin(c.x * 60) + math.sin(c.y * 47) > 1.1) else "hide"
    parts = jerkin(b, body, dapple, her.neck - 0.005, her.hip - 0.07, 0.016, arms=0.7, thighs=0.3)
    mantle(b, her, "fur", "fur_dark", her.bust + 0.06, hood=False)
    for side, sx in SIDES:
        centre, rot, r = pauldron(b, her, side, sx, "hide", "fur_dark", 1, 1.05)
        base = centre + V((sx * 0.02, r * 0.8, -0.02))
        b.horn([base, base + V((sx * 0.03, 0.1, -0.02)), base + V((sx * 0.07, 0.2, -0.04)), base + V((sx * 0.06, 0.3, -0.03))], 0.022, 0.004, side + "Arm", "bone_hi")
        b.horn([base + V((sx * 0.03, 0.1, -0.02)), base + V((sx * 0.1, 0.16, 0.03)), base + V((sx * 0.14, 0.2, 0.06))], 0.015, 0.004, side + "Arm", "bone")
        b.horn([base + V((sx * 0.07, 0.2, -0.04)), base + V((sx * 0.14, 0.25, -0.08))], 0.012, 0.004, side + "Arm", "bone")
    belt(b, her, "leather_dark", buckle="bone_hi", grow=0.035)
    return parts


def cinder_jerkin(b, body):
    """Sheet 6: a charred jerkin with glowing ember seams, ember-rimmed pads and collar."""
    her = b.her

    def seams(c, n):
        if abs(math.sin(c.x * 38 + c.y * 22)) < 0.12 or abs(math.sin(c.y * 31 - c.x * 17)) < 0.08:
            return "ember"
        return "black" if (c.y * 12) % 1 > 0.15 else "leather_dark"
    parts = jerkin(b, body, seams, her.neck - 0.005, her.hip - 0.07, 0.016, arms=0.6, thighs=0.3)
    collar(b, her, "black", "ember", 0.06, 0.028, 0.02)
    for side, sx in SIDES:
        pauldron(b, her, side, sx, "black", "ember_mid", 2, 1.0)
    belt(b, her, "black", buckle="ember_mid", grow=0.035)
    return parts


# ---------------------------------------------------------------- helms (their own layer, cut by her body)

def helm_rings(her, low, margin, front_low=None, steps=7, crown=0.085):
    """Rings clearing her head and hair by margin from low up to her crown, then rounding over it."""
    rings = []
    # The dome starts well below the crown, so a helm is round over her head rather than a pot with a lid.
    top = her.top - crown
    for i in range(steps + 1):
        y = top + (low - top) * i / steps
        rx, zf, zb = her.head(max(y, her.chin + 0.02))
        rings.append((y, rx + margin, zf + margin, zb - margin))
    # Over the crown: the top ring shrunk on a quarter ellipse to the apex.
    y0, rx, zf, zb = rings[0]
    dome = []
    for k in range(5, 0, -1):
        phi = math.radians(90 * k / 6)
        dome.append((y0 + (her.top + margin - y0) * math.sin(phi), rx * math.cos(phi), zf * math.cos(phi) + (zf + zb) / 2 * (1 - math.cos(phi)),
                     zb * math.cos(phi) + (zf + zb) / 2 * (1 - math.cos(phi))))
    return dome + rings, her.top + margin


def helmet(b, her, color, rim=None, low=None, margin=0.016, paint=None):
    low = her.chin - 0.005 if low is None else low
    rings, apex = helm_rings(her, low, margin)
    lathe(b, rings, "Head", paint or color, segs=28, cap=apex, thick=0.012)
    if rim:
        r = rings[-1]
        lathe(b, [(r[0] + 0.022, r[1] + 0.006, r[2] + 0.006, r[3] - 0.006), (r[0] - 0.004, r[1] + 0.006, r[2] + 0.006, r[3] - 0.006)],
              "Head", rim, segs=28, thick=0.012)
    return rings


def front_of(rings, y):
    """The helmet's front z at a height (between its rings)."""
    for a, c in zip(rings, rings[1:]):
        if c[0] <= y <= a[0]:
            t = (a[0] - y) / (a[0] - c[0])
            return a[2] + (c[2] - a[2]) * t
    return rings[-1][2]


def side_of(rings, y):
    for a, c in zip(rings, rings[1:]):
        if c[0] <= y <= a[0]:
            t = (a[0] - y) / (a[0] - c[0])
            return a[1] + (c[1] - a[1]) * t
    return rings[-1][1]


def hood_shape(b, body, color, dark, trim=None):
    """A cloth hood up over her head, the face open, a short cape on her shoulders. One surface of rings from a peak
    leaning back over her crown, close round her head and hair, closing under the jaw, spreading over the shoulders;
    the inside lined dark so the opening reads as depth. Its back hangs lower over her ponytail."""
    her = b.her
    hx, hf, hb = her.head(her.top - 0.06)
    # Over the head: rounded like a helmet's crown, loose (2026-11-02: rings straight up from her hair's envelope made a
    # bucket with a fin on top).
    crown, top = helm_rings(her, her.chin + 0.01, 0.02, steps=5)
    keys = [tuple(r) for r in crown]
    nx, nf, nb = her.trunk(her.neck + 0.02)
    shoulder = her.bone("LeftArm").x + her.girth("LeftArm", 0.1) + 0.02
    keys.append((her.neck + 0.03, nx + 0.06, nf + 0.07, min(nb, keys[-1][3]) - 0.0))
    keys.append((her.neck - 0.005, shoulder - 0.03, her.trunk(her.neck - 0.01)[1] + 0.05, keys[-1][3]))
    hem = her.neck - 0.07
    keys.append((hem, shoulder, her.trunk(hem)[1] + 0.035, keys[-1][3] + 0.01))
    rings = []
    for i in range(len(keys) - 1):
        for t in range(3):
            f = t / 3
            rings.append(tuple(k0 + (k1 - k0) * f for k0, k1 in zip(keys[i], keys[i + 1])))
    rings.append(keys[-1])
    segs = 36
    face_y = (her.chin + her.top) / 2 - 0.015
    frx, fry = 0.092, (her.top - her.chin) / 2 - 0.005
    apex = V((0, top, (keys[0][2] + keys[0][3]) / 2))

    def ring(r, inset):
        y, rx, zf, zb = r
        pts = []
        for k in range(segs):
            a = 2 * math.pi * k / segs
            s = math.sin(a)
            z = (zf - inset) * s if s > 0 else -(zb + inset) * s
            # The back hangs lower over her ponytail.
            drop = 0.06 * max(0.0, -s) if y < her.neck else 0.0
            pts.append(V(((rx - inset) * math.cos(a), y - drop, z)))
        return pts

    weights = head_weights(her, her.chin - 0.03, her.chin + 0.03)

    def hole(p):
        return p.z > 0.05 and p.y > her.chin - 0.02 and ((p.x - her.face_x) / frx) ** 2 + ((p.y - face_y) / fry) ** 2 < 1

    def near_hole(p):
        return p.z > 0.03 and p.y > her.chin - 0.03 and ((p.x - her.face_x) / (frx * 1.3)) ** 2 + ((p.y - face_y) / (fry * 1.2)) ** 2 < 1

    for inset, shade in ((0.0, color), (0.01, "slit")):
        grid = [ring(r, inset) for r in rings]
        if inset > 0:
            grid = [g for g, r in zip(grid, rings) if r[0] < her.top - 0.02]
        for k in range(segs if inset == 0 else 0):
            j = (k + 1) % segs
            tri = [apex, grid[0][k], grid[0][j]]
            b.facing(tri, V((0, 1, 0)), weights, shade)
        for i in range(len(grid) - 1):
            for k in range(segs):
                j = (k + 1) % segs
                quad = [grid[i][k], grid[i][j], grid[i + 1][j], grid[i + 1][k]]
                mid = sum(quad, V((0, 0, 0))) / 4
                if hole(mid):
                    continue
                edge = inset == 0 and near_hole(mid)
                low = inset == 0 and i >= len(grid) - 2 and trim
                b.add(quad, [[0, 1, 2, 3] if inset == 0 else [3, 2, 1, 0]], weights,
                      dark if edge else (trim if low else shade), fixed=True)
    return []


def hood(b, body):
    """Sheet 2: a crimson hood."""
    return hood_shape(b, body, "crimson", "crimson_dark")


def mask(b, body):
    """Sheet 2: a close leather cap with a brow band, and a dark mask over her nose and mouth: only her eyes show."""
    her = b.her
    eyes = her.chin + (her.top - her.chin) * 0.52
    rings, apex = helm_rings(her, eyes + 0.025, 0.014)
    lathe(b, rings, "Head", lambda m, i, a: "leather_hi" if (a % 60) < 6 else "leather", segs=28, cap=apex, thick=0.01)
    r = rings[-1]
    lathe(b, [(r[0] + 0.012, r[1] + 0.006, r[2] + 0.006, r[3] - 0.006), (r[0] - 0.01, r[1] + 0.006, r[2] + 0.006, r[3] - 0.006)],
          "Head", "strap", segs=28, thick=0.01)
    low = her.chin - 0.025
    mask_rings = [(eyes - 0.03, *her.head(eyes - 0.03)), (her.chin + 0.03, *her.head(her.chin + 0.03)), (low, *her.head(her.chin))]
    mask_rings = [(y, rx + 0.022, zf + 0.022, zb - 0.022) for y, rx, zf, zb in mask_rings]
    lathe(b, mask_rings, "Head", lambda m, i, a: "cloth" if i == 0 and m.y > eyes - 0.04 else "cloth_dark", 20, 160, 20, thick=0.01)
    return []


def barbute(b, body):
    """Sheet 2: a closed blue steel helm with a T opening and a ridge over the crown."""
    her = b.her
    eyes = her.chin + (her.top - her.chin) * 0.52
    rings = helmet(b, her, "blue", "blue_dark", her.chin - 0.02, 0.016,
                   lambda m, i, a: "blue_hi" if m.y > eyes + 0.04 and 75 < a < 105 else "blue")
    front = front_of(rings, eyes) + 0.003
    b.box(V((her.face_x, eyes, front)), (0.12, 0.022, 0.016), "Head", "slit")
    b.box(V((her.face_x, eyes - 0.05, front_of(rings, eyes - 0.05) + 0.003)), (0.032, 0.09, 0.016), "Head", "slit")
    crest(b, her, 0.028, 0.012, 0.025, "blue_hi", "blue_hi")
    return []


def crest(b, her, margin, width, height, color, alt):
    """A ridge or fin along her crown from brow to nape."""
    rx, zf, zb = her.head(her.top - 0.04)
    for k in range(9):
        a = math.radians(15 + k * 18.75)
        c = math.cos(a)
        z = (zf + margin) * c if c > 0 else (zb - margin) * -c
        y = her.top + margin - 0.01 + math.sin(a) * height
        b.box(V((0, y - 0.3 * (1 - math.sin(a)) * 0.1, z)), (width, 0.03 + height * math.sin(a), 0.055), "Head", alt if k % 2 else color)


def visored(b, body):
    """Sheet 2: a silver helm with a visor of slits, gold trims and a tall gold crest."""
    her = b.her
    eyes = her.chin + (her.top - her.chin) * 0.52
    rings = helmet(b, her, "silver", "gold", her.chin - 0.025, 0.018,
                   lambda m, i, a: "silver_hi" if m.y > eyes + 0.06 else "silver")
    # The visor: a raised plate over her face with an eye slit and breaths below.
    front = front_of(rings, eyes)
    lathe(b, [(eyes + 0.03, side_of(rings, eyes + 0.03) + 0.008, front_of(rings, eyes + 0.03) + 0.012, -0.0),
              (eyes - 0.04, side_of(rings, eyes - 0.04) + 0.01, front_of(rings, eyes - 0.04) + 0.02, -0.0),
              (her.chin - 0.02, side_of(rings, her.chin) + 0.004, front_of(rings, her.chin) + 0.01, -0.0)],
          "Head", lambda m, i, a: "gold" if abs(m.y - eyes - 0.03) < 0.008 else "silver", 30, 150, 20, thick=0.01)
    b.box(V((her.face_x, eyes, front + 0.017)), (0.14, 0.018, 0.012), "Head", "slit")
    for k in range(3):
        for sx in (-1, 1):
            b.box(V((her.face_x + sx * 0.04, eyes - 0.06 - k * 0.018, front_of(rings, eyes - 0.06) + 0.022)), (0.025, 0.006, 0.01), "Head", "slit")
    # The crest: a gold fin from brow to nape.
    crest(b, her, 0.028, 0.016, 0.06, "gold", "gold_dark")
    return []


def horned(b, body):
    """Sheet 2: a blackened helm, gold-trimmed, a T slit, and great horns curving out and up."""
    her = b.her
    eyes = her.chin + (her.top - her.chin) * 0.52
    rings = helmet(b, her, "black_steel", "gold", her.chin - 0.025, 0.018,
                   lambda m, i, a: "gold" if 82 < a < 98 and m.y > eyes + 0.03 else "black_steel")
    front = front_of(rings, eyes) + 0.003
    b.box(V((her.face_x, eyes, front)), (0.12, 0.02, 0.016), "Head", "eye_glow")
    b.box(V((her.face_x, eyes - 0.045, front_of(rings, eyes - 0.045) + 0.003)), (0.028, 0.07, 0.016), "Head", "slit")
    y = eyes + 0.06
    for side, sx in SIDES:
        base = V((sx * (side_of(rings, y) - 0.012), y, -0.03))
        pts = [base, base + V((sx * 0.08, 0.02, 0.01)), base + V((sx * 0.15, 0.08, 0.03)), base + V((sx * 0.18, 0.17, 0.05)),
               base + V((sx * 0.15, 0.26, 0.07))]
        b.horn(pts, 0.04, 0.005, "Head", "bone")
        b.limb(base - V((sx * 0.01, 0, 0)), base + V((sx * 0.03, 0.005, 0)), 0.045, 0.045, "Head", "gold", 10)
    return []


def falconer(b, body):
    """Sheet 6: a hood of feathers with a falcon's head over the brow, its beak hooked down."""
    her = b.her
    hood_shape(b, body, "feather_dark", "feather", "feather")
    eyes = her.chin + (her.top - her.chin) * 0.52
    _, zf, _ = her.head(her.top - 0.05)
    brow = V((her.face_x, her.top + 0.03, zf + 0.02))
    b.ball(brow, (0.07, 0.05, 0.07), "Head", "feather")
    b.ball(brow + V((0, 0.005, 0.045)), (0.04, 0.03, 0.04), "Head", "feather_dark")
    b.spike(brow + V((0, -0.005, 0.07)), brow + V((0, -0.06, 0.12)), 0.026, "Head", "gold")
    for sx in (-1, 1):
        b.ball(brow + V((sx * 0.04, 0.01, 0.05)), (0.012, 0.012, 0.01), "Head", "gold")
    for k in range(7):
        a = math.radians(-60 + k * 20)
        root = V((math.sin(a) * 0.06, her.top + 0.03, -0.06 - math.cos(a) * 0.03))
        b.limb(root, root + V((math.sin(a) * 0.12, 0.12, -0.13)), 0.024, 0.006, "Head", "feather" if k % 2 else "feather_dark", 4)
    return []


def unblinking_crown(b, body):
    """Sheet 6: a crown of dark iron round her brow, an eye open in its front, tall spikes."""
    her = b.her
    y = her.chin + (her.top - her.chin) * 0.72
    rx, zf, zb = her.head(y)
    band = [(y + 0.04, rx + 0.022, zf + 0.022, zb - 0.022), (y - 0.01, rx + 0.022, zf + 0.022, zb - 0.022)]
    lathe(b, band, "Head", "iron_dark", segs=28, thick=0.012)
    lathe(b, [(y + 0.046, rx + 0.028, zf + 0.028, zb - 0.028), (y + 0.034, rx + 0.028, zf + 0.028, zb - 0.028)], "Head", "violet_dark", segs=28, thick=0.01)
    for k in range(9):
        a = math.radians(90 + (k - 4) * 40)
        s, c = math.sin(a), math.cos(a)
        z = (zf + 0.024) * s if s > 0 else -(zb - 0.024) * s
        base = V(((rx + 0.024) * c, y + 0.04, z))
        h = 0.11 if k == 4 else (0.08 if k % 2 == 0 else 0.055)
        b.spike(base, base + V((c * 0.02, h, s * 0.02)), 0.022, "Head", "iron_dark")
    eye = V((her.face_x, y + 0.03, zf + 0.04))
    b.ball(eye, (0.03, 0.022, 0.014), "Head", "violet")
    b.ball(eye + V((0, 0, 0.012)), (0.012, 0.014, 0.008), "Head", "eye_glow")
    return []


# ---------------------------------------------------------------- boots

def shin(b, side, low, high, grow0, grow1, color):
    """A ring round her shin from a share low of the way from ankle to knee up to high, clearing her by grow."""
    her = b.her
    knee, ankle = her.bone(side + "Leg"), her.bone(side + "Foot")
    p0 = ankle + (knee - ankle) * low
    p1 = ankle + (knee - ankle) * high
    b.limb(p0, p1, her.girth(side + "Leg", 1 - low) + grow0, her.girth(side + "Leg", 1 - high) + grow1, side + "Leg", color, 12)


def boot(b, body, color_of, up, offset=0.014, smooth=2):
    return [shell(b.arm, body, feet_keep(b, up), offset, color_of, smooth, "Boots")]


def leg_point(b, side, f):
    knee, ankle = b.her.bone(side + "Leg"), b.her.bone(side + "Foot")
    return ankle + (knee - ankle) * f


def wrapped(b, body):
    """Sheet 4: soft shoes and cloth wound round her shins."""
    def wind(c, n):
        if c.y < 0.11:
            return "leather_dark"
        return "cloth" if ((c.y + c.x * 0.4) * 40) % 1 < 0.55 else "cloth_dark"
    return boot(b, body, wind, 0.75, 0.016)


def leather_boots(b, body):
    """Sheet 4: tall leather boots with a folded cuff under the knee."""
    parts = boot(b, body, lambda c, n: "leather_dark" if c.y < 0.03 else "leather", 0.82, 0.016)
    for side, _ in SIDES:
        shin(b, side, 0.8, 0.95, 0.01, 0.016, "leather_hi")
    return parts


def strapped(b, body):
    """Sheet 4: dark boots buckled with three straps."""
    parts = boot(b, body, lambda c, n: "leather_dark", 0.85, 0.016)
    for side, sx in SIDES:
        for k in range(3):
            f = 0.2 + k * 0.25
            shin(b, side, f, f + 0.05, 0.006, 0.006, "strap")
            p = leg_point(b, side, f + 0.025)
            r = b.her.girth(side + "Leg", 1 - f)
            b.box(p + V((sx * r * 0.7, 0, r * 0.75)), (0.026, 0.03, 0.012), side + "Leg", "bronze")
    return parts


def sabatons(b, body):
    """Sheet 4: silver plate over foot and shin, a domed knee guard."""
    parts = boot(b, body, lambda c, n: "silver_dark" if (c.z * 30) % 1 < 0.2 and c.y < 0.1 else ("silver_hi" if n.z > 0.7 else "silver"),
                 0.92, 0.024, 3)
    for side, sx in SIDES:
        shin(b, side, 0.9, 0.95, 0.014, 0.014, "silver_dark")
        knee = b.her.bone(side + "Leg")
        r = b.her.girth(side + "Leg", 0.05)
        b.ball(knee + V((0, -0.005, r * 0.6)), (r * 0.9, r * 0.85, r * 0.6), side + "Leg", "silver_hi")
        b.ball(knee + V((sx * r * 0.7, -0.01, r * 0.2)), (r * 0.35, r * 0.6, r * 0.6), side + "Leg", "silver")
    return parts


def spiked(b, body):
    """Sheet 4: blackened greaves, gold bands, spikes at the knee and the heel."""
    parts = boot(b, body, lambda c, n: "black_steel_hi" if n.z > 0.75 else "black_steel", 0.92, 0.022, 3)
    for side, sx in SIDES:
        shin(b, side, 0.3, 0.34, 0.012, 0.012, "gold")
        shin(b, side, 0.88, 0.93, 0.012, 0.012, "gold")
        knee = b.her.bone(side + "Leg")
        r = b.her.girth(side + "Leg", 0.05)
        b.ball(knee + V((0, 0, r * 0.6)), (r * 0.95, r * 0.9, r * 0.65), side + "Leg", "black_steel")
        b.spike(knee + V((0, 0, r * 1.1)), knee + V((0, 0.04, r * 1.1 + 0.11)), 0.026, side + "Leg", "gold")
        ankle = b.her.bone(side + "Foot")
        b.spike(ankle + V((0, 0.02, -0.05)), ankle + V((sx * 0.02, 0.04, -0.15)), 0.02, side + "Leg", "black_steel_hi")
    return parts


def windrunner(b, body):
    """Sheet 6: light boots with wings of feathers at the ankles."""
    parts = boot(b, body, lambda c, n: "leather_hi" if c.y < 0.03 else "leather", 0.6, 0.016)
    for side, sx in SIDES:
        shin(b, side, 0.55, 0.62, 0.01, 0.01, "gold")
        ankle = b.her.bone(side + "Foot")
        r = b.her.girth(side + "Leg", 0.9)
        for k in range(5):
            root = ankle + V((sx * (r + 0.01), 0.04 + k * 0.022, -0.02))
            b.limb(root, root + V((sx * (0.06 + k * 0.012), 0.07 + k * 0.012, -0.08 - k * 0.012)), 0.016, 0.004, side + "Leg",
                   "feather" if k % 2 else "feather_dark", 4)
    return parts


def stalker(b, body):
    """Not on the sheets (2026-11-02, after their look): black boots topped with fur, bone claws over the toes."""
    parts = boot(b, body, lambda c, n: "black", 0.6, 0.016)
    for side, sx in SIDES:
        for k in range(3):
            f = 0.74 + k * 0.07
            shin(b, side, f, f + 0.08, 0.016 - k * 0.003, 0.012 - k * 0.003, "fur_dark" if k % 2 else "fur")
        toe = b.her.bone(side + "ToeBase")
        for k in (-1, 0, 1):
            b.spike(toe + V((k * 0.025, 0.03, 0.03)), toe + V((k * 0.035, 0.0, 0.11)), 0.013, side + "Foot", "bone_hi")
    return parts


# ---------------------------------------------------------------- belts

def rope(b, body):
    """Sheet 5: a knotted rope, its ends hanging."""
    her = b.her
    lathe(b, around(her, her.hip + 0.03, her.hip + 0.005, 0.03, 0.03, 1), "Hips",
          lambda m, i, a: "cloth" if (a % 16) < 9 else "cloth_dark", segs=36, thick=0.012)
    rx, zf, zb = her.trunk(her.hip + 0.02)
    knot = V((0.05, her.hip + 0.02, zf + 0.035))
    b.ball(knot, (0.026, 0.024, 0.02), "Hips", "cloth_dark")
    for dx in (-0.01, 0.015):
        b.limb(knot, knot + V((dx + 0.01, -0.15, 0.015)), 0.009, 0.007, "Hips", "cloth", 5)
    return []


def pouch_belt(b, body):
    """Sheet 5: a leather belt hung with pouches."""
    her = b.her
    belt(b, her, "leather", her.hip + 0.015, 0.045, 0.03, buckle="iron_edge")
    for x, zs in ((0.09, 1), (-0.1, 1), (0.06, -1)):
        rx, zf, zb = her.trunk(her.hip - 0.02)
        z = zf + 0.03 if zs > 0 else zb - 0.03
        b.box(V((x, her.hip - 0.025, z)), (0.06, 0.07, 0.04), "Hips", "leather_dark")
        b.box(V((x, her.hip + 0.0, z + zs * 0.012)), (0.062, 0.02, 0.03), "Hips", "leather_hi")
    return []


def studded(b, body):
    """Sheet 5: a wide belt of dark leather with bronze studs and a big buckle."""
    her = b.her
    y = her.hip + 0.015
    lathe(b, around(her, y + 0.04, y - 0.04, 0.035, 0.035, 2), "Hips",
          lambda m, i, a: "bronze" if (a % 20) < 5 and abs(m.y - y) < 0.02 else "leather_dark", segs=48, thick=0.014)
    rx, zf, zb = her.trunk(y)
    b.box(V((0, y, zf + 0.048)), (0.075, 0.07, 0.014), "Hips", "bronze")
    b.box(V((0, y, zf + 0.055)), (0.04, 0.035, 0.01), "Hips", "leather_dark")
    return []


def tassets(b, body):
    """Sheet 5: a steel belt with plates hanging over the thighs."""
    her = b.her
    belt(b, her, "iron", her.hip + 0.015, 0.05, 0.035, "iron_edge", "iron_edge")
    for side, sx in SIDES:
        tasset(b, her, side, sx, her.leg - 0.02, 3, "iron", "iron_edge", 0.11, 0.06)
    return []


def war_girdle(b, body):
    """Sheet 5: a wide black girdle, gold-rimmed, a gold lion's head for a buckle, plates over the hips."""
    her = b.her
    y = her.hip + 0.02
    belt(b, her, "black", y, 0.085, 0.04, "gold")
    rx, zf, zb = her.trunk(y)
    face = V((0, y, zf + 0.06))
    b.ball(face, (0.05, 0.05, 0.025), "Hips", "gold")
    b.ball(face + V((0, -0.008, 0.02)), (0.022, 0.02, 0.012), "Hips", "gold_dark")
    for sx in (-1, 1):
        b.box(face + V((sx * 0.018, 0.012, 0.024)), (0.012, 0.008, 0.006), "Hips", "slit")
    for side, sx in SIDES:
        tasset(b, her, side, sx, her.leg - 0.03, 2, "black_steel", "gold", 0.12, 0.07)
    return []


def bandolier(b, body):
    """Sheet 6: a strap across her chest hung with small skulls, a belt below."""
    her = b.her
    belt(b, her, "leather_dark", her.hip + 0.015, 0.045, 0.03, buckle="bone_hi")
    rx, zf, zb = her.trunk(her.bust)
    sx_top = her.bone("LeftShoulder").x + 0.05
    top_f = V((sx_top, her.neck - 0.005, her.trunk(her.neck - 0.02)[1] + 0.03))
    low_f = V((-rx - 0.01, her.hip + 0.06, her.trunk(her.hip + 0.06)[1] + 0.025))
    top_b = V((sx_top, her.neck - 0.005, her.trunk(her.neck - 0.02)[2] - 0.03))
    low_b = V((-rx - 0.01, her.hip + 0.06, her.trunk(her.hip + 0.06)[2] - 0.025))
    for p0, p1, front in ((top_f, low_f, True), (top_b, low_b, False)):
        pts = []
        for k in range(9):
            p = p0 + (p1 - p0) * (k / 8)
            zf_, zb_ = her.trunk(p.y)[1:]
            p.z = max(p.z, zf_ + 0.025) if front else min(p.z, zb_ - 0.025)
            pts.append(p)
        for q0, q1 in zip(pts, pts[1:]):
            b.limb(q0, q1, 0.02, 0.02, "Spine1", "leather", 6)
    # Clear of her bust: the strap bowed out where it crosses it.
    for k in range(4):
        t = 0.25 + k * 0.17
        c = top_f + (low_f - top_f) * t
        c.z = max(c.z, her.trunk(c.y)[1] + 0.045)
        b.ball(c, (0.03, 0.034, 0.026), "Spine1", "bone_hi")
        for ex in (-1, 1):
            b.box(c + V((ex * 0.011, 0.006, 0.024)), (0.01, 0.009, 0.006), "Spine1", "slit")
    return []


# ---------------------------------------------------------------- gloves

def bracer(b, side, low, high, grow0, grow1, color):
    """A ring round her forearm from a share low of the way from wrist to elbow up to high, clearing her by grow."""
    her = b.her
    elbow, wrist = her.bone(side + "ForeArm"), her.bone(side + "Hand")
    b.limb(wrist + (elbow - wrist) * low, wrist + (elbow - wrist) * high, her.girth(side + "ForeArm", 1 - low) + grow0,
           her.girth(side + "ForeArm", 1 - high) + grow1, side + "ForeArm", color, 12)


def hands(b, body, color, up=0.25, offset=0.01):
    return [shell(b.arm, body, hands_keep(b, up), offset, color, 1, "Hands")]


def wraps(b, body):
    """Sheet 3: cloth wound round her hands and forearms."""
    return hands(b, body, lambda c, n: "cloth" if ((abs(c.x) + c.y * 0.5) * 40) % 1 < 0.5 else "cloth_dark", 0.6, 0.012)


def gloves(b, body):
    """Sheet 3: leather gloves with a flared cuff."""
    parts = hands(b, body, lambda c, n: "leather", 0.15)
    for side, _ in SIDES:
        bracer(b, side, 0.05, 0.3, 0.012, 0.03, "leather_hi")
    return parts


def bracers(b, body):
    """Sheet 3: leather bracers with bronze plates, laced, over leather gloves."""
    parts = hands(b, body, lambda c, n: "leather_dark", 0.1)
    for side, sx in SIDES:
        bracer(b, side, 0.08, 0.8, 0.014, 0.018, "leather")
        bracer(b, side, 0.2, 0.68, 0.022, 0.026, "bronze")
        for k in range(3):
            bracer(b, side, 0.12 + k * 0.28, 0.16 + k * 0.28, 0.028, 0.03, "strap")
    return parts


def gauntlets(b, body):
    """Sheet 3: silver gauntlets, plated fingers, a flared cuff."""
    parts = hands(b, body, lambda c, n: "silver_dark" if (abs(c.x) * 60) % 1 < 0.2 else "silver", 0.1, 0.014)
    for side, _ in SIDES:
        bracer(b, side, 0.0, 0.35, 0.016, 0.05, "silver_hi")
        bracer(b, side, 0.32, 0.38, 0.05, 0.052, "silver_dark")
        bracer(b, side, 0.36, 0.75, 0.018, 0.022, "silver")
    return parts


def claws(b, body):
    """Sheet 3: black gauntlets with gold knuckles and long claws."""
    her = b.her
    parts = hands(b, body, lambda c, n: "black_steel", 0.1, 0.014)
    for side, sx in SIDES:
        bracer(b, side, 0.0, 0.3, 0.016, 0.045, "black_steel")
        bracer(b, side, 0.27, 0.33, 0.045, 0.047, "gold")
        bracer(b, side, 0.3, 0.75, 0.018, 0.022, "black_steel")
        hand, tip = her.bone(side + "Hand"), her.tail(side + "Hand")
        d = (tip - hand).normalized()
        across = d.cross(V((0, 0, 1))).normalized()
        b.limb(tip - d * 0.005 - across * 0.03, tip - d * 0.005 + across * 0.03, 0.018, 0.018, side + "Hand", "gold", 8)
        for k in (-1, 0, 1):
            base = tip + across * (k * 0.02) + d * 0.01
            b.spike(base, base + d * 0.1 + V((0, -0.02, 0.02)), 0.009, side + "Hand", "iron_edge")
    return parts


def fletcher(b, body):
    """Sheet 6: green leather gloves, gold-banded cuffs with arrow feathers along them."""
    parts = hands(b, body, lambda c, n: "leather", 0.1)
    for side, sx in SIDES:
        elbow, wrist = b.her.bone(side + "ForeArm"), b.her.bone(side + "Hand")
        bracer(b, side, 0.05, 0.7, 0.014, 0.018, "green")
        bracer(b, side, 0.05, 0.1, 0.02, 0.02, "gold")
        bracer(b, side, 0.66, 0.71, 0.022, 0.022, "gold")
        r = b.her.girth(side + "ForeArm", 0.5)
        for k in range(3):
            root = wrist + (elbow - wrist) * (0.3 + k * 0.12) + V((0, r + 0.01, 0))
            b.limb(root, root + V((-sx * 0.06, 0.07, -0.02)), 0.015, 0.004, side + "ForeArm", "feather" if k % 2 else "crimson_hi", 4)
    return parts


def bloodletter(b, body):
    """Not on the sheets (2026-11-02, after their look): crimson gloves, black bracers with bone spurs."""
    parts = hands(b, body, lambda c, n: "crimson", 0.1, 0.012)
    for side, sx in SIDES:
        bracer(b, side, 0.0, 0.3, 0.014, 0.04, "crimson_hi")
        bracer(b, side, 0.28, 0.75, 0.018, 0.022, "black")
        elbow, wrist = b.her.bone(side + "ForeArm"), b.her.bone(side + "Hand")
        r = b.her.girth(side + "ForeArm", 0.5)
        for k in range(3):
            p0 = wrist + (elbow - wrist) * (0.35 + k * 0.17) + V((0, r + 0.015, 0))
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


def pose_as_rest(arm, objects):
    """Makes the pose her Mixamo file holds (the T-pose, frame 1) the rest pose of the armature and every mesh
    (2026-11-02, the owner: "Har feet are pointing inward"): her model rests in an A-pose, and exported so, Unity built
    each body's humanoid avatar from the A-pose, so the pack's clips bent her knees together and turned her toes in by
    about 30 degrees. Everything is built at rest first (her measures, the gear), then posed and frozen here."""
    bpy.context.view_layer.update()
    meshes = [o for o in objects if o.type == "MESH"]
    for o in meshes:
        bpy.context.view_layer.objects.active = o
        for m in [m for m in o.modifiers if m.type == "ARMATURE"]:
            bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    if arm.animation_data:
        arm.animation_data.action = None
    for o in meshes:
        o.modifiers.new("Armature", "ARMATURE").object = arm


def export(arm, objects, path):
    pose_as_rest(arm, objects)
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for o in objects:
        o.select_set(True)
    # Scale applied to everything (2026-10-13): left as units, Unity read the armature node at 100 and every bone with
    # it, so a piece the bake hangs on a bone (the quiver on her chest) came out a hundred times too big once her body
    # was ours rather than Mixamo's file.
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, add_leaf_bones=False, bake_anim=False,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="STRIP", embed_textures=False)
    print("REPORT wrote", os.path.basename(path))


# The looks, by tier (AppearanceRules: five bands of item level) and the named legendaries' own (2026-10-05).
BODIES = {"bare": None, "ranger": ranger, "brigand": brigand, "scale": scale, "plate": plate, "knight": knight,
          "stag_hide": stag_hide, "cinder_jerkin": cinder_jerkin}
WORN = {
    # The hood was the owner's Meshy model from 2026-10-07 (fit_hood.py) to 2026-10-13; built here again since.
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
            b = Builder(arm, body)
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
            b = Builder(arm, body)
            parts = build(b, body)
            if b.colors:
                piece = b.mesh("Gear")
                piece.data.materials.append(body.data.materials[0])
                parts.append(piece)
            bpy.data.objects.remove(body, do_unlink=True)
            export(arm, parts, os.path.join(FOLDER, "wild_arrow_%s_%s.fbx" % (code, look)))


if __name__ == "__main__":
    main()
