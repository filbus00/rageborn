# Weapons the forest's enemies hold (2026-11-02): rigid models with no rig, baked into the enemy's own sheet in its
# hands (EnemyBakeSetup.HandPieces, SpriteBakeJob.Piece.withBody). Hunyuan3D's shapes come with the weapons in the
# pictures fused to the hands, which Mixamo's auto-rigger refused, so the figures are drawn empty-handed and the weapons
# made here. Each in the held pieces' frame (WildArrowBakeSetup.Held, SpriteBaker.PlaceInFist): the grip's middle at the
# origin, the haft along +Y with the head up, the edge toward +Z, in metres. The texture is a strip of flat colours.
#   Blender -b -P ArtSource/tools/props/enemy_weapons.py
# Writes Assets/_Project/Art/Models/Weapons/enemy_<name>.fbx and enemy_<name>_albedo.png.
import bpy, bmesh, os, math
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
FOLDER = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Weapons")
COLORS = [("steel", (150, 152, 158)), ("steel_dark", (78, 80, 88)), ("leather", (58, 38, 26)), ("brass", (150, 112, 52)),
          ("wood", (92, 64, 40)), ("steel_light", (222, 226, 232)), ("hole", (12, 10, 10))]
NAMES = [c[0] for c in COLORS]


def texture(path):
    w, h = 8 * len(COLORS), 8
    px = np.zeros((h, w, 4), dtype=np.float32)
    for i, (_, c) in enumerate(COLORS):
        px[:, i * 8:(i + 1) * 8, :3] = np.array(c) / 255.0
        px[:, i * 8:(i + 1) * 8, 3] = 1.0
    img = bpy.data.images.new("albedo", w, h, alpha=True)
    img.pixels = px.ravel()
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()


class Model:
    def __init__(self):
        self.bm = bmesh.new()
        self.colors = []

    def add(self, verts, faces, color):
        vs = [self.bm.verts.new(v) for v in verts]
        for f in faces:
            try:
                self.bm.faces.new([vs[i] for i in f])
                self.colors.append(NAMES.index(color))
            except ValueError:
                pass

    def box(self, c, size, color):
        sx, sy, sz = (s / 2 for s in size)
        verts = [(c[0] + x, c[1] + y, c[2] + z) for x in (-sx, sx) for y in (-sy, sy) for z in (-sz, sz)]
        self.add(verts, [[0, 1, 3, 2], [4, 6, 7, 5], [0, 4, 5, 1], [2, 3, 7, 6], [0, 2, 6, 4], [1, 5, 7, 3]], color)

    def tube(self, y0, y1, r0, r1, color, sides=8):
        verts = []
        for y, r in ((y0, r0), (y1, r1)):
            for k in range(sides):
                a = 2 * math.pi * k / sides
                verts.append((math.cos(a) * r, y, math.sin(a) * r))
        faces = [[k, (k + 1) % sides, sides + (k + 1) % sides, sides + k] for k in range(sides)]
        faces += [list(reversed(range(sides))), list(range(sides, 2 * sides))]
        self.add(verts, faces, color)

    def blade(self, y0, length, width, curve, thick, color, edge):
        """A blade up +Y from y0, its edge toward +Z, curving toward +Z by curve at the tip, tapering to a point."""
        n = 8
        rows = []
        for i in range(n + 1):
            t = i / n
            y = y0 + length * t
            bend = curve * t * t
            w = width * (1 - t ** 1.6) + 0.002
            back, front = -w * 0.35 + bend, w * 0.65 + bend
            rows.append([(-thick / 2, y, back), (thick / 2, y, back), (0, y, front)])
        verts = [v for r in rows for v in r]
        faces = []
        for i in range(n):
            a, b = i * 3, (i + 1) * 3
            faces += [[a, a + 1, b + 1, b], [a + 1, a + 2, b + 2, b + 1], [a + 2, a, b, b + 2]]
        faces.append([0, 2, 1])
        self.add(verts, faces, color)
        # A darker spine along the back of the blade, so it reads at pixel size.
        self.add([(-thick / 2 - 0.0005, y0, -width * 0.35), (thick / 2 + 0.0005, y0, -width * 0.35),
                  (thick / 2 + 0.0005, y0 + length * 0.8, -width * 0.12 + curve * 0.64),
                  (-thick / 2 - 0.0005, y0 + length * 0.8, -width * 0.12 + curve * 0.64)], [[0, 1, 2, 3]], edge)

    def export(self, name):
        mesh = bpy.data.meshes.new(name)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces[:])
        self.bm.to_mesh(mesh)
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        uv = mesh.uv_layers.new(name="UVMap").data
        for poly, c in zip(mesh.polygons, self.colors):
            for li in poly.loop_indices:
                uv[li].uv = ((c + 0.5) / len(COLORS), 0.5)
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        obj.data.materials.append(mat)
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        path = os.path.join(FOLDER, "enemy_%s.fbx" % name)
        # Blender's z up becomes Unity's y up: the model is built y up, so it is turned to stand in Blender first.
        obj.rotation_euler = (math.radians(90), 0, 0)
        bpy.ops.object.transform_apply(rotation=True)
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                                 axis_forward="-Z", axis_up="Y", path_mode="STRIP")
        texture(path.replace(".fbx", "_albedo.png"))
        print("REPORT wrote", os.path.relpath(path, ROOT))


def curved_dagger():
    """The Cutthroat's dagger (cutthroat.jpeg): a curved steel blade a hand and a half long, a brass guard, a leather
    grip, a brass pommel."""
    m = Model()
    m.tube(-0.05, 0.05, 0.014, 0.013, "leather")
    m.box((0, -0.058, 0), (0.026, 0.016, 0.026), "brass")
    m.box((0, 0.058, 0.004), (0.016, 0.014, 0.07), "brass")
    m.blade(0.064, 0.21, 0.036, 0.05, 0.006, "steel", "steel_dark")
    m.export("curved_dagger")


def prism(m, outline, thick, color, x=0.0):
    """A flat slab: an outline in the (y, z) plane, extruded thick along x about x."""
    n = len(outline)
    verts = [(x - thick / 2, y, z) for y, z in outline] + [(x + thick / 2, y, z) for y, z in outline]
    faces = [list(reversed(range(n))), list(range(n, 2 * n))]
    faces += [[k, (k + 1) % n, n + (k + 1) % n, n + k] for k in range(n)]
    m.add(verts, faces, color)


def cleaver():
    """The Sack Thief's cleaver (2026-11-03, the owner: "make the design of the cleaver more cleaver-like"): a butcher's
    cleaver. A short thick round wooden handle with three brass rivets and a rounded butt; a steel bolster; a tall
    squared blade whose straight spine runs on from the handle's top, wider at its square tip than at its heel, its edge
    a little bellied and ground bright, a hanging hole in the top corner by the tip."""
    m = Model()
    # The handle along +Y, the grip's middle at the origin: thicker toward the butt, a rounded knob at the end.
    m.tube(-0.11, 0.08, 0.03, 0.026, "wood")
    m.tube(-0.125, -0.11, 0.022, 0.032, "wood")
    for y in (-0.06, 0.0, 0.06):
        m.box((0, y, 0.0), (0.066, 0.016, 0.016), "brass")
    # The bolster where the handle meets the blade.
    m.box((0, 0.095, 0.0), (0.03, 0.035, 0.062), "steel_dark")
    # The blade, from the bolster up along +Y; its spine just behind the handle's line (z -0.03), its edge toward +Z.
    heel, tip = 0.11, 0.5
    edge = []
    for i in range(9):
        t = i / 8
        y = heel + (tip - heel) * t
        edge.append((y, 0.2 + 0.05 * t + 0.022 * math.sin(math.pi * t)))
    outline = [(heel, -0.03), (tip, -0.03)] + list(reversed(edge))
    prism(m, outline, 0.012, "steel")
    # The edge ground bright: a band along the belly.
    band = [(y, z - 0.035) for y, z in edge] + list(reversed(edge))
    prism(m, band, 0.014, "steel_light")
    # The spine, thick and dark, and the hanging hole by the tip.
    m.box((0, (heel + tip) / 2, -0.026), (0.02, tip - heel, 0.016), "steel_dark")
    hole = [(tip - 0.06 + 0.02 * math.cos(a), 0.03 + 0.02 * math.sin(a)) for a in [2 * math.pi * k / 10 for k in range(10)]]
    prism(m, hole, 0.016, "hole")
    m.export("cleaver")


if __name__ == "__main__":
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(FOLDER, exist_ok=True)
    curved_dagger()
    cleaver()
