# The pet wolf from the owner's rigged fox (2026-11-07, the owner: "there is a rigged model of a fox. Remake it in blender
# to look like the wolf"; ArtSource/models/pets/fox_source/fox.glb, a low-poly fox with Idle, Walk, Run, Bite, Death and
# more). The fox is made the owner's wolf (ArtSource/pixel/ai_exchange/pets/wolf_strip.jpg) without touching its rig, so
# its animations stay as they are:
# - its shape: each vertex moved round the bones it is weighted to, by a factor across the bone (thicker or slimmer) and
#   along it from the bone's head (longer or shorter): thicker legs, a deeper chest and neck, a broader head and muzzle,
#   smaller ears, a slimmer tail;
# - its coat: the texture repainted by colour (the fox's orange the wolf's light grey, its dark legs grey, its white kept
#   as the pale face and chest, the tail's white tip dark), and a brown-grey saddle painted onto every face turned up
#   over the neck, the shoulders and the back;
# - its clips named as the game's (idle, walk, run, attack from Bite, hit from the start of Fall, death).
# Writes Assets/_Project/Art/Models/Pets/pet_wolf/pet_wolf.fbx and pet_wolf_albedo.png.
#   Blender -b -P ArtSource/tools/pets/fox_to_wolf.py -- <fox.glb> <out folder>
import bpy, sys, os, math
import numpy as np
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
src, out = os.path.abspath(args[0]), os.path.abspath(args[1])
os.makedirs(out, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
for o in list(bpy.context.scene.objects):
    if o.type == "MESH" and o.name.startswith("Icosphere"):
        bpy.data.objects.remove(o, do_unlink=True)
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")

# ------------------------------------------------------------------ the shape
# bone name start: (across, along). Across scales a vertex's distance from the bone's axis, along its distance from the
# bone's head along the axis.
SHAPE = {
    "Front_Leg_Upper": (1.35, 1.08), "Front_Leg_Lower": (1.35, 1.15), "Front_Leg_Ankle": (1.3, 1.15), "Front_Leg_Foot": (1.25, 1.0),
    "Back_Leg_Upper": (1.3, 1.08), "Back_Leg_Lower": (1.35, 1.12), "Back_Leg_Ankle": (1.3, 1.15), "Back_Leg_Foot": (1.25, 1.0),
    "Front_Leg_Shoulder": (1.2, 1.0), "Back_Leg_Pelvis": (1.15, 1.0),
    "Spine_3": (1.38, 1.0), "Spine_4": (1.6, 1.0), "Spine_2001": (1.12, 1.0), "Stomach": (1.1, 1.0),
    "Head": (1.42, 1.08), "Nose": (1.25, 1.45), "Chin": (1.22, 1.38),
    "Ear_": (0.95, 1.35),
    # The owner's wolf has a full brush of a tail, thickest past the root.
    "Tail_Base": (1.15, 1.0), "Tail_Mid": (1.35, 1.0), "Tail_End": (1.3, 1.0), "Tail_Tip": (1.0, 0.9),
}

mw = mesh.matrix_world
aw = arm.matrix_world
bones = {b.name: (aw @ b.head_local, aw @ b.tail_local) for b in arm.data.bones}
groups = {g.index: g.name for g in mesh.vertex_groups}


def factor(name):
    for key, f in SHAPE.items():
        if name.startswith(key):
            return f
    return None


me = mesh.data
moved = 0
new = []
for v in me.vertices:
    p = mw @ v.co
    offset = Vector((0, 0, 0))
    total = 0.0
    for g in v.groups:
        name = groups.get(g.group)
        f = factor(name) if name else None
        if not f or g.weight <= 0 or name not in bones:
            continue
        head, tail = bones[name]
        axis = (tail - head)
        length = axis.length
        if length < 1e-6:
            continue
        axis.normalize()
        rel = p - head
        along = rel.dot(axis)
        radial = rel - axis * along
        target = head + axis * (along * f[1]) + radial * f[0]
        offset += (target - p) * g.weight
        total += g.weight
    if total > 0:
        moved += 1
    new.append(mw.inverted() @ (p + offset))
for v, co in zip(me.vertices, new):
    v.co = co
print("REPORT shape: %d of %d vertices moved" % (moved, len(me.vertices)))

# ------------------------------------------------------------------ the coat
img = next(i for i in bpy.data.images if i.size[0] > 0)
w, h = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
rgb = px[..., :3]
mx, mn = rgb.max(-1), rgb.min(-1)
light = rgb[..., 0] * 0.3 + rgb[..., 1] * 0.55 + rgb[..., 2] * 0.15
# The fox's dark-brown legs are darker than its orange coat.
dark = light < 0.33
orange = (rgb[..., 0] > rgb[..., 2] + 0.18) & (mx > 0.25) & ~dark
white = (mn > 0.72)
# The wolf's greys (the strip's): its coat a little warm, by the fox's own shading.
coat = np.stack([light * 0.4 + 0.07, light * 0.4 + 0.075, light * 0.4 + 0.09], -1)
out_rgb = rgb.copy()
out_rgb[orange] = np.clip(coat[orange], 0, 1)
# The fox's dark legs the wolf's pale grey legs.
out_rgb[dark & ~white] = np.clip(np.stack([light * 0.3 + 0.36, light * 0.3 + 0.36, light * 0.3 + 0.37], -1)[dark & ~white], 0, 1)
out_rgb[white] = np.array([0.64, 0.63, 0.6])
px[..., :3] = out_rgb


def raster(tri_uv, colour, mix):
    """A face's triangle in UV space painted (mixed toward a colour)."""
    pts = np.array([[u * w, v * h] for u, v in tri_uv])
    x0, y0 = np.floor(pts.min(0)).astype(int)
    x1, y1 = np.ceil(pts.max(0)).astype(int)
    x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, w - 1), min(y1, h - 1)
    if x1 < x0 or y1 < y0:
        return
    ys, xs = np.mgrid[y0:y1 + 1, x0:x1 + 1]
    a, b, c = pts
    def edge(p, q, x, y):
        return (q[0] - p[0]) * (y + 0.5 - p[1]) - (q[1] - p[1]) * (x + 0.5 - p[0])
    e0, e1, e2 = edge(a, b, xs, ys), edge(b, c, xs, ys), edge(c, a, xs, ys)
    inside = ((e0 >= -0.5) & (e1 >= -0.5) & (e2 >= -0.5)) | ((e0 <= 0.5) & (e1 <= 0.5) & (e2 <= 0.5))
    region = px[y0:y1 + 1, x0:x1 + 1, :3]
    region[inside] = region[inside] * (1 - mix) + np.array(colour) * mix


# The mane: the neck and the shoulders a dark brown-grey all round but underneath (the owner's strip: a big dark ruff),
# the rest of the back a little darker on top.
uv = me.uv_layers.active.data
painted = 0
for poly in me.polygons:
    n = (mw.to_3x3() @ poly.normal).normalized()
    v = me.vertices[poly.vertices[0]]
    best = max(v.groups, key=lambda g: g.weight, default=None)
    name = groups.get(best.group) if best else ""
    if not name:
        continue
    loops = list(poly.loop_indices)
    tris = [[uv[loops[0]].uv, uv[loops[k]].uv, uv[loops[k + 1]].uv] for k in range(1, len(loops) - 1)]
    if name.startswith(("Spine_3", "Spine_4", "Front_Leg_Shoulder")) and n.z > -0.45:
        for t in tris:
            raster(t, (0.16, 0.135, 0.11), 0.6 + 0.25 * max(0.0, n.z))
        painted += 1
    elif name.startswith(("Head", "Nose", "Chin", "Headtip")):
        # The head distinct from the dark mane (the owner: "make the head more distinct"): a pale mask on the muzzle,
        # under the jaw and the lower cheeks (a badger's white face when it covered the whole head), the head's sides
        # a mid grey, a dark cap over the brow, the nose's tip black.
        # From the front these ran together (2026-11-07, the owner: "fix the face"): the cap now reaches down to the
        # brow and is near black, the whole muzzle but its ridge and the cheeks below the eyes are a bright mask.
        muzzle = name.startswith(("Nose", "Chin"))
        if n.z > 0.4 and not muzzle:
            colour, mix = (0.14, 0.135, 0.13), 0.85
        elif muzzle and n.z > 0.7:
            colour, mix = (0.3, 0.29, 0.29), 0.8
        elif muzzle or n.z < 0.0:
            colour, mix = (0.8, 0.78, 0.74), 0.92
        else:
            colour, mix = (0.52, 0.51, 0.5), 0.75
        for t in tris:
            raster(t, colour, mix)
    elif name.startswith("Ear_"):
        for t in tris:
            raster(t, (0.13, 0.12, 0.11), 0.8)
    elif name.startswith(("Tail_Base", "Tail_Mid")):
        # The tail's brush dark on top, lighter below.
        for t in tris:
            raster(t, (0.2, 0.19, 0.18) if n.z > -0.2 else (0.48, 0.47, 0.46), 0.45)
    elif name.startswith(("Spine", "Hips", "Back_Leg_Pelvis")) and n.z > 0.4:
        for t in tris:
            raster(t, (0.14, 0.14, 0.15), 0.3 * min(1.0, (n.z - 0.4) / 0.4))
# The nose's tip black: the Nose faces furthest forward.
nose = []
for poly in me.polygons:
    v = me.vertices[poly.vertices[0]]
    best = max(v.groups, key=lambda g: g.weight, default=None)
    if best and groups.get(best.group, "").startswith(("Nose", "Headtip")):
        nose.append(((mw @ poly.center).y, poly))
nose.sort(key=lambda c: c[0])
for _, poly in nose[:max(1, len(nose) // 4)]:
    loops = list(poly.loop_indices)
    for k in range(1, len(loops) - 1):
        raster([uv[loops[0]].uv, uv[loops[k]].uv, uv[loops[k + 1]].uv], (0.06, 0.05, 0.05), 0.95)
# The tail's tip dark: faces of the tail's tip bone.
for poly in me.polygons:
    v = me.vertices[poly.vertices[0]]
    best = max(v.groups, key=lambda g: g.weight, default=None)
    name = groups.get(best.group) if best else ""
    if name and name.startswith(("Tail_Tip", "Tail_End")):
        loops = list(poly.loop_indices)
        for k in range(1, len(loops) - 1):
            raster([uv[loops[0]].uv, uv[loops[k]].uv, uv[loops[k + 1]].uv], (0.18, 0.17, 0.16), 0.75)
print("REPORT coat: saddle on %d faces" % painted)
img.pixels = px.ravel()
albedo = os.path.join(out, "pet_wolf_albedo.png")
img.filepath_raw = albedo
img.file_format = "PNG"
img.save()

# ------------------------------------------------------------------ the clips
NAMES = {"Idle": "idle", "Walk": "walk", "Run": "run", "Bite": "attack", "Death": "death"}
keep = []
for act in list(bpy.data.actions):
    if act.name in NAMES:
        act.name = NAMES[act.name]
        keep.append(act)
    elif act.name == "Fall":
        # A hit: the first frames of the fall, a stagger (cut by its strip below).
        act.name = "hit"
        keep.append(act)
for act in list(bpy.data.actions):
    if act not in keep:
        bpy.data.actions.remove(act)
arm.animation_data_create()
for act in keep:
    act.use_fake_user = True
    track = arm.animation_data.nla_tracks.new()
    track.name = act.name
    strip = track.strips.new(act.name, int(act.frame_range[0]), act)
    if act.name == "hit":
        strip.action_frame_end = min(act.frame_range[1], 9.0)
arm.animation_data.action = None
print("REPORT clips: " + ", ".join(sorted(a.name for a in keep)))

bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active = arm
arm.name = "pet_wolf"
fbx = os.path.join(out, "pet_wolf.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False,
                         bake_anim_use_nla_strips=True, path_mode="STRIP", embed_textures=False, apply_scale_options="FBX_SCALE_ALL")
print("REPORT wrote", fbx)
