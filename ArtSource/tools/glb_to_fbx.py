# Blender script: turn an image-to-3D GLB into an FBX Mixamo (or Unity) accepts. Removes the importer's empty parent,
# scales the model to a height in metres (0 keeps its size), stands its feet on the ground at the origin, centred,
# facing front, and exports FBX with the textures embedded.
# Usage: Blender -b -P glb_to_fbx.py -- <input.glb> <output.fbx> <height_metres>
import bpy, sys, mathutils

argv = sys.argv[sys.argv.index("--") + 1:]
source, target, height = argv[0], argv[1], float(argv[2])

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

def bounds():
    lo = mathutils.Vector((1e9, 1e9, 1e9)); hi = -lo
    for o in meshes:
        for v in o.data.vertices:
            w = o.matrix_world @ v.co
            lo = mathutils.Vector(map(min, lo, w)); hi = mathutils.Vector(map(max, hi, w))
    return lo, hi

lo, hi = bounds()
scale = height / (hi.z - lo.z) if height > 0 else 1.0
for o in meshes:
    o.scale = (scale, scale, scale)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
lo, hi = bounds()
offset = mathutils.Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))
for o in meshes:
    o.location += offset
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
lo, hi = bounds()
print(f"REPORT size {hi.x - lo.x:.3f} x {hi.y - lo.y:.3f} x {hi.z - lo.z:.3f} m, feet at {lo.z:.3f}")

bpy.ops.export_scene.fbx(filepath=target, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                         path_mode="COPY", embed_textures=True, axis_forward="-Z", axis_up="Y", add_leaf_bones=False)
print(f"REPORT wrote {target}")
