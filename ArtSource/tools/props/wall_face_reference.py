# The crypt wall seen face on, three blocks wide (2026-10-26, a reference for the owner's AI-drawn walls): crypt_wall_1
# three times along the -y face, rendered straight at it, orthographic. Writes ArtSource/pixel/ai_exchange/
# wall_current_face.png (square: three blocks wide, the wall's full height).
#   Blender -b -P ArtSource/tools/props/wall_face_reference.py
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, mathutils
from kit import *
import themes

reset()
for i in range(3):
    before = set(bpy.data.objects)
    themes.crypt_wall(1)
    for obj in set(bpy.data.objects) - before:
        obj.location.x += (i - 1) * CELL
width = 3 * CELL
cam_data = bpy.data.cameras.new("Cam")
cam_data.type = "ORTHO"
cam_data.ortho_scale = max(width, themes.WALL_H)
cam = bpy.data.objects.new("Cam", cam_data)
bpy.context.scene.collection.objects.link(cam)
cam.location = (0, -10, themes.WALL_H / 2)
cam.rotation_euler = (math.radians(90), 0, 0)
scene = bpy.context.scene
scene.camera = cam
scene.render.resolution_x = scene.render.resolution_y = 1024
scene.render.film_transparent = False
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "ArtSource", "pixel", "ai_exchange", "wall_current_face.png")
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print("REPORT", out, "wall", round(width, 3), "x", round(themes.WALL_H, 3))
