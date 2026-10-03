# Blender script: turn a Meshy FBX or OBJ (textures in separate files) into one FBX Mixamo accepts and shows right.
# Keeps only the base colour texture on a plain, non-metallic material (Meshy's metallic, roughness and normal maps made
# the model look dark and blotchy in Mixamo's viewer), points every face's normal outward, scales the model to a
# height in metres, stands its feet on the ground at the origin, centred, and exports FBX with the texture embedded.
# Usage: Blender -b -P meshy_to_mixamo.py -- <input.fbx|.obj> <base_colour.png> <output.fbx> <height_metres>
import bpy, sys, mathutils

argv = sys.argv[sys.argv.index("--") + 1:]
source, texture, target, height = argv[0], argv[1], argv[2], float(argv[3])

bpy.ops.wm.read_factory_settings(use_empty=True)
if source.lower().endswith(".obj"):
    bpy.ops.wm.obj_import(filepath=source)
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

# Normals outward, and no custom split normals (they carry the import's shading errors).
for o in meshes:
    bpy.context.view_layer.objects.active = o
    if o.data.has_custom_normals:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    for p in o.data.polygons:
        p.use_smooth = True

# One plain material: base colour only.
material = bpy.data.materials.new("Body")
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
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
lo, hi = bounds()
offset = mathutils.Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))
for o in meshes:
    o.location += offset
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
lo, hi = bounds()
faces = sum(len(o.data.polygons) for o in meshes)
print(f"REPORT {len(meshes)} mesh(es), {faces} faces, size {hi.x - lo.x:.3f} x {hi.y - lo.y:.3f} x {hi.z - lo.z:.3f} m")

bpy.ops.export_scene.fbx(filepath=target, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                         path_mode="COPY", embed_textures=True, axis_forward="-Z", axis_up="Y", add_leaf_bones=False)
print(f"REPORT wrote {target}")
