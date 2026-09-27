# Blender script: import a GLB, report its meshes, size and textures, and render preview views.
# Usage: Blender -b -P inspect_and_preview.py -- <input.glb> <output_prefix>
import bpy, sys, math, mathutils

argv = sys.argv[sys.argv.index("--") + 1:]
source, prefix = argv[0], argv[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
if source.lower().endswith(".fbx"):
    bpy.ops.import_scene.fbx(filepath=source)
else:
    bpy.ops.import_scene.gltf(filepath=source)

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
tris = 0
lo = mathutils.Vector((1e9, 1e9, 1e9)); hi = -lo
for o in meshes:
    o.data.calc_loop_triangles()
    tris += len(o.data.loop_triangles)
    for v in o.data.vertices:
        w = o.matrix_world @ v.co
        lo = mathutils.Vector(map(min, lo, w)); hi = mathutils.Vector(map(max, hi, w))
size = hi - lo
print(f"REPORT meshes {len(meshes)}, triangles {tris}, size x {size.x:.3f} y {size.y:.3f} z {size.z:.3f} (Blender z is up)")
print(f"REPORT min {tuple(round(c,3) for c in lo)} max {tuple(round(c,3) for c in hi)}")
for o in meshes:
    print(f"REPORT mesh {o.name}: verts {len(o.data.vertices)}, materials {[m.name for m in o.data.materials if m]}, armature parent {o.parent.type if o.parent else None}")
for img in bpy.data.images:
    print(f"REPORT image {img.name}: {img.size[0]}x{img.size[1]}")
print(f"REPORT armatures {[o.name for o in bpy.context.scene.objects if o.type == 'ARMATURE']}")

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
scene.render.resolution_x = 512
scene.render.resolution_y = 768
scene.render.film_transparent = True
world = bpy.data.worlds.new("World"); scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[1].default_value = 1.2

center = (lo + hi) / 2
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 5.0
scene.view_settings.view_transform = "Standard"
fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "SUN")); fill.data.energy = 2.0; scene.collection.objects.link(fill)
scene.collection.objects.link(sun)
cam_data = bpy.data.cameras.new("Cam"); cam_data.type = "ORTHO"; cam_data.ortho_scale = max(size.z, size.x) * 1.15
cam = bpy.data.objects.new("Cam", cam_data); scene.collection.objects.link(cam); scene.camera = cam

# glTF imports with -Y as the model's front in Blender. Views: yaw around z, elevation above the horizon.
views = {"front": (0, 0), "left": (90, 0), "back": (180, 0), "game_s": (0, 30), "game_se": (45, 30)}
for name, (yaw, elev) in views.items():
    direction = mathutils.Vector((math.sin(math.radians(yaw)) * math.cos(math.radians(elev)) * -1,
                                  -math.cos(math.radians(yaw)) * math.cos(math.radians(elev)),
                                  math.sin(math.radians(elev))))
    cam.location = center + direction * 10
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    # Key from the camera's upper left, fill from behind the other side, both turning with the camera.
    right = mathutils.Vector((math.cos(math.radians(yaw)), math.sin(math.radians(yaw)), 0))
    sun.rotation_euler = (-(direction - right * 0.6 + mathutils.Vector((0, 0, 0.8)))).to_track_quat("-Z", "Y").to_euler()
    fill.rotation_euler = (-(-direction + right * 0.8)).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = f"{prefix}_{name}.png"
    bpy.ops.render.render(write_still=True)
    print(f"REPORT wrote {scene.render.filepath}")
