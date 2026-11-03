# Rigs and animates a Hunyuan3D beast in Blender (2026-11-03, the forest's Grey Wolf, Brood Spider, Tusked Boar and Brood
# Mother; Mixamo rigs only people). From ArtSource/models/forest/<name>/rig.json:
#   "name", "shape" (the textured GLB), "albedo", "out" (Assets/_Project/Art/Models/Enemies/<name>), "kind": "beast"
#   (four legs) or "spider" (eight), "height" (metres to the top of the figure), "facing": the head's way in the shape as
#   seen from above [x, y] (the shapes come turned: the wolf drawn three-quarter, the boar side-on).
# The figure is turned to face -Y (Unity's +Z, as wolf.py's beasts), stood on the ground and centred. Its feet are found
# as the clusters of its lowest vertices; each leg is two bones from the body down to its foot (a beast's hip or shoulder
# over its paw, a spider's from the thorax's side with its knee at the leg's highest point), the body a chain of bones
# from tail to head. Blender's automatic weights bind it, missed vertices taking their nearest weighted neighbour's.
# Clips keyed in code, in the takes wolf.py writes (EnemyBakeSetup.GenericBodies reads them by name): idle, walk, run,
# attack, hit, death, and dash (a beast's charge). Writes <out>/<name>.fbx and <out>/<name>_albedo.png.
#   Blender -b -P ArtSource/tools/rig_beast.py -- <rig.json>
import bpy, sys, os, json, math, shutil, mathutils
import numpy as np
from mathutils import kdtree

V = mathutils.Vector
M = mathutils.Matrix
args = sys.argv[sys.argv.index("--") + 1:]
CFG = json.load(open(os.path.abspath(args[0])))
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FPS = 24


def path(p):
    p = os.path.expanduser(p)
    return p if os.path.isabs(p) else os.path.join(ROOT, p)


def load():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = FPS
    bpy.ops.import_scene.gltf(filepath=path(CFG["shape"]))
    ms = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in bpy.context.scene.objects:
        o.select_set(o in ms)
    bpy.context.view_layer.objects.active = ms[0]
    if len(ms) > 1:
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for x in [x for x in bpy.context.scene.objects if x != o]:
        bpy.data.objects.remove(x, do_unlink=True)
    co = np.array([v.co[:] for v in o.data.vertices])
    # Turn the head to -Y: the long axis of the figure seen from above, its end on the side of the hint.
    xy = co[:, :2] - co[:, :2].mean(0)
    _, _, vt = np.linalg.svd(xy, full_matrices=False)
    axis = vt[0]
    hint = np.array(CFG["facing"], dtype=float)
    if np.dot(axis, hint) < 0:
        axis = -axis
    if CFG["kind"] == "spider":
        axis = hint / np.linalg.norm(hint)   # a spider's legs spread wider than it is long
    angle = math.atan2(axis[1], axis[0])
    turn = -math.pi / 2 - angle
    # Euler, or the turn is not applied: a glTF import turns its objects by quaternion.
    o.rotation_mode = "XYZ"
    o.rotation_euler = (0, 0, turn)
    bpy.ops.object.transform_apply(rotation=True)
    co = np.array([v.co[:] for v in o.data.vertices])
    s = CFG["height"] / (co[:, 2].max() - co[:, 2].min())
    o.scale = (s, s, s)
    bpy.ops.object.transform_apply(scale=True)
    co = np.array([v.co[:] for v in o.data.vertices])
    o.location = (-(co[:, 0].max() + co[:, 0].min()) / 2, -(co[:, 1].max() + co[:, 1].min()) / 2, -co[:, 2].min())
    bpy.ops.object.transform_apply(location=True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=0.00001)
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    o.name = CFG["name"]
    return o


def kmeans(points, k, iters=40):
    rng = np.random.default_rng(1)
    # Start from points far apart.
    centres = [points[rng.integers(len(points))]]
    for _ in range(k - 1):
        d = np.min([np.sum((points - c) ** 2, axis=1) for c in centres], axis=0)
        centres.append(points[np.argmax(d)])
    centres = np.array(centres)
    for _ in range(iters):
        lab = np.argmin(((points[:, None] - centres[None]) ** 2).sum(-1), axis=1)
        centres = np.array([points[lab == i].mean(0) if np.any(lab == i) else centres[i] for i in range(k)])
    return centres


def feet(co, count):
    H = co[:, 2].max()
    low = co[co[:, 2] < H * (0.06 if CFG["kind"] == "beast" else 0.12)]
    c = kmeans(low[:, :2], count)
    return [V((x, y, 0.02 * H)) for x, y in c]


def body_line(co):
    """The body's middle from back to front: the mid of the figure's upper half at each length."""
    H = co[:, 2].max()
    ys = np.linspace(co[:, 1].max(), co[:, 1].min(), 9)
    pts = []
    for y in ys:
        sel = co[(np.abs(co[:, 1] - y) < (ys[0] - ys[-1]) / 16) & (co[:, 2] > H * 0.35) & (np.abs(co[:, 0]) < 0.35 * (co[:, 0].max() - co[:, 0].min()))]
        if len(sel) < 10:
            continue
        pts.append(V((0.0, y, (sel[:, 2].min() + sel[:, 2].max()) / 2)))
    return pts


def build(o):
    co = np.array([v.co[:] for v in o.data.vertices])
    H = co[:, 2].max()
    line = body_line(co)
    arm_data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_data.edit_bones

    def bone(name, h, t, parent=None):
        b = eb.new(name)
        b.head, b.tail = V(h), V(t)
        if (b.tail - b.head).length < 1e-3:
            b.tail = b.head + V((0, 0, 0.05))
        if parent:
            b.parent = eb[parent]
        b.roll = 0
        return b
    back, front = line[0], line[-1]
    mid = line[len(line) // 2]
    bone("root", (0, 0, 0), (0, 0, H * 0.2))
    if CFG["kind"] == "beast":
        # Hips at the back third, chest at the front third, neck and head to the front end, a tail from the back.
        hips = line[len(line) // 4]
        chest = line[(3 * len(line)) // 4 - 1]
        bone("hips", hips, mid, "root")
        bone("chest", mid, chest, "hips")
        bone("neck", chest, line[-2], "chest")
        bone("head", line[-2], front + V((0, -0.02, 0)), "neck")
        bone("tail", hips, back + V((0, 0.05, -0.05 * H)), "hips")
        legs = feet(co, 4)
        for i, f in enumerate(sorted(legs, key=lambda p: (p.y, p.x))):
            fore = f.y < mid.y
            side = "L" if f.x > 0 else "R"
            name = ("front_" if fore else "hind_") + side
            top = V((f.x * 0.8, f.y, (chest if fore else hips).z - 0.05 * H))
            knee = V((f.x * 0.95, f.y + (0.03 if fore else -0.05) * H, H * 0.33))
            parent = "chest" if fore else "hips"
            bone(name + "_upper", top, knee, parent)
            bone(name + "_lower", knee, f, name + "_upper")
    else:
        # A thorax in the middle, the abdomen behind, the head in front; eight legs from the thorax's sides.
        bone("thorax", mid + V((0, 0.05 * H, 0)), mid + V((0, -0.08 * H, 0)), "root")
        bone("abdomen", mid + V((0, 0.05 * H, 0)), back, "thorax")
        bone("head", mid + V((0, -0.08 * H, 0)), front, "thorax")
        legs = feet(co, 8)
        for f in legs:
            side = "L" if f.x > 0 else "R"
            order = sorted([g for g in legs if (g.x > 0) == (f.x > 0)], key=lambda p: p.y).index(f)
            name = "leg%d_%s" % (order, side)
            attach = V((math.copysign(0.06 * H, f.x), mid.y + (f.y - mid.y) * 0.2, mid.z))
            # The knee: the highest point of the leg's vertices halfway out.
            halfway = attach.lerp(f, 0.45)
            sel = co[(np.abs(co[:, 0] - halfway.x) < 0.12 * H) & (np.abs(co[:, 1] - halfway.y) < 0.12 * H)]
            kz = sel[:, 2].max() if len(sel) else attach.z + 0.2 * H
            knee = V((halfway.x, halfway.y, max(kz - 0.04 * H, attach.z)))
            bone(name + "_upper", attach, knee, "thorax")
            bone(name + "_lower", knee, f, name + "_upper")
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def bind(o, arm):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    vs = o.data.vertices
    weighted = [v for v in vs if any(g.weight > 1e-3 for g in v.groups)]
    missed = len(vs) - len(weighted)
    if missed and weighted:
        tree = kdtree.KDTree(len(weighted))
        for i, v in enumerate(weighted):
            tree.insert(v.co, i)
        tree.balance()
        for v in vs:
            if any(g.weight > 1e-3 for g in v.groups):
                continue
            _, i, _ = tree.find(v.co)
            for g in weighted[i].groups:
                if g.weight > 1e-3:
                    o.vertex_groups[g.group].add([v.index], g.weight, "REPLACE")
    print("REPORT bound: %d of %d missed, filled" % (missed, len(vs)))


# ---------------------------------------------------------------- the clips

def ease(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def key(arm, name, seconds, pose):
    """pose(t) -> {bone: (axis 'x'|'z' in world, degrees)} or lists of those, and 'root_loc': (x, y, z)."""
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data_create()
    arm.animation_data.action = action
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    frames = int(round(seconds * FPS))
    for f in range(frames + 1):
        values = pose(f / FPS)
        for pb in arm.pose.bones:
            turns = values.get(pb.name, [])
            if isinstance(turns, tuple):
                turns = [turns]
            q = mathutils.Quaternion()
            rest = pb.bone.matrix_local.to_3x3()
            for ax, deg in turns:
                # A turn about a world axis, put in the bone's own frame.
                world_axis = V((1, 0, 0)) if ax == "x" else V((0, 0, 1)) if ax == "z" else V((0, 1, 0))
                local_axis = rest.inverted() @ world_axis
                q = mathutils.Quaternion(local_axis, math.radians(deg)) @ q
            pb.rotation_quaternion = q
            pb.keyframe_insert("rotation_quaternion", frame=f)
        root = arm.pose.bones["root"]
        root.location = arm.pose.bones["root"].bone.matrix_local.to_3x3().inverted() @ V(values.get("root_loc", (0, 0, 0)))
        root.keyframe_insert("location", frame=f)
    track = arm.animation_data.nla_tracks.new()
    track.name = name
    track.strips.new(name, 0, action)
    arm.animation_data.action = None


def beast_gait(p, stride, bend, H, legs, body=6):
    pose = {"root_loc": (0, 0, abs(math.sin(2 * math.pi * p)) * 0.04 * H)}
    pose["chest"] = ("x", math.sin(2 * math.pi * p) * body * 0.5)
    pose["neck"] = ("x", -math.sin(2 * math.pi * p) * body * 0.6)
    pose["tail"] = ("x", -10 + math.sin(2 * math.pi * p) * 10)
    for name, offset in legs:
        s, c = math.sin(2 * math.pi * (p + offset)), math.cos(2 * math.pi * (p + offset))
        pose[name + "_upper"] = ("x", stride * s)
        pose[name + "_lower"] = ("x", (bend if name.startswith("front") else -bend) * max(0.0, c) * 0.8)
    return pose


def beast_clips(arm, H):
    trot = [("front_L", 0.0), ("hind_R", 0.0), ("front_R", 0.5), ("hind_L", 0.5)]
    gallop = [("front_L", 0.0), ("front_R", 0.12), ("hind_L", 0.5), ("hind_R", 0.62)]

    def idle(t):
        b = math.sin(2 * math.pi * t / 2.0)
        return {"chest": ("x", b * 1.5), "neck": [("x", -b * 2), ("z", math.sin(2 * math.pi * t / 2.0 + 1) * 6)],
                "head": ("z", math.sin(2 * math.pi * t / 2.0 + 1) * 4), "tail": ("z", math.sin(2 * math.pi * t) * 8),
                "root_loc": (0, 0, b * 0.004 * H)}

    def attack(t):
        crouch = ease(t / 0.3) * (1 - ease((t - 0.3) / 0.15))
        spring = ease((t - 0.3) / 0.2) * (1 - ease((t - 0.62) / 0.28))
        bite = math.sin(min(1.0, max(0.0, (t - 0.45) / 0.2)) * math.pi)
        pose = {"root_loc": (0, -0.25 * H * spring, -0.08 * H * crouch + 0.08 * H * spring),
                "chest": ("x", -10 * crouch + 8 * spring), "hips": ("x", 6 * crouch - 6 * spring),
                "neck": ("x", -20 * crouch + 15 * spring), "head": ("x", 10 * crouch - 25 * bite), "tail": ("x", -25 * spring)}
        for side in ("L", "R"):
            pose["front_%s_upper" % side] = ("x", -25 * crouch + 45 * spring)
            pose["front_%s_lower" % side] = ("x", 30 * crouch - 10 * spring)
            pose["hind_%s_upper" % side] = ("x", 35 * crouch - 30 * spring)
            pose["hind_%s_lower" % side] = ("x", -45 * crouch + 15 * spring)
        return pose

    def hit(t):
        k = math.sin(min(1.0, t / 0.35) * math.pi)
        return {"chest": [("x", -8 * k), ("y", 6 * k)], "neck": ("x", 15 * k), "head": ("x", 10 * k), "root_loc": (0, 0.05 * H * k, 0)}

    def death(t):
        k = ease(t / 0.7)
        pose = {"root": ("y", -88 * k), "root_loc": (0, 0, -0.05 * H * k), "neck": ("x", 25 * k), "head": ("x", 15 * k), "tail": ("x", 10 * k)}
        for side in ("L", "R"):
            pose["front_%s_upper" % side] = ("x", 20 * k)
            pose["hind_%s_upper" % side] = ("x", -20 * k)
        return pose
    key(arm, "idle", 2.0, idle)
    key(arm, "walk", 1.0, lambda t: beast_gait(t / 1.0, 18, 30, H, trot, 3))
    key(arm, "run", 0.5, lambda t: beast_gait(t / 0.5, 36, 45, H, gallop, 6))
    key(arm, "dash", 0.5, lambda t: beast_gait(t / 0.5, 40, 50, H, gallop, 8))
    key(arm, "attack", 0.9, attack)
    key(arm, "hit", 0.35, hit)
    key(arm, "death", 1.0, death)


def spider_clips(arm, H):
    names = [pb.name[:-6] for pb in arm.pose.bones if pb.name.endswith("_upper")]
    # Alternating tetrapod: legs 0 and 2 of one side with 1 and 3 of the other.
    def group(n):
        i, side = int(n[3]), n[-1]
        return (i % 2 == 0) == (side == "L")

    def gait(p, swing, lift):
        pose = {"root_loc": (0, 0, abs(math.sin(4 * math.pi * p)) * 0.015 * H), "abdomen": ("x", math.sin(4 * math.pi * p) * 3)}
        for n in names:
            ph = p + (0.0 if group(n) else 0.5)
            side = 1 if n.endswith("L") else -1
            pose[n + "_upper"] = [("z", side * swing * math.sin(2 * math.pi * ph)), ("y", side * lift * max(0.0, math.cos(2 * math.pi * ph)))]
        return pose

    def idle(t):
        b = math.sin(2 * math.pi * t / 2.0)
        pose = {"abdomen": ("x", b * 3), "head": ("z", math.sin(2 * math.pi * t / 1.3) * 4), "root_loc": (0, 0, b * 0.006 * H)}
        for n in names:
            pose[n + "_upper"] = ("y", (1 if n.endswith("L") else -1) * math.sin(2 * math.pi * t / 2.0 + int(n[3])) * 3)
        return pose

    def attack(t):
        rear = ease(t / 0.35) * (1 - ease((t - 0.55) / 0.3))
        strike = math.sin(min(1.0, max(0.0, (t - 0.35) / 0.2)) * math.pi)
        pose = {"thorax": ("x", 25 * rear), "head": ("x", -30 * strike), "root_loc": (0, -0.15 * H * strike, 0.06 * H * rear)}
        for n in names:
            if n.startswith("leg0"):
                pose[n + "_upper"] = ("y", (1 if n.endswith("L") else -1) * (40 * rear))
        return pose

    def hit(t):
        k = math.sin(min(1.0, t / 0.35) * math.pi)
        return {"thorax": [("x", -10 * k)], "abdomen": ("x", 12 * k), "root_loc": (0, 0.04 * H * k, 0)}

    def death(t):
        k = ease(t / 0.8)
        pose = {"root": ("y", 180 * ease((t - 0.1) / 0.6) if CFG.get("flip_on_death", True) else 0), "root_loc": (0, 0, 0.1 * H * math.sin(min(1, t / 0.7) * math.pi))}
        for n in names:
            side = 1 if n.endswith("L") else -1
            pose[n + "_upper"] = ("y", side * 50 * k)
            pose[n + "_lower"] = ("y", side * 60 * k)
        return pose
    key(arm, "idle", 2.0, idle)
    key(arm, "walk", 1.0, lambda t: gait(t / 1.0, 12, 12))
    key(arm, "run", 0.5, lambda t: gait(t / 0.5, 20, 18))
    key(arm, "attack", 0.9, attack)
    key(arm, "hit", 0.35, hit)
    key(arm, "death", 1.0, death)


def main():
    out = path(CFG["out"])
    os.makedirs(out, exist_ok=True)
    o = load()
    H = np.array([v.co[2] for v in o.data.vertices]).max()
    arm = build(o)
    bind(o, arm)
    if CFG["kind"] == "beast":
        beast_clips(arm, H)
    else:
        spider_clips(arm, H)
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    f = os.path.join(out, CFG["name"] + ".fbx")
    bpy.ops.export_scene.fbx(filepath=f, use_selection=True, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
                             bake_anim_use_nla_strips=False, path_mode="STRIP", embed_textures=False, apply_scale_options="FBX_SCALE_ALL")
    shutil.copyfile(path(CFG["albedo"]), os.path.join(out, CFG["name"] + "_albedo.png"))
    print("REPORT wrote", os.path.relpath(f, ROOT), "bones", len(arm.data.bones))


main()
