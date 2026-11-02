# Paints a Hunyuan3D shape with its Gemini reference (2026-11-02, the forest's new enemies): the local Hunyuan3D-2mini
# (~/AI/Hunyuan3D-2) makes only the shape, its texture step needing an Nvidia GPU. The picture (its background removed,
# <name>_cut.png beside the shape) is laid onto the shape straight from the front: the shape's width and height match
# the figure's outline in the picture, every point takes the colour in front of it (the back of a hood the hood's, the
# back of a leg the leg's), and the colours run on past the outline so the sides carry no background. Baked into a
# texture over a fresh unwrap and written as a GLB with the texture inside, ready for meshy_to_mixamo.py.
#   Blender -b -P ArtSource/tools/project_texture.py -- <shape.glb> <cut.png> <out.glb> [texture size]
# The shape faces -Y in Blender (Hunyuan's front), z up.
import bpy, bmesh, sys, os
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
shape, picture, out = args[0], args[1], args[2]
size = int(args[3]) if len(args) > 3 else 2048

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.abspath(shape))
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
for o in bpy.context.scene.objects:
    o.select_set(o in meshes)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
co = np.array([v.co[:] for v in obj.data.vertices])
x0, x1 = co[:, 0].min(), co[:, 0].max()
z0, z1 = co[:, 2].min(), co[:, 2].max()

# The picture: its figure's outline, and its colours run on past the outline (each empty pixel takes the average of
# its filled neighbours, ring by ring), so a point just outside it in the projection still gets the figure's colour.
img = bpy.data.images.load(os.path.abspath(picture))
w, h = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
alpha = px[..., 3] > 0.5
ys, xs = np.nonzero(alpha)
bx0, bx1, by0, by1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
rgb = px[..., :3] * alpha[..., None]
filled = alpha.astype(np.float32)
for _ in range(48):
    acc = np.zeros_like(rgb)
    cnt = np.zeros_like(filled)
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
        acc += np.roll(np.roll(rgb * filled[..., None], dy, 0), dx, 1)
        cnt += np.roll(np.roll(filled, dy, 0), dx, 1)
    grow = (filled == 0) & (cnt > 0)
    rgb[grow] = acc[grow] / cnt[grow][:, None]
    filled[grow] = 1.0
    if filled.all():
        break
spread = bpy.data.images.new("spread", w, h, alpha=False)
spread.pixels = np.concatenate([rgb, np.ones((h, w, 1), np.float32)], axis=2).ravel()
# A blurred copy for the surfaces facing away: the back of a hood took the mask's eyes, the back of a coat its buckles.
# Blurred, the back keeps each part's colour without the front's details.
blur = rgb.copy()
radius = max(4, w // 90)
for axis in (0, 1):
    c = np.cumsum(np.pad(blur, [(radius + 1, radius) if a == axis else (0, 0) for a in range(3)], mode="edge"), axis=axis)
    blur = (np.take(c, range(2 * radius + 1, c.shape[axis]), axis=axis) - np.take(c, range(0, c.shape[axis] - 2 * radius - 1), axis=axis)) / (2 * radius + 1)
behind = bpy.data.images.new("behind", w, h, alpha=False)
behind.pixels = np.concatenate([blur, np.ones((h, w, 1), np.float32)], axis=2).ravel()

# The generated mesh comes with its triangles split apart and some wound inward (Mixamo showed them as black
# triangles all over the Cutthroat): joined, every face turned outward, and shaded smooth.
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.remove_doubles(threshold=0.0001)
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.object.shade_smooth()

# A fresh unwrap for the baked texture.
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=0.004)
bpy.ops.object.mode_set(mode="OBJECT")

# A material that shows the picture projected from the front: u from x, v from z, mapped onto the figure's outline.
mat = bpy.data.materials.new("Projected")
mat.use_nodes = True
nt = mat.node_tree
nt.nodes.clear()
coord = nt.nodes.new("ShaderNodeTexCoord")
sep = nt.nodes.new("ShaderNodeSeparateXYZ")
nt.links.new(coord.outputs["Object"], sep.inputs[0])


def affine(socket, scale, offset):
    m = nt.nodes.new("ShaderNodeMath"); m.operation = "MULTIPLY_ADD"
    nt.links.new(socket, m.inputs[0]); m.inputs[1].default_value = scale; m.inputs[2].default_value = offset
    return m.outputs[0]


# Pixel x = bx0 + (x - x0) / (x1 - x0) * (bx1 - bx0); u = pixel x / w. Blender's image rows run from the bottom, and so
# does z, so v follows the same way.
su = (bx1 - bx0) / (x1 - x0) / w
u = affine(sep.outputs["X"], su, bx0 / w - x0 * su)
sv = (by1 - by0) / (z1 - z0) / h
v = affine(sep.outputs["Z"], sv, by0 / h - z0 * sv)
comb = nt.nodes.new("ShaderNodeCombineXYZ")
nt.links.new(u, comb.inputs[0]); nt.links.new(v, comb.inputs[1])
tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = spread; tex.extension = "EXTEND"
nt.links.new(comb.outputs[0], tex.inputs[0])
back = nt.nodes.new("ShaderNodeTexImage"); back.image = behind; back.extension = "EXTEND"
nt.links.new(comb.outputs[0], back.inputs[0])
# How much a point faces away from the picture (+Y, the shape's back): 0 facing the front or the sides, 1 facing back.
geo = nt.nodes.new("ShaderNodeNewGeometry")
nsep = nt.nodes.new("ShaderNodeSeparateXYZ")
nt.links.new(geo.outputs["Normal"], nsep.inputs[0])
ramp = nt.nodes.new("ShaderNodeMapRange")
ramp.inputs["From Min"].default_value = 0.05
ramp.inputs["From Max"].default_value = 0.45
nt.links.new(nsep.outputs["Y"], ramp.inputs["Value"])
mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"
nt.links.new(ramp.outputs["Result"], mix.inputs["Factor"])
nt.links.new(tex.outputs["Color"], mix.inputs[6])
nt.links.new(back.outputs["Color"], mix.inputs[7])
emit = nt.nodes.new("ShaderNodeEmission")
nt.links.new(mix.outputs[2], emit.inputs[0])
outn = nt.nodes.new("ShaderNodeOutputMaterial")
nt.links.new(emit.outputs[0], outn.inputs[0])
baked = bpy.data.images.new(os.path.splitext(os.path.basename(out))[0] + "_albedo", size, size, alpha=False)
target = nt.nodes.new("ShaderNodeTexImage"); target.image = baked
nt.nodes.active = target
obj.data.materials.clear()
obj.data.materials.append(mat)

sc = bpy.context.scene
sc.render.engine = "CYCLES"
sc.cycles.samples = 1
sc.cycles.device = "CPU"
sc.render.bake.margin = 8
bpy.ops.object.bake(type="EMIT")

# The baked texture on a plain material, written into the GLB.
nt.nodes.clear()
bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
img_node = nt.nodes.new("ShaderNodeTexImage"); img_node.image = baked
nt.links.new(img_node.outputs["Color"], bsdf.inputs["Base Color"])
outn = nt.nodes.new("ShaderNodeOutputMaterial")
nt.links.new(bsdf.outputs[0], outn.inputs[0])
baked.filepath_raw = os.path.abspath(out).replace(".glb", "_albedo.png")
baked.file_format = "PNG"
baked.save()
bpy.ops.export_scene.gltf(filepath=os.path.abspath(out), use_selection=False, export_image_format="AUTO")
print("REPORT wrote", out, "and", baked.filepath_raw)
