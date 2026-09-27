# Art sources

Source models for the sprite bake (Docs/09-art-brief.md, 4.5). They are outside `Assets`, so Unity does not import
them; the rigged and animated FBX files that the bake uses go into `Assets/_Project/Art/Models/` once they exist.

| File | What it is |
|---|---|
| `wrathborn/body_leather.glb` | The Wrathborn's leather body, from image-to-3D (TRELLIS) on the approved A-pose turnaround, 2026-09-27. 8,013 triangles, one 2048 texture |
| `wrathborn/body_leather_for_mixamo.fbx` | The same, converted for Mixamo: 1.85 m tall, feet at the origin, centred, facing front, texture embedded |

Tools (run with Blender from the command line, `/Applications/Blender.app/Contents/MacOS/Blender -b -P <script> -- <args>`):

- `tools/glb_to_fbx.py <in.glb> <out.fbx> <height_m>`: the conversion above (height 0 keeps the size).
- `tools/inspect_and_preview.py <in.glb|in.fbx> <out_prefix>`: prints triangles, size and textures, and renders
  front, left, back and game-angle previews.
