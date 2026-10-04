# The menus' ornate frame, modelled and rendered in Blender (the owner, 2026-10-04: the menus "still look very bad"):
# a dark forged-iron band with a gold bead along its inner edge and a gold boss with a garnet in each corner, seen
# straight on, written to Assets/_Project/Resources/UI/frame.png for 9-slicing (border 40 of 256; the corners hold the
# ornaments, the edges are plain so they stretch). Colours are baked in: the game tints it white, or toward a rarity.
#   Blender -b -P ArtSource/tools/ui/render_frame.py
import os, math
import bpy, bmesh

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "UI")
SIZE = 256
BAND = 40 / SIZE * 2.0     # the 9-slice border in scene units (the frame spans -1..1)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.film_transparent = True
scene.view_settings.view_transform = "Standard"
scene.render.resolution_x = scene.render.resolution_y = SIZE
try:
    scene.eevee.taa_render_samples = 32
except AttributeError:
    pass
world = bpy.data.worlds.new("W")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes["Background"]
bg.inputs["Color"].default_value = (0.35, 0.3, 0.26, 1)
bg.inputs["Strength"].default_value = 0.8


def material(name, color, metal, rough):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = color
    p.inputs["Metallic"].default_value = metal
    p.inputs["Roughness"].default_value = rough
    noise = m.node_tree.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 60
    bump = m.node_tree.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.25
    m.node_tree.links.new(noise.outputs["Fac"], bump.inputs["Height"])
    m.node_tree.links.new(bump.outputs["Normal"], p.inputs["Normal"])
    return m


iron = material("Iron", (0.035, 0.03, 0.028, 1), 0.35, 0.6)
gold = material("Gold", (0.85, 0.62, 0.26, 1), 1.0, 0.3)
garnet = material("Garnet", (0.5, 0.03, 0.03, 1), 0.0, 0.15)


def ring(outer, inner, height, mat, bevel):
    """A square ring between half sizes outer and inner, raised height, bevelled."""
    bm = bmesh.new()
    o, i = outer, inner
    pts = [(-o, -o), (o, -o), (o, o), (-o, o), (-i, -i), (i, -i), (i, i), (-i, i)]
    bottom = [bm.verts.new((x, y, 0)) for x, y in pts]
    top = [bm.verts.new((x, y, height)) for x, y in pts]
    for a, b in ((0, 1), (1, 2), (2, 3), (3, 0)):
        bm.faces.new((top[a], top[b], top[b + 4], top[a + 4]))
        bm.faces.new((bottom[a], bottom[a + 4], bottom[b + 4], bottom[b]))
        bm.faces.new((bottom[a], bottom[b], top[b], top[a]))
        bm.faces.new((bottom[a + 4], top[a + 4], top[b + 4], bottom[b + 4]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bmesh.ops.bevel(bm, geom=[e for e in bm.edges if all(v.co.z > height * 0.5 for v in e.verts)], offset=bevel, segments=3, affect="EDGES")
    mesh = bpy.data.meshes.new("ring")
    bm.to_mesh(mesh)
    obj = bpy.data.objects.new("ring", mesh)
    scene.collection.objects.link(obj)
    obj.data.materials.append(mat)
    for p in mesh.polygons:
        p.use_smooth = True
    return obj


# The iron band, a gold bead along its inside and a thin gold line along its outside.
ring(1.0, 1.0 - BAND * 0.92, 0.05, iron, 0.02)
ring(1.0 - BAND * 0.78, 1.0 - BAND * 0.95, 0.07, gold, 0.012)
ring(1.0 - BAND * 0.12, 1.0 - BAND * 0.2, 0.06, gold, 0.006)
# Corner bosses: a gold pyramid with a garnet, filling the corner squares.
for sx in (-1, 1):
    for sy in (-1, 1):
        cx, cy = sx * (1.0 - BAND * 0.47), sy * (1.0 - BAND * 0.47)
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=BAND * 0.62, radius2=BAND * 0.18, depth=0.09, location=(cx, cy, 0.05))
        boss = bpy.context.active_object
        boss.rotation_euler = (0, 0, math.radians(45))
        boss.data.materials.append(gold)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=BAND * 0.17, location=(cx, cy, 0.1), segments=16, ring_count=8)
        gem = bpy.context.active_object
        gem.scale = (1, 1, 0.6)
        gem.data.materials.append(garnet)
        bpy.ops.object.shade_smooth()

# Light from the upper left, a soft fill.
key = bpy.data.objects.new("Key", bpy.data.lights.new("Key", "SUN"))
key.data.energy = 4.0
key.rotation_euler = (math.radians(40), math.radians(-30), math.radians(-30))
scene.collection.objects.link(key)
fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "SUN"))
fill.data.energy = 1.0
fill.rotation_euler = (math.radians(-35), math.radians(30), 0)
scene.collection.objects.link(fill)

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.type = "ORTHO"
cam.data.ortho_scale = 2.0
cam.location = (0, 0, 5)
scene.collection.objects.link(cam)
scene.camera = cam
os.makedirs(OUT, exist_ok=True)
scene.render.filepath = os.path.join(OUT, "frame.png")
bpy.ops.render.render(write_still=True)
print("REPORT frame")
