# Blender script: save every image in a GLB as PNG, named <prefix>_<n>.png (the first is <prefix>.png).
# Usage: Blender -b -P extract_textures.py -- <input.glb> <output_prefix>
import bpy, sys
argv = sys.argv[sys.argv.index("--") + 1:]
source, prefix = argv[0], argv[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source)
for i, image in enumerate(img for img in bpy.data.images if img.size[0] > 0):
    path = f"{prefix}.png" if i == 0 else f"{prefix}_{i}.png"
    image.filepath_raw = path
    image.file_format = "PNG"
    image.save()
    print(f"REPORT saved {path} {image.size[0]}x{image.size[1]}")
