# Blender script: turn an image-to-3D weapon GLB into an FBX ready to hold. The weapon's long axis must be Blender's
# up (z) with the head at the top, as image-to-3D gives it standing. It is scaled so it is <length> metres long and
# moved so the point <grip> of the way up from the butt, on the haft's centre line, is the origin: where a fist holds it.
# Usage: Blender -b -P prepare_weapon.py -- <input.glb> <output.fbx> <length_m> <grip_fraction>
import bpy, sys, mathutils

argv = sys.argv[sys.argv.index("--") + 1:]
source, target, length, grip = argv[0], argv[1], float(argv[2]), float(argv[3])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
for o in meshes:
    world = o.matrix_world.copy()
    o.parent = None
    o.matrix_world = world
for o in list(bpy.context.scene.objects):
    if o.type == "EMPTY":
        bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def verts():
    for o in meshes:
        for v in o.data.vertices:
            yield o.matrix_world @ v.co

zs = [v.z for v in verts()]
scale = length / (max(zs) - min(zs))
for o in meshes:
    o.scale = (scale, scale, scale)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

all_v = list(verts())
bottom, top = min(v.z for v in all_v), max(v.z for v in all_v)
# The haft's centre line: the middle of the vertices in the lowest fifth (the grip and butt, below the head).
low = [v for v in all_v if v.z < bottom + (top - bottom) * 0.2]
cx = sum(v.x for v in low) / len(low)
cy = sum(v.y for v in low) / len(low)
origin = mathutils.Vector((cx, cy, bottom + (top - bottom) * grip))
for o in meshes:
    o.location -= origin
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
print(f"REPORT length {length} m, grip at {grip} from the butt, haft centre ({cx:.4f}, {cy:.4f})")

bpy.ops.export_scene.fbx(filepath=target, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                         path_mode="COPY", embed_textures=True, axis_forward="-Z", axis_up="Y", add_leaf_bones=False)
print(f"REPORT wrote {target}")
