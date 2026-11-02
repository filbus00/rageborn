# Puts the Pro Longbow Pack's Mixamo clips on her own skeleton (2026-11-02, the owner: "Har feet are pointing inward").
# The pack's files carry a skeleton of their own whose default pose is the idle stance (feet wide, toes out); Unity's
# humanoid retargeting read that stance as neutral, so on her T-posed body every clip closed her knees and turned her
# toes in by 15 to 25 degrees. Here each bone of hers takes the pack bone's turn from its rest, frame by frame, in
# Blender (her lengths kept, the hips' travel scaled to her height), on her skeleton made to rest in her Mixamo T-pose
# exactly as wild_arrow_gear.py exports her bodies; the clips then play in Unity through her own avatar.
#   Blender -b -P ArtSource/tools/retarget_pack.py -- <pack folder>
# Writes Assets/_Project/Art/Models/WildArrow/wild_arrow_<name>.fbx for each clip below, and her skeleton at rest,
# Assets/_Project/Art/Models/Rigs/wild_arrow_rig/wild_arrow_rig.fbx. Unity imports them as generic clips
# (WildArrowBakeSetup.OwnClips): through a humanoid avatar her feet still came out straight, the clips' leg twist lost.
# A loop keeps its hips over one spot (its travel taken out evenly, its sway kept), as the bake expects of a loop.
import bpy, sys, os, mathutils

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FOLDER = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "WildArrow")
HER = os.path.join(FOLDER, "wild_arrow.fbx")
RIG = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Rigs", "wild_arrow_rig", "wild_arrow_rig.fbx")
CLIPS = {
    "idle": "standing idle 01", "run": "standing run forward", "run_back": "standing run back",
    "walk": "standing walk forward", "attack": "standing draw arrow", "overdraw": "standing aim overdraw",
    "recoil": "standing aim recoil", "hit": "standing react small from front", "death": "standing death backward 01",
    "dive": "standing dive forward", "walk_aim_forward": "standing aim walk forward",
    "walk_aim_back": "standing aim walk back", "walk_aim_left": "standing aim walk left",
    "walk_aim_right": "standing aim walk right",
}
LOOPS = {"idle", "run", "run_back", "walk", "walk_aim_forward", "walk_aim_back", "walk_aim_left", "walk_aim_right"}


def her_skeleton():
    """Her armature alone, resting in the T-pose her Mixamo file holds (as the bodies are exported)."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=HER)
    arm = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
    for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.context.view_layer.update()
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.animation_data_clear()
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.matrix_basis = mathutils.Matrix.Identity(4)
    return arm


def head_height(arm, matrices=False):
    return (arm.matrix_world @ arm.data.bones["mixamorig:Hips"].head_local).z


def retarget(her, pack, frame, scale, travel=None):
    """Poses her at a frame of the pack's clip: each bone turned as the pack's turned from its rest, in world space."""
    bpy.context.scene.frame_set(frame)
    for pb in her.pose.bones:
        pb.matrix_basis = mathutils.Matrix.Identity(4)
    bpy.context.view_layer.update()

    def walk(pb):
        src = pack.pose.bones.get(pb.name)
        if src is not None:
            delta = (pack.matrix_world @ src.matrix) @ (pack.matrix_world @ src.bone.matrix_local).inverted()
            turned = her.matrix_world.inverted() @ delta @ her.matrix_world @ pb.bone.matrix_local
            if pb.parent is None:
                # The hips travel as the pack's did, scaled to her height.
                rest = pack.matrix_world @ src.bone.matrix_local.to_translation()
                now = pack.matrix_world @ src.matrix.to_translation()
                if travel is not None:
                    now = now - travel(frame)
                at = her.matrix_world @ pb.bone.matrix_local.to_translation() + (now - rest) * scale
                head = her.matrix_world.inverted() @ at
            else:
                head = (pb.parent.matrix @ pb.parent.bone.matrix_local.inverted() @ pb.bone.matrix_local).to_translation()
            pb.matrix = mathutils.Matrix.LocRotScale(head, turned.to_quaternion(), None)
            bpy.context.view_layer.update()
        for child in pb.children:
            walk(child)
    walk(her.pose.bones["mixamorig:Hips"])


def export(arm, path, animated):
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, add_leaf_bones=False, bake_anim=animated,
                             bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_simplify_factor=0.0, apply_scale_options="FBX_SCALE_ALL", path_mode="STRIP")
    print("REPORT wrote", os.path.relpath(path, ROOT))


def main():
    pack_folder = sys.argv[sys.argv.index("--") + 1]
    her = her_skeleton()
    export(her, RIG, False)
    for name, source in CLIPS.items():
        her = her_skeleton()
        her_hips = head_height(her)
        before = set(bpy.context.scene.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(pack_folder, source + ".fbx"))
        pack = [o for o in bpy.context.scene.objects if o.type == "ARMATURE" and o not in before][0]
        action = pack.animation_data.action
        start, end = int(action.frame_range[0]), int(action.frame_range[1])
        bpy.context.scene.frame_set(start)
        scale = her_hips / max(1e-6, (pack.matrix_world @ pack.data.bones["mixamorig:Hips"].head_local).z)
        travel = None
        if name in LOOPS:
            # The hips' travel over the loop, taken out evenly (on the ground only): a run in place.
            def hips_at(f):
                bpy.context.scene.frame_set(f)
                return pack.matrix_world @ pack.pose.bones["mixamorig:Hips"].matrix.to_translation()
            first, last = hips_at(start), hips_at(end)
            step = (last - first) / max(1, end - start)
            step.z = 0.0
            travel = lambda f: step * (f - start)
        her.animation_data_create()
        her.animation_data.action = bpy.data.actions.new(name)
        for frame in range(start, end + 1):
            retarget(her, pack, frame, scale, travel)
            for pb in her.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=frame)
                if pb.parent is None:
                    pb.keyframe_insert("location", frame=frame)
        bpy.context.scene.frame_start, bpy.context.scene.frame_end = start, end
        bpy.context.scene.render.fps = int(round(bpy.context.scene.render.fps))
        for o in [o for o in bpy.context.scene.objects if o not in (her,)]:
            bpy.data.objects.remove(o, do_unlink=True)
        export(her, os.path.join(FOLDER, "wild_arrow_%s.fbx" % name), True)


if __name__ == "__main__":
    main()
