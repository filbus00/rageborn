# Blender script: turn a Meshy (or image-to-3D) quiver, FBX, OBJ or GLB, standing up along Blender's z with the arrows
# at the top, into an FBX the sprite bake hangs on the back (WildArrowBakeSetup: on the upper chest bone, in metres):
# <height> metres tall with the arrows, its centre at the origin, upright along +Y in Unity. Meshy's metallic, roughness
# and normal maps are dropped; the base colour texture goes beside the FBX as <output>_albedo.png.
# Usage: Blender -b -P prepare_quiver.py -- <input> <base_colour.png> <output.fbx> <height_m>
import bpy, sys, mathutils, shutil

argv = sys.argv[sys.argv.index("--") + 1:]
source, texture, target, height = argv[0], argv[1], argv[2], float(argv[3])

bpy.ops.wm.read_factory_settings(use_empty=True)
lower = source.lower()
if lower.endswith(".obj"):
    bpy.ops.wm.obj_import(filepath=source)
elif lower.endswith(".glb") or lower.endswith(".gltf"):
    bpy.ops.import_scene.gltf(filepath=source)
else:
    bpy.ops.import_scene.fbx(filepath=source)

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
for o in meshes:
    world = o.matrix_world.copy()
    o.parent = None
    o.matrix_world = world
for o in list(bpy.context.scene.objects):
    if o.type != "MESH":
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
scale = height / (hi.z - lo.z)
for o in meshes:
    o.scale = (scale, scale, scale)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
lo, hi = bounds()
for o in meshes:
    o.location -= (lo + hi) / 2
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

material = bpy.data.materials.new("Quiver")
material.use_nodes = True
nodes = material.node_tree.nodes
bsdf = nodes["Principled BSDF"]
bsdf.inputs["Metallic"].default_value = 0.0
bsdf.inputs["Roughness"].default_value = 0.9
image = nodes.new("ShaderNodeTexImage")
image.image = bpy.data.images.load(texture)
material.node_tree.links.new(image.outputs["Color"], bsdf.inputs["Base Color"])
for o in meshes:
    o.data.materials.clear()
    o.data.materials.append(material)

lo, hi = bounds()
print(f"REPORT size {hi.x - lo.x:.3f} x {hi.y - lo.y:.3f} x {hi.z - lo.z:.3f} m")
bpy.ops.export_scene.fbx(filepath=target, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                         path_mode="COPY", embed_textures=True, axis_forward="-Z", axis_up="Y", add_leaf_bones=False)
shutil.copyfile(texture, target.replace(".fbx", "_albedo.png"))
print(f"REPORT wrote {target}")
