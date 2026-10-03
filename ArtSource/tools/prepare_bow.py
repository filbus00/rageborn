# Blender script: turn a Meshy (or image-to-3D) bow, FBX, OBJ or GLB, standing up along Blender's z, into an FBX the
# sprite bake can hold (SpriteBakeJob.Piece.autoGrip): <length> metres tall, the middle of the handle at the origin,
# the limbs along +Y in Unity and the bow's back (the side the limbs curve away from, toward the target) facing +Z.
# Meshy's metallic, roughness and normal maps are dropped; the base colour texture goes beside the FBX as
# <output>_albedo.png, which the bake puts on the bow (MixamoImport.ApplyTexture).
# Usage: Blender -b -P prepare_bow.py -- <input> <base_colour.png> <output.fbx> <length_m>
import bpy, sys, math, mathutils, shutil

argv = sys.argv[sys.argv.index("--") + 1:]
source, texture, target, length = argv[0], argv[1], argv[2], float(argv[3])

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
middle = (bottom + top) / 2
band = (top - bottom) * 0.04
handle = [v for v in all_v if abs(v.z - middle) < band]
tips = [v for v in all_v if v.z > top - band or v.z < bottom + band]
hx = sum(v.x for v in handle) / len(handle)
hy = sum(v.y for v in handle) / len(handle)
tx = sum(v.x for v in tips) / len(tips)
ty = sum(v.y for v in tips) / len(tips)

# The handle stands out from the tips toward the bow's back. Turn about z so that way is Blender's -y, which the export
# (forward -Z, up Y) makes Unity's +Z.
back = mathutils.Vector((hx - tx, hy - ty))
angle = math.atan2(back.y, back.x)
turn = -math.pi / 2 - angle
for o in meshes:
    o.location -= mathutils.Vector((hx, hy, middle))
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
for o in meshes:
    o.rotation_euler = (0.0, 0.0, turn)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

# One plain material with the base colour only.
material = bpy.data.materials.new("Bow")
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

print(f"REPORT length {length} m, handle at ({hx:.3f}, {hy:.3f}), back {back.length:.3f} m deep, turned {math.degrees(turn):.0f} degrees")
bpy.ops.export_scene.fbx(filepath=target, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                         path_mode="COPY", embed_textures=True, axis_forward="-Z", axis_up="Y", add_leaf_bones=False)
shutil.copyfile(texture, target.replace(".fbx", "_albedo.png"))
print(f"REPORT wrote {target}")
