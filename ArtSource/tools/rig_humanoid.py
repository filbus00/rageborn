# Rigs a Hunyuan3D figure without Mixamo and puts Mixamo clips on it (2026-11-03, the owner: "Try rigging all the models
# and implement them in game"). The forest's figures stand in an A-pose facing -Y (Hunyuan's front), z up.
#   Blender -b -P ArtSource/tools/rig_humanoid.py -- <rig.json>
# rig.json (ArtSource/models/forest/<name>/rig.json):
#   "shape": the textured GLB (project_texture.py), "albedo": its texture, "height": metres, "name": the enemy's name,
#   "out": the folder to write (Assets/_Project/Art/Models/Enemies/<name>),
#   "joints": front-view positions [x, z] in metres after the figure is scaled to its height, feet at z 0 and centred in
#   x (read off front_grid.py's picture; the figure's left is +x): hips, neck, head, head_top, and per side (L, R):
#   arm (the shoulder joint), elbow, wrist, hand (the fingertips), upleg (the hip joint), knee, ankle,
#   "clips": {clip: [source fbx, loop]}: Mixamo files (any mixamorig skeleton) to put on this rig.
# Writes <out>/<name>.fbx (the body on its own rig) and <out>/<name>_<clip>.fbx (the rig alone, animated), all resting in
# the figure's own A-pose: Unity imports the clips as generic (EnemyBakeSetup.OwnRigs) so they pose its bones exactly.
#
# The skeleton has Mixamo's bone names (the game finds hands and hips through a humanoid avatar) and only the bones the
# figure needs: no fingers but a thumb and an index for the grip. Blender's automatic weights bind the mesh; where they
# fail on a generated mesh (vertices left with no weight) a smoothed copy is bound and its weights carried over.
# A clip is retargeted bone by bone in world space: each bone of ours turns as the source bone turned, after the turn
# that takes our rest direction onto the source's, so the arms' A-pose and the source's T-pose meet; the hips travel as
# the source's did, scaled to our height, and a loop's travel is taken out evenly (a run in place).
import bpy, sys, os, json, math, mathutils
import numpy as np

V = mathutils.Vector
args = sys.argv[sys.argv.index("--") + 1:]
CFG_PATH = os.path.abspath(args[0])
CFG = json.load(open(CFG_PATH))
BASE = os.path.dirname(CFG_PATH)
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))


def path(p):
    p = os.path.expanduser(p)
    return p if os.path.isabs(p) else os.path.join(ROOT, p)


P = "mixamorig:"


def load_figure():
    bpy.ops.wm.read_factory_settings(use_empty=True)
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
    s = CFG["height"] / (co[:, 2].max() - co[:, 2].min())
    o.scale = (s, s, s)
    bpy.ops.object.transform_apply(scale=True)
    co = np.array([v.co[:] for v in o.data.vertices])
    o.location = (-(co[:, 0].max() + co[:, 0].min()) / 2, -(co[:, 1].max() + co[:, 1].min()) / 2, -co[:, 2].min())
    bpy.ops.object.transform_apply(location=True)
    # One skin: the GLB is split at its texture seams.
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=0.00001)
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    o.name = CFG["name"]
    return o


def joints_3d(o):
    """Each joint's front-view position, given its depth: the middle of the figure's surface in front of and behind it."""
    co = np.array([v.co[:] for v in o.data.vertices])
    J = CFG["joints"]
    out = {}

    def depth(x, z, r=0.05):
        for rr in (r, r * 2, r * 4):
            sel = co[(np.abs(co[:, 0] - x) < rr) & (np.abs(co[:, 2] - z) < rr)]
            if len(sel) > 5:
                return (sel[:, 1].min() + sel[:, 1].max()) / 2
        return 0.0
    for k, v in J.items():
        out[k] = V((v[0], depth(v[0], v[1]), v[1]))
    return out


def build_armature(o, j):
    arm_data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_data.edit_bones
    up = V((0, 0, 1))
    fwd = V((0, -1, 0))
    hips, neck, head, top = j["hips"], j["neck"], j["head"], j["head_top"]
    spine = hips.lerp(neck, 0.22)
    spine1 = hips.lerp(neck, 0.47)
    spine2 = hips.lerp(neck, 0.72)
    bones = {}

    def bone(name, h, t, parent=None, roll_up=None):
        b = eb.new(P + name)
        b.head, b.tail = h, t
        if (t - h).length < 1e-4:
            b.tail = h + up * 0.03
        if parent:
            b.parent = eb[P + parent]
        b.align_roll(roll_up or fwd)
        bones[name] = b
        return b
    bone("Hips", hips, spine)
    bone("Spine", spine, spine1, "Hips")
    bone("Spine1", spine1, spine2, "Spine")
    bone("Spine2", spine2, neck, "Spine1")
    bone("Neck", neck, head, "Spine2")
    bone("Head", head, top, "Neck")
    bone("HeadTop_End", top, top + up * 0.08, "Head")
    for side, s in (("Left", "L"), ("Right", "R")):
        arm_j, elbow, wrist, hand = j["arm_" + s], j["elbow_" + s], j["wrist_" + s], j["hand_" + s]
        clav = V((neck.x + (arm_j.x - neck.x) * 0.25, neck.y, neck.z - 0.03))
        bone(side + "Shoulder", clav, arm_j, "Spine2", up)
        bone(side + "Arm", arm_j, elbow, side + "Shoulder", up)
        bone(side + "ForeArm", elbow, wrist, side + "Arm", up)
        bone(side + "Hand", wrist, wrist.lerp(hand, 0.45), side + "ForeArm", up)
        along = (hand - wrist).normalized()
        knuckle = wrist.lerp(hand, 0.45)
        bone(side + "HandIndex1", knuckle, hand, side + "Hand", up)
        thumb = wrist + along * 0.03 + fwd * 0.03
        bone(side + "HandThumb1", thumb, thumb + along * 0.03 + fwd * 0.02, side + "Hand", up)
        upleg, knee, ankle = j["upleg_" + s], j["knee_" + s], j["ankle_" + s]
        bone(side + "UpLeg", upleg, knee, "Hips")
        bone(side + "Leg", knee, ankle, side + "UpLeg")
        ball = V((ankle.x, ankle.y - CFG.get("foot", 0.12), 0.025))
        bone(side + "Foot", ankle, ball, side + "Leg", up)
        bone(side + "ToeBase", ball, ball + fwd * 0.07, side + "Foot", up)
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def bind(o, arm):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    unweighted = sum(1 for v in o.data.vertices if not any(g.weight > 1e-3 for g in v.groups))
    print("REPORT bound: %d of %d vertices without weight" % (unweighted, len(o.data.vertices)))
    if unweighted:
        # Vertices the automatic weights missed (bone heat fails on parts of a generated mesh: 211 on the Rotcap) take
        # the weights of the nearest vertex that has some. A smoothed copy bound instead lost every weight.
        from mathutils import kdtree
        vs = o.data.vertices
        weighted = [v for v in vs if any(g.weight > 1e-3 for g in v.groups)]
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
        unweighted = sum(1 for v in vs if not any(g.weight > 1e-3 for g in v.groups))
        print("REPORT filled from neighbours: %d without weight" % unweighted)


def export(objects, file, animated):
    bpy.ops.object.select_all(action="DESELECT")
    for x in objects:
        x.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=file, use_selection=True, add_leaf_bones=False, bake_anim=animated,
                             bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="STRIP", embed_textures=False)
    print("REPORT wrote", os.path.relpath(file, ROOT))


# The T-pose each Mixamo family of clips was made on: a clip file without skin rests in its own first frame, so the
# turns are measured from the family's real T-pose instead (2026-11-03: measured from the clip's own rest, a fighting
# idle came out as almost nothing, the arms down and the body square). The longbow pack's files (bow_*, walk) are on
# the Wild Arrow's skeleton, the rest on the first Cutthroat's.
TPOSES = {"bow": "ArtSource/models/mixamo_clips/tpose_bow.fbx", "cutthroat": "ArtSource/models/mixamo_clips/tpose_cutthroat.fbx"}


def family(src_file):
    name = os.path.basename(src_file)
    return "bow" if name.startswith(("bow_", "walk")) else "cutthroat"


_tpose_cache = {}


def tpose(fam):
    """Each bone's world rotation and the hips' world position in the family's T-pose (the T-pose file's posed frame)."""
    if fam in _tpose_cache:
        return _tpose_cache[fam]
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=path(TPOSES[fam]))
    new = [x for x in bpy.context.scene.objects if x not in before]
    ref = [x for x in new if x.type == "ARMATURE"][0]
    if ref.animation_data and ref.animation_data.action:
        bpy.context.scene.frame_set(int(ref.animation_data.action.frame_range[0]))
    bpy.context.view_layer.update()
    rot = {pb.name: (ref.matrix_world @ pb.matrix).to_3x3().normalized() for pb in ref.pose.bones}
    dirs = {pb.name: (ref.matrix_world.to_3x3() @ (pb.tail - pb.head)).normalized() for pb in ref.pose.bones}
    hips = ref.matrix_world @ ref.pose.bones[P + "Hips"].head
    for x in new:
        bpy.data.objects.remove(x, do_unlink=True)
    _tpose_cache[fam] = (rot, dirs, hips)
    return _tpose_cache[fam]


def retarget(arm, src_file, loop):
    t_rot, t_dir, t_hips = tpose(family(src_file))
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=path(src_file))
    new = [x for x in bpy.context.scene.objects if x not in before]
    src = [x for x in new if x.type == "ARMATURE"][0]
    for x in new:
        if x.type == "MESH":
            bpy.data.objects.remove(x, do_unlink=True)
    action = src.animation_data.action
    start, end = int(action.frame_range[0]), int(action.frame_range[1])
    sw, ow = src.matrix_world, arm.matrix_world
    ow_inv = ow.inverted()

    def src_rest(name):
        return t_rot[name]

    # Per bone: the turn taking our rest direction onto the family's T-pose direction (world space).
    corr = {}
    for pb in arm.pose.bones:
        if pb.name not in t_rot or src.pose.bones.get(pb.name) is None:
            continue
        ours = (ow.to_3x3() @ (pb.bone.tail_local - pb.bone.head_local)).normalized()
        corr[pb.name] = ours.rotation_difference(t_dir[pb.name]).to_matrix()
    rest_hips_src = t_hips
    scale = (ow @ arm.data.bones[P + "Hips"].head_local).z / max(1e-6, rest_hips_src.z)
    bpy.context.scene.frame_set(start)
    first = sw @ src.pose.bones[P + "Hips"].head
    bpy.context.scene.frame_set(end)
    last = sw @ src.pose.bones[P + "Hips"].head
    step = (last - first) / max(1, end - start)
    step.z = 0

    new_action = bpy.data.actions.new(os.path.splitext(os.path.basename(src_file))[0])
    arm.animation_data_create()
    arm.animation_data.action = new_action
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    order = []

    def walk(b):
        order.append(b)
        for c in b.children:
            walk(c)
    walk(arm.pose.bones[P + "Hips"])
    for f in range(start, end + 1):
        bpy.context.scene.frame_set(f)
        for pb in arm.pose.bones:
            pb.matrix_basis = mathutils.Matrix.Identity(4)
        bpy.context.view_layer.update()
        for pb in order:
            sp = src.pose.bones.get(pb.name)
            if sp is None or pb.name not in corr:
                continue
            ws_now = (sw @ sp.matrix).to_3x3().normalized()
            wo_rest = (ow @ pb.bone.matrix_local).to_3x3()
            target = ws_now @ src_rest(pb.name).inverted() @ corr[pb.name] @ wo_rest
            if pb.parent is None:
                now = sw @ sp.head
                if loop:
                    now = now - step * (f - start)
                head_world = ow @ pb.bone.head_local + (now - rest_hips_src) * scale
                head = ow_inv @ head_world
            else:
                head = (pb.parent.matrix @ pb.parent.bone.matrix_local.inverted() @ pb.bone.matrix_local).to_translation()
            pb.matrix = mathutils.Matrix.LocRotScale(head, (ow_inv.to_3x3() @ target).to_quaternion(), None)
            bpy.context.view_layer.update()
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f)
            if pb.parent is None:
                pb.keyframe_insert("location", frame=f)
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = start, end
    bpy.data.objects.remove(src, do_unlink=True)
    return new_action


def main():
    out = path(CFG["out"])
    os.makedirs(out, exist_ok=True)
    o = load_figure()
    j = joints_3d(o)
    # Centred on the hips: a weapon held out to one side (the Woodcutter's axe) put the body off the middle, and the bake
    # turns the figure about the origin.
    shift = V((-j["hips"].x, -j["hips"].y, 0))
    for v in o.data.vertices:
        v.co += shift
    j = {k: p + shift for k, p in j.items()}
    arm = build_armature(o, j)
    bind(o, arm)
    # The figure's material: its own texture, which Unity puts back from <name>_albedo.png (MixamoImport.ApplyTexture).
    export([arm, o], os.path.join(out, CFG["name"] + ".fbx"), False)
    import shutil
    shutil.copyfile(path(CFG["albedo"]), os.path.join(out, CFG["name"] + "_albedo.png"))
    o.hide_set(True)
    for clip, (src_file, loop) in CFG.get("clips", {}).items():
        act = retarget(arm, src_file, loop)
        export([arm], os.path.join(out, "%s_%s.fbx" % (CFG["name"], clip)), True)
        arm.animation_data.action = None
        for pb in arm.pose.bones:
            pb.matrix_basis = mathutils.Matrix.Identity(4)
    print("REPORT done", CFG["name"])


main()
